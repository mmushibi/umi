using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IPaymentProcessingService
    {
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentStatusResponse> CheckPaymentStatusAsync(string transactionId);
        Task<RefundResponse> ProcessRefundAsync(RefundRequest request);
        Task<List<PaymentMethodDto>> GetAvailablePaymentMethodsAsync(Guid tenantId);
        Task<bool> ValidatePaymentAsync(string transactionId, decimal amount);
        Task<MobileMoneyResponse> InitiateMobileMoneyPaymentAsync(MobileMoneyRequest request);
    }

    public class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentProcessingService> _logger;
        private readonly Dictionary<string, IMobileMoneyProvider> _mobileMoneyProviders;

        public PaymentProcessingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PaymentProcessingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _mobileMoneyProviders = InitializeMobileMoneyProviders();
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing payment for tenant {TenantId}, amount {Amount}", request.TenantId, request.Amount);

                switch (request.PaymentMethod.ToLower())
                {
                    case "cash":
                        return await ProcessCashPaymentAsync(request);
                    case "card":
                        return await ProcessCardPaymentAsync(request);
                    case "mobile_money":
                    case "mobile":
                        return await ProcessMobileMoneyPaymentAsync(request);
                    case "bank_transfer":
                        return await ProcessBankTransferAsync(request);
                    case "insurance":
                        return await ProcessInsurancePaymentAsync(request);
                    default:
                        return new PaymentResponse
                        {
                            Success = false,
                            Message = $"Unsupported payment method: {request.PaymentMethod}",
                            TransactionId = null
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for tenant {TenantId}", request.TenantId);
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Payment processing failed",
                    TransactionId = null
                };
            }
        }

        private async Task<PaymentResponse> ProcessCashPaymentAsync(PaymentRequest request)
        {
            // Cash payments are immediately successful
            return new PaymentResponse
            {
                Success = true,
                Message = "Cash payment processed successfully",
                TransactionId = GenerateTransactionId("CASH"),
                ProcessedAt = DateTime.UtcNow,
                Amount = request.Amount,
                PaymentMethod = "cash"
            };
        }

        private async Task<PaymentResponse> ProcessCardPaymentAsync(PaymentRequest request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var paymentGateway = _configuration["PaymentGateways:Card:Provider"];
                var apiKey = _configuration["PaymentGateways:Card:ApiKey"];
                var baseUrl = _configuration["PaymentGateways:Card:BaseUrl"];

                var cardRequest = new
                {
                    amount = request.Amount,
                    currency = request.Currency ?? "ZMW",
                    card_number = request.CardDetails?.CardNumber,
                    expiry_month = request.CardDetails?.ExpiryMonth,
                    expiry_year = request.CardDetails?.ExpiryYear,
                    cvv = request.CardDetails?.CVV,
                    holder_name = request.CardDetails?.HolderName,
                    reference = request.Reference,
                    description = $"Payment for sale {request.SaleId}"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(cardRequest),
                    System.Text.Encoding.UTF8,
                    "application/json");

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await httpClient.PostAsync($"{baseUrl}/payments", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<CardPaymentResponse>(responseContent);
                    return new PaymentResponse
                    {
                        Success = result.Status == "approved",
                        Message = result.Message,
                        TransactionId = result.TransactionId,
                        ProcessedAt = DateTime.UtcNow,
                        Amount = request.Amount,
                        PaymentMethod = "card",
                        GatewayResponse = result
                    };
                }
                else
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Card payment failed",
                        TransactionId = null
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing card payment");
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Card payment processing error",
                    TransactionId = null
                };
            }
        }

        private async Task<PaymentResponse> ProcessMobileMoneyPaymentAsync(PaymentRequest request)
        {
            if (string.IsNullOrEmpty(request.MobileMoneyProvider))
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Mobile money provider is required",
                    TransactionId = null
                };
            }

            if (!_mobileMoneyProviders.ContainsKey(request.MobileMoneyProvider.ToLower()))
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = $"Unsupported mobile money provider: {request.MobileMoneyProvider}",
                    TransactionId = null
                };
            }

            var provider = _mobileMoneyProviders[request.MobileMoneyProvider.ToLower()];
            return await provider.ProcessPaymentAsync(request);
        }

        private async Task<PaymentResponse> ProcessBankTransferAsync(PaymentRequest request)
        {
            // Bank transfers are initiated and require manual confirmation
            return new PaymentResponse
            {
                Success = true,
                Message = "Bank transfer initiated successfully",
                TransactionId = GenerateTransactionId("BANK"),
                ProcessedAt = DateTime.UtcNow,
                Amount = request.Amount,
                PaymentMethod = "bank_transfer",
                Status = "pending",
                RequiresConfirmation = true
            };
        }

        private async Task<PaymentResponse> ProcessInsurancePaymentAsync(PaymentRequest request)
        {
            // Insurance payments require validation
            if (string.IsNullOrEmpty(request.InuranceProvider) || string.IsNullOrEmpty(request.InsurancePolicyNumber))
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Insurance provider and policy number are required",
                    TransactionId = null
                };
            }

            // Validate insurance coverage (simplified)
            var isValid = await ValidateInsuranceCoverageAsync(request.InsuranceProvider, request.InsurancePolicyNumber, request.Amount);
            
            if (!isValid)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Insurance coverage validation failed",
                    TransactionId = null
                };
            }

            return new PaymentResponse
            {
                Success = true,
                Message = "Insurance payment processed successfully",
                TransactionId = GenerateTransactionId("INS"),
                ProcessedAt = DateTime.UtcNow,
                Amount = request.Amount,
                PaymentMethod = "insurance",
                InsuranceProvider = request.InsuranceProvider,
                InsurancePolicyNumber = request.InsurancePolicyNumber
            };
        }

        public async Task<PaymentStatusResponse> CheckPaymentStatusAsync(string transactionId)
        {
            try
            {
                _logger.LogInformation("Checking payment status for transaction {TransactionId}", transactionId);

                // For cash payments, always return completed
                if (transactionId.StartsWith("CASH_"))
                {
                    return new PaymentStatusResponse
                    {
                        TransactionId = transactionId,
                        Status = "completed",
                        Message = "Cash payment completed",
                        LastUpdated = DateTime.UtcNow
                    };
                }

                // For mobile money payments, check with provider
                if (transactionId.Contains("MM_"))
                {
                    var providerName = ExtractProviderFromTransactionId(transactionId);
                    if (_mobileMoneyProviders.ContainsKey(providerName))
                    {
                        var provider = _mobileMoneyProviders[providerName];
                        return await provider.CheckPaymentStatusAsync(transactionId);
                    }
                }

                // For other payment methods, return a default status
                return new PaymentStatusResponse
                {
                    TransactionId = transactionId,
                    Status = "unknown",
                    Message = "Payment status not available",
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for transaction {TransactionId}", transactionId);
                return new PaymentStatusResponse
                {
                    TransactionId = transactionId,
                    Status = "error",
                    Message = "Error checking payment status",
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public async Task<RefundResponse> ProcessRefundAsync(RefundRequest request)
        {
            try
            {
                _logger.LogInformation("Processing refund for transaction {TransactionId}, amount {Amount}", request.TransactionId, request.Amount);

                // Validate the original payment
                var originalPayment = await CheckPaymentStatusAsync(request.TransactionId);
                if (originalPayment.Status != "completed")
                {
                    return new RefundResponse
                    {
                        Success = false,
                        Message = "Cannot refund incomplete payment",
                        RefundId = null
                    };
                }

                // Process refund based on payment method
                if (request.TransactionId.StartsWith("CASH_"))
                {
                    return await ProcessCashRefundAsync(request);
                }
                else if (request.TransactionId.Contains("MM_"))
                {
                    var providerName = ExtractProviderFromTransactionId(request.TransactionId);
                    if (_mobileMoneyProviders.ContainsKey(providerName))
                    {
                        var provider = _mobileMoneyProviders[providerName];
                        return await provider.ProcessRefundAsync(request);
                    }
                }

                return new RefundResponse
                {
                    Success = false,
                    Message = "Refund not supported for this payment method",
                    RefundId = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", request.TransactionId);
                return new RefundResponse
                {
                    Success = false,
                    Message = "Refund processing failed",
                    RefundId = null
                };
            }
        }

        private async Task<RefundResponse> ProcessCashRefundAsync(RefundRequest request)
        {
            return new RefundResponse
            {
                Success = true,
                Message = "Cash refund processed successfully",
                RefundId = GenerateTransactionId("REFUND"),
                ProcessedAt = DateTime.UtcNow,
                Amount = request.Amount
            };
        }

        public async Task<List<PaymentMethodDto>> GetAvailablePaymentMethodsAsync(Guid tenantId)
        {
            var methods = new List<PaymentMethodDto>
            {
                new PaymentMethodDto { Code = "cash", Name = "Cash", IsEnabled = true, Icon = "cash-icon" },
                new PaymentMethodDto { Code = "card", Name = "Credit/Debit Card", IsEnabled = true, Icon = "card-icon" },
                new PaymentMethodDto { Code = "bank_transfer", Name = "Bank Transfer", IsEnabled = true, Icon = "bank-icon" },
                new PaymentMethodDto { Code = "insurance", Name = "Insurance", IsEnabled = true, Icon = "insurance-icon" }
            };

            // Add mobile money providers
            foreach (var provider in _mobileMoneyProviders)
            {
                methods.Add(new PaymentMethodDto
                {
                    Code = provider.Value.ProviderCode,
                    Name = provider.Value.ProviderName,
                    IsEnabled = provider.Value.IsEnabled,
                    Icon = provider.Value.Icon,
                    IsMobileMoney = true
                });
            }

            return methods;
        }

        public async Task<bool> ValidatePaymentAsync(string transactionId, decimal amount)
        {
            try
            {
                var status = await CheckPaymentStatusAsync(transactionId);
                return status.Status == "completed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment {TransactionId}", transactionId);
                return false;
            }
        }

        public async Task<MobileMoneyResponse> InitiateMobileMoneyPaymentAsync(MobileMoneyRequest request)
        {
            try
            {
                if (!_mobileMoneyProviders.ContainsKey(request.Provider.ToLower()))
                {
                    return new MobileMoneyResponse
                    {
                        Success = false,
                        Message = $"Unsupported mobile money provider: {request.Provider}",
                        TransactionId = null
                    };
                }

                var provider = _mobileMoneyProviders[request.Provider.ToLower()];
                var paymentRequest = new PaymentRequest
                {
                    TenantId = request.TenantId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    MobileMoneyProvider = request.Provider,
                    PhoneNumber = request.PhoneNumber,
                    Reference = request.Reference,
                    CustomerEmail = request.CustomerEmail,
                    CustomerName = request.CustomerName
                };

                var result = await provider.ProcessPaymentAsync(paymentRequest);
                
                return new MobileMoneyResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    TransactionId = result.TransactionId,
                    Provider = request.Provider,
                    PhoneNumber = request.PhoneNumber,
                    Amount = request.Amount,
                    Status = result.Status,
                    Instructions = result.Instructions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating mobile money payment with provider {Provider}", request.Provider);
                return new MobileMoneyResponse
                {
                    Success = false,
                    Message = "Mobile money payment initiation failed",
                    TransactionId = null
                };
            }
        }

        private Dictionary<string, IMobileMoneyProvider> InitializeMobileMoneyProviders()
        {
            var providers = new Dictionary<string, IMobileMoneyProvider>();

            // Initialize MTN Mobile Money
            if (_configuration.GetValue<bool>("MobileMoney:MTN:Enabled"))
            {
                providers.Add("mtn", new MtnMobileMoneyProvider(
                    _configuration["MobileMoney:MTN:ApiKey"],
                    _configuration["MobileMoney:MTN:BaseUrl"],
                    _logger));
            }

            // Initialize Airtel Money
            if (_configuration.GetValue<bool>("MobileMoney:Airtel:Enabled"))
            {
                providers.Add("airtel", new AirtelMoneyProvider(
                    _configuration["MobileMoney:Airtel:ApiKey"],
                    _configuration["MobileMoney:Airtel:BaseUrl"],
                    _logger));
            }

            // Initialize Zamtel Mobile Money
            if (_configuration.GetValue<bool>("MobileMoney:Zamtel:Enabled"))
            {
                providers.Add("zamtel", new ZamtelMobileMoneyProvider(
                    _configuration["MobileMoney:Zamtel:ApiKey"],
                    _configuration["MobileMoney:Zamtel:BaseUrl"],
                    _logger));
            }

            return providers;
        }

        private string GenerateTransactionId(string prefix)
        {
            return $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        private string ExtractProviderFromTransactionId(string transactionId)
        {
            // Extract provider code from transaction ID format: MM_PROVIDER_timestamp_random
            if (transactionId.StartsWith("MM_"))
            {
                var parts = transactionId.Split('_');
                if (parts.Length >= 2)
                {
                    return parts[1].ToLower();
                }
            }
            return "unknown";
        }

        private async Task<bool> ValidateInsuranceCoverageAsync(string provider, string policyNumber, decimal amount)
        {
            // Simplified insurance validation - in real implementation, this would call insurance provider APIs
            await Task.Delay(100); // Simulate API call
            return true; // Assume valid for now
        }
    }
}
