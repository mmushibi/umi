using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UmiHealth.Api.Middleware;
using UmiHealth.Infrastructure;
using UmiHealth.Application;
using UmiHealth.Api.Services;
using UmiHealth.Api.Configuration;
using UmiHealth.Application.Configuration;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Version"),
        new QueryStringApiVersionReader("version"));
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Enhanced Swagger Configuration
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Umi Health API",
        Version = "v1",
        Description = "Comprehensive pharmacy management system API with multi-tenant architecture",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Umi Health Support",
            Email = "support@umihealth.com",
            Url = new Uri("https://umihealth.com/support")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add JWT Authentication
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML Comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Database configuration
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Add Hangfire services
builder.Services.AddHangfireServices(builder.Configuration);

// API Gateway Service
builder.Services.AddSingleton<IApiGatewayService, ApiGatewayService>();

// Multi-tenancy Services
builder.Services.AddScoped<IBranchInventoryService, BranchInventoryService>();
builder.Services.AddScoped<IStockTransferService, StockTransferService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<IBranchPermissionService, BranchPermissionService>();
builder.Services.AddScoped<IBranchReportingService, BranchReportingService>();

// Analytics and Reporting Services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();
builder.Services.AddScoped<IDataExportImportService, DataExportImportService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();

// Payment Processing Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();

// Receipt Generation Services
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IPdfGenerator, PdfGenerator>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// RSA Key Service for JWT
builder.Services.AddSingleton<IRsaKeyService, RsaKeyService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

// JWT Authentication with RSA
builder.Services.AddUmiAuthentication(builder.Configuration);

// Enhanced Rate limiting with multiple policies
builder.Services.AddRateLimiter(options =>
{
    // Default policy for general API calls
    options.AddPolicy("Default", context =>
        Microsoft.AspNetCore.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new Microsoft.AspNetCore.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 10
            }));

    // Strict policy for authentication endpoints
    options.AddPolicy("Auth", context =>
        Microsoft.AspNetCore.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new Microsoft.AspNetCore.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2
            }));

    // Permissive policy for read operations
    options.AddPolicy("Read", context =>
        Microsoft.AspNetCore.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new Microsoft.AspNetCore.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 10
            }));

    // Strict policy for write operations
    options.AddPolicy("Write", context =>
        Microsoft.AspNetCore.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new Microsoft.AspNetCore.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 5
            }));

    // Premium user policy (higher limits)
    options.AddPolicy("Premium", context =>
        Microsoft.AspNetCore.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User?.FindFirst("user_id")?.Value ?? context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new Microsoft.AspNetCore.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 500,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 20
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Umi Health API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Umi Health API Documentation";
        options.DefaultModelsExpandDepth(-1); // Hide models by default
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.EnableFilter();
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.ShowExtensions();
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Configure Hangfire dashboard
app.UseHangfireDashboard(builder.Configuration);

// Custom middleware pipeline - Order matters!
// Exception handling should be first to catch all exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiGatewayMiddleware>();
app.UseMiddleware<TenantContextMiddleware>();
app.UseMiddleware<AuthenticationMiddleware>();
app.UseMiddleware<AuthorizationMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

// Apply rate limiting
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks
app.MapHealthChecks("/health");

// Initialize background jobs
using (var scope = app.Services.CreateScope())
{
    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
    backgroundJobService.ScheduleRecurringJobs();
}

app.Run();
