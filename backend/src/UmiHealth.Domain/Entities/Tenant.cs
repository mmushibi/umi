using System;
using System.Collections.Generic;

namespace UmiHealth.Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public string SubscriptionPlan { get; set; } = "basic";
        public int MaxBranches { get; set; } = 1;
        public int MaxUsers { get; set; } = 10;
        public Dictionary<string, object> Settings { get; set; } = new();
        public Dictionary<string, object> BillingInfo { get; set; } = new();
        public Dictionary<string, object> ComplianceSettings { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }

    public class Branch
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LicenseNumber { get; set; }
        public Dictionary<string, object> OperatingHours { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }

    public class User
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<Guid> BranchAccess { get; set; } = new();
        public Dictionary<string, object> Permissions { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public bool EmailVerified { get; set; } = false;
        public bool PhoneVerified { get; set; } = false;
        public bool TwoFactorEnabled { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
    }

    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public string BillingCycle { get; set; } = "monthly";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public Dictionary<string, object> Features { get; set; } = new();
        public Dictionary<string, object> Limits { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool AutoRenew { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
    }
}
