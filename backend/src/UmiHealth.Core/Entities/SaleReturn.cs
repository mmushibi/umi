namespace UmiHealth.Core.Entities;

public class SaleReturn : TenantEntity
{
    public Guid SaleId { get; set; }
    public Guid PatientId { get; set; }
    public Guid CashierId { get; set; }
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Sale Sale { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
    public virtual User Cashier { get; set; } = null!;
}
