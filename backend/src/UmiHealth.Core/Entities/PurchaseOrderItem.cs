namespace UmiHealth.Core.Entities;

public class PurchaseOrderItem : TenantEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int QuantityReceived { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
