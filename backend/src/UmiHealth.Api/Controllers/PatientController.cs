using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UmiHealth.Core.Interfaces;
using UmiHealth.Application.Services;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientController> _logger;

        public PatientController(
            IPatientService patientService,
            ILogger<PatientController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPatients([FromQuery] Guid tenantId, [FromQuery] PatientFilter? filter = null)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var patients = await _patientService.GetPatientsAsync(tenantId, filter);
                return Ok(new { data = patients, total = patients.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patients for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch patients" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatient(Guid id, [FromQuery] Guid tenantId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var patient = await _patientService.GetPatientAsync(id, tenantId);
                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient {PatientId} for tenant {TenantId}", id, tenantId);
                return StatusCode(500, new { error = "Failed to fetch patient" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromQuery] Guid tenantId, [FromBody] CreatePatientRequest request)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var patient = await _patientService.CreatePatientAsync(tenantId, request);
                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id, tenantId }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to create patient" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromQuery] Guid tenantId, [FromBody] UpdatePatientRequest request)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var patient = await _patientService.UpdatePatientAsync(id, tenantId, request);
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId} for tenant {TenantId}", id, tenantId);
                return StatusCode(500, new { error = "Failed to update patient" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(Guid id, [FromQuery] Guid tenantId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var result = await _patientService.DeletePatientAsync(id, tenantId);
                if (!result)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                return Ok(new { message = "Patient deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId} for tenant {TenantId}", id, tenantId);
                return StatusCode(500, new { error = "Failed to delete patient" });
            }
        }

        [HttpGet("{id}/medical-history")]
        public async Task<IActionResult> GetPatientHistory(Guid id, [FromQuery] Guid tenantId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var history = await _patientService.GetPatientHistoryAsync(id, tenantId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient history {PatientId} for tenant {TenantId}", id, tenantId);
                return StatusCode(500, new { error = "Failed to fetch patient history" });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPatients([FromQuery] Guid tenantId, [FromQuery] string query, [FromQuery] int limit = 10)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var filter = new PatientFilter(
                    Search: query,
                    Gender: null,
                    DateOfBirthFrom: null,
                    DateOfBirthTo: null,
                    BloodType: null,
                    InsuranceProvider: null,
                    IsActive: true
                );

                var patients = await _patientService.GetPatientsAsync(tenantId, filter);
                var limitedResults = patients.Take(limit).ToList();
                
                return Ok(new { data = limitedResults, total = limitedResults.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to search patients" });
            }
        }

        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImportPatients([FromQuery] Guid tenantId, [FromBody] List<CreatePatientRequest> patients)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var results = new List<object>();
                var errors = new List<object>();

                foreach (var patientRequest in patients)
                {
                    try
                    {
                        var patient = await _patientService.CreatePatientAsync(tenantId, patientRequest);
                        results.Add(patient);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new { patient = patientRequest, error = ex.Message });
                    }
                }

                return Ok(new { 
                    message = $"Imported {results.Count} patients successfully",
                    imported = results,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk importing patients for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to bulk import patients" });
            }
        }
    }
}
