using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace UmiHealth.Application.Services
{
    public class SuperAdminService : ISuperAdminService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<SuperAdminService> _logger;
        private readonly ICrossPortalSyncService _crossPortalSync;

        public SuperAdminService(
            SharedDbContext context,
            ILogger<SuperAdminService> logger,
            ICrossPortalSyncService crossPortalSync)
        {
            _context = context;
            _logger = logger;
            _crossPortalSync = crossPortalSync;
        }

        // Dashboard
        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            var totalTenants = await _context.Tenants.CountAsync();
            var activeTenants = await _context.Tenants.CountAsync(t => t.Status == "active");
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive && u.LastLogin.HasValue && u.LastLogin > thirtyDaysAgo);
            
            // Calculate total revenue (mock implementation)
            var totalRevenue = await _context.Subscriptions
                .Where(s => s.Status == "active")
                .SumAsync(s => s.Amount);

            var criticalSecurityEvents = await _context.SecurityEvents
                .CountAsync(e => e.Severity == "critical" && e.CreatedAt > thirtyDaysAgo);

            var pendingReports = await _context.SuperAdminReports
                .CountAsync(r => r.Status == "generating");

            var failedBackups = await _context.BackupRecords
                .CountAsync(b => b.Status == "failed" && b.CreatedAt > thirtyDaysAgo);

            var recentAnalytics = await _context.SystemAnalytics
                .OrderByDescending(a => a.Date)
                .Take(7)
                .Select(a => new AnalyticsDto
                {
                    Date = a.Date,
                    ActiveTenants = a.ActiveTenants,
                    TotalUsers = a.TotalUsers,
                    ActiveUsers = a.ActiveUsers,
                    TotalTransactions = a.TotalTransactions,
                    TotalRevenue = a.TotalRevenue,
                    TenantStats = a.TenantStats,
                    UserRoleStats = a.UserRoleStats,
                    ApiUsageStats = a.ApiUsageStats,
                    PerformanceMetrics = a.PerformanceMetrics
                })
                .ToListAsync();

            var recentSecurityEvents = await _context.SecurityEvents
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .Select(e => new SecurityEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    Severity = e.Severity,
                    UserId = e.UserId,
                    TenantId = e.TenantId,
                    IpAddress = e.IpAddress,
                    Resource = e.Resource,
                    Action = e.Action,
                    Success = e.Success,
                    FailureReason = e.FailureReason,
                    Details = e.Details,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            var recentLogs = await _context.SuperAdminLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .Select(l => new SuperAdminLogDto
                {
                    Id = l.Id,
                    LogLevel = l.LogLevel,
                    Category = l.Category,
                    Message = l.Message,
                    Details = l.Details,
                    UserId = l.UserId,
                    TenantId = l.TenantId,
                    IpAddress = l.IpAddress,
                    UserAgent = l.UserAgent,
                    Metadata = l.Metadata,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new DashboardSummaryDto
            {
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalRevenue = totalRevenue,
                TotalTransactions = 0, // Would need transaction table
                CriticalSecurityEvents = criticalSecurityEvents,
                PendingReports = pendingReports,
                FailedBackups = failedBackups,
                RecentAnalytics = recentAnalytics,
                RecentSecurityEvents = recentSecurityEvents,
                RecentLogs = recentLogs
            };
        }

        // Analytics
        public async Task<PaginatedResponseDto<AnalyticsDto>> GetAnalyticsAsync(AnalyticsFilterDto filter)
        {
            var query = _context.SystemAnalytics.AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(a => a.Date >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(a => a.Date <= filter.EndDate.Value);

            var totalCount = await query.CountAsync();
            var page = filter.Page;
            var pageSize = filter.PageSize;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var analytics = await query
                .OrderByDescending(a => a.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AnalyticsDto
                {
                    Date = a.Date,
                    ActiveTenants = a.ActiveTenants,
                    TotalUsers = a.TotalUsers,
                    ActiveUsers = a.ActiveUsers,
                    TotalTransactions = a.TotalTransactions,
                    TotalRevenue = a.TotalRevenue,
                    TenantStats = a.TenantStats,
                    UserRoleStats = a.UserRoleStats,
                    ApiUsageStats = a.ApiUsageStats,
                    PerformanceMetrics = a.PerformanceMetrics
                })
                .ToListAsync();

            return new PaginatedResponseDto<AnalyticsDto>
            {
                Data = analytics,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<AnalyticsDto> GetAnalyticsByIdAsync(Guid id)
        {
            var analytics = await _context.SystemAnalytics.FindAsync(id);
            if (analytics == null) return null;

            return new AnalyticsDto
            {
                Date = analytics.Date,
                ActiveTenants = analytics.ActiveTenants,
                TotalUsers = analytics.TotalUsers,
                ActiveUsers = analytics.ActiveUsers,
                TotalTransactions = analytics.TotalTransactions,
                TotalRevenue = analytics.TotalRevenue,
                TenantStats = analytics.TenantStats,
                UserRoleStats = analytics.UserRoleStats,
                ApiUsageStats = analytics.ApiUsageStats,
                PerformanceMetrics = analytics.PerformanceMetrics
            };
        }

        public async Task<AnalyticsDto> GenerateAnalyticsAsync(DateTime date)
        {
            var existing = await _context.SystemAnalytics
                .FirstOrDefaultAsync(a => a.Date.Date == date.Date);

            if (existing != null)
            {
                return new AnalyticsDto
                {
                    Date = existing.Date,
                    ActiveTenants = existing.ActiveTenants,
                    TotalUsers = existing.TotalUsers,
                    ActiveUsers = existing.ActiveUsers,
                    TotalTransactions = existing.TotalTransactions,
                    TotalRevenue = existing.TotalRevenue,
                    TenantStats = existing.TenantStats,
                    UserRoleStats = existing.UserRoleStats,
                    ApiUsageStats = existing.ApiUsageStats,
                    PerformanceMetrics = existing.PerformanceMetrics
                };
            }

            var analytics = new SystemAnalytics
            {
                Id = Guid.NewGuid(),
                Date = date.Date,
                ActiveTenants = await _context.Tenants.CountAsync(t => t.Status == "active"),
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                TotalTransactions = 0, // Would need transaction table
                TotalRevenue = await _context.Subscriptions
                    .Where(s => s.Status == "active")
                    .SumAsync(s => s.Amount),
                TenantStats = new Dictionary<string, int>(),
                UserRoleStats = new Dictionary<string, int>(),
                ApiUsageStats = new Dictionary<string, int>(),
                PerformanceMetrics = new Dictionary<string, object>()
            };

            _context.SystemAnalytics.Add(analytics);
            await _context.SaveChangesAsync();

            return new AnalyticsDto
            {
                Date = analytics.Date,
                ActiveTenants = analytics.ActiveTenants,
                TotalUsers = analytics.TotalUsers,
                ActiveUsers = analytics.ActiveUsers,
                TotalTransactions = analytics.TotalTransactions,
                TotalRevenue = analytics.TotalRevenue,
                TenantStats = analytics.TenantStats,
                UserRoleStats = analytics.UserRoleStats,
                ApiUsageStats = analytics.ApiUsageStats,
                PerformanceMetrics = analytics.PerformanceMetrics
            };
        }

        // Logs
        public async Task<PaginatedResponseDto<SuperAdminLogDto>> GetLogsAsync(LogFilterDto filter)
        {
            var query = _context.SuperAdminLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filter.LogLevel))
                query = query.Where(l => l.LogLevel == filter.LogLevel);

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(l => l.Category == filter.Category);

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(l => l.Message.Contains(filter.Search));

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(l => l.UserId == filter.UserId);

            if (!string.IsNullOrEmpty(filter.TenantId))
                query = query.Where(l => l.TenantId == filter.TenantId);

            var totalCount = await query.CountAsync();
            var page = filter.Page;
            var pageSize = filter.PageSize;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new SuperAdminLogDto
                {
                    Id = l.Id,
                    LogLevel = l.LogLevel,
                    Category = l.Category,
                    Message = l.Message,
                    Details = l.Details,
                    UserId = l.UserId,
                    TenantId = l.TenantId,
                    IpAddress = l.IpAddress,
                    UserAgent = l.UserAgent,
                    Metadata = l.Metadata,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<SuperAdminLogDto>
            {
                Data = logs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<SuperAdminLogDto> GetLogByIdAsync(Guid id)
        {
            var log = await _context.SuperAdminLogs.FindAsync(id);
            if (log == null) return null;

            return new SuperAdminLogDto
            {
                Id = log.Id,
                LogLevel = log.LogLevel,
                Category = log.Category,
                Message = log.Message,
                Details = log.Details,
                UserId = log.UserId,
                TenantId = log.TenantId,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                Metadata = log.Metadata,
                CreatedAt = log.CreatedAt
            };
        }

        public async Task<SuperAdminLogDto> CreateLogAsync(string logLevel, string category, string message, string? details = null, string? userId = null, string? tenantId = null)
        {
            var log = new SuperAdminLog
            {
                Id = Guid.NewGuid(),
                LogLevel = logLevel,
                Category = category,
                Message = message,
                Details = details,
                UserId = userId,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SuperAdminLogs.Add(log);
            await _context.SaveChangesAsync();

            return new SuperAdminLogDto
            {
                Id = log.Id,
                LogLevel = log.LogLevel,
                Category = log.Category,
                Message = log.Message,
                Details = log.Details,
                UserId = log.UserId,
                TenantId = log.TenantId,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                Metadata = log.Metadata,
                CreatedAt = log.CreatedAt
            };
        }

        public async Task<bool> ClearLogsAsync(DateTime? beforeDate = null)
        {
            var query = _context.SuperAdminLogs.AsQueryable();

            if (beforeDate.HasValue)
                query = query.Where(l => l.CreatedAt < beforeDate.Value);

            _context.SuperAdminLogs.RemoveRange(query);
            await _context.SaveChangesAsync();

            return true;
        }

        // Reports
        public async Task<PaginatedResponseDto<SuperAdminReportDto>> GetReportsAsync(int page = 1, int pageSize = 50)
        {
            var query = _context.SuperAdminReports.AsQueryable();
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var reports = await query
                .OrderByDescending(r => r.GeneratedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new SuperAdminReportDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    Description = r.Description,
                    Status = r.Status,
                    Parameters = r.Parameters,
                    Results = r.Results,
                    FilePath = r.FilePath,
                    GeneratedBy = r.GeneratedBy,
                    GeneratedAt = r.GeneratedAt,
                    CompletedAt = r.CompletedAt,
                    ExpiresAt = r.ExpiresAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<SuperAdminReportDto>
            {
                Data = reports,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<SuperAdminReportDto> GetReportByIdAsync(Guid id)
        {
            var report = await _context.SuperAdminReports.FindAsync(id);
            if (report == null) return null;

            return new SuperAdminReportDto
            {
                Id = report.Id,
                Name = report.Name,
                Type = report.Type,
                Description = report.Description,
                Status = report.Status,
                Parameters = report.Parameters,
                Results = report.Results,
                FilePath = report.FilePath,
                GeneratedBy = report.GeneratedBy,
                GeneratedAt = report.GeneratedAt,
                CompletedAt = report.CompletedAt,
                ExpiresAt = report.ExpiresAt
            };
        }

        public async Task<SuperAdminReportDto> CreateReportAsync(CreateReportDto createDto, string createdBy)
        {
            var report = new SuperAdminReport
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Type = createDto.Type,
                Description = createDto.Description,
                Status = "generating",
                Parameters = createDto.Parameters,
                GeneratedBy = createdBy,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = createDto.ExpiresAt
            };

            _context.SuperAdminReports.Add(report);
            await _context.SaveChangesAsync();

            return new SuperAdminReportDto
            {
                Id = report.Id,
                Name = report.Name,
                Type = report.Type,
                Description = report.Description,
                Status = report.Status,
                Parameters = report.Parameters,
                Results = report.Results,
                FilePath = report.FilePath,
                GeneratedBy = report.GeneratedBy,
                GeneratedAt = report.GeneratedAt,
                CompletedAt = report.CompletedAt,
                ExpiresAt = report.ExpiresAt
            };
        }

        public async Task<SuperAdminReportDto> UpdateReportAsync(Guid id, CreateReportDto updateDto)
        {
            var report = await _context.SuperAdminReports.FindAsync(id);
            if (report == null) return null;

            report.Name = updateDto.Name;
            report.Type = updateDto.Type;
            report.Description = updateDto.Description;
            report.Parameters = updateDto.Parameters;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SuperAdminReportDto
            {
                Id = report.Id,
                Name = report.Name,
                Type = report.Type,
                Description = report.Description,
                Status = report.Status,
                Parameters = report.Parameters,
                Results = report.Results,
                FilePath = report.FilePath,
                GeneratedBy = report.GeneratedBy,
                GeneratedAt = report.GeneratedAt,
                CompletedAt = report.CompletedAt,
                ExpiresAt = report.ExpiresAt
            };
        }

        public async Task<bool> DeleteReportAsync(Guid id)
        {
            var report = await _context.SuperAdminReports.FindAsync(id);
            if (report == null) return false;

            _context.SuperAdminReports.Remove(report);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<byte[]> DownloadReportAsync(Guid id)
        {
            var report = await _context.SuperAdminReports.FindAsync(id);
            if (report == null || string.IsNullOrEmpty(report.FilePath))
                return Array.Empty<byte>();

            // In a real implementation, this would read the file from storage
            return Array.Empty<byte>();
        }

        public async Task<SuperAdminReportDto> GenerateReportAsync(Guid id)
        {
            var report = await _context.SuperAdminReports.FindAsync(id);
            if (report == null) return null;

            // Generate report based on type and parameters
            report.Status = "completed";
            report.CompletedAt = DateTime.UtcNow;
            report.Results = new Dictionary<string, object>
            {
                ["generatedAt"] = DateTime.UtcNow,
                ["recordCount"] = 100 // Mock count
            };

            await _context.SaveChangesAsync();

            return new SuperAdminReportDto
            {
                Id = report.Id,
                Name = report.Name,
                Type = report.Type,
                Description = report.Description,
                Status = report.Status,
                Parameters = report.Parameters,
                Results = report.Results,
                FilePath = report.FilePath,
                GeneratedBy = report.GeneratedBy,
                GeneratedAt = report.GeneratedAt,
                CompletedAt = report.CompletedAt,
                ExpiresAt = report.ExpiresAt
            };
        }

        // Security Events
        public async Task<PaginatedResponseDto<SecurityEventDto>> GetSecurityEventsAsync(SecurityFilterDto filter)
        {
            var query = _context.SecurityEvents.AsQueryable();

            if (!string.IsNullOrEmpty(filter.EventType))
                query = query.Where(e => e.EventType == filter.EventType);

            if (!string.IsNullOrEmpty(filter.Severity))
                query = query.Where(e => e.Severity == filter.Severity);

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(e => e.UserId == filter.UserId);

            if (!string.IsNullOrEmpty(filter.TenantId))
                query = query.Where(e => e.TenantId == filter.TenantId);

            if (filter.Success.HasValue)
                query = query.Where(e => e.Success == filter.Success.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(e => e.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(e => e.CreatedAt <= filter.EndDate.Value);

            var totalCount = await query.CountAsync();
            var page = filter.Page;
            var pageSize = filter.PageSize;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new SecurityEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    Severity = e.Severity,
                    UserId = e.UserId,
                    TenantId = e.TenantId,
                    IpAddress = e.IpAddress,
                    Resource = e.Resource,
                    Action = e.Action,
                    Success = e.Success,
                    FailureReason = e.FailureReason,
                    Details = e.Details,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<SecurityEventDto>
            {
                Data = events,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<SecurityEventDto> GetSecurityEventByIdAsync(Guid id)
        {
            var securityEvent = await _context.SecurityEvents.FindAsync(id);
            if (securityEvent == null) return null;

            return new SecurityEventDto
            {
                Id = securityEvent.Id,
                EventType = securityEvent.EventType,
                Severity = securityEvent.Severity,
                UserId = securityEvent.UserId,
                TenantId = securityEvent.TenantId,
                IpAddress = securityEvent.IpAddress,
                Resource = securityEvent.Resource,
                Action = securityEvent.Action,
                Success = securityEvent.Success,
                FailureReason = securityEvent.FailureReason,
                Details = securityEvent.Details,
                CreatedAt = securityEvent.CreatedAt
            };
        }

        public async Task<SecurityEventDto> CreateSecurityEventAsync(string eventType, string severity, bool success, string? userId = null, string? tenantId = null, string? ipAddress = null, string? resource = null, string? action = null, string? failureReason = null)
        {
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Severity = severity,
                UserId = userId,
                TenantId = tenantId,
                IpAddress = ipAddress,
                Resource = resource,
                Action = action,
                Success = success,
                FailureReason = failureReason,
                CreatedAt = DateTime.UtcNow
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            return new SecurityEventDto
            {
                Id = securityEvent.Id,
                EventType = securityEvent.EventType,
                Severity = securityEvent.Severity,
                UserId = securityEvent.UserId,
                TenantId = securityEvent.TenantId,
                IpAddress = securityEvent.IpAddress,
                Resource = securityEvent.Resource,
                Action = securityEvent.Action,
                Success = securityEvent.Success,
                FailureReason = securityEvent.FailureReason,
                Details = securityEvent.Details,
                CreatedAt = securityEvent.CreatedAt
            };
        }

        public async Task<bool> ClearSecurityEventsAsync(DateTime? beforeDate = null)
        {
            var query = _context.SecurityEvents.AsQueryable();

            if (beforeDate.HasValue)
                query = query.Where(e => e.CreatedAt < beforeDate.Value);

            _context.SecurityEvents.RemoveRange(query);
            await _context.SaveChangesAsync();

            return true;
        }

        // System Settings
        public async Task<List<SystemSettingDto>> GetSystemSettingsAsync(string? category = null)
        {
            var query = _context.SystemSettings.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category == category);

            var settings = await query
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Key)
                .Select(s => new SystemSettingDto
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.Value,
                    Category = s.Category,
                    DataType = s.DataType,
                    Description = s.Description,
                    IsPublic = s.IsPublic,
                    IsEditable = s.IsEditable,
                    ValidationRules = s.ValidationRules,
                    UpdatedBy = s.UpdatedBy,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return settings;
        }

        public async Task<SystemSettingDto> GetSystemSettingByKeyAsync(string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null) return null;

            return new SystemSettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Category = setting.Category,
                DataType = setting.DataType,
                Description = setting.Description,
                IsPublic = setting.IsPublic,
                IsEditable = setting.IsEditable,
                ValidationRules = setting.ValidationRules,
                UpdatedBy = setting.UpdatedBy,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<SystemSettingDto> UpdateSystemSettingAsync(string key, UpdateSystemSettingDto updateDto, string updatedBy)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null) return null;

            setting.Value = updateDto.Value;
            setting.UpdatedBy = updatedBy;
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SystemSettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Category = setting.Category,
                DataType = setting.DataType,
                Description = setting.Description,
                IsPublic = setting.IsPublic,
                IsEditable = setting.IsEditable,
                ValidationRules = setting.ValidationRules,
                UpdatedBy = setting.UpdatedBy,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<SystemSettingDto> CreateSystemSettingAsync(SystemSettingDto settingDto)
        {
            var setting = new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = settingDto.Key,
                Value = settingDto.Value,
                Category = settingDto.Category,
                DataType = settingDto.DataType,
                Description = settingDto.Description,
                IsPublic = settingDto.IsPublic,
                IsEditable = settingDto.IsEditable,
                ValidationRules = settingDto.ValidationRules,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SystemSettings.Add(setting);
            await _context.SaveChangesAsync();

            return new SystemSettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Category = setting.Category,
                DataType = setting.DataType,
                Description = setting.Description,
                IsPublic = setting.IsPublic,
                IsEditable = setting.IsEditable,
                ValidationRules = setting.ValidationRules,
                UpdatedBy = setting.UpdatedBy,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<bool> DeleteSystemSettingAsync(string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null) return false;

            _context.SystemSettings.Remove(setting);
            await _context.SaveChangesAsync();

            return true;
        }

        // Super Admin Users
        public async Task<PaginatedResponseDto<SuperAdminUserDto>> GetSuperAdminUsersAsync(int page = 1, int pageSize = 50, string? search = null)
        {
            var query = _context.SuperAdminUsers.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.Email.Contains(search) || 
                    u.FirstName.Contains(search) || 
                    u.LastName.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new SuperAdminUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    Permissions = u.Permissions,
                    IsActive = u.IsActive,
                    LastLogin = u.LastLogin,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    Preferences = u.Preferences,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<SuperAdminUserDto>
            {
                Data = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<SuperAdminUserDto> GetSuperAdminUserByIdAsync(Guid id)
        {
            var user = await _context.SuperAdminUsers.FindAsync(id);
            if (user == null) return null;

            return new SuperAdminUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Permissions = user.Permissions,
                IsActive = user.IsActive,
                LastLogin = user.LastLogin,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Preferences = user.Preferences,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<SuperAdminUserDto> CreateSuperAdminUserAsync(CreateSuperAdminUserDto createDto)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password);

            var user = new SuperAdminUser
            {
                Id = Guid.NewGuid(),
                Email = createDto.Email,
                PasswordHash = passwordHash,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                Role = createDto.Role,
                Permissions = createDto.Permissions,
                Preferences = createDto.Preferences,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SuperAdminUsers.Add(user);
            await _context.SaveChangesAsync();

            return new SuperAdminUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Permissions = user.Permissions,
                IsActive = user.IsActive,
                LastLogin = user.LastLogin,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Preferences = user.Preferences,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<SuperAdminUserDto> UpdateSuperAdminUserAsync(Guid id, UpdateSuperAdminUserDto updateDto)
        {
            var user = await _context.SuperAdminUsers.FindAsync(id);
            if (user == null) return null;

            if (updateDto.FirstName != null) user.FirstName = updateDto.FirstName;
            if (updateDto.LastName != null) user.LastName = updateDto.LastName;
            if (updateDto.Role != null) user.Role = updateDto.Role;
            if (updateDto.Permissions != null) user.Permissions = updateDto.Permissions;
            if (updateDto.IsActive.HasValue) user.IsActive = updateDto.IsActive.Value;
            if (updateDto.Preferences != null) user.Preferences = updateDto.Preferences;

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new SuperAdminUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Permissions = user.Permissions,
                IsActive = user.IsActive,
                LastLogin = user.LastLogin,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Preferences = user.Preferences,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<bool> DeleteSuperAdminUserAsync(Guid id)
        {
            var user = await _context.SuperAdminUsers.FindAsync(id);
            if (user == null) return false;

            _context.SuperAdminUsers.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleSuperAdminUserStatusAsync(Guid id)
        {
            var user = await _context.SuperAdminUsers.FindAsync(id);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string> ResetSuperAdminUserPasswordAsync(Guid id)
        {
            var user = await _context.SuperAdminUsers.FindAsync(id);
            if (user == null) return null;

            var newPassword = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return newPassword;
        }

        public async Task<bool> EnableTwoFactorAsync(Guid id, string secret)
        {
            var user = await _context.SuperAdminUsers.FindAsync(id);
            if (user == null) return false;

            user.TwoFactorEnabled = true;
            user.TwoFactorSecret = secret;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DisableTwoFactorAsync(Guid id)
        {
            var user = await _context.SuperAdminUsers.FindAsync(id);
            if (user == null) return false;

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // System Notifications
        public async Task<PaginatedResponseDto<SystemNotificationDto>> GetSystemNotificationsAsync(int page = 1, int pageSize = 50)
        {
            var query = _context.SystemNotifications.AsQueryable();
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new SystemNotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    TargetAudience = n.TargetAudience,
                    TargetTenants = n.TargetTenants,
                    TargetUsers = n.TargetUsers,
                    IsGlobal = n.IsGlobal,
                    IsActive = n.IsActive,
                    StartDate = n.StartDate,
                    EndDate = n.EndDate,
                    Metadata = n.Metadata,
                    CreatedBy = n.CreatedBy,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<SystemNotificationDto>
            {
                Data = notifications,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<SystemNotificationDto> GetSystemNotificationByIdAsync(Guid id)
        {
            var notification = await _context.SystemNotifications.FindAsync(id);
            if (notification == null) return null;

            return new SystemNotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                TargetAudience = notification.TargetAudience,
                TargetTenants = notification.TargetTenants,
                TargetUsers = notification.TargetUsers,
                IsGlobal = notification.IsGlobal,
                IsActive = notification.IsActive,
                StartDate = notification.StartDate,
                EndDate = notification.EndDate,
                Metadata = notification.Metadata,
                CreatedBy = notification.CreatedBy,
                CreatedAt = notification.CreatedAt,
                UpdatedAt = notification.UpdatedAt
            };
        }

        public async Task<SystemNotificationDto> CreateSystemNotificationAsync(CreateSystemNotificationDto createDto, string createdBy)
        {
            var notification = new SystemNotification
            {
                Id = Guid.NewGuid(),
                Title = createDto.Title,
                Message = createDto.Message,
                Type = createDto.Type,
                TargetAudience = createDto.TargetAudience,
                TargetTenants = createDto.TargetTenants,
                TargetUsers = createDto.TargetUsers,
                IsGlobal = createDto.IsGlobal,
                IsActive = true,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                Metadata = createDto.Metadata,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SystemNotifications.Add(notification);
            await _context.SaveChangesAsync();

            return new SystemNotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                TargetAudience = notification.TargetAudience,
                TargetTenants = notification.TargetTenants,
                TargetUsers = notification.TargetUsers,
                IsGlobal = notification.IsGlobal,
                IsActive = notification.IsActive,
                StartDate = notification.StartDate,
                EndDate = notification.EndDate,
                Metadata = notification.Metadata,
                CreatedBy = notification.CreatedBy,
                CreatedAt = notification.CreatedAt,
                UpdatedAt = notification.UpdatedAt
            };
        }

        public async Task<SystemNotificationDto> UpdateSystemNotificationAsync(Guid id, CreateSystemNotificationDto updateDto)
        {
            var notification = await _context.SystemNotifications.FindAsync(id);
            if (notification == null) return null;

            notification.Title = updateDto.Title;
            notification.Message = updateDto.Message;
            notification.Type = updateDto.Type;
            notification.TargetAudience = updateDto.TargetAudience;
            notification.TargetTenants = updateDto.TargetTenants;
            notification.TargetUsers = updateDto.TargetUsers;
            notification.IsGlobal = updateDto.IsGlobal;
            notification.StartDate = updateDto.StartDate;
            notification.EndDate = updateDto.EndDate;
            notification.Metadata = updateDto.Metadata;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SystemNotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                TargetAudience = notification.TargetAudience,
                TargetTenants = notification.TargetTenants,
                TargetUsers = notification.TargetUsers,
                IsGlobal = notification.IsGlobal,
                IsActive = notification.IsActive,
                StartDate = notification.StartDate,
                EndDate = notification.EndDate,
                Metadata = notification.Metadata,
                CreatedBy = notification.CreatedBy,
                CreatedAt = notification.CreatedAt,
                UpdatedAt = notification.UpdatedAt
            };
        }

        public async Task<bool> DeleteSystemNotificationAsync(Guid id)
        {
            var notification = await _context.SystemNotifications.FindAsync(id);
            if (notification == null) return false;

            _context.SystemNotifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleSystemNotificationStatusAsync(Guid id)
        {
            var notification = await _context.SystemNotifications.FindAsync(id);
            if (notification == null) return false;

            notification.IsActive = !notification.IsActive;
            notification.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<SystemNotificationDto>> GetActiveNotificationsAsync(string? userId = null, string? tenantId = null)
        {
            var now = DateTime.UtcNow;
            var query = _context.SystemNotifications
                .Where(n => n.IsActive && 
                    (!n.StartDate.HasValue || n.StartDate <= now) && 
                    (!n.EndDate.HasValue || n.EndDate >= now));

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(n => n.IsGlobal || n.TargetUsers.Contains(userId));

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(n => n.IsGlobal || n.TargetTenants.Contains(tenantId));

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new SystemNotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    TargetAudience = n.TargetAudience,
                    TargetTenants = n.TargetTenants,
                    TargetUsers = n.TargetUsers,
                    IsGlobal = n.IsGlobal,
                    IsActive = n.IsActive,
                    StartDate = n.StartDate,
                    EndDate = n.EndDate,
                    Metadata = n.Metadata,
                    CreatedBy = n.CreatedBy,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .ToListAsync();

            return notifications;
        }

        // Backup Management
        public async Task<PaginatedResponseDto<BackupRecordDto>> GetBackupsAsync(int page = 1, int pageSize = 50, string? tenantId = null)
        {
            var query = _context.BackupRecords.AsQueryable();

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(b => b.TenantId == tenantId);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var backups = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BackupRecordDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Type = b.Type,
                    Status = b.Status,
                    TenantId = b.TenantId,
                    FilePath = b.FilePath,
                    FileSize = b.FileSize,
                    Checksum = b.Checksum,
                    Configuration = b.Configuration,
                    ErrorMessage = b.ErrorMessage,
                    CreatedBy = b.CreatedBy,
                    CreatedAt = b.CreatedAt,
                    CompletedAt = b.CompletedAt,
                    ExpiresAt = b.ExpiresAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<BackupRecordDto>
            {
                Data = backups,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<BackupRecordDto> GetBackupByIdAsync(Guid id)
        {
            var backup = await _context.BackupRecords.FindAsync(id);
            if (backup == null) return null;

            return new BackupRecordDto
            {
                Id = backup.Id,
                Name = backup.Name,
                Type = backup.Type,
                Status = backup.Status,
                TenantId = backup.TenantId,
                FilePath = backup.FilePath,
                FileSize = backup.FileSize,
                Checksum = backup.Checksum,
                Configuration = backup.Configuration,
                ErrorMessage = backup.ErrorMessage,
                CreatedBy = backup.CreatedBy,
                CreatedAt = backup.CreatedAt,
                CompletedAt = backup.CompletedAt,
                ExpiresAt = backup.ExpiresAt
            };
        }

        public async Task<BackupRecordDto> CreateBackupAsync(CreateBackupDto createDto, string createdBy)
        {
            var backup = new BackupRecord
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Type = createDto.Type,
                Status = "pending",
                TenantId = createDto.TenantId,
                Configuration = createDto.Configuration,
                ExpiresAt = createDto.ExpiresAt,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.BackupRecords.Add(backup);
            await _context.SaveChangesAsync();

            return new BackupRecordDto
            {
                Id = backup.Id,
                Name = backup.Name,
                Type = backup.Type,
                Status = backup.Status,
                TenantId = backup.TenantId,
                FilePath = backup.FilePath,
                FileSize = backup.FileSize,
                Checksum = backup.Checksum,
                Configuration = backup.Configuration,
                ErrorMessage = backup.ErrorMessage,
                CreatedBy = backup.CreatedBy,
                CreatedAt = backup.CreatedAt,
                CompletedAt = backup.CompletedAt,
                ExpiresAt = backup.ExpiresAt
            };
        }

        public async Task<bool> DeleteBackupAsync(Guid id)
        {
            var backup = await _context.BackupRecords.FindAsync(id);
            if (backup == null) return false;

            _context.BackupRecords.Remove(backup);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<byte[]> DownloadBackupAsync(Guid id)
        {
            var backup = await _context.BackupRecords.FindAsync(id);
            if (backup == null || string.IsNullOrEmpty(backup.FilePath))
                return Array.Empty<byte>();

            // In a real implementation, this would read the backup file from storage
            return Array.Empty<byte>();
        }

        public async Task<BackupRecordDto> RestoreBackupAsync(Guid id)
        {
            var backup = await _context.BackupRecords.FindAsync(id);
            if (backup == null) return null;

            // Update status to running
            backup.Status = "running";
            await _context.SaveChangesAsync();

            // In a real implementation, this would perform the restore operation

            backup.Status = "completed";
            backup.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new BackupRecordDto
            {
                Id = backup.Id,
                Name = backup.Name,
                Type = backup.Type,
                Status = backup.Status,
                TenantId = backup.TenantId,
                FilePath = backup.FilePath,
                FileSize = backup.FileSize,
                Checksum = backup.Checksum,
                Configuration = backup.Configuration,
                ErrorMessage = backup.ErrorMessage,
                CreatedBy = backup.CreatedBy,
                CreatedAt = backup.CreatedAt,
                CompletedAt = backup.CompletedAt,
                ExpiresAt = backup.ExpiresAt
            };
        }

        public async Task<bool> ScheduleBackupAsync(CreateBackupDto createDto, string schedule)
        {
            // In a real implementation, this would set up a scheduled backup
            return true;
        }

        // API Keys
        public async Task<PaginatedResponseDto<ApiKeyDto>> GetApiKeysAsync(int page = 1, int pageSize = 50)
        {
            var query = _context.ApiKeys.AsQueryable();
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var apiKeys = await query
                .OrderByDescending(k => k.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new ApiKeyDto
                {
                    Id = k.Id,
                    Name = k.Name,
                    Prefix = k.Prefix,
                    Permissions = k.Permissions,
                    AllowedEndpoints = k.AllowedEndpoints,
                    AllowedIpAddresses = k.AllowedIpAddresses,
                    IsActive = k.IsActive,
                    LastUsed = k.LastUsed,
                    ExpiresAt = k.ExpiresAt,
                    UsageCount = k.UsageCount,
                    Metadata = k.Metadata,
                    CreatedAt = k.CreatedAt,
                    UpdatedAt = k.UpdatedAt
                })
                .ToListAsync();

            return new PaginatedResponseDto<ApiKeyDto>
            {
                Data = apiKeys,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<ApiKeyDto> GetApiKeyByIdAsync(Guid id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return null;

            return new ApiKeyDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Prefix = apiKey.Prefix,
                Permissions = apiKey.Permissions,
                AllowedEndpoints = apiKey.AllowedEndpoints,
                AllowedIpAddresses = apiKey.AllowedIpAddresses,
                IsActive = apiKey.IsActive,
                LastUsed = apiKey.LastUsed,
                ExpiresAt = apiKey.ExpiresAt,
                UsageCount = apiKey.UsageCount,
                Metadata = apiKey.Metadata,
                CreatedAt = apiKey.CreatedAt,
                UpdatedAt = apiKey.UpdatedAt
            };
        }

        public async Task<(ApiKeyDto apiKey, string plainKey)> CreateApiKeyAsync(CreateApiKeyDto createDto, string createdBy)
        {
            var plainKey = GenerateApiKey();
            var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(plainKey));
            var prefix = plainKey.Substring(0, 8);

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                KeyHash = Convert.ToBase64String(keyHash),
                Prefix = prefix,
                Permissions = createDto.Permissions,
                AllowedEndpoints = createDto.AllowedEndpoints,
                AllowedIpAddresses = createDto.AllowedIpAddresses,
                IsActive = true,
                ExpiresAt = createDto.ExpiresAt,
                Metadata = createDto.Metadata,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            var apiKeyDto = new ApiKeyDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Prefix = apiKey.Prefix,
                Permissions = apiKey.Permissions,
                AllowedEndpoints = apiKey.AllowedEndpoints,
                AllowedIpAddresses = apiKey.AllowedIpAddresses,
                IsActive = apiKey.IsActive,
                LastUsed = apiKey.LastUsed,
                ExpiresAt = apiKey.ExpiresAt,
                UsageCount = apiKey.UsageCount,
                Metadata = apiKey.Metadata,
                CreatedAt = apiKey.CreatedAt,
                UpdatedAt = apiKey.UpdatedAt
            };

            return (apiKeyDto, plainKey);
        }

        public async Task<ApiKeyDto> UpdateApiKeyAsync(Guid id, CreateApiKeyDto updateDto)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return null;

            apiKey.Name = updateDto.Name;
            apiKey.Permissions = updateDto.Permissions;
            apiKey.AllowedEndpoints = updateDto.AllowedEndpoints;
            apiKey.AllowedIpAddresses = updateDto.AllowedIpAddresses;
            apiKey.ExpiresAt = updateDto.ExpiresAt;
            apiKey.Metadata = updateDto.Metadata;
            apiKey.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ApiKeyDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Prefix = apiKey.Prefix,
                Permissions = apiKey.Permissions,
                AllowedEndpoints = apiKey.AllowedEndpoints,
                AllowedIpAddresses = apiKey.AllowedIpAddresses,
                IsActive = apiKey.IsActive,
                LastUsed = apiKey.LastUsed,
                ExpiresAt = apiKey.ExpiresAt,
                UsageCount = apiKey.UsageCount,
                Metadata = apiKey.Metadata,
                CreatedAt = apiKey.CreatedAt,
                UpdatedAt = apiKey.UpdatedAt
            };
        }

        public async Task<bool> DeleteApiKeyAsync(Guid id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return false;

            _context.ApiKeys.Remove(apiKey);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleApiKeyStatusAsync(Guid id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return false;

            apiKey.IsActive = !apiKey.IsActive;
            apiKey.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RegenerateApiKeyAsync(Guid id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return false;

            var plainKey = GenerateApiKey();
            var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(plainKey));
            
            apiKey.KeyHash = Convert.ToBase64String(keyHash);
            apiKey.Prefix = plainKey.Substring(0, 8);
            apiKey.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return true;
        }

        // System Health
        public async Task<Dictionary<string, object>> GetSystemHealthAsync()
        {
            return new Dictionary<string, object>
            {
                ["status"] = "healthy",
                ["timestamp"] = DateTime.UtcNow,
                ["database"] = "connected",
                ["memory"] = "normal",
                ["cpu"] = "normal"
            };
        }

        public async Task<Dictionary<string, object>> GetSystemMetricsAsync()
        {
            return new Dictionary<string, object>
            {
                ["activeTenants"] = await _context.Tenants.CountAsync(t => t.Status == "active"),
                ["totalUsers"] = await _context.Users.CountAsync(),
                ["activeUsers"] = await _context.Users.CountAsync(u => u.IsActive),
                ["totalReports"] = await _context.SuperAdminReports.CountAsync(),
                ["pendingReports"] = await _context.SuperAdminReports.CountAsync(r => r.Status == "generating"),
                ["securityEvents24h"] = await _context.SecurityEvents.CountAsync(e => e.CreatedAt > DateTime.UtcNow.AddHours(-24)),
                ["criticalEvents24h"] = await _context.SecurityEvents.CountAsync(e => e.Severity == "critical" && e.CreatedAt > DateTime.UtcNow.AddHours(-24))
            };
        }

        public async Task<List<string>> GetSystemWarningsAsync()
        {
            var warnings = new List<string>();

            var criticalEvents = await _context.SecurityEvents
                .CountAsync(e => e.Severity == "critical" && e.CreatedAt > DateTime.UtcNow.AddHours(-24));

            if (criticalEvents > 0)
                warnings.Add($"{criticalEvents} critical security events in the last 24 hours");

            var failedBackups = await _context.BackupRecords
                .CountAsync(b => b.Status == "failed" && b.CreatedAt > DateTime.UtcNow.AddDays(-7));

            if (failedBackups > 0)
                warnings.Add($"{failedBackups} failed backups in the last 7 days");

            return warnings;
        }

        // Data Export/Import
        public async Task<byte[]> ExportSystemDataAsync(string type, Dictionary<string, object> parameters)
        {
            // In a real implementation, this would generate and return the export data
            return Array.Empty<byte>();
        }

        public async Task<bool> ImportSystemDataAsync(string type, byte[] data, Dictionary<string, object> parameters)
        {
            // In a real implementation, this would process the import data
            return true;
        }

        // Tenant Management (Super Admin Level)
        public async Task<bool> SuspendTenantAsync(Guid tenantId, string reason)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            tenant.Status = "suspended";
            tenant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnsuspendTenantAsync(Guid tenantId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            tenant.Status = "active";
            tenant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTenantAsync(Guid tenantId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return false;

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ResetTenantPasswordAsync(Guid tenantId, string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email);

            if (user == null) return false;

            var newPassword = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // User Management (Super Admin Level)
        public async Task<bool> SuspendUserAsync(Guid userId, string reason)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnsuspendUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ResetUserPasswordAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var newPassword = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ForceLogoutUserAsync(Guid userId)
        {
            // In a real implementation, this would invalidate the user's sessions/tokens
            return true;
        }

        // Helper methods
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new char[12];

            for (int i = 0; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            return new string(password);
        }

        private string GenerateApiKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var key = new char[32];

            for (int i = 0; i < key.Length; i++)
            {
                key[i] = chars[random.Next(chars.Length)];
            }

            return "umi_" + new string(key);
        }
    }
}
