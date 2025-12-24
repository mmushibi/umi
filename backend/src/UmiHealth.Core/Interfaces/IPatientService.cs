namespace UmiHealth.Core.Interfaces;

public interface IPatientService
{
    Task<IReadOnlyList<Patient>> GetPatientsAsync(Guid tenantId, PatientFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Patient?> GetPatientAsync(Guid patientId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Patient> CreatePatientAsync(Guid tenantId, CreatePatientRequest request, CancellationToken cancellationToken = default);
    Task<Patient> UpdatePatientAsync(Guid patientId, Guid tenantId, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePatientAsync(Guid patientId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<PatientHistory> GetPatientHistoryAsync(Guid patientId, Guid tenantId, CancellationToken cancellationToken = default);
}

public record PatientFilter(
    string? Search,
    string? Gender,
    DateTime? DateOfBirthFrom,
    DateTime? DateOfBirthTo,
    string? BloodType,
    string? InsuranceProvider,
    bool? IsActive
);

public record CreatePatientRequest(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string? NationalId,
    string? PassportNumber,
    string Email,
    string PhoneNumber,
    string? AlternativePhone,
    string Address,
    string City,
    string Country,
    string PostalCode,
    string BloodType,
    string? Allergies,
    string? ChronicConditions,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelationship,
    string InsuranceProvider,
    string InsurancePolicyNumber
);

public record UpdatePatientRequest(
    string FirstName,
    string LastName,
    string Gender,
    string? NationalId,
    string? PassportNumber,
    string Email,
    string PhoneNumber,
    string? AlternativePhone,
    string Address,
    string City,
    string Country,
    string PostalCode,
    string BloodType,
    string? Allergies,
    string? ChronicConditions,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelationship,
    string InsuranceProvider,
    string InsurancePolicyNumber,
    bool IsActive
);

public record PatientHistory(
    Guid PatientId,
    PatientInfo Patient,
    List<PrescriptionSummary> Prescriptions,
    List<SaleSummary> Sales,
    List<AllergyInfo> Allergies,
    List<ConditionInfo> ChronicConditions
);

public record PatientInfo(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string Email,
    string PhoneNumber
);

public record PrescriptionSummary(
    Guid Id,
    string PrescriptionNumber,
    DateTime PrescriptionDate,
    string DoctorName,
    string Status,
    int MedicationCount
);

public record SaleSummary(
    Guid Id,
    string SaleNumber,
    DateTime SaleDate,
    decimal TotalAmount,
    string Status,
    int ItemCount
);

public record AllergyInfo(
    string Allergy,
    string Severity,
    DateTime RecordedAt
);

public record ConditionInfo(
    string Condition,
    string DiagnosisDate,
    string Status
);
