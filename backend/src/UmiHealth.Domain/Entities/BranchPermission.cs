using System;
using System.Collections.Generic;
using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities
{
    public class BranchPermission : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public Guid BranchId { get; set; }
        public List<string> Permissions { get; set; } = new(); // inventory_read, inventory_write, sales_read, etc.
        public bool IsManager { get; set; } = false;
        public bool CanTransferStock { get; set; } = false;
        public bool CanApproveTransfers { get; set; } = false;
        public bool CanViewReports { get; set; } = false;
        public bool CanManageUsers { get; set; } = false;
        public Dictionary<string, object> Restrictions { get; set; } = new();
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
    }

    public static class BranchPermissions
    {
        public const string INVENTORY_READ = "inventory_read";
        public const string INVENTORY_WRITE = "inventory_write";
        public const string INVENTORY_DELETE = "inventory_delete";
        public const string SALES_READ = "sales_read";
        public const string SALES_WRITE = "sales_write";
        public const string SALES_DELETE = "sales_delete";
        public const string PATIENTS_READ = "patients_read";
        public const string PATIENTS_WRITE = "patients_write";
        public const string PATIENTS_DELETE = "patients_delete";
        public const string PRESCRIPTIONS_READ = "prescriptions_read";
        public const string PRESCRIPTIONS_WRITE = "prescriptions_write";
        public const string PRESCRIPTIONS_DELETE = "prescriptions_delete";
        public const string STOCK_TRANSFER = "stock_transfer";
        public const string APPROVE_TRANSFERS = "approve_transfers";
        public const string VIEW_REPORTS = "view_reports";
        public const string MANAGE_USERS = "manage_users";
        public const string MANAGE_SETTINGS = "manage_settings";
        public const string PROCUREMENT_READ = "procurement_read";
        public const string PROCUREMENT_WRITE = "procurement_write";
    }
}
