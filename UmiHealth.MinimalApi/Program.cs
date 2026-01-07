using UmiHealth.MinimalApi.Services;
using UmiHealth.MinimalApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddCors();

// Add in-memory database for demo
builder.Services.AddSingleton(new Dictionary<string, object>());

// Tier service (scaffolding)
builder.Services.AddSingleton<ITierService, TierService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Tier-based rate limiting and feature gates (scaffolding)
app.UseMiddleware<TierRateLimitMiddleware>();
app.UseMiddleware<FeatureGateMiddleware>();

// Get in-memory databases
var usersDb = app.Services.GetRequiredService<Dictionary<string, object>>();
var tenantsDb = new Dictionary<string, object>();
var inventoryDb = new Dictionary<string, object>();

// Basic registration endpoint with database saving
app.MapPost("/api/v1/auth/register", async (HttpRequest request) =>
{
    try
    {
        var usersDb = app.Services.GetRequiredService<Dictionary<string, object>>();
        var tenantsDb = new Dictionary<string, object>();
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
app.MapGet("/api/v1/admin/users", (Dictionary<string, object> usersDb) =>
{
    var users = usersDb.Values.ToList();
    return Results.Ok(new {
        success = true,
        data = users,
        total = users.Count
    });
});

// Admin create user endpoint
app.MapPost("/api/v1/admin/users", async (HttpRequest request, Dictionary<string, object> usersDb) =>
{
    try
    {
        var userData = await request.ReadFromJsonAsync<Dictionary<string, object>>();
        if (userData == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid user data" 
            });
        }

        var userId = Guid.NewGuid().ToString();
        var newUser = new {
            id = userId,
            firstName = userData.ContainsKey("firstName") ? userData["firstName"] : "",
            lastName = userData.ContainsKey("lastName") ? userData["lastName"] : "",
            email = userData.ContainsKey("email") ? userData["email"] : "",
            phone = userData.ContainsKey("phone") ? userData["phone"] : "",
            role = userData.ContainsKey("role") ? userData["role"] : "employee",
            department = userData.ContainsKey("department") ? userData["department"] : "",
            branchId = userData.ContainsKey("branchId") ? userData["branchId"] : "",
            status = "active",
            hireDate = userData.ContainsKey("hireDate") ? userData["hireDate"] : DateTime.UtcNow.ToString("yyyy-MM-dd"),
            salary = userData.ContainsKey("salary") ? userData["salary"] : 0,
            employmentType = userData.ContainsKey("employmentType") ? userData["employmentType"] : "full-time",
            defaultShift = userData.ContainsKey("defaultShift") ? userData["defaultShift"] : "day",
            workDays = userData.ContainsKey("workDays") ? userData["workDays"] : "monday-friday",
            nrc = userData.ContainsKey("nrc") ? userData["nrc"] : "",
            licenseNumber = userData.ContainsKey("licenseNumber") ? userData["licenseNumber"] : "",
            licenseExpiry = userData.ContainsKey("licenseExpiry") ? userData["licenseExpiry"] : "",
            qualifications = userData.ContainsKey("qualifications") ? userData["qualifications"] : "",
            experience = userData.ContainsKey("experience") ? userData["experience"] : 0,
            createdAt = DateTime.UtcNow,
            tenantId = userData.ContainsKey("tenantId") ? userData["tenantId"] : "default-tenant"
        };

        usersDb[userId] = newUser;

        return Results.Ok(new {
            success = true,
            data = newUser,
            message = "User created successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = $"Error creating user: {ex.Message}" 
        });
    }
});

// Admin update user endpoint
app.MapPut("/admin/users/{userId}", async (string userId, HttpRequest request, Dictionary<string, object> usersDb) =>
{
    try
    {
        if (!usersDb.ContainsKey(userId))
        {
            return Results.NotFound(new { 
                success = false, 
                message = "User not found" 
            });
        }

        var userData = await request.ReadFromJsonAsync<Dictionary<string, object>>();
        if (userData == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid user data" 
            });
        }

        var existingUser = usersDb[userId];
        // Update user properties (simplified - in production you'd merge properties properly)
        var updatedUser = new {
            id = userId,
            firstName = userData.ContainsKey("firstName") ? userData["firstName"] : existingUser.GetType().GetProperty("firstName")?.GetValue(existingUser),
            lastName = userData.ContainsKey("lastName") ? userData["lastName"] : existingUser.GetType().GetProperty("lastName")?.GetValue(existingUser),
            email = userData.ContainsKey("email") ? userData["email"] : existingUser.GetType().GetProperty("email")?.GetValue(existingUser),
            phone = userData.ContainsKey("phone") ? userData["phone"] : existingUser.GetType().GetProperty("phone")?.GetValue(existingUser),
            role = userData.ContainsKey("role") ? userData["role"] : existingUser.GetType().GetProperty("role")?.GetValue(existingUser),
            department = userData.ContainsKey("department") ? userData["department"] : existingUser.GetType().GetProperty("department")?.GetValue(existingUser),
            branchId = userData.ContainsKey("branchId") ? userData["branchId"] : existingUser.GetType().GetProperty("branchId")?.GetValue(existingUser),
            status = userData.ContainsKey("status") ? userData["status"] : existingUser.GetType().GetProperty("status")?.GetValue(existingUser),
            updatedAt = DateTime.UtcNow,
            tenantId = existingUser.GetType().GetProperty("tenantId")?.GetValue(existingUser)
        };

        usersDb[userId] = updatedUser;

        return Results.Ok(new {
            success = true,
            data = updatedUser,
            message = "User updated successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = $"Error updating user: {ex.Message}" 
        });
    }
});

