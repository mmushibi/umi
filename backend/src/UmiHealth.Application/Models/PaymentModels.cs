using System;
using System.ComponentModel.DataAnnotations;

namespace UmiHealth.Application.Models
{
    public class PaymentRequest
    {
        [Required]
        public string PlanType { get; set; } = string.Empty;
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        public string? TransactionReference { get; set; }
        
        public string? PaymentReceipt { get; set; } // Base64 encoded image
        public string? AdditionalNotes { get; set; }
    }

    public class PaymentMethod
    {
        public const string MobileMoney = "mobile_money";
        public const string BankTransfer = "bank_transfer";
        public const string Cash = "cash";
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? PaymentId { get; set; }
        public string? Status { get; set; }
        public DateTime? EstimatedApprovalTime { get; set; }
    }

    public class PaymentApprovalRequest
    {
        [Required]
        public string PaymentId { get; set; } = string.Empty;
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        [Required]
        public string TenantName { get; set; } = string.Empty;
        
        [Required]
        public string PlanType { get; set; } = string.Empty;
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
        
        public string? TransactionReference { get; set; }
        
        public string? PaymentReceipt { get; set; } // Base64 encoded image
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        public string? RequestedBy { get; set; } // User who made the request
        
        public string? AdditionalNotes { get; set; }
    }

    public class PaymentApprovalResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? TransactionId { get; set; }
        public string? ConfirmationNumber { get; set; }
    }

    public class PaymentStatusUpdate
    {
        public string PaymentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? TransactionId { get; set; }
        public string? ConfirmationNumber { get; set; }
        public string? PaymentReceipt { get; set; } // Base64 encoded receipt
        public string? AdditionalNotes { get; set; }
    }

    public enum PaymentStatusType
    {
        Pending = "pending",
        Approved = "approved", 
        Rejected = "rejected",
        Processing = "processing"
    }
}
