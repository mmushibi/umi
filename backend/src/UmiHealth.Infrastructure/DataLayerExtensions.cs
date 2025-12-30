using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using UmiHealth.Infrastructure.Configuration;
using UmiHealth.Infrastructure.MultiTenant;

namespace UmiHealth.Infrastructure
{
    public static class DataLayerExtensions
    {
        /// <summary>
        /// Adds the complete data layer infrastructure to the service collection
        /// </summary>
        public static IServiceCollection AddUmiHealthDataLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDataLayer(configuration);
            services.AddHealthChecks(configuration);
            
            return services;
        }

        /// <summary>
        /// Adds tenant middleware to the application pipeline
        /// </summary>
        public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
}
