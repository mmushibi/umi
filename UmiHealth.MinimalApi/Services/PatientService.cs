using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace UmiHealth.MinimalApi.Services;

public class PatientService : IPatientService
{
    private readonly UmiHealthDbContext _context;
    private readonly IValidationService _validationService;
    private readonly IAuditService _auditService;

    public PatientService(UmiHealthDbContext context, IValidationService validationService, IAuditService auditService)
    {
        _context = context;
        _validationService = validationService;
        _auditService = auditService;
    }

    public async Task<(bool Success, string Message, Patient? Patient)> CreatePatientAsync(Patient patient)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patient.FirstName) || string.IsNullOrWhiteSpace(patient.LastName))
            {
                return (false, "First name and last name are required", null);
            }

            if (!_validationService.IsValidEmail(patient.Email))
            {
                return (false, "Valid email address is required", null);
            }

            if (!_validationService.IsValidPhoneNumber(patient.PhoneNumber))
            {
                return (false, "Valid phone number is required", null);
            }

            patient.Id = Guid.NewGuid().ToString();
            patient.CreatedAt = DateTime.UtcNow;
            patient.UpdatedAt = DateTime.UtcNow;

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            _auditService.LogSuperAdminAction(
                null,
                patient.TenantId,
                "PATIENT_CREATED",
                "Patient",
                new Dictionary<string, object> { ["PatientId"] = patient.Id, ["Name"] = $"{patient.FirstName} {patient.LastName}" }
            );

            return (true, "Patient created successfully", patient);
        }
        catch (Exception ex)
        {
            return (false, $"Patient creation error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, Patient? Patient)> UpdatePatientAsync(string patientId, Patient patient)
    {
        try
        {
            var existingPatient = await _context.Patients.FindAsync(patientId);
            if (existingPatient == null)
            {
                return (false, "Patient not found", null);
            }

            existingPatient.FirstName = _validationService.SanitizeInput(patient.FirstName);
            existingPatient.LastName = _validationService.SanitizeInput(patient.LastName);
            existingPatient.Email = _validationService.SanitizeInput(patient.Email);
            existingPatient.PhoneNumber = _validationService.SanitizeInput(patient.PhoneNumber);
            existingPatient.DateOfBirth = patient.DateOfBirth;
            existingPatient.Gender = _validationService.SanitizeInput(patient.Gender);
            existingPatient.Address = _validationService.SanitizeInput(patient.Address);
            existingPatient.EmergencyContact = _validationService.SanitizeInput(patient.EmergencyContact);
            existingPatient.MedicalHistory = _validationService.SanitizeInput(patient.MedicalHistory);
            existingPatient.Allergies = _validationService.SanitizeInput(patient.Allergies);
            existingPatient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Patient updated successfully", existingPatient);
        }
        catch (Exception ex)
        {
            return (false, $"Patient update error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeletePatientAsync(string patientId)
    {
        try
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return (false, "Patient not found");
            }

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            _auditService.LogSuperAdminAction(
                null,
                patient.TenantId,
                "PATIENT_DELETED",
                "Patient",
                new Dictionary<string, object> { ["PatientId"] = patientId, ["Name"] = $"{patient.FirstName} {patient.LastName}" }
            );

            return (true, "Patient deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Patient deletion error: {ex.Message}");
        }
    }

    public async Task<Patient?> GetPatientByIdAsync(string patientId)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId);
    }

    public async Task<List<Patient>> GetPatientsByTenantAsync(string tenantId)
    {
        return await _context.Patients
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<List<Patient>> SearchPatientsAsync(string tenantId, string searchTerm)
    {
        var sanitizedTerm = _validationService.SanitizeInput(searchTerm);
        
        return await _context.Patients
            .Where(p => p.TenantId == tenantId && 
                       (p.FirstName.Contains(sanitizedTerm) ||
                        p.LastName.Contains(sanitizedTerm) ||
                        p.Email.Contains(sanitizedTerm) ||
                        p.PhoneNumber.Contains(sanitizedTerm)))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> AddPatientAllergyAsync(string patientId, string allergy)
    {
        try
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return (false, "Patient not found");
            }

            var sanitizedAllergy = _validationService.SanitizeInput(allergy);
            
            if (string.IsNullOrWhiteSpace(patient.Allergies))
            {
                patient.Allergies = sanitizedAllergy;
            }
            else
            {
                var allergies = patient.Allergies.Split(',').Select(a => a.Trim()).ToList();
                if (!allergies.Contains(sanitizedAllergy))
                {
                    allergies.Add(sanitizedAllergy);
                    patient.Allergies = string.Join(", ", allergies);
                }
                else
                {
                    return (false, "Allergy already exists");
                }
            }

            patient.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Allergy added successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error adding allergy: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RemovePatientAllergyAsync(string patientId, string allergy)
    {
        try
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return (false, "Patient not found");
            }

            if (string.IsNullOrWhiteSpace(patient.Allergies))
            {
                return (false, "No allergies to remove");
            }

            var sanitizedAllergy = _validationService.SanitizeInput(allergy);
            var allergies = patient.Allergies.Split(',').Select(a => a.Trim()).ToList();
            
            if (allergies.Remove(sanitizedAllergy))
            {
                patient.Allergies = allergies.Any() ? string.Join(", ", allergies) : null;
                patient.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (true, "Allergy removed successfully");
            }

            return (false, "Allergy not found");
        }
        catch (Exception ex)
        {
            return (false, $"Error removing allergy: {ex.Message}");
        }
    }
}
