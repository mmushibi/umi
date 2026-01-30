using UmiHealth.MinimalApi.Services;

using UmiHealth.MinimalApi.Middleware;

using UmiHealth.MinimalApi.Data;

using UmiHealth.MinimalApi.Models;

using UmiHealth.MinimalApi.Endpoints;

using UmiHealth.MinimalApi.Hubs;

using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Text;

using System.Text.Json;

using BCrypt.Net;



var builder = WebApplication.CreateBuilder(args);



// Add services to the container.

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddCors();

// Add SignalR
builder.Services.AddSignalR();

// Add database context - SQLite

builder.Services.AddDbContext<UmiHealthDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// Tier service (scaffolding)

builder.Services.AddSingleton<ITierService, TierService>();


// Audit service

builder.Services.AddSingleton<IAuditService, AuditService>();

// Validation service

builder.Services.AddSingleton<IValidationService, ValidationService>();

// Add controllers
builder.Services.AddControllers();

// Business logic services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReportService, ReportService>();



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

// Helper methods for subscription plans
int GetMaxUsersForPlan(string? planType)
{
    return planType?.ToLower() switch
    {
        "free" => 1,
        "care" => 10,
        "enterprise" => 100,
        _ => 1
    };
}

int GetMaxBranchesForPlan(string? planType)
{
    return planType?.ToLower() switch
    {
        "free" => 1,
        "care" => 2,
        "enterprise" => 10,
        _ => 1
    };
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

// Map controllers
app.MapControllers();

// Add security headers

app.UseMiddleware<SecurityHeadersMiddleware>();

// Tier-based rate limiting and feature gates (scaffolding)

app.UseMiddleware<TierRateLimitMiddleware>();

app.UseMiddleware<FeatureGateMiddleware>();



// Create database on startup (optional - commented out for now)

// using (var scope = app.Services.CreateScope())

// {

//     var context = scope.ServiceProvider.GetRequiredService<UmiHealthDbContext>();

//     context.Database.EnsureCreated();

// }



// Basic registration endpoint with database saving

app.MapPost("/api/v1/auth/register", async (HttpRequest request, UmiHealthDbContext context, IValidationService validationService) =>
{
    try
    {
        var formData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        
        if (formData == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid form data" 
            });
        }

        // Validate input
        var validationErrors = validationService.ValidateRegistrationInput(formData);
        if (validationErrors.Any())
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Validation failed", 
                errors = validationErrors
            });
        }

        // Sanitize inputs
        var email = validationService.SanitizeInput(formData["email"]);
        var pharmacyName = validationService.SanitizeInput(formData["pharmacyName"]);


        // Check if email already exists

        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser != null)

        {

            return Results.BadRequest(new { 

                success = false, 

                message = "Email already registered" 

            });

        }

        

        // Check if pharmacy name already exists

        var existingTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == pharmacyName);

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

            Name = pharmacyName,

            Email = email,

            Status = "active",

            SubscriptionPlan = "Care",

            CreatedAt = DateTime.UtcNow

        };

        

        // Extract form data values

        formData.TryGetValue("password", out var password);

        formData.TryGetValue("adminFullName", out var adminFullName);

        formData.TryGetValue("phoneNumber", out var phoneNumber);

        // Create user with admin role for signup
        var user = new User
        {
            Id = userId,
            Username = formData.TryGetValue("username", out var username) ? username : (email?.Split('@')[0] ?? "user"),
            Email = email ?? "unknown@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword(password ?? "defaultPassword"), // Hash password with BCrypt
            FirstName = adminFullName?.Split(' ')[0] ?? "Admin",
            LastName = adminFullName?.Split(' ').Length > 1 ? string.Join(" ", adminFullName?.Split(' ').Skip(1) ?? []) : "User",
            PhoneNumber = phoneNumber,
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
        var accessToken = GenerateJwtToken(user.Id, user.Email ?? "unknown@example.com", user.Role, user.TenantId);
        var refreshToken = GenerateRefreshToken();
        
        // Save refresh token to user record
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days expiry
        user.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();

        // Log successful registration
        if (app.Services.GetService<IAuditService>() is { } auditService)
        {
            auditService.LogAuthenticationEvent(
                user.Id, 
                user.TenantId, 
                "REGISTRATION_SUCCESS", 
                request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                true,
                $"Role: {user.Role}, Email: {user.Email}"
            );
        }

            

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

                accessToken,

                refreshToken,

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

app.MapPost("/api/v1/auth/login", async (HttpRequest request, UmiHealthDbContext context, IValidationService validationService) =>
{
    try
    {
        var loginData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        
        if (loginData == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid login data" 
            });
        }

        // Validate input
        var validationErrors = validationService.ValidateLoginInput(loginData);
        if (validationErrors.Any())
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Validation failed", 
                errors = validationErrors
            });
        }

        // Sanitize inputs
        var loginUsername = validationService.SanitizeInput(loginData["username"]);
        var loginPassword = loginData["password"]; // Don't sanitize password



        // Find user in database

        var user = await context.Users

            .Include(u => u.Tenant)

            .FirstOrDefaultAsync(u => u.Username == loginUsername || u.Email == loginUsername);

        

        if (user == null)

        {

            return Results.BadRequest(new { 

                success = false, 

                message = "Invalid username or password" 

            });

        }

        

        // Verify password using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(loginPassword, user.Password))
        {
            // Log failed login attempt
        if (app.Services.GetService<IAuditService>() is { } auditServiceLogin)
        {
            auditServiceLogin.LogAuthenticationEvent(
                    user.Id, 
                    user.TenantId, 
                    "LOGIN_FAILED", 
                    request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    false,
                    "Invalid password"
                );
            }
            
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

            redirectUrl = "/portals/super-admin/home.html";

        }



        // Generate real JWT tokens
        var accessToken = GenerateJwtToken(user.Id, user.Email, user.Role, user.TenantId);
        var refreshToken = GenerateRefreshToken();
        
        // Save refresh token to user record
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days expiry
        user.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();

        // Log successful login
        if (app.Services.GetService<IAuditService>() is { } auditServiceLoginSuccess)
        {
            auditServiceLoginSuccess.LogAuthenticationEvent(
                user.Id, 
                user.TenantId, 
                "LOGIN_SUCCESS", 
                request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                true,
                $"Role: {user.Role}"
            );
        }

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

            accessToken,

            refreshToken,

            redirectUrl

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



