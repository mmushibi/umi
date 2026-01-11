namespace UmiHealth.Application.Models
{
    public class SubscriptionStatus
    {
        public bool HasAccess { get; set; }
        public bool IsTrial { get; set; }
        public string PlanType { get; set; } = string.Empty;
        public DateTime? TrialEndDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool TrialExpired { get; set; }
        public bool TenantSuspended { get; set; }
        public bool IsPaidSubscription { get; set; }
    }

    public class PlanLimits
    {
        public int MaxUsers { get; set; }
        public int MaxBranches { get; set; }
    }
}
