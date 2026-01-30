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



// Create database on startup (optional - commented out for now)

// using (var scope = app.Services.CreateScope())

// {

//     var context = scope.ServiceProvider.GetRequiredService<UmiHealthDbContext>();

//     context.Database.EnsureCreated();

// }



// Basic registration endpoint with database saving

app.MapPost("/api/v1/auth/register", async (HttpRequest request, UmiHealthDbContext context) =>

{

    try

    {

        var formData = await request.ReadFromJsonAsync<Dictionary<string, string>>();

        

        if (formData == null || 

            !formData.TryGetValue("email", out var email) || 

            !formData.TryGetValue("pharmacyName", out var pharmacyName))

        {

            return Results.BadRequest(new { 

                success = false, 

                message = "Missing required fields: email and pharmacyName" 

            });

        }

        

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

            Password = password ?? "defaultPassword", // In production, hash this

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

app.MapPost("/api/v1/auth/login", async (HttpRequest request, UmiHealthDbContext context) =>

{

    try

    {

        var loginData = await request.ReadFromJsonAsync<Dictionary<string, string>>();

        

        if (loginData == null || 

            !loginData.TryGetValue("username", out var loginUsername) || 

            !loginData.TryGetValue("password", out var loginPassword))

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

            .FirstOrDefaultAsync(u => u.Username == loginUsername || u.Email == loginUsername);

        

        if (user == null)

        {

            return Results.BadRequest(new { 

                success = false, 

                message = "Invalid username or password" 

            });

        }

        

        // Verify password (simple check for demo - in production, use proper hashing)

        if (user.Password != loginPassword)

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

            redirectUrl = "/portals/super-admin/home.html";

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

        if (app.Services.GetService<IAuditService>() is { } auditService)

        {

            auditService.LogSuperAdminAction(

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



// Update user profile

app.MapPut("/api/v1/admin/profile", async (HttpContext httpContext, UpdateProfileRequest request, UmiHealthDbContext context) =>

{

    try

    {

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))

        {

            return Results.Unauthorized();

        }



        var user = await context.Users.FindAsync(userId);

        if (user == null)

        {

            return Results.NotFound(new { success = false, message = "User not found" });

        }



        // Update user profile

        user.FirstName = request.FirstName ?? user.FirstName;

        user.LastName = request.LastName ?? user.LastName;

        user.Email = request.Email ?? user.Email;

        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

        user.Bio = request.Bio ?? user.Bio;

        user.UpdatedAt = DateTime.UtcNow;



        await context.SaveChangesAsync();



        return Results.Ok(new { success = true, message = "Profile updated successfully" });

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



// Update pharmacy settings

app.MapPut("/api/v1/admin/pharmacy/settings", async (HttpContext httpContext, PharmacySettingsRequest request, UmiHealthDbContext context) =>

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



        // Update tenant information

        tenant.Name = request.Name ?? tenant.Name;

        tenant.Email = request.Email ?? tenant.Email;

        tenant.PhoneNumber = request.PhoneNumber ?? tenant.PhoneNumber;

        tenant.Address = request.Address ?? tenant.Address;

        tenant.City = request.City ?? tenant.City;

        tenant.Province = request.Province ?? tenant.Province;

        tenant.PostalCode = request.PostalCode ?? tenant.PostalCode;

        tenant.LicenseNumber = request.LicenseNumber ?? tenant.LicenseNumber;

        tenant.UpdatedAt = DateTime.UtcNow;



        await context.SaveChangesAsync();



        return Results.Ok(new { success = true, message = "Pharmacy settings updated successfully" });

    }

    catch (Exception ex)

    {

        return Results.BadRequest(new { success = false, message = ex.Message });

    }

});



// Change password

app.MapPost("/api/v1/admin/auth/change-password", async (HttpContext httpContext, ChangePasswordRequest request, UmiHealthDbContext context) =>

{

    try

    {

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))

        {

            return Results.Unauthorized();

        }



        if (request.NewPassword != request.ConfirmPassword)

        {

            return Results.BadRequest(new { success = false, message = "Passwords do not match" });

        }



        var user = await context.Users.FindAsync(userId);

        if (user == null)

        {

            return Results.NotFound(new { success = false, message = "User not found" });

        }



        // Verify current password (simple check for demo - in production, use proper hashing)

        if (user.Password != request.CurrentPassword)

        {

            return Results.BadRequest(new { success = false, message = "Current password is incorrect" });

        }



        // Update password

        user.Password = request.NewPassword; // In production, hash this

        user.UpdatedAt = DateTime.UtcNow;



        await context.SaveChangesAsync();



        return Results.Ok(new { success = true, message = "Password changed successfully" });

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



// Update notification settings

app.MapPut("/api/v1/admin/notification/settings", async (HttpContext httpContext, NotificationSettingsRequest request, UmiHealthDbContext context) =>

{

    try

    {

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))

        {

            return Results.Unauthorized();

        }



        // For now, just return success (in production, save to database)

        return Results.Ok(new { success = true, message = "Notification settings saved successfully" });

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

// Register API endpoints
app.RegisterApiEndpoints();

// Map SignalR hub
app.MapHub<UmiHealthHub>("/umiHealthHub");

app.Run();

