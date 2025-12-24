namespace UmiHealth.Domain.Entities;

public class Sale : TenantEntity
{
    public Guid BranchId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public Guid PatientId { get; set; }
    public Guid? CashierId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Insurance, Mobile
    public string PaymentStatus { get; set; } = string.Empty; // Pending, Paid, Partial, Refunded
    public string Status { get; set; } = string.Empty; // Completed, Cancelled, Returned
    public string? Notes { get; set; }
    public string? PrescriptionNumber { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
    public virtual User? Cashier { get; set; }
    public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();
}

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
    public DateTime? ExpiryDate { get; set; }

    public virtual Sale Sale { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class Payment : TenantEntity
{
    public Guid SaleId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string? CardLastFour { get; set; }
    public string? MobileNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty; // Completed, Failed, Refunded

    public virtual Sale Sale { get; set; } = null!;
}

public class SaleReturn : TenantEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Completed
    public string? Notes { get; set; }

    public virtual Sale Sale { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
