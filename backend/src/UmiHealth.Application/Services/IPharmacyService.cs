using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IPharmacyService
    {
        Task<PharmacySettingsDto> GetPharmacySettingsAsync(Guid tenantId);
        Task<PharmacySettingsDto> UpdatePharmacySettingsAsync(Guid tenantId, UpdatePharmacySettingsRequest request);
        Task<IEnumerable<SupplierDto>> GetSuppliersAsync(Guid tenantId);
        Task<SupplierDto> GetSupplierByIdAsync(Guid tenantId, Guid supplierId);
        Task<SupplierDto> CreateSupplierAsync(Guid tenantId, CreateSupplierRequest request);
        Task<SupplierDto> UpdateSupplierAsync(Guid tenantId, Guid supplierId, UpdateSupplierRequest request);
        Task<bool> DeleteSupplierAsync(Guid tenantId, Guid supplierId);
        Task<IEnumerable<ProcurementOrderDto>> GetProcurementOrdersAsync(Guid tenantId);
        Task<ProcurementOrderDto> CreateProcurementOrderAsync(Guid tenantId, CreateProcurementOrderRequest request);
        Task<ProcurementOrderDto> ReceiveProcurementOrderAsync(Guid tenantId, Guid orderId, ReceiveProcurementOrderRequest request);
        Task<ComplianceReportDto> GetComplianceReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }
}
