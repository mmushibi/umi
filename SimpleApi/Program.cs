var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseHttpsRedirection();

// Basic health check
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Basic auth endpoints for testing
app.MapPost("/api/v1/auth/login", async (HttpContext context) => {
    try {
        var loginData = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();
        if (loginData == null || !loginData.ContainsKey("username") || !loginData.ContainsKey("password")) {
            return Results.BadRequest(new { success = false, message = "Invalid login data" });
        }

        // Mock authentication for testing
        if (loginData["username"] == "admin" && loginData["password"] == "admin") {
            return Results.Ok(new {
                success = true,
                message = "Login successful!",
                data = new {
                    id = "1",
                    username = "admin",
                    email = "admin@umihealth.com",
                    role = "admin",
                    tenantId = "1"
                },
                accessToken = "mock-jwt-token",
                refreshToken = "mock-refresh-token",
                redirectUrl = "/portals/admin/home.html"
            });
        }

        return Results.BadRequest(new { success = false, message = "Invalid username or password" });
    }
    catch (Exception ex) {
        return Results.BadRequest(new { success = false, message = "Login failed: " + ex.Message });
    }
});

app.MapPost("/api/v1/auth/register", async (HttpContext context) => {
    try {
        return Results.Ok(new {
            success = true,
            message = "Registration successful! (Mock)",
            redirectUrl = "/portals/admin/home.html"
        });
    }
    catch (Exception ex) {
        return Results.BadRequest(new { success = false, message = "Registration failed: " + ex.Message });
    }
});

// Mock data endpoints
app.MapGet("/api/v1/admin/dashboard/stats", () => new {
    success = true,
    data = new {
        totalPatients = 150,
        totalSales = 25000,
        lowStockItems = 5,
        pendingPrescriptions = 12
    }
});

app.MapGet("/api/v1/admin/patients", () => new {
    success = true,
    data = new[] {
        new { id = "1", firstName = "John", lastName = "Doe", email = "john@example.com", phone = "123-456-7890" },
        new { id = "2", firstName = "Jane", lastName = "Smith", email = "jane@example.com", phone = "098-765-4321" }
    }
});

app.MapGet("/api/v1/admin/inventory", () => new {
    success = true,
    data = new[] {
        new { id = "1", productName = "Paracetamol", currentStock = 100, minStockLevel = 20, unitPrice = 5.99 },
        new { id = "2", productName = "Amoxicillin", currentStock = 15, minStockLevel = 25, unitPrice = 12.99 }
    }
});

app.Run();
