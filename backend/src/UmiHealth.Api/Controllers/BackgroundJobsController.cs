using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class BackgroundJobsController : ControllerBase
    {
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<BackgroundJobsController> _logger;

        public BackgroundJobsController(IBackgroundJobService backgroundJobService, ILogger<BackgroundJobsController> logger)
        {
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        [HttpPost("low-stock-alerts")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<string>>> TriggerLowStockAlerts([FromBody] TriggerJobRequest request)
        {
            try
            {
                var tenantId = request.TenantId ?? GetTenantId();
                var branchId = request.BranchId ?? GetBranchId() ?? Guid.Empty;

                var jobId = _backgroundJobService.SendLowStockAlerts(tenantId, branchId);
                
                _logger.LogInformation("Low stock alerts job triggered: {JobId} for tenant {TenantId}, branch {BranchId}", 
                    jobId, tenantId, branchId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, "Low stock alerts job triggered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering low stock alerts job");
                return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("expiry-alerts")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<string>>> TriggerExpiryAlerts([FromBody] TriggerJobRequest request)
        {
            try
            {
                var tenantId = request.TenantId ?? GetTenantId();
                var branchId = request.BranchId ?? GetBranchId() ?? Guid.Empty;

                var jobId = _backgroundJobService.SendExpiryAlerts(tenantId, branchId);
                
                _logger.LogInformation("Expiry alerts job triggered: {JobId} for tenant {TenantId}, branch {BranchId}", 
                    jobId, tenantId, branchId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, "Expiry alerts job triggered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering expiry alerts job");
                return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("daily-reports")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<string>>> TriggerDailyReports([FromBody] TriggerJobRequest request)
        {
            try
            {
                var tenantId = request.TenantId ?? GetTenantId();
                var branchId = request.BranchId ?? GetBranchId() ?? Guid.Empty;

                var jobId = _backgroundJobService.GenerateDailyReports(tenantId, branchId);
                
                _logger.LogInformation("Daily reports job triggered: {JobId} for tenant {TenantId}, branch {BranchId}", 
                    jobId, tenantId, branchId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, "Daily reports job triggered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering daily reports job");
                return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("prescription-reminders")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<string>>> TriggerPrescriptionReminders([FromBody] TriggerJobRequest request)
        {
            try
            {
                var tenantId = request.TenantId ?? GetTenantId();
                var branchId = request.BranchId ?? GetBranchId() ?? Guid.Empty;

                var jobId = _backgroundJobService.ProcessPrescriptionReminders(tenantId, branchId);
                
                _logger.LogInformation("Prescription reminders job triggered: {JobId} for tenant {TenantId}, branch {BranchId}", 
                    jobId, tenantId, branchId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, "Prescription reminders job triggered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering prescription reminders job");
                return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("cleanup-tokens")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<string>>> TriggerTokenCleanup()
        {
            try
            {
                var jobId = _backgroundJobService.CleanExpiredTokens();
                
                _logger.LogInformation("Token cleanup job triggered: {JobId}", jobId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, "Token cleanup job triggered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering token cleanup job");
                return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("archive-data")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<string>>> TriggerDataArchiving()
        {
            try
            {
                var jobId = _backgroundJobService.ArchiveOldData();
                
                _logger.LogInformation("Data archiving job triggered: {JobId}", jobId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, "Data archiving job triggered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering data archiving job");
                return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("subscription-reminders")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ApiResponse<string>>> TriggerSubscriptionReminders()
        {
            try
            {
                var jobId = _backgroundJobService.SendSubscriptionReminders();
                
                _logger.LogInformation("Subscription reminders job triggered: {JobId}", jobId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, "Subscription reminders job triggered successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering subscription reminders job");
                return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("status")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<BackgroundJobStatusDto>>> GetJobStatus([FromQuery] string? jobId = null)
        {
            try
            {
                // This would typically use Hangfire's JobStorage.Current to get job status
                // For now, returning a placeholder implementation
                var status = new BackgroundJobStatusDto
                {
                    JobId = jobId ?? "all",
                    Status = "processing",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                return Ok(ApiResponse<BackgroundJobStatusDto>.SuccessResult(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job status");
                return BadRequest(ApiResponse<BackgroundJobStatusDto>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("recurring")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RecurringJobDto>>>> GetRecurringJobs()
        {
            try
            {
                // This would typically query Hangfire for recurring jobs
                // For now, returning a placeholder list
                var recurringJobs = new List<RecurringJobDto>
                {
                    new() { Id = "daily-low-stock-alerts", Cron = "0 9 * * *", Description = "Daily low stock alerts" },
                    new() { Id = "daily-expiry-alerts", Cron = "30 9 * * *", Description = "Daily expiry alerts" },
                    new() { Id = "daily-reports", Cron = "0 23 * * *", Description = "Daily reports generation" },
                    new() { Id = "prescription-reminders", Cron = "0 * * * *", Description = "Hourly prescription reminders" },
                    new() { Id = "weekly-inventory-report", Cron = "0 8 * * 1", Description = "Weekly inventory report" },
                    new() { Id = "monthly-financial-report", Cron = "0 8 1 * *", Description = "Monthly financial report" },
                    new() { Id = "subscription-reminders", Cron = "0 10 * * 1", Description = "Weekly subscription reminders" },
                    new() { Id = "cleanup-expired-tokens", Cron = "0 * * * *", Description = "Hourly token cleanup" },
                    new() { Id = "archive-old-data", Cron = "0 2 1 * *", Description = "Monthly data archiving" }
                };

                return Ok(ApiResponse<IEnumerable<RecurringJobDto>>.SuccessResult(recurringJobs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recurring jobs");
                return BadRequest(ApiResponse<IEnumerable<RecurringJobDto>>.ErrorResult(ex.Message));
            }
        }

        private Guid GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            return tenantIdClaim != null ? Guid.Parse(tenantIdClaim.Value) : Guid.Empty;
        }

        private Guid? GetBranchId()
        {
            var branchIdClaim = User.FindFirst("branch_id");
            return branchIdClaim != null && Guid.TryParse(branchIdClaim.Value, out var branchId) ? branchId : null;
        }
    }

    // DTOs
    public class TriggerJobRequest
    {
        public Guid? TenantId { get; set; }
        public Guid? BranchId { get; set; }
    }

    public class BackgroundJobStatusDto
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class RecurringJobDto
    {
        public string Id { get; set; } = string.Empty;
        public string Cron { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
