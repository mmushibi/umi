using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Application.Services;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ISuperAdminService _superAdminService;
        private readonly SharedDbContext _context;
        private readonly ILogger<SuperAdminController> _logger;

        public SuperAdminController(
            ISuperAdminService superAdminService,
            SharedDbContext context,
            ILogger<SuperAdminController> logger)
        {
            _superAdminService = superAdminService;
            _context = context;
            _logger = logger;
        }

        // Dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary()
        {
            try
            {
                var summary = await _superAdminService.GetDashboardSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard summary");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Analytics
        [HttpGet("analytics")]
        public async Task<ActionResult<PaginatedResponseDto<AnalyticsDto>>> GetAnalytics([FromQuery] AnalyticsFilterDto filter)
        {
            try
            {
                var analytics = await _superAdminService.GetAnalyticsAsync(filter);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("analytics/{id}")]
        public async Task<ActionResult<AnalyticsDto>> GetAnalyticsById(Guid id)
        {
            try
            {
                var analytics = await _superAdminService.GetAnalyticsByIdAsync(id);
                if (analytics == null)
                    return NotFound(new { error = "Analytics not found" });

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("analytics/generate")]
        public async Task<ActionResult<AnalyticsDto>> GenerateAnalytics([FromBody] GenerateAnalyticsRequest request)
        {
            try
            {
                var analytics = await _superAdminService.GenerateAnalyticsAsync(request.Date);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating analytics for date: {Date}", request.Date);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Logs
        [HttpGet("logs")]
        public async Task<ActionResult<PaginatedResponseDto<SuperAdminLogDto>>> GetLogs([FromQuery] LogFilterDto filter)
        {
            try
            {
                var logs = await _superAdminService.GetLogsAsync(filter);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("logs/{id}")]
        public async Task<ActionResult<SuperAdminLogDto>> GetLogById(Guid id)
        {
            try
            {
                var log = await _superAdminService.GetLogByIdAsync(id);
                if (log == null)
                    return NotFound(new { error = "Log not found" });

                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving log by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("logs")]
        public async Task<ActionResult<SuperAdminLogDto>> CreateLog([FromBody] CreateLogRequest request)
        {
            try
            {
                var log = await _superAdminService.CreateLogAsync(
                    request.LogLevel, 
                    request.Category, 
                    request.Message, 
                    request.Details, 
                    request.UserId, 
                    request.TenantId);

                return CreatedAtAction(nameof(GetLogById), new { id = log.Id }, log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("logs")]
        public async Task<ActionResult> ClearLogs([FromQuery] DateTime? beforeDate = null)
        {
            try
            {
                await _superAdminService.ClearLogsAsync(beforeDate);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Reports
        [HttpGet("reports")]
        public async Task<ActionResult<PaginatedResponseDto<SuperAdminReportDto>>> GetReports(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var reports = await _superAdminService.GetReportsAsync(page, pageSize);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("reports/{id}")]
        public async Task<ActionResult<SuperAdminReportDto>> GetReportById(Guid id)
        {
            try
            {
                var report = await _superAdminService.GetReportByIdAsync(id);
                if (report == null)
                    return NotFound(new { error = "Report not found" });

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("reports")]
        public async Task<ActionResult<SuperAdminReportDto>> CreateReport([FromBody] CreateReportDto createDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var report = await _superAdminService.CreateReportAsync(createDto, userId);
                return CreatedAtAction(nameof(GetReportById), new { id = report.Id }, report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("reports/{id}")]
        public async Task<ActionResult<SuperAdminReportDto>> UpdateReport(Guid id, [FromBody] CreateReportDto updateDto)
        {
            try
            {
                var report = await _superAdminService.UpdateReportAsync(id, updateDto);
                if (report == null)
                    return NotFound(new { error = "Report not found" });

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("reports/{id}")]
        public async Task<ActionResult> DeleteReport(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteReportAsync(id);
                if (!result)
                    return NotFound(new { error = "Report not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("reports/{id}/download")]
        public async Task<ActionResult> DownloadReport(Guid id)
        {
            try
            {
                var data = await _superAdminService.DownloadReportAsync(id);
                if (data.Length == 0)
                    return NotFound(new { error = "Report file not found" });

                return File(data, "application/octet-stream", $"report_{id}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("reports/{id}/generate")]
        public async Task<ActionResult<SuperAdminReportDto>> GenerateReport(Guid id)
        {
            try
            {
                var report = await _superAdminService.GenerateReportAsync(id);
                if (report == null)
                    return NotFound(new { error = "Report not found" });

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Security Events
        [HttpGet("security-events")]
        public async Task<ActionResult<PaginatedResponseDto<SecurityEventDto>>> GetSecurityEvents([FromQuery] SecurityFilterDto filter)
        {
            try
            {
                var events = await _superAdminService.GetSecurityEventsAsync(filter);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security events");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("security-events/{id}")]
        public async Task<ActionResult<SecurityEventDto>> GetSecurityEventById(Guid id)
        {
            try
            {
                var securityEvent = await _superAdminService.GetSecurityEventByIdAsync(id);
                if (securityEvent == null)
                    return NotFound(new { error = "Security event not found" });

                return Ok(securityEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security event by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("security-events")]
        public async Task<ActionResult<SecurityEventDto>> CreateSecurityEvent([FromBody] CreateSecurityEventRequest request)
        {
            try
            {
                var securityEvent = await _superAdminService.CreateSecurityEventAsync(
                    request.EventType, 
                    request.Severity, 
                    request.Success, 
                    request.UserId, 
                    request.TenantId, 
                    request.IpAddress, 
                    request.Resource, 
                    request.Action, 
                    request.FailureReason);

                return CreatedAtAction(nameof(GetSecurityEventById), new { id = securityEvent.Id }, securityEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating security event");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("security-events")]
        public async Task<ActionResult> ClearSecurityEvents([FromQuery] DateTime? beforeDate = null)
        {
            try
            {
                await _superAdminService.ClearSecurityEventsAsync(beforeDate);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing security events");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // System Settings
        [HttpGet("settings")]
        public async Task<ActionResult<List<SystemSettingDto>>> GetSystemSettings([FromQuery] string? category = null)
        {
            try
            {
                var settings = await _superAdminService.GetSystemSettingsAsync(category);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("settings/{key}")]
        public async Task<ActionResult<SystemSettingDto>> GetSystemSettingByKey(string key)
        {
            try
            {
                var setting = await _superAdminService.GetSystemSettingByKeyAsync(key);
                if (setting == null)
                    return NotFound(new { error = "Setting not found" });

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system setting by key: {Key}", key);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("settings/{key}")]
        public async Task<ActionResult<SystemSettingDto>> UpdateSystemSetting(string key, [FromBody] UpdateSystemSettingDto updateDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var setting = await _superAdminService.UpdateSystemSettingAsync(key, updateDto, userId);
                if (setting == null)
                    return NotFound(new { error = "Setting not found" });

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system setting: {Key}", key);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("settings")]
        public async Task<ActionResult<SystemSettingDto>> CreateSystemSetting([FromBody] SystemSettingDto settingDto)
        {
            try
            {
                var setting = await _superAdminService.CreateSystemSettingAsync(settingDto);
                return CreatedAtAction(nameof(GetSystemSettingByKey), new { key = setting.Key }, setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system setting");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("settings/{key}")]
        public async Task<ActionResult> DeleteSystemSetting(string key)
        {
            try
            {
                var result = await _superAdminService.DeleteSystemSettingAsync(key);
                if (!result)
                    return NotFound(new { error = "Setting not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system setting: {Key}", key);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Super Admin Users
        [HttpGet("users")]
        public async Task<ActionResult<PaginatedResponseDto<SuperAdminUserDto>>> GetSuperAdminUsers(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50, 
            [FromQuery] string? search = null)
        {
            try
            {
                var users = await _superAdminService.GetSuperAdminUsersAsync(page, pageSize, search);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving super admin users");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<SuperAdminUserDto>> GetSuperAdminUserById(Guid id)
        {
            try
            {
                var user = await _superAdminService.GetSuperAdminUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { error = "User not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving super admin user by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users")]
        public async Task<ActionResult<SuperAdminUserDto>> CreateSuperAdminUser([FromBody] CreateSuperAdminUserDto createDto)
        {
            try
            {
                var user = await _superAdminService.CreateSuperAdminUserAsync(createDto);
                return CreatedAtAction(nameof(GetSuperAdminUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating super admin user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<ActionResult<SuperAdminUserDto>> UpdateSuperAdminUser(Guid id, [FromBody] UpdateSuperAdminUserDto updateDto)
        {
            try
            {
                var user = await _superAdminService.UpdateSuperAdminUserAsync(id, updateDto);
                if (user == null)
                    return NotFound(new { error = "User not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating super admin user: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<ActionResult> DeleteSuperAdminUser(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteSuperAdminUserAsync(id);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting super admin user: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users/{id}/toggle-status")]
        public async Task<ActionResult> ToggleSuperAdminUserStatus(Guid id)
        {
            try
            {
                var result = await _superAdminService.ToggleSuperAdminUserStatusAsync(id);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling super admin user status: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users/{id}/reset-password")]
        public async Task<ActionResult<string>> ResetSuperAdminUserPassword(Guid id)
        {
            try
            {
                var newPassword = await _superAdminService.ResetSuperAdminUserPasswordAsync(id);
                if (newPassword == null)
                    return NotFound(new { error = "User not found" });

                return Ok(new { password = newPassword });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting super admin user password: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users/{id}/enable-2fa")]
        public async Task<ActionResult> EnableTwoFactor(Guid id, [FromBody] EnableTwoFactorRequest request)
        {
            try
            {
                var result = await _superAdminService.EnableTwoFactorAsync(id, request.Secret);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA for super admin user: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users/{id}/disable-2fa")]
        public async Task<ActionResult> DisableTwoFactor(Guid id)
        {
            try
            {
                var result = await _superAdminService.DisableTwoFactorAsync(id);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling 2FA for super admin user: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // System Notifications
        [HttpGet("notifications")]
        public async Task<ActionResult<PaginatedResponseDto<SystemNotificationDto>>> GetSystemNotifications(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var notifications = await _superAdminService.GetSystemNotificationsAsync(page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("notifications/active")]
        public async Task<ActionResult<List<SystemNotificationDto>>> GetActiveNotifications(
            [FromQuery] string? userId = null, 
            [FromQuery] string? tenantId = null)
        {
            try
            {
                var notifications = await _superAdminService.GetActiveNotificationsAsync(userId, tenantId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("notifications/{id}")]
        public async Task<ActionResult<SystemNotificationDto>> GetSystemNotificationById(Guid id)
        {
            try
            {
                var notification = await _superAdminService.GetSystemNotificationByIdAsync(id);
                if (notification == null)
                    return NotFound(new { error = "Notification not found" });

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system notification by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("notifications")]
        public async Task<ActionResult<SystemNotificationDto>> CreateSystemNotification([FromBody] CreateSystemNotificationDto createDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var notification = await _superAdminService.CreateSystemNotificationAsync(createDto, userId);
                return CreatedAtAction(nameof(GetSystemNotificationById), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system notification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("notifications/{id}")]
        public async Task<ActionResult<SystemNotificationDto>> UpdateSystemNotification(Guid id, [FromBody] CreateSystemNotificationDto updateDto)
        {
            try
            {
                var notification = await _superAdminService.UpdateSystemNotificationAsync(id, updateDto);
                if (notification == null)
                    return NotFound(new { error = "Notification not found" });

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system notification: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("notifications/{id}")]
        public async Task<ActionResult> DeleteSystemNotification(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteSystemNotificationAsync(id);
                if (!result)
                    return NotFound(new { error = "Notification not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system notification: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("notifications/{id}/toggle-status")]
        public async Task<ActionResult> ToggleSystemNotificationStatus(Guid id)
        {
            try
            {
                var result = await _superAdminService.ToggleSystemNotificationStatusAsync(id);
                if (!result)
                    return NotFound(new { error = "Notification not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling system notification status: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Backup Management
        [HttpGet("backups")]
        public async Task<ActionResult<PaginatedResponseDto<BackupRecordDto>>> GetBackups(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50, 
            [FromQuery] string? tenantId = null)
        {
            try
            {
                var backups = await _superAdminService.GetBackupsAsync(page, pageSize, tenantId);
                return Ok(backups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving backups");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("backups/{id}")]
        public async Task<ActionResult<BackupRecordDto>> GetBackupById(Guid id)
        {
            try
            {
                var backup = await _superAdminService.GetBackupByIdAsync(id);
                if (backup == null)
                    return NotFound(new { error = "Backup not found" });

                return Ok(backup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving backup by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("backups")]
        public async Task<ActionResult<BackupRecordDto>> CreateBackup([FromBody] CreateBackupDto createDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var backup = await _superAdminService.CreateBackupAsync(createDto, userId);
                return CreatedAtAction(nameof(GetBackupById), new { id = backup.Id }, backup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("backups/{id}")]
        public async Task<ActionResult> DeleteBackup(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteBackupAsync(id);
                if (!result)
                    return NotFound(new { error = "Backup not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting backup: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("backups/{id}/download")]
        public async Task<ActionResult> DownloadBackup(Guid id)
        {
            try
            {
                var data = await _superAdminService.DownloadBackupAsync(id);
                if (data.Length == 0)
                    return NotFound(new { error = "Backup file not found" });

                return File(data, "application/octet-stream", $"backup_{id}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading backup: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("backups/{id}/restore")]
        public async Task<ActionResult<BackupRecordDto>> RestoreBackup(Guid id)
        {
            try
            {
                var backup = await _superAdminService.RestoreBackupAsync(id);
                if (backup == null)
                    return NotFound(new { error = "Backup not found" });

                return Ok(backup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring backup: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("backups/schedule")]
        public async Task<ActionResult> ScheduleBackup([FromBody] ScheduleBackupRequest request)
        {
            try
            {
                var result = await _superAdminService.ScheduleBackupAsync(request.CreateDto, request.Schedule);
                if (!result)
                    return BadRequest(new { error = "Failed to schedule backup" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling backup");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // API Keys
        [HttpGet("api-keys")]
        public async Task<ActionResult<PaginatedResponseDto<ApiKeyDto>>> GetApiKeys(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var apiKeys = await _superAdminService.GetApiKeysAsync(page, pageSize);
                return Ok(apiKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("api-keys/{id}")]
        public async Task<ActionResult<ApiKeyDto>> GetApiKeyById(Guid id)
        {
            try
            {
                var apiKey = await _superAdminService.GetApiKeyByIdAsync(id);
                if (apiKey == null)
                    return NotFound(new { error = "API key not found" });

                return Ok(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key by ID: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("api-keys")]
        public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey([FromBody] CreateApiKeyDto createDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var (apiKey, plainKey) = await _superAdminService.CreateApiKeyAsync(createDto, userId);
                
                return Ok(new CreateApiKeyResponse
                {
                    ApiKey = apiKey,
                    PlainKey = plainKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("api-keys/{id}")]
        public async Task<ActionResult<ApiKeyDto>> UpdateApiKey(Guid id, [FromBody] CreateApiKeyDto updateDto)
        {
            try
            {
                var apiKey = await _superAdminService.UpdateApiKeyAsync(id, updateDto);
                if (apiKey == null)
                    return NotFound(new { error = "API key not found" });

                return Ok(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("api-keys/{id}")]
        public async Task<ActionResult> DeleteApiKey(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteApiKeyAsync(id);
                if (!result)
                    return NotFound(new { error = "API key not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("api-keys/{id}/toggle-status")]
        public async Task<ActionResult> ToggleApiKeyStatus(Guid id)
        {
            try
            {
                var result = await _superAdminService.ToggleApiKeyStatusAsync(id);
                if (!result)
                    return NotFound(new { error = "API key not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling API key status: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("api-keys/{id}/regenerate")]
        public async Task<ActionResult> RegenerateApiKey(Guid id)
        {
            try
            {
                var result = await _superAdminService.RegenerateApiKeyAsync(id);
                if (!result)
                    return NotFound(new { error = "API key not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating API key: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // System Health
        [HttpGet("health")]
        public async Task<ActionResult<Dictionary<string, object>>> GetSystemHealth()
        {
            try
            {
                var health = await _superAdminService.GetSystemHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system health");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<Dictionary<string, object>>> GetSystemMetrics()
        {
            try
            {
                var metrics = await _superAdminService.GetSystemMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system metrics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("warnings")]
        public async Task<ActionResult<List<string>>> GetSystemWarnings()
        {
            try
            {
                var warnings = await _superAdminService.GetSystemWarningsAsync();
                return Ok(warnings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system warnings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Data Export/Import
        [HttpPost("export")]
        public async Task<ActionResult> ExportSystemData([FromBody] ExportRequest request)
        {
            try
            {
                var data = await _superAdminService.ExportSystemDataAsync(request.Type, request.Parameters);
                return File(data, "application/octet-stream", $"export_{request.Type}_{DateTime.UtcNow:yyyyMMdd}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting system data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("import")]
        public async Task<ActionResult> ImportSystemData([FromBody] ImportRequest request)
        {
            try
            {
                var data = Convert.FromBase64String(request.Data);
                var result = await _superAdminService.ImportSystemDataAsync(request.Type, data, request.Parameters);
                
                if (result)
                    return Ok(new { message = "Import completed successfully" });
                else
                    return BadRequest(new { error = "Import failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing system data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Tenant Management (Super Admin Level)
        [HttpPost("tenants/{id}/suspend")]
        public async Task<ActionResult> SuspendTenant(Guid id, [FromBody] SuspendTenantRequest request)
        {
            try
            {
                var result = await _superAdminService.SuspendTenantAsync(id, request.Reason);
                if (!result)
                    return NotFound(new { error = "Tenant not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending tenant: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("tenants/{id}/unsuspend")]
        public async Task<ActionResult> UnsuspendTenant(Guid id)
        {
            try
            {
                var result = await _superAdminService.UnsuspendTenantAsync(id);
                if (!result)
                    return NotFound(new { error = "Tenant not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsuspending tenant: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("tenants/{id}")]
        public async Task<ActionResult> DeleteTenant(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteTenantAsync(id);
                if (!result)
                    return NotFound(new { error = "Tenant not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("tenants/{id}/reset-password")]
        public async Task<ActionResult> ResetTenantPassword(Guid id, [FromBody] ResetTenantPasswordRequest request)
        {
            try
            {
                var result = await _superAdminService.ResetTenantPasswordAsync(id, request.Email);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting tenant password: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // User Management (Super Admin Level)
        [HttpPost("users-all/{id}/suspend")]
        public async Task<ActionResult> SuspendUser(Guid id, [FromBody] SuspendUserRequest request)
        {
            try
            {
                var result = await _superAdminService.SuspendUserAsync(id, request.Reason);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users-all/{id}/unsuspend")]
        public async Task<ActionResult> UnsuspendUser(Guid id)
        {
            try
            {
                var result = await _superAdminService.UnsuspendUserAsync(id);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsuspending user: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users-all/{id}/reset-password")]
        public async Task<ActionResult> ResetUserPassword(Guid id)
        {
            try
            {
                var result = await _superAdminService.ResetUserPasswordAsync(id);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting user password: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("users-all/{id}/force-logout")]
        public async Task<ActionResult> ForceLogoutUser(Guid id)
        {
            try
            {
                var result = await _superAdminService.ForceLogoutUserAsync(id);
                if (!result)
                    return NotFound(new { error = "User not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force logging out user: {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // Request/Response DTOs
    public class GenerateAnalyticsRequest
    {
        public DateTime Date { get; set; }
    }

    public class CreateLogRequest
    {
        public string LogLevel { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
    }

    public class CreateSecurityEventRequest
    {
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public string? IpAddress { get; set; }
        public string? Resource { get; set; }
        public string? Action { get; set; }
        public string? FailureReason { get; set; }
    }

    public class EnableTwoFactorRequest
    {
        public string Secret { get; set; } = string.Empty;
    }

    public class ScheduleBackupRequest
    {
        public CreateBackupDto CreateDto { get; set; } = new();
        public string Schedule { get; set; } = string.Empty;
    }

    public class CreateApiKeyResponse
    {
        public ApiKeyDto ApiKey { get; set; } = new();
        public string PlainKey { get; set; } = string.Empty;
    }

    public class ExportRequest
    {
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class ImportRequest
    {
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty; // Base64 encoded
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class SuspendTenantRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ResetTenantPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class SuspendUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
