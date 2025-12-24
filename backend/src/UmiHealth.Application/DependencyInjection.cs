using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using UmiHealth.Application.Behaviors;
using UmiHealth.Core.Interfaces;

namespace UmiHealth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Application Services
        services.AddScoped<ITenantService, Services.TenantService>();
        services.AddScoped<IAuthService, Services.AuthService>();
        services.AddScoped<IPharmacyService, Services.PharmacyService>();
        services.AddScoped<IPosService, Services.PosService>();
        services.AddScoped<IPatientService, Services.PatientService>();
        services.AddScoped<IPrescriptionService, Services.PrescriptionService>();
        services.AddScoped<IReportService, Services.ReportService>();

        return services;
    }
}
