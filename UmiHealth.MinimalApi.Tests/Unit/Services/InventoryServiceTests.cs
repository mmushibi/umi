using Microsoft.EntityFrameworkCore;
using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using UmiHealth.MinimalApi.Tests.TestHelpers;
using Xunit;
using FluentAssertions;

namespace UmiHealth.MinimalApi.Tests.Unit.Services
{
    public class InventoryServiceTests
    {
        private readonly UmiHealthDbContext _context;

        public InventoryServiceTests()
        {
            _context = TestDatabaseFactory.CreateInMemoryDatabase();
        }

        [Fact]
        public async Task CreateInventoryItem_ShouldAddItemToDatabase()
        {
            // Arrange
            var newItem = new Inventory
            {
                Id = "new-inventory",
                ProductName = "New Medication",
                GenericName = "New Generic",
                Category = "New Category",
                ProductCode = "NEW001",
                Barcode = "1111111111",
                Unit = "capsules",
                CurrentStock = 75,
                MinStockLevel = 15,
                MaxStockLevel = 150,
                UnitPrice = 12.99m,
                Manufacturer = "New Manufacturer",
                Supplier = "New Supplier",
                ExpiryDate = "2025-08-31",
                Status = "active",
                TenantId = "test-tenant-1",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            _context.Inventory.Add(newItem);
            await _context.SaveChangesAsync();

            // Assert
            var createdItem = await _context.Inventory.FindAsync("new-inventory");
            createdItem.Should().NotBeNull();
            createdItem.ProductName.Should().Be("New Medication");
            createdItem.CurrentStock.Should().Be(75);
        }

        [Fact]
        public async Task GetInventoryItemById_ShouldReturnItem_WhenItemExists()
        {
            // Arrange
            var itemId = "inventory-1";

            // Act
            var item = await _context.Inventory.FindAsync(itemId);

            // Assert
            item.Should().NotBeNull();
            item.Id.Should().Be(itemId);
            item.ProductName.Should().Be("Paracetamol 500mg");
        }

        [Fact]
        public async Task GetLowStockItems_ShouldReturnItemsBelowReorderLevel()
        {
            // Arrange
            var lowStockItem = new Inventory
            {
                Id = "low-stock-item",
                ProductName = "Low Stock Medication",
                GenericName = "Low Generic",
                Category = "Low Category",
                ProductCode = "LOW001",
                Barcode = "2222222222",
                Unit = "tablets",
                CurrentStock = 5, // Below reorder level
                MinStockLevel = 20,
                MaxStockLevel = 100,
                UnitPrice = 8.99m,
                Manufacturer = "Low Manufacturer",
                Supplier = "Low Supplier",
                ExpiryDate = "2025-09-30",
                Status = "active",
                TenantId = "test-tenant-1",
                CreatedAt = DateTime.UtcNow
            };

            _context.Inventory.Add(lowStockItem);
            await _context.SaveChangesAsync();

            // Act
            var lowStockItems = await _context.Inventory
                .Where(i => i.TenantId == "test-tenant-1" && i.CurrentStock <= i.MinStockLevel)
                .ToListAsync();

            // Assert
            lowStockItems.Should().NotBeEmpty();
            lowStockItems.Should().OnlyContain(i => i.CurrentStock <= i.MinStockLevel);
        }

        [Fact]
        public async Task GetExpiringItems_ShouldReturnItemsExpiringWithin30Days()
        {
            // Arrange
            var expiringItem = new Inventory
            {
                Id = "expiring-item",
                ProductName = "Expiring Medication",
                GenericName = "Expiring Generic",
                Category = "Expiring Category",
                ProductCode = "EXP001",
                Barcode = "3333333333",
                Unit = "tablets",
                CurrentStock = 50,
                MinStockLevel = 10,
                MaxStockLevel = 100,
                UnitPrice = 15.99m,
                Manufacturer = "Expiring Manufacturer",
                Supplier = "Expiring Supplier",
                ExpiryDate = DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd"), // Expires in 15 days
                Status = "active",
                TenantId = "test-tenant-1",
                CreatedAt = DateTime.UtcNow
            };

            _context.Inventory.Add(expiringItem);
            await _context.SaveChangesAsync();

            // Act
            var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);
            var allItems = await _context.Inventory
                .Where(i => i.TenantId == "test-tenant-1")
                .ToListAsync();
            
            var expiringItems = allItems
                .Where(i => DateTime.TryParse(i.ExpiryDate, out var expiryDate) && 
                           expiryDate <= thirtyDaysFromNow)
                .ToList();

            // Assert
            expiringItems.Should().NotBeEmpty();
        }

        [Fact]
        public async Task UpdateInventoryQuantity_ShouldModifyQuantity()
        {
            // Arrange
            var itemId = "inventory-1";
            var item = await _context.Inventory.FindAsync(itemId);
            item.Should().NotBeNull();

            var originalQuantity = item!.CurrentStock;
            var newQuantity = 150;

            // Act
            item.CurrentStock = newQuantity;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Assert
            var updatedItem = await _context.Inventory.FindAsync(itemId);
            updatedItem.Should().NotBeNull();
            updatedItem.CurrentStock.Should().Be(newQuantity);
            updatedItem.CurrentStock.Should().NotBe(originalQuantity);
        }

        [Fact]
        public async Task GetInventoryByCategory_ShouldReturnOnlyCategoryItems()
        {
            // Arrange
            var category = "Analgesics";

            // Act
            var categoryItems = await _context.Inventory
                .Where(i => i.TenantId == "test-tenant-1" && i.Category == category)
                .ToListAsync();

            // Assert
            categoryItems.Should().NotBeEmpty();
            categoryItems.Should().OnlyContain(i => i.Category == category);
        }

        [Fact]
        public async Task DeleteInventoryItem_ShouldRemoveItemFromDatabase()
        {
            // Arrange
            var newItem = new Inventory
            {
                Id = "item-to-delete",
                ProductName = "Delete Medication",
                GenericName = "Delete Generic",
                Category = "Delete Category",
                ProductCode = "DEL001",
                Barcode = "4444444444",
                Unit = "tablets",
                CurrentStock = 25,
                MinStockLevel = 5,
                MaxStockLevel = 50,
                UnitPrice = 6.99m,
                Manufacturer = "Delete Manufacturer",
                Supplier = "Delete Supplier",
                ExpiryDate = "2025-10-31",
                Status = "active",
                TenantId = "test-tenant-1",
                CreatedAt = DateTime.UtcNow
            };

            _context.Inventory.Add(newItem);
            await _context.SaveChangesAsync();

            // Act
            _context.Inventory.Remove(newItem);
            await _context.SaveChangesAsync();

            // Assert
            var deletedItem = await _context.Inventory.FindAsync("item-to-delete");
            deletedItem.Should().BeNull();
        }

        [Fact]
        public async Task GetActiveInventory_ShouldReturnOnlyActiveItems()
        {
            // Arrange
            var tenantId = "test-tenant-1";

            // Act
            var activeItems = await _context.Inventory
                .Where(i => i.TenantId == tenantId && i.Status == "active")
                .ToListAsync();

            // Assert
            activeItems.Should().NotBeEmpty();
            activeItems.Should().OnlyContain(i => i.Status == "active");
        }
    }
}
