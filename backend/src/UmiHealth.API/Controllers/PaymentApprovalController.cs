using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Application.Models;
using UmiHealth.Application.Services;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;
using UmiHealth.API.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "super_admin,operations")]
    public class PaymentApprovalController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PaymentApprovalController> _logger;
        private readonly IHubContext<PaymentNotificationHub> _hubContext;
        private readonly IPaymentNotificationService _notificationService;

        public PaymentApprovalController(
            SharedDbContext context,
            ILogger<PaymentApprovalController> logger,
            IHubContext<PaymentNotificationHub> hubContext,
            IPaymentNotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get pending payment approvals
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPayments()
        {
            try
            {
                var pendingPayments = await _context.Payments
                    .Where(p => p.Status == PaymentStatusType.Pending)
                    .Include(p => p.Tenant)
                    .OrderByDescending(p => p.RequestDate)
                    .Select(p => new
                    {
                        paymentId = p.Id,
                        tenantId = p.TenantId,
                        tenantName = p.Tenant.Name,
                        planType = p.PlanType,
                        amount = p.Amount,
                        paymentMethod = p.PaymentMethod,
                        transactionReference = p.TransactionReference,
                        paymentReceipt = p.PaymentReceipt,
                        requestDate = p.RequestDate,
                        additionalNotes = p.AdditionalNotes
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = pendingPayments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending payments");
                return StatusCode(500, new { success = false, message = "Error retrieving pending payments" });
            }
        }

        /// <summary>
        /// Approve payment
        /// </summary>
        [HttpPost("approve")]
        public async Task<IActionResult> ApprovePayment([FromBody] PaymentStatusUpdate request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PaymentId))
                {
                    return BadRequest(new { success = false, message = "Payment ID required" });
                }

                var payment = await _context.Payments
                    .Include(p => p.Tenant)
                    .FirstOrDefaultAsync(p => p.Id == request.PaymentId);

                if (payment == null)
                {
                    return NotFound(new { success = false, message = "Payment not found" });
                }

                if (payment.Status != PaymentStatusType.Pending)
                {
                    return BadRequest(new { success = false, message = "Payment already processed" });
                }

                // Update payment status
                payment.Status = PaymentStatusType.Approved;
                payment.ApprovalDate = DateTime.UtcNow;
                payment.ApprovedBy = User.Identity?.Name ?? "Unknown";
                payment.TransactionId = request.TransactionId;
                payment.ConfirmationNumber = GenerateConfirmationNumber();

                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
                // Activate subscription
                await ActivateSubscription(payment.TenantId, payment.PlanType);

                // Send confirmation to tenant
                await SendConfirmationToTenant(payment);
                await _hubContext.Clients.All.SendAsync("paymentApproved", new PaymentApprovalNotification
                {
                    PaymentId = payment.Id,
                    TenantId = payment.TenantId,
                    Success = true,
                    Message = "Payment approved successfully",
                    ApprovedBy = payment.ApprovedBy,
                    ApprovalDate = payment.ApprovalDate,
                    TransactionId = payment.TransactionId,
                    ConfirmationNumber = payment.ConfirmationNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payment");
                return StatusCode(500, new { success = false, message = "Error approving payment" });
            }
        }

        /// <summary>
        /// Reject payment
        /// </summary>
        [HttpPost("reject")]
        public async Task<IActionResult> RejectPayment([FromBody] PaymentStatusUpdate request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PaymentId))
                {
                    return BadRequest(new { success = false, message = "Payment ID required" });
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == request.PaymentId);

                if (payment == null)
                {
                    return NotFound(new { success = false, message = "Payment not found" });
                }

                if (payment.Status != PaymentStatusType.Pending)
                {
                    return BadRequest(new { success = false, message = "Payment already processed" });
                }

                // Update payment status
                payment.Status = PaymentStatusType.Rejected;
                payment.ApprovalDate = DateTime.UtcNow;
                payment.ApprovedBy = User.Identity?.Name ?? "Unknown";
                payment.AdditionalNotes = request.AdditionalNotes;

                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                // Send rejection notification to tenant
                await _notificationService.SendPaymentRejectionAsync(payment.TenantId, payment.Id, payment.AdditionalNotes);
                _logger.LogInformation("Payment rejection sent to tenant {TenantId} for payment {PaymentId}", 
                    payment.TenantId, payment.Id);

                return Ok(new
                {
                    success = true,
                    message = "Payment rejected successfully",
                    approvedBy = payment.ApprovedBy,
                    approvalDate = payment.ApprovalDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment");
                return StatusCode(500, new { success = false, message = "Error rejecting payment" });
            }
        }

        /// <summary>
        /// Get user limit override requests
        /// </summary>
        [HttpGet("user-requests")]
        public async Task<IActionResult> GetUserLimitRequests()
        {
            try
            {
                var requests = await _context.UserLimitRequests
                    .Include(r => r.Tenant)
                    .Include(r => r.RequestedByUser)
                    .Where(r => r.Status == "pending")
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new
                    {
                        id = r.Id,
                        tenantId = r.TenantId,
                        tenantName = r.Tenant.Name,
                        requestedBy = r.RequestedByUser.Email,
                        currentUsers = r.CurrentUsers,
                        requestedUsers = r.RequestedUsers,
                        additionalUsers = r.AdditionalUsers,
                        additionalCost = r.AdditionalCost,
                        reason = r.Reason,
                        requestDate = r.RequestDate
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user limit requests");
                return StatusCode(500, new { success = false, message = "Error retrieving user limit requests" });
            }
        }

        /// <summary>
        /// Approve user limit increase
        /// </summary>
        [HttpPost("approve-user-limit")]
        public async Task<IActionResult> ApproveUserLimit([FromBody] UserLimitApprovalRequest request)
        {
            try
            {
                var limitRequest = await _context.UserLimitRequests
                    .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                if (limitRequest == null)
                {
                    return NotFound(new { success = false, message = "Request not found" });
                }

                // Update request status
                limitRequest.Status = "approved";
                limitRequest.ApprovalDate = DateTime.UtcNow;
                limitRequest.ApprovedBy = User.Identity?.Name ?? "Unknown";

                // Update tenant user limit
                var tenant = await _context.Tenants.FindAsync(limitRequest.TenantId);
                if (tenant != null)
                {
                    tenant.MaxUsers = limitRequest.RequestedUsers;
                    _context.Tenants.Update(tenant);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User limit request {RequestId} approved for tenant {TenantId}", 
                    request.RequestId, limitRequest.TenantId);
                
                await _notificationService.SendUserLimitApprovalAsync(limitRequest.TenantId, limitRequest.Id, limitRequest.RequestedUsers);

                return Ok(new { success = true, message = "User limit approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user limit");
                return StatusCode(500, new { success = false, message = "Error approving user limit" });
            }
        }

        private async Task ActivateSubscription(Guid tenantId, string planType)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (subscription != null)
            {
                subscription.PlanType = planType;
                subscription.StartDate = DateTime.UtcNow;
                subscription.EndDate = DateTime.UtcNow.AddDays(30);
                subscription.IsActive = true;
                _context.Subscriptions.Update(subscription);
            }
            else
            {
                // Create new subscription
                var newSubscription = new Subscription
                {
                    TenantId = tenantId,
                    PlanType = planType,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true
                };
                _context.Subscriptions.Add(newSubscription);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SendConfirmationToTenant(PaymentRecord payment)
        {
            await _hubContext.Clients.Group($"tenant_{payment.TenantId}").SendAsync("paymentApproved", new PaymentApprovalNotification
            {
                PaymentId = payment.Id,
                TenantId = payment.TenantId,
                Status = "approved",
                ApprovedBy = payment.ApprovedBy,
                ApprovalDate = payment.ApprovalDate,
                AdditionalNotes = payment.AdditionalNotes
            });

            _logger.LogInformation("Payment approval sent to tenant {TenantId} for payment {PaymentId}", 
                payment.TenantId, payment.Id);
        }

        private async Task NotifyUserLimitRequest(string requestId, string status, string? approvedBy = null)
        {
            await _hubContext.Clients.Group($"tenant_{requestId}").SendAsync("userLimitRequestUpdate", new
            {
                requestId = requestId,
                status = status,
                approvedBy = approvedBy,
                updatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("User limit request update sent: {RequestId} - {Status}", requestId, status);
        }
    }

    public class UserLimitApprovalRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