// Admin branches endpoint
app.MapGet("/api/v1/admin/branches", () =>
{
    var branches = new object[0]; // Empty array - no sample data
    
    return Results.Ok(new {
        success = true,
        data = branches,
        total = branches.Length
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

// Admin products endpoint - return empty array (no sample data)
app.MapGet("/admin/products", () =>
{
    var products = new object[0]; // Empty array - no sample data
    
    return Results.Ok(new {
        success = true,
        data = products,
        total = products.Length
    });
});

// ==================== INVENTORY ENDPOINTS ====================

// Get all inventory items
app.MapGet("/api/v1/inventory", (Dictionary<string, object> inventoryDb) =>
{
    var inventory = inventoryDb.Values.ToList();
    return Results.Ok(new {
        success = true,
        data = inventory,
        total = inventory.Count
    });
});

// Get inventory item by ID
app.MapGet("/api/v1/inventory/{id}", (string id, Dictionary<string, object> inventoryDb) =>
{
    if (!inventoryDb.ContainsKey(id))
    {
        return Results.NotFound(new { 
            success = false, 
            message = "Inventory item not found" 
        });
    }

    return Results.Ok(new {
        success = true,
        data = inventoryDb[id]
    });
});

// Create new inventory item
app.MapPost("/api/v1/inventory", async (HttpRequest request, Dictionary<string, object> inventoryDb) =>
{
    try
    {
        var itemData = await request.ReadFromJsonAsync<Dictionary<string, object>>();
        if (itemData == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid inventory data" 
            });
        }

        var itemId = Guid.NewGuid().ToString();
        var newItem = new {
            id = itemId,
            genericName = itemData.ContainsKey("genericName") ? itemData["genericName"] : "",
            brandName = itemData.ContainsKey("brandName") ? itemData["brandName"] : "",
            description = itemData.ContainsKey("description") ? itemData["description"] : "",
            ndcBarcode = itemData.ContainsKey("ndcBarcode") ? itemData["ndcBarcode"] : "",
            strength = itemData.ContainsKey("strength") ? itemData["strength"] : "",
            form = itemData.ContainsKey("form") ? itemData["form"] : "",
            supplier = itemData.ContainsKey("supplier") ? itemData["supplier"] : "",
            batchNumber = itemData.ContainsKey("batchNumber") ? itemData["batchNumber"] : "",
            licenseNumber = itemData.ContainsKey("licenseNumber") ? itemData["licenseNumber"] : "",
            zambiaRegNumber = itemData.ContainsKey("zambiaRegNumber") ? itemData["zambiaRegNumber"] : "",
            manufactureDate = itemData.ContainsKey("manufactureDate") ? itemData["manufactureDate"] : "",
            expiryDate = itemData.ContainsKey("expiryDate") ? itemData["expiryDate"] : "",
            stockQuantity = itemData.ContainsKey("stockQuantity") ? itemData["stockQuantity"] : 0,
            packingType = itemData.ContainsKey("packingType") ? itemData["packingType"] : "",
            location = itemData.ContainsKey("location") ? itemData["location"] : "",
            storageConditions = itemData.ContainsKey("storageConditions") ? itemData["storageConditions"] : "",
            unitCost = itemData.ContainsKey("unitCost") ? itemData["unitCost"] : 0,
            unitPrice = itemData.ContainsKey("unitPrice") ? itemData["unitPrice"] : 0,
            totalValue = itemData.ContainsKey("totalValue") ? itemData["totalValue"] : 0,
            currency = itemData.ContainsKey("currency") ? itemData["currency"] : "ZMW",
            minStockLevel = itemData.ContainsKey("minStockLevel") ? itemData["minStockLevel"] : 0,
            maxStockLevel = itemData.ContainsKey("maxStockLevel") ? itemData["maxStockLevel"] : 0,
            reorderQuantity = itemData.ContainsKey("reorderQuantity") ? itemData["reorderQuantity"] : 0,
            leadTime = itemData.ContainsKey("leadTime") ? itemData["leadTime"] : 0,
            stockStatus = "in-stock",
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        inventoryDb[itemId] = newItem;

        return Results.Ok(new {
            success = true,
            data = newItem,
            message = "Inventory item created successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = $"Error creating inventory item: {ex.Message}" 
        });
    }
}).WithMetadata(new FeatureRequirement("inventory:create"));

// Update inventory item
app.MapPut("/api/v1/inventory/{id}", async (string id, HttpRequest request, Dictionary<string, object> inventoryDb) =>
{
    try
    {
        if (!inventoryDb.ContainsKey(id))
        {
            return Results.NotFound(new { 
                success = false, 
                message = "Inventory item not found" 
            });
        }

        var itemData = await request.ReadFromJsonAsync<Dictionary<string, object>>();
        if (itemData == null)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Invalid inventory data" 
            });
        }

        var existingItem = inventoryDb[id];
        var updatedItem = new {
            id = id,
            genericName = itemData.ContainsKey("genericName") ? itemData["genericName"] : existingItem.GetType().GetProperty("genericName")?.GetValue(existingItem),
            brandName = itemData.ContainsKey("brandName") ? itemData["brandName"] : existingItem.GetType().GetProperty("brandName")?.GetValue(existingItem),
            description = itemData.ContainsKey("description") ? itemData["description"] : existingItem.GetType().GetProperty("description")?.GetValue(existingItem),
            ndcBarcode = itemData.ContainsKey("ndcBarcode") ? itemData["ndcBarcode"] : existingItem.GetType().GetProperty("ndcBarcode")?.GetValue(existingItem),
            strength = itemData.ContainsKey("strength") ? itemData["strength"] : existingItem.GetType().GetProperty("strength")?.GetValue(existingItem),
            form = itemData.ContainsKey("form") ? itemData["form"] : existingItem.GetType().GetProperty("form")?.GetValue(existingItem),
            supplier = itemData.ContainsKey("supplier") ? itemData["supplier"] : existingItem.GetType().GetProperty("supplier")?.GetValue(existingItem),
            batchNumber = itemData.ContainsKey("batchNumber") ? itemData["batchNumber"] : existingItem.GetType().GetProperty("batchNumber")?.GetValue(existingItem),
            licenseNumber = itemData.ContainsKey("licenseNumber") ? itemData["licenseNumber"] : existingItem.GetType().GetProperty("licenseNumber")?.GetValue(existingItem),
            zambiaRegNumber = itemData.ContainsKey("zambiaRegNumber") ? itemData["zambiaRegNumber"] : existingItem.GetType().GetProperty("zambiaRegNumber")?.GetValue(existingItem),
            manufactureDate = itemData.ContainsKey("manufactureDate") ? itemData["manufactureDate"] : existingItem.GetType().GetProperty("manufactureDate")?.GetValue(existingItem),
            expiryDate = itemData.ContainsKey("expiryDate") ? itemData["expiryDate"] : existingItem.GetType().GetProperty("expiryDate")?.GetValue(existingItem),
            stockQuantity = itemData.ContainsKey("stockQuantity") ? itemData["stockQuantity"] : existingItem.GetType().GetProperty("stockQuantity")?.GetValue(existingItem),
            packingType = itemData.ContainsKey("packingType") ? itemData["packingType"] : existingItem.GetType().GetProperty("packingType")?.GetValue(existingItem),
            location = itemData.ContainsKey("location") ? itemData["location"] : existingItem.GetType().GetProperty("location")?.GetValue(existingItem),
            storageConditions = itemData.ContainsKey("storageConditions") ? itemData["storageConditions"] : existingItem.GetType().GetProperty("storageConditions")?.GetValue(existingItem),
            unitCost = itemData.ContainsKey("unitCost") ? itemData["unitCost"] : existingItem.GetType().GetProperty("unitCost")?.GetValue(existingItem),
            unitPrice = itemData.ContainsKey("unitPrice") ? itemData["unitPrice"] : existingItem.GetType().GetProperty("unitPrice")?.GetValue(existingItem),
            totalValue = itemData.ContainsKey("totalValue") ? itemData["totalValue"] : existingItem.GetType().GetProperty("totalValue")?.GetValue(existingItem),
            currency = itemData.ContainsKey("currency") ? itemData["currency"] : existingItem.GetType().GetProperty("currency")?.GetValue(existingItem),
            minStockLevel = itemData.ContainsKey("minStockLevel") ? itemData["minStockLevel"] : existingItem.GetType().GetProperty("minStockLevel")?.GetValue(existingItem),
            maxStockLevel = itemData.ContainsKey("maxStockLevel") ? itemData["maxStockLevel"] : existingItem.GetType().GetProperty("maxStockLevel")?.GetValue(existingItem),
            reorderQuantity = itemData.ContainsKey("reorderQuantity") ? itemData["reorderQuantity"] : existingItem.GetType().GetProperty("reorderQuantity")?.GetValue(existingItem),
            leadTime = itemData.ContainsKey("leadTime") ? itemData["leadTime"] : existingItem.GetType().GetProperty("leadTime")?.GetValue(existingItem),
            stockStatus = itemData.ContainsKey("stockStatus") ? itemData["stockStatus"] : existingItem.GetType().GetProperty("stockStatus")?.GetValue(existingItem),
            createdAt = existingItem.GetType().GetProperty("createdAt")?.GetValue(existingItem),
            updatedAt = DateTime.UtcNow
        };

        inventoryDb[id] = updatedItem;

        return Results.Ok(new {
            success = true,
            data = updatedItem,
            message = "Inventory item updated successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = $"Error updating inventory item: {ex.Message}" 
        });
    }
});

