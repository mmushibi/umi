namespace UmiHealth.Core.Entities;

public class Supplier : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? LicenseNumber { get; set; }
    public bool IsActive { get; set; } = true;
}