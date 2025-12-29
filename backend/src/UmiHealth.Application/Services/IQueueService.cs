using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs.Queue;

namespace UmiHealth.Application.Services
{
    public interface IQueueService
    {
        Task<QueueDataResponse> GetCurrentQueueAsync(Guid tenantId, Guid branchId);
        Task<QueueStatsResponse> GetQueueStatsAsync(Guid tenantId, Guid branchId);
        Task<QueuePatientResponse> AddPatientToQueueAsync(Guid tenantId, Guid branchId, AddPatientRequest request, string userId);
        Task ServePatientAsync(Guid tenantId, Guid branchId, Guid patientId, string userId);
        Task RemovePatientFromQueueAsync(Guid tenantId, Guid branchId, Guid patientId, string userId);
        Task ClearQueueAsync(Guid tenantId, Guid branchId, string userId);
        Task<QueuePatientResponse> CallNextPatientAsync(Guid tenantId, Guid branchId, string userId);
        Task CompletePatientServiceAsync(Guid tenantId, Guid branchId, Guid patientId, string userId);
        Task UpdatePatientPositionAsync(Guid tenantId, Guid branchId, Guid patientId, int newPosition, string userId);
        Task<QueueHistoryResponse> GetQueueHistoryAsync(Guid tenantId, Guid branchId, QueueHistoryFilters filters);
        Task<EmergencyQueueResponse> GetEmergencyQueueAsync(Guid tenantId, Guid branchId);
        Task<ProvidersResponse> GetProvidersAsync(Guid tenantId, Guid branchId);
        Task AssignProviderAsync(Guid tenantId, Guid branchId, Guid patientId, Guid providerId, string userId);
        Task UpdatePatientPriorityAsync(Guid tenantId, Guid branchId, Guid patientId, string priority, string userId);
        Task BulkServePatientsAsync(Guid tenantId, Guid branchId, List<Guid> patientIds, string userId);
        Task BulkRemovePatientsAsync(Guid tenantId, Guid branchId, List<Guid> patientIds, string userId);
        Task<byte[]> ExportQueueAsync(Guid tenantId, Guid branchId, QueueExportFilters filters);
        Task PrintDailyReportAsync(Guid tenantId, Guid branchId, DateTime date);
        Task PrintQueueSlipAsync(Guid tenantId, Guid branchId, Guid patientId);
    }
}
