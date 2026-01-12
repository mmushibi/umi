using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmiHealth.Domain.Entities
{
    public class UserLimitRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public int CurrentUsers { get; set; }
        
        [Required]
        public int RequestedUsers { get; set; }
        
        public int AdditionalUsers => RequestedUsers - CurrentUsers;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalCost => AdditionalUsers * 50m; // 50 per additional user
        
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, approved, rejected
        
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? ApprovalDate { get; set; }
        
        [MaxLength(255)]
        public string? ApprovedBy { get; set; }
        
        [Required]
        public Guid RequestedByUserId { get; set; }
        
        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User RequestedByUser { get; set; } = null!;
    }
}
