using UmiHealth.Core.Entities;

namespace UmiHealth.Core.Interfaces;

public interface IPharmacyService
{
    // Pharmacy Settings
    Task<PharmacySettings?> GetSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PharmacySettings> UpdateSettingsAsync(Guid tenantId, UpdatePharmacySettingsRequest request, CancellationToken cancellationToken = default);

    // Product Management
    Task<Product> CreateProductAsync(Guid tenantId, CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Product> UpdateProductAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetProductsAsync(Guid tenantId, ProductFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(Guid productId, Guid tenantId, CancellationToken cancellationToken = default);

    // Inventory Management
    Task<IReadOnlyList<Inventory>> GetInventoryAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
    Task<Inventory> UpdateInventoryAsync(Guid inventoryId, UpdateInventoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> StockTransferAsync(Guid tenantId, StockTransferRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockTransaction>> GetStockTransactionsAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);

    // Supplier Management
    Task<Supplier> CreateSupplierAsync(Guid tenantId, CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<Supplier> UpdateSupplierAsync(Guid supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public record PharmacySettings(
    Guid Id,
    Guid TenantId,
    string PharmacyName,
    string PharmacyLicense,
    string Address,
    string Phone,
    string Email,
    decimal DefaultTaxRate,
    bool EnablePrescriptionValidation,
    bool EnableLowStockAlerts,
    int DefaultReorderLevel,
    string Currency,
    string TimeZone
);

public record UpdatePharmacySettingsRequest(
    string PharmacyName,
    string PharmacyLicense,
    string Address,
    string Phone,
    string Email,
    decimal DefaultTaxRate,
    bool EnablePrescriptionValidation,
    bool EnableLowStockAlerts,
    int DefaultReorderLevel,
    string Currency,
    string TimeZone
);

public record CreateProductRequest(
    string Name,
    string Description,
    string GenericName,
    string Brand,
    string Category,
    string DosageForm,
    string Strength,
    string Manufacturer,
    string NdcCode,
    string Barcode,
    bool RequiresPrescription,
    bool IsControlledSubstance,
    string StorageConditions,
    decimal UnitPrice,
    string? ImageUrl
);

public record UpdateProductRequest(
    string Name,
    string Description,
    string GenericName,
    string Brand,
    string Category,
    string DosageForm,
    string Strength,
    string Manufacturer,
    bool RequiresPrescription,
    bool IsControlledSubstance,
    string StorageConditions,
    decimal UnitPrice,
    string? ImageUrl
);

public record ProductFilter(
    string? Category,
    string? Brand,
    bool? RequiresPrescription,
    bool? IsActive,
    string? Search
);

public record UpdateInventoryRequest(
    int QuantityOnHand,
    int ReorderLevel,
    int MaxStockLevel,
    decimal UnitCost,
    decimal UnitPrice,
    string? BatchNumber,
    DateTime? ExpiryDate,
    string? Location
);

public record StockTransferRequest(
    Guid FromBranchId,
    Guid ToBranchId,
    Guid ProductId,
    int Quantity,
    string? Notes
);

public record CreateSupplierRequest(
    string Name,
    string ContactPerson,
    string Email,
    string Phone,
    string Address,
    string City,
    string Country,
    string PostalCode,
    string TaxId,
    string PaymentTerms
);

public record UpdateSupplierRequest(
    string Name,
    string ContactPerson,
    string Email,
    string Phone,
    string Address,
    string City,
    string Country,
    string PostalCode,
    string TaxId,
    string PaymentTerms
);
