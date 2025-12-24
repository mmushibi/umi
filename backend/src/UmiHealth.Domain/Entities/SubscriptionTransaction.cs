using System;

namespace UmiHealth.Domain.Entities
{
    public class SubscriptionTransaction
    {
        public Guid Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public Guid SubscriptionId { get; set; }
        public Guid TenantId { get; set; }
        public string Type { get; set; } = string.Empty; // upgrade, new, cancellation, renewal
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string Status { get; set; } = string.Empty; // pending_approval, approved, rejected, completed
        public string RequestedBy { get; set; } = string.Empty; // User email or ID
        public string PlanFrom { get; set; } = string.Empty;
        public string PlanTo { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; } // Approver's email or ID
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
