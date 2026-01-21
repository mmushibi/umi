using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class Prescription
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string PatientId { get; set; } = string.Empty;
        
        [Required]
        public string DoctorId { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string PrescriptionNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(1000)]
        public string Diagnosis { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "active"; // active, completed, cancelled
        
        public DateTime? FilledDate { get; set; }
        
        [MaxLength(100)]
        public string? FilledBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public User Doctor { get; set; } = null!;
        public Tenant Tenant { get; set; } = null!;
        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }
    
    public class PrescriptionItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string PrescriptionId { get; set; } = string.Empty;
        
        [Required]
        public string InventoryId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string MedicationName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Dosage { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Frequency { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Duration { get; set; } = string.Empty;
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [MaxLength(500)]
        public string? Instructions { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Prescription Prescription { get; set; } = null!;
        public Inventory Inventory { get; set; } = null!;
    }
}
