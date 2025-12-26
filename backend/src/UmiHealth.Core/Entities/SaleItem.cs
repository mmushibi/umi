namespace UmiHealth.Core.Entities;

public class SaleItem : TenantEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? BatchNumber { get; set; }
    
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Sale Sale { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
