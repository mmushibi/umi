using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        [MaxLength(500)]
        public string? Bio { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "user";
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "active";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string TenantId { get; set; } = string.Empty;
        
        public bool OnboardingCompleted { get; set; } = false;
        
        public DateTime? UpdatedAt { get; set; }
        
        [MaxLength(500)]
        public string? RefreshToken { get; set; }
        
        public DateTime? RefreshTokenExpiryTime { get; set; }
        
        // Navigation property
        public Tenant Tenant { get; set; } = null!;
    }

    public class Tenant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "active";
        
        [Required]
        [MaxLength(50)]
        public string SubscriptionPlan { get; set; } = "Care";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Onboarding fields
        [MaxLength(50)]
        public string? LicenseNumber { get; set; }
        
        [MaxLength(500)]
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? City { get; set; }
        
        [MaxLength(100)]
        public string? Province { get; set; }
        
        [MaxLength(20)]
        public string? PostalCode { get; set; }
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        [MaxLength(200)]
        public string? OperatingHours { get; set; }
        
        [MaxLength(50)]
        public string? PharmacyType { get; set; }
        
        public int? YearsInBusiness { get; set; }
        
        public int? StaffCount { get; set; }
        
        [MaxLength(100)]
        public string? CurrentSystem { get; set; }
        
        [MaxLength(500)]
        public string? EnabledFeatures { get; set; }
        
        public bool EnableNotifications { get; set; } = true;
        
        public bool OnboardingCompleted { get; set; } = false;
        
        public DateTime? OnboardingCompletedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
