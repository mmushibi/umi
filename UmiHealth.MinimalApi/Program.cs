var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddCors();

// Add in-memory database for demo
builder.Services.AddSingleton(new Dictionary<string, object>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Get in-memory database
var usersDb = app.Services.GetRequiredService<Dictionary<string, object>>();
var tenantsDb = new Dictionary<string, object>();

// Basic registration endpoint with database saving
app.MapPost("/api/v1/auth/register", async (HttpRequest request, Dictionary<string, object> usersDb, Dictionary<string, object> tenantsDb) =>
{
    try
    {
        var formData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        
        if (formData == null || !formData.ContainsKey("email") || !formData.ContainsKey("pharmacyName"))
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Missing required fields: email and pharmacyName" 
            });
        }
        
        var userId = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid().ToString();
        
        // Create user with admin role for signup
        var user = new {
            id = userId,
            username = formData.ContainsKey("username") ? formData["username"] : (formData["email"]?.Split('@')[0] ?? "user"),
            email = formData["email"],
            password = formData["password"], // In production, hash this
            confirmPassword = formData.ContainsKey("confirmPassword") ? formData["confirmPassword"] : formData["password"],
            firstName = formData["adminFullName"]?.Split(' ')[0] ?? "Admin",
            lastName = formData["adminFullName"]?.Split(' ').Length > 1 ? string.Join(" ", formData["adminFullName"]?.Split(' ').Skip(1)) : "User",
            phoneNumber = formData["phoneNumber"],
            role = "admin", // Users who sign up become tenant admins
            status = "active",
            createdAt = DateTime.UtcNow,
            tenantId = tenantId
        };
        usersDb[userId] = user;
        
        // Save tenant to database
        var tenant = new {
            id = tenantId,
            name = formData["pharmacyName"],
            email = formData["email"],
            status = "active",
            subscriptionPlan = "Care",
            createdAt = DateTime.UtcNow
        };
        tenantsDb[tenantId] = tenant;
        
        return Results.Ok(new { 
            success = true, 
            message = "Registration successful! Account created and saved to database.",
            data = user,
            redirectUrl = "/portals/admin/home.html"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = "Registration failed: " + ex.Message 
        });
    }
});

// Login endpoint with role-based authentication
app.MapPost("/api/v1/auth/login", async (HttpRequest request, Dictionary<string, object> usersDb, Dictionary<string, object> tenantsDb) =>
{
    try
    {
        var loginData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        
        if (loginData == null || 
            !loginData.ContainsKey("username") || 
            !loginData.ContainsKey("password"))
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Username and password are required" 
            });
        }

        var username = loginData["username"];
        var password = loginData["password"];

        // Find user in database
        var userEntry = usersDb.FirstOrDefault(u => 
            ((dynamic)u.Value).username?.ToString() == username);
        
        if (userEntry.Value == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid username or password" 
            });
        }

        var user = (dynamic)userEntry.Value;
        
        // Verify password (simple check for demo - in production, use proper hashing)
        if (user.password?.ToString() != password)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid username or password" 
            });
        }

        // Get tenant information
        var tenantId = user.tenantId?.ToString();
        var tenant = tenantsDb.ContainsKey(tenantId) ? (dynamic)tenantsDb[tenantId] : null;

        // Determine redirect URL based on user role
        string redirectUrl = "/portals/admin/home.html"; // Default for tenant admin
        if (user.role?.ToString() == "cashier")
        {
            redirectUrl = "/portals/cashier/home.html";
        }
        else if (user.role?.ToString() == "pharmacist")
        {
            redirectUrl = "/portals/pharmacist/home.html";
        }
        else if (user.role?.ToString() == "superadmin")
        {
            redirectUrl = "/portals/admin/home.html";
        }

        return Results.Ok(new { 
            success = true, 
            message = "Login successful!",
            data = new {
                id = user.id,
                username = user.username,
                email = user.email,
                role = user.role,
                tenantId = tenantId,
                tenant = tenant,
                accessToken = "mock-jwt-token-" + Guid.NewGuid().ToString("N")[..8],
                refreshToken = "mock-refresh-token-" + Guid.NewGuid().ToString("N")[..8]
            },
            redirectUrl = redirectUrl
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = "Login failed: " + ex.Message 
        });
    }
});

