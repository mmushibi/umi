namespace UmiHealth.Core.Entities;

public class UserClaim : TenantEntity
{
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
