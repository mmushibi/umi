using System;

namespace UmiHealth.Domain.Entities;

public class SaleReturn : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid SaleId { get; set; }
    public Guid CashierId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
