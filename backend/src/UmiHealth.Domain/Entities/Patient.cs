using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities;

public class Patient : TenantEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? PassportNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AlternativePhone { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string InsuranceProvider { get; set; } = string.Empty;
    public string InsurancePolicyNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
