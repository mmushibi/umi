using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace UmiHealth.MinimalApi.Services;

public class InventoryService : IInventoryService
{
    private readonly UmiHealthDbContext _context;
    private readonly IValidationService _validationService;
    private readonly IAuditService _auditService;

    public InventoryService(UmiHealthDbContext context, IValidationService validationService, IAuditService auditService)
    {
        _context = context;
        _validationService = validationService;
        _auditService = auditService;
    }

    public async Task<(bool Success, string Message, Inventory? Inventory)> CreateInventoryItemAsync(Inventory inventory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(inventory.ProductName) || string.IsNullOrWhiteSpace(inventory.ProductCode))
            {
                return (false, "Item name and product code are required", null);
            }

            if (inventory.CurrentStock < 0)
            {
                return (false, "Quantity cannot be negative", null);
            }

            if (inventory.SellingPrice <= 0)
            {
                return (false, "Price must be greater than 0", null);
            }

            inventory.Id = Guid.NewGuid().ToString();
            inventory.CreatedAt = DateTime.UtcNow;
            inventory.UpdatedAt = DateTime.UtcNow;

            _context.Inventory.Add(inventory);
            await _context.SaveChangesAsync();

            _auditService.LogSuperAdminAction(
                null,
                inventory.TenantId,
                "INVENTORY_CREATED",
                "Inventory",
                new Dictionary<string, object> { ["InventoryId"] = inventory.Id, ["Name"] = inventory.ProductName, ["Quantity"] = inventory.CurrentStock }
            );

