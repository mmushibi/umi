namespace UmiHealth.Shared.DTOs;

public class SaleDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public PatientDto Patient { get; set; } = new();
    public UserDto? Cashier { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? PrescriptionNumber { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSaleDto
{
    public Guid BranchId { get; set; }
    public Guid PatientId { get; set; }
    public List<CreateSaleItemDto> Items { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal? DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public string? PrescriptionNumber { get; set; }
}

public class SaleItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class CreateSaleItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? DiscountPercentage { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string? CardLastFour { get; set; }
    public string? MobileNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProcessPaymentDto
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string? CardLastFour { get; set; }
    public string? MobileNumber { get; set; }
}

public class SaleReturnDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProcessReturnDto
{
    public Guid SaleId { get; set; }
    public List<ReturnItemDto> Items { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ReturnItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ReceiptDto
{
    public Guid SaleId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public PatientInfoDto Patient { get; set; } = new();
    public BranchInfoDto Branch { get; set; } = new();
    public List<ReceiptItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public CashierInfoDto Cashier { get; set; } = new();
}

public class ReceiptItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
}

public class PatientInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class BranchInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class CashierInfoDto
{
    public string Name { get; set; } = string.Empty;
}

public class SaleFilterDto : PagedRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PatientName { get; set; }
    public string? SaleNumber { get; set; }
    public Guid? BranchId { get; set; }
}
