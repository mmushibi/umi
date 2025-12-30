namespace UmiHealth.Core.Entities;

public class Payment : TenantEntity
{
    public Guid SaleId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Mobile, Insurance
    public string PaymentStatus { get; set; } = "Completed"; // Pending, Completed, Failed, Refunded
    public DateTime PaymentDate { get; set; }
    public string? TransactionReference { get; set; }
    public string? PaymentGateway { get; set; }
    public string? GatewayResponse { get; set; }
    public string Status { get; set; } = "pending";
    public string Currency { get; set; } = "ZMW";
    public string? CardLastFour { get; set; }
    public string? MobileNumber { get; set; }
    public string? Notes { get; set; }
    
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Sale Sale { get; set; } = null!;
}
