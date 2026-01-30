using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmiHealth.MinimalApi.Services;
using UmiHealth.MinimalApi.Models;
using System.Security.Claims;

namespace UmiHealth.MinimalApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly IAuditService _auditService;

    public PatientController(IPatientService patientService, IAuditService auditService)
    {
        _patientService = patientService;
        _auditService = auditService;
    }

    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetPatient(string patientId)
    {
        var patient = await _patientService.GetPatientByIdAsync(patientId);
        if (patient == null)
        {
            return NotFound(new { success = false, message = "Patient not found" });
        }

        return Ok(new { success = true, patient });
    }

    [HttpGet]
    public async Task<IActionResult> GetPatients()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var patients = await _patientService.GetPatientsByTenantAsync(tenantId);
        return Ok(new { success = true, patients });
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchPatients([FromBody] SearchRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var patients = await _patientService.SearchPatientsAsync(tenantId, request.SearchTerm);
        return Ok(new { success = true, patients });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> CreatePatient([FromBody] Patient patient)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        patient.TenantId = tenantId;
        var result = await _patientService.CreatePatientAsync(patient);
        
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, patient = result.Patient });
    }

    [HttpPut("{patientId}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> UpdatePatient(string patientId, [FromBody] Patient patient)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var existingPatient = await _patientService.GetPatientByIdAsync(patientId);
        if (existingPatient == null || existingPatient.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Patient not found" });
        }

        patient.Id = patientId;
        patient.TenantId = tenantId;
        
        var result = await _patientService.UpdatePatientAsync(patientId, patient);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, patient = result.Patient });
    }

    [HttpDelete("{patientId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePatient(string patientId)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var existingPatient = await _patientService.GetPatientByIdAsync(patientId);
        if (existingPatient == null || existingPatient.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Patient not found" });
        }

        var result = await _patientService.DeletePatientAsync(patientId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost("{patientId}/allergies")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> AddAllergy(string patientId, [FromBody] AllergyRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var existingPatient = await _patientService.GetPatientByIdAsync(patientId);
        if (existingPatient == null || existingPatient.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Patient not found" });
        }

        var result = await _patientService.AddPatientAllergyAsync(patientId, request.Allergy);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpDelete("{patientId}/allergies")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> RemoveAllergy(string patientId, [FromBody] AllergyRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var existingPatient = await _patientService.GetPatientByIdAsync(patientId);
        if (existingPatient == null || existingPatient.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Patient not found" });
        }

        var result = await _patientService.RemovePatientAllergyAsync(patientId, request.Allergy);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }
}

public class SearchRequest
{
    public string SearchTerm { get; set; } = string.Empty;
}

public class AllergyRequest
{
    public string Allergy { get; set; } = string.Empty;
}
