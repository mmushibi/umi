using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IOnboardingService
    {
        Task<TenantOnboardingDto> GetTenantOnboardingAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<bool> UpdateTenantOnboardingAsync(Guid tenantId, UpdateOnboardingRequest request, CancellationToken cancellationToken = default);
        Task<List<TenantOnboardingDto>> GetAllTenantsOnboardingAsync(CancellationToken cancellationToken = default);
    }

    public class OnboardingService : IOnboardingService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<OnboardingService> _logger;

        public OnboardingService(SharedDbContext context, ILogger<OnboardingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TenantOnboardingDto> GetTenantOnboardingAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenant = await _context.Tenants
                    .Include(t => t.Branches)
                    .Include(t => t.Users)
                    .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

                if (tenant == null)
                {
                    return null;
                }

                var settings = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(tenant.Settings))
                {
                    try
                    {
                        settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tenant.Settings);
                    }
                    catch
                    {
                        settings = new Dictionary<string, object>();
                    }
                }

                var mainBranch = tenant.Branches?.FirstOrDefault(b => b.IsMainBranch);
                var adminUser = tenant.Users?.FirstOrDefault(u => u.Role == "admin");

                return new TenantOnboardingDto
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    PharmacyType = settings.GetValueOrDefault("pharmacyType", "").ToString(),
                    LicenseNumber = mainBranch?.LicenseNumber,
                    LicenseExpiryDate = settings.GetValueOrDefault("licenseExpiryDate", "").ToString(),
                    ZamraNumber = settings.GetValueOrDefault("zamraNumber", "").ToString(),
                    PhysicalAddress = settings.GetValueOrDefault("physicalAddress", "").ToString(),
                    Province = settings.GetValueOrDefault("province", "").ToString(),
                    City = settings.GetValueOrDefault("city", "").ToString(),
                    PostalCode = settings.GetValueOrDefault("postalCode", "").ToString(),
                    OperatingHours = "{}",
                    ContactEmail = mainBranch?.Email,
                    PhoneNumber = mainBranch?.Phone,
                    EmergencyContact = settings.GetValueOrDefault("emergencyContact", "").ToString(),
                    Website = settings.GetValueOrDefault("website", "").ToString(),
                    PharmacistCount = Convert.ToInt32(settings.GetValueOrDefault("pharmacistCount", 0)),
                    Services = settings.GetValueOrDefault("services", new { }) as Dictionary<string, object> ?? new Dictionary<string, object>(),
                    AdminFullName = $"{adminUser?.FirstName} {adminUser?.LastName}".Trim(),
                    AdminEmail = adminUser?.Email,
                    AdminTitle = settings.GetValueOrDefault("adminTitle", "").ToString(),
                    AdminExperience = Convert.ToInt32(settings.GetValueOrDefault("adminExperience", 0)),
                    OnboardingCompleted = Convert.ToBoolean(settings.GetValueOrDefault("onboardingCompleted", false)),
                    OnboardingCompletedAt = settings.GetValueOrDefault("onboardingCompletedAt", "").ToString(),
                    CreatedAt = tenant.CreatedAt,
                    UpdatedAt = tenant.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant onboarding data for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task<bool> UpdateTenantOnboardingAsync(Guid tenantId, UpdateOnboardingRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var tenant = await _context.Tenants
                    .Include(t => t.Branches)
                    .Include(t => t.Users)
                    .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

                if (tenant == null)
                {
                    return false;
                }

                var settings = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(tenant.Settings))
                {
                    try
                    {
                        settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tenant.Settings);
                    }
                    catch
                    {
                        settings = new Dictionary<string, object>();
                    }
                }

                // Update settings
                settings["pharmacyType"] = request.PharmacyType;
                settings["zamraNumber"] = request.ZamraNumber;
                settings["physicalAddress"] = request.PhysicalAddress;
                settings["province"] = request.Province;
                settings["city"] = request.City;
                settings["postalCode"] = request.PostalCode;
                settings["emergencyContact"] = request.EmergencyContact;
                settings["website"] = request.Website;
                settings["pharmacistCount"] = request.PharmacistCount;
                settings["services"] = request.Services;
                settings["adminTitle"] = request.AdminTitle;
                settings["adminExperience"] = request.AdminExperience;
                settings["onboardingCompleted"] = true;
                settings["onboardingCompletedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                tenant.Settings = System.Text.Json.JsonSerializer.Serialize(settings);
                tenant.UpdatedAt = DateTime.UtcNow;

                // Update main branch
                var mainBranch = tenant.Branches?.FirstOrDefault(b => b.IsMainBranch);
                if (mainBranch != null)
                {
                    mainBranch.LicenseNumber = request.LicenseNumber;
                    if (!string.IsNullOrEmpty(request.OperatingHours))
                    {
                        try
                        {
                            mainBranch.OperatingHours = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(request.OperatingHours) ?? new Dictionary<string, object>();
                        }
                        catch
                        {
                            mainBranch.OperatingHours = new Dictionary<string, object>();
                        }
                    }
                    else
                    {
                        mainBranch.OperatingHours = new Dictionary<string, object>();
                    }
                    mainBranch.Email = request.ContactEmail;
                    mainBranch.Phone = request.PhoneNumber;
                    mainBranch.UpdatedAt = DateTime.UtcNow;
                }

                // Update admin user
                var adminUser = tenant.Users?.FirstOrDefault(u => u.Role == "admin");
                if (adminUser != null && !string.IsNullOrEmpty(request.AdminFullName))
                {
                    var nameParts = request.AdminFullName.Split(' ', 2);
                    adminUser.FirstName = nameParts.Length > 0 ? nameParts[0] : adminUser.FirstName;
                    adminUser.LastName = nameParts.Length > 1 ? nameParts[1] : adminUser.LastName;
                    adminUser.Email = request.AdminEmail ?? adminUser.Email;
                    adminUser.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated onboarding data for tenant {TenantId}", tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant onboarding data for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<List<TenantOnboardingDto>> GetAllTenantsOnboardingAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var tenants = await _context.Tenants
                    .Include(t => t.Branches)
                    .Include(t => t.Users)
                    .ToListAsync(cancellationToken);

                var result = new List<TenantOnboardingDto>();

                foreach (var tenant in tenants)
                {
                    var settings = new Dictionary<string, object>();
                    if (!string.IsNullOrEmpty(tenant.Settings))
                    {
                        try
                        {
                            settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tenant.Settings);
                        }
                        catch
                        {
                            settings = new Dictionary<string, object>();
                        }
                    }

                    var mainBranch = tenant.Branches?.FirstOrDefault(b => b.IsMainBranch);
                    var adminUser = tenant.Users?.FirstOrDefault(u => u.Role == "admin");

                    result.Add(new TenantOnboardingDto
                    {
                        TenantId = tenant.Id,
                        TenantName = tenant.Name,
                        PharmacyType = settings.GetValueOrDefault("pharmacyType", "").ToString(),
                        LicenseNumber = mainBranch?.LicenseNumber,
                        LicenseExpiryDate = settings.GetValueOrDefault("licenseExpiryDate", "").ToString(),
                        ZamraNumber = settings.GetValueOrDefault("zamraNumber", "").ToString(),
                        PhysicalAddress = settings.GetValueOrDefault("physicalAddress", "").ToString(),
                        Province = settings.GetValueOrDefault("province", "").ToString(),
                        City = settings.GetValueOrDefault("city", "").ToString(),
                        PostalCode = settings.GetValueOrDefault("postalCode", "").ToString(),
                        OperatingHours = mainBranch?.OperatingHours != null ? System.Text.Json.JsonSerializer.Serialize(mainBranch.OperatingHours) : "{}",
                        ContactEmail = mainBranch?.Email,
                        PhoneNumber = mainBranch?.Phone,
                        EmergencyContact = settings.GetValueOrDefault("emergencyContact", "").ToString(),
                        Website = settings.GetValueOrDefault("website", "").ToString(),
                        PharmacistCount = Convert.ToInt32(settings.GetValueOrDefault("pharmacistCount", 0)),
                        Services = settings.GetValueOrDefault("services", new { }) as Dictionary<string, object> ?? new Dictionary<string, object>(),
                        AdminFullName = $"{adminUser?.FirstName} {adminUser?.LastName}".Trim(),
                        AdminEmail = adminUser?.Email,
                        AdminTitle = settings.GetValueOrDefault("adminTitle", "").ToString(),
                        AdminExperience = Convert.ToInt32(settings.GetValueOrDefault("adminExperience", 0)),
                        OnboardingCompleted = Convert.ToBoolean(settings.GetValueOrDefault("onboardingCompleted", false)),
                        OnboardingCompletedAt = settings.GetValueOrDefault("onboardingCompletedAt", "").ToString(),
                        CreatedAt = tenant.CreatedAt,
                        UpdatedAt = tenant.UpdatedAt
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tenants onboarding data");
                return new List<TenantOnboardingDto>();
            }
        }
    }

    public class TenantOnboardingDto
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
        public string PharmacyType { get; set; }
        public string LicenseNumber { get; set; }
        public string LicenseExpiryDate { get; set; }
        public string ZamraNumber { get; set; }
        public string PhysicalAddress { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string OperatingHours { get; set; }
        public string ContactEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string EmergencyContact { get; set; }
        public string Website { get; set; }
        public int PharmacistCount { get; set; }
        public Dictionary<string, object> Services { get; set; }
        public string AdminFullName { get; set; }
        public string AdminEmail { get; set; }
        public string AdminTitle { get; set; }
        public int AdminExperience { get; set; }
        public bool OnboardingCompleted { get; set; }
        public string OnboardingCompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateOnboardingRequest
    {
        public string PharmacyType { get; set; }
        public string LicenseNumber { get; set; }
        public string LicenseExpiryDate { get; set; }
        public string ZamraNumber { get; set; }
        public string PhysicalAddress { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string OperatingHours { get; set; }
        public string ContactEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string EmergencyContact { get; set; }
        public string Website { get; set; }
        public int PharmacistCount { get; set; }
        public Dictionary<string, object> Services { get; set; }
        public string AdminFullName { get; set; }
        public string AdminEmail { get; set; }
        public string AdminTitle { get; set; }
        public int AdminExperience { get; set; }
    }
}
