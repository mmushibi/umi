namespace UmiHealth.Core.Entities;

public class Sale : TenantEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid CashierId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? Notes { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string Status { get; set; } = "Completed"; // Pending, Completed, Cancelled, Refunded
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
    public virtual User Cashier { get; set; } = null!;
    public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