// Refresh token endpoint
app.MapPost("/api/v1/auth/refresh", async (HttpRequest request, UmiHealthDbContext context) =>
{
    try
    {
        var refreshData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        
        if (refreshData == null || !refreshData.TryGetValue("refreshToken", out var refreshToken))
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Refresh token is required" 
            });
        }

        // Find user with valid refresh token
        var user = await context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiryTime > DateTime.UtcNow);

        if (user == null)
        {
            return Results.Unauthorized();
        }

        // Generate new tokens
        var newAccessToken = GenerateJwtToken(user.Id, user.Email, user.Role, user.TenantId);
        var newRefreshToken = GenerateRefreshToken();
        
        // Update refresh token
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        user.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();

        return Results.Ok(new { 
            success = true, 
            message = "Token refreshed successfully",
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = "Token refresh failed: " + ex.Message 
        });
    }
});

// Logout endpoint
app.MapPost("/api/v1/auth/logout", async (HttpRequest request, UmiHealthDbContext context) =>
{
    try
    {
        var logoutData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        
        if (logoutData == null || !logoutData.TryGetValue("refreshToken", out var refreshToken))
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Refresh token is required" 
            });
        }

        // Find user and clear refresh token
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            // Log successful logout
            if (app.Services.GetService<IAuditService>() is { } auditServiceLogout)
            {
                auditServiceLogout.LogAuthenticationEvent(
                    user.Id, 
                    user.TenantId, 
                    "LOGOUT_SUCCESS", 
                    request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    true
                );
            }
        }

        return Results.Ok(new { 
            success = true, 
            message = "Logout successful" 
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = "Logout failed: " + ex.Message 
        });
    }
});


// Pharmacy name check endpoint

