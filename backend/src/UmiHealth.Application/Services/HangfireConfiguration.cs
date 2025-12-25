using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using UmiHealth.Application.Services;

namespace UmiHealth.Application.Configuration
{
    public static class HangfireConfiguration
    {
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Hangfire services
            services.AddHangfire(config =>
            {
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                
                // Use memory storage for development, PostgreSQL for production
                if (configuration.GetValue<bool>("UseInMemoryStorage"))
                {
                    config.UseMemoryStorage();
                }
                else
                {
                    var connectionString = configuration.GetConnectionString("DefaultConnection");
                    config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
                    {
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        CountersAggregateInterval = TimeSpan.FromMinutes(5),
                        PrepareSchemaIfNecessary = true,
                        DashboardJobListLimit = 50000,
                        TransactionSynchronisationTimeout = TimeSpan.FromMinutes(5),
                        TablesPrefix = "hangfire."
                    });
                }

                // Configure options
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
            });

            // Add Hangfire server
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = new[] { "default", "critical", "reports", "notifications" };
                options.ServerTimeout = TimeSpan.FromMinutes(5);
                options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
                options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                options.ServerCheckInterval = TimeSpan.FromMinutes(1);
                options.StopTimeout = TimeSpan.FromMinutes(15);
                options.ShutdownTimeout = TimeSpan.FromMinutes(30);
            });

            // Register background job service
            services.AddScoped<IBackgroundJobService, BackgroundJobService>();

            return services;
        }

        public static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration)
        {
            // Configure Hangfire dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
                DashboardTitle = "Umi Health Background Jobs",
                StatsPollingInterval = 2000,
                DisplayStorageConnectionString = false,
                IgnoreAntiforgeryToken = true
            });

            return app;
        }
    }

    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // Check if user is authenticated and has admin role
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // Check if user has required role/permission
            var hasAdminRole = httpContext.User.IsInRole("Admin") || 
                              httpContext.User.IsInRole("System Administrator") ||
                              httpContext.User.HasClaim("permission", "view_background_jobs");

            return hasAdminRole;
        }
    }
}
