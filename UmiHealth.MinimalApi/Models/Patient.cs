using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class Patient
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        [MaxLength(10)]
        public string? DateOfBirth { get; set; }
        
        [MaxLength(10)]
        public string Gender { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? EmergencyContact { get; set; }
        
        [MaxLength(20)]
        public string? EmergencyPhone { get; set; }
        
        [MaxLength(50)]
        public string BloodType { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Allergies { get; set; }
        
        [MaxLength(1000)]
        public string? MedicalHistory { get; set; }
        
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
