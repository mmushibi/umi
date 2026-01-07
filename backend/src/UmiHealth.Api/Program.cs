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
using UmiHealth.Api.Hubs;
using UmiHealth.Core.Interfaces;
using UmiHealth.Application.Services;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using UmiHealth.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add environment-specific configuration files
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Security.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()  // Read from environment variables (highest priority)
    .Build();

// Validate critical configuration on startup
var jwtSecret = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET is not configured or too short (min 32 chars). " +
        "Set the JWT_SECRET environment variable before starting the application.");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "DATABASE_CONNECTION is not configured. " +
        "Set the DefaultConnection connection string or DATABASE_CONNECTION environment variable.");
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add SignalR with authentication
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "UmiHealth API", Version = "v1" });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Infrastructure and Application layers
builder.Services.AddInfrastructure(builder.Configuration);

// Register application services
builder.Services.AddScoped<IPatientService, PatientService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Configure SignalR hubs (must be before UseAuthorization)
app.MapHub<PharmacyHub>("/pharmacyHub");
app.MapHub<InventoryHub>("/inventoryHub");
app.MapHub<SalesHub>("/salesHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<TestHub>("/testHub");

app.UseAuthorization();

app.MapControllers();

app.Run();
