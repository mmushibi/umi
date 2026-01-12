using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Application.DTOs;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/superadmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ISuperAdminService _superAdminService;
        private readonly ILogger<SuperAdminController> _logger;

        public SuperAdminController(
            ISuperAdminService superAdminService,
            ILogger<SuperAdminController> logger)
        {
            _superAdminService = superAdminService;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var dashboardData = await _superAdminService.GetDashboardSummaryAsync();
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard summary");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] AnalyticsFilterDto filter)
        {
            try
            {
                var analytics = await _superAdminService.GetAnalyticsAsync(filter);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] LogFilterDto filter)
        {
            try
            {
                var logs = await _superAdminService.GetLogsAsync(filter);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("logs")]
        public async Task<IActionResult> CreateLog([FromBody] SuperAdminLogDto logDto)
        {
            try
            {
                var log = await _superAdminService.CreateLogAsync(logDto.LogLevel, logDto.Category, logDto.Message, logDto.Details, logDto.UserId, logDto.IpAddress);
                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log entry");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("logs")]
        public async Task<IActionResult> ClearLogs([FromQuery] DateTime? beforeDate)
        {
            try
            {
                await _superAdminService.ClearLogsAsync(beforeDate);
                return Ok(new { message = "Logs cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing logs");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var reports = await _superAdminService.GetReportsAsync(page, pageSize);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("reports/{id}")]
        public async Task<IActionResult> GetReportById(Guid id)
        {
            try
            {
                var report = await _superAdminService.GetReportByIdAsync(id);
                if (report == null)
                    return NotFound();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report {ReportId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("reports")]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto reportDto)
        {
            try
            {
                var report = await _superAdminService.CreateReportAsync(new CreateReportDto { Name = reportDto.Name, Type = reportDto.Type }, "System");
                return CreatedAtAction(nameof(GetReportById), new { id = report.Id }, report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("reports/{id}")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] SuperAdminReportDto reportDto)
        {
            try
            {
                var report = await _superAdminService.UpdateReportAsync(id, new CreateReportDto { Name = reportDto.Name, Type = reportDto.Type });
                if (report == null)
                    return NotFound();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report {ReportId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("reports/{id}")]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteReportAsync(id);
                if (!result)
                    return NotFound();

                return Ok(new { message = "Report deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("reports/{id}/download")]
        public async Task<IActionResult> DownloadReport(Guid id)
        {
            try
            {
                var reportData = await _superAdminService.DownloadReportAsync(id);
                if (reportData == null)
                    return NotFound();

                return File(reportData, "application/octet-stream", "report.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report {ReportId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("reports/{id}/generate")]
        public async Task<IActionResult> GenerateReport(Guid id)
        {
            try
            {
                await _superAdminService.GenerateReportAsync(id);
                return Ok(new { message = "Report generation started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report {ReportId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("security-events")]
        public async Task<IActionResult> GetSecurityEvents([FromQuery] SecurityFilterDto filter)
        {
            try
            {
                var events = await _superAdminService.GetSecurityEventsAsync(filter);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security events");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("security-events")]
        public async Task<IActionResult> CreateSecurityEvent([FromBody] SecurityEventDto eventDto)
        {
            try
            {
                var securityEvent = await _superAdminService.CreateSecurityEventAsync(eventDto.EventType, eventDto.FailureReason ?? "Security event", false, eventDto.UserId, eventDto.TenantId, eventDto.IpAddress, eventDto.UserAgent, "Medium");
                return Ok(securityEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating security event");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("security-events")]
        public async Task<IActionResult> ClearSecurityEvents([FromQuery] DateTime? beforeDate)
        {
            try
            {
                await _superAdminService.ClearSecurityEventsAsync(beforeDate);
                return Ok(new { message = "Security events cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing security events");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSystemSettings([FromQuery] string? category)
        {
            try
            {
                var settings = await _superAdminService.GetSystemSettingsAsync(category);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system settings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("settings/{key}")]
        public async Task<IActionResult> GetSystemSettingByKey(string key)
        {
            try
            {
                var setting = await _superAdminService.GetSystemSettingByKeyAsync(key);
                if (setting == null)
                    return NotFound();

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system setting {Key}", key);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("settings/{key}")]
        public async Task<IActionResult> UpdateSystemSetting(string key, [FromBody] UpdateSystemSettingDto settingDto)
        {
            try
            {
                var setting = await _superAdminService.UpdateSystemSettingAsync(key, settingDto, "System");
                if (setting == null)
                    return NotFound();

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system setting {Key}", key);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("settings")]
        public async Task<IActionResult> CreateSystemSetting([FromBody] SystemSettingDto settingDto)
        {
            try
            {
                var setting = await _superAdminService.CreateSystemSettingAsync(settingDto);
                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system setting");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("settings/{key}")]
        public async Task<IActionResult> DeleteSystemSetting(string key)
        {
            try
            {
                var result = await _superAdminService.DeleteSystemSettingAsync(key);
                if (!result)
                    return NotFound();

                return Ok(new { message = "System setting deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system setting {Key}", key);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetSuperAdminUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
        {
            try
            {
                var users = await _superAdminService.GetSuperAdminUsersAsync(page, pageSize, search);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving super admin users");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetSuperAdminUserById(Guid id)
        {
            try
            {
                var user = await _superAdminService.GetSuperAdminUserByIdAsync(id);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving super admin user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateSuperAdminUser([FromBody] CreateSuperAdminUserDto userDto)
        {
            try
            {
                var user = await _superAdminService.CreateSuperAdminUserAsync(userDto);
                return CreatedAtAction(nameof(GetSuperAdminUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating super admin user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateSuperAdminUser(Guid id, [FromBody] UpdateSuperAdminUserDto userDto)
        {
            try
            {
                var user = await _superAdminService.UpdateSuperAdminUserAsync(id, userDto);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating super admin user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteSuperAdminUser(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteSuperAdminUserAsync(id);
                if (!result)
                    return NotFound();

                return Ok(new { message = "Super admin user deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting super admin user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleSuperAdminUserStatus(Guid id)
        {
            try
            {
                var user = await _superAdminService.ToggleSuperAdminUserStatusAsync(id);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling super admin user status {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> ResetSuperAdminUserPassword(Guid id)
        {
            try
            {
                var result = await _superAdminService.ResetSuperAdminUserPasswordAsync(id);
                if (string.IsNullOrEmpty(result))
                    return NotFound();

                return Ok(new { message = "Password reset email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting super admin user password {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users/{id}/enable-2fa")]
        public async Task<IActionResult> EnableTwoFactor(Guid id, [FromBody] UpdateSuperAdminUserDto twoFactorDto)
        {
            try
            {
                await _superAdminService.EnableTwoFactorAsync(id, twoFactorDto.FirstName ?? "");
                return Ok(new { message = "2FA enabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA for super admin user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users/{id}/disable-2fa")]
        public async Task<IActionResult> DisableTwoFactor(Guid id)
        {
            try
            {
                await _superAdminService.DisableTwoFactorAsync(id);
                return Ok(new { message = "2FA disabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling 2FA for super admin user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetSystemNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var notifications = await _superAdminService.GetSystemNotificationsAsync(page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system notifications");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("notifications/active")]
        public async Task<IActionResult> GetActiveNotifications([FromQuery] Guid? userId = null, [FromQuery] Guid? tenantId = null)
        {
            try
            {
                var notifications = await _superAdminService.GetActiveNotificationsAsync(userId?.ToString(), tenantId?.ToString());
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active notifications");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("notifications/{id}")]
        public async Task<IActionResult> GetSystemNotificationById(Guid id)
        {
            try
            {
                var notification = await _superAdminService.GetSystemNotificationByIdAsync(id);
                if (notification == null)
                    return NotFound();

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system notification {NotificationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> CreateSystemNotification([FromBody] CreateSystemNotificationDto notificationDto)
        {
            try
            {
                var notification = await _superAdminService.CreateSystemNotificationAsync(notificationDto, "System");
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system notification");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("notifications/{id}")]
        public async Task<IActionResult> UpdateSystemNotification(Guid id, [FromBody] CreateSystemNotificationDto notificationDto)
        {
            try
            {
                var notification = await _superAdminService.UpdateSystemNotificationAsync(id, notificationDto);
                if (notification == null)
                    return NotFound();

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system notification {NotificationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("notifications/{id}")]
        public async Task<IActionResult> DeleteSystemNotification(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteSystemNotificationAsync(id);
                if (!result)
                    return NotFound();

                return Ok(new { message = "System notification deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system notification {NotificationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("notifications/{id}/toggle-status")]
        public async Task<IActionResult> ToggleSystemNotificationStatus(Guid id)
        {
            try
            {
                var notification = await _superAdminService.ToggleSystemNotificationStatusAsync(id);
                if (notification == null)
                    return NotFound();

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling system notification status {NotificationId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("backups")]
        public async Task<IActionResult> GetBackups([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] Guid? tenantId = null)
        {
            try
            {
                var backups = await _superAdminService.GetBackupsAsync(page, pageSize, tenantId?.ToString());
                return Ok(backups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving backups");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("backups/{id}")]
        public async Task<IActionResult> GetBackupById(Guid id)
        {
            try
            {
                var backup = await _superAdminService.GetBackupByIdAsync(id);
                if (backup == null)
                    return NotFound();

                return Ok(backup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving backup {BackupId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("backups")]
        public async Task<IActionResult> CreateBackup([FromBody] CreateBackupDto backupDto)
        {
            try
            {
                var backup = await _superAdminService.CreateBackupAsync(backupDto, "System");
                return Ok(backup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("backups/{id}")]
        public async Task<IActionResult> DeleteBackup(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteBackupAsync(id);
                if (!result)
                    return NotFound();

                return Ok(new { message = "Backup deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting backup {BackupId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("backups/{id}/download")]
        public async Task<IActionResult> DownloadBackup(Guid id)
        {
            try
            {
                var backupData = await _superAdminService.DownloadBackupAsync(id);
                if (backupData == null)
                    return NotFound();

                return File(backupData, "application/octet-stream", "backup.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading backup {BackupId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("backups/{id}/restore")]
        public async Task<IActionResult> RestoreBackup(Guid id)
        {
            try
            {
                await _superAdminService.RestoreBackupAsync(id);
                return Ok(new { message = "Backup restoration started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring backup {BackupId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("backups/schedule")]
        public async Task<IActionResult> ScheduleBackup([FromBody] CreateBackupDto scheduleDto)
        {
            try
            {
                await _superAdminService.ScheduleBackupAsync(scheduleDto, "daily");
                return Ok(new { message = "Backup scheduled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling backup");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("api-keys")]
        public async Task<IActionResult> GetApiKeys([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var apiKeys = await _superAdminService.GetApiKeysAsync(page, pageSize);
                return Ok(apiKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("api-keys/{id}")]
        public async Task<IActionResult> GetApiKeyById(Guid id)
        {
            try
            {
                var apiKey = await _superAdminService.GetApiKeyByIdAsync(id);
                if (apiKey == null)
                    return NotFound();

                return Ok(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key {ApiKey}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("api-keys")]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyDto apiKeyDto)
        {
            try
            {
                var (apiKey, plainKey) = await _superAdminService.CreateApiKeyAsync(apiKeyDto, "System");
                return Ok(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("api-keys/{id}")]
        public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] CreateApiKeyDto apiKeyDto)
        {
            try
            {
                var apiKey = await _superAdminService.UpdateApiKeyAsync(id, apiKeyDto);
                if (apiKey == null)
                    return NotFound();

                return Ok(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key {ApiKey}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("api-keys/{id}")]
        public async Task<IActionResult> DeleteApiKey(Guid id)
        {
            try
            {
                var result = await _superAdminService.DeleteApiKeyAsync(id);
                if (!result)
                    return NotFound();

                return Ok(new { message = "API key deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key {ApiKey}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("api-keys/{id}/toggle-status")]
        public async Task<IActionResult> ToggleApiKeyStatus(Guid id)
        {
            try
            {
                var result = await _superAdminService.ToggleApiKeyStatusAsync(id);
                if (!result)
                    return NotFound();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling API key status {ApiKey}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("api-keys/{id}/regenerate")]
        public async Task<IActionResult> RegenerateApiKey(Guid id)
        {
            try
            {
                var result = await _superAdminService.RegenerateApiKeyAsync(id);
                if (!result)
                    return NotFound();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating API key {ApiKey}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            try
            {
                var health = await _superAdminService.GetSystemHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system health");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetSystemMetrics()
        {
            try
            {
                var metrics = await _superAdminService.GetSystemMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system metrics");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("warnings")]
        public async Task<IActionResult> GetSystemWarnings()
        {
            try
            {
                var warnings = await _superAdminService.GetSystemWarningsAsync();
                return Ok(warnings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system warnings");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportSystemData([FromBody] BackupRecordDto exportDto)
        {
            try
            {
                var exportData = await _superAdminService.ExportSystemDataAsync(exportDto.Type ?? "all", new Dictionary<string, object>());
                return File(exportData, "application/octet-stream", "backup.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting system data");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportSystemData([FromBody] BackupRecordDto importDto)
        {
            try
            {
                var result = await _superAdminService.ImportSystemDataAsync(importDto.Type ?? "all", importDto.Data ?? new byte[0], new Dictionary<string, object>());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing system data");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("tenants/{id}/suspend")]
        public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] SuperAdminUserDto suspendDto)
        {
            try
            {
                await _superAdminService.SuspendTenantAsync(id, suspendDto.Email ?? "Suspended by admin");
                return Ok(new { message = "Tenant suspended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending tenant {TenantId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("tenants/{id}/unsuspend")]
        public async Task<IActionResult> UnsuspendTenant(Guid id)
        {
            try
            {
                await _superAdminService.UnsuspendTenantAsync(id);
                return Ok(new { message = "Tenant unsuspended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsuspending tenant {TenantId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("tenants/{id}")]
        public async Task<IActionResult> DeleteTenant(Guid id)
        {
            try
            {
                await _superAdminService.DeleteTenantAsync(id);
                return Ok(new { message = "Tenant deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("tenants/{id}/reset-password")]
        public async Task<IActionResult> ResetTenantPassword(Guid id, [FromBody] CreateSuperAdminUserDto resetDto)
        {
            try
            {
                await _superAdminService.ResetTenantPasswordAsync(id, resetDto.Email ?? "");
                return Ok(new { message = "Password reset email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting tenant password {TenantId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users-all/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id, [FromBody] UpdateSuperAdminUserDto suspendDto)
        {
            try
            {
                await _superAdminService.SuspendUserAsync(id, suspendDto.FirstName ?? "Suspended by admin");
                return Ok(new { message = "User suspended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users-all/{id}/unsuspend")]
        public async Task<IActionResult> UnsuspendUser(Guid id)
        {
            try
            {
                await _superAdminService.UnsuspendUserAsync(id);
                return Ok(new { message = "User unsuspended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsuspending user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users-all/{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(Guid id)
        {
            try
            {
                await _superAdminService.ResetUserPasswordAsync(id);
                return Ok(new { message = "Password reset email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting user password {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("users-all/{id}/force-logout")]
        public async Task<IActionResult> ForceLogoutUser(Guid id)
        {
            try
            {
                await _superAdminService.ForceLogoutUserAsync(id);
                return Ok(new { message = "User logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force logging out user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
