using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using System;
using System.Collections.Generic;

namespace UmiHealth.MinimalApi.Tests.TestHelpers
{
    public static class TestDatabaseFactory
    {
        public static UmiHealthDbContext CreateInMemoryDatabase()
        {
            var options = new DbContextOptionsBuilder<UmiHealthDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new UmiHealthDbContext(options);
            
            // Seed test data
            SeedTestData(context);
            
            return context;
        }

        public static void SeedTestData(UmiHealthDbContext context)
        {
            // Create test tenant
            var tenant = new Tenant
            {
                Id = "test-tenant-1",
                Name = "Test Pharmacy",
                Email = "test@pharmacy.com",
                Status = "active",
                SubscriptionPlan = "Care",
                CreatedAt = DateTime.UtcNow
            };

            // Create test users
            var users = new List<User>
            {
                new User
                {
                    Id = "user-1",
                    Username = "admin",
                    Email = "admin@test.com",
                    Password = "hashed_password",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "admin",
                    Status = "active",
                    TenantId = tenant.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = "user-2",
                    Username = "cashier",
                    Email = "cashier@test.com",
                    Password = "hashed_password",
                    FirstName = "Cashier",
                    LastName = "User",
                    Role = "cashier",
                    Status = "active",
                    TenantId = tenant.Id,
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Create test patients
            var patients = new List<Patient>
            {
                new Patient
                {
                    Id = "patient-1",
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@email.com",
                    PhoneNumber = "123-456-7890",
                    Gender = "Male",
                    Address = "123 Main St",
                    BloodType = "O+",
                    Status = "active",
                    TenantId = tenant.Id,
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Create test inventory items
            var inventoryItems = new List<Inventory>
            {
                new Inventory
                {
                    Id = "inventory-1",
                    ProductName = "Paracetamol 500mg",
                    GenericName = "Acetaminophen",
                    Category = "Analgesics",
                    ProductCode = "PAR001",
                    Barcode = "1234567890",
                    Unit = "tablets",
                    CurrentStock = 100,
                    MinStockLevel = 20,
                    MaxStockLevel = 200,
                    UnitPrice = 5.99m,
                    Manufacturer = "PharmaCorp",
                    Supplier = "Medical Supplies Inc",
                    ExpiryDate = "2025-12-31",
                    Status = "active",
                    TenantId = tenant.Id,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Tenants.Add(tenant);
            context.Users.AddRange(users);
            context.Patients.AddRange(patients);
            context.Inventory.AddRange(inventoryItems);
            
            context.SaveChanges();
        }

        public static void CleanDatabase(UmiHealthDbContext context)
        {
            context.SaleItems.RemoveRange(context.SaleItems);
            context.Sales.RemoveRange(context.Sales);
            context.PrescriptionItems.RemoveRange(context.PrescriptionItems);
            context.Prescriptions.RemoveRange(context.Prescriptions);
            context.Payments.RemoveRange(context.Payments);
            context.Reports.RemoveRange(context.Reports);
            context.Inventory.RemoveRange(context.Inventory);
            context.Patients.RemoveRange(context.Patients);
            context.Users.RemoveRange(context.Users);
            context.Tenants.RemoveRange(context.Tenants);
            
            context.SaveChanges();
        }
    }
}
