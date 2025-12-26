using System;
using System.Collections.Generic;
using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities
{
    public class ProcurementRequest : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public Guid RequestingBranchId { get; set; }
        public Guid? ApprovingBranchId { get; set; } // Usually main branch
        public string Status { get; set; } = "pending"; // pending, approved, ordered, received, cancelled
        public string Type { get; set; } = "central"; // central, branch
        public Guid RequestedByUserId { get; set; }
        public Guid? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public decimal TotalAmount { get; set; } = 0;
        public string Currency { get; set; } = "ZMW";
        public string? SupplierId { get; set; }
        public string? Notes { get; set; }
        public List<ProcurementItem> Items { get; set; } = new();
        public List<ProcurementDistribution> Distributions { get; set; } = new(); // How items are distributed to branches
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Branch RequestingBranch { get; set; } = null!;
        public virtual Branch? ApprovingBranch { get; set; }
        public virtual User RequestedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
    }

    public class ProcurementItem : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid ProcurementRequestId { get; set; }
        public Guid ProductId { get; set; }
        public int QuantityRequested { get; set; }
        public int QuantityApproved { get; set; }
        public int QuantityReceived { get; set; } = 0;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ProcurementRequest ProcurementRequest { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }

    public class ProcurementDistribution : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid ProcurementRequestId { get; set; }
        public Guid ProcurementItemId { get; set; }
        public Guid BranchId { get; set; }
        public int QuantityAllocated { get; set; }
        public int QuantityReceived { get; set; } = 0;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ProcurementRequest ProcurementRequest { get; set; } = null!;
        public virtual ProcurementItem ProcurementItem { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
    }

    public class BranchReport : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // sales, inventory, financial, etc.
        public string Period { get; set; } = string.Empty; // daily, weekly, monthly, etc.
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public string Status { get; set; } = "generating"; // generating, completed, failed
        public string? GeneratedByUserId { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
        public virtual User? GeneratedByUser { get; set; }
    }
}
