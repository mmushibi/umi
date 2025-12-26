using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities;

public class Prescription : TenantEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string DoctorNotes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Active, Completed, Cancelled, Expired
    public DateTime? DispensedDate { get; set; }
    public Guid? DispensedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRefillable { get; set; }
    public int RefillCount { get; set; }
    public int MaxRefills { get; set; }

    public virtual Patient Patient { get; set; } = null!;
    public virtual User Doctor { get; set; } = null!;
    public virtual User? DispensedByUser { get; set; }
    public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

public class PrescriptionItem : TenantEntity
{
    public Guid PrescriptionId { get; set; }
    public Guid ProductId { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string DurationUnit { get; set; } = string.Empty; // Days, Weeks, Months
    public int Quantity { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public bool IsDispensed { get; set; }
    public DateTime? DispensedDate { get; set; }
    public int DispensedQuantity { get; set; }
    public string? DispensedBatchNumber { get; set; }
    public DateTime? DispensedExpiryDate { get; set; }

    public virtual Prescription Prescription { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