// Delete inventory item
app.MapDelete("/api/v1/inventory/{id}", (string id, Dictionary<string, object> inventoryDb) =>
{
    if (!inventoryDb.ContainsKey(id))
    {
        return Results.NotFound(new { 
            success = false, 
            message = "Inventory item not found" 
        });
    }

    inventoryDb.Remove(id);

    return Results.Ok(new {
        success = true,
        message = "Inventory item deleted successfully"
    });
});

// Dispense inventory item (reduce stock)
app.MapPost("/api/v1/inventory/{id}/dispense", async (string id, HttpRequest request, Dictionary<string, object> inventoryDb) =>
{
    try
    {
        if (!inventoryDb.ContainsKey(id))
        {
            return Results.NotFound(new { 
                success = false, 
                message = "Inventory item not found" 
            });
        }

        var dispenseData = await request.ReadFromJsonAsync<Dictionary<string, object>>();
        if (dispenseData == null || !dispenseData.ContainsKey("quantity"))
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Quantity is required" 
            });
        }

        var existingItem = inventoryDb[id];
        var currentStock = Convert.ToInt32(existingItem.GetType().GetProperty("stockQuantity")?.GetValue(existingItem) ?? 0);
        var minStockLevel = Convert.ToInt32(existingItem.GetType().GetProperty("minStockLevel")?.GetValue(existingItem) ?? 0);
        var quantity = Convert.ToInt32(dispenseData["quantity"]);

        if (quantity > currentStock)
        {
            return Results.BadRequest(new { 
                success = false, 
                message = "Insufficient stock available" 
            });
        }

        var newStockQuantity = currentStock - quantity;
        var newStockStatus = newStockQuantity <= 0 ? "out-of-stock" : 
                           newStockQuantity <= minStockLevel ? "low-stock" : "in-stock";

        var updatedItem = new {
            id = id,
            genericName = existingItem.GetType().GetProperty("genericName")?.GetValue(existingItem),
            brandName = existingItem.GetType().GetProperty("brandName")?.GetValue(existingItem),
            description = existingItem.GetType().GetProperty("description")?.GetValue(existingItem),
            ndcBarcode = existingItem.GetType().GetProperty("ndcBarcode")?.GetValue(existingItem),
            strength = existingItem.GetType().GetProperty("strength")?.GetValue(existingItem),
            form = existingItem.GetType().GetProperty("form")?.GetValue(existingItem),
            supplier = existingItem.GetType().GetProperty("supplier")?.GetValue(existingItem),
            batchNumber = existingItem.GetType().GetProperty("batchNumber")?.GetValue(existingItem),
            licenseNumber = existingItem.GetType().GetProperty("licenseNumber")?.GetValue(existingItem),
            zambiaRegNumber = existingItem.GetType().GetProperty("zambiaRegNumber")?.GetValue(existingItem),
            manufactureDate = existingItem.GetType().GetProperty("manufactureDate")?.GetValue(existingItem),
            expiryDate = existingItem.GetType().GetProperty("expiryDate")?.GetValue(existingItem),
            stockQuantity = newStockQuantity,
            packingType = existingItem.GetType().GetProperty("packingType")?.GetValue(existingItem),
            location = existingItem.GetType().GetProperty("location")?.GetValue(existingItem),
            storageConditions = existingItem.GetType().GetProperty("storageConditions")?.GetValue(existingItem),
            unitCost = existingItem.GetType().GetProperty("unitCost")?.GetValue(existingItem),
            unitPrice = existingItem.GetType().GetProperty("unitPrice")?.GetValue(existingItem),
            totalValue = existingItem.GetType().GetProperty("totalValue")?.GetValue(existingItem),
            currency = existingItem.GetType().GetProperty("currency")?.GetValue(existingItem),
            minStockLevel = minStockLevel,
            maxStockLevel = existingItem.GetType().GetProperty("maxStockLevel")?.GetValue(existingItem),
            reorderQuantity = existingItem.GetType().GetProperty("reorderQuantity")?.GetValue(existingItem),
            leadTime = existingItem.GetType().GetProperty("leadTime")?.GetValue(existingItem),
            stockStatus = newStockStatus,
            createdAt = existingItem.GetType().GetProperty("createdAt")?.GetValue(existingItem),
            updatedAt = DateTime.UtcNow
        };

        inventoryDb[id] = updatedItem;

        return Results.Ok(new {
            success = true,
            data = updatedItem,
            message = $"Dispensed {quantity} units successfully"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { 
            success = false, 
            message = $"Error dispensing inventory: {ex.Message}" 
        });
    }
});

