using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Infrastructure.Cache;
using UmiHealth.Infrastructure.Storage;
using UmiHealth.Infrastructure.Repositories;
using StackExchange.Redis;

namespace UmiHealth.Infrastructure.Configuration
{
    public static class DataLayerConfiguration
    {
        public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // Add PostgreSQL contexts
            services.AddDbContext<SharedDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("UmiHealth.Infrastructure")));

            // Add tenant database factory
            services.AddScoped<TenantDbContextFactory>();

            // Add Redis caching
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                options.InstanceName = "UmiHealth:";
            });

            services.AddSingleton<IRedisCacheService, RedisCacheService>();

            // Add file storage
            services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

            // Add repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(ITenantRepository<>), typeof(TenantRepository<>));

            return services;
        }

        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<SharedDbContext>("shared_database")
                .AddRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379", "redis");

            return services;
        }
    }
}
