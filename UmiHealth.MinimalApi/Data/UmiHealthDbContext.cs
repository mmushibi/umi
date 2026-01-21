using Microsoft.EntityFrameworkCore;
using UmiHealth.MinimalApi.Models;

namespace UmiHealth.MinimalApi.Data
{
    public class UmiHealthDbContext : DbContext
    {
        public UmiHealthDbContext(DbContextOptions<UmiHealthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Inventory> Inventory { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleItem> SaleItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50).HasDefaultValue("user");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.TenantId).IsRequired();

                entity.HasOne(u => u.Tenant)
                      .WithMany(t => t.Users)
                      .HasForeignKey(u => u.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure Tenant entity
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.SubscriptionPlan).IsRequired().HasMaxLength(50).HasDefaultValue("Care");

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure Patient entity
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.EmergencyContact).HasMaxLength(100);
                entity.Property(e => e.EmergencyPhone).HasMaxLength(20);
                entity.Property(e => e.BloodType).HasMaxLength(50);
                entity.Property(e => e.Allergies).HasMaxLength(500);
                entity.Property(e => e.MedicalHistory).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.TenantId).IsRequired();

                entity.HasOne(p => p.Tenant)
                      .WithMany()
                      .HasForeignKey(p => p.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Inventory entity
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.GenericName).HasMaxLength(100);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.ProductCode).HasMaxLength(20);
                entity.Property(e => e.Barcode).HasMaxLength(20);
                entity.Property(e => e.Unit).HasMaxLength(50).HasDefaultValue("pieces");
                entity.Property(e => e.Manufacturer).HasMaxLength(255);
                entity.Property(e => e.Supplier).HasMaxLength(100);
                entity.Property(e => e.ExpiryDate).HasMaxLength(10);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.TenantId).IsRequired();

                entity.HasOne(i => i.Tenant)
                      .WithMany()
                      .HasForeignKey(i => i.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Prescription entity
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.PatientId).IsRequired();
                entity.Property(e => e.DoctorId).IsRequired();
                entity.Property(e => e.PrescriptionNumber).HasMaxLength(50);
                entity.Property(e => e.Diagnosis).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("active");
                entity.Property(e => e.FilledBy).HasMaxLength(100);
                entity.Property(e => e.TenantId).IsRequired();

                entity.HasOne(p => p.Patient)
                      .WithMany()
                      .HasForeignKey(p => p.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Doctor)
                      .WithMany()
                      .HasForeignKey(p => p.DoctorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Tenant)
                      .WithMany()
                      .HasForeignKey(p => p.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PrescriptionItem entity
            modelBuilder.Entity<PrescriptionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.PrescriptionId).IsRequired();
                entity.Property(e => e.InventoryId).IsRequired();
                entity.Property(e => e.MedicationName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Dosage).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Frequency).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Duration).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Instructions).HasMaxLength(500);

                entity.HasOne(pi => pi.Prescription)
                      .WithMany(p => p.Items)
                      .HasForeignKey(pi => pi.PrescriptionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pi => pi.Inventory)
                      .WithMany()
                      .HasForeignKey(pi => pi.InventoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Sale entity
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.PatientId).IsRequired();
                entity.Property(e => e.CashierId).IsRequired();
                entity.Property(e => e.SaleNumber).HasMaxLength(50);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50).HasDefaultValue("cash");
                entity.Property(e => e.PaymentReference).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("completed");
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.TenantId).IsRequired();

                entity.HasOne(s => s.Patient)
                      .WithMany()
                      .HasForeignKey(s => s.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Cashier)
                      .WithMany()
                      .HasForeignKey(s => s.CashierId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Tenant)
                      .WithMany()
                      .HasForeignKey(s => s.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SaleItem entity
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.SaleId).IsRequired();
                entity.Property(e => e.InventoryId).IsRequired();
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);

                entity.HasOne(si => si.Sale)
                      .WithMany(s => s.Items)
                      .HasForeignKey(si => si.SaleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(si => si.Inventory)
                      .WithMany()
                      .HasForeignKey(si => si.InventoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payment entity
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.PatientId).IsRequired();
                entity.Property(e => e.SaleId).IsRequired();
                entity.Property(e => e.PaymentNumber).HasMaxLength(50);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50).HasDefaultValue("cash");
                entity.Property(e => e.PaymentReference).HasMaxLength(100);
                entity.Property(e => e.TransactionId).HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("completed");
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.TenantId).IsRequired();

                entity.HasOne(p => p.Patient)
                      .WithMany()
                      .HasForeignKey(p => p.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Sale)
                      .WithMany()
                      .HasForeignKey(p => p.SaleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Tenant)
                      .WithMany()
                      .HasForeignKey(p => p.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Report entity
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.ReportName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ReportType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.GeneratedBy).HasMaxLength(100);
                entity.Property(e => e.FilePath).HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("generated");
                entity.Property(e => e.Parameters).HasMaxLength(1000);
                entity.Property(e => e.TenantId).IsRequired();

                entity.HasOne(r => r.Tenant)
                      .WithMany()
                      .HasForeignKey(r => r.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
