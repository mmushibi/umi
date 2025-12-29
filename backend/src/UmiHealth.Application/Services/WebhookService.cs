using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IWebhookService
    {
        Task<string> GetWebhookSecretAsync(Guid tenantId);
        Task<bool> RegisterWebhookEndpointAsync(WebhookRegistration registration);
        Task<List<WebhookEndpoint>> GetWebhookEndpointsAsync(Guid tenantId);
        Task<bool> UnregisterWebhookEndpointAsync(Guid endpointId, Guid tenantId);
        Task TriggerWebhookEventAsync(WebhookEvent webhookEvent);
        Task AcknowledgeWebhookAsync(string webhookId);
        Task<WebhookDeliveryReport> GetDeliveryReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<List<WebhookRetryLog>> GetRetryLogsAsync(Guid tenantId, DateTime? startDate = null);
        Task<bool> TestWebhookEndpointAsync(WebhookTestRequest testRequest);
    }

    public class WebhookService : IWebhookService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<WebhookService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WebhookService(
            SharedDbContext context,
            ILogger<WebhookService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetWebhookSecretAsync(Guid tenantId)
        {
            var webhookConfig = await _context.WebhookConfigurations
                .FirstOrDefaultAsync(wc => wc.TenantId == tenantId && wc.IsActive);

            return webhookConfig?.Secret ?? string.Empty;
        }

        public async Task<bool> RegisterWebhookEndpointAsync(WebhookRegistration registration)
        {
            try
            {
                var existingEndpoint = await _context.WebhookEndpoints
                    .FirstOrDefaultAsync(we => we.TenantId == registration.TenantId && 
                                             we.Url == registration.Url && 
                                             we.EventType == registration.EventType);

                if (existingEndpoint != null)
                {
                    // Update existing endpoint
                    existingEndpoint.IsActive = registration.IsActive;
                    existingEndpoint.Secret = registration.Secret;
                    existingEndpoint.Headers = registration.Headers;
                    existingEndpoint.RetryPolicy = registration.RetryPolicy;
                    existingEndpoint.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new endpoint
                    existingEndpoint = new WebhookEndpoint
                    {
                        Id = Guid.NewGuid(),
                        TenantId = registration.TenantId,
                        Url = registration.Url,
                        EventType = registration.EventType,
                        Secret = registration.Secret,
                        Headers = registration.Headers,
                        RetryPolicy = registration.RetryPolicy,
                        IsActive = registration.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.WebhookEndpoints.Add(existingEndpoint);
                }

                await _context.SaveChangesAsync();

                // Test the webhook endpoint
                if (registration.IsActive)
                {
                    await TestWebhookEndpointAsync(new WebhookTestRequest
                    {
                        TenantId = registration.TenantId,
                        EndpointId = existingEndpoint.Id,
                        Url = registration.Url,
                        Secret = registration.Secret
                    });
                }

                _logger.LogInformation("Webhook endpoint registered: {EndpointId} for tenant {TenantId}", 
                    existingEndpoint.Id, registration.TenantId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register webhook endpoint for tenant {TenantId}", registration.TenantId);
                return false;
            }
        }

        public async Task<List<WebhookEndpoint>> GetWebhookEndpointsAsync(Guid tenantId)
        {
            return await _context.WebhookEndpoints
                .Where(we => we.TenantId)
                .OrderBy(we => we.EventType)
                .ThenBy(we => we.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UnregisterWebhookEndpointAsync(Guid endpointId, Guid tenantId)
        {
            try
            {
                var endpoint = await _context.WebhookEndpoints
                    .FirstOrDefaultAsync(we => we.Id == endpointId && we.TenantId == tenantId);

                if (endpoint != null)
                {
                    endpoint.IsActive = false;
                    endpoint.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Webhook endpoint unregistered: {EndpointId}", endpointId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister webhook endpoint {EndpointId}", endpointId);
                return false;
            }
        }

        public async Task TriggerWebhookEventAsync(WebhookEvent webhookEvent)
        {
            try
            {
                // Get active webhook endpoints for this event type
                var endpoints = await _context.WebhookEndpoints
                    .Where(we => we.TenantId == webhookEvent.TenantId && 
                                   we.EventType == webhookEvent.EventType && 
                                   we.IsActive)
                    .ToListAsync();

                if (!endpoints.Any())
                {
                    _logger.LogWarning("No active webhook endpoints found for event {EventType} in tenant {TenantId}", 
                        webhookEvent.EventType, webhookEvent.TenantId);
                    return;
                }

                // Trigger webhook for each endpoint
                var tasks = endpoints.Select(endpoint => 
                    DeliverWebhookAsync(endpoint, webhookEvent));

                await Task.WhenAll(tasks);

                _logger.LogInformation("Webhook event {EventType} triggered for tenant {TenantId} with {EndpointCount} endpoints", 
                    webhookEvent.EventType, webhookEvent.TenantId, endpoints.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger webhook event {EventType} for tenant {TenantId}", 
                    webhookEvent.EventType, webhookEvent.TenantId);
            }
        }

        public async Task AcknowledgeWebhookAsync(string webhookId)
        {
            try
            {
                var delivery = await _context.WebhookDeliveries
                    .FirstOrDefaultAsync(wd => wd.WebhookId == webhookId);

                if (delivery != null)
                {
                    delivery.AcknowledgedAt = DateTime.UtcNow;
                    delivery.Status = "acknowledged";
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acknowledge webhook {WebhookId}", webhookId);
            }
        }

        public async Task<WebhookDeliveryReport> GetDeliveryReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var deliveries = await _context.WebhookDeliveries
                .Where(wd => wd.TenantId == tenantId && 
                           wd.CreatedAt >= startDate && 
                           wd.CreatedAt <= endDate)
                .Include(wd => wd.Endpoint)
                .ToListAsync();

            var totalDeliveries = deliveries.Count;
            var successfulDeliveries = deliveries.Count(d => d.Status == "delivered");
            var failedDeliveries = deliveries.Count(d => d.Status == "failed");
            var pendingDeliveries = deliveries.Count(d => d.Status == "pending");

            var deliveriesByStatus = deliveries
                .GroupBy(d => d.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var deliveriesByEndpoint = deliveries
                .GroupBy(d => d.Endpoint.EventType)
                .Select(g => new EndpointDeliveryStats
                {
                    EventType = g.Key,
                    TotalDeliveries = g.Count(),
                    SuccessfulDeliveries = g.Count(d => d.Status == "delivered"),
                    FailedDeliveries = g.Count(d => d.Status == "failed"),
                    AverageDeliveryTime = g.Where(d => d.DeliveredAt.HasValue)
                        .Average(d => (d.DeliveredAt!.Value - d.CreatedAt).TotalMilliseconds)
                })
                .ToList();

            return new WebhookDeliveryReport
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalDeliveries = totalDeliveries,
                SuccessfulDeliveries = successfulDeliveries,
                FailedDeliveries = failedDeliveries,
                PendingDeliveries = pendingDeliveries,
                SuccessRate = totalDeliveries > 0 ? (double)successfulDeliveries / totalDeliveries * 100 : 0,
                DeliveriesByStatus = deliveriesByStatus,
                DeliveriesByEndpoint = deliveriesByEndpoint
            };
        }

        public async Task<List<WebhookRetryLog>> GetRetryLogsAsync(Guid tenantId, DateTime? startDate = null)
        {
            var query = _context.WebhookRetryLogs
                .Where(wrl => wrl.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(wrl => wrl.CreatedAt >= startDate.Value);

            return await query
                .OrderByDescending(wrl => wrl.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<bool> TestWebhookEndpointAsync(WebhookTestRequest testRequest)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                
                var testPayload = new
                {
                    testId = Guid.NewGuid().ToString(),
                    source = "UmiHealth Webhook Test",
                    timestamp = DateTime.UtcNow,
                    data = new
                    {
                        message = "This is a test webhook to verify your endpoint is working correctly",
                        tenantId = testRequest.TenantId,
                        endpointId = testRequest.EndpointId
                    }
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(testPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Add signature header
                var signature = ComputeSignature(jsonPayload, testRequest.Secret);
                content.Headers.Add("X-Webhook-Signature", signature);
                content.Headers.Add("X-Webhook-Event", "test");

                var response = await httpClient.PostAsync(testRequest.Url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook test successful for endpoint {EndpointId}", testRequest.EndpointId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Webhook test failed for endpoint {EndpointId}: {StatusCode}", 
                        testRequest.EndpointId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook test failed for endpoint {EndpointId}", testRequest.EndpointId);
                return false;
            }
        }

        private async Task DeliverWebhookAsync(WebhookEndpoint endpoint, WebhookEvent webhookEvent)
        {
            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                TenantId = webhookEvent.TenantId,
                EndpointId = endpoint.Id,
                WebhookId = Guid.NewGuid().ToString(),
                EventType = webhookEvent.EventType,
                Payload = System.Text.Json.JsonSerializer.Serialize(webhookEvent.Data),
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            _context.WebhookDeliveries.Add(delivery);
            await _context.SaveChangesAsync();

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(webhookEvent.Data);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Add headers
                content.Headers.Add("X-Webhook-Event", webhookEvent.EventType);
                content.Headers.Add("X-Webhook-Id", delivery.WebhookId);
                
                if (!string.IsNullOrEmpty(endpoint.Secret))
                {
                    var signature = ComputeSignature(jsonPayload, endpoint.Secret);
                    content.Headers.Add("X-Webhook-Signature", signature);
                }

                // Add custom headers
                if (!string.IsNullOrEmpty(endpoint.Headers))
                {
                    var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(endpoint.Headers);
                    foreach (var header in headers)
                    {
                        content.Headers.Add(header.Key, header.Value);
                    }
                }

                var response = await httpClient.PostAsync(endpoint.Url, content);

                if (response.IsSuccessStatusCode)
                {
                    delivery.Status = "delivered";
                    delivery.DeliveredAt = DateTime.UtcNow;
                    delivery.ResponseCode = ((int)response.StatusCode).ToString();
                }
                else
                {
                    delivery.Status = "failed";
                    delivery.ResponseCode = ((int)response.StatusCode).ToString();
                    delivery.ErrorMessage = response.ReasonPhrase;
                    
                    // Schedule retry if configured
                    if (endpoint.RetryPolicy?.Enabled == true)
                    {
                        await ScheduleWebhookRetryAsync(delivery, endpoint.RetryPolicy);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                delivery.Status = "failed";
                delivery.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
                
                _logger.LogError(ex, "Failed to deliver webhook {WebhookId} to endpoint {EndpointId}", 
                    delivery.WebhookId, endpoint.Id);
            }
        }

        private async Task ScheduleWebhookRetryAsync(WebhookDelivery delivery, WebhookRetryPolicy retryPolicy)
        {
            var retryLog = new WebhookRetryLog
            {
                Id = Guid.NewGuid(),
                TenantId = delivery.TenantId,
                DeliveryId = delivery.Id,
                RetryAttempt = delivery.RetryCount + 1,
                ScheduledAt = CalculateNextRetryTime(delivery, retryPolicy),
                CreatedAt = DateTime.UtcNow
            };

            _context.WebhookRetryLogs.Add(retryLog);
            await _context.SaveChangesAsync();
        }

        private DateTime CalculateNextRetryTime(WebhookDelivery delivery, WebhookRetryPolicy retryPolicy)
        {
            var delayMinutes = retryPolicy.InitialDelayMinutes;
            
            // Exponential backoff
            for (int i = 0; i < delivery.RetryCount; i++)
            {
                delayMinutes = (int)(delayMinutes * Math.Pow(retryPolicy.BackoffMultiplier, i));
            }

            // Cap at maximum delay
            delayMinutes = Math.Min(delayMinutes, retryPolicy.MaxDelayMinutes);

            return DateTime.UtcNow.AddMinutes(delayMinutes);
        }

        private string ComputeSignature(string payload, string secret)
        {
            using var hmac = System.Security.Cryptography.HMACSHA256.Create();
            hmac.Key = Encoding.UTF8.GetBytes(secret);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    // Supporting DTOs and Entities
    public class WebhookRegistration
    {
        public Guid TenantId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public WebhookRetryPolicy RetryPolicy { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class WebhookTestRequest
    {
        public Guid TenantId { get; set; }
        public Guid EndpointId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
    }

    public class WebhookEvent
    {
        public Guid TenantId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public object Data { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class WebhookDeliveryReport
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public int PendingDeliveries { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, int> DeliveriesByStatus { get; set; } = new();
        public List<EndpointDeliveryStats> DeliveriesByEndpoint { get; set; } = new();
    }

    public class EndpointDeliveryStats
    {
        public string EventType { get; set; } = string.Empty;
        public int TotalDeliveries { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public int FailedDeliveries { get; set; }
        public double AverageDeliveryTime { get; set; }
    }

    // Entity classes
    public class WebhookEndpoint : TenantEntity
    {
        public string Url { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public WebhookRetryPolicy RetryPolicy { get; set; } = new();
        public bool IsActive { get; set; } = true;

        public virtual List<WebhookDelivery> Deliveries { get; set; } = new();
    }

    public class WebhookDelivery : TenantEntity
    {
        public Guid EndpointId { get; set; }
        public string WebhookId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, delivered, failed
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string ResponseCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }

        public virtual WebhookEndpoint Endpoint { get; set; } = null!;
    }

    public class WebhookRetryLog : TenantEntity
    {
        public Guid DeliveryId { get; set; }
        public int RetryAttempt { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string Status { get; set; } = string.Empty; // scheduled, processed, failed
        public string ErrorMessage { get; set; } = string.Empty;

        public virtual WebhookDelivery Delivery { get; set; } = null!;
    }

    public class WebhookRetryPolicy
    {
        public bool Enabled { get; set; } = true;
        public int InitialDelayMinutes { get; set; } = 5;
        public double BackoffMultiplier { get; set; } = 2.0;
        public int MaxDelayMinutes { get; set; } = 60;
        public int MaxRetries { get; set; } = 5;
    }

    public class WebhookConfiguration : TenantEntity
    {
        public string Secret { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? LastRotatedAt { get; set; }
    }
}
