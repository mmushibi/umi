using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CashierController : ControllerBase
    {
        private readonly ICashierIntegrationService _cashierIntegrationService;
        private readonly ILogger<CashierController> _logger;
        private readonly ISubscriptionFeatureService _subscriptionFeatureService;

        public CashierController(
            ICashierIntegrationService cashierIntegrationService,
            ILogger<CashierController> logger,
            ISubscriptionFeatureService subscriptionFeatureService)
        {
            _cashierIntegrationService = cashierIntegrationService;
            _logger = logger;
            _subscriptionFeatureService = subscriptionFeatureService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterPortal()
        {
            try
            {
                // Check subscription feature access
                var featureCheck = await _subscriptionFeatureService.EnforceFeatureAccessAsync(HttpContext, "basic_prescriptions");
                if (featureCheck != null)
                {
                    return featureCheck;
                }

                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User, tenant, or branch information missing");
                }

                await _cashierIntegrationService.RegisterCashierPortalAsync(
                    Guid.Parse(tenantId),
                    Guid.Parse(branchId),
                    Guid.Parse(userId));

                return Ok(new { message = "Cashier portal registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering cashier portal");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("unregister")]
        public async Task<IActionResult> UnregisterPortal()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User, tenant, or branch information missing");
                }

                await _cashierIntegrationService.UnregisterCashierPortalAsync(
                    Guid.Parse(tenantId),
                    Guid.Parse(branchId),
                    Guid.Parse(userId));

                return Ok(new { message = "Cashier portal unregistered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering cashier portal");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("notify/{entityType}")]
        public async Task<IActionResult> NotifyDataChange(string entityType, [FromBody] object data)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId))
                {
                    return BadRequest("Tenant or branch information missing");
                }

                await _cashierIntegrationService.NotifyDataChangeAsync(
                    Guid.Parse(tenantId),
                    Guid.Parse(branchId),
                    entityType,
                    data);

                return Ok(new { message = "Data change notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data change notification for {EntityType}", entityType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetPortalStatus()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User, tenant, or branch information missing");
                }

                // This would typically check the active portals registry
                // For now, return a basic status
                return Ok(new
                {
                    isRegistered = true,
                    tenantId,
                    branchId,
                    lastActivity = DateTime.UtcNow,
                    syncStatus = "active"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting portal status");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
