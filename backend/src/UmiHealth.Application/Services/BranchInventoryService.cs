using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IBranchInventoryService
    {
        Task<IEnumerable<Inventory>> GetBranchInventoryAsync(Guid branchId);
        Task<Inventory?> GetInventoryItemAsync(Guid branchId, Guid productId);
        Task<Inventory> UpdateInventoryAsync(Guid branchId, Guid productId, int quantity, string? reason = null);
        Task<bool> ReserveInventoryAsync(Guid branchId, Guid productId, int quantity);
        Task<bool> ReleaseInventoryAsync(Guid branchId, Guid productId, int quantity);
        Task<IEnumerable<Inventory>> GetLowStockItemsAsync(Guid branchId);
        Task<IEnumerable<Inventory>> GetExpiringItemsAsync(Guid branchId, int daysThreshold = 30);
        Task<Dictionary<string, object>> GetInventoryStatsAsync(Guid branchId);
        Task<bool> TransferInventoryAsync(Guid sourceBranchId, Guid destinationBranchId, Guid productId, int quantity, string? notes = null);
    }

    public class BranchInventoryService : IBranchInventoryService
    {
        private readonly SharedDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<BranchInventoryService> _logger;

        public BranchInventoryService(
            SharedDbContext context,
            ITenantService tenantService,
            ILogger<BranchInventoryService> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        public async Task<IEnumerable<Inventory>> GetBranchInventoryAsync(Guid branchId)
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Branch)
                .Where(i => i.BranchId == branchId)
                .OrderBy(i => i.Product.Name)
                .ToListAsync();
        }

        public async Task<Inventory?> GetInventoryItemAsync(Guid branchId, Guid productId)
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Branch)
                .FirstOrDefaultAsync(i => i.BranchId == branchId && i.ProductId == productId);
        }

        public async Task<Inventory> UpdateInventoryAsync(Guid branchId, Guid productId, int quantity, string? reason = null)
        {
            var inventory = await GetInventoryItemAsync(branchId, productId);
            
            if (inventory == null)
            {
                // Create new inventory item if it doesn't exist
                inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    TenantId = _context.GetCurrentTenantId(),
                    BranchId = branchId,
                    ProductId = productId,
                    QuantityOnHand = Math.Max(0, quantity),
                    QuantityReserved = 0,
                    QuantityAvailable = Math.Max(0, quantity),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.Inventories.Add(inventory);
            }
            else
            {
                // Update existing inventory
                var oldQuantity = inventory.QuantityOnHand;
                inventory.QuantityOnHand = Math.Max(0, quantity);
                inventory.QuantityAvailable = inventory.QuantityOnHand - inventory.QuantityReserved;
                inventory.UpdatedAt = DateTime.UtcNow;
                
                // Log the adjustment
                await LogInventoryAdjustment(inventory, oldQuantity, quantity, reason);
            }

            await _context.SaveChangesAsync();
            return inventory;
        }

        public async Task<bool> ReserveInventoryAsync(Guid branchId, Guid productId, int quantity)
        {
            var inventory = await GetInventoryItemAsync(branchId, productId);
            
            if (inventory == null || inventory.QuantityAvailable < quantity)
            {
                return false;
            }

            inventory.QuantityReserved += quantity;
            inventory.QuantityAvailable = inventory.QuantityOnHand - inventory.QuantityReserved;
            inventory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReleaseInventoryAsync(Guid branchId, Guid productId, int quantity)
        {
            var inventory = await GetInventoryItemAsync(branchId, productId);
            
            if (inventory == null || inventory.QuantityReserved < quantity)
            {
                return false;
            }

            inventory.QuantityReserved -= quantity;
            inventory.QuantityAvailable = inventory.QuantityOnHand - inventory.QuantityReserved;
            inventory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Inventory>> GetLowStockItemsAsync(Guid branchId)
        {
            return await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.BranchId == branchId && 
                           i.QuantityOnHand <= i.ReorderLevel && 
                           i.ReorderLevel > 0)
                .OrderBy(i => i.QuantityOnHand)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetExpiringItemsAsync(Guid branchId, int daysThreshold = 30)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
            
            return await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.BranchId == branchId && 
                           i.ExpiryDate.HasValue && 
                           i.ExpiryDate <= thresholdDate &&
                           i.QuantityOnHand > 0)
                .OrderBy(i => i.ExpiryDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetInventoryStatsAsync(Guid branchId)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.BranchId == branchId)
                .ToListAsync();

            var totalItems = inventory.Count;
            var totalValue = inventory.Where(i => i.CostPrice.HasValue)
                                   .Sum(i => i.QuantityOnHand * i.CostPrice!.Value);
            var lowStockCount = inventory.Count(i => i.QuantityOnHand <= i.ReorderLevel && i.ReorderLevel > 0);
            var outOfStockCount = inventory.Count(i => i.QuantityOnHand == 0);
            var expiringCount = inventory.Count(i => i.ExpiryDate.HasValue && 
                                                   i.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                                                   i.QuantityOnHand > 0);

            return new Dictionary<string, object>
            {
                ["total_items"] = totalItems,
                ["total_value"] = totalValue,
                ["low_stock_count"] = lowStockCount,
                ["out_of_stock_count"] = outOfStockCount,
                ["expiring_count"] = expiringCount,
                ["last_updated"] = DateTime.UtcNow
            };
        }

        public async Task<bool> TransferInventoryAsync(Guid sourceBranchId, Guid destinationBranchId, Guid productId, int quantity, string? notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Check source inventory
                var sourceInventory = await GetInventoryItemAsync(sourceBranchId, productId);
                if (sourceInventory == null || sourceInventory.QuantityAvailable < quantity)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Get or create destination inventory
                var destInventory = await GetInventoryItemAsync(destinationBranchId, productId);
                if (destInventory == null)
                {
                    destInventory = new Inventory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = sourceInventory.TenantId,
                        BranchId = destinationBranchId,
                        ProductId = productId,
                        QuantityOnHand = 0,
                        QuantityReserved = 0,
                        QuantityAvailable = 0,
                        Status = "active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Inventories.Add(destInventory);
                }

                // Update source inventory
                sourceInventory.QuantityOnHand -= quantity;
                sourceInventory.QuantityAvailable = sourceInventory.QuantityOnHand - sourceInventory.QuantityReserved;
                sourceInventory.UpdatedAt = DateTime.UtcNow;

                // Update destination inventory
                destInventory.QuantityOnHand += quantity;
                destInventory.QuantityAvailable = destInventory.QuantityOnHand - destInventory.QuantityReserved;
                destInventory.UpdatedAt = DateTime.UtcNow;

                // Create stock transfer record
                var transfer = new StockTransfer
                {
                    Id = Guid.NewGuid(),
                    TenantId = sourceInventory.TenantId,
                    TransferNumber = await GenerateTransferNumberAsync(),
                    SourceBranchId = sourceBranchId,
                    DestinationBranchId = destinationBranchId,
                    Status = "completed",
                    RequestedByUserId = _context.GetCurrentUserId(),
                    ApprovedByUserId = _context.GetCurrentUserId(),
                    ApprovedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    Notes = notes,
                    Items = new List<StockTransferItem>
                    {
                        new StockTransferItem
                        {
                            Id = Guid.NewGuid(),
                            StockTransferId = Guid.NewGuid(), // Will be set after transfer is saved
                            ProductId = productId,
                            SourceInventoryId = sourceInventory.Id,
                            DestinationInventoryId = destInventory.Id,
                            QuantityRequested = quantity,
                            QuantityApproved = quantity,
                            QuantityTransferred = quantity,
                            BatchNumber = sourceInventory.BatchNumber,
                            ExpiryDate = sourceInventory.ExpiryDate,
                            CostPrice = sourceInventory.CostPrice,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StockTransfers.Add(transfer);

                // Log the transfer
                await LogStockTransferAsync(transfer);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error transferring inventory from branch {SourceBranchId} to {DestinationBranchId}", 
                    sourceBranchId, destinationBranchId);
                return false;
            }
        }

        private async Task LogInventoryAdjustment(Inventory inventory, int oldQuantity, int newQuantity, string? reason)
        {
            // Implementation would depend on your audit logging system
            _logger.LogInformation("Inventory adjusted for product {ProductId} at branch {BranchId}: {OldQty} -> {NewQty}. Reason: {Reason}",
                inventory.ProductId, inventory.BranchId, oldQuantity, newQuantity, reason);
        }

        private async Task LogStockTransferAsync(StockTransfer transfer)
        {
            // Implementation would depend on your audit logging system
            _logger.LogInformation("Stock transfer {TransferId} completed: {SourceBranch} -> {DestinationBranch}",
                transfer.Id, transfer.SourceBranchId, transfer.DestinationBranchId);
        }

        private async Task<string> GenerateTransferNumberAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.StockTransfers
                .CountAsync(st => st.CreatedAt.Date == DateTime.UtcNow.Date) + 1;
            
            return $"TRF{today}{count:D4}";
        }
    }
}
