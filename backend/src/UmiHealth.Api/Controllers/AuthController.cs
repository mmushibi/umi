using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ISubscriptionService _subscriptionService;

        public AuthController(IAuthenticationService authService, ISubscriptionService subscriptionService)
        {
            _authService = authService;
            _subscriptionService = subscriptionService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Login failed. Please try again." });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                
                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message });
                }

                return Created("", result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Registration failed. Please try again." });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                
                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Token refresh failed." });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await _authService.LogoutAsync(userId);
                }

                return Ok(new { success = true, message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Logout failed." });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new UserDto
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
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get user information." });
            }
        }

        [HttpGet("subscription-status")]
        [Authorize]
        public async Task<ActionResult<SubscriptionReminder>> GetSubscriptionStatus()
        {
            try
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    return BadRequest("Tenant information not found");
                }

                var tenantId = Guid.Parse(tenantIdClaim);
                var reminder = await _subscriptionService.GetSubscriptionReminderAsync(tenantId);

                if (reminder == null)
                {
                    return Ok(new { 
                        showUpgradeBanner = false, 
                        message = "No active subscription found" 
                    });
                }

                return Ok(reminder);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get subscription status." });
            }
        }

        [HttpGet("check-setup")]
        [Authorize]
        public async Task<ActionResult> CheckTenantSetup()
        {
            try
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    return BadRequest("Tenant information not found");
                }

                var tenantId = Guid.Parse(tenantIdClaim);
                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(tenantId);

                var response = new
                {
                    requiresSetup = subscription?.PlanType == "trial",
                    setupCompleted = false, // This would be checked from tenant settings
                    redirectUrl = subscription?.PlanType == "trial" ? "/account/setup" : "/home"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to check setup status." });
            }
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
