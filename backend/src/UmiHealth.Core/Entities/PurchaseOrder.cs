namespace UmiHealth.Core.Entities;

public class PurchaseOrder : TenantEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Received, Cancelled
    public decimal TotalAmount { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
