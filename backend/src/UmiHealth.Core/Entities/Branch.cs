using UmiHealth.Core.Entities;

namespace UmiHealth.Core.Entities;

public class Branch : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
