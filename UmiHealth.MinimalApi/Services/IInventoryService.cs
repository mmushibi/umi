using UmiHealth.MinimalApi.Models;

namespace UmiHealth.MinimalApi.Services;

public interface IInventoryService
{
    Task<(bool Success, string Message, Inventory? Inventory)> CreateInventoryItemAsync(Inventory inventory);
    Task<(bool Success, string Message, Inventory? Inventory)> UpdateInventoryAsync(string inventoryId, Inventory inventory);
    Task<(bool Success, string Message)> DeleteInventoryAsync(string inventoryId);
    Task<Inventory?> GetInventoryByIdAsync(string inventoryId);
    Task<List<Inventory>> GetInventoryByTenantAsync(string tenantId);
    Task<List<Inventory>> GetLowStockItemsAsync(string tenantId, int threshold = 10);
    Task<(bool Success, string Message)> UpdateStockAsync(string inventoryId, int quantity, string operation);
    Task<(bool Success, string Message)> BatchUpdateStockAsync(Dictionary<string, (int Quantity, string Operation)> updates);
    Task<List<Inventory>> SearchInventoryAsync(string tenantId, string searchTerm);
    Task<(bool Success, string Message)> CheckExpiryDatesAsync(string tenantId, int daysThreshold = 30);
}
