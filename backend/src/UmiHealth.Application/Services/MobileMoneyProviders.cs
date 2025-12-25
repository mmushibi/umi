using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IMobileMoneyProvider
    {
        string ProviderCode { get; }
        string ProviderName { get; }
        bool IsEnabled { get; }
        string Icon { get; }
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentStatusResponse> CheckPaymentStatusAsync(string transactionId);
        Task<RefundResponse> ProcessRefundAsync(RefundRequest request);
    }

    public abstract class BaseMobileMoneyProvider : IMobileMoneyProvider
    {
        protected readonly string _apiKey;
        protected readonly string _baseUrl;
        protected readonly ILogger _logger;
        protected readonly IHttpClientFactory _httpClientFactory;

        protected BaseMobileMoneyProvider(
            string apiKey,
            string baseUrl,
            ILogger logger,
            IHttpClientFactory httpClientFactory)
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public abstract string ProviderCode { get; }
        public abstract string ProviderName { get; }
        public abstract bool IsEnabled { get; }
        public abstract string Icon { get; }

        public abstract Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);
        public abstract Task<PaymentStatusResponse> CheckPaymentStatusAsync(string transactionId);
        public abstract Task<RefundResponse> ProcessRefundAsync(RefundRequest request);

        protected async Task<T> MakeApiRequestAsync<T>(string endpoint, object request)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API request failed: {response.StatusCode}, {responseContent}");
            }

            return JsonSerializer.Deserialize<T>(responseContent);
        }

        protected async Task<T> MakeApiGetRequestAsync<T>(string endpoint)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await httpClient.GetAsync($"{_baseUrl}{endpoint}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API request failed: {response.StatusCode}, {responseContent}");
            }

            return JsonSerializer.Deserialize<T>(responseContent);
        }
    }

    public class MtnMobileMoneyProvider : BaseMobileMoneyProvider
    {
        public override string ProviderCode => "mtn";
        public override string ProviderName => "MTN Mobile Money";
        public override bool IsEnabled => true;
        public override string Icon => "mtn-icon";

        public MtnMobileMoneyProvider(string apiKey, string baseUrl, ILogger logger, IHttpClientFactory httpClientFactory = null)
            : base(apiKey, baseUrl, logger, httpClientFactory)
        {
        }

        public override async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing MTN Mobile Money payment for {Amount}", request.Amount);

                var mtnRequest = new
                {
                    amount = request.Amount,
                    currency = request.Currency ?? "ZMW",
                    msisdn = request.PhoneNumber,
                    reference = request.Reference,
                    customer_email = request.CustomerEmail,
                    customer_name = request.CustomerName,
                    transaction_id = GenerateTransactionId()
                };

                var response = await MakeApiRequestAsync<MtnPaymentResponse>("/payments", mtnRequest);

                return new PaymentResponse
                {
                    Success = response.Status == "success",
                    Message = response.Message,
                    TransactionId = response.TransactionId,
                    ProcessedAt = DateTime.UtcNow,
                    Amount = request.Amount,
                    PaymentMethod = "mobile_money",
                    MobileMoneyProvider = "mtn",
                    Status = response.Status,
                    Instructions = response.Instructions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MTN Mobile Money payment");
                return new PaymentResponse
                {
                    Success = false,
                    Message = "MTN Mobile Money payment processing failed",
                    TransactionId = null
                };
            }
        }

        private string GenerateTransactionId()
        {
            while (true)
            {
                var suffix = new Random().Next(1000, 9999).ToString();
                var transactionId = $"MM_MTN_{DateTime.UtcNow:yyyyMMddHHmmss}_{suffix}";
                
                if (!TransactionIdExistsAsync(transactionId).GetAwaiter().GetResult())
                {
                    return transactionId;
                }
            }
        }

        private async Task<bool> TransactionIdExistsAsync(string transactionId)
        {
            // In a real implementation, this would check against the database
            return false;
        }
    }

    public class AirtelMoneyProvider : BaseMobileMoneyProvider
    {
        public override string ProviderCode => "airtel";
        public override string ProviderName => "Airtel Money";
        public override bool IsEnabled => true;
        public override string Icon => "airtel-icon";

        public AirtelMoneyProvider(string apiKey, string baseUrl, ILogger logger, IHttpClientFactory httpClientFactory = null)
            : base(apiKey, baseUrl, logger, httpClientFactory)
        {
        }

        public override async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing Airtel Money payment for {Amount}", request.Amount);

                var airtelRequest = new
                {
                    amount = request.Amount,
                    currency = request.Currency ?? "ZMW",
                    msisdn = request.PhoneNumber,
                    reference = request.Reference,
                    customer_email = request.CustomerEmail,
                    customer_name = request.CustomerName,
                    transaction_id = GenerateTransactionId()
                };

                var response = await MakeApiRequestAsync<AirtelPaymentResponse>("/payments", airtelRequest);

                return new PaymentResponse
                {
                    Success = response.Status == "success",
                    Message = response.Message,
                    TransactionId = response.TransactionId,
                    ProcessedAt = DateTime.UtcNow,
                    Amount = request.Amount,
                    PaymentMethod = "mobile_money",
                    MobileMoneyProvider = "airtel",
                    Status = response.Status,
                    Instructions = response.Instructions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Airtel Money payment");
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Airtel Money payment processing failed",
                    TransactionId = null
                };
            }
        }

        public override async Task<PaymentStatusResponse> CheckPaymentStatusAsync(string transactionId)
        {
            try
            {
                var response = await MakeApiGetRequestAsync<AirtelStatusResponse>($"/payments/{transactionId}/status");

                return new PaymentStatusResponse
                {
                    TransactionId = transactionId,
                    Status = response.Status,
                    Message = response.Message,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Airtel Money payment status");
                return new PaymentStatusResponse
                {
                    TransactionId = transactionId,
                    Status = "error",
                    Message = "Status check failed",
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public override async Task<RefundResponse> ProcessRefundAsync(RefundRequest request)
        {
            try
            {
                var refundRequest = new
                {
                    transaction_id = request.TransactionId,
                    amount = request.Amount,
                    reason = request.Reason
                };

                var response = await MakeApiRequestAsync<AirtelRefundResponse>("/refunds", refundRequest);

                return new RefundResponse
                {
                    Success = response.Status == "success",
                    Message = response.Message,
                    RefundId = response.RefundId,
                    ProcessedAt = DateTime.UtcNow,
                    Amount = request.Amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Airtel Money refund");
                return new RefundResponse
                {
                    Success = false,
                    Message = "Airtel Money refund failed",
                    RefundId = null
                };
            }
        }

        private string GenerateTransactionId()
        {
            while (true)
            {
                var suffix = new Random().Next(1000, 9999).ToString();
                var transactionId = $"MM_AIRTEL_{DateTime.UtcNow:yyyyMMddHHmmss}_{suffix}";
                
                if (!await TransactionIdExistsAsync(transactionId))
                {
                    return transactionId;
                }
            }
        }

        private async Task<bool> TransactionIdExistsAsync(string transactionId)
        {
            // In a real implementation, this would check against the database
            return false;
        }
    }

    public class ZamtelMobileMoneyProvider : BaseMobileMoneyProvider
    {
        public override string ProviderCode => "zamtel";
        public override string ProviderName => "Zamtel Mobile Money";
        public override bool IsEnabled => true;
        public override string Icon => "zamtel-icon";

        public ZamtelMobileMoneyProvider(string apiKey, string baseUrl, ILogger logger, IHttpClientFactory httpClientFactory = null)
            : base(apiKey, baseUrl, logger, httpClientFactory)
        {
        }

        public override async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing Zamtel Mobile Money payment for {Amount}", request.Amount);

                var zamtelRequest = new
                {
                    amount = request.Amount,
                    currency = request.Currency ?? "ZMW",
                    msisdn = request.PhoneNumber,
                    reference = request.Reference,
                    customer_email = request.CustomerEmail,
                    customer_name = request.CustomerName,
                    transaction_id = GenerateTransactionId()
                };

                var response = await MakeApiRequestAsync<ZamtelPaymentResponse>("/payments", zamtelRequest);

                return new PaymentResponse
                {
                    Success = response.Status == "success",
                    Message = response.Message,
                    TransactionId = response.TransactionId,
                    ProcessedAt = DateTime.UtcNow,
                    Amount = request.Amount,
                    PaymentMethod = "mobile_money",
                    MobileMoneyProvider = "zamtel",
                    Status = response.Status,
                    Instructions = response.Instructions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Zamtel Mobile Money payment");
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Zamtel Mobile Money payment processing failed",
                    TransactionId = null
                };
            }
        }

        public override async Task<PaymentStatusResponse> CheckPaymentStatusAsync(string transactionId)
        {
            try
            {
                var response = await MakeApiGetRequestAsync<ZamtelStatusResponse>($"/payments/{transactionId}/status");

                return new PaymentStatusResponse
                {
                    TransactionId = transactionId,
                    Status = response.Status,
                    Message = response.Message,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Zamtel Mobile Money payment status");
                return new PaymentStatusResponse
                {
                    TransactionId = transactionId,
                    Status = "error",
                    Message = "Status check failed",
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public override async Task<RefundResponse> ProcessRefundAsync(RefundRequest request)
        {
            try
            {
                var refundRequest = new
                {
                    transaction_id = request.TransactionId,
                    amount = request.Amount,
                    reason = request.Reason
                };

                var response = await MakeApiRequestAsync<ZamtelRefundResponse>("/refunds", refundRequest);

                return new RefundResponse
                {
                    Success = response.Status == "success",
                    Message = response.Message,
                    RefundId = response.RefundId,
                    ProcessedAt = DateTime.UtcNow,
                    Amount = request.Amount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Zamtel Mobile Money refund");
                return new RefundResponse
                {
                    Success = false,
                    Message = "Zamtel Mobile Money refund failed",
                    RefundId = null
                };
            }
        }

        private string GenerateTransactionId()
        {
            while (true)
            {
                var suffix = new Random().Next(1000, 9999).ToString();
                var transactionId = $"MM_ZAMTEL_{DateTime.UtcNow:yyyyMMddHHmmss}_{suffix}";
                
                if (!await TransactionIdExistsAsync(transactionId))
                {
                    return transactionId;
                }
            }
        }

        private async Task<bool> TransactionIdExistsAsync(string transactionId)
        {
            // In a real implementation, this would check against the database
            return false;
        }
    }

    // Response DTOs for mobile money providers
    public class MtnPaymentResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string Instructions { get; set; }
    }

    public class MtnStatusResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class MtnRefundResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string RefundId { get; set; }
    }

    public class AirtelPaymentResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string Instructions { get; set; }
    }

    public class AirtelStatusResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class AirtelRefundResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string RefundId { get; set; }
    }

    public class ZamtelPaymentResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string Instructions { get; set; }
    }

    public class ZamtelStatusResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class ZamtelRefundResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string RefundId { get; set; }
    }
}
