using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;

        public PrescriptionsController(IPrescriptionService prescriptionService)
        {
            _prescriptionService = prescriptionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetPrescriptions([FromQuery] PrescriptionQueryParameters parameters)
        {
            try
            {
                var tenantId = GetTenantId();
                var prescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId, parameters);
                
                return Ok(new PrescriptionListResponse
                {
                    Prescriptions = prescriptions,
                    TotalCount = prescriptions.Count,
                    Page = parameters.Page ?? 1,
                    PageSize = parameters.PageSize ?? 20
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve prescriptions." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PrescriptionDto>> GetPrescription(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var prescription = await _prescriptionService.GetPrescriptionByIdAsync(tenantId, id);
                
                if (prescription == null)
                    return NotFound();

                return Ok(prescription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve prescription." });
            }
        }

        [HttpPost]
        public async Task<ActionResult<PrescriptionDto>> CreatePrescription([FromBody] CreatePrescriptionRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var prescription = await _prescriptionService.CreatePrescriptionAsync(tenantId, userId, request);
                
                return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, prescription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to create prescription." });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PrescriptionDto>> UpdatePrescription(Guid id, [FromBody] UpdatePrescriptionRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var prescription = await _prescriptionService.UpdatePrescriptionAsync(tenantId, id, userId, request);
                
                if (prescription == null)
                    return NotFound();

                return Ok(prescription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update prescription." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePrescription(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var success = await _prescriptionService.DeletePrescriptionAsync(tenantId, id, userId);
                
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete prescription." });
            }
        }

        [HttpPost("{id}/dispense")]
        public async Task<ActionResult<PrescriptionDto>> DispensePrescription(Guid id, [FromBody] DispensePrescriptionRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var prescription = await _prescriptionService.DispensePrescriptionAsync(tenantId, id, userId, request);
                
                if (prescription == null)
                    return NotFound();

                return Ok(prescription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to dispense prescription." });
            }
        }

        [HttpPost("{id}/verify")]
        public async Task<ActionResult<PrescriptionDto>> VerifyPrescription(Guid id, [FromBody] VerifyPrescriptionRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var prescription = await _prescriptionService.VerifyPrescriptionAsync(tenantId, id, userId, request);
                
                if (prescription == null)
                    return NotFound();

                return Ok(prescription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to verify prescription." });
            }
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetPendingPrescriptions()
        {
            try
            {
                var tenantId = GetTenantId();
                var prescriptions = await _prescriptionService.GetPendingPrescriptionsAsync(tenantId);
                
                return Ok(prescriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve pending prescriptions." });
            }
        }

        [HttpGet("expired")]
        public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetExpiredPrescriptions()
        {
            try
            {
                var tenantId = GetTenantId();
                var prescriptions = await _prescriptionService.GetExpiredPrescriptionsAsync(tenantId);
                
                return Ok(prescriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve expired prescriptions." });
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetPrescriptionsByPatient(Guid patientId)
        {
            try
            {
                var tenantId = GetTenantId();
                var prescriptions = await _prescriptionService.GetPrescriptionsByPatientAsync(tenantId, patientId);
                
                return Ok(prescriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve patient prescriptions." });
            }
        }

        private Guid GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                throw new UnauthorizedAccessException("Tenant information not found");
            
            return Guid.Parse(tenantIdClaim);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User information not found");
            
            return Guid.Parse(userIdClaim);
        }
    }

    public class PrescriptionQueryParameters
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? PatientId { get; set; }
        public string? DoctorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public class CreatePrescriptionRequest
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public List<PrescriptionItemRequest> Medications { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsUrgent { get; set; }
        public string? ExternalReference { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }

    public class UpdatePrescriptionRequest
    {
        public Guid? PatientId { get; set; }
        public Guid? DoctorId { get; set; }
        public string? Diagnosis { get; set; }
        public List<PrescriptionItemRequest>? Medications { get; set; }
        public string? Notes { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? IsUrgent { get; set; }
        public string? ExternalReference { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }

    public class PrescriptionItemRequest
    {
        public Guid ProductId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class DispensePrescriptionRequest
    {
        public List<DispensedItemRequest> DispensedItems { get; set; } = new();
        public string DispensedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime? DispensedDate { get; set; }
    }

    public class DispensedItemRequest
    {
        public Guid ProductId { get; set; }
        public int QuantityDispensed { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
    }

    public class VerifyPrescriptionRequest
    {
        public bool IsVerified { get; set; }
        public string VerificationNotes { get; set; } = string.Empty;
        public string VerifiedBy { get; set; } = string.Empty;
        public DateTime? VerificationDate { get; set; }
    }

    public class PrescriptionListResponse
    {
        public IEnumerable<PrescriptionDto> Prescriptions { get; set; } = new List<PrescriptionDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class PrescriptionDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public List<PrescriptionItemDto> Medications { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty; // pending, verified, dispensed, expired, cancelled
        public bool IsUrgent { get; set; }
        public string? ExternalReference { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        
        // Related entities
        public PatientDto? Patient { get; set; }
        public DoctorDto? Doctor { get; set; }
        public DispenseInfoDto? DispenseInfo { get; set; }
    }

    public class PrescriptionItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int QuantityDispensed { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public ProductDto? Product { get; set; }
    }

    public class DispenseInfoDto
    {
        public DateTime DispensedDate { get; set; }
        public string DispensedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<DispensedItemDto> DispensedItems { get; set; } = new();
    }

    public class DispensedItemDto
    {
        public Guid ProductId { get; set; }
        public int QuantityDispensed { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
    }

    public class PatientDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? MedicalAidNumber { get; set; }
        public string? MedicalAidProvider { get; set; }
    }

    public class DoctorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
