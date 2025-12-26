var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Mock auth endpoints
app.MapPost("/api/v1/auth/register", (RegisterRequest request) => {
    Console.WriteLine($"Received signup request: {System.Text.Json.JsonSerializer.Serialize(request)}");
    
    // Simulate successful registration
    return Results.Ok(new {
        success = true,
        message = "Registration successful",
        data = new {
            token = "mock-jwt-token",
            refreshToken = "mock-refresh-token",
            user = new {
                id = Guid.NewGuid(),
                email = request.Email,
                firstName = request.FirstName,
                lastName = request.LastName,
                phoneNumber = request.PhoneNumber
            }
        }
    });
});

app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run("http://localhost:5001");

record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid TenantId,
    Guid? BranchId,
    string PharmacyName,
    string PharmacyLicenseNumber,
    string Address,
    string Province,
    string Username
);
