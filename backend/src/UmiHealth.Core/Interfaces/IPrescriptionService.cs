using UmiHealth.Domain.Entities;

namespace UmiHealth.Core.Interfaces;

public interface IPrescriptionService
{
    Task<IReadOnlyList<Prescription>> GetPrescriptionsAsync(Guid tenantId, PrescriptionFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Prescription?> GetPrescriptionAsync(Guid prescriptionId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Prescription> CreatePrescriptionAsync(Guid tenantId, CreatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<Prescription> UpdatePrescriptionAsync(Guid prescriptionId, Guid tenantId, UpdatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DispensePrescriptionAsync(Guid prescriptionId, Guid tenantId, DispensePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<bool> CancelPrescriptionAsync(Guid prescriptionId, Guid tenantId, CancellationToken cancellationToken = default);
}

public record PrescriptionFilter(
    Guid? PatientId,
    Guid? DoctorId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Status,
    string? PrescriptionNumber,
    string? PatientName
);

public record CreatePrescriptionRequest(
    Guid PatientId,
    Guid DoctorId,
    string Diagnosis,
    string DoctorNotes,
    DateTime? ExpiresAt,
    bool IsRefillable,
    int MaxRefills,
    List<CreatePrescriptionItemRequest> Items
);

public record UpdatePrescriptionRequest(
    string Diagnosis,
    string DoctorNotes,
    DateTime? ExpiresAt,
    bool IsRefillable,
    int MaxRefills
);

public record CreatePrescriptionItemRequest(
    Guid ProductId,
    string Dosage,
    string Frequency,
    string Route,
    int Duration,
    string DurationUnit,
    int Quantity,
    string Instructions
);

public record DispensePrescriptionRequest(
    Guid DispensedBy,
    List<DispenseItemRequest> Items
);

public record DispenseItemRequest(
    Guid PrescriptionItemId,
    int DispensedQuantity,
    string? BatchNumber,
    DateTime? ExpiryDate
);
