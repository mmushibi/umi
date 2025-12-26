namespace UmiHealth.Core.Entities;

public class Prescription : TenantEntity
{
    public string PrescriptionNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string? Diagnosis { get; set; }
    public string? DoctorNotes { get; set; }
    public Guid? DispensedBy { get; set; }
    public DateTime? DispensedAt { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Dispensed, Cancelled
    
    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
    public virtual User? Doctor { get; set; }
    public virtual User? DispensedByUser { get; set; }
    public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
