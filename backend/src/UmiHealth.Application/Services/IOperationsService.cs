using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IOperationsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<IEnumerable<RecentTenantDto>> GetRecentTenantsAsync(int count);
        Task<PagedResult<TenantDto>> GetTenantsAsync(int page, int pageSize, string? search = null, string? status = null);
        Task<TenantDto> CreateTenantAsync(CreateTenantRequest request);
        Task<TenantDto?> UpdateTenantAsync(Guid id, UpdateTenantRequest request);
        Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null);
        Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task<PagedResult<SubscriptionDto>> GetSubscriptionsAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null);
        Task<SubscriptionDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequest request);
        Task<SubscriptionDto?> UpgradeSubscriptionAsync(Guid id, UpgradeSubscriptionRequest request);
        Task<PagedResult<TransactionDto>> GetTransactionsAsync(int page, int pageSize, string? search = null, string? status = null, string? tenantId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<TransactionReceiptDto?> GenerateTransactionReceiptAsync(Guid id);
        Task<SyncStatusDto> GetSyncStatusAsync();
        Task TriggerSyncAsync(string syncType);
    }
}
