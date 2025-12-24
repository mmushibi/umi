using System;
using System.Collections.Generic;

namespace UmiHealth.Application.DTOs
{
    // Pharmacy Settings DTOs
    public class PharmacySettingsDto
    {
        public Guid TenantId { get; set; }
        public string? PharmacyName { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public Dictionary<string, object> OperatingHours { get; set; } = new();
        public Dictionary<string, object> ZamraSettings { get; set; } = new();
        public Dictionary<string, object> TaxSettings { get; set; } = new();
    }

    // Supplier DTOs
    public class SupplierDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public Dictionary<string, object> PaymentTerms { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Procurement Order DTOs
    public class ProcurementOrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public string? Notes { get; set; }
        public List<ProcurementItemDto> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ProcurementItemDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? Manufacturer { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    // Compliance Report DTOs
    public class ComplianceReportDto
    {
        public object Period { get; set; } = new();
        public int TotalPrescriptions { get; set; }
        public int ControlledSubstanceTransactions { get; set; }
        public int ExpiringProductsCount { get; set; }
        public List<ExpiringProductDto> ExpiringProducts { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class ExpiringProductDto
    {
        public string? ProductName { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public Guid BranchId { get; set; }
    }
}
