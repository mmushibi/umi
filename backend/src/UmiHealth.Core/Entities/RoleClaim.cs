namespace UmiHealth.Core.Entities;

public class RoleClaim : TenantEntity
{
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    
    // Navigation properties
    public virtual Role Role { get; set; } = null!;
}
