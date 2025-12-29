using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Application.DTOs.Queue;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class QueueController : ControllerBase
    {
        private readonly IQueueService _queueService;
        private readonly ILogger<QueueController> _logger;

        public QueueController(
            IQueueService queueService,
            ILogger<QueueController> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        private Guid GetTenantId()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new UnauthorizedAccessException("Tenant ID not found in token");
            }
            return Guid.Parse(tenantId);
        }

        private Guid GetBranchId()
        {
            var branchId = User.FindFirst("BranchId")?.Value;
            if (string.IsNullOrEmpty(branchId))
            {
                throw new UnauthorizedAccessException("Branch ID not found in token");
            }
            return Guid.Parse(branchId);
        }

        [HttpGet("current")]
        public async Task<ActionResult<QueueDataResponse>> GetCurrentQueue()
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var queueData = await _queueService.GetCurrentQueueAsync(tenantId, branchId);
                
                return Ok(queueData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current queue");
                return StatusCode(500, new { success = false, message = "Failed to retrieve queue data." });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<QueueStatsResponse>> GetQueueStats()
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var stats = await _queueService.GetQueueStatsAsync(tenantId, branchId);
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving queue stats");
                return StatusCode(500, new { success = false, message = "Failed to retrieve queue stats." });
            }
        }

        [HttpPost("add")]
        public async Task<ActionResult<QueuePatientResponse>> AddPatientToQueue([FromBody] AddPatientRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                var result = await _queueService.AddPatientToQueueAsync(tenantId, branchId, request, userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding patient to queue");
                return StatusCode(500, new { success = false, message = "Failed to add patient to queue." });
            }
        }

        [HttpPost("{patientId}/serve")]
        public async Task<ActionResult> ServePatient(Guid patientId)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.ServePatientAsync(tenantId, branchId, patientId, userId);
                
                return Ok(new { success = true, message = "Patient served successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving patient");
                return StatusCode(500, new { success = false, message = "Failed to serve patient." });
            }
        }

        [HttpDelete("{patientId}/remove")]
        public async Task<ActionResult> RemovePatientFromQueue(Guid patientId)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.RemovePatientFromQueueAsync(tenantId, branchId, patientId, userId);
                
                return Ok(new { success = true, message = "Patient removed from queue successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing patient from queue");
                return StatusCode(500, new { success = false, message = "Failed to remove patient from queue." });
            }
        }

        [HttpPost("clear")]
        public async Task<ActionResult> ClearQueue()
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.ClearQueueAsync(tenantId, branchId, userId);
                
                return Ok(new { success = true, message = "Queue cleared successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing queue");
                return StatusCode(500, new { success = false, message = "Failed to clear queue." });
            }
        }

        [HttpPost("call-next")]
        public async Task<ActionResult<QueuePatientResponse>> CallNextPatient()
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                var result = await _queueService.CallNextPatientAsync(tenantId, branchId, userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling next patient");
                return StatusCode(500, new { success = false, message = "Failed to call next patient." });
            }
        }

        [HttpPost("{patientId}/complete")]
        public async Task<ActionResult> CompletePatientService(Guid patientId)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.CompletePatientServiceAsync(tenantId, branchId, patientId, userId);
                
                return Ok(new { success = true, message = "Patient service completed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing patient service");
                return StatusCode(500, new { success = false, message = "Failed to complete patient service." });
            }
        }

        [HttpPut("{patientId}/position")]
        public async Task<ActionResult> UpdatePatientPosition(Guid patientId, [FromBody] UpdatePositionRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.UpdatePatientPositionAsync(tenantId, branchId, patientId, request.NewPosition, userId);
                
                return Ok(new { success = true, message = "Patient position updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient position");
                return StatusCode(500, new { success = false, message = "Failed to update patient position." });
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<QueueHistoryResponse>> GetQueueHistory([FromQuery] QueueHistoryFilters filters)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                
                var history = await _queueService.GetQueueHistoryAsync(tenantId, branchId, filters);
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving queue history");
                return StatusCode(500, new { success = false, message = "Failed to retrieve queue history." });
            }
        }

        // Emergency Queue endpoints
        [HttpGet("emergency")]
        public async Task<ActionResult<EmergencyQueueResponse>> GetEmergencyQueue()
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var emergencyQueue = await _queueService.GetEmergencyQueueAsync(tenantId, branchId);
                
                return Ok(emergencyQueue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emergency queue");
                return StatusCode(500, new { success = false, message = "Failed to retrieve emergency queue." });
            }
        }

        [HttpPost("emergency/add")]
        public async Task<ActionResult<QueuePatientResponse>> AddEmergencyPatient([FromBody] AddPatientRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                request.Priority = "emergency";
                var result = await _queueService.AddPatientToQueueAsync(tenantId, branchId, request, userId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding emergency patient to queue");
                return StatusCode(500, new { success = false, message = "Failed to add emergency patient to queue." });
            }
        }

        // Provider Management endpoints
        [HttpGet("providers")]
        public async Task<ActionResult<ProvidersResponse>> GetProviders()
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var providers = await _queueService.GetProvidersAsync(tenantId, branchId);
                
                return Ok(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving providers");
                return StatusCode(500, new { success = false, message = "Failed to retrieve providers." });
            }
        }

        [HttpPost("{patientId}/assign-provider")]
        public async Task<ActionResult> AssignProvider(Guid patientId, [FromBody] AssignProviderRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.AssignProviderAsync(tenantId, branchId, patientId, request.ProviderId, userId);
                
                return Ok(new { success = true, message = "Provider assigned successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning provider");
                return StatusCode(500, new { success = false, message = "Failed to assign provider." });
            }
        }

        [HttpPut("{patientId}/priority")]
        public async Task<ActionResult> UpdatePatientPriority(Guid patientId, [FromBody] UpdatePriorityRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.UpdatePatientPriorityAsync(tenantId, branchId, patientId, request.Priority, userId);
                
                return Ok(new { success = true, message = "Patient priority updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient priority");
                return StatusCode(500, new { success = false, message = "Failed to update patient priority." });
            }
        }

        // Bulk Operations
        [HttpPost("bulk/serve")]
        public async Task<ActionResult> BulkServePatients([FromBody] BulkOperationRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.BulkServePatientsAsync(tenantId, branchId, request.PatientIds, userId);
                
                return Ok(new { success = true, message = "Patients served successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk serving patients");
                return StatusCode(500, new { success = false, message = "Failed to bulk serve patients." });
            }
        }

        [HttpPost("bulk/remove")]
        public async Task<ActionResult> BulkRemovePatients([FromBody] BulkOperationRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await _queueService.BulkRemovePatientsAsync(tenantId, branchId, request.PatientIds, userId);
                
                return Ok(new { success = true, message = "Patients removed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk removing patients");
                return StatusCode(500, new { success = false, message = "Failed to bulk remove patients." });
            }
        }

        // Export endpoints
        [HttpGet("export")]
        public async Task<FileResult> ExportQueue([FromQuery] QueueExportFilters filters)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                
                var fileBytes = await _queueService.ExportQueueAsync(tenantId, branchId, filters);
                var contentType = filters.Format.ToLower() == "pdf" ? "application/pdf" : "text/csv";
                var fileName = $"queue_export_{DateTime.Now:yyyyMMdd}.{filters.Format.ToLower()}";
                
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting queue");
                throw;
            }
        }

        [HttpPost("print/daily-report")]
        public async Task<ActionResult> PrintDailyReport([FromBody] DailyReportRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                
                await _queueService.PrintDailyReportAsync(tenantId, branchId, request.Date);
                
                return Ok(new { success = true, message = "Daily report sent to printer successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing daily report");
                return StatusCode(500, new { success = false, message = "Failed to print daily report." });
            }
        }

        [HttpPost("{patientId}/print/slip")]
        public async Task<ActionResult> PrintQueueSlip(Guid patientId)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();
                
                await _queueService.PrintQueueSlipAsync(tenantId, branchId, patientId);
                
                return Ok(new { success = true, message = "Queue slip printed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing queue slip");
                return StatusCode(500, new { success = false, message = "Failed to print queue slip." });
            }
        }
    }
}
