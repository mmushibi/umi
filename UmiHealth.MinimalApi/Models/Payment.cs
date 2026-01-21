using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class Payment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string PatientId { get; set; } = string.Empty;
        
        [Required]
        public string SaleId { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string PaymentNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public decimal Amount { get; set; }
        
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "cash"; // cash, card, mobile, insurance
        
        [MaxLength(100)]
        public string? PaymentReference { get; set; }
        
        [MaxLength(100)]
        public string? TransactionId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "completed"; // pending, completed, failed, refunded
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public Sale Sale { get; set; } = null!;
        public Tenant Tenant { get; set; } = null!;
    }
}
