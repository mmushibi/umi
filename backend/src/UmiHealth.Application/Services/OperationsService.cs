using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UmiHealth.Api.Controllers;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Application.Services
{
    public class OperationsService : IOperationsService
    {
        private readonly ITenantService _tenantService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IAuthenticationService _authService;
        private readonly IDataSyncService _dataSyncService;
        private readonly ILogger<OperationsService> _logger;

        public OperationsService(
            ITenantService tenantService,
            ISubscriptionService subscriptionService,
            IAuthenticationService authService,
            IDataSyncService dataSyncService,
            ILogger<OperationsService> logger)
        {
            _tenantService = tenantService;
            _subscriptionService = subscriptionService;
            _authService = authService;
            _dataSyncService = dataSyncService;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            try
            {
                var tenants = await _tenantService.GetAllAsync();
                var subscriptions = await _subscriptionService.GetAllAsync();
                var users = await _authService.GetAllUsersAsync();

                var activeSubscriptions = subscriptions.Count(s => s.Status == "active");
                var expiringSoon = subscriptions.Count(s => 
                    s.EndDate.HasValue && 
                    s.EndDate.Value <= DateTime.UtcNow.AddDays(30) && 
                    s.Status == "active");

                var monthlyRevenue = subscriptions
                    .Where(s => s.Status == "active" && s.BillingCycle == "monthly")
                    .Sum(s => s.Amount);

                var yearlyRevenue = subscriptions
                    .Where(s => s.Status == "active" && s.BillingCycle == "annually")
                    .Sum(s => s.Amount) + (monthlyRevenue * 12);

                var newTenantsThisMonth = tenants.Count(t => 
                    t.CreatedAt >= DateTime.UtcNow.AddDays(-30));

                var newUsersThisMonth = users.Count(u => 
                    u.CreatedAt >= DateTime.UtcNow.AddDays(-30));

                return new DashboardStatsDto
                {
                    TotalTenants = tenants.Count(),
                    ActiveSubscriptions = activeSubscriptions,
                    TotalUsers = users.Count(),
                    ExpiringSoon = expiringSoon,
                    MonthlyRevenue = monthlyRevenue,
                    YearlyRevenue = yearlyRevenue,
                    NewTenantsThisMonth = newTenantsThisMonth,
                    NewUsersThisMonth = newUsersThisMonth
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                throw;
            }
        }

        public async Task<IEnumerable<RecentTenantDto>> GetRecentTenantsAsync(int count)
        {
            try
            {
                var tenants = await _tenantService.GetAllAsync();
                return tenants
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(count)
                    .Select(t => new RecentTenantDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Email = t.Settings.GetValueOrDefault("contact_email", "").ToString(),
                        Domain = $"{t.Subdomain}.umihealth.com",
                        CreatedDate = t.CreatedAt,
                        Status = t.Status
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent tenants");
                throw;
            }
        }

        public async Task<PagedResult<TenantDto>> GetTenantsAsync(int page, int pageSize, string? search = null, string? status = null)
        {
            try
            {
                var tenants = await _tenantService.GetAllAsync();
                
                var query = tenants.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(t => 
                        t.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.Subdomain.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                var totalCount = query.Count();
                var pagedTenants = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TenantDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Subdomain = t.Subdomain,
                        Status = t.Status,
                        SubscriptionPlan = t.SubscriptionPlan,
                        CreatedAt = t.CreatedAt,
                        ContactName = t.Settings.GetValueOrDefault("contact_name", "").ToString(),
                        ContactEmail = t.Settings.GetValueOrDefault("contact_email", "").ToString(),
                        ContactPhone = t.Settings.GetValueOrDefault("contact_phone", "").ToString(),
                        Address = t.Settings.GetValueOrDefault("address", "").ToString(),
                        Notes = t.Settings.GetValueOrDefault("notes", "").ToString()
                    });

                return new PagedResult<TenantDto>
                {
                    Data = pagedTenants,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenants");
                throw;
            }
        }

        public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request)
        {
            try
            {
                var tenant = new Tenant
                {
                    Name = request.Name,
                    Subdomain = request.Domain.Replace(".umihealth.com", "").ToLower(),
                    DatabaseName = $"umi_{request.Domain.Replace(".umihealth.com", "").ToLower()}",
                    SubscriptionPlan = request.SubscriptionPlan,
                    Status = "active",
                    Settings = new Dictionary<string, object>
                    {
                        ["contact_name"] = request.ContactName,
                        ["contact_email"] = request.ContactEmail,
                        ["contact_phone"] = request.ContactPhone ?? "",
                        ["address"] = request.Address ?? "",
                        ["notes"] = request.Notes ?? ""
                    }
                };

                var createdTenant = await _tenantService.CreateAsync(tenant);
                
                // Create initial subscription
                await _subscriptionService.CreateSubscriptionAsync(createdTenant.Id, new CreateSubscriptionRequest
                {
                    PlanType = request.SubscriptionPlan,
                    BillingCycle = "monthly",
                    Amount = GetPlanPrice(request.SubscriptionPlan),
                    Currency = "USD",
                    AutoRenew = true
                });

                return new TenantDto
                {
                    Id = createdTenant.Id,
                    Name = createdTenant.Name,
                    Subdomain = createdTenant.Subdomain,
                    Status = createdTenant.Status,
                    SubscriptionPlan = createdTenant.SubscriptionPlan,
                    CreatedAt = createdTenant.CreatedAt,
                    ContactName = request.ContactName,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    Notes = request.Notes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tenant");
                throw;
            }
        }

        public async Task<TenantDto?> UpdateTenantAsync(Guid id, UpdateTenantRequest request)
        {
            try
            {
                var existingTenant = await _tenantService.GetByIdAsync(id);
                if (existingTenant == null) return null;

                existingTenant.Name = request.Name;
                existingTenant.Subdomain = request.Domain.Replace(".umihealth.com", "").ToLower();
                existingTenant.SubscriptionPlan = request.SubscriptionPlan;
                existingTenant.Status = request.Status;
                existingTenant.UpdatedAt = DateTime.UtcNow;

                existingTenant.Settings["contact_name"] = request.ContactName;
                existingTenant.Settings["contact_email"] = request.ContactEmail;
                existingTenant.Settings["contact_phone"] = request.ContactPhone ?? "";
                existingTenant.Settings["address"] = request.Address ?? "";
                existingTenant.Settings["notes"] = request.Notes ?? "";

                var updatedTenant = await _tenantService.UpdateAsync(id, existingTenant);
                
                return new TenantDto
                {
                    Id = updatedTenant.Id,
                    Name = updatedTenant.Name,
                    Subdomain = updatedTenant.Subdomain,
                    Status = updatedTenant.Status,
                    SubscriptionPlan = updatedTenant.SubscriptionPlan,
                    CreatedAt = updatedTenant.CreatedAt,
                    ContactName = request.ContactName,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Address = request.Address,
                    Notes = request.Notes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant");
                throw;
            }
        }

        public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null)
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                var tenants = await _tenantService.GetAllAsync();
                
                var query = users.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => 
                        (u.FirstName + " " + u.LastName).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    var isActive = status.Equals("active", StringComparison.OrdinalIgnoreCase);
                    query = query.Where(u => u.IsActive == isActive);
                }

                if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tenantGuid))
                {
                    query = query.Where(u => u.TenantId == tenantGuid);
                }

                var totalCount = query.Count();
                var pagedUsers = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Name = $"{u.FirstName} {u.LastName}",
                        Email = u.Email,
                        Role = u.Role,
                        Status = u.IsActive ? "Active" : "Inactive",
                        Phone = u.PhoneNumber,
                        Tenant = tenants.FirstOrDefault(t => t.Id == u.TenantId)?.Name ?? "Unknown",
                        LastActive = u.LastLogin?.ToString("yyyy-MM-dd") ?? "Never"
                    });

                return new PagedResult<UserDto>
                {
                    Data = pagedUsers,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                throw;
            }
        }

        public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            try
            {
                var existingUser = await _authService.GetUserByIdAsync(id.ToString());
                if (existingUser == null) return null;

                var nameParts = request.Name.Split(' ', 2);
                existingUser.FirstName = nameParts.Length > 0 ? nameParts[0] : existingUser.FirstName;
                existingUser.LastName = nameParts.Length > 1 ? nameParts[1] : existingUser.LastName;
                existingUser.Email = request.Email;
                existingUser.Role = request.Role;
                existingUser.IsActive = request.Status.Equals("active", StringComparison.OrdinalIgnoreCase);
                existingUser.PhoneNumber = request.Phone;
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _authService.UpdateUserAsync(existingUser);

                return new UserDto
                {
                    Id = existingUser.Id,
                    Name = request.Name,
                    Email = request.Email,
                    Role = request.Role,
                    Status = request.Status,
                    Phone = request.Phone,
                    Tenant = (await _tenantService.GetByIdAsync(existingUser.TenantId))?.Name ?? "Unknown",
                    LastActive = existingUser.LastLogin?.ToString("yyyy-MM-dd") ?? "Never"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                throw;
            }
        }

        public async Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null)
        {
            try
            {
                var subscriptions = await _subscriptionService.GetAllAsync();
                var tenants = await _tenantService.GetAllAsync();
                
                var query = subscriptions.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => 
                        s.PlanType.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tenantGuid))
                {
                    query = query.Where(s => s.TenantId == tenantGuid);
                }

                var totalCount = query.Count();
                var pagedSubscriptions = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new SubscriptionDto
                    {
                        Id = s.Id,
                        TenantId = s.TenantId,
                        TenantName = tenants.FirstOrDefault(t => t.Id == s.TenantId)?.Name ?? "Unknown",
                        TenantDomain = $"{tenants.FirstOrDefault(t => t.Id == s.TenantId)?.Subdomain}.umihealth.com",
                        Plan = s.PlanType,
                        BillingCycle = s.BillingCycle,
                        Status = s.Status,
                        NextBilling = s.EndDate?.ToString("yyyy-MM-dd"),
                        Price = s.Amount
                    });

                return new PagedResult<SubscriptionDto>
                {
                    Data = pagedSubscriptions,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions");
                throw;
            }
        }

        public async Task<SubscriptionDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequest request)
        {
            try
            {
                var existingSubscription = await _subscriptionService.GetByIdAsync(id);
                if (existingSubscription == null) return null;

                existingSubscription.PlanType = request.Plan;
                existingSubscription.BillingCycle = request.BillingCycle;
                existingSubscription.Amount = request.Price;
                existingSubscription.Status = request.Status;
                existingSubscription.EndDate = request.NextBilling;
                existingSubscription.UpdatedAt = DateTime.UtcNow;

                var updatedSubscription = await _subscriptionService.UpdateAsync(existingSubscription);
                var tenants = await _tenantService.GetAllAsync();

                return new SubscriptionDto
                {
                    Id = updatedSubscription.Id,
                    TenantId = updatedSubscription.TenantId,
                    TenantName = tenants.FirstOrDefault(t => t.Id == updatedSubscription.TenantId)?.Name ?? "Unknown",
                    TenantDomain = $"{tenants.FirstOrDefault(t => t.Id == updatedSubscription.TenantId)?.Subdomain}.umihealth.com",
                    Plan = updatedSubscription.PlanType,
                    BillingCycle = updatedSubscription.BillingCycle,
                    Status = updatedSubscription.Status,
                    NextBilling = updatedSubscription.EndDate?.ToString("yyyy-MM-dd"),
                    Price = updatedSubscription.Amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription");
                throw;
            }
        }

        public async Task<SubscriptionDto?> UpgradeSubscriptionAsync(Guid id, UpgradeSubscriptionRequest request)
        {
            try
            {
                var existingSubscription = await _subscriptionService.GetByIdAsync(id);
                if (existingSubscription == null) return null;

                existingSubscription.PlanType = request.TargetPlan;
                existingSubscription.Amount = GetPlanPrice(request.TargetPlan);
                existingSubscription.UpdatedAt = DateTime.UtcNow;

                var updatedSubscription = await _subscriptionService.UpdateAsync(existingSubscription);
                var tenants = await _tenantService.GetAllAsync();

                return new SubscriptionDto
                {
                    Id = updatedSubscription.Id,
                    TenantId = updatedSubscription.TenantId,
                    TenantName = tenants.FirstOrDefault(t => t.Id == updatedSubscription.TenantId)?.Name ?? "Unknown",
                    TenantDomain = $"{tenants.FirstOrDefault(t => t.Id == updatedSubscription.TenantId)?.Subdomain}.umihealth.com",
                    Plan = updatedSubscription.PlanType,
                    BillingCycle = updatedSubscription.BillingCycle,
                    Status = updatedSubscription.Status,
                    NextBilling = updatedSubscription.EndDate?.ToString("yyyy-MM-dd"),
                    Price = updatedSubscription.Amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading subscription");
                throw;
            }
        }

        public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // For now, return mock data - in real implementation, this would query a transactions table
                var transactions = GenerateMockTransactions();
                var tenants = await _tenantService.GetAllAsync();
                
                var query = transactions.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(t => 
                        t.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        t.TenantName.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tenantGuid))
                {
                    query = query.Where(t => t.TenantId == tenantGuid);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(t => DateTime.Parse(t.Date) >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(t => DateTime.Parse(t.Date) <= endDate.Value);
                }

                var totalCount = query.Count();
                var pagedTransactions = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return new PagedResult<TransactionDto>
                {
                    Data = pagedTransactions,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                throw;
            }
        }

        public async Task<TransactionReceiptDto?> GenerateTransactionReceiptAsync(Guid id)
        {
            try
            {
                // Mock implementation - in real scenario, generate PDF receipt
                return new TransactionReceiptDto
                {
                    Content = new byte[0], // PDF content would go here
                    FileName = $"receipt-{id}.pdf",
                    ContentType = "application/pdf"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating transaction receipt");
                throw;
            }
        }

        public async Task<SyncStatusDto> GetSyncStatusAsync()
        {
            try
            {
                return await _dataSyncService.GetSyncStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync status");
                throw;
            }
        }

        public async Task TriggerSyncAsync(string syncType)
        {
            try
            {
                await _dataSyncService.TriggerSyncAsync(syncType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering sync");
                throw;
            }
        }

        private decimal GetPlanPrice(string plan)
        {
            return plan.ToLower() switch
            {
                "basic" => 99m,
                "professional" => 299m,
                "enterprise" => 999m,
                _ => 99m
            };
        }

        private List<TransactionDto> GenerateMockTransactions()
        {
            return new List<TransactionDto>
            {
                new TransactionDto
                {
                    Id = "TRX001",
                    TenantId = Guid.NewGuid(),
                    TenantName = "MedCare Pharmacy",
                    TenantDomain = "medcare.umihealth.com",
                    Type = "Subscription",
                    Amount = 299.00m,
                    Date = "2024-01-15",
                    Status = "Completed"
                },
                new TransactionDto
                {
                    Id = "TRX002",
                    TenantId = Guid.NewGuid(),
                    TenantName = "HealthPlus Drugs",
                    TenantDomain = "healthplus.umihealth.com",
                    Type = "User License",
                    Amount = 49.00m,
                    Date = "2024-01-10",
                    Status = "Completed"
                }
                // Add more mock transactions as needed
            };
        }
    }

    // Additional DTOs
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Tenant { get; set; } = string.Empty;
        public string LastActive { get; set; } = string.Empty;
    }

    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string TenantDomain { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string NextBilling { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class TransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string TenantDomain { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class TransactionReceiptDto
    {
        public byte[] Content { get; set; } = new byte[0];
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }

    public class CreateSubscriptionRequest
    {
        public string PlanType { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool AutoRenew { get; set; }
    }
}
