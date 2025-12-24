using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CrossPortalController : ControllerBase
    {
        private readonly ICrossPortalSyncService _crossPortalSync;
        private readonly ILogger<CrossPortalController> _logger;

        public CrossPortalController(
            ICrossPortalSyncService crossPortalSync,
            ILogger<CrossPortalController> logger)
        {
            _crossPortalSync = crossPortalSync;
            _logger = logger;
        }

        [HttpPost("broadcast/{entityType}")]
        public async Task<IActionResult> BroadcastDataChange(string entityType, [FromBody] BroadcastRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var portalType = User.FindFirst("PortalType")?.Value ?? "unknown";

                await _crossPortalSync.BroadcastDataChangeAsync(
                    entityType, 
                    request.Data, 
                    portalType, 
                    string.IsNullOrEmpty(tenantId) ? null : Guid.Parse(tenantId));

                return Ok(new { message = "Data broadcast successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting data change for {EntityType}", entityType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("subscribe/{entityType}")]
        public async Task<IActionResult> SubscribeToEntity(string entityType, [FromBody] SubscribeRequest request)
        {
            try
            {
                var portalType = User.FindFirst("PortalType")?.Value ?? "unknown";

                await _crossPortalSync.SubscribeToEntityAsync(
                    entityType, 
                    portalType, 
                    async (data) => {
                        // This would typically use SignalR or WebSocket to push data
                        // For now, we'll just log the callback
                        _logger.LogInformation("Received data update for {EntityType} in portal {PortalType}", entityType, portalType);
                        return Task.CompletedTask;
                    });

                return Ok(new { message = "Subscription successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to {EntityType}", entityType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("unsubscribe/{entityType}")]
        public async Task<IActionResult> UnsubscribeFromEntity(string entityType)
        {
            try
            {
                var portalType = User.FindFirst("PortalType")?.Value ?? "unknown";

                await _crossPortalSync.UnsubscribeFromEntityAsync(entityType, portalType);

                return Ok(new { message = "Unsubscription successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from {EntityType}", entityType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterPortal([FromBody] RegisterPortalRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Tenant or user information missing");
                }

                await _crossPortalSync.RegisterPortalAsync(
                    request.PortalType,
                    Guid.Parse(tenantId),
                    Guid.Parse(userId));

                return Ok(new { message = "Portal registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering portal");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("unregister")]
        public async Task<IActionResult> UnregisterPortal([FromBody] UnregisterPortalRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Tenant or user information missing");
                }

                await _crossPortalSync.UnregisterPortalAsync(
                    request.PortalType,
                    Guid.Parse(tenantId),
                    Guid.Parse(userId));

                return Ok(new { message = "Portal unregistered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering portal");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("active-portals")]
        public async Task<IActionResult> GetActivePortals()
        {
            try
            {
                var activePortals = await _crossPortalSync.GetActivePortalsAsync();
                return Ok(activePortals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active portals");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetSyncStats()
        {
            try
            {
                var stats = await _crossPortalSync.GetSyncStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync stats");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class BroadcastRequest
    {
        public object Data { get; set; } = new();
        public string? TargetPortal { get; set; }
        public bool IncludeSelf { get; set; } = false;
    }

    public class SubscribeRequest
    {
        public string? CallbackUrl { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
    }

    public class RegisterPortalRequest
    {
        public string PortalType { get; set; } = string.Empty;
        public Dictionary<string, object>? Capabilities { get; set; }
    }

    public class UnregisterPortalRequest
    {
        public string PortalType { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