// Add user endpoint (for tenant admins)
app.MapPost("/api/v1/users", async (HttpRequest request, Dictionary<string, object> usersDb, Dictionary<string, object> tenantsDb) =>
{
    try
    {
        var userData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        
        if (userData == null || 
            !userData.ContainsKey("username") || 
            !userData.ContainsKey("email") ||
            !userData.ContainsKey("password") ||
            !userData.ContainsKey("role"))
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Username, email, password, and role are required" 
            });
        }

        // Validate tenant exists
        if (!userData.ContainsKey("tenantId") || 
            !tenantsDb.ContainsKey(userData["tenantId"]))
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid tenant ID" 
            });
        }

        var userId = Guid.NewGuid().ToString();
        var user = new {
            id = userId,
            username = userData["username"],
            email = userData["email"],
            password = userData["password"], // In production, hash this
            role = userData["role"], // admin, cashier, pharmacist
            firstName = userData.ContainsKey("firstName") ? userData["firstName"] : "",
            lastName = userData.ContainsKey("lastName") ? userData["lastName"] : "",
            phoneNumber = userData.ContainsKey("phoneNumber") ? userData["phoneNumber"] : "",
            status = "active",
            createdAt = DateTime.UtcNow,
            tenantId = userData["tenantId"]
        };
        
        usersDb[userId] = user;
        
        return Results.Ok(new { 
            success = true, 
            message = "User created successfully!",
            data = user
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = "Failed to create user: " + ex.Message 
        });
    }
});

// Admin users endpoint - return all registered users
app.MapGet("/admin/users", (Dictionary<string, object> usersDb) =>
{
    var users = usersDb.Values.ToList();
    return Results.Ok(new {
        success = true,
        data = users,
        total = users.Count
    });
});

// Admin tenants endpoint - return all tenants
app.MapGet("/admin/tenants", (Dictionary<string, object> tenantsDb) =>
{
    var tenants = tenantsDb.Values.ToList();
    return Results.Ok(new {
        success = true,
        data = tenants,
        total = tenants.Count
    });
});

// Pharmacy name check endpoint
app.MapGet("/api/auth/check-pharmacy-name/{pharmacyName}", (string pharmacyName) =>
{
    // Check if pharmacy name already exists
    var existingTenant = tenantsDb.Values.FirstOrDefault(t => 
        t.GetType().GetProperty("name")?.GetValue(t)?.ToString()?.Equals(pharmacyName, StringComparison.OrdinalIgnoreCase) == true
    );
    
    return Results.Ok(new { 
        success = true, 
        available = existingTenant == null,
        message = existingTenant == null ? "Pharmacy name is available" : "Pharmacy name already exists"
    });
});

// Admin products endpoint - return sample products
app.MapGet("/admin/products", () =>
{
    var products = new[] {
        new { id = 1, name = "Paracetamol 500mg", price = 25.50m, stock = 150, category = "medication" },
        new { id = 2, name = "Amoxicillin 250mg", price = 85.00m, stock = 80, category = "medication" },
        new { id = 3, name = "Vitamin C 1000mg", price = 45.75m, stock = 200, category = "supplements" },
        new { id = 4, name = "Face Masks", price = 15.00m, stock = 500, category = "medical" },
        new { id = 5, name = "Ibuprofen 400mg", price = 35.00m, stock = 120, category = "medication" },
        new { id = 6, name = "Hand Sanitizer", price = 20.00m, stock = 300, category = "medical" },
        new { id = 7, name = "Vitamin D3", price = 55.00m, stock = 100, category = "supplements" },
        new { id = 8, name = "Blood Pressure Monitor", price = 450.00m, stock = 25, category = "medical" }
    };
    
    return Results.Ok(new {
        success = true,
        data = products,
        total = products.Length
    });
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
