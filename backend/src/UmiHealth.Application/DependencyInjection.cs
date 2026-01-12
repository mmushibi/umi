using Microsoft.Extensions.DependencyInjection;
using UmiHealth.Application.Services;

namespace UmiHealth.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register application services
            services.AddScoped<ISimpleRegistrationService, SimpleRegistrationService>();
            services.AddScoped<IOnboardingService, OnboardingService>();
            services.AddScoped<IDatabaseSecurityAuditService, DatabaseSecurityAuditService>();
            services.AddScoped<DataSyncService>();
            services.AddScoped<IPaymentNotificationService, PaymentNotificationService>();
            
            return services;
        }
    }
}
