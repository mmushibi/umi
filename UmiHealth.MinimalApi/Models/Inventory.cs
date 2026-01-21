using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class Inventory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string GenericName { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string ProductCode { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Barcode { get; set; } = string.Empty;
        
        [Required]
        public int CurrentStock { get; set; }
        
        [Required]
        public int MinStockLevel { get; set; }
        
        [Required]
        public int MaxStockLevel { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [Required]
        public decimal SellingPrice { get; set; }
        
        [MaxLength(50)]
        public string Unit { get; set; } = "pieces";
        
        [MaxLength(255)]
        public string? Manufacturer { get; set; }
        
        [MaxLength(100)]
        public string? Supplier { get; set; }
        
        [MaxLength(10)]
        public string? ExpiryDate { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "active";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        // Navigation property
        public Tenant Tenant { get; set; } = null!;
    }
}
