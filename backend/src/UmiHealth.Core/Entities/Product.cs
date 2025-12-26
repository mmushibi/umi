namespace UmiHealth.Core.Entities;

public class Product : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string NdcCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public bool RequiresPrescription { get; set; }
    public bool IsControlledSubstance { get; set; }
    public string StorageConditions { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
