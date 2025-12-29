using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Core.Entities;

namespace UmiHealth.Infrastructure.Data
{
    public class TenantDbContextFactory
    {
        private readonly IConfiguration _configuration;
        private readonly string _sharedConnectionString;

        public TenantDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
            _sharedConnectionString = _configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not configured");
        }

        public async Task<TenantDbContext> CreateDbContextAsync(string tenantId)
        {
            // Get tenant database name from shared database
            var databaseName = await GetTenantDatabaseName(tenantId);
            
            // Build tenant-specific connection string
            var tenantConnectionString = BuildTenantConnectionString(databaseName);
            
            // Create options for tenant context
            var options = new DbContextOptionsBuilder<TenantDbContext>()
                .UseNpgsql(tenantConnectionString)
                .Options;

            return new TenantDbContext(options);
        }

        public TenantDbContext CreateDbContext(string tenantId)
        {
            return CreateDbContextAsync(tenantId).GetAwaiter().GetResult();
        }

        private async Task<string> GetTenantDatabaseName(string tenantId)
        {
            using var sharedContext = new SharedDbContext(
                new DbContextOptionsBuilder<SharedDbContext>()
                    .UseNpgsql(_sharedConnectionString)
                    .Options);

            var tenant = await sharedContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id.ToString() == tenantId);

            if (tenant == null)
            {
                throw new InvalidOperationException($"Tenant with ID {tenantId} not found");
            }

            return tenant.DatabaseName;
        }

        private string BuildTenantConnectionString(string databaseName)
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(_sharedConnectionString)
            {
                Database = databaseName
            };

            return builder.ToString();
        }
    }

    public class TenantDbContext : DbContext
    {
        public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
        {
        }

        // Tenant-specific tables
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigurePatient(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureInventory(modelBuilder);
            ConfigurePrescription(modelBuilder);
            ConfigureSale(modelBuilder);
            ConfigurePayment(modelBuilder);
            ConfigureAuditLog(modelBuilder);

            ApplySoftDeletes(modelBuilder);
        }

        private void ConfigurePatient(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("tenant_patients");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PatientNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Phone)
                    .HasMaxLength(50);

                entity.Property(e => e.Email)
                    .HasMaxLength(100);

                entity.Property(e => e.EmergencyContact)
                    .HasColumnType("jsonb");

                entity.Property(e => e.MedicalHistory)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Allergies)
                    .HasColumnType("jsonb");

                entity.Property(e => e.InsuranceInfo)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("active");

                entity.HasIndex(e => e.PatientNumber)
                    .IsUnique();

                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.Phone);
                entity.HasIndex(e => e.Email);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("tenant_products");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Sku)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.GenericName)
                    .HasMaxLength(255);

                entity.Property(e => e.Category)
                    .HasMaxLength(100);

                entity.Property(e => e.Manufacturer)
                    .HasMaxLength(100);

                entity.Property(e => e.Strength)
                    .HasMaxLength(50);

                entity.Property(e => e.Form)
                    .HasMaxLength(50);

                entity.Property(e => e.StorageRequirements)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Pricing)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Barcode)
                    .HasMaxLength(100);

                entity.Property(e => e.Images)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("active");

                entity.HasIndex(e => e.Sku)
                    .IsUnique();

                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Barcode);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void ConfigureInventory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("tenant_inventory");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.BatchNumber)
                    .HasMaxLength(100);

                entity.Property(e => e.Location)
                    .HasMaxLength(100);

                entity.HasIndex(e => new { e.BranchId, e.ProductId });
                entity.HasIndex(e => e.BatchNumber);
                entity.HasIndex(e => e.ExpiryDate);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void ConfigurePrescription(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.ToTable("tenant_prescriptions");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PrescriptionNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("pending");

                entity.Property(e => e.Items)
                    .HasColumnType("jsonb");

                entity.Property(e => e.DispensedItems)
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.PrescriptionNumber)
                    .IsUnique();

                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.PrescriberId);
                entity.HasIndex(e => e.DatePrescribed);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void ConfigureSale(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.ToTable("tenant_sales");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.SaleNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PaymentMethod)
                    .HasMaxLength(50);

                entity.Property(e => e.PaymentStatus)
                    .HasMaxLength(20)
                    .HasDefaultValue("pending");

                entity.Property(e => e.Items)
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.SaleNumber)
                    .IsUnique();

                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.CashierId);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("tenant_payments");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.TransactionReference)
                    .HasMaxLength(100);

                entity.Property(e => e.PaymentGateway)
                    .HasMaxLength(50);

                entity.Property(e => e.GatewayResponse)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("pending");

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .HasDefaultValue("ZMW");

                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.SaleId);
                entity.HasIndex(e => e.TransactionReference);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("tenant_audit_logs");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EntityType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UserAgent)
                    .HasColumnType("text");

                entity.Property(e => e.SessionId)
                    .HasMaxLength(255);

                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.Timestamp);

                entity.Property(e => e.Timestamp)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void ApplySoftDeletes(ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(e => typeof(ISoftDeletable).IsAssignableFrom(e.ClrType));

            foreach (var entityType in entityTypes)
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, "DeletedAt");
                var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null));
                var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
