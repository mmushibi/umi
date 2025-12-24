using System;
using System.Collections.Generic;

namespace UmiHealth.Domain.Entities
{
    // Tenant-specific entities
    public class Patient : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public Dictionary<string, object> EmergencyContact { get; set; } = new();
        public Dictionary<string, object> MedicalHistory { get; set; } = new();
        public Dictionary<string, object> Allergies { get; set; } = new();
        public Dictionary<string, object> InsuranceInfo { get; set; } = new();
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }

    public class Product : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Manufacturer { get; set; }
        public string? Strength { get; set; }
        public string? Form { get; set; }
        public bool RequiresPrescription { get; set; } = false;
        public bool ControlledSubstance { get; set; } = false;
        public Dictionary<string, object> StorageRequirements { get; set; } = new();
        public Dictionary<string, object> Pricing { get; set; } = new();
        public string? Barcode { get; set; }
        public List<object> Images { get; set; } = new();
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }

    public class Inventory : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Guid ProductId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int QuantityOnHand { get; set; } = 0;
        public int QuantityReserved { get; set; } = 0;
        public int ReorderLevel { get; set; } = 0;
        public int ReorderQuantity { get; set; } = 0;
        public decimal? CostPrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public Guid? SupplierId { get; set; }
        public string? Location { get; set; }
        public DateTime? LastCounted { get; set; }
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Prescription : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Guid PatientId { get; set; }
        public Guid PrescriberId { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;
        public DateTime DatePrescribed { get; set; }
        public string Status { get; set; } = "pending";
        public string? Notes { get; set; }
        public string? Diagnosis { get; set; }
        public List<object> Items { get; set; } = new();
        public List<object> DispensedItems { get; set; } = new();
        public Guid? PharmacistId { get; set; }
        public DateTime? DispensedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }

    public class Sale : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public Guid? PatientId { get; set; }
        public Guid CashierId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string PaymentStatus { get; set; } = "pending";
        public List<object> Items { get; set; } = new();
        public List<Guid> Prescriptions { get; set; } = new();
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }

    public class Payment : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Guid? SaleId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string? TransactionReference { get; set; }
        public string? PaymentGateway { get; set; }
        public Dictionary<string, object> GatewayResponse { get; set; } = new();
        public string Status { get; set; } = "pending";
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }

    public class AuditLog : ISoftDeletable
    {
        public Guid Id { get; set; }
        public Guid? BranchId { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public Dictionary<string, object>? OldValues { get; set; }
        public Dictionary<string, object>? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public interface ISoftDeletable
    {
        public DateTime? DeletedAt { get; set; }
    }
}
