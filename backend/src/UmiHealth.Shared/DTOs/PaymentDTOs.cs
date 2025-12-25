using System;
using System.Collections.Generic;

namespace UmiHealth.Shared.DTOs
{
    // Payment Processing DTOs
    public class PaymentRequest
    {
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? SaleId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Card payment details
        public CardDetailsDto? CardDetails { get; set; }
        
        // Mobile money details
        public string MobileMoneyProvider { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        // Insurance details
        public string InuranceProvider { get; set; } = string.Empty;
        public string InsurancePolicyNumber { get; set; } = string.Empty;
        
        // Customer details
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        
        // Additional metadata
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string MobileMoneyProvider { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public bool RequiresConfirmation { get; set; } = false;
        public string InsuranceProvider { get; set; } = string.Empty;
        public string InsurancePolicyNumber { get; set; } = string.Empty;
        public object GatewayResponse { get; set; }
    }

    public class PaymentStatusResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, completed, failed, cancelled
        public string Message { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }

    public class RefundRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RefundReference { get; set; } = string.Empty;
        public Guid RequestedBy { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class RefundResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RefundId { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OriginalTransactionId { get; set; } = string.Empty;
    }

    // Card Payment DTOs
    public class CardDetailsDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty; // visa, mastercard, etc.
        public string BillingAddress { get; set; } = string.Empty;
        public string BillingCity { get; set; } = string.Empty;
        public string BillingCountry { get; set; } = string.Empty;
        public string BillingPostalCode { get; set; } = string.Empty;
    }

    // Mobile Money DTOs
    public class MobileMoneyRequest
    {
        public Guid TenantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string Provider { get; set; } = string.Empty; // mtn, airtel, zamtel
        public string PhoneNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class MobileMoneyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    // Payment Method DTOs
    public class PaymentMethodDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string Icon { get; set; } = string.Empty;
        public bool IsMobileMoney { get; set; } = false;
        public List<string> SupportedCurrencies { get; set; } = new();
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    // Transaction History DTOs
    public class TransactionHistoryDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MobileMoneyProvider { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public bool IsRefund { get; set; } = false;
        public string OriginalTransactionId { get; set; } = string.Empty;
    }

    // Payment Analytics DTOs
    public class PaymentAnalyticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public List<PaymentMethodAnalytics> PaymentMethods { get; set; } = new();
        public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
        public List<FailedPaymentDto> FailedPayments { get; set; } = new();
        public List<RefundAnalyticsDto> Refunds { get; set; } = new();
    }

    public class PaymentMethodAnalytics
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
        public decimal AverageAmount { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new();
    }

    public class FailedPaymentDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class RefundAnalyticsDto
    {
        public string RefundId { get; set; } = string.Empty;
        public DateTime RefundDate { get; set; }
        public decimal RefundAmount { get; set; }
        public string OriginalTransactionId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    // Payment Configuration DTOs
    public class PaymentConfigurationDto
    {
        public Guid TenantId { get; set; }
        public List<PaymentMethodConfiguration> EnabledPaymentMethods { get; set; } = new();
        public MobileMoneyConfiguration MobileMoney { get; set; } = new();
        public CardPaymentConfiguration CardPayment { get; set; } = new();
        public BankTransferConfiguration BankTransfer { get; set; } = new();
        public InsuranceConfiguration Insurance { get; set; } = new();
        public bool RequireCustomerDetails { get; set; } = true;
        public bool EnableRefunds { get; set; } = true;
        public int RefundTimeLimitHours { get; set; } = 24;
        public List<string> SupportedCurrencies { get; set; } = new();
    }

    public class PaymentMethodConfiguration
    {
        public string Code { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal TransactionFee { get; set; }
        public decimal TransactionFeePercentage { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class MobileMoneyConfiguration
    {
        public bool MtnEnabled { get; set; } = true;
        public bool AirtelEnabled { get; set; } = true;
        public bool ZamtelEnabled { get; set; } = true;
        public int PaymentTimeoutMinutes { get; set; } = 15;
        public bool AutoConfirmPayments { get; set; } = false;
        public int MaxRetryAttempts { get; set; } = 3;
        public decimal MaxAmount { get; set; } = 50000;
        public decimal MinAmount { get; set; } = 1;
    }

    public class CardPaymentConfiguration
    {
        public bool IsEnabled { get; set; } = true;
        public string Provider { get; set; } = string.Empty; // stripe, paypal, etc.
        public bool RequireCVV { get; set; } = true;
        public bool SaveCards { get; set; } = false;
        public decimal MaxAmount { get; set; } = 100000;
        public decimal MinAmount { get; set; } = 1;
        public decimal TransactionFee { get; set; } = 0;
        public decimal TransactionFeePercentage { get; set; } = 2.5m;
    }

    public class BankTransferConfiguration
    {
        public bool IsEnabled { get; set; } = true;
        public List<BankAccountDto> BankAccounts { get; set; } = new();
        public bool RequireReference { get; set; } = true;
        public int ConfirmationTimeoutHours { get; set; } = 24;
        public decimal MaxAmount { get; set; } = 1000000;
        public decimal MinAmount { get; set; } = 100;
    }

    public class InsuranceConfiguration
    {
        public bool IsEnabled { get; set; } = true;
        public List<string> SupportedProviders { get; set; } = new();
        public bool RequirePreAuthorization { get; set; } = true;
        public decimal MaxAmount { get; set; } = 50000;
        public decimal MinAmount { get; set; } = 1;
        public int ValidationTimeoutMinutes { get; set; } = 5;
    }

    public class BankAccountDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string RoutingNumber { get; set; } = string.Empty;
        public string SwiftCode { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    // Payment Notification DTOs
    public class PaymentNotificationDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // payment_initiated, payment_completed, payment_failed, refund_processed
        public DateTime Timestamp { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }
}
