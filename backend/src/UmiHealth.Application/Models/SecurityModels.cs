namespace UmiHealth.Application.Models
{
    public enum SecurityEventType
    {
        Login = "login",
        Logout = "logout",
        PasswordChange = "password_change",
        PaymentApproval = "payment_approval",
        PaymentRejection = "payment_rejection",
        UserLimitRequest = "user_limit_request"
    }

    public enum SecurityRiskLevel
    {
        Low = "low",
        Medium = "medium",
        High = "high",
        Critical = "critical"
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
