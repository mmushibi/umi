using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            SharedDbContext context,
            ILogger<UserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Request additional user limit
        /// </summary>
        [HttpPost("request-user-limit")]
        public async Task<IActionResult> RequestUserLimit([FromBody] UserLimitRequestData request)
        {
            try
            {
                var tenantId = GetTenantId();
                if (!tenantId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Tenant not found" });
                }

                // Get current user count
                var currentUsers = await _context.Users
                    .CountAsync(u => u.TenantId == tenantId.Value);

                // Create user limit request
                var limitRequest = new UserLimitRequest
                {
                    TenantId = tenantId.Value,
                    CurrentUsers = currentUsers,
                    RequestedUsers = request.RequestedUsers,
                    Reason = request.Reason,
                    RequestedByUserId = GetCurrentUserId(),
                    Status = "pending"
                };

                _context.UserLimitRequests.Add(limitRequest);
                await _context.SaveChangesAsync();

                // Notify operations and super admin
                await NotifyUserLimitRequest(limitRequest);

                _logger.LogInformation("User limit request {RequestId} submitted for tenant {TenantId}", 
                    limitRequest.Id, tenantId.Value);

                return Ok(new 
                { 
                    success = true, 
                    message = "User limit request submitted for approval",
                    requestId = limitRequest.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting user limit request");
                return StatusCode(500, new { success = false, message = "Error submitting request" });
            }
        }

        /// <summary>
        /// Get current user count and limits
        /// </summary>
        [HttpGet("user-stats")]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var tenantId = GetTenantId();
                if (!tenantId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Tenant not found" });
                }

                var currentUsers = await _context.Users
                    .CountAsync(u => u.TenantId == tenantId.Value);

                var tenant = await _context.Tenants.FindAsync(tenantId.Value);
                var maxUsers = 0;
                if (tenant != null && !string.IsNullOrEmpty(tenant.Settings))
                {
                    try
                    {
                        // Support settings stored as json
                        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tenant.Settings);
                        if (settings != null && settings.TryGetValue("maxUsers", out var maxUsersObj) && maxUsersObj != null)
                        {
                            int.TryParse(maxUsersObj.ToString(), out maxUsers);
                        }
                    }
                    catch
                    {
                        // Support simple fallback format: "maxUsers=10"
                        var parts = tenant.Settings.Split('=', 2, StringSplitOptions.TrimEntries);
                        if (parts.Length == 2 && parts[0].Equals("maxUsers", StringComparison.OrdinalIgnoreCase))
                        {
                            int.TryParse(parts[1], out maxUsers);
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        currentUsers = currentUsers,
                        maxUsers = maxUsers,
                        canAddUsers = currentUsers < maxUsers,
                        additionalUsersAllowed = maxUsers - currentUsers
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, new { success = false, message = "Error retrieving user stats" });
            }
        }

        private async Task NotifyUserLimitRequest(UserLimitRequest request)
        {
            // TODO: Implement real-time notification to operations and super admin
            _logger.LogInformation("User limit request notification sent for {RequestId}", request.Id);
        }

        private Guid? GetTenantId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = User.FindFirst("TenantId") ?? User.FindFirst("tenant_id");
                if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
                {
                    return tenantId;
                }
            }
            return null;
        }

        private Guid GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("user_id");
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }
            }
            return Guid.Empty;
        }
    }

    public class UserLimitRequestData
    {
        public int CurrentUsers { get; set; }
        public int RequestedUsers { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