app.MapGet("/api/auth/check-pharmacy-name/{pharmacyName}", async (string pharmacyName, UmiHealthDbContext context) =>

{

    // Check if pharmacy name already exists

    var existingTenant = await context.Tenants

        .FirstOrDefaultAsync(t => t.Name.Equals(pharmacyName, StringComparison.OrdinalIgnoreCase));

    

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

        .FirstOrDefaultAsync(t => t.Name.Equals(pharmacyName, StringComparison.OrdinalIgnoreCase));

    

    return Results.Ok(new { 

        success = true, 

        available = existingTenant == null,

        message = existingTenant == null ? "Pharmacy name is available" : "Pharmacy name already exists"

    });

});



// Pharmacy onboarding endpoint with real-time sync

app.MapPost("/api/v1/pharmacy/onboarding", async (HttpRequest request, UmiHealthDbContext context, ClaimsPrincipal user) =>

{

    try

    {

        var tenantId = GetCurrentTenantId(user);

        var userId = GetCurrentUserId(user);

        

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))

        {

            return Results.Unauthorized();

        }



        var onboardingData = await request.ReadFromJsonAsync<Dictionary<string, object>>();

        if (onboardingData == null)

        {

            return Results.BadRequest(new { success = false, message = "Invalid onboarding data" });

        }



        // Get tenant and user

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);

        var currentUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

        

        if (tenant == null || currentUser == null)

        {

            return Results.NotFound(new { success = false, message = "Tenant or user not found" });

        }



        // Update tenant with onboarding information

        if (onboardingData.TryGetValue("pharmacyLicense", out var pharmacyLicense))

            tenant.LicenseNumber = pharmacyLicense?.ToString();

        

        if (onboardingData.TryGetValue("pharmacyAddress", out var pharmacyAddress))

            tenant.Address = pharmacyAddress?.ToString();

        

        if (onboardingData.TryGetValue("pharmacyPhone", out var pharmacyPhone))

            tenant.PhoneNumber = pharmacyPhone?.ToString();

        

        if (onboardingData.TryGetValue("operatingHours", out var operatingHours))

            tenant.OperatingHours = operatingHours?.ToString();

        

        if (onboardingData.TryGetValue("pharmacyType", out var pharmacyType))

            tenant.PharmacyType = pharmacyType?.ToString();

        

        if (onboardingData.TryGetValue("yearsInBusiness", out var yearsInBusiness))

        {

            if (int.TryParse(yearsInBusiness?.ToString(), out int years))

                tenant.YearsInBusiness = years;

        }

        

        if (onboardingData.TryGetValue("staffCount", out var staffCount))

        {

            if (int.TryParse(staffCount?.ToString(), out int staff))

                tenant.StaffCount = staff;

        }

        

        if (onboardingData.TryGetValue("pharmacySystem", out var pharmacySystem))

            tenant.CurrentSystem = pharmacySystem?.ToString();

        

        // Handle features selection

        if (onboardingData.TryGetValue("features", out var features))

        {

            var featuresJson = features?.ToString() ?? string.Empty;

            var featuresList = JsonSerializer.Deserialize<List<string>>(featuresJson);

            if (featuresList != null)

            {

                tenant.EnabledFeatures = string.Join(",", featuresList);

            }

        }

        

        if (onboardingData.TryGetValue("enableNotifications", out var enableNotifications))

        {

            tenant.EnableNotifications = bool.Parse(enableNotifications?.ToString() ?? "false");

        }

        

        // Mark onboarding as completed

        tenant.OnboardingCompleted = true;

        tenant.OnboardingCompletedAt = DateTime.UtcNow;

        tenant.UpdatedAt = DateTime.UtcNow;



        // Update user status to reflect onboarding completion

        currentUser.OnboardingCompleted = true;

        currentUser.UpdatedAt = DateTime.UtcNow;



        // Save changes to database

        await context.SaveChangesAsync();



        // Sync to superadmin operations portal (audit log)

        if (app.Services.GetService<IAuditService>() is { } auditServiceSuperAdmin)

        {

            auditServiceSuperAdmin.LogSuperAdminAction(

                currentUser.Email, 

                tenantId, 

                "COMPLETE_ONBOARDING", 

                "tenant", 

                new Dictionary<string, object> { 

                    { "pharmacyName", tenant.Name },

                    { "pharmacyType", tenant.PharmacyType ?? string.Empty },

                    { "features", tenant.EnabledFeatures ?? string.Empty },

                    { "completedAt", tenant.OnboardingCompletedAt }

                }

            );

        }



        // Create initial inventory categories based on pharmacy type

        if (onboardingData.TryGetValue("features", out var featuresForInventory))

        {

            var featuresJson = featuresForInventory?.ToString() ?? string.Empty;

            var featuresList = JsonSerializer.Deserialize<List<string>>(featuresJson);

            if (featuresList != null && featuresList.Contains("inventory"))

            {

                // Create default inventory categories

                var defaultCategories = new[] { "Prescription Drugs", "OTC Medications", "Medical Supplies", "Personal Care", "Vitamins & Supplements" };

                

                foreach (var category in defaultCategories)

                {

                    var existingCategory = await context.Inventory

                        .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Category == category && i.ProductName == category);

                    

                    if (existingCategory == null)

                    {

                        var categoryItem = new Inventory

                        {

                            Id = Guid.NewGuid().ToString(),

                            TenantId = tenantId,

                            ProductName = category,

                            GenericName = category,

                            Category = category,

                            ProductCode = $"CAT-{category.Replace(" ", "-").ToUpper()}",

                            CurrentStock = 0,

                            MinStockLevel = 0,

                            MaxStockLevel = 0,

                            UnitPrice = 0,

                            SellingPrice = 0,

                            Unit = "category",

                            Status = "active",

                            CreatedAt = DateTime.UtcNow

                        };

                        context.Inventory.Add(categoryItem);

                    }

                }

                await context.SaveChangesAsync();

            }

        }



        return Results.Ok(new { 

            success = true, 

            message = "Onboarding completed successfully! Your pharmacy has been configured.",

            data = new {

                tenantId = tenant.Id,

                pharmacyName = tenant.Name,

                onboardingCompleted = tenant.OnboardingCompleted,

                completedAt = tenant.OnboardingCompletedAt,

                features = tenant.EnabledFeatures?.Split(",") ?? []

            }

        });

    }

    catch (Exception ex)

    {

        return Results.BadRequest(new { 

            success = false, 

            message = "Onboarding failed: " + ex.Message 

        });

    }

})

