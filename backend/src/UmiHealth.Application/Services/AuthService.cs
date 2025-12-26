using Microsoft.Extensions.Logging;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace UmiHealth.Application.Services;

public class AuthService : IAuthService
{
    private readonly ITenantRepository<User> _userRepository;
    private readonly ITenantRepository<Role> _roleRepository;
    private readonly ITenantRepository<UserRole> _userRoleRepository;
    private readonly ITenantRepository<RefreshToken> _refreshTokenRepository;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ITenantRepository<User> userRepository,
        ITenantRepository<Role> roleRepository,
        ITenantRepository<UserRole> userRoleRepository,
        ITenantRepository<RefreshToken> refreshTokenRepository,
        ITenantService tenantService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // For pharmacy registration, create tenant if it doesn't exist
        Guid tenantId = request.TenantId;
        Guid? branchId = request.BranchId;
        
        if (request.TenantId == Guid.Empty)
        {
            // Create new tenant for pharmacy registration
            var tenantRequest = new CreateTenantRequest(
                request.PharmacyName,
                $"{request.PharmacyName} - Pharmacy Management System",
                request.PharmacyName.ToLower().Replace(" ", "-"),
                request.Email,
                request.PhoneNumber,
                request.Address,
                "", // City will be extracted from address
                "Zambia", // Default country
                "", // Postal code
                "Trial" // Default subscription plan
            );
            
            var newTenant = await _tenantService.CreateTenantAsync(tenantRequest, cancellationToken);
            tenantId = newTenant.Id;
            
            // Create main branch for the tenant
            var branchRequest = new CreateBranchRequest(
                $"{request.PharmacyName} - Main Branch",
                "MAIN",
                request.Address,
                request.Province,
                "Zambia",
                "",
                request.PhoneNumber,
                request.Email,
                true, // Is main branch
                $"{request.FirstName} {request.LastName}",
                request.PhoneNumber
            );
            
            var newBranch = await _tenantService.CreateBranchAsync(tenantId, branchRequest, cancellationToken);
            branchId = newBranch.Id;
        }
        else
        {
            // Check if tenant exists and is active
            if (!await _tenantService.IsSubscriptionActiveAsync(tenantId, cancellationToken))
            {
                return new AuthResponse(false, "Tenant subscription is not active", null, null, null, null);
            }
        }

        // Check if user already exists
        var existingUser = (await _userRepository.FindAsync(
            u => u.TenantId == tenantId && u.Email == request.Email, 
            cancellationToken)).FirstOrDefault();

        if (existingUser != null)
        {
            return new AuthResponse(false, "User with this email already exists", null, null, null, null);
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            UserName = request.Username,
            PasswordHash = HashPassword(request.Password),
            IsActive = true,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // Assign default role (Admin for new pharmacy registrations)
        var defaultRole = (await _roleRepository.FindAsync(
            r => r.TenantId == tenantId && r.Name == "Administrator", 
            cancellationToken)).FirstOrDefault();

        if (defaultRole == null)
        {
            // Create Administrator role if it doesn't exist
            defaultRole = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Administrator",
                Description = "Pharmacy Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _roleRepository.AddAsync(defaultRole, cancellationToken);
        }

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            RoleId = defaultRole.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRoleRepository.AddAsync(userRole, cancellationToken);

        var userDto = await CreateUserDtoAsync(user, cancellationToken);
        return new AuthResponse(true, "Registration successful", userDto, null, null, null);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        User? user = null;

        if (!string.IsNullOrEmpty(request.TenantSubdomain))
        {
            // Find tenant by subdomain first
            var tenant = await _tenantService.GetTenantBySubdomainAsync(request.TenantSubdomain, cancellationToken);
            if (tenant == null || !await _tenantService.IsSubscriptionActiveAsync(tenant.Id, cancellationToken))
            {
                return new AuthResponse(false, "Invalid tenant or subscription expired", null, null, null, null);
            }

            user = (await _userRepository.FindAsync(
                u => u.TenantId == tenant.Id && u.Email == request.Email && u.IsActive, 
                cancellationToken)).FirstOrDefault();
        }
        else
        {
            // Try to find user by email across all tenants (for admin users)
            user = (await _userRepository.FindAsync(
                u => u.Email == request.Email && u.IsActive, 
                cancellationToken)).FirstOrDefault();
        }

        if (user == null)
        {
            return new AuthResponse(false, "Invalid credentials", null, null, null, null);
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
            }
            await _userRepository.UpdateAsync(user, cancellationToken);
            return new AuthResponse(false, "Invalid credentials", null, null, null, null);
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return new AuthResponse(false, "Account is locked", null, null, null, null);
        }

        // Reset failed attempts and update last login
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = "127.0.0.1"; // In real app, get from HttpContext
        await _userRepository.UpdateAsync(user, cancellationToken);

        var userDto = await CreateUserDtoAsync(user, cancellationToken);
        var token = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user, cancellationToken);

        return new AuthResponse(true, "Login successful", userDto, token, refreshToken, DateTime.UtcNow.AddHours(1));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var storedToken = (await _refreshTokenRepository.FindAsync(
            rt => rt.Token == request.RefreshToken && !rt.IsUsed && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow, 
            cancellationToken)).FirstOrDefault();

        if (storedToken == null)
        {
            return new AuthResponse(false, "Invalid refresh token", null, null, null, null);
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return new AuthResponse(false, "Invalid user", null, null, null, null);
        }

        // Mark old token as used
        storedToken.IsUsed = true;
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        var userDto = await CreateUserDtoAsync(user, cancellationToken);
        var token = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user, cancellationToken);

        return new AuthResponse(true, "Token refreshed", userDto, token, refreshToken, DateTime.UtcNow.AddHours(1));
    }

    public async Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return false;
        }

        var refreshTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == userGuid && !rt.IsUsed, 
            cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        }

        return true;
    }

    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        var tenant = await _tenantService.GetTenantByIdAsync(user.TenantId, cancellationToken);
        var branch = user.BranchId.HasValue ? null : null; // TODO: Get branch

        return new UserProfile(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.TenantId,
            tenant?.Name ?? "",
            user.BranchId,
            branch?.Name,
            user.CreatedAt,
            user.LastLoginAt ?? DateTime.MinValue,
            user.IsActive
        );
    }

    public async Task<UserProfile> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            // TODO: Implement email change with verification
            user.Email = request.Email;
            user.EmailConfirmed = false;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        var profile = await GetProfileAsync(userId, cancellationToken);
        return profile!;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement forgot password with email verification
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement password reset with token validation
        await Task.CompletedTask;
        return true;
    }

    private async Task<UserDto> CreateUserDtoAsync(User user, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetTenantByIdAsync(user.TenantId, cancellationToken);
        var userRoles = await _userRoleRepository.FindAsync(
            ur => ur.UserId == user.Id, 
            cancellationToken);

        var roles = new List<string>();
        foreach (var userRole in userRoles)
        {
            var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
            if (role != null)
            {
                roles.Add(role.Name);
            }
        }

        return new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.TenantId,
            tenant?.Name ?? "",
            user.BranchId,
            null, // TODO: Get branch name
            roles
        );
    }

    private string HashPassword(string password)
    {
        // TODO: Implement proper password hashing (e.g., BCrypt)
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }

    private string GenerateJwtToken(User user)
    {
        // TODO: Implement proper JWT token generation
        return $"jwt_token_for_{user.Id}_{DateTime.UtcNow:O}";
    }

    private async Task<string> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            JwtTokenId = Guid.NewGuid().ToString(),
            IsUsed = false,
            IsRevoked = false,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        return refreshToken.Token;
    }
}
