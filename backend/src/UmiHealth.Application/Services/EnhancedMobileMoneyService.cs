using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IEnhancedMobileMoneyService
    {
        Task<UssdSessionResult> InitiateUssdPaymentAsync(UssdPaymentRequest request);
        Task<UssdSessionStatus> CheckUssdSessionStatusAsync(string sessionId);
        Task<MobileMoneyRefundResult> ProcessMobileMoneyRefundAsync(MobileMoneyRefundRequest request);
        Task<List<MobileMoneyTransactionDto>> GetMobileMoneyTransactionsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<MobileMoneyAnalyticsDto> GetMobileMoneyAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<bool> ValidateMobileMoneyNumberAsync(string phoneNumber, string provider);
        Task<MobileMoneyProviderStatus> CheckProviderStatusAsync(string provider);
        Task<byte[]> GenerateMobileMoneyReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }

    public class EnhancedMobileMoneyService : IEnhancedMobileMoneyService
    {
        private readonly ILogger<EnhancedMobileMoneyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Dictionary<string, IMobileMoneyProvider> _providers;

        public EnhancedMobileMoneyService(
            ILogger<EnhancedMobileMoneyService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _providers = InitializeProviders();
        }

        public async Task<UssdSessionResult> InitiateUssdPaymentAsync(UssdPaymentRequest request)
        {
            try
            {
                var provider = _providers.GetValueOrDefault(request.Provider.ToLower());
                if (provider == null)
                {
                    return new UssdSessionResult
                    {
                        Success = false,
                        Message = $"Provider {request.Provider} not supported"
                    };
                }

                // Generate USSD session
                var sessionId = GenerateUssdSessionId();
                var ussdCode = GetUssdCode(request.Provider);
                
                // Store session details
                var session = new UssdSession
                {
                    SessionId = sessionId,
                    Provider = request.Provider,
                    PhoneNumber = request.PhoneNumber,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Description = request.Description,
                    Status = "initiated",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };

                // Initiate mobile money payment
                var paymentRequest = new PaymentRequest
                {
                    TenantId = request.TenantId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethod = "mobile_money",
                    MobileMoneyProvider = request.Provider,
                    PhoneNumber = request.PhoneNumber,
                    Reference = request.Reference,
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    Description = request.Description
                };

                var paymentResponse = await provider.ProcessPaymentAsync(paymentRequest);

                if (paymentResponse.Success)
                {
                    session.TransactionId = paymentResponse.TransactionId;
                    session.Status = "pending";
                    session.Instructions = paymentResponse.Instructions;
                }
                else
                {
                    session.Status = "failed";
                    session.ErrorMessage = paymentResponse.Message;
                }

                // Store session (in a real implementation, this would be saved to database)
                await StoreUssdSessionAsync(session);

                return new UssdSessionResult
                {
                    Success = paymentResponse.Success,
                    SessionId = sessionId,
                    UssdCode = ussdCode,
                    Instructions = paymentResponse.Instructions,
                    TransactionId = paymentResponse.TransactionId,
                    Message = paymentResponse.Message,
                    ExpiresAt = session.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initiating USSD payment for provider {request.Provider}");
                return new UssdSessionResult
                {
                    Success = false,
                    Message = "Failed to initiate USSD payment"
                };
            }
        }

        public async Task<UssdSessionStatus> CheckUssdSessionStatusAsync(string sessionId)
        {
            try
            {
                var session = await GetUssdSessionAsync(sessionId);
                if (session == null)
                {
                    return new UssdSessionStatus
                    {
                        Success = false,
                        Message = "Session not found"
                    };
                }

                if (session.Status == "completed" || session.Status == "failed")
                {
                    return new UssdSessionStatus
                    {
                        Success = true,
                        SessionId = sessionId,
                        Status = session.Status,
                        TransactionId = session.TransactionId,
                        Amount = session.Amount,
                        CompletedAt = session.CompletedAt,
                        Message = session.Status == "completed" ? "Payment completed successfully" : session.ErrorMessage
                    };
                }

                // Check with provider if still pending
                if (!string.IsNullOrEmpty(session.TransactionId))
                {
                    var provider = _providers.GetValueOrDefault(session.Provider.ToLower());
                    if (provider != null)
                    {
                        var statusResponse = await provider.CheckPaymentStatusAsync(session.TransactionId);
                        
                        if (statusResponse.Status == "completed")
                        {
                            session.Status = "completed";
                            session.CompletedAt = DateTime.UtcNow;
                        }
                        else if (statusResponse.Status == "failed")
                        {
                            session.Status = "failed";
                            session.ErrorMessage = statusResponse.Message;
                        }

                        await UpdateUssdSessionAsync(session);
                    }
                }

                return new UssdSessionStatus
                {
                    Success = true,
                    SessionId = sessionId,
                    Status = session.Status,
                    TransactionId = session.TransactionId,
                    Amount = session.Amount,
                    CompletedAt = session.CompletedAt,
                    Message = session.Status == "pending" ? "Payment is being processed" : session.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking USSD session status for {sessionId}");
                return new UssdSessionStatus
                {
                    Success = false,
                    Message = "Failed to check session status"
                };
            }
        }

        public async Task<MobileMoneyRefundResult> ProcessMobileMoneyRefundAsync(MobileMoneyRefundRequest request)
        {
            try
            {
                var provider = _providers.GetValueOrDefault(request.Provider.ToLower());
                if (provider == null)
                {
                    return new MobileMoneyRefundResult
                    {
                        Success = false,
                        Message = $"Provider {request.Provider} not supported"
                    };
                }

                var refundRequest = new RefundRequest
                {
                    TransactionId = request.OriginalTransactionId,
                    Amount = request.Amount,
                    Reason = request.Reason,
                    RefundReference = request.RefundReference,
                    RequestedBy = request.RequestedBy,
                    Notes = request.Notes
                };

                var refundResponse = await provider.ProcessRefundAsync(refundRequest);

                // Log refund attempt
                await LogMobileMoneyRefundAsync(new MobileMoneyRefundLog
                {
                    Id = Guid.NewGuid(),
                    OriginalTransactionId = request.OriginalTransactionId,
                    RefundId = refundResponse.RefundId,
                    Provider = request.Provider,
                    Amount = request.Amount,
                    Reason = request.Reason,
                    Status = refundResponse.Success ? "processed" : "failed",
                    ProcessedAt = DateTime.UtcNow,
                    RequestedBy = request.RequestedBy,
                    ErrorMessage = refundResponse.Success ? null : refundResponse.Message
                });

                return new MobileMoneyRefundResult
                {
                    Success = refundResponse.Success,
                    RefundId = refundResponse.RefundId,
                    Amount = request.Amount,
                    Message = refundResponse.Message,
                    ProcessedAt = refundResponse.ProcessedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing mobile money refund for {request.Provider}");
                return new MobileMoneyRefundResult
                {
                    Success = false,
                    Message = "Failed to process refund"
                };
            }
        }

        public async Task<List<MobileMoneyTransactionDto>> GetMobileMoneyTransactionsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            // In a real implementation, this would query the database
            // For now, return mock data
            return new List<MobileMoneyTransactionDto>
            {
                new MobileMoneyTransactionDto
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Provider = "mtn",
                    PhoneNumber = "+260123456789",
                    Amount = 150.00m,
                    Currency = "ZMW",
                    Status = "completed",
                    TransactionDate = DateTime.UtcNow.AddHours(-2),
                    CustomerName = "Moses Mushibi",
                    Reference = "MM001"
                },
                new MobileMoneyTransactionDto
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Provider = "airtel",
                    PhoneNumber = "+260987654321",
                    Amount = 75.50m,
                    Currency = "ZMW",
                    Status = "pending",
                    TransactionDate = DateTime.UtcNow.AddHours(-1),
                    CustomerName = "Jane Smith",
                    Reference = "MM002"
                }
            };
        }

        public async Task<MobileMoneyAnalyticsDto> GetMobileMoneyAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await GetMobileMoneyTransactionsAsync(tenantId, startDate, endDate);
            
            var totalAmount = transactions.Where(t => t.Status == "completed").Sum(t => t.Amount);
            var totalTransactions = transactions.Count(t => t.Status == "completed");
            var successfulTransactions = transactions.Count(t => t.Status == "completed");
            var pendingTransactions = transactions.Count(t => t.Status == "pending");
            var failedTransactions = transactions.Count(t => t.Status == "failed");

            return new MobileMoneyAnalyticsDto
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalAmount = totalAmount,
                TotalTransactions = totalTransactions,
                SuccessfulTransactions = successfulTransactions,
                PendingTransactions = pendingTransactions,
                FailedTransactions = failedTransactions,
                SuccessRate = totalTransactions > 0 ? (double)successfulTransactions / totalTransactions * 100 : 0,
                AverageTransactionValue = successfulTransactions > 0 ? totalAmount / successfulTransactions : 0,
                ProviderBreakdown = transactions
                    .Where(t => t.Status == "completed")
                    .GroupBy(t => t.Provider)
                    .Select(g => new ProviderAnalytics
                    {
                        Provider = g.Key,
                        TransactionCount = g.Count(),
                        TotalAmount = g.Sum(t => t.Amount),
                        AverageAmount = g.Average(t => t.Amount),
                        SuccessRate = 100.0 // All completed transactions in this group
                    })
                    .ToList()
            };
        }

        public async Task<bool> ValidateMobileMoneyNumberAsync(string phoneNumber, string provider)
        {
            try
            {
                // Basic validation based on provider patterns
                return provider.ToLower() switch
                {
                    "mtn" => phoneNumber.StartsWith("+2609") || phoneNumber.StartsWith("09"),
                    "airtel" => phoneNumber.StartsWith("+2609") || phoneNumber.StartsWith("09"),
                    "zamtel" => phoneNumber.StartsWith("+2609") || phoneNumber.StartsWith("09"),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating mobile money number for {provider}");
                return false;
            }
        }

        public async Task<MobileMoneyProviderStatus> CheckProviderStatusAsync(string provider)
        {
            try
            {
                var providerInstance = _providers.GetValueOrDefault(provider.ToLower());
                if (providerInstance == null)
                {
                    return new MobileMoneyProviderStatus
                    {
                        Provider = provider,
                        IsOnline = false,
                        Message = "Provider not supported"
                    };
                }

                // In a real implementation, this would check the provider's health endpoint
                // For now, assume all providers are online
                return new MobileMoneyProviderStatus
                {
                    Provider = provider,
                    IsOnline = true,
                    Message = "Provider is operational",
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = 150 // milliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking provider status for {provider}");
                return new MobileMoneyProviderStatus
                {
                    Provider = provider,
                    IsOnline = false,
                    Message = "Unable to check provider status",
                    LastChecked = DateTime.UtcNow
                };
            }
        }

        public async Task<byte[]> GenerateMobileMoneyReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await GetMobileMoneyTransactionsAsync(tenantId, startDate, endDate);
            var analytics = await GetMobileMoneyAnalyticsAsync(tenantId, startDate, endDate);

            var csv = new StringBuilder();
            csv.AppendLine("MOBILE MONEY REPORT");
            csv.AppendLine($"Tenant ID: {tenantId}");
            csv.AppendLine($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}");
            csv.AppendLine($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm:ss}");
            csv.AppendLine();
            
            csv.AppendLine("SUMMARY");
            csv.AppendLine($"Total Amount: {analytics.TotalAmount:C}");
            csv.AppendLine($"Total Transactions: {analytics.TotalTransactions:N0}");
            csv.AppendLine($"Success Rate: {analytics.SuccessRate:F1}%");
            csv.AppendLine($"Average Transaction Value: {analytics.AverageTransactionValue:C}");
            csv.AppendLine();

            csv.AppendLine("TRANSACTIONS");
            csv.AppendLine("Transaction ID,Provider,Phone Number,Amount,Status,Date,Customer Name,Reference");
            
            foreach (var transaction in transactions)
            {
                csv.AppendLine($"{transaction.TransactionId},{transaction.Provider},{transaction.PhoneNumber},{transaction.Amount},{transaction.Status},{transaction.TransactionDate:yyyy-MM-dd HH:mm:ss},{transaction.CustomerName},{transaction.Reference}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        #region Helper Methods

        private Dictionary<string, IMobileMoneyProvider> InitializeProviders()
        {
            var providers = new Dictionary<string, IMobileMoneyProvider>();
            
            // Initialize providers with configuration
            var mtnApiKey = _configuration["MobileMoney:MTN:ApiKey"] ?? "";
            var mtnBaseUrl = _configuration["MobileMoney:MTN:BaseUrl"] ?? "https://api.mtn.com";
            
            var airtelApiKey = _configuration["MobileMoney:Airtel:ApiKey"] ?? "";
            var airtelBaseUrl = _configuration["MobileMoney:Airtel:BaseUrl"] ?? "https://api.airtel.com";
            
            var zamtelApiKey = _configuration["MobileMoney:Zamtel:ApiKey"] ?? "";
            var zamtelBaseUrl = _configuration["MobileMoney:Zamtel:BaseUrl"] ?? "https://api.zamtel.com";

            providers["mtn"] = new MtnMobileMoneyProvider(mtnApiKey, mtnBaseUrl, _logger, _httpClientFactory);
            providers["airtel"] = new AirtelMoneyProvider(airtelApiKey, airtelBaseUrl, _logger, _httpClientFactory);
            providers["zamtel"] = new ZamtelMobileMoneyProvider(zamtelApiKey, zamtelBaseUrl, _logger, _httpClientFactory);

            return providers;
        }

        private string GenerateUssdSessionId()
        {
            return $"USSD_{DateTime.UtcNow:yyyyMMddHHmmss}_{new Random().Next(1000, 9999)}";
        }

        private string GetUssdCode(string provider)
        {
            return provider.ToLower() switch
            {
                "mtn" => "*303#",
                "airtel" => "*778#",
                "zamtel" => "*444#",
                _ => "#"
            };
        }

        private Task StoreUssdSessionAsync(UssdSession session)
        {
            // In a real implementation, this would save to database
            _logger.LogInformation($"Storing USSD session {session.SessionId} for provider {session.Provider}");
            return Task.CompletedTask;
        }

        private Task<UssdSession?> GetUssdSessionAsync(string sessionId)
        {
            // In a real implementation, this would retrieve from database
            return Task.FromResult<UssdSession?>(new UssdSession
            {
                SessionId = sessionId,
                Provider = "mtn",
                PhoneNumber = "+260123456789",
                Amount = 150.00m,
                Status = "pending",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });
        }

        private Task UpdateUssdSessionAsync(UssdSession session)
        {
            // In a real implementation, this would update in database
            _logger.LogInformation($"Updating USSD session {session.SessionId} to status {session.Status}");
            return Task.CompletedTask;
        }

        private Task LogMobileMoneyRefundAsync(MobileMoneyRefundLog log)
        {
            // In a real implementation, this would save to database
            _logger.LogInformation($"Logging mobile money refund {log.RefundId} for transaction {log.OriginalTransactionId}");
            return Task.CompletedTask;
        }

        #endregion
    }

    // Supporting DTOs and Classes
    public class UssdPaymentRequest
    {
        public Guid TenantId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
    }

    public class UssdSessionResult
    {
        public bool Success { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string UssdCode { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }

    public class UssdSessionStatus
    {
        public bool Success { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // initiated, pending, completed, failed, expired
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MobileMoneyRefundRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string OriginalTransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RefundReference { get; set; } = string.Empty;
        public Guid RequestedBy { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class MobileMoneyRefundResult
    {
        public bool Success { get; set; }
        public string RefundId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }

    public class MobileMoneyTransactionDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    public class MobileMoneyAnalyticsDto
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public double SuccessRate { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public List<ProviderAnalytics> ProviderBreakdown { get; set; } = new();
    }

    public class ProviderAnalytics
    {
        public string Provider { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public double SuccessRate { get; set; }
    }

    public class MobileMoneyProviderStatus
    {
        public string Provider { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public int ResponseTime { get; set; } // in milliseconds
    }

    // Internal classes for session management
    internal class UssdSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    internal class MobileMoneyRefundLog
    {
        public Guid Id { get; set; }
        public string OriginalTransactionId { get; set; } = string.Empty;
        public string RefundId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public Guid RequestedBy { get; set; }
        public string ErrorMessage { get; set; }
    }
}