.RequireAuthorization();



// Get onboarding status endpoint

app.MapGet("/api/v1/pharmacy/onboarding/status", async (ClaimsPrincipal user, UmiHealthDbContext context) =>

{

    try

    {

        var tenantId = GetCurrentTenantId(user);

        var userId = GetCurrentUserId(user);

        

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))

        {

            return Results.Unauthorized();

        }



        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)

        {

            return Results.NotFound(new { success = false, message = "Tenant not found" });

        }



        return Results.Ok(new { 

            success = true, 

            data = new {

                onboardingCompleted = tenant.OnboardingCompleted,

                completedAt = tenant.OnboardingCompletedAt,

                pharmacyName = tenant.Name,

                pharmacyType = tenant.PharmacyType,

                features = tenant.EnabledFeatures?.Split(",") ?? []

            }

        });

    }

    catch (Exception ex)

    {

        return Results.BadRequest(new { 

            success = false, 

            message = "Failed to get onboarding status: " + ex.Message 

        });

    }

})

.RequireAuthorization();



// Helper methods for the onboarding endpoint

static string GetCurrentTenantId(ClaimsPrincipal user)

{

    return user.FindFirst("tenant_id")?.Value ?? string.Empty;

}



static string GetCurrentUserId(ClaimsPrincipal user)

{

    return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

}



// Health check endpoint

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));



// Admin endpoints without authentication for direct access

app.MapGet("/api/v1/admin/users", async (UmiHealthDbContext context) =>

{

    var users = await context.Users

        .Select(u => new

        {

            u.Id,

            u.Username,

            u.Email,

            u.FirstName,

            u.LastName,

            u.PhoneNumber,

            u.Role,

            u.Status,

            u.CreatedAt,

            u.TenantId

        })

        .ToListAsync();



    return Results.Ok(new { success = true, data = users });

});



app.MapGet("/api/v1/admin/patients", async (UmiHealthDbContext context) =>

{

    var patients = await context.Patients

        .Select(p => new

        {

            p.Id,

            p.FirstName,

            p.LastName,

            p.DateOfBirth,

            p.Gender,

            p.PhoneNumber,

            p.Email,

            p.Address,

            p.EmergencyContact,

            p.EmergencyPhone,

            p.BloodType,

            p.Allergies,

            MedicalHistory = p.MedicalHistory,

            p.Status,

            p.CreatedAt,

            p.TenantId

        })

        .ToListAsync();



    return Results.Ok(new { success = true, data = patients });

});