// Get low stock items
app.MapGet("/api/v1/inventory/low-stock", (Dictionary<string, object> inventoryDb) =>
{
    var lowStockItems = inventoryDb.Values.Where(item => {
        var stockQuantity = Convert.ToInt32(item.GetType().GetProperty("stockQuantity")?.GetValue(item) ?? 0);
        var minStockLevel = Convert.ToInt32(item.GetType().GetProperty("minStockLevel")?.GetValue(item) ?? 0);
        return stockQuantity > 0 && stockQuantity <= minStockLevel;
    }).ToList();

    return Results.Ok(new {
        success = true,
        data = lowStockItems,
        total = lowStockItems.Count
    });
});

// Get expiring items
app.MapGet("/api/v1/inventory/expiring", (Dictionary<string, object> inventoryDb) =>
{
    var expiringItems = inventoryDb.Values.Where(item => {
        var expiryDateStr = item.GetType().GetProperty("expiryDate")?.GetValue(item)?.ToString();
        if (string.IsNullOrEmpty(expiryDateStr)) return false;
        
        if (DateTime.TryParse(expiryDateStr, out DateTime expiryDate))
        {
            var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;
            return daysUntilExpiry <= 30 && daysUntilExpiry > 0;
        }
        return false;
    }).ToList();

    return Results.Ok(new {
        success = true,
        data = expiringItems,
        total = expiringItems.Count
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
