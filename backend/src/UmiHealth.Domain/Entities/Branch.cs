using System;
using System.Collections.Generic;
using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities
{
    public interface ISoftDeletable
    {
        public DateTime? DeletedAt { get; set; }
    }

    public class Branch : TenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LicenseNumber { get; set; }
        public Dictionary<string, object> OperatingHours { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
        public bool IsMainBranch { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public Guid? ParentBranchId { get; set; }
        public List<Guid> ChildBranchIds { get; set; } = new();
        public Dictionary<string, object> Location { get; set; } = new();
        public string? ManagerName { get; set; }
        public string? ManagerContact { get; set; }
        public Dictionary<string, object> InventorySettings { get; set; } = new();
        public Dictionary<string, object> ReportingSettings { get; set; } = new();

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Branch? ParentBranch { get; set; }
        public virtual ICollection<Branch> ChildBranches { get; set; } = new List<Branch>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }

    public class StockTransfer : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public Guid SourceBranchId { get; set; }
        public Guid DestinationBranchId { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, in_transit, completed, cancelled
        public Guid RequestedByUserId { get; set; }
        public Guid? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public List<StockTransferItem> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Branch SourceBranch { get; set; } = null!;
        public virtual Branch DestinationBranch { get; set; } = null!;
        public virtual User RequestedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
    }

    public class StockTransferItem : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid StockTransferId { get; set; }
        public Guid ProductId { get; set; }
        public Guid SourceInventoryId { get; set; }
        public Guid? DestinationInventoryId { get; set; }
        public int QuantityRequested { get; set; }
        public int QuantityApproved { get; set; }
        public int QuantityTransferred { get; set; } = 0;
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? CostPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual StockTransfer StockTransfer { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Inventory SourceInventory { get; set; } = null!;
        public virtual Inventory? DestinationInventory { get; set; }
    }
}
