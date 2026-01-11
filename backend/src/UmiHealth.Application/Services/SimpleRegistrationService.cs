using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Identity.Services;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface ISimpleRegistrationService
    {
        Task<SimpleRegistrationResult> RegisterPharmacyAsync(SimpleRegistrationRequest request, CancellationToken cancellationToken = default);
    }

    public class SimpleRegistrationService : ISimpleRegistrationService
    {
        private readonly SharedDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<SimpleRegistrationService> _logger;

        public SimpleRegistrationService(
            SharedDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            ILogger<SimpleRegistrationService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<SimpleRegistrationResult> RegisterPharmacyAsync(SimpleRegistrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if pharmacy name already exists
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == request.PharmacyName.ToLower(), cancellationToken);

                if (existingTenant != null)
                {
                    return new SimpleRegistrationResult
                    {
                        Success = false,
                        Message = "A pharmacy with this name already exists"
                    };
                }

                // Generate email from pharmacy name
                var email = $"{request.PharmacyName.ToLower().Replace(" ", ".")}@pharmacy.local";

                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

                if (existingUser != null)
                {
                    return new SimpleRegistrationResult
                    {
                        Success = false,
                        Message = "An account with this email already exists"
                    };
                }

                // Create tenant
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = request.PharmacyName,
                    Subdomain = GenerateSubdomain(request.PharmacyName),
                    DatabaseName = $"umi_tenant_{Guid.NewGuid():N}",
                    Status = "active",
                    SubscriptionPlan = "trial",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Create main branch
                var branch = new Branch
                {
                    Id = Guid.NewGuid(),
                    Name = request.PharmacyName,
                    Code = GenerateBranchCode(request.PharmacyName),
                    Phone = request.PhoneNumber,
                    Email = email,
                    IsMainBranch = true,
                    IsActive = true
                };

                // Hash password
                var passwordHash = _passwordService.HashPassword(request.Password);

                // Create admin user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    BranchId = branch.Id,
                    UserName = email,
                    Email = email,
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = passwordHash,
                    FirstName = request.PharmacyName,
                    LastName = "Admin",
                    Role = "admin",
                    IsActive = true,
                    EmailConfirmed = false
                };

                // Create trial subscription
                var subscription = new Subscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    PlanType = "trial",
                    Status = "active",
                    BillingCycle = "monthly",
                    Currency = "ZMW",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save all entities
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    _context.Tenants.Add(tenant);
                    _context.Branches.Add(branch);
                    _context.Users.Add(user);
                    _context.Subscriptions.Add(subscription);

                    await _context.SaveChangesAsync(cancellationToken);

                    // Generate tokens
                    var accessToken = _jwtService.GenerateToken(user, tenant);
                    var refreshToken = GenerateRefreshToken();

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Successfully registered pharmacy {PharmacyName} with tenant ID {TenantId}", 
                        request.PharmacyName, tenant.Id);

                    return new SimpleRegistrationResult
                    {
                        Success = true,
                        Message = "Pharmacy registered successfully",
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        User = new SimpleUserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Role = user.Role
                        },
                        Tenant = new SimpleTenantDto
                        {
                            Id = tenant.Id,
                            Name = tenant.Name,
                            Subdomain = tenant.Subdomain
                        },
                        RequiresOnboarding = true
                    };
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during pharmacy registration for {PharmacyName}", request.PharmacyName);
                return new SimpleRegistrationResult
                {
                    Success = false,
                    Message = "An error occurred during registration"
                };
            }
        }

        private string GenerateSubdomain(string pharmacyName)
        {
            var baseName = pharmacyName.ToLower().Replace(" ", "-").Replace("&", "and");
            var random = new Random().Next(1000, 9999);
            return $"{baseName}-{random}";
        }

        private string GenerateBranchCode(string pharmacyName)
        {
            var initials = string.Join("", pharmacyName.Split(' ').Select(s => s.FirstOrDefault())).ToUpper();
            var random = new Random().Next(100, 999);
            return $"{initials}{random}";
        }

        private string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }
    }

    public class SimpleRegistrationRequest
    {
        public string PharmacyName { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }

    public class SimpleRegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public SimpleUserDto User { get; set; }
        public SimpleTenantDto Tenant { get; set; }
        public bool RequiresOnboarding { get; set; }
    }

    public class SimpleUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
    }

    public class SimpleTenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }
    }
}
