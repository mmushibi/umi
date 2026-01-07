using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
// using UmiHealth.Application.Services; // Temporarily commented out due to project reference issue
using UmiHealth.Core.Interfaces;
using UmiHealth.Core.Entities;
using UmiHealth.Infrastructure.Cache;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Infrastructure.Repositories;
using UmiHealth.Infrastructure.Storage;
using UmiHealth.Persistence;

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
        services.AddScoped<ICacheService, RedisCacheServiceAdapter>();

        // Repositories
        services.AddScoped(typeof(UmiHealth.Core.Interfaces.IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(UmiHealth.Core.Interfaces.ITenantRepository<>), typeof(TenantRepository<>));

        // Specific repositories
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Domain.Entities.Tenant>, TenantRepository>();
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Core.Entities.Branch>, BranchRepository>();
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Core.Entities.User>, UserRepository>();
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Core.Entities.Product>, ProductRepository>();
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Core.Entities.Inventory>, InventoryRepository>();
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Core.Entities.Patient>, PatientRepository>();
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Core.Entities.Sale>, SaleRepository>();
        services.AddScoped<UmiHealth.Core.Interfaces.ITenantRepository<UmiHealth.Core.Entities.Prescription>, PrescriptionRepository>();

        // Token Blacklist Service - registered in Identity layer
        // services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

        // File Storage
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Application Services - Temporarily commented out due to project reference issue
        // services.AddScoped<ISubscriptionService, SubscriptionService>();
        // services.AddScoped<INotificationService, NotificationService>();
        // services.AddScoped<IAdditionalUserService, AdditionalUserService>();
        // services.AddScoped<IPaymentVerificationService, PaymentVerificationService>();

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

    public override async Task<IReadOnlyList<Branch>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet
            .Where(b => b.TenantId == tenantId && b.Id == branchId)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }
}

public class UserRepository : TenantRepository<User>
{
    public UserRepository(AppDbContext context) : base(context) { }

    public override async Task<IReadOnlyList<User>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet
            .Where(u => u.TenantId == tenantId && u.BranchId == branchId)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }
}

public class ProductRepository : TenantRepository<Product>
{
    public ProductRepository(AppDbContext context) : base(context) { }
}

public class InventoryRepository : TenantRepository<Inventory>
{
    public InventoryRepository(AppDbContext context) : base(context) { }

    public override async Task<IReadOnlyList<Inventory>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet
            .Where(i => i.TenantId == tenantId && i.BranchId == branchId)
            .Include(i => i.Product)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }
}

public class PatientRepository : TenantRepository<Patient>
{
    public PatientRepository(AppDbContext context) : base(context) { }
}

public class SaleRepository : TenantRepository<Sale>
{
    public SaleRepository(AppDbContext context) : base(context) { }

    public override async Task<IReadOnlyList<Sale>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet
            .Where(s => s.TenantId == tenantId && s.BranchId == branchId)
            .Include(s => s.Patient)
            .Include(s => s.Items)
            .ThenInclude(si => si.Product)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }
}

public class PrescriptionRepository : TenantRepository<Prescription>
{
    public PrescriptionRepository(AppDbContext context) : base(context) { }

    public override async Task<IReadOnlyList<Prescription>> GetByTenantAndBranchAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .ThenInclude(pi => pi.Product)
            .ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }
}
