using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UmiHealth.Core.Entities;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSet properties for all entities
    public DbSet<UmiHealth.Domain.Entities.Tenant> Tenants { get; set; }
    public DbSet<UmiHealth.Core.Entities.Branch> Branches { get; set; }
    public DbSet<UmiHealth.Core.Entities.User> Users { get; set; }
    public DbSet<UmiHealth.Core.Entities.Role> Roles { get; set; }
    public DbSet<UmiHealth.Core.Entities.UserRole> UserRoles { get; set; }
    public DbSet<UmiHealth.Core.Entities.RoleClaim> RoleClaims { get; set; }
    public DbSet<UmiHealth.Core.Entities.UserClaim> UserClaims { get; set; }
    public DbSet<UmiHealth.Core.Entities.RefreshToken> RefreshTokens { get; set; }
    public DbSet<UmiHealth.Core.Entities.Product> Products { get; set; }
    public DbSet<UmiHealth.Core.Entities.Inventory> Inventories { get; set; }
    public DbSet<UmiHealth.Core.Entities.StockTransaction> StockTransactions { get; set; }
    public DbSet<UmiHealth.Core.Entities.Supplier> Suppliers { get; set; }
    public DbSet<UmiHealth.Core.Entities.PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<UmiHealth.Core.Entities.PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<UmiHealth.Core.Entities.Sale> Sales { get; set; }
    public DbSet<UmiHealth.Core.Entities.SaleItem> SaleItems { get; set; }
    public DbSet<UmiHealth.Core.Entities.Payment> Payments { get; set; }
    public DbSet<UmiHealth.Core.Entities.SaleReturn> SaleReturns { get; set; }
    public DbSet<UmiHealth.Core.Entities.Patient> Patients { get; set; }
    public DbSet<UmiHealth.Core.Entities.Prescription> Prescriptions { get; set; }
    public DbSet<UmiHealth.Core.Entities.PrescriptionItem> PrescriptionItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Tenant entity
        modelBuilder.Entity<UmiHealth.Domain.Entities.Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContactEmail).HasMaxLength(200);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.HasIndex(e => e.Subdomain).IsUnique();
        });

        // Configure Branch entity
        modelBuilder.Entity<UmiHealth.Core.Entities.Branch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Tenant).WithMany(t => t.Branches).HasForeignKey(e => e.TenantId);
        });

        // Configure User entity
        modelBuilder.Entity<UmiHealth.Core.Entities.User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Tenant).WithMany(t => t.Users).HasForeignKey(e => e.TenantId);
            entity.HasOne(e => e.Branch).WithMany(b => b.Users).HasForeignKey(e => e.BranchId);
        });

        // Configure Role entity
        modelBuilder.Entity<UmiHealth.Core.Entities.Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.NormalizedName).IsRequired().HasMaxLength(100);
            // Skip the Roles navigation for now due to type mismatch between Core and Domain Tenant
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
        });

        // Configure UserRole entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
        });

        // Configure Product entity
        modelBuilder.Entity<UmiHealth.Core.Entities.Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GenericName).HasMaxLength(200);
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.NdcCode).HasMaxLength(50);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
        });

        // Configure Inventory entity
        modelBuilder.Entity<UmiHealth.Core.Entities.Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Branch).WithMany(b => b.Inventories).HasForeignKey(e => e.BranchId);
            entity.HasOne(e => e.Product).WithMany(p => p.Inventories).HasForeignKey(e => e.ProductId);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
        });

        // Configure Patient entity
        modelBuilder.Entity<UmiHealth.Core.Entities.Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
        });

        // Configure Sale entity
        modelBuilder.Entity<UmiHealth.Core.Entities.Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Branch).WithMany(b => b.Sales).HasForeignKey(e => e.BranchId);
            entity.HasOne(e => e.Patient).WithMany(p => p.Sales).HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.Cashier).WithMany().HasForeignKey(e => e.CashierId);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
        });

        // Configure Prescription entity
        modelBuilder.Entity<UmiHealth.Core.Entities.Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrescriptionNumber).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Patient).WithMany(p => p.Prescriptions).HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.Doctor).WithMany().HasForeignKey(e => e.DoctorId);
            entity.HasOne(e => e.DispensedByUser).WithMany().HasForeignKey(e => e.DispensedBy);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
        });

        // Apply soft delete filter globally
        ApplySoftDeleteFilter(modelBuilder);
    }

    private void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    typeof(SoftDeleteFilter).GetMethod(nameof(SoftDeleteFilter.IsDeleted))!
                        .MakeGenericMethod(entityType.ClrType).Invoke(null, null) as LambdaExpression);
            }
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            var user = "system"; // In real app, get from current user context

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = user;
                    entry.Entity.UpdatedBy = user;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = user;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.DeletedBy = user;
                    break;
            }
        }
    }
}

public static class SoftDeleteFilter
{
    public static LambdaExpression IsDeleted<TEntity>() where TEntity : BaseEntity
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }
}
