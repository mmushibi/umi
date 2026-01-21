using UmiHealth.MinimalApi.Services;
using UmiHealth.MinimalApi.Middleware;
using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using UmiHealth.MinimalApi.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

// Add database context - SQLite for simplicity
builder.Services.AddDbContext<UmiHealthDbContext>(options =>
    options.UseSqlite("Data Source=umihealth.db"));

// Tier service (scaffolding)
builder.Services.AddSingleton<ITierService, TierService>();

// Audit service
builder.Services.AddSingleton<IAuditService, AuditService>();

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key not configured in appsettings.json");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

// JWT Token generation function
string GenerateJwtToken(string userId, string email, string role, string tenantId)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(jwtKey);
    
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role),
        new Claim("tenant_id", tenantId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = jwtIssuer,
        Audience = jwtAudience,
        SigningCredentials = signingCredentials
    };
    
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

string GenerateRefreshToken()
{
    return Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Tier-based rate limiting and feature gates (scaffolding)
app.UseMiddleware<TierRateLimitMiddleware>();
app.UseMiddleware<FeatureGateMiddleware>();

// Create database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UmiHealthDbContext>();
    context.Database.EnsureCreated();
}

// Basic registration endpoint with database saving
app.MapPost("/api/v1/auth/register", async (HttpRequest request, UmiHealthDbContext context) =>
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
        
        // Check if email already exists
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == formData["email"]);
        if (existingUser != null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Email already registered" 
            });
        }
        
        // Check if pharmacy name already exists
        var existingTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == formData["pharmacyName"]);
        if (existingTenant != null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Pharmacy name already exists" 
            });
        }
        
        var userId = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid().ToString();
        
        // Create tenant
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = formData["pharmacyName"],
            Email = formData["email"],
            Status = "active",
            SubscriptionPlan = "Care",
            CreatedAt = DateTime.UtcNow
        };
        
        // Create user with admin role for signup
        var user = new User
        {
            Id = userId,
            Username = formData.ContainsKey("username") ? formData["username"] : (formData["email"]?.Split('@')[0] ?? "user"),
            Email = formData["email"],
            Password = formData["password"], // In production, hash this
            FirstName = formData["adminFullName"]?.Split(' ')[0] ?? "Admin",
            LastName = formData["adminFullName"]?.Split(' ').Length > 1 ? string.Join(" ", formData["adminFullName"]?.Split(' ').Skip(1)) : "User",
            PhoneNumber = formData["phoneNumber"],
            Role = "admin", // Users who sign up become tenant admins
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
        
        // Save to database
        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
            // Generate real JWT tokens
            var accessToken = GenerateJwtToken(user.Id, user.Email, user.Role, user.TenantId);
            var refreshToken = GenerateRefreshToken();
            
            return Results.Ok(new { 
                success = true, 
                message = "Registration successful! Account created and saved to database.",
                user = new {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role,
                    tenantId = user.TenantId
                },
                tenant = new {
                    id = tenant.Id,
                    name = tenant.Name,
                    email = tenant.Email,
                    subscriptionPlan = tenant.SubscriptionPlan
                },
                accessToken = accessToken,
                refreshToken = refreshToken,
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
app.MapPost("/api/v1/auth/login", async (HttpRequest request, UmiHealthDbContext context) =>
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
        var user = await context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);
        
        if (user == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid username or password" 
            });
        }
        
        // Verify password (simple check for demo - in production, use proper hashing)
        if (user.Password != password)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid username or password" 
            });
        }

        // Determine redirect URL based on user role
        string redirectUrl = "/portals/admin/home.html"; // Default for tenant admin
        if (user.Role == "cashier")
        {
            redirectUrl = "/portals/cashier/home.html";
        }
        else if (user.Role == "pharmacist")
        {
            redirectUrl = "/portals/pharmacist/home.html";
        }
        else if (user.Role == "superadmin")
        {
            redirectUrl = "/portals/admin/home.html";
        }

        // Generate real JWT tokens
        var accessToken = GenerateJwtToken(user.Id, user.Email, user.Role, user.TenantId);
        var refreshToken = GenerateRefreshToken();

        return Results.Ok(new { 
            success = true, 
            message = "Login successful!",
            data = new {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                role = user.Role,
                tenantId = user.TenantId,
                tenant = new {
                    id = user.Tenant.Id,
                    name = user.Tenant.Name,
                    email = user.Tenant.Email,
                    subscriptionPlan = user.Tenant.SubscriptionPlan
                }
            },
            accessToken = accessToken,
            refreshToken = refreshToken,
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

// Pharmacy name check endpoint
app.MapGet("/api/auth/check-pharmacy-name/{pharmacyName}", async (string pharmacyName, UmiHealthDbContext context) =>
{
    // Check if pharmacy name already exists
    var existingTenant = await context.Tenants
        .FirstOrDefaultAsync(t => t.Name.ToLower() == pharmacyName.ToLower());
    
    return Results.Ok(new { 
        success = true, 
        available = existingTenant == null,
        message = existingTenant == null ? "Pharmacy name is available" : "Pharmacy name already exists"
    });
});

// Pharmacy name check endpoint (v1 version for consistency)
app.MapGet("/api/v1/auth/check-pharmacy-name/{pharmacyName}", async (string pharmacyName, UmiHealthDbContext context) =>
{
    // Check if pharmacy name already exists
    var existingTenant = await context.Tenants
        .FirstOrDefaultAsync(t => t.Name.ToLower() == pharmacyName.ToLower());
    
    return Results.Ok(new { 
        success = true, 
        available = existingTenant == null,
        message = existingTenant == null ? "Pharmacy name is available" : "Pharmacy name already exists"
    });
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Register all API endpoints
app.RegisterApiEndpoints();

app.Run();
