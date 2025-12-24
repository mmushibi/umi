using Microsoft.Extensions.DependencyInjection;
using UmiHealth.Application.Services;

namespace UmiHealth.Application
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register application services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IOperationsService, OperationsService>();
            services.AddScoped<IDataSyncService, OperationsDataSyncService>();
            
            return services;
        }
    }
}
