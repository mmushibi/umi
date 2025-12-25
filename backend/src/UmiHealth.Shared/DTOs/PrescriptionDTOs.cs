namespace UmiHealth.Shared.DTOs;

public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public PatientDto Patient { get; set; } = new();
    public Guid DoctorId { get; set; }
    public UserDto Doctor { get; set; } = new();
    public string PrescriptionNumber { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string DoctorNotes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DispensedDate { get; set; }
    public UserDto? DispensedByUser { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRefillable { get; set; }
    public int RefillCount { get; set; }
    public int MaxRefills { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePrescriptionDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string DoctorNotes { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsRefillable { get; set; }
    public int MaxRefills { get; set; }
    public List<CreatePrescriptionItemDto> Items { get; set; } = new();
}

public class UpdatePrescriptionDto
{
    public string Diagnosis { get; set; } = string.Empty;
    public string DoctorNotes { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsRefillable { get; set; }
    public int MaxRefills { get; set; }
}

public class PrescriptionItemDto
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public Guid ProductId { get; set; }
    public ProductDto Product { get; set; } = new();
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string DurationUnit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public bool IsDispensed { get; set; }
    public DateTime? DispensedDate { get; set; }
    public int DispensedQuantity { get; set; }
    public string? DispensedBatchNumber { get; set; }
    public DateTime? DispensedExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePrescriptionItemDto
{
    public Guid ProductId { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string DurationUnit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Instructions { get; set; } = string.Empty;
}

public class DispensePrescriptionDto
{
    public Guid DispensedBy { get; set; }
    public List<DispenseItemDto> Items { get; set; } = new();
}

public class DispenseItemDto
{
    public Guid PrescriptionItemId { get; set; }
    public int DispensedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class PrescriptionFilterDto : PagedRequest
{
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string? PatientName { get; set; }
}
