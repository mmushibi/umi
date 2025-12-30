using System;

namespace UmiHealth.Application.DTOs
{
    // Operations-related DTOs
    public class DashboardStatsDto
    {
        public int TotalTenants { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NewTenantsThisMonth { get; set; }
        public int NewUsersThisMonth { get; set; }
    }

    public class RecentTenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SyncStatusDto
    {
        public string LastSyncTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int PendingRecords { get; set; }
        public int FailedRecords { get; set; }
    }
}
