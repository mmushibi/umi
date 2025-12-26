namespace UmiHealth.Core.Entities;

public class PrescriptionItem : TenantEntity
{
    public Guid PrescriptionId { get; set; }
    public Guid ProductId { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string DurationUnit { get; set; } = "Days";
    public int Quantity { get; set; }
    public string? Instructions { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Prescription Prescription { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
