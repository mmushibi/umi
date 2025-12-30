using System;
using System.Collections.Generic;

namespace UmiHealth.Domain.Entities
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string BillingCycle { get; set; } = "monthly"; // monthly, yearly
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
        public bool IsActive { get; set; } = true;
        public string Features { get; set; } = string.Empty;
        public string? StorageQuota { get; set; }
        public string? ApiQuota { get; set; }
        public string? SupportLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SubscriptionTransaction
    {
        public Guid Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid? SubscriptionId { get; set; }
        public Guid? RequestedBy { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = "pending_approval";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string? PlanFrom { get; set; }
        public string? PlanTo { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Subscription? Subscription { get; set; }
        public virtual User? RequestedByUser { get; set; }
        public virtual User? ApprovedByUser { get; set; }
    }

    public class AdditionalUserRequest
    {
        public Guid Id { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid RequestedBy { get; set; }
        public Guid? ApprovedBy { get; set; }
        public Guid? UserCreatedId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string SubscriptionPlanAtRequest { get; set; } = string.Empty;
        public string Status { get; set; } = "pending_approval";
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
        public virtual User RequestedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
        public virtual User? UserCreated { get; set; }
    }

    public class AdditionalUserCharge
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public Guid? ApprovedBy { get; set; }
        public decimal Amount { get; set; }
        public decimal ChargeAmount => Amount; // Computed property for compatibility
        public string Currency { get; set; } = "ZMW";
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public string Status { get; set; } = "pending_payment";
        public string? PaymentReference { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
    }

    public class Notification
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? SenderId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime ReadAt { get; set; }
        public string? Data { get; set; }
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User? User { get; set; }
        public virtual User? Sender { get; set; }
    }

    public class NotificationSettings
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public bool EmailNotifications { get; set; } = true;
        public bool SmsNotifications { get; set; } = false;
        public bool PushNotifications { get; set; } = true;
        public bool LowStockAlerts { get; set; } = true;
        public bool ExpiryAlerts { get; set; } = true;
        public bool SalesReports { get; set; } = true;
        public bool SystemUpdates { get; set; } = false;
        public string? CustomAlerts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User? User { get; set; }
    }

    public class PaymentTransaction
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public Guid? ChargeId { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public DateTime TransactionDate { get; set; }
        public string? RefundReason { get; set; }
        public DateTime? RefundDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual AdditionalUserCharge? Charge { get; set; }
    }
}
