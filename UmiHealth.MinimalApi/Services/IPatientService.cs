using UmiHealth.MinimalApi.Models;

namespace UmiHealth.MinimalApi.Services;

public interface IPatientService
{
    Task<(bool Success, string Message, Patient? Patient)> CreatePatientAsync(Patient patient);
    Task<(bool Success, string Message, Patient? Patient)> UpdatePatientAsync(string patientId, Patient patient);
    Task<(bool Success, string Message)> DeletePatientAsync(string patientId);
    Task<Patient?> GetPatientByIdAsync(string patientId);
    Task<List<Patient>> GetPatientsByTenantAsync(string tenantId);
    Task<List<Patient>> SearchPatientsAsync(string tenantId, string searchTerm);
    Task<(bool Success, string Message)> AddPatientAllergyAsync(string patientId, string allergy);
    Task<(bool Success, string Message)> RemovePatientAllergyAsync(string patientId, string allergy);
}
