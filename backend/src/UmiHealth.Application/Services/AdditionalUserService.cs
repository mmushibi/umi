using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IAdditionalUserService
    {
        Task<AdditionalUserRequestResult> RequestAdditionalUserAsync(Guid tenantId, CreateAdditionalUserRequest request);
        Task<IEnumerable<AdditionalUserRequestDto>> GetPendingRequestsAsync();
        Task<bool> ApproveAdditionalUserRequestAsync(string requestId, Guid approvedBy);
        Task<bool> RejectAdditionalUserRequestAsync(string requestId, Guid approvedBy, string rejectionReason);
        Task<IEnumerable<AdditionalUserChargeDto>> GetMonthlyChargesAsync(Guid tenantId, int year, int month);
        Task<AdditionalUserSummaryDto> GetAdditionalUserSummaryAsync(Guid tenantId);
        Task<bool> ProcessAdditionalUserPaymentAsync(string chargeId, PaymentDetails paymentDetails);
        Task NotifyOperationsAndSuperAdminAsync(string requestId);
    }

    public class AdditionalUserService : IAdditionalUserService
    {
        private readonly SharedDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdditionalUserService> _logger;

        public AdditionalUserService(
            SharedDbContext context,
            INotificationService notificationService,
            ILogger<AdditionalUserService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<AdditionalUserRequestResult> RequestAdditionalUserAsync(Guid tenantId, CreateAdditionalUserRequest request)
        {
            // Get tenant's current subscription and user count
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null)
                return new AdditionalUserRequestResult { Success = false, Message = "Tenant not found" };

            var currentUserCount = await _context.Users
                .CountAsync(u => u.TenantId == tenantId && u.IsActive);

            var maxAllowedUsers = await GetMaxAllowedUsersAsync(tenantId);
            
            // Check if additional user is needed
            if (currentUserCount <= maxAllowedUsers)
            {
                return new AdditionalUserRequestResult 
                { 
                    Success = false, 
                    Message = "No additional user charge required. Current users within subscription limit." 
                };
            }

            var additionalUsers = currentUserCount - maxAllowedUsers;
            var totalCharge = additionalUsers * 50.00m; // K50 per additional user

            // Create additional user request
            var additionalUserRequest = new AdditionalUserRequest
            {
                Id = Guid.NewGuid(),
                RequestId = GenerateRequestId(),
                TenantId = tenantId,
                UserEmail = request.UserEmail,
                UserFirstName = request.UserFirstName,
                UserLastName = request.UserLastName,
                UserRole = request.UserRole,
                BranchId = request.BranchId,
                RequestedBy = request.RequestedBy,
                SubscriptionPlanAtRequest = tenant.SubscriptionPlan,
                CurrentUserCount = currentUserCount,
                MaxAllowedUsers = maxAllowedUsers,
                ChargeAmount = totalCharge,
                Status = "pending_approval",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AdditionalUserRequests.Add(additionalUserRequest);
            await _context.SaveChangesAsync();

            // Notify operations and super admin
            await NotifyOperationsAndSuperAdminAsync(additionalUserRequest.RequestId);

            return new AdditionalUserRequestResult
            {
                Success = true,
                RequestId = additionalUserRequest.RequestId,
                ChargeAmount = totalCharge,
                AdditionalUsers = additionalUsers,
                Message = "Additional user request submitted for approval"
            };
        }

        public async Task<IEnumerable<AdditionalUserRequestDto>> GetPendingRequestsAsync()
        {
            return await _context.AdditionalUserRequests
                .Where(r => r.Status == "pending_approval")
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AdditionalUserRequestDto
                {
                    RequestId = r.RequestId,
                    TenantId = r.TenantId,
                    TenantName = _context.Tenants.Where(t => t.Id == r.TenantId).Select(t => t.Name).FirstOrDefault(),
                    UserEmail = r.UserEmail,
                    UserFirstName = r.UserFirstName,
                    UserLastName = r.UserLastName,
                    UserRole = r.UserRole,
                    BranchName = _context.Branches.Where(b => b.Id == r.BranchId).Select(b => b.Name).FirstOrDefault(),
                    RequestedBy = r.RequestedBy,
                    RequestedByEmail = _context.Users.Where(u => u.Id == r.RequestedBy).Select(u => u.Email).FirstOrDefault(),
                    SubscriptionPlanAtRequest = r.SubscriptionPlanAtRequest,
                    CurrentUserCount = r.CurrentUserCount,
                    MaxAllowedUsers = r.MaxAllowedUsers,
                    ChargeAmount = r.ChargeAmount,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> ApproveAdditionalUserRequestAsync(string requestId, Guid approvedBy)
        {
            var request = await _context.AdditionalUserRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return false;

            // Update request status
            request.Status = "approved";
            request.ApprovedBy = approvedBy;
            request.ApprovedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            // Create the additional user charge record
            var billingMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var charge = new AdditionalUserCharge
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UserId = request.UserCreatedId ?? Guid.NewGuid(), // Will be updated when user is created
                ChargeAmount = request.ChargeAmount,
                Currency = "ZMW",
                BillingMonth = billingMonth,
                Status = "pending_payment",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AdditionalUserCharges.Add(charge);
            await _context.SaveChangesAsync();

            // Notify tenant admin about approval
            await NotifyTenantAboutApprovalAsync(request, "approved");

            _logger.LogInformation($"Additional user request {requestId} approved by {approvedBy}");
            return true;
        }

        public async Task<bool> RejectAdditionalUserRequestAsync(string requestId, Guid approvedBy, string rejectionReason)
        {
            var request = await _context.AdditionalUserRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return false;

            request.Status = "rejected";
            request.ApprovedBy = approvedBy;
            request.ApprovedAt = DateTime.UtcNow;
            request.RejectionReason = rejectionReason;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify tenant admin about rejection
            await NotifyTenantAboutApprovalAsync(request, "rejected");

            _logger.LogInformation($"Additional user request {requestId} rejected by {approvedBy}. Reason: {rejectionReason}");
            return true;
        }

        public async Task<IEnumerable<AdditionalUserChargeDto>> GetMonthlyChargesAsync(Guid tenantId, int year, int month)
        {
            var billingMonth = new DateTime(year, month, 1);
            
            return await _context.AdditionalUserCharges
                .Where(c => c.TenantId == tenantId && c.BillingMonth == billingMonth)
                .Select(c => new AdditionalUserChargeDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserEmail = _context.Users.Where(u => u.Id == c.UserId).Select(u => u.Email).FirstOrDefault(),
                    ChargeAmount = c.ChargeAmount,
                    Currency = c.Currency,
                    BillingMonth = c.BillingMonth,
                    Status = c.Status,
                    PaymentReference = c.PaymentReference,
                    PaymentMethod = c.PaymentMethod,
                    PaymentDate = c.PaymentDate,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<AdditionalUserSummaryDto> GetAdditionalUserSummaryAsync(Guid tenantId)
        {
            var currentUserCount = await _context.Users
                .CountAsync(u => u.TenantId == tenantId && u.IsActive);

            var maxAllowedUsers = await GetMaxAllowedUsersAsync(tenantId);
            var additionalUsers = Math.Max(0, currentUserCount - maxAllowedUsers);
            var totalMonthlyCharge = additionalUsers * 50.00m;

            var currentMonthCharges = await _context.AdditionalUserCharges
                .Where(c => c.TenantId == tenantId && 
                           c.BillingMonth.Year == DateTime.UtcNow.Year &&
                           c.BillingMonth.Month == DateTime.UtcNow.Month)
                .ToListAsync();

            var pendingRequests = await _context.AdditionalUserRequests
                .CountAsync(r => r.TenantId == tenantId && r.Status == "pending_approval");

            return new AdditionalUserSummaryDto
            {
                CurrentUserCount = currentUserCount,
                MaxAllowedUsers = maxAllowedUsers,
                AdditionalUsers = additionalUsers,
                TotalMonthlyCharge = totalMonthlyCharge,
                CurrentMonthCharges = currentMonthCharges.Count,
                CurrentMonthPaidCharges = currentMonthCharges.Count(c => c.Status == "paid"),
                PendingRequests = pendingRequests,
                RequiresAdditionalCharge = additionalUsers > 0
            };
        }

        public async Task<bool> ProcessAdditionalUserPaymentAsync(string chargeId, PaymentDetails paymentDetails)
        {
            var charge = await _context.AdditionalUserCharges
                .FirstOrDefaultAsync(c => c.Id.ToString() == chargeId);

            if (charge == null)
                return false;

            charge.Status = "paid";
            charge.PaymentReference = paymentDetails.PaymentReference;
            charge.PaymentMethod = paymentDetails.PaymentMethod;
            charge.PaymentDate = DateTime.UtcNow;
            charge.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify about successful payment
            await NotifyPaymentProcessedAsync(charge, true);

            _logger.LogInformation($"Payment processed for additional user charge {chargeId}");
            return true;
        }

        public async Task NotifyOperationsAndSuperAdminAsync(string requestId)
        {
            var request = await _context.AdditionalUserRequests
                .Include(r => r.Tenant)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null) return;

            // Get operations and super admin users
            var adminUsers = await _context.Users
                .Where(u => u.IsActive && 
                           (u.Role == "operations" || u.Role == "super_admin"))
                .ToListAsync();

            foreach (var admin in adminUsers)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.TenantId,
                    admin.Id,
                    new CreateNotificationRequest
                    {
                        Type = "additional_user_request",
                        Title = "Additional User Request Pending Approval",
                        Message = $"{request.Tenant.Name} has requested {request.ChargeAmount / 50.0m} additional user(s) for K{request.ChargeAmount:F2}. User: {request.UserFirstName} {request.UserLastName} ({request.UserEmail})",
                        Data = new Dictionary<string, object>
                        {
                            { "requestId", request.RequestId },
                            { "tenantId", request.TenantId },
                            { "tenantName", request.Tenant.Name },
                            { "userEmail", request.UserEmail },
                            { "chargeAmount", request.ChargeAmount },
                            { "additionalUsers", request.ChargeAmount / 50.0m }
                        },
                        ActionUrl = $"/operations/additional-users/requests/{request.RequestId}",
                        IsHighPriority = true
                    });
            }
        }

        private async Task<int> GetMaxAllowedUsersAsync(Guid tenantId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return 0;

            // Get max users from subscription plan
            var subscriptionPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(sp => sp.Name == tenant.SubscriptionPlan);

            return subscriptionPlan?.MaxUsers ?? 0;
        }

        private async Task NotifyTenantAboutApprovalAsync(AdditionalUserRequest request, string status)
        {
            var tenantAdmins = await _context.Users
                .Where(u => u.TenantId == request.TenantId && 
                           u.IsActive && 
                           (u.Role == "admin" || u.Role == "super_admin"))
                .ToListAsync();

            var title = status == "approved" 
                ? "Additional User Request Approved" 
                : "Additional User Request Rejected";

            var message = status == "approved"
                ? $"Your request for additional user {request.UserFirstName} {request.UserLastName} has been approved. Please proceed with payment of K{request.ChargeAmount:F2}."
                : $"Your request for additional user {request.UserFirstName} {request.UserLastName} has been rejected. Reason: {request.RejectionReason}";

            foreach (var admin in tenantAdmins)
            {
                await _notificationService.CreateNotificationAsync(
                    request.TenantId,
                    admin.Id,
                    new CreateNotificationRequest
                    {
                        Type = "additional_user_approval",
                        Title = title,
                        Message = message,
                        Data = new Dictionary<string, object>
                        {
                            { "requestId", request.RequestId },
                            { "status", status },
                            { "chargeAmount", request.ChargeAmount }
                        },
                        ActionUrl = $"/admin/additional-users/requests/{request.RequestId}",
                        IsHighPriority = true
                    });
            }
        }

        private async Task NotifyPaymentProcessedAsync(AdditionalUserCharge charge, bool success)
        {
            var tenantAdmins = await _context.Users
                .Where(u => u.TenantId == charge.TenantId && 
                           u.IsActive && 
                           (u.Role == "admin" || u.Role == "super_admin"))
                .ToListAsync();

            var title = success ? "Payment Received" : "Payment Failed";
            var message = success
                ? $"Payment of K{charge.ChargeAmount:F2} for additional user has been received and processed."
                : $"Payment processing failed for additional user charge of K{charge.ChargeAmount:F2}.";

            foreach (var admin in tenantAdmins)
            {
                await _notificationService.CreateNotificationAsync(
                    charge.TenantId,
                    admin.Id,
                    new CreateNotificationRequest
                    {
                        Type = "additional_user_payment",
                        Title = title,
                        Message = message,
                        Data = new Dictionary<string, object>
                        {
                            { "chargeId", charge.Id },
                            { "amount", charge.ChargeAmount },
                            { "success", success }
                        },
                        ActionUrl = $"/admin/additional-users/charges/{charge.Id}",
                        IsHighPriority = success
                    });
            }
        }

        private string GenerateRequestId()
        {
            return $"AUR{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }

    // DTOs
    public class AdditionalUserRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public decimal ChargeAmount { get; set; }
        public int AdditionalUsers { get; set; }
    }

    public class CreateAdditionalUserRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
        public Guid RequestedBy { get; set; }
    }

    public class AdditionalUserRequestDto
    {
        public string RequestId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string? TenantName { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string? BranchName { get; set; }
        public Guid RequestedBy { get; set; }
        public string? RequestedByEmail { get; set; }
        public string SubscriptionPlanAtRequest { get; set; } = string.Empty;
        public int CurrentUserCount { get; set; }
        public int MaxAllowedUsers { get; set; }
        public decimal ChargeAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdditionalUserChargeDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? UserEmail { get; set; }
        public decimal ChargeAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime BillingMonth { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentReference { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdditionalUserSummaryDto
    {
        public int CurrentUserCount { get; set; }
        public int MaxAllowedUsers { get; set; }
        public int AdditionalUsers { get; set; }
        public decimal TotalMonthlyCharge { get; set; }
        public int CurrentMonthCharges { get; set; }
        public int CurrentMonthPaidCharges { get; set; }
        public int PendingRequests { get; set; }
        public bool RequiresAdditionalCharge { get; set; }
    }

    public class PaymentDetails
    {
        public string PaymentReference { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
