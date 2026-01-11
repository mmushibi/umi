using System;

namespace UmiHealth.Domain.Entities
{
    public class UserAdditionalUser
    {
        public Guid Id { get; set; }
        public Guid MainUserId { get; set; }
        public Guid AdditionalUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User MainUser { get; set; } = null!;
        public virtual User AdditionalUser { get; set; } = null!;
    }
}
