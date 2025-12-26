namespace UmiHealth.Core.Entities;

public class StockTransaction : TenantEntity
{
    public Guid InventoryId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // IN, OUT, TRANSFER
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public Guid? FromBranchId { get; set; }
    public Guid? ToBranchId { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Inventory Inventory { get; set; } = null!;
    public virtual Branch? FromBranch { get; set; }
    public virtual Branch? ToBranch { get; set; }
}
