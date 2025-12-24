using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Cache;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Infrastructure.Repositories;
using UmiHealth.Infrastructure.Storage;

namespace UmiHealth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Redis
        var redisConnectionString = configuration.GetConnectionString("Redis");
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString!));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "UmiHealth";
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(ITenantRepository<>), typeof(TenantRepository<>));

        // Specific repositories
        services.AddScoped<ITenantRepository<Tenant>, TenantRepository<Tenant>>();
        services.AddScoped<ITenantRepository<Branch>, BranchRepository>();
        services.AddScoped<ITenantRepository<User>, UserRepository>();
        services.AddScoped<ITenantRepository<Product>, ProductRepository>();
        services.AddScoped<ITenantRepository<Inventory>, InventoryRepository>();
        services.AddScoped<ITenantRepository<Patient>, PatientRepository>();
        services.AddScoped<ITenantRepository<Sale>, SaleRepository>();
        services.AddScoped<ITenantRepository<Prescription>, PrescriptionRepository>();

        // File Storage
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }
}

// Specific repository implementations
public class BranchRepository : TenantRepository<Branch>
{
    public BranchRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<Branch>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.TenantId == tenantId && b.Id == branchId)
            .ToListAsync(cancellationToken);
    }
}

public class UserRepository : TenantRepository<User>
{
    public UserRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<User>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.TenantId == tenantId && u.BranchId == branchId)
            .ToListAsync(cancellationToken);
    }
}

public class ProductRepository : TenantRepository<Product>
{
    public ProductRepository(AppDbContext context) : base(context) { }
}

public class InventoryRepository : TenantRepository<Inventory>
{
    public InventoryRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<Inventory>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(i => i.TenantId == tenantId && i.BranchId == branchId)
            .Include(i => i.Product)
            .ToListAsync(cancellationToken);
    }
}

public class PatientRepository : TenantRepository<Patient>
{
    public PatientRepository(AppDbContext context) : base(context) { }
}

public class SaleRepository : TenantRepository<Sale>
{
    public SaleRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<Sale>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.TenantId == tenantId && s.BranchId == branchId)
            .Include(s => s.Patient)
            .Include(s => s.Items)
            .ThenInclude(si => si.Product)
            .ToListAsync(cancellationToken);
    }
}

public class PrescriptionRepository : TenantRepository<Prescription>
{
    public PrescriptionRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<Prescription>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .ThenInclude(pi => pi.Product)
            .ToListAsync(cancellationToken);
    }
}
