using Microsoft.EntityFrameworkCore;
using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using System.Security.Claims;

namespace UmiHealth.MinimalApi.Endpoints
{
    public static class ApiEndpoints
    {
        // Helper method to get current user's tenant ID from JWT token
        static string GetCurrentTenantId(ClaimsPrincipal user)
        {
            return user.FindFirst("tenant_id")?.Value ?? string.Empty;
        }

        // Helper method to get current user ID from JWT token
        static string GetCurrentUserId(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        // Helper method to check if user has required role
        static bool HasRole(ClaimsPrincipal user, string role)
        {
            return user.IsInRole(role);
        }

        public static void RegisterApiEndpoints(this WebApplication app)
        {
            // User Management APIs
            app.MapGet("/api/v1/users", async (ClaimsPrincipal user, UmiHealthDbContext context) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var users = await context.Users
                    .Where(u => u.TenantId == tenantId)
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
                        u.CreatedAt
                    })
                    .ToListAsync();

                return Results.Ok(new { success = true, data = users });
            })
            .RequireAuthorization();

            app.MapPost("/api/v1/users", async (ClaimsPrincipal user, UmiHealthDbContext context, User newUser) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                if (!HasRole(user, "admin") && !HasRole(user, "superadmin"))
                    return Results.Forbid();

                newUser.Id = Guid.NewGuid().ToString();
                newUser.TenantId = tenantId;
                newUser.CreatedAt = DateTime.UtcNow;

                context.Users.Add(newUser);
                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "User created successfully", data = newUser });
            })
            .RequireAuthorization();

            app.MapPut("/api/v1/users/{id}", async (ClaimsPrincipal user, UmiHealthDbContext context, string id, User updatedUser) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                if (!HasRole(user, "admin") && !HasRole(user, "superadmin"))
                    return Results.Forbid();

                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

                if (existingUser == null)
                    return Results.NotFound(new { success = false, message = "User not found" });

                existingUser.Username = updatedUser.Username;
                existingUser.Email = updatedUser.Email;
                existingUser.FirstName = updatedUser.FirstName;
                existingUser.LastName = updatedUser.LastName;
                existingUser.PhoneNumber = updatedUser.PhoneNumber;
                existingUser.Role = updatedUser.Role;
                existingUser.Status = updatedUser.Status;

                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "User updated successfully", data = existingUser });
            })
            .RequireAuthorization();

            app.MapDelete("/api/v1/users/{id}", async (ClaimsPrincipal user, UmiHealthDbContext context, string id) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                if (!HasRole(user, "admin") && !HasRole(user, "superadmin"))
                    return Results.Forbid();

                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

                if (existingUser == null)
                    return Results.NotFound(new { success = false, message = "User not found" });

                context.Users.Remove(existingUser);
                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "User deleted successfully" });
            })
            .RequireAuthorization();

            // Patient Management APIs
            app.MapGet("/api/v1/patients", async (ClaimsPrincipal user, UmiHealthDbContext context) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var patients = await context.Patients
                    .Where(p => p.TenantId == tenantId)
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = patients });
            })
            .RequireAuthorization();

            app.MapPost("/api/v1/patients", async (ClaimsPrincipal user, UmiHealthDbContext context, Patient newPatient) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                newPatient.Id = Guid.NewGuid().ToString();
                newPatient.TenantId = tenantId;
                newPatient.CreatedAt = DateTime.UtcNow;

                context.Patients.Add(newPatient);
                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Patient created successfully", data = newPatient });
            })
            .RequireAuthorization();

            app.MapPut("/api/v1/patients/{id}", async (ClaimsPrincipal user, UmiHealthDbContext context, string id, Patient updatedPatient) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var existingPatient = await context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

                if (existingPatient == null)
                    return Results.NotFound(new { success = false, message = "Patient not found" });

                existingPatient.FirstName = updatedPatient.FirstName;
                existingPatient.LastName = updatedPatient.LastName;
                existingPatient.Email = updatedPatient.Email;
                existingPatient.PhoneNumber = updatedPatient.PhoneNumber;
                existingPatient.DateOfBirth = updatedPatient.DateOfBirth;
                existingPatient.Gender = updatedPatient.Gender;
                existingPatient.Address = updatedPatient.Address;
                existingPatient.EmergencyContact = updatedPatient.EmergencyContact;
                existingPatient.EmergencyPhone = updatedPatient.EmergencyPhone;
                existingPatient.BloodType = updatedPatient.BloodType;
                existingPatient.Allergies = updatedPatient.Allergies;
                existingPatient.MedicalHistory = updatedPatient.MedicalHistory;
                existingPatient.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Patient updated successfully", data = existingPatient });
            })
            .RequireAuthorization();

            // Inventory Management APIs
            app.MapGet("/api/v1/inventory", async (ClaimsPrincipal user, UmiHealthDbContext context) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var inventory = await context.Inventory
                    .Where(i => i.TenantId == tenantId)
                    .OrderBy(i => i.ProductName)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = inventory });
            })
            .RequireAuthorization();

            app.MapPost("/api/v1/inventory", async (ClaimsPrincipal user, UmiHealthDbContext context, Inventory newItem) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                newItem.Id = Guid.NewGuid().ToString();
                newItem.TenantId = tenantId;
                newItem.CreatedAt = DateTime.UtcNow;

                context.Inventory.Add(newItem);
                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Inventory item created successfully", data = newItem });
            })
            .RequireAuthorization();

            app.MapPut("/api/v1/inventory/{id}", async (ClaimsPrincipal user, UmiHealthDbContext context, string id, Inventory updatedItem) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var existingItem = await context.Inventory
                    .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

                if (existingItem == null)
                    return Results.NotFound(new { success = false, message = "Inventory item not found" });

                existingItem.ProductName = updatedItem.ProductName;
                existingItem.GenericName = updatedItem.GenericName;
                existingItem.Category = updatedItem.Category;
                existingItem.ProductCode = updatedItem.ProductCode;
                existingItem.Barcode = updatedItem.Barcode;
                existingItem.CurrentStock = updatedItem.CurrentStock;
                existingItem.MinStockLevel = updatedItem.MinStockLevel;
                existingItem.MaxStockLevel = updatedItem.MaxStockLevel;
                existingItem.UnitPrice = updatedItem.UnitPrice;
                existingItem.SellingPrice = updatedItem.SellingPrice;
                existingItem.Unit = updatedItem.Unit;
                existingItem.Manufacturer = updatedItem.Manufacturer;
                existingItem.Supplier = updatedItem.Supplier;
                existingItem.ExpiryDate = updatedItem.ExpiryDate;
                existingItem.Description = updatedItem.Description;
                existingItem.Status = updatedItem.Status;
                existingItem.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Inventory item updated successfully", data = existingItem });
            })
            .RequireAuthorization();

            // Sales APIs
            app.MapGet("/api/v1/sales", async (ClaimsPrincipal user, UmiHealthDbContext context, DateTime? startDate, DateTime? endDate) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var query = context.Sales
                    .Include(s => s.Patient)
                    .Include(s => s.Cashier)
                    .Include(s => s.Items)
                    .Where(s => s.TenantId == tenantId);

                if (startDate.HasValue)
                    query = query.Where(s => s.SaleDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.SaleDate <= endDate.Value);

                var sales = await query
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = sales });
            })
            .RequireAuthorization();

            app.MapPost("/api/v1/sales", async (ClaimsPrincipal user, UmiHealthDbContext context, Sale newSale) =>
            {
                var tenantId = GetCurrentTenantId(user);
                var userId = GetCurrentUserId(user);
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                newSale.Id = Guid.NewGuid().ToString();
                newSale.TenantId = tenantId;
                newSale.CashierId = userId;
                newSale.SaleDate = DateTime.UtcNow;
                newSale.CreatedAt = DateTime.UtcNow;

                // Generate sale number
                var saleCount = await context.Sales.CountAsync(s => s.TenantId == tenantId);
                newSale.SaleNumber = $"SALE-{DateTime.Now:yyyyMMdd}-{(saleCount + 1):D4}";

                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    context.Sales.Add(newSale);
                    await context.SaveChangesAsync();

                    // Update inventory stock
                    foreach (var item in newSale.Items)
                    {
                        var inventoryItem = await context.Inventory
                            .FirstOrDefaultAsync(i => i.Id == item.InventoryId && i.TenantId == tenantId);
                        
                        if (inventoryItem != null)
                        {
                            inventoryItem.CurrentStock -= item.Quantity;
                            item.Id = Guid.NewGuid().ToString();
                            item.SaleId = newSale.Id;
                            item.CreatedAt = DateTime.UtcNow;
                        }
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Results.Ok(new { success = true, message = "Sale created successfully", data = newSale });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { success = false, message = "Failed to create sale" });
                }
            })
            .RequireAuthorization();

            // Payment APIs
            app.MapGet("/api/v1/payments", async (ClaimsPrincipal user, UmiHealthDbContext context, DateTime? startDate, DateTime? endDate) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var query = context.Payments
                    .Include(p => p.Patient)
                    .Include(p => p.Sale)
                    .Where(p => p.TenantId == tenantId);

                if (startDate.HasValue)
                    query = query.Where(p => p.PaymentDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(p => p.PaymentDate <= endDate.Value);

                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = payments });
            })
            .RequireAuthorization();

            app.MapPost("/api/v1/payments", async (ClaimsPrincipal user, UmiHealthDbContext context, Payment newPayment) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                newPayment.Id = Guid.NewGuid().ToString();
                newPayment.TenantId = tenantId;
                newPayment.PaymentDate = DateTime.UtcNow;
                newPayment.CreatedAt = DateTime.UtcNow;

                // Generate payment number
                var paymentCount = await context.Payments.CountAsync(p => p.TenantId == tenantId);
                newPayment.PaymentNumber = $"PAY-{DateTime.Now:yyyyMMdd}-{(paymentCount + 1):D4}";

                context.Payments.Add(newPayment);
                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Payment created successfully", data = newPayment });
            })
            .RequireAuthorization();

            // Prescription APIs
            app.MapGet("/api/v1/prescriptions", async (ClaimsPrincipal user, UmiHealthDbContext context) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var prescriptions = await context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Doctor)
                    .Include(p => p.Items)
                    .Where(p => p.TenantId == tenantId)
                    .OrderByDescending(p => p.PrescriptionDate)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = prescriptions });
            })
            .RequireAuthorization();

            app.MapPost("/api/v1/prescriptions", async (ClaimsPrincipal user, UmiHealthDbContext context, Prescription newPrescription) =>
            {
                var tenantId = GetCurrentTenantId(user);
                var userId = GetCurrentUserId(user);
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                newPrescription.Id = Guid.NewGuid().ToString();
                newPrescription.TenantId = tenantId;
                newPrescription.DoctorId = userId;
                newPrescription.PrescriptionDate = DateTime.UtcNow;
                newPrescription.CreatedAt = DateTime.UtcNow;

                // Generate prescription number
                var prescriptionCount = await context.Prescriptions.CountAsync(p => p.TenantId == tenantId);
                newPrescription.PrescriptionNumber = $"RX-{DateTime.Now:yyyyMMdd}-{(prescriptionCount + 1):D4}";

                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    context.Prescriptions.Add(newPrescription);
                    await context.SaveChangesAsync();

                    foreach (var item in newPrescription.Items)
                    {
                        item.Id = Guid.NewGuid().ToString();
                        item.PrescriptionId = newPrescription.Id;
                        item.CreatedAt = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Results.Ok(new { success = true, message = "Prescription created successfully", data = newPrescription });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { success = false, message = "Failed to create prescription" });
                }
            })
            .RequireAuthorization();

            // Reports APIs
            app.MapGet("/api/v1/reports", async (ClaimsPrincipal user, UmiHealthDbContext context) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var reports = await context.Reports
                    .Where(r => r.TenantId == tenantId)
                    .OrderByDescending(r => r.GeneratedAt)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = reports });
            })
            .RequireAuthorization();

            app.MapPost("/api/v1/reports", async (ClaimsPrincipal user, UmiHealthDbContext context, Report newReport) =>
            {
                var tenantId = GetCurrentTenantId(user);
                var userId = GetCurrentUserId(user);
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();

                newReport.Id = Guid.NewGuid().ToString();
                newReport.TenantId = tenantId;
                newReport.GeneratedBy = userId;
                newReport.GeneratedAt = DateTime.UtcNow;
                newReport.Status = "generating";

                context.Reports.Add(newReport);
                await context.SaveChangesAsync();

                // In a real implementation, you would generate the report here
                // For now, we'll just mark it as generated
                newReport.Status = "generated";
                await context.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Report generated successfully", data = newReport });
            })
            .RequireAuthorization();

            // Dashboard/Summary APIs
            app.MapGet("/api/v1/dashboard/summary", async (ClaimsPrincipal user, UmiHealthDbContext context) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);

                var totalPatients = await context.Patients.CountAsync(p => p.TenantId == tenantId);
                var totalInventory = await context.Inventory.CountAsync(i => i.TenantId == tenantId);
                var lowStockItems = await context.Inventory.CountAsync(i => i.TenantId == tenantId && i.CurrentStock <= i.MinStockLevel);
                
                var todaySales = await context.Sales
                    .Where(s => s.TenantId == tenantId && s.SaleDate.Date == today)
                    .SumAsync(s => s.TotalAmount);
                
                var thisMonthSales = await context.Sales
                    .Where(s => s.TenantId == tenantId && s.SaleDate >= thisMonth)
                    .SumAsync(s => s.TotalAmount);

                var lastMonthSales = await context.Sales
                    .Where(s => s.TenantId == tenantId && s.SaleDate >= lastMonth && s.SaleDate < thisMonth)
                    .SumAsync(s => s.TotalAmount);

                var recentPrescriptions = await context.Prescriptions
                    .Include(p => p.Patient)
                    .Where(p => p.TenantId == tenantId)
                    .OrderByDescending(p => p.PrescriptionDate)
                    .Take(5)
                    .Select(p => new
                    {
                        p.Id,
                        p.PrescriptionNumber,
                        PatientName = p.Patient.FirstName + " " + p.Patient.LastName,
                        p.PrescriptionDate,
                        p.Status
                    })
                    .ToListAsync();

                var summary = new
                {
                    totalPatients,
                    totalInventory,
                    lowStockItems,
                    todaySales,
                    thisMonthSales,
                    lastMonthSales,
                    salesGrowth = lastMonthSales > 0 ? ((thisMonthSales - lastMonthSales) / lastMonthSales) * 100 : 0,
                    recentPrescriptions
                };

                return Results.Ok(new { success = true, data = summary });
            })
            .RequireAuthorization();

            // Search APIs
            app.MapGet("/api/v1/search/patients", async (ClaimsPrincipal user, UmiHealthDbContext context, string query) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var patients = await context.Patients
                    .Where(p => p.TenantId == tenantId && 
                           (p.FirstName.Contains(query) || 
                            p.LastName.Contains(query) || 
                            (p.Email != null && p.Email.Contains(query)) ||
                            (p.PhoneNumber != null && p.PhoneNumber.Contains(query))))
                    .Take(10)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = patients });
            })
            .RequireAuthorization();

            app.MapGet("/api/v1/search/inventory", async (ClaimsPrincipal user, UmiHealthDbContext context, string query) =>
            {
                var tenantId = GetCurrentTenantId(user);
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var inventory = await context.Inventory
                    .Where(i => i.TenantId == tenantId && 
                           (i.ProductName.Contains(query) || 
                            i.GenericName.Contains(query) || 
                            i.ProductCode.Contains(query) ||
                            i.Barcode.Contains(query)))
                    .Take(10)
                    .ToListAsync();

                return Results.Ok(new { success = true, data = inventory });
            })
            .RequireAuthorization();
        }
    }
}
