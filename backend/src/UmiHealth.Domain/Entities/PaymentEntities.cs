using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealth.Domain.Entities
{
    public class PaymentRecord : ISoftDeletable
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        public Guid TenantId { get; set; }

        // Make branch optional to avoid forcing a DB default during migration
        public Guid? BranchId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string PlanType { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? TransactionReference { get; set; }
        
        [MaxLength(5000)] // Base64 encoded image
        public string? PaymentReceipt { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, approved, rejected
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? ApprovalDate { get; set; }
        
        [MaxLength(255)]
        public string? ApprovedBy { get; set; }
        
        [MaxLength(100)]
        public string? TransactionId { get; set; }
        
        [MaxLength(100)]
        public string? ConfirmationNumber { get; set; }
        
        [MaxLength(500)]
        public string? AdditionalNotes { get; set; }

        // Track last update (used by sync ordering)
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete support (ApplySoftDeletes will use ISoftDeletable)
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
    }

    public class PaymentApprovalNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PaymentId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public string? PaymentReceipt { get; set; }
        public string? AdditionalNotes { get; set; }
        public string? RequestedBy { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public bool IsProcessed { get; set; } = false;
        public DateTime? ProcessedDate { get; set; }
        public string? ProcessedBy { get; set; }
    }
}