            return (true, "Inventory item created successfully", inventory);
        }
        catch (Exception ex)
        {
            return (false, $"Inventory creation error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, Inventory? Inventory)> UpdateInventoryAsync(string inventoryId, Inventory inventory)
    {
        try
        {
            var existingItem = await _context.Inventory.FindAsync(inventoryId);
            if (existingItem == null)
            {
                return (false, "Inventory item not found", null);
            }

            existingItem.ProductName = _validationService.SanitizeInput(inventory.ProductName);
            existingItem.Description = _validationService.SanitizeInput(inventory.Description);
            existingItem.ProductCode = _validationService.SanitizeInput(inventory.ProductCode);
            existingItem.Category = _validationService.SanitizeInput(inventory.Category);
            existingItem.Manufacturer = _validationService.SanitizeInput(inventory.Manufacturer);
            existingItem.SellingPrice = inventory.SellingPrice;
            existingItem.UnitPrice = inventory.UnitPrice;
            existingItem.ExpiryDate = inventory.ExpiryDate;
            existingItem.Barcode = _validationService.SanitizeInput(inventory.Barcode);
            existingItem.Supplier = _validationService.SanitizeInput(inventory.Supplier);
            existingItem.MinStockLevel = inventory.MinStockLevel;
            existingItem.MaxStockLevel = inventory.MaxStockLevel;
            existingItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Inventory item updated successfully", existingItem);
        }
        catch (Exception ex)
        {
            return (false, $"Inventory update error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteInventoryAsync(string inventoryId)
    {
        try
        {
            var inventory = await _context.Inventory.FindAsync(inventoryId);
            if (inventory == null)
            {
                return (false, "Inventory item not found");
            }

            _context.Inventory.Remove(inventory);
            await _context.SaveChangesAsync();

            _auditService.LogSuperAdminAction(
                null,
                inventory.TenantId,
                "INVENTORY_DELETED",
                "Inventory",
                new Dictionary<string, object> { ["InventoryId"] = inventoryId, ["Name"] = inventory.ProductName }
            );

            return (true, "Inventory item deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Inventory deletion error: {ex.Message}");
        }
    }

    public async Task<Inventory?> GetInventoryByIdAsync(string inventoryId)
    {
        return await _context.Inventory
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
    }

    public async Task<List<Inventory>> GetInventoryByTenantAsync(string tenantId)
    {
        return await _context.Inventory
            .Where(i => i.TenantId == tenantId)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.ProductName)
            .ToListAsync();
    }

    public async Task<List<Inventory>> GetLowStockItemsAsync(string tenantId, int threshold = 10)
    {
        return await _context.Inventory
            .Where(i => i.TenantId == tenantId && i.CurrentStock <= threshold)
            .OrderBy(i => i.CurrentStock)
            .ThenBy(i => i.ProductName)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> UpdateStockAsync(string inventoryId, int quantity, string operation)
    {
        try
        {
            var inventory = await _context.Inventory.FindAsync(inventoryId);
            if (inventory == null)
            {
                return (false, "Inventory item not found");
            }

            var originalQuantity = inventory.CurrentStock;

            switch (operation.ToLower())
            {
                case "add":
                    inventory.CurrentStock += quantity;
                    break;
                case "subtract":
                    if (inventory.CurrentStock < quantity)
                    {
                        return (false, "Insufficient stock");
                    }
                    inventory.CurrentStock -= quantity;
                    break;
                case "set":
                    if (quantity < 0)
                    {
                        return (false, "Quantity cannot be negative");
                    }
                    inventory.CurrentStock = quantity;
                    break;
                default:
                    return (false, "Invalid operation. Use 'add', 'subtract', or 'set'");
            }

            inventory.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _auditService.LogSuperAdminAction(
                null,
                inventory.TenantId,
                "STOCK_UPDATED",
                "Inventory",
                new Dictionary<string, object> 
                { 
                    ["InventoryId"] = inventoryId, 
                    ["Name"] = inventory.ProductName,
                    ["OriginalQuantity"] = originalQuantity,
                    ["NewQuantity"] = inventory.CurrentStock,
                    ["Operation"] = operation,
                    ["Quantity"] = quantity
                }
            );

            return (true, "Stock updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Stock update error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> BatchUpdateStockAsync(Dictionary<string, (int Quantity, string Operation)> updates)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            foreach (var update in updates)
            {
                var result = await UpdateStockAsync(update.Key, update.Value.Quantity, update.Value.Operation);
                if (!result.Success)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Failed to update inventory item {update.Key}: {result.Message}");
                }
            }

            await transaction.CommitAsync();
            return (true, "Batch stock update completed successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Batch stock update error: {ex.Message}");
        }
    }

    public async Task<List<Inventory>> SearchInventoryAsync(string tenantId, string searchTerm)
    {
        var sanitizedTerm = _validationService.SanitizeInput(searchTerm);
        
        return await _context.Inventory
            .Where(i => i.TenantId == tenantId && 
                       (i.ProductName.Contains(sanitizedTerm) ||
                        i.ProductCode.Contains(sanitizedTerm) ||
                        i.Description.Contains(sanitizedTerm) ||
                        i.Category.Contains(sanitizedTerm) ||
                        i.Manufacturer.Contains(sanitizedTerm)))
            .OrderBy(i => i.Category)
            .ThenBy(i => i.ProductName)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CheckExpiryDatesAsync(string tenantId, int daysThreshold = 30)
    {
        try
        {
            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
            var expiringItems = await _context.Inventory
                .Where(i => i.TenantId == tenantId && 
                           !string.IsNullOrEmpty(i.ExpiryDate) &&
                           DateTime.Parse(i.ExpiryDate) <= thresholdDate)
                .ToListAsync();

            if (expiringItems.Any())
            {
                var itemNames = expiringItems.Select(i => $"{i.ProductName} (Expires: {i.ExpiryDate})");
                return (true, $"Found {expiringItems.Count} items expiring within {daysThreshold} days: {string.Join(", ", itemNames)}");
            }

            return (true, "No items expiring within the specified threshold");
        }
        catch (Exception ex)
        {
            return (false, $"Expiry check error: {ex.Message}");
        }
    }
}
