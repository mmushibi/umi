using System;
using System.Collections.Generic;
using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities
{
    public class Tenant : TenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public string SubscriptionPlan { get; set; } = "basic";
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Settings { get; set; }
        public string? BillingInfo { get; set; }
        public string? ComplianceSettings { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<UmiHealth.Core.Entities.Role> Roles { get; set; } = new List<UmiHealth.Core.Entities.Role>();
    }

    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public string BillingCycle { get; set; } = "monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string? Features { get; set; }
        public string? Limits { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
    }

    public class User
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? BranchAccess { get; set; }
        public string? Permissions { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        // Computed properties
        public string FullName => $"{FirstName} {LastName}";

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
