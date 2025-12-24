using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class SubscriptionsOperationsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<SubscriptionsOperationsController> _logger;

        public SubscriptionsOperationsController(SharedDbContext context, ILogger<SubscriptionsOperationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSubscriptions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? planType = null,
            [FromQuery] string? tenantId = null)
        {
            try
            {
                // Mock subscription data for demonstration
                var subscriptions = new List<object>();
                
                for (int i = 1; i <= 25; i++)
                {
                    subscriptions.Add(new
                    {
                        Id = Guid.NewGuid(),
                        TenantId = string.IsNullOrEmpty(tenantId) ? Guid.NewGuid() : Guid.Parse(tenantId),
                        PlanType = i % 3 == 0 ? "basic" : i % 2 == 0 ? "professional" : "enterprise",
                        Status = i % 4 == 0 ? "expired" : i % 3 == 0 ? "cancelled" : "active",
                        BillingCycle = i % 2 == 0 ? "monthly" : "yearly",
                        Amount = i % 3 == 0 ? 50 : i % 2 == 0 ? 150 : 500,
                        Currency = "ZMW",
                        StartDate = DateTime.UtcNow.AddDays(-i * 30),
                        EndDate = DateTime.UtcNow.AddDays(i * 30),
                        AutoRenew = i % 3 != 0,
                        Features = i % 3 == 0 ? 
                            new List<string> { "Basic POS", "Inventory Management" } :
                            i % 2 == 0 ?
                            new List<string> { "Advanced POS", "Inventory Management", "Reports", "Multi-branch" } :
                            new List<string> { "Full POS Suite", "Advanced Analytics", "API Access", "Priority Support", "Unlimited Branches" },
                        Limits = i % 3 == 0 ?
                            new Dictionary<string, object> { ["users"] = 5, ["branches"] = 1, ["transactions"] = 1000 } :
                            i % 2 == 0 ?
                            new Dictionary<string, object> { ["users"] = 25, ["branches"] = 5, ["transactions"] = 10000 } :
                            new Dictionary<string, object> { ["users"] = 100, ["branches"] = 50, ["transactions"] = "unlimited" },
                        CreatedAt = DateTime.UtcNow.AddDays(-i * 30),
                        UpdatedAt = DateTime.UtcNow.AddDays(-i * 10)
                    });
                }

                if (!string.IsNullOrEmpty(search))
                {
                    subscriptions = subscriptions.Where(s => 
                        s.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.PlanType.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(status))
                {
                    subscriptions = subscriptions.Where(s => s.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(planType))
                {
                    subscriptions = subscriptions.Where(s => s.PlanType.ToString().Equals(planType, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var totalCount = subscriptions.Count;
                var pagedSubscriptions = subscriptions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    subscriptions = pagedSubscriptions,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscriptions");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSubscription(Guid id)
        {
            try
            {
                // Mock subscription data for demonstration
                var subscription = new
                {
                    Id = id,
                    TenantId = Guid.NewGuid(),
                    PlanType = "professional",
                    Status = "active",
                    BillingCycle = "monthly",
                    Amount = 150,
                    Currency = "ZMW",
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    AutoRenew = true,
                    Features = new List<string> { "Advanced POS", "Inventory Management", "Reports", "Multi-branch" },
                    Limits = new Dictionary<string, object> { ["users"] = 25, ["branches"] = 5, ["transactions"] = 10000 },
                    PaymentMethod = "card",
                    LastPaymentDate = DateTime.UtcNow.AddDays(-30),
                    NextBillingDate = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10),
                    BillingHistory = new List<object>
                    {
                        new
                        {
                            Id = Guid.NewGuid(),
                            Date = DateTime.UtcNow.AddDays(-30),
                            Amount = 150,
                            Status = "paid",
                            PaymentMethod = "card",
                            TransactionId = "txn_" + Guid.NewGuid().ToString("N")[..8]
                        },
                        new
                        {
                            Id = Guid.NewGuid(),
                            Date = DateTime.UtcNow.AddDays(-60),
                            Amount = 150,
                            Status = "paid",
                            PaymentMethod = "card",
                            TransactionId = "txn_" + Guid.NewGuid().ToString("N")[..8]
                        }
                    }
                };

                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription {SubscriptionId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateSubscription(CreateSubscriptionRequest request)
        {
            try
            {
                // Mock subscription creation for demonstration
                var subscription = new
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    PlanType = request.PlanType,
                    Status = "active",
                    BillingCycle = request.BillingCycle,
                    Amount = request.Amount,
                    Currency = request.Currency ?? "ZMW",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(request.BillingCycle == "monthly" ? 30 : 365),
                    AutoRenew = request.AutoRenew,
                    Features = request.Features ?? new List<string>(),
                    Limits = request.Limits ?? new Dictionary<string, object>(),
                    PaymentMethod = request.PaymentMethod,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubscription(Guid id, UpdateSubscriptionRequest request)
        {
            try
            {
                // Mock subscription update for demonstration
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription {SubscriptionId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelSubscription(Guid id, CancelSubscriptionRequest request)
        {
            try
            {
                // Mock subscription cancellation for demonstration
                return Ok(new { message = "Subscription cancelled successfully", effectiveDate = request.EffectiveDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/renew")]
        public async Task<IActionResult> RenewSubscription(Guid id, RenewSubscriptionRequest request)
        {
            try
            {
                // Mock subscription renewal for demonstration
                return Ok(new { message = "Subscription renewed successfully", newEndDate = DateTime.UtcNow.AddDays(30) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing subscription {SubscriptionId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<SubscriptionStats>> GetSubscriptionStats()
        {
            try
            {
                // Mock stats for demonstration
                return Ok(new SubscriptionStats
                {
                    TotalSubscriptions = 150,
                    ActiveSubscriptions = 120,
                    ExpiredSubscriptions = 15,
                    CancelledSubscriptions = 10,
                    TrialSubscriptions = 5,
                    MonthlyRevenue = 22500,
                    YearlyRevenue = 270000,
                    ChurnRate = 6.7,
                    PlansBreakdown = new List<object>
                    {
                        new { Plan = "basic", Count = 60, Revenue = 3000 },
                        new { Plan = "professional", Count = 75, Revenue = 11250 },
                        new { Plan = "enterprise", Count = 15, Revenue = 7500 }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription stats");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class CreateSubscriptionRequest
    {
        public Guid TenantId { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public bool AutoRenew { get; set; } = true;
        public List<string>? Features { get; set; }
        public Dictionary<string, object>? Limits { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class UpdateSubscriptionRequest
    {
        public string? PlanType { get; set; }
        public string? BillingCycle { get; set; }
        public decimal? Amount { get; set; }
        public bool? AutoRenew { get; set; }
        public List<string>? Features { get; set; }
        public Dictionary<string, object>? Limits { get; set; }
    }

    public class CancelSubscriptionRequest
    {
        public string Reason { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public bool RefundEligible { get; set; } = false;
    }

    public class RenewSubscriptionRequest
    {
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class SubscriptionStats
    {
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int CancelledSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public double ChurnRate { get; set; }
        public List<object> PlansBreakdown { get; set; } = new();
    }
}
