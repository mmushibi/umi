namespace UmiHealth.Core.Entities;

public class BlacklistedToken : BaseEntity
{
    public string TokenId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime BlacklistedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
