using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;
using UmiHealth.Core.Interfaces;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [EnableRateLimiting("Auth")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(new(
                    request.Email,
                    request.Password,
                    request.TenantSubdomain
                ));

                if (!result.Success)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult(result.Message));
                }

                var response = new LoginResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    User = result.User,
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    ExpiresAt = result.ExpiresAt
                };

                return Ok(ApiResponse<LoginResponse>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Login failed. Please try again."));
            }
        }

        [HttpPost("register")]
        [EnableRateLimiting("Auth")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(new(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.PhoneNumber,
                    request.TenantId,
                    request.BranchId
                ));

                if (!result.Success)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult(result.Message));
                }

                var response = new LoginResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    User = result.User,
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    ExpiresAt = result.ExpiresAt
                };

                return Created("", ApiResponse<LoginResponse>.SuccessResult(response, "Registration successful"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Registration failed. Please try again."));
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(new(
                    request.Token,
                    request.RefreshToken
                ));

                if (!result.Success)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult(result.Message));
                }

                var response = new LoginResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    User = result.User,
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    ExpiresAt = result.ExpiresAt
                };

                return Ok(ApiResponse<LoginResponse>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Token refresh failed."));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    var result = await _authService.LogoutAsync(userIdClaim);
                    return Ok(ApiResponse<bool>.SuccessResult(result, "Logged out successfully"));
                }

                return BadRequest(ApiResponse<bool>.ErrorResult("User not found"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Logout failed."));
            }
        }

        [HttpGet("me")]
        [Authorize]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var userProfile = await _authService.GetProfileAsync(userId);
                if (userProfile == null)
                {
                    return NotFound(ApiResponse<UserProfileDto>.ErrorResult("User not found"));
                }

                var profileDto = new UserProfileDto
                {
                    Id = userProfile.Id,
                    Email = userProfile.Email,
                    FirstName = userProfile.FirstName,
                    LastName = userProfile.LastName,
                    PhoneNumber = userProfile.PhoneNumber,
                    TenantId = userProfile.TenantId,
                    TenantName = userProfile.TenantName,
                    BranchId = userProfile.BranchId,
                    BranchName = userProfile.BranchName,
                    CreatedAt = userProfile.CreatedAt,
                    LastLoginAt = userProfile.LastLoginAt,
                    IsActive = userProfile.IsActive
                };

                return Ok(ApiResponse<UserProfileDto>.SuccessResult(profileDto));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<UserProfileDto>.ErrorResult("Failed to get user information."));
            }
        }

        [HttpPut("me")]
        [Authorize]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var userProfile = await _authService.UpdateProfileAsync(userId, new(
                    request.FirstName,
                    request.LastName,
                    request.PhoneNumber,
                    request.Email
                ));

                var profileDto = new UserProfileDto
                {
                    Id = userProfile.Id,
                    Email = userProfile.Email,
                    FirstName = userProfile.FirstName,
                    LastName = userProfile.LastName,
                    PhoneNumber = userProfile.PhoneNumber,
                    TenantId = userProfile.TenantId,
                    TenantName = userProfile.TenantName,
                    BranchId = userProfile.BranchId,
                    BranchName = userProfile.BranchName,
                    CreatedAt = userProfile.CreatedAt,
                    LastLoginAt = userProfile.LastLoginAt,
                    IsActive = userProfile.IsActive
                };

                return Ok(ApiResponse<UserProfileDto>.SuccessResult(profileDto, "Profile updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<UserProfileDto>.ErrorResult("Failed to update profile."));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var result = await _authService.ChangePasswordAsync(userId, new(
                    request.CurrentPassword,
                    request.NewPassword,
                    request.ConfirmPassword
                ));

                if (!result)
                {
                    return BadRequest(ApiResponse<bool>.ErrorResult("Current password is incorrect"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true, "Password changed successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Failed to change password."));
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("Auth")]
        public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var result = await _authService.ForgotPasswordAsync(new(
                    request.Email,
                    request.TenantSubdomain
                ));

                return Ok(ApiResponse<bool>.SuccessResult(true, "Password reset email sent"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Failed to send password reset email."));
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("Auth")]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(new(
                    request.Email,
                    request.Token,
                    request.NewPassword,
                    request.ConfirmPassword
                ));

                if (!result)
                {
                    return BadRequest(ApiResponse<bool>.ErrorResult("Invalid or expired reset token"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true, "Password reset successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Failed to reset password."));
            }
        }
    }
}
