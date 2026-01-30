using System.ComponentModel.DataAnnotations;

namespace UmiHealth.MinimalApi.Models
{
    public class UpdateProfileRequest
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }
        
        [MaxLength(100)]
        public string? LastName { get; set; }
        
        [MaxLength(255)]
        [EmailAddress]
        public string? Email { get; set; }
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        [MaxLength(500)]
        public string? Bio { get; set; }
    }

    public class PharmacySettingsRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }
        
        [MaxLength(255)]
        public string? Email { get; set; }
        
        [MaxLength(500)]
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? City { get; set; }
        
        [MaxLength(100)]
        public string? Province { get; set; }
        
        [MaxLength(20)]
        public string? PostalCode { get; set; }
        
        [MaxLength(50)]
        public string? LicenseNumber { get; set; }
        
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
        
        public bool EnableNotifications { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        [MaxLength(255)]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class NotificationSettingsRequest
    {
        public bool EnableNotifications { get; set; }
        
        [MaxLength(500)]
        public string? NotificationEmail { get; set; }
        
        [MaxLength(20)]
        public string? NotificationPhone { get; set; }
    }
}
