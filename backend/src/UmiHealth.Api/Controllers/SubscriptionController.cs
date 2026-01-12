using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealth.Application.Services;
using UmiHealth.Application.Models;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;
using System;
using System.Threading.Tasks;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly SharedDbContext _context;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            SharedDbContext context,
            ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get current subscription status for the tenant
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            try
            {
                var tenantId = GetTenantId();
                if (!tenantId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Tenant not found" });
                }

                // Get tenant info for trial calculation
                var tenant = await _context.Tenants.FindAsync(tenantId.Value);
                if (tenant == null)
                {
                    return NotFound(new { success = false, message = "Tenant not found" });
                }

                // Check trial period
                var trialEndDate = tenant.CreatedAt.AddDays(14);
                var isInTrial = DateTime.UtcNow <= trialEndDate;
                var trialDaysRemaining = isInTrial ? (int)(trialEndDate - DateTime.UtcNow).TotalDays : 0;

                // Get active subscription
                var subscription = await _subscriptionService.GetTenantSubscriptionAsync(tenantId.Value);
                
                var response = new
                {
                    success = true,
                    data = new
                    {
                        tenantId = tenantId.Value,
                        isInTrial = isInTrial,
                        trialEndDate = trialEndDate,
                        trialDaysRemaining = trialDaysRemaining,
                        hasActiveSubscription = subscription != null && subscription.EndDate > DateTime.UtcNow,
                        subscription = subscription != null ? new
                        {
                            id = subscription.Id,
                            planType = subscription.PlanType,
                            startDate = subscription.StartDate,
                            endDate = subscription.EndDate,
                            isActive = subscription.IsActive
                        } : null,
                        availablePlans = GetAvailablePlans()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status");
                return StatusCode(500, new { success = false, message = "Error retrieving subscription status" });
            }
        }

        /// <summary>
        /// Create or upgrade subscription with payment confirmation
        /// </summary>
        [HttpPost("subscribe")]
        public async Task<IActionResult> CreateSubscription([FromBody] SubscriptionRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                if (!tenantId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Tenant not found" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
                }

                // For now, create subscription without payment (existing behavior)
                // TODO: Integrate with payment system when ready
                var subscription = await _subscriptionService.CreateSubscriptionAsync(tenantId.Value, request.PlanType);
                
                // Set subscription to expire in 30 days (or based on plan)
                subscription.EndDate = DateTime.UtcNow.AddDays(30);
                subscription.IsActive = true;
                
                // Update subscription in database
                _context.Subscriptions.Update(subscription);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created subscription {SubscriptionId} for tenant {TenantId} with plan {Plan}", 
                    subscription.Id, tenantId.Value, request.PlanType);

                return Ok(new 
                { 
                    success = true, 
                    message = "Subscription activated successfully",
                    data = new
                    {
                        subscriptionId = subscription.Id,
                        planType = subscription.PlanType,
                        startDate = subscription.StartDate,
                        endDate = subscription.EndDate,
                        isActive = subscription.IsActive,
                        maxUsers = GetPlanMaxUsers(request.PlanType),
                        maxBranches = GetPlanMaxBranches(request.PlanType)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return StatusCode(500, new { success = false, message = "Error creating subscription" });
            }
        }

        /// <summary>
        /// Submit payment for subscription with receipt
        /// </summary>
        [HttpPost("submit-payment")]
        public async Task<IActionResult> SubmitPayment([FromBody] PaymentRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                if (!tenantId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Tenant not found" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid payment data", errors = ModelState });
                }

                // Validate payment amount matches plan price
                var expectedAmount = GetPlanPrice(request.PlanType);
                if (request.Amount != expectedAmount)
                {
                    return BadRequest(new { success = false, message = $"Payment amount {request.Amount} does not match plan price {expectedAmount}" });
                }

                // Generate payment ID
                var paymentId = Guid.NewGuid().ToString("N")[..8].ToUpper();
                
                // Create payment record
                var payment = new PaymentRecord
                {
                    Id = paymentId,
                    TenantId = tenantId.Value,
                    PlanType = request.PlanType,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    TransactionReference = request.TransactionReference,
                    PaymentReceipt = request.PaymentReceipt,
                    Status = PaymentStatusType.Pending,
                    RequestDate = DateTime.UtcNow,
                    AdditionalNotes = request.AdditionalNotes
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Send real-time notification to operations and super admin
                await NotifyPaymentApprovalRequest(payment);

                _logger.LogInformation("Payment submitted {PaymentId} for tenant {TenantId}, plan {Plan}, amount {Amount}", 
                    paymentId, tenantId.Value, request.PlanType, request.Amount);

                return Ok(new PaymentResponse
                {
                    Success = true,
                    Message = "Payment submitted for approval",
                    PaymentId = paymentId,
                    Status = PaymentStatusType.Pending,
                    EstimatedApprovalTime = DateTime.UtcNow.AddHours(2) // Estimate 2 hours for approval
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting payment");
                return StatusCode(500, new { success = false, message = "Error submitting payment" });
            }
        }

        /// <summary>
        /// Get payment status
        /// </summary>
        [HttpGet("payment-status/{paymentId}")]
        public async Task<IActionResult> GetPaymentStatus(string paymentId)
        {
            try
            {
                if (string.IsNullOrEmpty(paymentId))
                {
                    return BadRequest(new { success = false, message = "Payment ID required" });
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    return NotFound(new { success = false, message = "Payment not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        paymentId = payment.Id,
                        status = payment.Status,
                        planType = payment.PlanType,
                        amount = payment.Amount,
                        paymentMethod = payment.PaymentMethod,
                        requestDate = payment.RequestDate,
                        approvalDate = payment.ApprovalDate,
                        approvedBy = payment.ApprovedBy,
                        transactionId = payment.TransactionId,
                        confirmationNumber = payment.ConfirmationNumber,
                        paymentReceipt = payment.PaymentReceipt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status");
                return StatusCode(500, new { success = false, message = "Error retrieving payment status" });
            }
        }

        /// <summary>
        /// Cancel subscription
        /// </summary>
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelSubscription()
        {
            try
            {
                var tenantId = GetTenantId();
                if (!tenantId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Tenant not found" });
                }

                var subscription = await _subscriptionService.GetTenantSubscriptionAsync(tenantId.Value);
                if (subscription == null)
                {
                    return BadRequest(new { success = false, message = "No active subscription found" });
                }

                var result = await _subscriptionService.CancelSubscriptionAsync(subscription.Id);
                if (result)
                {
                    _logger.LogInformation("Cancelled subscription {SubscriptionId} for tenant {TenantId}", 
                        subscription.Id, tenantId.Value);
                    
                    return Ok(new { success = true, message = "Subscription cancelled successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to cancel subscription" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription");
                return StatusCode(500, new { success = false, message = "Error cancelling subscription" });
            }
        }

        /// <summary>
        /// Get available subscription plans
        /// </summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        public IActionResult GetAvailablePlans()
        {
            var plans = new[]
            {
                new
                {
                    id = "starter",
                    name = "Starter",
                    description = "Perfect for small pharmacies",
                    regularPrice = "ZMW 300",
                    promoPrice = "ZMW 249",
                    currency = "ZMW",
                    billingCycle = "monthly",
                    maxUsers = 3,
                    maxBranches = 1,
                    features = new[] { "Core pharmacy management", "Inventory tracking", "Basic reports", "Email support" }
                },
                new
                {
                    id = "professional",
                    name = "Professional",
                    description = "Ideal for growing pharmacies",
                    regularPrice = "ZMW 500",
                    promoPrice = "ZMW 449",
                    currency = "ZMW",
                    billingCycle = "monthly",
                    maxUsers = 10,
                    maxBranches = 3,
                    features = new[] { "All Starter features", "Advanced analytics", "Multi-branch support", "Priority support", "API access" }
                },
                new
                {
                    id = "business",
                    name = "Business",
                    description = "For established pharmacies",
                    regularPrice = "ZMW 1000",
                    promoPrice = "ZMW 849",
                    currency = "ZMW",
                    billingCycle = "monthly",
                    maxUsers = 25,
                    maxBranches = 5,
                    features = new[] { "All Professional features", "Custom integrations", "Dedicated support", "Advanced security", "Priority updates" }
                },
                new
                {
                    id = "enterprise",
                    name = "Enterprise",
                    description = "For large pharmacy chains",
                    regularPrice = "ZMW 1800",
                    promoPrice = "ZMW 1549",
                    currency = "ZMW",
                    billingCycle = "monthly",
                    maxUsers = 50,
                    maxBranches = 10,
                    features = new[] { "All Business features", "Unlimited storage", "24/7 phone support", "Custom development", "White-label options" }
                }
            };

            return Ok(new { success = true, data = plans });
        }

        private decimal GetPlanPrice(string planType)
        {
            return planType.ToLower() switch
            {
                "starter" => 249m, // ZMW 249
                "professional" => 449m, // ZMW 449
                "business" => 849m, // ZMW 849
                "enterprise" => 1549m, // ZMW 1549
                _ => 249m
            };
        }

        private async Task NotifyPaymentApprovalRequest(PaymentRecord payment)
        {
            // Create notification for operations and super admin users
            var notification = new PaymentApprovalRequest
            {
                PaymentId = payment.Id,
                TenantId = payment.TenantId.ToString(),
                TenantName = await GetTenantName(payment.TenantId),
                PlanType = payment.PlanType,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                TransactionReference = payment.TransactionReference,
                PaymentReceipt = payment.PaymentReceipt,
                AdditionalNotes = payment.AdditionalNotes,
                RequestedBy = await GetRequestingUser(payment.TenantId)
            };

            // Send to SignalR hub for real-time notifications
            // This would be implemented with a SignalR hub
            _logger.LogInformation("Payment approval request sent for payment {PaymentId}", payment.Id);
        }

        private async Task<string> GetTenantName(Guid tenantId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            return tenant?.Name ?? "Unknown Tenant";
        }

        private async Task<string> GetRequestingUser(Guid tenantId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId);
            return user?.Email ?? "Unknown User";
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

        private int GetPlanMaxUsers(string planType)
        {
            return planType.ToLower() switch
            {
                "starter" => 3,
                "professional" => 10,
                "business" => 25,
                "enterprise" => 50,
                _ => 3
            };
        }

        private int GetPlanMaxBranches(string planType)
        {
            return planType.ToLower() switch
            {
                "starter" => 1,
                "professional" => 3,
                "business" => 5,
                "enterprise" => 10,
                _ => 1
            };
        }
    }

    public class SubscriptionRequest
    {
        public string PlanType { get; set; } = string.Empty;
    }
}
