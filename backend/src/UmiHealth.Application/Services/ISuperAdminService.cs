using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Application.Services
{
    public interface ISuperAdminService
    {
        // Dashboard
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        
        // Analytics
        Task<PaginatedResponseDto<AnalyticsDto>> GetAnalyticsAsync(AnalyticsFilterDto filter);
        Task<AnalyticsDto> GetAnalyticsByIdAsync(Guid id);
        Task<AnalyticsDto> GenerateAnalyticsAsync(DateTime date);
        
        // Logs
        Task<PaginatedResponseDto<SuperAdminLogDto>> GetLogsAsync(LogFilterDto filter);
        Task<SuperAdminLogDto> GetLogByIdAsync(Guid id);
        Task<SuperAdminLogDto> CreateLogAsync(string logLevel, string category, string message, string? details = null, string? userId = null, string? tenantId = null);
        Task<bool> ClearLogsAsync(DateTime? beforeDate = null);
        
        // Reports
        Task<PaginatedResponseDto<SuperAdminReportDto>> GetReportsAsync(int page = 1, int pageSize = 50);
        Task<SuperAdminReportDto> GetReportByIdAsync(Guid id);
        Task<SuperAdminReportDto> CreateReportAsync(CreateReportDto createDto, string createdBy);
        Task<SuperAdminReportDto> UpdateReportAsync(Guid id, CreateReportDto updateDto);
        Task<bool> DeleteReportAsync(Guid id);
        Task<byte[]> DownloadReportAsync(Guid id);
        Task<SuperAdminReportDto> GenerateReportAsync(Guid id);
        
        // Security Events
        Task<PaginatedResponseDto<SecurityEventDto>> GetSecurityEventsAsync(SecurityFilterDto filter);
        Task<SecurityEventDto> GetSecurityEventByIdAsync(Guid id);
        Task<SecurityEventDto> CreateSecurityEventAsync(string eventType, string severity, bool success, string? userId = null, string? tenantId = null, string? ipAddress = null, string? resource = null, string? action = null, string? failureReason = null);
        Task<bool> ClearSecurityEventsAsync(DateTime? beforeDate = null);
        
        // System Settings
        Task<List<SystemSettingDto>> GetSystemSettingsAsync(string? category = null);
        Task<SystemSettingDto> GetSystemSettingByKeyAsync(string key);
        Task<SystemSettingDto> UpdateSystemSettingAsync(string key, UpdateSystemSettingDto updateDto, string updatedBy);
        Task<SystemSettingDto> CreateSystemSettingAsync(SystemSettingDto settingDto);
        Task<bool> DeleteSystemSettingAsync(string key);
        
        // Super Admin Users
        Task<PaginatedResponseDto<SuperAdminUserDto>> GetSuperAdminUsersAsync(int page = 1, int pageSize = 50, string? search = null);
        Task<SuperAdminUserDto> GetSuperAdminUserByIdAsync(Guid id);
        Task<SuperAdminUserDto> CreateSuperAdminUserAsync(CreateSuperAdminUserDto createDto);
        Task<SuperAdminUserDto> UpdateSuperAdminUserAsync(Guid id, UpdateSuperAdminUserDto updateDto);
        Task<bool> DeleteSuperAdminUserAsync(Guid id);
        Task<bool> ToggleSuperAdminUserStatusAsync(Guid id);
        Task<string> ResetSuperAdminUserPasswordAsync(Guid id);
        Task<bool> EnableTwoFactorAsync(Guid id, string secret);
        Task<bool> DisableTwoFactorAsync(Guid id);
        
        // System Notifications
        Task<PaginatedResponseDto<SystemNotificationDto>> GetSystemNotificationsAsync(int page = 1, int pageSize = 50);
        Task<SystemNotificationDto> GetSystemNotificationByIdAsync(Guid id);
        Task<SystemNotificationDto> CreateSystemNotificationAsync(CreateSystemNotificationDto createDto, string createdBy);
        Task<SystemNotificationDto> UpdateSystemNotificationAsync(Guid id, CreateSystemNotificationDto updateDto);
        Task<bool> DeleteSystemNotificationAsync(Guid id);
        Task<bool> ToggleSystemNotificationStatusAsync(Guid id);
        Task<List<SystemNotificationDto>> GetActiveNotificationsAsync(string? userId = null, string? tenantId = null);
        
        // Backup Management
        Task<PaginatedResponseDto<BackupRecordDto>> GetBackupsAsync(int page = 1, int pageSize = 50, string? tenantId = null);
        Task<BackupRecordDto> GetBackupByIdAsync(Guid id);
        Task<BackupRecordDto> CreateBackupAsync(CreateBackupDto createDto, string createdBy);
        Task<bool> DeleteBackupAsync(Guid id);
        Task<byte[]> DownloadBackupAsync(Guid id);
        Task<BackupRecordDto> RestoreBackupAsync(Guid id);
        Task<bool> ScheduleBackupAsync(CreateBackupDto createDto, string schedule);
        
        // API Keys
        Task<PaginatedResponseDto<ApiKeyDto>> GetApiKeysAsync(int page = 1, int pageSize = 50);
        Task<ApiKeyDto> GetApiKeyByIdAsync(Guid id);
        Task<(ApiKeyDto apiKey, string plainKey)> CreateApiKeyAsync(CreateApiKeyDto createDto, string createdBy);
        Task<ApiKeyDto> UpdateApiKeyAsync(Guid id, CreateApiKeyDto updateDto);
        Task<bool> DeleteApiKeyAsync(Guid id);
        Task<bool> ToggleApiKeyStatusAsync(Guid id);
        Task<bool> RegenerateApiKeyAsync(Guid id);
        
        // System Health
        Task<Dictionary<string, object>> GetSystemHealthAsync();
        Task<Dictionary<string, object>> GetSystemMetricsAsync();
        Task<List<string>> GetSystemWarningsAsync();
        
        // Data Export/Import
        Task<byte[]> ExportSystemDataAsync(string type, Dictionary<string, object> parameters);
        Task<bool> ImportSystemDataAsync(string type, byte[] data, Dictionary<string, object> parameters);
        
        // Tenant Management (Super Admin Level)
        Task<bool> SuspendTenantAsync(Guid tenantId, string reason);
        Task<bool> UnsuspendTenantAsync(Guid tenantId);
        Task<bool> DeleteTenantAsync(Guid tenantId);
        Task<bool> ResetTenantPasswordAsync(Guid tenantId, string email);
        
        // User Management (Super Admin Level)
        Task<bool> SuspendUserAsync(Guid userId, string reason);
        Task<bool> UnsuspendUserAsync(Guid userId);
        Task<bool> ResetUserPasswordAsync(Guid userId);
        Task<bool> ForceLogoutUserAsync(Guid userId);
    }
}
