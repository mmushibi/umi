using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UmiHealth.Identity;
using UmiHealth.Identity.Services;
using UmiHealth.Shared.DTOs;
using UmiHealth.Application.Services;
using UmiHealth.Persistence.Data;
namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly UmiHealth.Identity.Services.IJwtService _jwtService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly ISimpleRegistrationService _simpleRegistrationService;
        private readonly IOnboardingService _onboardingService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly SharedDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly ISecurityAuditService _securityAuditService;

        public AuthController(
            IAuthenticationService authenticationService,
            UmiHealth.Identity.Services.IJwtService jwtService,
            ITokenBlacklistService tokenBlacklistService,
            ISimpleRegistrationService simpleRegistrationService,
            IOnboardingService onboardingService,
            ISubscriptionService subscriptionService,
            SharedDbContext context,
            ILogger<AuthController> logger,
            ISecurityAuditService securityAuditService)
        {
            _authenticationService = authenticationService;
            _jwtService = jwtService;
            _tokenBlacklistService = tokenBlacklistService;
            _simpleRegistrationService = simpleRegistrationService;
            _onboardingService = onboardingService;
            _subscriptionService = subscriptionService;
            _context = context;
            _logger = logger;
            _securityAuditService = securityAuditService;
        }

        /// <summary>
        /// Login user with email or phone number and password
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

                // Determine if identifier is email or phone number
                bool isEmail = request.Identifier.Contains("@");
                string identifier = request.Identifier.Trim();

                var result = await _authenticationService.LoginAsync(identifier, request.Password, cancellationToken);

                if (!result.Success)
                {
                    // Log failed login attempt
                    await _securityAuditService.LogSecurityEventAsync(new SecurityEvent
                    {
                        EventType = SecurityEventType.LoginFailure,
                        Description = $"Login failed for identifier: {identifier}",
                        IpAddress = GetClientIpAddress(),
                        UserId = null,
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        RequestPath = "/api/v1/auth/login",
                        RiskLevel = SecurityRiskLevel.Medium,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Identifier"] = identifier,
                            ["Reason"] = result.Error
                        }
                    });

                    _logger.LogWarning("Login failed for identifier: {Identifier}", identifier);
                    return Unauthorized(new { success = false, message = result.Error });
                }

                // Log successful login
                await _securityAuditService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.LoginSuccess,
                    Description = $"User logged in successfully: {identifier}",
                    IpAddress = GetClientIpAddress(),
                    UserId = result.User?.Id.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    RequestPath = "/api/v1/auth/login",
                    RiskLevel = SecurityRiskLevel.Low,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Identifier"] = identifier,
                        ["Roles"] = result.Roles?.Select(r => r.Name).ToList()
                    }
                });

                _logger.LogInformation("User logged in successfully: {Identifier}", identifier);

                // Determine redirect URL based on user role
                var redirectUrl = GetRoleBasedRedirectUrl(result.Roles?.FirstOrDefault()?.Name);

                // Check subscription status and modify redirect if needed
                var subscriptionStatus = await CheckSubscriptionStatusForLogin(result.User?.TenantId);
                if (!subscriptionStatus.HasAccess)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Login successful",
                        data = new
                        {
                            accessToken = result.AccessToken,
                            refreshToken = result.RefreshToken,
                            redirectUrl = subscriptionStatus.TenantSuspended ? "/public/account-suspended.html" : "/portals/admin/subscription.html",
                            user = new
                            {
                                id = result.User?.Id,
                                email = result.User?.Email,
                                phoneNumber = result.User?.PhoneNumber,
                                firstName = result.User?.FirstName,
                                lastName = result.User?.LastName,
                                tenantId = result.User?.TenantId,
                                branchId = result.User?.BranchId,
                                roles = result.Roles
                            },
                            subscriptionStatus = new
                            {
                                hasAccess = subscriptionStatus.HasAccess,
                                isTrial = subscriptionStatus.IsTrial,
                                planType = subscriptionStatus.PlanType,
                                trialEndDate = subscriptionStatus.TrialEndDate,
                                subscriptionEndDate = subscriptionStatus.SubscriptionEndDate,
                                reason = subscriptionStatus.Reason,
                                isSuspended = subscriptionStatus.TenantSuspended
                            }
                        }
                    });
                }

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
                            phoneNumber = result.User?.PhoneNumber,
                            firstName = result.User?.FirstName,
                            lastName = result.User?.LastName,
                            tenantId = result.User?.TenantId,
                            branchId = result.User?.BranchId,
                            roles = result.Roles
                        },
                        subscriptionStatus = new
                        {
                            hasAccess = subscriptionStatus.HasAccess,
                            isTrial = subscriptionStatus.IsTrial,
                            planType = subscriptionStatus.PlanType,
                            trialEndDate = subscriptionStatus.TrialEndDate,
                            subscriptionEndDate = subscriptionStatus.SubscriptionEndDate,
                            reason = subscriptionStatus.Reason,
                            isSuspended = subscriptionStatus.TenantSuspended
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
        /// Simple registration endpoint for pharmacy signup
        /// </summary>
        [HttpPost("simple-register")]
        [AllowAnonymous]
        public async Task<IActionResult> SimpleRegister([FromBody] SimpleRegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
                }

                var registrationRequest = new SimpleRegistrationRequest
                {
                    PharmacyName = request.PharmacyName,
                    PhoneNumber = request.PhoneNumber,
                    Password = request.Password
                };

                var result = await _simpleRegistrationService.RegisterPharmacyAsync(registrationRequest, cancellationToken);

                if (result.Success)
                {
                    return Ok(new { 
                        success = true, 
                        message = result.Message,
                        data = new {
                            accessToken = result.AccessToken,
                            refreshToken = result.RefreshToken,
                            user = result.User,
                            tenant = result.Tenant,
                            requiresOnboarding = result.RequiresOnboarding,
                            redirectUrl = result.RequiresOnboarding ? "/public/onboarding.html" : "/portals/admin/home.html"
                        }
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during simple registration");
                return StatusCode(500, new { success = false, message = "Registration failed due to an internal error" });
            }
        }

        public class SimpleRegisterRequest
        {
            public string PharmacyName { get; set; }
            public string PhoneNumber { get; set; }
            public string Password { get; set; }
        }

        /// <summary>
        /// Complete onboarding for newly registered pharmacy
        /// </summary>
        [HttpPost("complete-onboarding")]
        [Authorize]
        public async Task<IActionResult> CompleteOnboarding([FromBody] OnboardingRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
                }

                // Get current user from JWT token
                var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Invalid user token" });
                }

                var user = await _context.Users
                    .Include(u => u.Tenant)
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Update branch information
                if (user.Branch != null)
                {
                    user.Branch.Phone = request.PhoneNumber ?? user.Branch.Phone;
                    user.Branch.Email = request.ContactEmail ?? user.Branch.Email;
                    user.Branch.LicenseNumber = request.LicenseNumber ?? user.Branch.LicenseNumber;
                    if (!string.IsNullOrEmpty(request.OperatingHours))
                    {
                        try
                        {
                            user.Branch.OperatingHours = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(request.OperatingHours) ?? user.Branch.OperatingHours;
                        }
                        catch
                        {
                            // Keep existing OperatingHours if deserialization fails
                        }
                    }
                    user.Branch.UpdatedAt = DateTime.UtcNow;
                }

                // Update tenant information
                if (user.Tenant != null)
                {
                    var settings = new
                    {
                        pharmacyType = request.PharmacyType,
                        zamraNumber = request.ZamraNumber,
                        physicalAddress = request.PhysicalAddress,
                        province = request.Province,
                        city = request.City,
                        postalCode = request.PostalCode,
                        website = request.Website,
                        pharmacistCount = request.PharmacistCount,
                        services = request.Services,
                        emergencyContact = request.EmergencyContact,
                        onboardingCompleted = true,
                        onboardingCompletedAt = DateTime.UtcNow
                    };

                    user.Tenant.Settings = System.Text.Json.JsonSerializer.Serialize(settings);
                    user.Tenant.UpdatedAt = DateTime.UtcNow;
                }

                // Update user information
                user.FirstName = request.AdminFullName?.Split(' ').FirstOrDefault() ?? user.FirstName;
                user.LastName = request.AdminFullName?.Split(' ').LastOrDefault() ?? user.LastName;
                user.Email = request.AdminEmail ?? user.Email;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User {UserId} completed onboarding for tenant {TenantId}", userId, user.TenantId);

                return Ok(new { 
                    success = true, 
                    message = "Onboarding completed successfully",
                    data = new {
                        redirectUrl = "/portals/admin/home.html"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during onboarding completion");
                return StatusCode(500, new { success = false, message = "Failed to complete onboarding" });
            }
        }

        public class OnboardingRequest
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
            public ServicesDto Services { get; set; }
            public string AdminFullName { get; set; }
            public string AdminEmail { get; set; }
            public string AdminTitle { get; set; }
            public int AdminExperience { get; set; }
        }

        public class ServicesDto
        {
            public bool PrescriptionFilling { get; set; }
            public bool Compounding { get; set; }
            public bool HealthScreening { get; set; }
            public bool Vaccination { get; set; }
            public bool MedicinalTherapy { get; set; }
            public bool HealthConsultation { get; set; }
        }

        /// <summary>
        /// Test endpoint to verify API is working
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new { success = true, message = "API is working!", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Get tenant onboarding data (for operations and super admin)
        /// </summary>
        [HttpGet("onboarding/{tenantId}")]
        [Authorize(Roles = "admin,superadmin")]
        public async Task<IActionResult> GetTenantOnboarding(Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await _onboardingService.GetTenantOnboardingAsync(tenantId, cancellationToken);
                if (data == null)
                {
                    return NotFound(new { success = false, message = "Tenant not found" });
                }

                return Ok(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving onboarding data for tenant {TenantId}", tenantId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all tenants onboarding data (for super admin)
        /// </summary>
        [HttpGet("onboarding")]
        [Authorize(Roles = "superadmin")]
        public async Task<IActionResult> GetAllTenantsOnboarding(CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await _onboardingService.GetAllTenantsOnboardingAsync(cancellationToken);
                return Ok(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tenants onboarding data");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update tenant onboarding data (for operations and super admin)
        /// </summary>
        [HttpPut("onboarding/{tenantId}")]
        [Authorize(Roles = "admin,superadmin")]
        public async Task<IActionResult> UpdateTenantOnboarding(Guid tenantId, [FromBody] UpdateOnboardingRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
                }

                var success = await _onboardingService.UpdateTenantOnboardingAsync(tenantId, request, cancellationToken);
                if (!success)
                {
                    return NotFound(new { success = false, message = "Tenant not found" });
                }

                return Ok(new { success = true, message = "Onboarding data updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating onboarding data for tenant {TenantId}", tenantId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
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
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            return null;
        }

        private string GetClientIpAddress()
        {
            var xForwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private Guid GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
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

        private async Task<SubscriptionLoginStatus> CheckSubscriptionStatusForLogin(Guid? tenantId)
        {
            try
            {
                if (!tenantId.HasValue)
                {
                    return new SubscriptionLoginStatus
                    {
                        HasAccess = false,
                        Reason = "No tenant found"
                    };
                }

                // Get tenant info
                var tenant = await _context.Tenants.FindAsync(tenantId.Value);
                if (tenant == null)
                {
                    return new SubscriptionLoginStatus
                    {
                        HasAccess = false,
                        Reason = "Tenant not found"
                    };
                }

                // Check if tenant is suspended
                if (tenant.IsSuspended)
                {
                    return new SubscriptionLoginStatus
                    {
                        HasAccess = false,
                        IsTrial = false,
                        PlanType = "Suspended",
                        TrialExpired = false,
                        TenantSuspended = true,
                        Reason = "Tenant account is suspended. Please contact support."
                    };
                }

                // Check if tenant has active paid subscription
                var subscription = await _subscriptionService.GetTenantSubscriptionAsync(tenantId.Value);
                if (subscription != null && subscription.EndDate > DateTime.UtcNow)
                {
                    return new SubscriptionLoginStatus
                    {
                        HasAccess = true,
                        IsTrial = false,
                        PlanType = subscription.PlanType,
                        SubscriptionEndDate = subscription.EndDate,
                        TenantSuspended = false,
                        Reason = "Active subscription"
                    };
                }

                // Check if tenant is in 14-day trial period
                var trialEndDate = tenant.CreatedAt.AddDays(14);
                var isInTrial = DateTime.UtcNow <= trialEndDate;

                if (isInTrial)
                {
                    return new SubscriptionLoginStatus
                    {
                        HasAccess = true,
                        IsTrial = true,
                        PlanType = "Trial",
                        TrialEndDate = trialEndDate,
                        TenantSuspended = false,
                        Reason = "Trial period active"
                    };
                }

                // No active subscription and trial expired
                return new SubscriptionLoginStatus
                {
                    HasAccess = false,
                    IsTrial = false,
                    TrialExpired = true,
                    TenantSuspended = false,
                    Reason = "Trial period expired and no active subscription"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription status during login");
                return new SubscriptionLoginStatus
                {
                    HasAccess = false,
                    Reason = "Error checking subscription status"
                };
            }
        }
    }

    // Request DTOs
    public class LoginRequest
    {
        public string Identifier { get; set; } = string.Empty; // Can be email or phone number
        public string Password { get; set; } = string.Empty;
        public string? TenantSubdomain { get; set; }
    }

    public class SubscriptionLoginStatus
    {
        public bool HasAccess { get; set; }
        public bool IsTrial { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public DateTime? TrialEndDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool TrialExpired { get; set; }
        public bool TenantSuspended { get; set; }
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
