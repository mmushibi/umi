using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace UmiHealth.Application.Services
{
    public interface IAuthenticationService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(string userId);
        Task<User> GetUserByIdAsync(string userId);
        Task<User> GetUserByUsernameOrEmailOrPhone(string identifier);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly SharedDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly ISubscriptionService _subscriptionService;

        public AuthenticationService(
            SharedDbContext context,
            IConfiguration configuration,
            ITenantService tenantService,
            ISubscriptionService subscriptionService)
        {
            _context = context;
            _configuration = configuration;
            _tenantService = tenantService;
            _subscriptionService = subscriptionService;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Find user by username, email, or phone
            var user = await GetUserByUsernameOrEmailOrPhone(request.Identifier);
            
            if (user == null)
            {
                return new AuthResponse { Success = false, Message = "Invalid credentials" };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse { Success = false, Message = "Invalid credentials" };
            }

            if (!user.IsActive)
            {
                return new AuthResponse { Success = false, Message = "Account is deactivated" };
            }

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Get tenant information
            var tenant = await _tenantService.GetByIdAsync(user.TenantId);
            if (tenant == null)
            {
                return new AuthResponse { Success = false, Message = "Tenant not found" };
            }

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user);

            // Get subscription info
            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(user.TenantId);

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = MapToUserDto(user),
                Tenant = MapToTenantDto(tenant),
                Subscription = subscription != null ? MapToSubscriptionDto(subscription) : null,
                ExpiresIn = 3600,
                RequiresSetup = await RequiresTenantSetup(user.TenantId)
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if tenant already exists
            var existingTenant = await _tenantService.GetBySubdomainAsync(request.PharmacyName.ToLower().Replace(" ", "-"));
            if (existingTenant != null)
            {
                return new AuthResponse { Success = false, Message = "Pharmacy name already exists" };
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .AnyAsync(u => u.Email == request.Email || u.Phone == request.PhoneNumber);

            if (existingUser)
            {
                return new AuthResponse { Success = false, Message = "Email or phone number already registered" };
            }

            // Create tenant
            var tenant = new Tenant
            {
                Name = request.PharmacyName,
                Subdomain = request.PharmacyName.ToLower().Replace(" ", "-"),
                DatabaseName = $"umi_{request.PharmacyName.ToLower().Replace(" ", "-")}",
                Status = "active",
                SubscriptionPlan = "trial",
                MaxBranches = 1,
                MaxUsers = 10,
                Settings = new Dictionary<string, object>(),
                BillingInfo = new Dictionary<string, object>(),
                ComplianceSettings = new Dictionary<string, object>()
            };

            tenant = await _tenantService.CreateAsync(tenant);

            // Create main branch
            var branch = new Branch
            {
                Name = $"{request.PharmacyName} - Main Branch",
                Code = "MAIN",
                Address = request.Address,
                Phone = request.PhoneNumber,
                Email = request.Email,
                LicenseNumber = request.PharmacyLicenseNumber,
                OperatingHours = new Dictionary<string, object>(),
                Settings = new Dictionary<string, object>()
            };

            branch = await _tenantService.CreateBranchAsync(tenant.Id, branch);

            // Create user
            var user = new User
            {
                TenantId = tenant.Id,
                BranchId = branch.Id,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                FirstName = request.AdminFullName.Split(' ').FirstOrDefault() ?? "",
                LastName = request.AdminFullName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "admin",
                BranchAccess = new List<Guid> { branch.Id },
                Permissions = new Dictionary<string, object>(),
                IsActive = true,
                EmailVerified = false,
                PhoneVerified = false,
                TwoFactorEnabled = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create 14-day trial subscription
            var trialSubscription = await _subscriptionService.CreateTrialSubscriptionAsync(tenant.Id);

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user);

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = MapToUserDto(user),
                Tenant = MapToTenantDto(tenant),
                Subscription = MapToSubscriptionDto(trialSubscription),
                ExpiresIn = 3600,
                RequiresSetup = true // New tenant always requires setup
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:RefreshSecret"]!);

                var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new AuthResponse { Success = false, Message = "Invalid refresh token" };
                }

                var user = await _context.Users.FindAsync(Guid.Parse(userId));
                if (user == null || !user.IsActive)
                {
                    return new AuthResponse { Success = false, Message = "User not found" };
                }

                var newAccessToken = GenerateAccessToken(user);
                var newRefreshToken = GenerateRefreshToken(user);

                return new AuthResponse
                {
                    Success = true,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = 3600
                };
            }
            catch
            {
                return new AuthResponse { Success = false, Message = "Invalid refresh token" };
            }
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            // In a real implementation, you would invalidate the token
            // For now, we'll just return true
            return true;
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.Tenant)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);
        }

        public async Task<User> GetUserByUsernameOrEmailOrPhone(string identifier)
        {
            return await _context.Users
                .Include(u => u.Tenant)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => 
                    u.Email == identifier || 
                    u.PhoneNumber == identifier || 
                    u.Username == identifier);
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("branch_id", user.BranchId?.ToString() ?? ""),
                new Claim("username", user.Username ?? user.Email)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:RefreshSecret"]!);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("tenant_id", user.TenantId.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<bool> RequiresTenantSetup(Guid tenantId)
        {
            // Check if tenant has completed initial setup
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            return tenant?.Settings?.ContainsKey("setup_completed") != true;
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Username = user.Username ?? user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                TenantId = user.TenantId,
                BranchId = user.BranchId,
                BranchAccess = user.BranchAccess,
                Permissions = user.Permissions,
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                PhoneVerified = user.PhoneVerified,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LastLogin = user.LastLogin
            };
        }

        private TenantDto MapToTenantDto(Tenant tenant)
        {
            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                DatabaseName = tenant.DatabaseName,
                Status = tenant.Status,
                SubscriptionPlan = tenant.SubscriptionPlan,
                MaxBranches = tenant.MaxBranches,
                MaxUsers = tenant.MaxUsers,
                Settings = tenant.Settings,
                CreatedAt = tenant.CreatedAt
            };
        }

        private SubscriptionDto MapToSubscriptionDto(Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                PlanType = subscription.PlanType,
                Status = subscription.Status,
                BillingCycle = subscription.BillingCycle,
                Amount = subscription.Amount,
                Currency = subscription.Currency,
                Features = subscription.Features,
                Limits = subscription.Limits,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                AutoRenew = subscription.AutoRenew,
                DaysRemaining = subscription.EndDate.HasValue ? 
                    (int)(subscription.EndDate.Value - DateTime.UtcNow).TotalDays : 0,
                IsTrial = subscription.PlanType == "trial",
                RequiresUpgrade = subscription.EndDate.HasValue && 
                               subscription.EndDate.Value <= DateTime.UtcNow.AddDays(7) &&
                               subscription.PlanType == "trial"
            };
        }
    }

    // DTOs
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public TenantDto Tenant { get; set; } = null!;
        public SubscriptionDto Subscription { get; set; } = null!;
        public int ExpiresIn { get; set; }
        public bool RequiresSetup { get; set; }
    }

    public class LoginRequest
    {
        public string Identifier { get; set; } = string.Empty; // Username, email, or phone
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string PharmacyName { get; set; } = string.Empty;
        public string PharmacyLicenseNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public List<Guid> BranchAccess { get; set; } = new();
        public Dictionary<string, object> Permissions { get; set; } = new();
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public bool PhoneVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public int MaxBranches { get; set; }
        public int MaxUsers { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Dictionary<string, object> Features { get; set; } = new();
        public Dictionary<string, object> Limits { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool AutoRenew { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsTrial { get; set; }
        public bool RequiresUpgrade { get; set; }
    }
}
