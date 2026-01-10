using System;
using System.Collections.Generic;
using UmiHealth.Core.Entities;

namespace UmiHealth.Application.DTOs
{
    // Tenant-related DTOs
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime? NextBilling { get; set; }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
    }

    public class UpdateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Subscription-related DTOs
    public class SubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string TenantDomain { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string NextBilling { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime? NextBillingDate { get; set; }
    }

    public class UpdateSubscriptionRequest
    {
        public string Plan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class UpgradeSubscriptionRequest
    {
        public string TargetPlan { get; set; } = string.Empty;
        public bool ProRated { get; set; } = true;
    }

    // Transaction-related DTOs
    public class TransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string TenantDomain { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }

    public class TransactionReceiptDto
    {
        public byte[] Content { get; set; } = new byte[0];
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    // Pagination DTOs
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // Payment-related DTOs
    public class PaymentPlanRequest
    {
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? SaleId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DownPaymentAmount { get; set; }
        public string DownPaymentMethod { get; set; } = string.Empty;
        public int InstallmentCount { get; set; }
        public string InstallmentFrequency { get; set; } = "monthly"; // weekly, biweekly, monthly, quarterly
        public DateTime StartDate { get; set; }
        public decimal InterestRate { get; set; }
        public decimal LateFeeRate { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class PaymentPlanResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? PaymentPlanId { get; set; }
        public string? PlanNumber { get; set; }
        public List<Installment>? Installments { get; set; }
        public DateTime? NextPaymentDate { get; set; }
    }

    // Payment Plan entity
    public class PaymentPlan : TenantEntity
    {
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? SaleId { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DownPaymentAmount { get; set; }
        public decimal? DownPaymentPaid { get; set; }
        public DateTime? DownPaymentDate { get; set; }
        public int InstallmentCount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public string InstallmentFrequency { get; set; } = "monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal InterestRate { get; set; }
        public decimal LateFeeRate { get; set; }
        public string Status { get; set; } = "active"; // active, completed, defaulted, cancelled
        public DateTime? CompletedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        public virtual UmiHealth.Core.Entities.User Customer { get; set; } = null!;
        public virtual UmiHealth.Core.Entities.Sale? Sale { get; set; } = null!;
        public virtual List<Installment> Installments { get; set; } = new();
    }

    public class Installment : TenantEntity
    {
        public Guid PaymentPlanId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = "pending"; // pending, paid, overdue, cancelled
        public decimal LateFee { get; set; }
        public DateTime? LateFeeAppliedDate { get; set; }
        
        // Navigation property
        public virtual PaymentPlan PaymentPlan { get; set; } = null!;
    }

    // Reporting DTOs
    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
        public string Currency { get; set; } = "ZMW";
    }

    public class FailedPaymentDto
    {
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime FailureDate { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    public class RefundAnalyticsDto
    {
        public DateTime Date { get; set; }
        public decimal RefundAmount { get; set; }
        public int RefundCount { get; set; }
        public string RefundReason { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
