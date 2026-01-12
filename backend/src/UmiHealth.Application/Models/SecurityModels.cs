namespace UmiHealth.Application.Models
{
    public enum SecurityEventType
    {
        Login = 1,
        LoginSuccess = 2,
        LoginFailure = 3,
        Logout = 4,
        PasswordChange = 5,
        PaymentApproval = 6,
        PaymentRejection = 7,
        UserLimitRequest = 8,
        UnauthorizedAccess = 9,
        SuspiciousActivity = 10,
        SecurityViolation = 11,
        RateLimitExceeded = 12
    }

    public enum SecurityRiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public class SecurityEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public SecurityRiskLevel RiskLevel { get; set; } = SecurityRiskLevel.Low;
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        public int EventCode { get; set; } = 0;
        public string EventCodeString => EventCode.ToString("D2");
    }
}