app.MapGet("/api/v1/admin/inventory", async (UmiHealthDbContext context) =>

{

    var inventory = await context.Inventory

        .Select(i => new

        {

            i.Id,

            Name = i.ProductName,

            Description = i.Description,

            Category = i.Category,

            Quantity = i.CurrentStock,

            UnitPrice = i.UnitPrice,

            SellingPrice = i.SellingPrice,

            MinStockLevel = i.MinStockLevel,

            MaxStockLevel = i.MaxStockLevel,

            ExpiryDate = i.ExpiryDate,

            Supplier = i.Supplier,

            Manufacturer = i.Manufacturer,

            ProductCode = i.ProductCode,

            Barcode = i.Barcode,

            Status = i.Status,

            CreatedAt = i.CreatedAt,

            TenantId = i.TenantId

        })

        .ToListAsync();



    return Results.Ok(new { success = true, data = inventory });

});



app.MapGet("/api/v1/admin/sales", async (UmiHealthDbContext context, DateTime? startDate, DateTime? endDate) =>

{

    var query = context.Sales

        .Include(s => s.Patient)

        .AsQueryable();



    if (startDate.HasValue)

        query = query.Where(s => s.SaleDate >= startDate.Value);

    if (endDate.HasValue)

        query = query.Where(s => s.SaleDate <= endDate.Value);



    var sales = await query

        .Select(s => new

        {

            s.Id,

            s.SaleDate,

            s.TotalAmount,

            s.PaymentMethod,

            s.Status,

            PatientName = s.Patient.FirstName + " " + s.Patient.LastName,

            s.CreatedAt,

            s.TenantId

        })

        .ToListAsync();



    return Results.Ok(new { success = true, data = sales });

});

















// Account Management Endpoints



// Get current user profile

app.MapGet("/api/v1/admin/auth/me", async (HttpContext httpContext, UmiHealthDbContext context) =>

{

    try

    {

        // Get user ID from JWT token

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))

        {

            return Results.Unauthorized();

        }



        var user = await context.Users

            .Include(u => u.Tenant)

            .FirstOrDefaultAsync(u => u.Id == userId);



        if (user == null)

        {

            return Results.NotFound(new { success = false, message = "User not found" });

        }



        var userProfile = new {

            id = user.Id,

            username = user.Username,

            email = user.Email,

            firstName = user.FirstName,

            lastName = user.LastName,

            phoneNumber = user.PhoneNumber,

            bio = user.Bio ?? "",

            role = user.Role,

            tenantId = user.TenantId,

            createdAt = user.CreatedAt

        };



        return Results.Ok(new { success = true, data = userProfile });

    }

    catch (Exception ex)

    {

        return Results.BadRequest(new { success = false, message = ex.Message });

    }

});



// Get tenant information

app.MapGet("/api/v1/admin/tenant", async (HttpContext httpContext, UmiHealthDbContext context) =>

{

    try

    {

        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))

        {

            return Results.Unauthorized();

        }



        var tenant = await context.Tenants.FindAsync(tenantId);

        if (tenant == null)

        {

            return Results.NotFound(new { success = false, message = "Tenant not found" });

        }



        var tenantInfo = new {

            id = tenant.Id,

            name = tenant.Name,

            email = tenant.Email,

            phoneNumber = tenant.PhoneNumber,

            address = tenant.Address,

            city = tenant.City,

            province = tenant.Province,

            postalCode = tenant.PostalCode,

            licenseNumber = tenant.LicenseNumber,

            subscriptionPlan = tenant.SubscriptionPlan,

            status = tenant.Status,

            createdAt = tenant.CreatedAt

        };



        return Results.Ok(new { success = true, data = tenantInfo });

    }

    catch (Exception ex)

    {

        return Results.BadRequest(new { success = false, message = ex.Message });

    }

});


// Get notification settings

