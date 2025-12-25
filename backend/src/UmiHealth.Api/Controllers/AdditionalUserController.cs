using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdditionalUserController : ControllerBase
    {
        private readonly IAdditionalUserService _additionalUserService;
        private readonly ILogger<AdditionalUserController> _logger;

        public AdditionalUserController(
            IAdditionalUserService additionalUserService,
            ILogger<AdditionalUserController> logger)
        {
            _additionalUserService = additionalUserService;
            _logger = logger;
        }

        /// <summary>
        /// Request additional user beyond subscription limit
        /// </summary>
        [HttpPost("request")]
        public async Task<ActionResult<AdditionalUserRequestResult>> RequestAdditionalUser([FromBody] CreateAdditionalUserRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                request.RequestedBy = GetCurrentUserId();

                var result = await _additionalUserService.RequestAdditionalUserAsync(tenantId, request);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(new { error = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting additional user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get pending additional user requests (Operations and Super Admin only)
        /// </summary>
        [HttpGet("requests/pending")]
        [Authorize(Roles = "operations,super_admin")]
        public async Task<ActionResult<IEnumerable<AdditionalUserRequestDto>>> GetPendingRequests()
        {
            try
            {
                var requests = await _additionalUserService.GetPendingRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending additional user requests");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Approve additional user request (Operations and Super Admin only)
        /// </summary>
        [HttpPost("requests/{requestId}/approve")]
        [Authorize(Roles = "operations,super_admin")]
        public async Task<ActionResult> ApproveAdditionalUserRequest(string requestId)
        {
            try
            {
                var approvedBy = GetCurrentUserId();
                var success = await _additionalUserService.ApproveAdditionalUserRequestAsync(requestId, approvedBy);

                if (success)
                {
                    return Ok(new { message = "Additional user request approved successfully" });
                }
                else
                {
                    return NotFound(new { error = "Request not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving additional user request");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Reject additional user request (Operations and Super Admin only)
        /// </summary>
        [HttpPost("requests/{requestId}/reject")]
        [Authorize(Roles = "operations,super_admin")]
        public async Task<ActionResult> RejectAdditionalUserRequest(string requestId, [FromBody] RejectRequestModel model)
        {
            try
            {
                var approvedBy = GetCurrentUserId();
                var success = await _additionalUserService.RejectAdditionalUserRequestAsync(requestId, approvedBy, model.RejectionReason);

                if (success)
                {
                    return Ok(new { message = "Additional user request rejected successfully" });
                }
                else
                {
                    return NotFound(new { error = "Request not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting additional user request");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get monthly additional user charges for tenant
        /// </summary>
        [HttpGet("charges/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<AdditionalUserChargeDto>>> GetMonthlyCharges(int year, int month)
        {
            try
            {
                var tenantId = GetTenantId();
                var charges = await _additionalUserService.GetMonthlyChargesAsync(tenantId, year, month);
                return Ok(charges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly additional user charges");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get additional user summary for tenant
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<AdditionalUserSummaryDto>> GetAdditionalUserSummary()
        {
            try
            {
                var tenantId = GetTenantId();
                var summary = await _additionalUserService.GetAdditionalUserSummaryAsync(tenantId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting additional user summary");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Process payment for additional user charge
        /// </summary>
        [HttpPost("charges/{chargeId}/payment")]
        public async Task<ActionResult> ProcessPayment(string chargeId, [FromBody] PaymentDetails paymentDetails)
        {
            try
            {
                var success = await _additionalUserService.ProcessAdditionalUserPaymentAsync(chargeId, paymentDetails);

                if (success)
                {
                    return Ok(new { message = "Payment processed successfully" });
                }
                else
                {
                    return NotFound(new { error = "Charge not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for additional user charge");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all additional user requests for operations dashboard
        /// </summary>
        [HttpGet("operations/requests")]
        [Authorize(Roles = "operations,super_admin")]
        public async Task<ActionResult<IEnumerable<AdditionalUserRequestDto>>> GetAllRequests()
        {
            try
            {
                var requests = await _additionalUserService.GetPendingRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all additional user requests");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get monthly billing report for operations
        /// </summary>
        [HttpGet("operations/billing/{year}/{month}")]
        [Authorize(Roles = "operations,super_admin")]
        public async Task<ActionResult> GetMonthlyBillingReport(int year, int month)
        {
            try
            {
                // This would typically be implemented in the service layer
                // For now, return a placeholder response
                return Ok(new { message = "Monthly billing report feature coming soon" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly billing report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private Guid GetTenantId()
        {
            // Extract tenant ID from JWT claims or user context
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                return tenantId;
            }
            throw new UnauthorizedAccessException("Tenant ID not found in token");
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("user_id");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User ID not found in token");
        }
    }

    public class RejectRequestModel
    {
        public string RejectionReason { get; set; } = string.Empty;
    }
}
