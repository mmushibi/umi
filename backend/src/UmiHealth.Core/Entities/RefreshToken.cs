namespace UmiHealth.Core.Entities;

public class RefreshToken : TenantEntity
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? IssuedAt { get; set; } = DateTime.UtcNow;
    public string? JwtTokenId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
