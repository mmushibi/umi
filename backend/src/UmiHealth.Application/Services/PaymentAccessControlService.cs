using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IPaymentAccessControlService
    {
        Task<bool> HasPermissionAsync(Guid userId, string permission, Guid? tenantId = null);
        Task<bool> CanAccessPaymentAsync(Guid userId, Guid paymentId);
        Task<bool> CanProcessRefundAsync(Guid userId, decimal refundAmount);
        Task<bool> CanAccessPaymentPlanAsync(Guid userId, Guid planId);
        Task<bool> CanViewReportsAsync(Guid userId, string reportType);
        Task<List<UserPermission>> GetUserPermissionsAsync(Guid userId);
        Task<bool> GrantPermissionAsync(Guid userId, string permission, Guid grantedBy, Guid? tenantId = null);
        Task<bool> RevokePermissionAsync(Guid userId, string permission, Guid revokedBy);
        Task<List<AccessLogEntry>> GetAccessLogsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class PaymentAccessControlService : IPaymentAccessControlService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PaymentAccessControlService> _logger;
        private readonly IAuditTrailService _auditService;

        public PaymentAccessControlService(
            SharedDbContext context,
            ILogger<PaymentAccessControlService> logger,
            IAuditTrailService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string permission, Guid? tenantId = null)
        {
            try
            {
                var userPermissions = await _context.UserPermissions
                    .Include(up => up.Role)
                        .ThenInclude(r => r.Permissions)
                    .Where(up => up.UserId == userId && 
                                 up.IsActive && 
                                 (tenantId == null || up.TenantId == tenantId))
                    .ToListAsync();

                // Check direct user permissions
                var directPermissions = userPermissions
                    .Where(up => up.Permission != null)
                    .Any(up => up.Permission.Code == permission);

                // Check role-based permissions
                var rolePermissions = userPermissions
                    .Where(up => up.Role != null)
                    .SelectMany(up => up.Role.Permissions)
                    .Any(p => p.Code == permission);

                var hasPermission = directPermissions || rolePermissions;

                // Log access check
                await LogAccessCheckAsync(userId, permission, hasPermission, tenantId);

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<bool> CanAccessPaymentAsync(Guid userId, Guid paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Sale)
                    .ThenInclude(s => s.Branch)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return false;

            // Check if user can access all payments
            if (await HasPermissionAsync(userId, "payments.view.all"))
                return true;

            // Check if user can access branch payments
            if (payment.Sale?.BranchId != null)
            {
                var userBranches = await _context.UserBranches
                    .Where(ub => ub.UserId == userId && ub.IsActive)
                    .Select(ub => ub.BranchId)
                    .ToListAsync();

                if (userBranches.Contains(payment.Sale.BranchId.Value))
                    return true;
            }

            // Check if user created the payment
            if (payment.CreatedBy == userId)
                return true;

            return false;
        }

        public async Task<bool> CanProcessRefundAsync(Guid userId, decimal refundAmount)
        {
            // Check refund permissions
            if (!await HasPermissionAsync(userId, "payments.refund"))
                return false;

            // Check refund amount limits
            var refundLimits = await GetRefundLimitsAsync(userId);
            if (refundAmount > refundLimits.MaxSingleRefund)
                return false;

            // Check daily refund limits
            var todayRefunds = await GetTodayRefundTotalAsync(userId);
            if (todayRefunds + refundAmount > refundLimits.MaxDailyRefund)
                return false;

            return true;
        }

        public async Task<bool> CanAccessPaymentPlanAsync(Guid userId, Guid planId)
        {
            var paymentPlan = await _context.PaymentPlans
                .Include(pp => pp.Customer)
                .FirstOrDefaultAsync(pp => pp.Id == planId);

            if (paymentPlan == null)
                return false;

            // Check if user can access all payment plans
            if (await HasPermissionAsync(userId, "payment_plans.view.all"))
                return true;

            // Check if user is assigned to the customer
            if (await HasPermissionAsync(userId, "payment_plans.view.assigned"))
            {
                var assignedCustomers = await _context.UserCustomerAssignments
                    .Where(uca => uca.UserId == userId && uca.IsActive)
                    .Select(uca => uca.CustomerId)
                    .ToListAsync();

                if (assignedCustomers.Contains(paymentPlan.CustomerId))
                    return true;
            }

            // Check if user created the plan
            if (paymentPlan.CreatedBy == userId)
                return true;

            return false;
        }

        public async Task<bool> CanViewReportsAsync(Guid userId, string reportType)
        {
            var reportPermissions = new Dictionary<string, string>
            {
                { "payment_analytics", "reports.payments.analytics" },
                { "refund_report", "reports.refunds.view" },
                { "tax_report", "reports.tax.view" },
                { "revenue_report", "reports.revenue.view" },
                { "customer_report", "reports.customers.view" },
                { "audit_log", "audit.view" }
            };

            if (!reportPermissions.ContainsKey(reportType))
                return false;

            return await HasPermissionAsync(userId, reportPermissions[reportType]);
        }

        public async Task<List<UserPermission>> GetUserPermissionsAsync(Guid userId)
        {
            var permissions = await _context.UserPermissions
                .Include(up => up.Role)
                    .ThenInclude(r => r.Permissions)
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId && up.IsActive)
                .ToListAsync();

            var result = new List<UserPermission>();

            // Add role-based permissions
            foreach (var userPermission in permissions.Where(up => up.Role != null))
            {
                foreach (var rolePermission in userPermission.Role.Permissions)
                {
                    result.Add(new UserPermission
                    {
                        UserId = userId,
                        PermissionCode = rolePermission.Code,
                        PermissionName = rolePermission.Name,
                        Source = "Role",
                        SourceName = userPermission.Role.Name
                    });
                }
            }

            // Add direct permissions
            foreach (var userPermission in permissions.Where(up => up.Permission != null))
            {
                result.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionCode = userPermission.Permission.Code,
                    PermissionName = userPermission.Permission.Name,
                    Source = "Direct",
                    SourceName = "Direct Assignment"
                });
            }

            return result.Distinct().ToList();
        }

        public async Task<bool> GrantPermissionAsync(Guid userId, string permission, Guid grantedBy, Guid? tenantId = null)
        {
            try
            {
                // Check if granter has permission to grant
                if (!await HasPermissionAsync(grantedBy, "permissions.grant"))
                    return false;

                var permissionEntity = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Code == permission);

                if (permissionEntity == null)
                    return false;

                // Check if permission already exists
                var existingPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && 
                                             up.PermissionId == permissionEntity.Id &&
                                             (tenantId == null || up.TenantId == tenantId));

                if (existingPermission != null)
                {
                    existingPermission.IsActive = true;
                    existingPermission.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    existingPermission = new UserPermissionEntity
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        PermissionId = permissionEntity.Id,
                        TenantId = tenantId,
                        GrantedBy = grantedBy,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserPermissions.Add(existingPermission);
                }

                await _context.SaveChangesAsync();

                // Log permission grant
                await _auditService.LogActivityAsync(new AuditLogEntry
                {
                    TenantId = tenantId ?? Guid.Empty,
                    UserId = grantedBy,
                    Action = "PermissionGranted",
                    EntityType = "UserPermission",
                    EntityId = userId.ToString(),
                    Description = $"Granted permission '{permission}' to user {userId}",
                    AdditionalData = System.Text.Json.JsonSerializer.Serialize(new { permission, grantedBy })
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to grant permission {Permission} to user {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<bool> RevokePermissionAsync(Guid userId, string permission, Guid revokedBy)
        {
            try
            {
                // Check if revoker has permission to revoke
                if (!await HasPermissionAsync(revokedBy, "permissions.revoke"))
                    return false;

                var permissionEntity = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Code == permission);

                if (permissionEntity == null)
                    return false;

                var userPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionEntity.Id);

                if (userPermission != null)
                {
                    userPermission.IsActive = false;
                    userPermission.UpdatedAt = DateTime.UtcNow;
                    userPermission.RevokedBy = revokedBy;
                    userPermission.RevokedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    // Log permission revocation
                    await _auditService.LogActivityAsync(new AuditLogEntry
                    {
                        UserId = revokedBy,
                        Action = "PermissionRevoked",
                        EntityType = "UserPermission",
                        EntityId = userId.ToString(),
                        Description = $"Revoked permission '{permission}' from user {userId}",
                        AdditionalData = System.Text.Json.JsonSerializer.Serialize(new { permission, revokedBy })
                    });

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke permission {Permission} from user {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<List<AccessLogEntry>> GetAccessLogsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AccessLogs
                .Where(al => al.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .Take(1000)
                .ToListAsync();
        }

        private async Task<RefundLimits> GetRefundLimitsAsync(Guid userId)
        {
            var userRole = await _context.UserPermissions
                .Include(up => up.Role)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.Role != null);

            // Default limits
            var limits = new RefundLimits
            {
                MaxSingleRefund = 1000,
                MaxDailyRefund = 5000
            };

            // Adjust based on role
            if (userRole?.Role?.Name == "Manager")
            {
                limits.MaxSingleRefund = 5000;
                limits.MaxDailyRefund = 20000;
            }
            else if (userRole?.Role?.Name == "Administrator")
            {
                limits.MaxSingleRefund = decimal.MaxValue;
                limits.MaxDailyRefund = decimal.MaxValue;
            }

            return limits;
        }

        private async Task<decimal> GetTodayRefundTotalAsync(Guid userId)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _context.RefundRequests
                .Where(rr => rr.RequestedBy == userId && 
                           rr.RequestedAt >= today && 
                           rr.RequestedAt < tomorrow &&
                           rr.Status == "completed")
                .SumAsync(rr => rr.Amount);
        }

        private async Task LogAccessCheckAsync(Guid userId, string permission, bool granted, Guid? tenantId)
        {
            var logEntry = new AccessLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId ?? Guid.Empty,
                Permission = permission,
                AccessGranted = granted,
                Timestamp = DateTime.UtcNow,
                IpAddress = "System", // Would come from HttpContext in real implementation
                UserAgent = "AccessControlService"
            };

            _context.AccessLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }
    }

    // Supporting DTOs and Entities
    public class UserPermission
    {
        public Guid UserId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // Role, Direct
        public string SourceName { get; set; } = string.Empty;
    }

    public class RefundLimits
    {
        public decimal MaxSingleRefund { get; set; }
        public decimal MaxDailyRefund { get; set; }
    }

    public class AccessLogEntry
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Permission { get; set; } = string.Empty;
        public bool AccessGranted { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }

    // Entity classes
    public class UserPermissionEntity : TenantEntity
    {
        public Guid UserId { get; set; }
        public Guid? RoleId { get; set; }
        public Guid? PermissionId { get; set; }
        public Guid GrantedBy { get; set; }
        public Guid? RevokedBy { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual User User { get; set; } = null!;
        public virtual Role? Role { get; set; }
        public virtual Permission? Permission { get; set; }
    }

    public class Role : TenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public virtual List<RolePermission> Permissions { get; set; } = new();
        public virtual List<UserPermissionEntity> UserPermissions { get; set; } = new();
    }

    public class Permission : TenantEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public virtual List<RolePermission> RolePermissions { get; set; } = new();
        public virtual List<UserPermissionEntity> UserPermissions { get; set; } = new();
    }

    public class RolePermission : TenantEntity
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
        
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }

    public class UserBranch : TenantEntity
    {
        public Guid UserId { get; set; }
        public Guid BranchId { get; set; }
        public bool IsActive { get; set; } = true;
        
        public virtual User User { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
    }

    public class UserCustomerAssignment : TenantEntity
    {
        public Guid UserId { get; set; }
        public Guid CustomerId { get; set; }
        public bool IsActive { get; set; } = true;
        
        public virtual User User { get; set; } = null!;
        public virtual Customer Customer { get; set; } = null!;
    }

    public class AccessLog : TenantEntity
    {
        public Guid UserId { get; set; }
        public string Permission { get; set; } = string.Empty;
        public bool AccessGranted { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        
        public virtual User User { get; set; } = null!;
    }
}
