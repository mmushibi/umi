using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using UmiHealth.Core.Entities;

namespace UmiHealth.Infrastructure
{
    public class UmiHealthDbContext : DbContext
    {
        private readonly Guid? _tenantId;
        private readonly Guid? _userId;
        private readonly string? _userRole;
        private readonly Guid? _branchId;

        public UmiHealthDbContext(DbContextOptions<UmiHealthDbContext> options) : base(options)
        {
        }

        public UmiHealthDbContext(
            DbContextOptions<UmiHealthDbContext> options,
            Guid? tenantId = null,
            Guid? userId = null,
            string? userRole = null,
            Guid? branchId = null) : base(options)
        {
            _tenantId = tenantId;
            _userId = userId;
            _userRole = userRole;
            _branchId = branchId;
        }

        // Tenants
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Branch> Branches { get; set; }

        // Users and Authentication
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RoleClaim> RoleClaims { get; set; }
        public DbSet<UserClaim> UserClaims { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }

        // Products and Inventory
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

        // Patients and Prescriptions
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; }

        // Sales and Payments
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SaleReturn> SaleReturns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure PostgreSQL specific features
            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresExtension("pgcrypto");

            // Configure entities
            ConfigureTenant(modelBuilder);
            ConfigureBranch(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureInventory(modelBuilder);
            ConfigurePatient(modelBuilder);
            ConfigurePrescription(modelBuilder);
            ConfigureSale(modelBuilder);

            // Apply soft delete query filters
            ApplySoftDeleteFilters(modelBuilder);

            // Apply tenant isolation filters
            ApplyTenantIsolationFilters(modelBuilder);
        }

