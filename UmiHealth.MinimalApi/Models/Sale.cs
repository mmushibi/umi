using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class Sale
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string PatientId { get; set; } = string.Empty;
        
        [Required]
        public string CashierId { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string SaleNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public decimal Subtotal { get; set; }
        
        [Required]
        public decimal TaxAmount { get; set; }
        
        [Required]
        public decimal DiscountAmount { get; set; }
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        [Required]
        public decimal AmountPaid { get; set; }
        
        public decimal ChangeAmount { get; set; }
        
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "cash"; // cash, card, mobile, insurance
        
        [MaxLength(100)]
        public string? PaymentReference { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "completed"; // pending, completed, cancelled, refunded
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public User Cashier { get; set; } = null!;
        public Tenant Tenant { get; set; } = null!;
        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    }
    
    public class SaleItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string SaleId { get; set; } = string.Empty;
        
        [Required]
        public string InventoryId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [Required]
        public decimal DiscountPercentage { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Sale Sale { get; set; } = null!;
        public Inventory Inventory { get; set; } = null!;
    }
}
