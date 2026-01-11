using System;

namespace UmiHealth.Domain.Entities
{
    public class UserInvitation
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid InvitedByUserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public bool IsAccepted { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User InvitedByUser { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
    }
}
