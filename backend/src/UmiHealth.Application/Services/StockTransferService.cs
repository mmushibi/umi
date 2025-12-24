using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IStockTransferService
    {
        Task<StockTransfer> CreateTransferRequestAsync(CreateStockTransferRequest request);
        Task<IEnumerable<StockTransfer>> GetPendingTransfersAsync(Guid branchId);
        Task<IEnumerable<StockTransfer>> GetTransferHistoryAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<StockTransfer?> GetTransferAsync(Guid transferId);
        Task<bool> ApproveTransferAsync(Guid transferId, Guid approvedByUserId, string? notes = null);
        Task<bool> RejectTransferAsync(Guid transferId, Guid rejectedByUserId, string reason);
        Task<bool> CompleteTransferAsync(Guid transferId, List<CompleteTransferItem> items);
        Task<bool> CancelTransferAsync(Guid transferId, Guid cancelledByUserId, string reason);
        Task<Dictionary<string, object>> GetTransferStatsAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null);
    }

    public class StockTransferService : IStockTransferService
    {
        private readonly SharedDbContext _context;
        private readonly IBranchInventoryService _inventoryService;
        private readonly ILogger<StockTransferService> _logger;

        public StockTransferService(
            SharedDbContext context,
            IBranchInventoryService inventoryService,
            ILogger<StockTransferService> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<StockTransfer> CreateTransferRequestAsync(CreateStockTransferRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Validate source branch has sufficient inventory
                foreach (var item in request.Items)
                {
                    var inventory = await _inventoryService.GetInventoryItemAsync(request.SourceBranchId, item.ProductId);
                    if (inventory == null || inventory.QuantityAvailable < item.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient inventory for product {item.ProductId}");
                    }
                }

                var transfer = new StockTransfer
                {
                    Id = Guid.NewGuid(),
                    TenantId = _context.GetCurrentTenantId(),
                    TransferNumber = await GenerateTransferNumberAsync(),
                    SourceBranchId = request.SourceBranchId,
                    DestinationBranchId = request.DestinationBranchId,
                    Status = "pending",
                    RequestedByUserId = request.RequestedByUserId,
                    Notes = request.Notes,
                    Items = request.Items.Select(item => new StockTransferItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = item.ProductId,
                        QuantityRequested = item.Quantity,
                        QuantityApproved = 0,
                        QuantityTransferred = 0,
                        Notes = item.Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StockTransfers.Add(transfer);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock transfer request created: {TransferId} from {SourceBranch} to {DestinationBranch}",
                    transfer.Id, transfer.SourceBranchId, transfer.DestinationBranchId);

                return transfer;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating stock transfer request");
                throw;
            }
        }

        public async Task<IEnumerable<StockTransfer>> GetPendingTransfersAsync(Guid branchId)
        {
            return await _context.StockTransfers
                .Include(st => st.SourceBranch)
                .Include(st => st.DestinationBranch)
                .Include(st => st.RequestedByUser)
                .Include(st => st.Items)
                .ThenInclude(item => item.Product)
                .Where(st => (st.SourceBranchId == branchId || st.DestinationBranchId == branchId) &&
                           st.Status == "pending")
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockTransfer>> GetTransferHistoryAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.StockTransfers
                .Include(st => st.SourceBranch)
                .Include(st => st.DestinationBranch)
                .Include(st => st.RequestedByUser)
                .Include(st => st.ApprovedByUser)
                .Include(st => st.Items)
                .ThenInclude(item => item.Product)
                .Where(st => (st.SourceBranchId == branchId || st.DestinationBranchId == branchId));

            if (fromDate.HasValue)
                query = query.Where(st => st.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(st => st.CreatedAt <= toDate.Value);

            return await query
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();
        }

        public async Task<StockTransfer?> GetTransferAsync(Guid transferId)
        {
            return await _context.StockTransfers
                .Include(st => st.SourceBranch)
                .Include(st => st.DestinationBranch)
                .Include(st => st.RequestedByUser)
                .Include(st => st.ApprovedByUser)
                .Include(st => st.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(st => st.Id == transferId);
        }

        public async Task<bool> ApproveTransferAsync(Guid transferId, Guid approvedByUserId, string? notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var transfer = await _context.StockTransfers
                    .Include(st => st.Items)
                    .FirstOrDefaultAsync(st => st.Id == transferId);

                if (transfer == null || transfer.Status != "pending")
                {
                    return false;
                }

                // Validate inventory availability again before approval
                foreach (var item in transfer.Items)
                {
                    var inventory = await _inventoryService.GetInventoryItemAsync(transfer.SourceBranchId, item.ProductId);
                    if (inventory == null || inventory.QuantityAvailable < item.QuantityRequested)
                    {
                        throw new InvalidOperationException($"Insufficient inventory for product {item.ProductId} at time of approval");
                    }
                }

                // Reserve inventory
                foreach (var item in transfer.Items)
                {
                    await _inventoryService.ReserveInventoryAsync(transfer.SourceBranchId, item.ProductId, item.QuantityRequested);
                    item.QuantityApproved = item.QuantityRequested;
                    item.UpdatedAt = DateTime.UtcNow;
                }

                transfer.Status = "approved";
                transfer.ApprovedByUserId = approvedByUserId;
                transfer.ApprovedAt = DateTime.UtcNow;
                transfer.Notes = string.IsNullOrEmpty(transfer.Notes) ? notes : $"{transfer.Notes}\n{notes}";
                transfer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock transfer {TransferId} approved by user {UserId}", transferId, approvedByUserId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving stock transfer {TransferId}", transferId);
                throw;
            }
        }

        public async Task<bool> RejectTransferAsync(Guid transferId, Guid rejectedByUserId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var transfer = await _context.StockTransfers
                    .Include(st => st.Items)
                    .FirstOrDefaultAsync(st => st.Id == transferId);

                if (transfer == null || transfer.Status != "pending")
                {
                    return false;
                }

                transfer.Status = "cancelled";
                transfer.Notes = string.IsNullOrEmpty(transfer.Notes) ? reason : $"{transfer.Notes}\nRejection reason: {reason}";
                transfer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock transfer {TransferId} rejected by user {UserId}: {Reason}", transferId, rejectedByUserId, reason);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting stock transfer {TransferId}", transferId);
                throw;
            }
        }

        public async Task<bool> CompleteTransferAsync(Guid transferId, List<CompleteTransferItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var transfer = await _context.StockTransfers
                    .Include(st => st.Items)
                    .FirstOrDefaultAsync(st => st.Id == transferId);

                if (transfer == null || transfer.Status != "approved")
                {
                    return false;
                }

                // Process each item
                foreach (var completeItem in items)
                {
                    var transferItem = transfer.Items.FirstOrDefault(ti => ti.Id == completeItem.TransferItemId);
                    if (transferItem == null)
                    {
                        throw new InvalidOperationException($"Transfer item {completeItem.TransferItemId} not found");
                    }

                    // Transfer inventory
                    var success = await _inventoryService.TransferInventoryAsync(
                        transfer.SourceBranchId,
                        transfer.DestinationBranchId,
                        transferItem.ProductId,
                        completeItem.QuantityTransferred,
                        completeItem.Notes);

                    if (!success)
                    {
                        throw new InvalidOperationException($"Failed to transfer inventory for product {transferItem.ProductId}");
                    }

                    // Update transfer item
                    transferItem.QuantityTransferred = completeItem.QuantityTransferred;
                    transferItem.UpdatedAt = DateTime.UtcNow;

                    // Release reserved inventory
                    await _inventoryService.ReleaseInventoryAsync(
                        transfer.SourceBranchId,
                        transferItem.ProductId,
                        transferItem.QuantityApproved - completeItem.QuantityTransferred);
                }

                transfer.Status = "completed";
                transfer.CompletedAt = DateTime.UtcNow;
                transfer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock transfer {TransferId} completed", transferId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing stock transfer {TransferId}", transferId);
                throw;
            }
        }

        public async Task<bool> CancelTransferAsync(Guid transferId, Guid cancelledByUserId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var transfer = await _context.StockTransfers
                    .Include(st => st.Items)
                    .FirstOrDefaultAsync(st => st.Id == transferId);

                if (transfer == null)
                {
                    return false;
                }

                // Release reserved inventory if approved
                if (transfer.Status == "approved")
                {
                    foreach (var item in transfer.Items)
                    {
                        await _inventoryService.ReleaseInventoryAsync(
                            transfer.SourceBranchId,
                            item.ProductId,
                            item.QuantityApproved);
                    }
                }

                transfer.Status = "cancelled";
                transfer.Notes = string.IsNullOrEmpty(transfer.Notes) ? reason : $"{transfer.Notes}\nCancellation reason: {reason}";
                transfer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock transfer {TransferId} cancelled by user {UserId}: {Reason}", transferId, cancelledByUserId, reason);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling stock transfer {TransferId}", transferId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetTransferStatsAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.StockTransfers
                .Where(st => (st.SourceBranchId == branchId || st.DestinationBranchId == branchId));

            if (fromDate.HasValue)
                query = query.Where(st => st.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(st => st.CreatedAt <= toDate.Value);

            var transfers = await query.ToListAsync();

            var totalTransfers = transfers.Count;
            var pendingTransfers = transfers.Count(st => st.Status == "pending");
            var approvedTransfers = transfers.Count(st => st.Status == "approved");
            var completedTransfers = transfers.Count(st => st.Status == "completed");
            var cancelledTransfers = transfers.Count(st => st.Status == "cancelled");

            var outgoingTransfers = transfers.Count(st => st.SourceBranchId == branchId);
            var incomingTransfers = transfers.Count(st => st.DestinationBranchId == branchId);

            return new Dictionary<string, object>
            {
                ["total_transfers"] = totalTransfers,
                ["pending_transfers"] = pendingTransfers,
                ["approved_transfers"] = approvedTransfers,
                ["completed_transfers"] = completedTransfers,
                ["cancelled_transfers"] = cancelledTransfers,
                ["outgoing_transfers"] = outgoingTransfers,
                ["incoming_transfers"] = incomingTransfers,
                ["period_start"] = fromDate?.ToString("yyyy-MM-dd") ?? "all time",
                ["period_end"] = toDate?.ToString("yyyy-MM-dd") ?? "present"
            };
        }

        private async Task<string> GenerateTransferNumberAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.StockTransfers
                .CountAsync(st => st.CreatedAt.Date == DateTime.UtcNow.Date) + 1;
            
            return $"TRF{today}{count:D4}";
        }
    }

    // DTOs for stock transfer operations
    public class CreateStockTransferRequest
    {
        public Guid SourceBranchId { get; set; }
        public Guid DestinationBranchId { get; set; }
        public Guid RequestedByUserId { get; set; }
        public string? Notes { get; set; }
        public List<CreateTransferItem> Items { get; set; } = new();
    }

    public class CreateTransferItem
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class CompleteTransferItem
    {
        public Guid TransferItemId { get; set; }
        public int QuantityTransferred { get; set; }
        public string? Notes { get; set; }
    }
}
