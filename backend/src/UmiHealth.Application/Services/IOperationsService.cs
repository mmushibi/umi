using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IOperationsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<IEnumerable<RecentTenantDto>> GetRecentTenantsAsync(int count);
        Task<PagedResult<UmiHealth.Shared.DTOs.TenantDto>> GetTenantsAsync(int page, int pageSize, string? search = null, string? status = null);
        Task<UmiHealth.Shared.DTOs.TenantDto> CreateTenantAsync(CreateTenantRequest request);
        Task<UmiHealth.Shared.DTOs.TenantDto?> UpdateTenantAsync(Guid id, UpdateTenantRequest request);
        Task<PagedResult<UmiHealth.Shared.DTOs.UserDto>> GetUsersAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null);
        Task<UmiHealth.Shared.DTOs.UserDto?> UpdateUserAsync(Guid id, UmiHealth.Core.Interfaces.UpdateUserRequest request);
        Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null);
        Task<SubscriptionDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequest request);
        Task<SubscriptionDto?> UpgradeSubscriptionAsync(Guid id, UpgradeSubscriptionRequest request);
        Task<PagedResult<TransactionDto>> GetTransactionsAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<TransactionReceiptDto?> GenerateTransactionReceiptAsync(Guid id);
        Task<SyncStatusDto> GetSyncStatusAsync();
        Task TriggerSyncAsync(string syncType);
    }
}
