using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UmiHealth.Application.Services
{
    /// <summary>
    /// Payment processing interface
    /// </summary>
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentResult> VerifyPaymentAsync(string transactionId);
        Task<bool> RefundPaymentAsync(string transactionId, decimal amount);
        Task<List<PaymentTransaction>> GetPaymentHistoryAsync(Guid tenantId, Guid saleId);
    }

    /// <summary>
    /// Payment request DTO
    /// </summary>
    public class PaymentRequest
    {
        public Guid SaleId { get; set; }
        public Guid TenantId { get; set; }
        public Guid BranchId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } // Cash, Card, MobileMoney, Cheque
        public string MobileMoneyProvider { get; set; } // MTN, Airtel, Zesco, etc.
        public string PhoneNumber { get; set; }
        public string Reference { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Payment result DTO
    /// </summary>
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string ProviderReference { get; set; }
        public string Status { get; set; } // Pending, Completed, Failed
        public decimal Amount { get; set; }
        public DateTime ProcessedAt { get; set; }
        public Dictionary<string, object> ProviderResponse { get; set; }
    }

    /// <summary>
    /// Payment transaction entity
    /// </summary>
    public class PaymentTransaction
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SaleId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Provider { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public string ProviderReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Payment service implementation
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, IMobileMoneyProvider> _providers;

        public PaymentService(
            ILogger<PaymentService> logger,
            IConfiguration configuration,
            MtnMobileMoneyProvider mtnProvider,
            AirtelMoneyProvider airtelProvider)
        {
            _logger = logger;
            _configuration = configuration;

            // Register providers
            _providers = new Dictionary<string, IMobileMoneyProvider>(StringComparer.OrdinalIgnoreCase)
            {
                { "MTN", mtnProvider },
                { "Airtel", airtelProvider }
            };
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                switch (request.PaymentMethod.ToLower())
                {
                    case "cash":
                        return ProcessCashPayment(request);

                    case "card":
                        return await ProcessCardPayment(request);

                    case "mobilemoney":
                        return await ProcessMobileMoneyPayment(request);

                    case "cheque":
                        return ProcessChequePayment(request);

                    default:
                        return new PaymentResult
                        {
                            Success = false,
                            Message = $"Unknown payment method: {request.PaymentMethod}"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Payment processing failed for sale {request.SaleId}");
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Payment processing failed: {ex.Message}",
                    Status = "Failed"
                };
            }
        }

        public async Task<PaymentResult> VerifyPaymentAsync(string transactionId)
        {
            // Implementation would verify with providers
            return await Task.FromResult(new PaymentResult { Success = true });
        }

        public async Task<bool> RefundPaymentAsync(string transactionId, decimal amount)
        {
            // Implementation would process refund
            return await Task.FromResult(true);
        }

        public async Task<List<PaymentTransaction>> GetPaymentHistoryAsync(Guid tenantId, Guid saleId)
        {
            // Implementation would fetch from database
            return await Task.FromResult(new List<PaymentTransaction>());
        }

        private PaymentResult ProcessCashPayment(PaymentRequest request)
        {
            _logger.LogInformation($"Cash payment received: {request.Amount} for sale {request.SaleId}");

            return new PaymentResult
            {
                Success = true,
                Message = "Cash payment received",
                Status = "Completed",
                Amount = request.Amount,
                TransactionId = Guid.NewGuid().ToString(),
                ProcessedAt = DateTime.UtcNow
            };
        }

        private async Task<PaymentResult> ProcessCardPayment(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Card payment processing: {request.Amount} for sale {request.SaleId}");

                // Integration with payment gateway (Stripe, PayPal, etc.)
                return await Task.FromResult(new PaymentResult
                {
                    Success = true,
                    Message = "Card payment processed",
                    Status = "Completed",
                    Amount = request.Amount,
                    TransactionId = Guid.NewGuid().ToString(),
                    ProcessedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Card payment failed");
                return new PaymentResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<PaymentResult> ProcessMobileMoneyPayment(PaymentRequest request)
        {
            if (string.IsNullOrEmpty(request.MobileMoneyProvider))
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = "Mobile money provider not specified"
                };
            }

            if (!_providers.TryGetValue(request.MobileMoneyProvider, out var provider))
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Unsupported mobile money provider: {request.MobileMoneyProvider}"
                };
            }

            return await provider.InitiatePaymentAsync(request);
        }

        private PaymentResult ProcessChequePayment(PaymentRequest request)
        {
            _logger.LogInformation($"Cheque payment recorded: {request.Amount} for sale {request.SaleId}");

            return new PaymentResult
            {
                Success = true,
                Message = "Cheque payment recorded",
                Status = "Pending",
                Amount = request.Amount,
                TransactionId = Guid.NewGuid().ToString(),
                Reference = request.Reference,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }
}
