using System;
using System.Collections.Generic;
using UmiHealth.Core.Interfaces;

namespace UmiHealth.Application.Adapters
{
    public class UserDtoAdapter : IUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; }

        // Extra convenience properties
        public List<string> Roles { get; set; } = new();
    }
}
