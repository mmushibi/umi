using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UmiHealth.Identity;
using UmiHealth.Identity.Services;
using UmiHealth.Shared.DTOs;
namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly UmiHealth.Identity.Services.IJwtService _jwtService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthenticationService authenticationService,
            UmiHealth.Identity.Services.IJwtService jwtService,
            ITokenBlacklistService tokenBlacklistService,
            ILogger<AuthController> logger)
        {
            _authenticationService = authenticationService;
            _jwtService = jwtService;
            _tokenBlacklistService = tokenBlacklistService;
            _logger = logger;
        }

        /// <summary>
        /// Login user with email and password
        /// Returns access token and refresh token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
                }

                var result = await _authenticationService.LoginAsync(request.Email, request.Password, cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for email: {Email}", request.Email);
                    return Unauthorized(new { success = false, message = result.Error });
                }

                _logger.LogInformation("User logged in successfully: {Email}", request.Email);

                // Determine redirect URL based on user role
                var redirectUrl = GetRoleBasedRedirectUrl(result.Roles?.FirstOrDefault()?.Name);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = new
                    {
                        accessToken = result.AccessToken,
                        refreshToken = result.RefreshToken,
                        redirectUrl = redirectUrl,
                        user = new
                        {
                            id = result.User?.Id,
                            email = result.User?.Email,
                            firstName = result.User?.FirstName,
                            lastName = result.User?.LastName,
                            tenantId = result.User?.TenantId,
                            branchId = result.User?.BranchId,
                            roles = result.Roles
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { success = false, message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
                }

                // Map controller DTO to identity registration DTO
                var regRequest = new UmiHealth.Identity.RegisterRequest
                {
                    TenantId = request.TenantId,
                    BranchId = request.BranchId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber ?? string.Empty,
                    UserName = request.UserName,
                    Password = request.Password,
                    RoleName = request.RoleName
                };

                var regResult = await _authenticationService.RegisterAsync(regRequest, cancellationToken);

                if (!regResult.Success)
                {
                    _logger.LogWarning("Registration failed: {Message}", regResult.Error);
                    return BadRequest(new { success = false, message = regResult.Error });
                }

                _logger.LogInformation("User registered successfully: {Email}", request.Email);

                return Ok(new
                {
                    success = true,
                    message = "Registration successful. Please login.",
                    data = new
                    {
                        userId = regResult.User?.Id,
                        email = regResult.User?.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { success = false, message = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new { success = false, message = "Refresh token is required" });
                }

                // Check if refresh token is blacklisted
                var isBlacklisted = await _tokenBlacklistService.IsTokenBlacklistedAsync(request.RefreshToken, cancellationToken);
                if (isBlacklisted)
                {
                    _logger.LogWarning("Attempt to use blacklisted refresh token");
                    return Unauthorized(new { success = false, message = "Refresh token has been revoked" });
                }

                var result = await _authenticationService.RefreshTokenAsync(request.AccessToken, request.RefreshToken, cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning("Token refresh failed");
                    return Unauthorized(new { success = false, message = result.Error });
                }

                _logger.LogInformation("Token refreshed successfully");

                return Ok(new
                {
                    success = true,
                    message = "Token refreshed successfully",
                    data = new
                    {
                        accessToken = result.AccessToken,
                        refreshToken = result.RefreshToken
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { success = false, message = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Logout user - invalidate refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new { success = false, message = "Refresh token is required" });
                }

                // Revoke refresh token
                var result = await _authenticationService.LogoutAsync(request.RefreshToken, cancellationToken);

                if (!result)
                {
                    _logger.LogWarning("Logout failed");
                    return StatusCode(500, new { success = false, message = "Failed to logout" });
                }

                // Add access token to blacklist as well
                var accessToken = ExtractTokenFromHeader();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await _tokenBlacklistService.BlacklistTokenAsync(accessToken, "User logout", cancellationToken);
                }

                _logger.LogInformation("User logged out successfully");

                return Ok(new { success = true, message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { success = false, message = "An error occurred during logout" });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _authenticationService.GetProfileAsync(userId, cancellationToken);

                if (!result.Success)
                {
                    return NotFound(new { success = false, message = result.Message });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = result.User?.Id,
                        email = result.User?.Email,
                        firstName = result.User?.FirstName,
                        lastName = result.User?.LastName,
                        tenantId = result.User?.TenantId,
                        branchId = result.User?.BranchId,
                        roles = result.Roles?.Select(r => r.Name)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user profile");
                return StatusCode(500, new { success = false, message = "An error occurred fetching user profile" });
            }
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email is required" });
                }

                var result = await _authenticationService.ForgotPasswordAsync(request.Email, cancellationToken);

                // Always return success to prevent email enumeration
                _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

                return Ok(new
                {
                    success = true,
                    message = "If the email exists in our system, you will receive a password reset link"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { success = false, message = "Current and new password are required" });
                }

                var userId = GetUserIdFromClaims();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _authenticationService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);

                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to change password. Current password may be incorrect." });
                }

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

                return Ok(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Force logout user by blacklisting all tokens (admin only)
        /// </summary>
        [HttpPost("force-logout/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ForceLogout(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all active refresh tokens for the user and revoke them
                await _tokenBlacklistService.BlacklistUserTokensAsync(userId, "Admin force logout", cancellationToken);

                _logger.LogInformation("User {UserId} force logged out by admin", userId);

                return Ok(new { success = true, message = "User has been logged out" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during force logout for user: {UserId}", userId);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Validate token (check if still valid and not blacklisted)
        /// </summary>
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { success = false, message = "Token is required" });
                }

                // Check if token is blacklisted
                var isBlacklisted = await _tokenBlacklistService.IsTokenBlacklistedAsync(request.Token, cancellationToken);
                if (isBlacklisted)
                {
                    return Ok(new { success = false, message = "Token has been revoked", isValid = false });
                }

                    // Validate token signature and expiry
                    bool isValid;
                    try
                    {
                        var principal = _jwtService.GetPrincipalFromToken(request.Token);
                        isValid = principal != null;
                    }
                    catch
                    {
                        isValid = false;
                    }

                return Ok(new
                {
                    success = isValid,
                    message = isValid ? "Token is valid" : "Token is invalid",
                    isValid = isValid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, new { success = false, message = "An error occurred validating token", isValid = false });
            }
        }

        // Helper methods
        private string? ExtractTokenFromHeader()
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        private Guid GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }

        private string GetRoleBasedRedirectUrl(string? role)
        {
            return role?.ToLower() switch
            {
                "admin" => "/portals/admin/home.html",
                "pharmacist" => "/portals/pharmacist/home.html",
                "cashier" => "/portals/cashier/home.html",
                "operations" => "/portals/operations/home.html",
                _ => "/portals/admin/home.html" // Default fallback
            };
        }
    }

    // Request DTOs
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? TenantSubdomain { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public string? RoleName { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
