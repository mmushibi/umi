using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class Report
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(100)]
        public string ReportName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty; // sales, inventory, patient, financial
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [MaxLength(100)]
        public string? GeneratedBy { get; set; }
        
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? FilePath { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "generated"; // generating, generated, failed
        
        [MaxLength(1000)]
        public string? Parameters { get; set; } // JSON string of report parameters
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        // Navigation property
        public Tenant Tenant { get; set; } = null!;
    }
}
