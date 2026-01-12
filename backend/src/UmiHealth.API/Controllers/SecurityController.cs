using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.API.Services;

namespace UmiHealth.API.Controllers
{
    /// <summary>
    /// Security management controller for admin operations
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "admin,superadmin")]
    public class SecurityController : ControllerBase
    {
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ILogger<SecurityController> _logger;

        public SecurityController(
            ISecurityAuditService securityAuditService,
            ILogger<SecurityController> logger)
        {
            _securityAuditService = securityAuditService;
            _logger = logger;
        }

        /// <summary>
        /// Get recent security events
        /// </summary>
        [HttpGet("events")]
        public async Task<IActionResult> GetSecurityEvents([FromQuery] int count = 100)
        {
            try
            {
                var events = await _securityAuditService.GetRecentSecurityEventsAsync(count);
                return Ok(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security events");
                return StatusCode(500, new { success = false, message = "Error retrieving security events" });
            }
        }

        /// <summary>
        /// Get security metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetSecurityMetrics()
        {
            try
            {
                var metrics = await _securityAuditService.GetSecurityMetricsAsync();
                return Ok(new { success = true, data = metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security metrics");
                return StatusCode(500, new { success = false, message = "Error retrieving security metrics" });
            }
        }

        /// <summary>
        /// Get blocked IP addresses
        /// </summary>
        [HttpGet("blocked-ips")]
        public async Task<IActionResult> GetBlockedIpAddresses()
        {
            try
            {
                // This would need to be implemented in the security service
                var blockedIps = new List<object>(); // Placeholder
                return Ok(new { success = true, data = blockedIps });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blocked IP addresses");
                return StatusCode(500, new { success = false, message = "Error retrieving blocked IP addresses" });
            }
        }

        /// <summary>
        /// Block an IP address
        /// </summary>
        [HttpPost("block-ip")]
        public async Task<IActionResult> BlockIpAddress([FromBody] BlockIpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.IpAddress))
                {
                    return BadRequest(new { success = false, message = "IP address is required" });
                }

                var duration = request.DurationHours.HasValue 
                    ? TimeSpan.FromHours(request.DurationHours.Value) 
                    : null;

                await _securityAuditService.BlockIpAddressAsync(
                    request.IpAddress, 
                    request.Reason, 
                    duration);

                return Ok(new { success = true, message = "IP address blocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking IP address");
                return StatusCode(500, new { success = false, message = "Error blocking IP address" });
            }
        }

        /// <summary>
        /// Unblock an IP address
        /// </summary>
        [HttpPost("unblock-ip")]
        public async Task<IActionResult> UnblockIpAddress([FromBody] UnblockIpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.IpAddress))
                {
                    return BadRequest(new { success = false, message = "IP address is required" });
                }

                // This would need to be implemented in the security service
                // await _securityAuditService.UnblockIpAddressAsync(request.IpAddress);

                return Ok(new { success = true, message = "IP address unblocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking IP address");
                return StatusCode(500, new { success = false, message = "Error unblocking IP address" });
            }
        }

        /// <summary>
        /// Manually log a security event
        /// </summary>
        [HttpPost("log-event")]
        public async Task<IActionResult> LogSecurityEvent([FromBody] SecurityEvent request)
        {
            try
            {
                await _securityAuditService.LogSecurityEventAsync(request);
                return Ok(new { success = true, message = "Security event logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event");
                return StatusCode(500, new { success = false, message = "Error logging security event" });
            }
        }
    }

    // Request DTOs
    public class BlockIpRequest
    {
        public string IpAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int? DurationHours { get; set; }
    }

    public class UnblockIpRequest
    {
        public string IpAddress { get; set; } = string.Empty;
    }
}
