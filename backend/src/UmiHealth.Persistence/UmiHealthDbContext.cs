using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using CoreEntities = UmiHealth.Core.Entities;
using DomainEntities = UmiHealth.Domain.Entities;

namespace UmiHealth.Persistence
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
        public DbSet<DomainEntities.Tenant> Tenants { get; set; }
        public DbSet<DomainEntities.Branch> Branches { get; set; }

        // Users and Authentication
        public DbSet<CoreEntities.User> Users { get; set; }
        public DbSet<CoreEntities.Role> Roles { get; set; }
        public DbSet<CoreEntities.UserRole> UserRoles { get; set; }
        public DbSet<CoreEntities.RoleClaim> RoleClaims { get; set; }
        public DbSet<CoreEntities.UserClaim> UserClaims { get; set; }
        public DbSet<CoreEntities.RefreshToken> RefreshTokens { get; set; }
        public DbSet<CoreEntities.BlacklistedToken> BlacklistedTokens { get; set; }
        public DbSet<DomainEntities.UserInvitation> UserInvitations { get; set; }

        // Subscriptions and Billing
        public DbSet<DomainEntities.Subscription> Subscriptions { get; set; }
        public DbSet<DomainEntities.SubscriptionTransaction> SubscriptionTransactions { get; set; }

        // Notifications
        public DbSet<DomainEntities.Notification> Notifications { get; set; }

        // Additional Users
        public DbSet<DomainEntities.UserAdditionalUser> UserAdditionalUsers { get; set; }

        // Products and Inventory
        public DbSet<DomainEntities.Product> Products { get; set; }
        public DbSet<DomainEntities.Supplier> Suppliers { get; set; }
        public DbSet<DomainEntities.Inventory> Inventories { get; set; }
        public DbSet<DomainEntities.StockTransaction> StockTransactions { get; set; }
        public DbSet<DomainEntities.PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<DomainEntities.PurchaseOrderItem> PurchaseOrderItems { get; set; }

        // Patients and Prescriptions
        public DbSet<DomainEntities.Patient> Patients { get; set; }
        public DbSet<DomainEntities.Prescription> Prescriptions { get; set; }
        public DbSet<DomainEntities.PrescriptionItem> PrescriptionItems { get; set; }

        // Sales and Payments
        public DbSet<DomainEntities.Sale> Sales { get; set; }
        public DbSet<DomainEntities.SaleItem> SaleItems { get; set; }
        public DbSet<CoreEntities.Payment> Payments { get; set; }
        public DbSet<DomainEntities.SaleReturn> SaleReturns { get; set; }

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
            ConfigureUserInvitation(modelBuilder);
            ConfigureSubscription(modelBuilder);
            ConfigureNotification(modelBuilder);
            ConfigureAdditionalUser(modelBuilder);
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
            modelBuilder.Entity<CoreEntities.Tenant>(entity =>
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
            modelBuilder.Entity<DomainEntities.Branch>(entity =>
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
            modelBuilder.Entity<CoreEntities.User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
                entity.HasIndex(e => new { e.TenantId, e.UserName }).IsUnique();
            });

            modelBuilder.Entity<CoreEntities.Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => new { e.TenantId, e.NormalizedName }).IsUnique();
            });

            modelBuilder.Entity<CoreEntities.UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RoleId);
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            });

            modelBuilder.Entity<DomainEntities.UserInvitation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Token);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsAccepted);
            });

            modelBuilder.Entity<DomainEntities.Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.PlanType).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.EndDate);
                entity.HasIndex(e => e.IsActive);
            });

            modelBuilder.Entity<DomainEntities.Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.CreatedAt);
            });

            modelBuilder.Entity<DomainEntities.UserAdditionalUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.HasOne(e => e.MainUser).WithMany().HasForeignKey(e => e.MainUserId);
                entity.HasOne(e => e.AdditionalUser).WithMany().HasForeignKey(e => e.AdditionalUserId);
                entity.HasIndex(e => e.MainUserId);
                entity.HasIndex(e => e.AdditionalUserId);
                entity.HasIndex(e => new { e.MainUserId, e.AdditionalUserId }).IsUnique();
            });
        }

        private void ConfigureUserInvitation(ModelBuilder modelBuilder)
        {
            // Already configured above
        }

        private void ConfigureSubscription(ModelBuilder modelBuilder)
        {
            // Already configured above
        }

        private void ConfigureNotification(ModelBuilder modelBuilder)
        {
            // Already configured above
        }

        private void ConfigureAdditionalUser(ModelBuilder modelBuilder)
        {
            // Already configured above
        }

        private void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DomainEntities.Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Barcode).HasMaxLength(100);
                entity.Property(e => e.UnitPrice).HasDefaultValue(0.00m);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Barcode);
                entity.HasIndex(e => e.IsActive);
            });

            modelBuilder.Entity<DomainEntities.Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.IsActive);
            });
        }

        private void ConfigureInventory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DomainEntities.Inventory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.QuantityOnHand).HasDefaultValue(0);
                entity.Property(e => e.ReorderLevel).HasDefaultValue(0);
                entity.Property(e => e.UnitCost).HasDefaultValue(0.00m);
                entity.Property(e => e.UnitPrice).HasDefaultValue(0.00m);
                entity.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId);
                entity.HasOne(e => e.Product).WithMany(p => p.Inventories).HasForeignKey(e => e.ProductId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.BatchNumber);
                entity.HasIndex(e => new { e.TenantId, e.BranchId, e.ProductId, e.BatchNumber }).IsUnique();
            });

            modelBuilder.Entity<DomainEntities.StockTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Quantity);
                entity.Property(e => e.UnitCost).HasDefaultValue(0.00m);
                entity.Property(e => e.TotalCost).HasDefaultValue(0.00m);
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
            modelBuilder.Entity<DomainEntities.Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.NationalId).HasMaxLength(50);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Phone);
                entity.HasIndex(e => e.IsActive);
            });
        }

        private void ConfigurePrescription(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DomainEntities.Prescription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.PrescriptionNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Diagnosis);
                entity.Property(e => e.DoctorNotes);
                entity.HasOne(e => e.Patient).WithMany(p => p.Prescriptions).HasForeignKey(e => e.PatientId);
                entity.HasOne(e => e.Doctor).WithMany().HasForeignKey(e => e.DoctorId);
                entity.HasOne(e => e.DispensedByUser).WithMany().HasForeignKey(e => e.DispensedBy);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.DoctorId);
                entity.HasIndex(e => e.PrescriptionNumber);
                entity.HasIndex(e => new { e.TenantId, e.PrescriptionNumber }).IsUnique();
            });

            modelBuilder.Entity<DomainEntities.PrescriptionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Dosage).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Frequency).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Route).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DurationUnit).HasDefaultValue("Days");
                entity.Property(e => e.Quantity);
                entity.Property(e => e.Instructions);
                entity.HasOne(e => e.Prescription).WithMany(p => p.Items).HasForeignKey(e => e.PrescriptionId);
                entity.HasOne(e => e.Product).WithMany(p => p.PrescriptionItems).HasForeignKey(e => e.ProductId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.PrescriptionId);
                entity.HasIndex(e => e.ProductId);
            });
        }

        private void ConfigureSale(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DomainEntities.Sale>(entity =>
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
                entity.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId);
                entity.HasOne(e => e.Patient).WithMany(p => p.Sales).HasForeignKey(e => e.PatientId);
                entity.HasOne(e => e.Cashier).WithMany().HasForeignKey(e => e.CashierId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.BranchId);
                entity.HasIndex(e => e.PatientId);
                entity.HasIndex(e => e.SaleDate);
                entity.HasIndex(e => new { e.TenantId, e.SaleNumber }).IsUnique();
            });

            modelBuilder.Entity<DomainEntities.SaleItem>(entity =>
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
                entity.HasOne(e => e.Sale).WithMany(s => s.Items).HasForeignKey(e => e.SaleId);
                entity.HasOne(e => e.Product).WithMany(p => p.SaleItems).HasForeignKey(e => e.ProductId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.SaleId);
                entity.HasIndex(e => e.ProductId);
            });

            modelBuilder.Entity<CoreEntities.Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
                entity.Property(e => e.Amount).HasDefaultValue(0.00m);
                entity.Property(e => e.TransactionReference).HasMaxLength(255);
                entity.Property(e => e.CardLastFour).HasMaxLength(4);
                entity.Property(e => e.MobileNumber).HasMaxLength(50);
                entity.HasOne(e => e.Sale).WithMany(s => s.Payments).HasForeignKey(e => e.SaleId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.SaleId);
            });
        }

        private void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
        {
            // Apply soft delete filters to all entities that inherit from CoreEntities.BaseEntity
            modelBuilder.Entity<CoreEntities.Role>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CoreEntities.UserRole>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CoreEntities.RoleClaim>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CoreEntities.UserClaim>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CoreEntities.RefreshToken>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CoreEntities.Payment>().HasQueryFilter(e => !e.IsDeleted);
        }

        private void ApplyTenantIsolationFilters(ModelBuilder modelBuilder)
        {
            if (_tenantId.HasValue)
            {
                // Apply tenant isolation filters to all entities that inherit from CoreEntities.TenantEntity
                modelBuilder.Entity<DomainEntities.Branch>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.User>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<CoreEntities.Role>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<CoreEntities.UserRole>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<CoreEntities.RoleClaim>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<CoreEntities.UserClaim>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<CoreEntities.RefreshToken>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.Product>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.Supplier>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.Inventory>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.StockTransaction>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.PurchaseOrder>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.PurchaseOrderItem>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.Patient>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.Prescription>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.PrescriptionItem>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.Sale>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.SaleItem>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<CoreEntities.Payment>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
                modelBuilder.Entity<DomainEntities.SaleReturn>().HasQueryFilter(e => e.TenantId == _tenantId.Value);
            }

            // Apply branch-level filters for non-admin users
            if (_branchId.HasValue && !IsAdminUser())
            {
                modelBuilder.Entity<DomainEntities.User>().HasQueryFilter(e => e.BranchId == null || e.BranchId == _branchId.Value);
                modelBuilder.Entity<DomainEntities.Inventory>().HasQueryFilter(e => e.BranchId == _branchId.Value);
                modelBuilder.Entity<DomainEntities.Sale>().HasQueryFilter(e => e.BranchId == _branchId.Value);
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
            var entries = ChangeTracker.Entries<CoreEntities.BaseEntity>();

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
                var entries = ChangeTracker.Entries<CoreEntities.TenantEntity>()
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
