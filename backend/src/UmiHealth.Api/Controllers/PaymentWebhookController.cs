using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous]
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IWebhookService _webhookService;
        private readonly ILogger<PaymentWebhookController> _logger;

        public PaymentWebhookController(
            IPaymentService paymentService,
            IWebhookService webhookService,
            ILogger<PaymentWebhookController> logger)
        {
            _paymentService = paymentService;
            _webhookService = webhookService;
            _logger = logger;
        }

        [HttpPost("mobile-money")]
        public async Task<IActionResult> MobileMoneyWebhook([FromBody] MobileMoneyWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("Received mobile money webhook: {WebhookId}", payload.TransactionId);

                // Verify webhook signature
                if (!await VerifyWebhookSignature(payload))
                {
                    _logger.LogWarning("Invalid webhook signature for transaction {TransactionId}", payload.TransactionId);
                    return Unauthorized(new { error = "Invalid signature" });
                }

                // Process webhook based on event type
                switch (payload.EventType.ToLower())
                {
                    case "payment.completed":
                        await ProcessMobileMoneyPaymentAsync(payload);
                        break;
                    case "payment.failed":
                        await ProcessMobileMoneyFailureAsync(payload);
                        break;
                    case "payment.pending":
                        await ProcessMobileMoneyPendingAsync(payload);
                        break;
                    default:
                        _logger.LogWarning("Unknown webhook event type: {EventType}", payload.EventType);
                        break;
                }

                // Acknowledge receipt
                await AcknowledgeWebhookAsync(payload.WebhookId);

                return Ok(new { status = "received", transactionId = payload.TransactionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mobile money webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("bank-transfer")]
        public async Task<IActionResult> BankTransferWebhook([FromBody] BankTransferWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("Received bank transfer webhook: {WebhookId}", payload.TransactionId);

                if (!await VerifyWebhookSignature(payload))
                {
                    return Unauthorized(new { error = "Invalid signature" });
                }

                switch (payload.EventType.ToLower())
                {
                    case "transfer.completed":
                        await ProcessBankTransferCompletedAsync(payload);
                        break;
                    case "transfer.failed":
                        await ProcessBankTransferFailedAsync(payload);
                        break;
                    case "transfer.initiated":
                        await ProcessBankTransferInitiatedAsync(payload);
                        break;
                }

                await AcknowledgeWebhookAsync(payload.WebhookId);

                return Ok(new { status = "received", transactionId = payload.TransactionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bank transfer webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("card-payment")]
        public async Task<IActionResult> CardPaymentWebhook([FromBody] CardPaymentWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("Received card payment webhook: {WebhookId}", payload.TransactionId);

                if (!await VerifyWebhookSignature(payload))
                {
                    return Unauthorized(new { error = "Invalid signature" });
                }

                switch (payload.EventType.ToLower())
                {
                    case "payment.succeeded":
                        await ProcessCardPaymentSuccessAsync(payload);
                        break;
                    case "payment.failed":
                        await ProcessCardPaymentFailureAsync(payload);
                        break;
                    case "payment.refunded":
                        await ProcessCardPaymentRefundAsync(payload);
                        break;
                    case "dispute.created":
                        await ProcessCardDisputeAsync(payload);
                        break;
                }

                await AcknowledgeWebhookAsync(payload.WebhookId);

                return Ok(new { status = "received", transactionId = payload.TransactionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing card payment webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("subscription")]
        public async Task<IActionResult> SubscriptionWebhook([FromBody] SubscriptionWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("Received subscription webhook: {WebhookId}", payload.SubscriptionId);

                if (!await VerifyWebhookSignature(payload))
                {
                    return Unauthorized(new { error = "Invalid signature" });
                }

                switch (payload.EventType.ToLower())
                {
                    case "subscription.created":
                        await ProcessSubscriptionCreatedAsync(payload);
                        break;
                    case "subscription.updated":
                        await ProcessSubscriptionUpdatedAsync(payload);
                        break;
                    case "subscription.cancelled":
                        await ProcessSubscriptionCancelledAsync(payload);
                        break;
                    case "invoice.payment_succeeded":
                        await ProcessSubscriptionPaymentAsync(payload);
                        break;
                    case "invoice.payment_failed":
                        await ProcessSubscriptionPaymentFailedAsync(payload);
                        break;
                }

                await AcknowledgeWebhookAsync(payload.WebhookId);

                return Ok(new { status = "received", subscriptionId = payload.SubscriptionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("endpoints")]
        public IActionResult GetWebhookEndpoints()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/paymentwebhook";
            
            return Ok(new
            {
                mobileMoney = $"{baseUrl}/mobile-money",
                bankTransfer = $"{baseUrl}/bank-transfer",
                cardPayment = $"{baseUrl}/card-payment",
                subscription = $"{baseUrl}/subscription",
                documentation = $"{Request.Scheme}://{Request.Host}/api/docs/webhooks"
            });
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestWebhook([FromBody] TestWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("Received test webhook: {TestId}", payload.TestId);

                // Simulate webhook processing for testing
                await Task.Delay(100); // Simulate processing time

                return Ok(new 
                { 
                    status = "test_received",
                    testId = payload.TestId,
                    timestamp = DateTime.UtcNow,
                    message = "Webhook endpoint is working correctly"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing test webhook");
                return StatusCode(500, new { error = "Test webhook failed" });
            }
        }

        private async Task<bool> VerifyWebhookSignature(object payload)
        {
            try
            {
                // Get signature from header
                var signature = Request.Headers["X-Webhook-Signature"];
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Missing webhook signature");
                    return false;
                }

                // Get webhook secret for tenant
                var tenantId = ExtractTenantIdFromPayload(payload);
                var webhookSecret = await _webhookService.GetWebhookSecretAsync(tenantId);

                if (string.IsNullOrEmpty(webhookSecret))
                {
                    _logger.LogWarning("No webhook secret configured for tenant {TenantId}", tenantId);
                    return false;
                }

                // Calculate expected signature
                var payloadString = System.Text.Json.JsonSerializer.Serialize(payload);
                var expectedSignature = ComputeSignature(payloadString, webhookSecret);

                // Compare signatures securely
                return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook signature");
                return false;
            }
        }

        private async Task ProcessMobileMoneyPaymentAsync(MobileMoneyWebhookPayload payload)
        {
            // Update payment status in database
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "completed", payload.Data);

            // Trigger real-time notifications
            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "mobile_money.payment_completed",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Mobile money payment completed: {TransactionId}", payload.TransactionId);
        }

        private async Task ProcessMobileMoneyFailureAsync(MobileMoneyWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "failed", payload.Data?.ErrorMessage);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "mobile_money.payment_failed",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogWarning("Mobile money payment failed: {TransactionId}", payload.TransactionId);
        }

        private async Task ProcessMobileMoneyPendingAsync(MobileMoneyWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "pending", payload.Data);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "mobile_money.payment_pending",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessBankTransferCompletedAsync(BankTransferWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "completed", payload.Data);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "bank_transfer.completed",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessBankTransferFailedAsync(BankTransferWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "failed", payload.Data?.ErrorMessage);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "bank_transfer.failed",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessBankTransferInitiatedAsync(BankTransferWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "pending", payload.Data);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "bank_transfer.initiated",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessCardPaymentSuccessAsync(CardPaymentWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "completed", payload.Data);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "card_payment.succeeded",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessCardPaymentFailureAsync(CardPaymentWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "failed", payload.Data?.ErrorMessage);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "card_payment.failed",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessCardPaymentRefundAsync(CardPaymentWebhookPayload payload)
        {
            await _paymentService.UpdatePaymentStatusAsync(payload.TransactionId, "refunded", payload.Data);

            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "card_payment.refunded",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessCardDisputeAsync(CardPaymentWebhookPayload payload)
        {
            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "card_payment.dispute_created",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessSubscriptionCreatedAsync(SubscriptionWebhookPayload payload)
        {
            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "subscription.created",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessSubscriptionUpdatedAsync(SubscriptionWebhookPayload payload)
        {
            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "subscription.updated",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessSubscriptionCancelledAsync(SubscriptionWebhookPayload payload)
        {
            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "subscription.cancelled",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessSubscriptionPaymentAsync(SubscriptionWebhookPayload payload)
        {
            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "subscription.payment_succeeded",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task ProcessSubscriptionPaymentFailedAsync(SubscriptionWebhookPayload payload)
        {
            await _webhookService.TriggerWebhookEventAsync(new WebhookEvent
            {
                TenantId = payload.TenantId,
                EventType = "subscription.payment_failed",
                Data = payload,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task AcknowledgeWebhookAsync(string webhookId)
        {
            await _webhookService.AcknowledgeWebhookAsync(webhookId);
        }

        private Guid ExtractTenantIdFromPayload(object payload)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var element = System.Text.Json.JsonDocument.Parse(json).RootElement;
                
                if (element.TryGetProperty("tenantId", out var tenantIdProperty))
                {
                    return Guid.Parse(tenantIdProperty.GetString() ?? Guid.Empty.ToString());
                }

                return Guid.Empty;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        private string ComputeSignature(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    // Webhook Payload DTOs
    public class MobileMoneyWebhookPayload
    {
        public string WebhookId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public MobileMoneyData Data { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class BankTransferWebhookPayload
    {
        public string WebhookId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public BankTransferData Data { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class CardPaymentWebhookPayload
    {
        public string WebhookId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public CardPaymentData Data { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class SubscriptionWebhookPayload
    {
        public string WebhookId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public SubscriptionData Data { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class TestWebhookPayload
    {
        public string TestId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    // Data classes
    public class MobileMoneyData
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string Reference { get; set; } = string.Empty;
    }

    public class BankTransferData
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ErrorMessage { get; set; }
        public string Reference { get; set; } = string.Empty;
    }

    public class CardPaymentData
    {
        public string CardLastFour { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CardType { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string AuthorizationCode { get; set; } = string.Empty;
    }

    public class SubscriptionData
    {
        public string PlanId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime NextBillingDate { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
