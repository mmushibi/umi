namespace UmiHealth.Core.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public string SubscriptionPlan { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; }
    public bool IsSuspended { get; set; }

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
