using System;
using System.Collections.Generic;

namespace UmiHealth.Application.DTOs.Pharmacy
{
    public class UpdatePharmacySettingsRequest
    {
        public string? SettingName { get; set; }
        public string? SettingValue { get; set; }
        public string? Description { get; set; }
    }

    public class CreateSupplierRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public List<string> Categories { get; set; } = new();
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }

    public class UpdateSupplierRequest
    {
        public string? Name { get; set; }
        public string? ContactPerson { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public List<string>? Categories { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateProcurementOrderRequest
    {
        public Guid SupplierId { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public string? Notes { get; set; }
        public List<CreateProcurementItemRequest> Items { get; set; } = new();
    }

    public class CreateProcurementItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class ReceiveProcurementOrderRequest
    {
        public Guid OrderId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; }
        public string? Notes { get; set; }
        public List<ReceiveItemRequest> Items { get; set; } = new();
    }

    public class ReceiveItemRequest
    {
        public Guid OrderItemId { get; set; }
        public int ReceivedQuantity { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? ActualUnitCost { get; set; }
        public string? Notes { get; set; }
    }

    public class ProcurementOrderItem
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = "pending";
        
        // Additional properties for pharmacy service
        public string Strength { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public decimal ActualUnitCost { get; set; }
    }
}