        private void ConfigureTenant(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.SubscriptionPlan).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Subdomain).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });
        }

        private void ConfigureBranch(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.HasOne(e => e.Tenant).WithMany(t => t.Branches).HasForeignKey(e => e.TenantId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Code).IsUnique();
            });
        }

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Tenant).WithMany(t => t.Users).HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Branch).WithMany(b => b.Users).HasForeignKey(e => e.BranchId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
                entity.HasIndex(e => new { e.TenantId, e.UserName }).IsUnique();
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => new { e.TenantId, e.NormalizedName }).IsUnique();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RoleId);
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            });
        }

        private void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Barcode).HasMaxLength(100);
                entity.Property(e => e.UnitPrice).HasDefaultValue(0.00m);
                entity.HasOne(e => e.Tenant).WithMany(t => t.Products).HasForeignKey(e => e.TenantId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Barcode);
                entity.HasIndex(e => e.IsActive);
            });

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.IsActive);
            });
        }

        private void ConfigureInventory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.QuantityOnHand).HasDefaultValue(0);
                entity.Property(e => e.QuantityReserved).HasDefaultValue(0);
                entity.Property(e => e.ReorderLevel).HasDefaultValue(0);
                entity.Property(e => e.UnitCost).HasDefaultValue(0.00m);
                entity.Property(e => e.UnitPrice).HasDefaultValue(0.00m);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Branch).WithMany(b => b.Inventories).HasForeignKey(e => e.BranchId);
                entity.HasOne(e => e.Product).WithMany(p => p.Inventories).HasForeignKey(e => e.ProductId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.BatchNumber);
                entity.HasIndex(e => new { e.TenantId, e.BranchId, e.ProductId, e.BatchNumber }).IsUnique();
            });

            modelBuilder.Entity<StockTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Quantity);
                entity.Property(e => e.UnitCost).HasDefaultValue(0.00m);
                entity.Property(e => e.TotalCost).HasDefaultValue(0.00m);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Inventory).WithMany(i => i.StockTransactions).HasForeignKey(e => e.InventoryId);
                entity.HasOne(e => e.FromBranch).WithMany().HasForeignKey(e => e.FromBranchId);
                entity.HasOne(e => e.ToBranch).WithMany().HasForeignKey(e => e.ToBranchId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.InventoryId);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private void ConfigurePatient(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.NationalId).HasMaxLength(50);
                entity.HasOne(e => e.Tenant).WithMany(t => t.Patients).HasForeignKey(e => e.TenantId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Phone);
                entity.HasIndex(e => e.IsActive);
            });
        }

        private void ConfigurePrescription(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prescription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.PrescriptionNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Diagnosis);
                entity.Property(e => e.DoctorNotes);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Patient).WithMany(p => p.Prescriptions).HasForeignKey(e => e.PatientId);
                entity.HasOne(e => e.Doctor).WithMany().HasForeignKey(e => e.DoctorId);
                entity.HasOne(e => e.DispensedByUser).WithMany().HasForeignKey(e => e.DispensedBy);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.DoctorId);
                entity.HasIndex(e => e.PrescriptionNumber);
                entity.HasIndex(e => new { e.TenantId, e.PrescriptionNumber }).IsUnique();
            });

            modelBuilder.Entity<PrescriptionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Dosage).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Frequency).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Route).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DurationUnit).HasDefaultValue("Days");
                entity.Property(e => e.Quantity);
                entity.Property(e => e.Instructions);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Prescription).WithMany(p => p.Items).HasForeignKey(e => e.PrescriptionId);
                entity.HasOne(e => e.Product).WithMany(p => p.PrescriptionItems).HasForeignKey(e => e.ProductId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.PrescriptionId);
                entity.HasIndex(e => e.ProductId);
            });
        }

        private void ConfigureSale(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subtotal).HasDefaultValue(0.00m);
                entity.Property(e => e.TaxAmount).HasDefaultValue(0.00m);
                entity.Property(e => e.DiscountAmount).HasDefaultValue(0.00m);
                entity.Property(e => e.TotalAmount).HasDefaultValue(0.00m);
                entity.Property(e => e.AmountPaid).HasDefaultValue(0.00m);
                entity.Property(e => e.ChangeAmount).HasDefaultValue(0.00m);
                entity.Property(e => e.Notes);
                entity.Property(e => e.PrescriptionNumber).HasMaxLength(100);
                entity.HasOne(e => e.Tenant).WithMany(t => t.Sales).HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Branch).WithMany(b => b.Sales).HasForeignKey(e => e.BranchId);
                entity.HasOne(e => e.Patient).WithMany(p => p.Sales).HasForeignKey(e => e.PatientId);
                entity.HasOne(e => e.Cashier).WithMany().HasForeignKey(e => e.CashierId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.SaleDate);
                entity.HasIndex(e => new { e.TenantId, e.SaleNumber }).IsUnique();
            });

            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Quantity);
                entity.Property(e => e.UnitPrice).HasDefaultValue(0.00m);
                entity.Property(e => e.DiscountPercentage).HasDefaultValue(0.00m);
                entity.Property(e => e.DiscountAmount).HasDefaultValue(0.00m);
                entity.Property(e => e.Subtotal).HasDefaultValue(0.00m);
                entity.Property(e => e.TaxAmount).HasDefaultValue(0.00m);
                entity.Property(e => e.Total).HasDefaultValue(0.00m);
                entity.Property(e => e.BatchNumber).HasMaxLength(100);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Sale).WithMany(s => s.Items).HasForeignKey(e => e.SaleId);
                entity.HasOne(e => e.Product).WithMany(p => p.SaleItems).HasForeignKey(e => e.ProductId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.SaleId);
                entity.HasIndex(e => e.ProductId);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Amount).HasDefaultValue(0.00m);
                entity.Property(e => e.TransactionReference).HasMaxLength(255);
                entity.Property(e => e.CardLastFour).HasMaxLength(4);
                entity.Property(e => e.MobileNumber).HasMaxLength(50);
                entity.HasOne(e => e.Tenant).WithMany(t => t.Payments).HasForeignKey(e => e.TenantId);
                entity.HasOne(e => e.Sale).WithMany(s => s.Payments).HasForeignKey(e => e.SaleId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.SaleId);
            });
        }

        private void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
        {
            // Apply soft delete filters to all entities that inherit from BaseEntity
            modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Branch>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Role>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<UserRole>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<RoleClaim>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<UserClaim>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Inventory>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<StockTransaction>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Prescription>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PrescriptionItem>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Sale>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SaleItem>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SaleReturn>().HasQueryFilter(e => !e.IsDeleted);
        }

        private void ApplyTenantIsolationFilters(ModelBuilder modelBuilder)
        {
            if (_tenantId.HasValue)
            {
                // Apply tenant isolation filters to all entities that inherit from TenantEntity
                modelBuilder.Entity<Branch>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Role>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<UserRole>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<RoleClaim>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<UserClaim>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Product>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Supplier>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Inventory>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<StockTransaction>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Patient>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Prescription>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<PrescriptionItem>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Sale>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<SaleItem>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<Payment>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<SaleReturn>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
            }

            // Apply branch-level filters for non-admin users
            if (_branchId.HasValue && !IsAdminUser())
            {
                modelBuilder.Entity<User>().HasQueryFilter(e => e.BranchId == null || e.BranchId == _branchId.Value);
                modelBuilder.Entity<Inventory>().HasQueryFilter(e => e.BranchId == _branchId.Value);
                modelBuilder.Entity<Sale>().HasQueryFilter(e => e.BranchId == _branchId.Value);
            }
        }

        private bool IsAdminUser()
        {
            return _userRole == "Admin" || _userRole == "SuperAdmin";
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            UpdateTenantContext();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            UpdateTenantContext();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                var currentUser = _userId?.ToString() ?? "System";

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.UpdatedBy = currentUser;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                }
            }
        }

        private void UpdateTenantContext()
        {
            if (_tenantId.HasValue)
            {
                var entries = ChangeTracker.Entries<TenantEntity>()
                    .Where(e => e.State == EntityState.Added);

                foreach (var entry in entries)
                {
                    entry.Property("TenantId").CurrentValue = _tenantId.Value;
                }
            }
        }

        public async Task<IDbContextTransaction> BeginTenantTransactionAsync(
            Guid tenantId, 
            Guid userId, 
            string userRole, 
            Guid? branchId = null,
            CancellationToken cancellationToken = default)
        {
            // Set PostgreSQL session variables for RLS policies
            await Database.ExecuteSqlRawAsync(
                "SELECT set_tenant_context(@tenantId, @userId, @userRole, @branchId)",
                new[]
                {
                    new NpgsqlParameter("tenantId", tenantId),
                    new NpgsqlParameter("userId", userId),
                    new NpgsqlParameter("userRole", userRole),
                    new NpgsqlParameter("branchId", (object?)branchId ?? DBNull.Value)
                },
                cancellationToken);

            return await Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task ClearTenantContextAsync(CancellationToken cancellationToken = default)
        {
            await Database.ExecuteSqlRawAsync("SELECT clear_tenant_context()", cancellationToken);
        }
    }
}
