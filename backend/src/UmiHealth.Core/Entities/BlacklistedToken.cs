namespace UmiHealth.Core.Entities;

public class BlacklistedToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? Reason { get; set; }
}