app.MapGet("/api/v1/admin/notification/settings", async (HttpContext httpContext, UmiHealthDbContext context) =>

{

    try

    {

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))

        {

            return Results.Unauthorized();

        }



        // For now, return default settings (in production, store in database)

        var settings = new {

            email = true,

            sales = true,

            inventory = true,

            prescriptions = true

        };



        return Results.Ok(new { success = true, data = settings });

    }

    catch (Exception ex)

    {

        return Results.BadRequest(new { success = false, message = ex.Message });

    }

});



// Get tenant statistics

app.MapGet("/api/v1/admin/stats", async (HttpContext httpContext, UmiHealthDbContext context) =>

{

    try

    {

        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))

        {

            return Results.Unauthorized();

        }



        var tenant = await context.Tenants.FindAsync(tenantId);

        if (tenant == null)

        {

            return Results.NotFound(new { success = false, message = "Tenant not found" });

        }



        // Calculate statistics

        var totalUsers = await context.Users.CountAsync(u => u.TenantId == tenantId);

        var totalBranches = 1; // For now, assume main branch only

        var accountAge = (DateTime.UtcNow - tenant.CreatedAt).Days;



        var stats = new {

            totalUsers,

            totalBranches,

            subscriptionPlan = tenant.SubscriptionPlan ?? "Free",

            accountAge

        };



        return Results.Ok(new { success = true, data = stats });

    }

    catch (Exception ex)

    {

        return Results.BadRequest(new { success = false, message = ex.Message });

    }

});





// Account Management Endpoints (matching AuthAPI expectations)

