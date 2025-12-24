using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(SharedDbContext context, ILogger<PatientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var query = _context.Patients.Where(p => p.DeletedAt == null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => 
                        p.FirstName.Contains(search) || 
                        p.LastName.Contains(search) ||
                        p.PatientNumber.Contains(search) ||
                        (p.Email != null && p.Email.Contains(search)) ||
                        (p.Phone != null && p.Phone.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                var patients = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    patients,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(Guid id)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

                if (patient == null)
                {
                    return NotFound();
                }

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient {PatientId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient(CreatePatientRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId))
                {
                    return Unauthorized("Tenant or branch not found");
                }

                var patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    BranchId = Guid.Parse(branchId),
                    PatientNumber = await GeneratePatientNumberAsync(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Phone = request.Phone,
                    Email = request.Email,
                    Address = request.Address,
                    EmergencyContact = request.EmergencyContact ?? new Dictionary<string, object>(),
                    MedicalHistory = request.MedicalHistory ?? new Dictionary<string, object>(),
                    Allergies = request.Allergies ?? new Dictionary<string, object>(),
                    InsuranceInfo = request.InsuranceInfo ?? new Dictionary<string, object>(),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(Guid id, UpdatePatientRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

                if (patient == null)
                {
                    return NotFound();
                }

                patient.FirstName = request.FirstName ?? patient.FirstName;
                patient.LastName = request.LastName ?? patient.LastName;
                patient.DateOfBirth = request.DateOfBirth ?? patient.DateOfBirth;
                patient.Gender = request.Gender ?? patient.Gender;
                patient.Phone = request.Phone ?? patient.Phone;
                patient.Email = request.Email ?? patient.Email;
                patient.Address = request.Address ?? patient.Address;
                patient.EmergencyContact = request.EmergencyContact ?? patient.EmergencyContact;
                patient.MedicalHistory = request.MedicalHistory ?? patient.MedicalHistory;
                patient.Allergies = request.Allergies ?? patient.Allergies;
                patient.InsuranceInfo = request.InsuranceInfo ?? patient.InsuranceInfo;
                patient.Status = request.Status ?? patient.Status;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

                if (patient == null)
                {
                    return NotFound();
                }

                patient.DeletedAt = DateTime.UtcNow;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<PatientStats>> GetPatientStats()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var totalPatients = await _context.Patients.CountAsync(p => p.DeletedAt == null);
                var activePatients = await _context.Patients.CountAsync(p => p.DeletedAt == null && p.Status == "active");
                var inactivePatients = await _context.Patients.CountAsync(p => p.DeletedAt == null && p.Status == "inactive");
                
                var recentPatients = await _context.Patients
                    .Where(p => p.DeletedAt == null && p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                return Ok(new PatientStats
                {
                    TotalPatients = totalPatients,
                    ActivePatients = activePatients,
                    InactivePatients = inactivePatients,
                    RecentPatients = recentPatients
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient stats");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<string> GeneratePatientNumberAsync()
        {
            var prefix = "PAT";
            var year = DateTime.UtcNow.Year.ToString();
            var random = new Random();
            
            for (int i = 0; i < 10; i++)
            {
                var suffix = random.Next(1000, 9999).ToString();
                var patientNumber = $"{prefix}{year}{suffix}";
                
                var exists = await _context.Patients.AnyAsync(p => p.PatientNumber == patientNumber);
                if (!exists)
                {
                    return patientNumber;
                }
            }

            throw new Exception("Unable to generate unique patient number");
        }
    }

    public class CreatePatientRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public Dictionary<string, object>? EmergencyContact { get; set; }
        public Dictionary<string, object>? MedicalHistory { get; set; }
        public Dictionary<string, object>? Allergies { get; set; }
        public Dictionary<string, object>? InsuranceInfo { get; set; }
    }

    public class UpdatePatientRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public Dictionary<string, object>? EmergencyContact { get; set; }
        public Dictionary<string, object>? MedicalHistory { get; set; }
        public Dictionary<string, object>? Allergies { get; set; }
        public Dictionary<string, object>? InsuranceInfo { get; set; }
        public string? Status { get; set; }
    }

    public class PatientStats
    {
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int InactivePatients { get; set; }
        public int RecentPatients { get; set; }
    }
}
