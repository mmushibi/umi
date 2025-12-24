using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UmiHealth.Core.Entities;
using UmiHealth.Infrastructure;

namespace UmiHealth.Identity
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<AuthenticationResult> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
        Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly UmiHealthDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly JwtSettings _jwtSettings;

        public AuthenticationService(
            UmiHealthDbContext context,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            ILogger<AuthenticationService> logger,
            JwtSettings jwtSettings)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _jwtSettings = jwtSettings;
        }

        public async Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User not found for email {Email}", email);
                    return AuthenticationResult.Failed("Invalid email or password");
                }

                // Check if account is locked
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    _logger.LogWarning("Login attempt failed: Account locked for user {UserId}", user.Id);
                    return AuthenticationResult.Failed("Account is temporarily locked");
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                        _logger.LogWarning("Account locked due to failed attempts: {UserId}", user.Id);
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                    return AuthenticationResult.Failed("Invalid email or password");
                }

                // Reset failed login attempts on successful login
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.LastLoginAt = DateTime.UtcNow;

                // Get user roles
                var roles = user.UserRoles.Select(ur => ur.Role).ToList();

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(user, roles, user.BranchId);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    Token = refreshToken,
                    JwtTokenId = GenerateJwtTokenId(accessToken),
                    IsUsed = false,
                    IsRevoked = false,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiration)
                };

                await _jwtService.SaveRefreshTokenAsync(refreshTokenEntity, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

                return AuthenticationResult.Successful(accessToken, refreshToken, user, roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", email);
                return AuthenticationResult.Failed("An error occurred during login");
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get principal from expired access token
                var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
                var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                // Validate refresh token
                if (!await _jwtService.ValidateRefreshTokenAsync(refreshToken, cancellationToken))
                {
                    _logger.LogWarning("Invalid refresh token provided for user {UserId}", userId);
                    return AuthenticationResult.Failed("Invalid refresh token");
                }

                // Get user with roles
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User not found during token refresh: {UserId}", userId);
                    return AuthenticationResult.Failed("User not found");
                }

                // Revoke old refresh token
                await _jwtService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);

                // Generate new tokens
                var roles = user.UserRoles.Select(ur => ur.Role).ToList();
                var newAccessToken = _jwtService.GenerateAccessToken(user, roles, user.BranchId);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Save new refresh token
                var newRefreshTokenEntity = new RefreshToken
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    Token = newRefreshToken,
                    JwtTokenId = GenerateJwtTokenId(newAccessToken),
                    IsUsed = false,
                    IsRevoked = false,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiration)
                };

                await _jwtService.SaveRefreshTokenAsync(newRefreshTokenEntity, cancellationToken);

                _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

                return AuthenticationResult.Successful(newAccessToken, newRefreshToken, user, roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return AuthenticationResult.Failed("An error occurred during token refresh");
            }
        }

        public async Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                await _jwtService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
                _logger.LogInformation("User logged out successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<RegistrationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() || 
                                   u.UserName.ToLower() == request.UserName.ToLower(), cancellationToken);

                if (existingUser)
                {
                    return RegistrationResult.Failed("User with this email or username already exists");
                }

                // Validate tenant exists
                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == request.TenantId && t.IsActive, cancellationToken);

                if (tenant == null)
                {
                    return RegistrationResult.Failed("Invalid tenant");
                }

                // Create user
                var user = new User
                {
                    TenantId = request.TenantId,
                    BranchId = request.BranchId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    UserName = request.UserName,
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    IsActive = true,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    FailedLoginAttempts = 0
                };

                // Assign default role if specified
                if (!string.IsNullOrEmpty(request.RoleName))
                {
                    var role = await _context.Roles
                        .FirstOrDefaultAsync(r => r.TenantId == request.TenantId && 
                                             r.Name.ToLower() == request.RoleName.ToLower(), cancellationToken);

                    if (role != null)
                    {
                        user.UserRoles.Add(new UserRole
                        {
                            TenantId = request.TenantId,
                            RoleId = role.Id
                        });
                    }
                }

                await _context.Users.AddAsync(user, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User registered successfully: {UserId}", user.Id);

                return RegistrationResult.Successful(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return RegistrationResult.Failed("An error occurred during registration");
            }
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

                if (user == null)
                {
                    return false;
                }

                if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
                {
                    return false;
                }

                user.PasswordHash = _passwordHasher.HashPassword(newPassword);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive, cancellationToken);

                if (user == null)
                {
                    // Don't reveal that user doesn't exist
                    return true;
                }

                // Generate password reset token (simplified - in production, use secure token generation)
                var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                var expiry = DateTime.UtcNow.AddHours(1);

                // Store token (you might want a separate table for password reset tokens)
                user.UserClaims.Add(new UserClaim
                {
                    TenantId = user.TenantId,
                    ClaimType = "password_reset_token",
                    ClaimValue = resetToken
                });

                await _context.SaveChangesAsync(cancellationToken);

                // TODO: Send email with reset token
                _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var userClaim = await _context.UserClaims
                    .Include(uc => uc.User)
                    .FirstOrDefaultAsync(uc => uc.ClaimType == "password_reset_token" && 
                                            uc.ClaimValue == token, cancellationToken);

                if (userClaim?.User == null)
                {
                    return false;
                }

                var user = userClaim.User;
                user.PasswordHash = _passwordHasher.HashPassword(newPassword);

                // Remove the reset token
                _context.UserClaims.Remove(userClaim);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Password reset successfully for user: {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return false;
            }
        }

        private string GenerateJwtTokenId(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Id;
        }
    }

    // DTOs
    public class RegisterRequest
    {
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? RoleName { get; set; }
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public User? User { get; set; }
        public IEnumerable<Role>? Roles { get; set; }
        public string? Error { get; set; }

        public static AuthenticationResult Successful(string accessToken, string refreshToken, User user, IEnumerable<Role> roles)
        {
            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = user,
                Roles = roles
            };
        }

        public static AuthenticationResult Failed(string error)
        {
            return new AuthenticationResult
            {
                Success = false,
                Error = error
            };
        }
    }

    public class RegistrationResult
    {
        public bool Success { get; set; }
        public User? User { get; set; }
        public string? Error { get; set; }

        public static RegistrationResult Successful(User user)
        {
            return new RegistrationResult
            {
                Success = true,
                User = user
            };
        }

        public static RegistrationResult Failed(string error)
        {
            return new RegistrationResult
            {
                Success = false,
                Error = error
            };
        }
    }
}