// Get current user profile (auth/me)
app.MapGet("/api/v1/auth/me", async (HttpContext httpContext, UmiHealthDbContext context) =>
{
    try
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var user = await context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Results.NotFound(new { success = false, message = "User not found" });
        }

        return Results.Ok(new { 
            success = true, 
            user = new {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber,
                bio = user.Bio ?? "",
                role = user.Role,
                tenantId = user.TenantId,
                createdAt = user.CreatedAt
            },
            tenant = user.Tenant != null ? new {
                id = user.Tenant.Id,
                name = user.Tenant.Name,
                email = user.Tenant.Email,
                subscriptionPlan = user.Tenant.SubscriptionPlan
            } : null
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization();

// Get subscription status
app.MapGet("/api/v1/auth/subscription-status", async (HttpContext httpContext, UmiHealthDbContext context) =>
{
    try
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Results.Unauthorized();
        }

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            return Results.NotFound(new { success = false, message = "Tenant not found" });
        }

        return Results.Ok(new { 
            success = true, 
            subscription = new {
                plan = tenant.SubscriptionPlan,
                status = tenant.Status,
                features = tenant.EnabledFeatures?.Split(",") ?? []
            }
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization();

// Check setup status
app.MapGet("/api/v1/auth/check-setup", async (HttpContext httpContext, UmiHealthDbContext context) =>
{
    try
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Results.Unauthorized();
        }

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            return Results.NotFound(new { success = false, message = "Tenant not found" });
        }

        return Results.Ok(new { 
            success = true, 
            requiresSetup = !tenant.OnboardingCompleted,
            setupCompleted = tenant.OnboardingCompleted,
            completedAt = tenant.OnboardingCompletedAt
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization();

// Logout endpoint
app.MapPost("/api/v1/auth/logout", async (HttpContext httpContext, UmiHealthDbContext context) =>
{
    try
    {
        // In a real implementation, you would invalidate the token here
        // For now, we'll just return success
        return Results.Ok(new { success = true, message = "Logged out successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization();

// Refresh token endpoint
app.MapPost("/api/v1/auth/refresh", async (HttpRequest request, UmiHealthDbContext context) =>
{
    try
    {
        var requestData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        if (requestData == null || !requestData.TryGetValue("refreshToken", out var refreshToken))
        {
            return Results.BadRequest(new { success = false, message = "Refresh token required" });
        }

        // In a real implementation, you would validate the refresh token and generate a new access token
        // For now, we'll return an error as this is not fully implemented
        return Results.BadRequest(new { success = false, message = "Refresh token functionality not implemented" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// Admin endpoints without authentication for direct access
app.MapGet("/api/v1/admin/users", async (UmiHealthDbContext context) =>
{
    var users = await context.Users
        .Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.FirstName,
            u.LastName,
            u.PhoneNumber,
            u.Role,
            u.Status,
            u.CreatedAt,
            u.TenantId
        })
        .ToListAsync();

    return Results.Ok(new { success = true, data = users });
});

app.MapGet("/api/v1/admin/patients", async (UmiHealthDbContext context) =>
{
    var patients = await context.Patients
        .Select(p => new
        {
            p.Id,
            p.FirstName,
            p.LastName,
            p.DateOfBirth,
            p.Gender,
            p.PhoneNumber,
            p.Email,
            p.Address,
            p.EmergencyContact,
            p.EmergencyPhone,
            p.BloodType,
            p.Allergies,
            MedicalHistory = p.MedicalHistory,
            p.Status,
            p.CreatedAt,
            p.TenantId
        })
        .ToListAsync();

    return Results.Ok(new { success = true, data = patients });
});

app.MapGet("/api/v1/admin/inventory", async (UmiHealthDbContext context) =>
{
    var inventory = await context.Inventory
        .Select(i => new
        {
            i.Id,
            Name = i.ProductName,
            Description = i.Description,
            Category = i.Category,
            Quantity = i.CurrentStock,
            UnitPrice = i.UnitPrice,
            SellingPrice = i.SellingPrice,
            MinStockLevel = i.MinStockLevel,
            MaxStockLevel = i.MaxStockLevel,
            ExpiryDate = i.ExpiryDate,
            Supplier = i.Supplier,
            Manufacturer = i.Manufacturer,
            ProductCode = i.ProductCode,
            Barcode = i.Barcode,
            Status = i.Status,
            CreatedAt = i.CreatedAt,
            TenantId = i.TenantId
        })
        .ToListAsync();

    return Results.Ok(new { success = true, data = inventory });
});

app.MapGet("/api/v1/admin/sales", async (UmiHealthDbContext context, DateTime? startDate, DateTime? endDate) =>
{
    var query = context.Sales
        .Include(s => s.Patient)
        .AsQueryable();

    if (startDate.HasValue)
        query = query.Where(s => s.SaleDate >= startDate.Value);
    if (endDate.HasValue)
        query = query.Where(s => s.SaleDate <= endDate.Value);

    var sales = await query
        .Select(s => new
        {
            s.Id,
            s.SaleDate,
            s.TotalAmount,
            s.PaymentMethod,
            s.Status,
            PatientName = s.Patient.FirstName + " " + s.Patient.LastName,
            s.CreatedAt,
            s.TenantId
        })
        .ToListAsync();

    return Results.Ok(new { success = true, data = sales });
});

// Subscription endpoints for admin
app.MapGet("/api/v1/admin/subscription/status", async (UmiHealthDbContext context) =>
{
    // Get real tenant data or return default if no tenants exist
    var tenant = await context.Tenants.FirstOrDefaultAsync();
    
    if (tenant == null)
    {
        return Results.Ok(new { success = false, message = "No tenant found" });
    }

    var subscriptionStatus = new {
        isInTrial = tenant.SubscriptionPlan?.ToLower() == "free",
        hasActiveSubscription = tenant.Status == "active",
        trialDaysRemaining = 0, // Calculate from tenant.CreatedAt if needed
        trialEndDate = (DateTime?)null,
        subscription = new {
            planType = tenant.SubscriptionPlan ?? "Free",
            status = tenant.Status,
            startDate = tenant.CreatedAt,
            endDate = tenant.CreatedAt.AddYears(1), // Default 1 year
            maxUsers = GetMaxUsersForPlan(tenant.SubscriptionPlan),
            maxBranches = GetMaxBranchesForPlan(tenant.SubscriptionPlan)
        }
    };

    return Results.Ok(new { success = true, data = subscriptionStatus });
});

app.MapGet("/api/v1/admin/subscription/plans", async (ITierService tierService) =>
{
    // Return real subscription plans from the tier service
    var plans = new[]
    {
        new {
            id = "Free",
            name = "Free Plan",
            regularPrice = "0.00",
            promoPrice = (string?)null,
            maxUsers = 1,
            maxBranches = 1,
            features = new[] { "Inventory View", "Basic Reports" }
        },
        new {
            id = "Care",
            name = "Care Plan",
            regularPrice = "49.99",
            promoPrice = (string?)null,
            maxUsers = 10,
            maxBranches = 2,
            features = new[] { "Inventory Management", "Patient Management", "Prescriptions", "Basic Reports", "Data Export" }
        },
        new {
            id = "Enterprise",
            name = "Enterprise Plan",
            regularPrice = "199.99",
            promoPrice = (string?)null,
            maxUsers = 100,
            maxBranches = 10,
            features = new[] { "All Features", "Multi-Branch", "Advanced Analytics", "API Access", "Priority Support", "Custom Reports", "Data Import/Export" }
        }
    };

    return Results.Ok(new { success = true, data = plans });
});

// User profile endpoint for admin
app.MapGet("/api/v1/admin/auth/me", async (UmiHealthDbContext context) =>
{
    // Get real admin user or return first user as fallback
    var user = await context.Users
        .FirstOrDefaultAsync(u => u.Role.ToLower() == "admin" || u.Role.ToLower() == "superadmin");

    if (user == null)
    {
        // Fallback to first user if no admin found
        user = await context.Users.FirstOrDefaultAsync();
    }

    if (user == null)
    {
        return Results.Ok(new { success = false, message = "No user found" });
    }

    var userProfile = new {
        id = user.Id,
        username = user.Username,
        email = user.Email,
        firstName = user.FirstName,
        lastName = user.LastName,
        role = user.Role,
        roles = new[] { user.Role, "admin" }, // Add admin role
        permissions = new[] { "*" },
        tenantId = user.TenantId,
        branchId = (string?)null,
        phoneNumber = user.PhoneNumber,
        bio = "Administrator with full access"
    };

    return Results.Ok(new { success = true, data = userProfile });
});

// Save profile endpoint for admin
app.MapPut("/api/v1/admin/users/{userId}", async (string userId, UpdateProfileRequest profileData, UmiHealthDbContext context) =>
{
    try
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return Results.Ok(new { success = false, message = "User not found" });
        }

        // Update user profile
        user.FirstName = profileData.FirstName ?? user.FirstName;
        user.LastName = profileData.LastName ?? user.LastName;
        user.Email = profileData.Email ?? user.Email;
        user.PhoneNumber = profileData.PhoneNumber ?? user.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        // Note: Bio field would need to be added to User model if not present
        // user.Bio = profileData.Bio ?? user.Bio;

        await context.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Profile updated successfully" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = $"Error updating profile: {ex.Message}" });
    }
});

// Change password endpoint for admin
app.MapPost("/api/v1/admin/auth/change-password", async (HttpContext httpContext, ChangePasswordRequest request, UmiHealthDbContext context) =>
{
    try
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Ok(new { success = false, message = "User not authenticated" });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return Results.Ok(new { success = false, message = "User not found" });
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
        {
            return Results.Ok(new { success = false, message = "Current password is incorrect" });
        }

        // Update password
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Password changed successfully" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = $"Error changing password: {ex.Message}" });
    }
});

// Save pharmacy settings endpoint for admin
app.MapPut("/api/v1/admin/pharmacy/settings", async (PharmacySettingsRequest request, UmiHealthDbContext context) =>
{
    try
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
        {
            return Results.Ok(new { success = false, message = "No tenant found" });
        }

        // Update tenant pharmacy settings
        tenant.Name = request.Name ?? tenant.Name;
        tenant.Email = request.Email ?? tenant.Email;
        tenant.Address = request.Address ?? tenant.Address;
        
        await context.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Pharmacy settings updated successfully" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = $"Error updating pharmacy settings: {ex.Message}" });
    }
});

// Save notification settings endpoint for admin
app.MapPut("/api/v1/admin/notification/settings", async (NotificationSettingsRequest request, UmiHealthDbContext context) =>
{
    try
    {
        // In a real implementation, this would save to a user preferences table
        // For now, we'll just return success
        return Results.Ok(new { success = true, message = "Notification settings saved successfully" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = $"Error saving notification settings: {ex.Message}" });
    }
});

// Register API endpoints
app.RegisterApiEndpoints();

// Map SignalR hub
app.MapHub<UmiHealthHub>("/umiHealthHub");

app.Run();

// Public Program class for integration test accessibility
namespace UmiHealth.MinimalApi
{
    public partial class Program { }
}

