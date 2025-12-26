namespace UmiHealth.Core.Entities;

public class Role : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();
}
