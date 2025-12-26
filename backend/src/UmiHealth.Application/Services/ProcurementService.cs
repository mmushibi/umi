using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface IProcurementService
    {
        Task<ProcurementRequest> CreateProcurementRequestAsync(CreateProcurementRequest request);
        Task<IEnumerable<ProcurementRequest>> GetPendingRequestsAsync(Guid branchId);
        Task<IEnumerable<ProcurementRequest>> GetProcurementHistoryAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<ProcurementRequest?> GetProcurementRequestAsync(Guid requestId);
        Task<bool> ApproveProcurementRequestAsync(Guid requestId, Guid approvedByUserId, List<ApproveProcurementItem> items, string? notes = null);
        Task<bool> RejectProcurementRequestAsync(Guid requestId, Guid rejectedByUserId, string reason);
        Task<bool> ReceiveProcurementAsync(Guid requestId, List<ReceiveProcurementItem> items);
        Task<bool> DistributeProcurementAsync(Guid requestId, List<ProcurementDistribution> distributions);
        Task<Dictionary<string, object>> GetProcurementStatsAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<ProcurementRequest>> GetCentralProcurementRequestsAsync(Guid tenantId);
    }

    public class ProcurementService : IProcurementService
    {
        private readonly SharedDbContext _context;
        private readonly IBranchInventoryService _inventoryService;
        private readonly ILogger<ProcurementService> _logger;

        public ProcurementService(
            SharedDbContext context,
            IBranchInventoryService inventoryService,
            ILogger<ProcurementService> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<ProcurementRequest> CreateProcurementRequestAsync(CreateProcurementRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var procurement = new ProcurementRequest
                {
                    Id = Guid.NewGuid(),
                    TenantId = _context.GetCurrentTenantId(),
                    RequestNumber = await GenerateProcurementNumberAsync(),
                    RequestingBranchId = request.RequestingBranchId,
                    Type = request.Type,
                    RequestedByUserId = request.RequestedByUserId,
                    ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                    SupplierId = request.SupplierId,
                    Notes = request.Notes,
                    Items = request.Items.Select(item => new ProcurementItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = item.ProductId,
                        QuantityRequested = item.QuantityRequested,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.QuantityRequested * item.UnitPrice,
                        Notes = item.Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                procurement.TotalAmount = procurement.Items.Sum(item => item.TotalPrice);

                _context.ProcurementRequests.Add(procurement);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Procurement request created: {RequestId} by branch {BranchId}",
                    procurement.Id, procurement.RequestingBranchId);

                return procurement;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating procurement request");
                throw;
            }
        }

        public async Task<IEnumerable<ProcurementRequest>> GetPendingRequestsAsync(Guid branchId)
        {
            return await _context.ProcurementRequests
                .Include(pr => pr.RequestingBranch)
                .Include(pr => pr.ApprovingBranch)
                .Include(pr => pr.RequestedByUser)
                .Include(pr => pr.Items)
                .ThenInclude(item => item.Product)
                .Where(pr => (pr.RequestingBranchId == branchId || pr.ApprovingBranchId == branchId) &&
                           pr.Status == "pending")
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProcurementRequest>> GetProcurementHistoryAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.ProcurementRequests
                .Include(pr => pr.RequestingBranch)
                .Include(pr => pr.ApprovingBranch)
                .Include(pr => pr.RequestedByUser)
                .Include(pr => pr.ApprovedByUser)
                .Include(pr => pr.Items)
                .ThenInclude(item => item.Product)
                .Include(pr => pr.Distributions)
                .ThenInclude(d => d.Branch)
                .Where(pr => (pr.RequestingBranchId == branchId || pr.ApprovingBranchId == branchId));

            if (fromDate.HasValue)
                query = query.Where(pr => pr.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(pr => pr.CreatedAt <= toDate.Value);

            return await query
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProcurementRequest?> GetProcurementRequestAsync(Guid requestId)
        {
            return await _context.ProcurementRequests
                .Include(pr => pr.RequestingBranch)
                .Include(pr => pr.ApprovingBranch)
                .Include(pr => pr.RequestedByUser)
                .Include(pr => pr.ApprovedByUser)
                .Include(pr => pr.Items)
                .ThenInclude(item => item.Product)
                .Include(pr => pr.Distributions)
                .ThenInclude(d => d.Branch)
                .FirstOrDefaultAsync(pr => pr.Id == requestId);
        }

        public async Task<bool> ApproveProcurementRequestAsync(Guid requestId, Guid approvedByUserId, List<ApproveProcurementItem> items, string? notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var procurement = await _context.ProcurementRequests
                    .Include(pr => pr.Items)
                    .FirstOrDefaultAsync(pr => pr.Id == requestId);

                if (procurement == null || procurement.Status != "pending")
                {
                    return false;
                }

                // Update approved quantities and prices
                foreach (var approvedItem in items)
                {
                    var item = procurement.Items.FirstOrDefault(i => i.Id == approvedItem.ProcurementItemId);
                    if (item != null)
                    {
                        item.QuantityApproved = approvedItem.QuantityApproved;
                        item.UnitPrice = approvedItem.UnitPrice;
                        item.TotalPrice = approvedItem.QuantityApproved * approvedItem.UnitPrice;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Set approving branch (usually main branch)
                var mainBranch = await _context.Branches
                    .FirstOrDefaultAsync(b => b.TenantId == procurement.TenantId && b.IsMainBranch);
                
                procurement.ApprovingBranchId = mainBranch?.Id;
                procurement.Status = "approved";
                procurement.ApprovedByUserId = approvedByUserId;
                procurement.ApprovedAt = DateTime.UtcNow;
                procurement.TotalAmount = procurement.Items.Sum(item => item.TotalPrice);
                procurement.Notes = string.IsNullOrEmpty(procurement.Notes) ? notes : $"{procurement.Notes}\n{notes}";
                procurement.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Procurement request {RequestId} approved by user {UserId}", requestId, approvedByUserId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving procurement request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<bool> RejectProcurementRequestAsync(Guid requestId, Guid rejectedByUserId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var procurement = await _context.ProcurementRequests
                    .FirstOrDefaultAsync(pr => pr.Id == requestId);

                if (procurement == null || procurement.Status != "pending")
                {
                    return false;
                }

                procurement.Status = "cancelled";
                procurement.Notes = string.IsNullOrEmpty(procurement.Notes) ? reason : $"{procurement.Notes}\nRejection reason: {reason}";
                procurement.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Procurement request {RequestId} rejected by user {UserId}: {Reason}", requestId, rejectedByUserId, reason);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting procurement request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<bool> ReceiveProcurementAsync(Guid requestId, List<ReceiveProcurementItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var procurement = await _context.ProcurementRequests
                    .Include(pr => pr.Items)
                    .FirstOrDefaultAsync(pr => pr.Id == requestId);

                if (procurement == null || procurement.Status != "approved")
                {
                    return false;
                }

                // Update received quantities
                foreach (var receivedItem in items)
                {
                    var item = procurement.Items.FirstOrDefault(i => i.Id == receivedItem.ProcurementItemId);
                    if (item != null)
                    {
                        item.QuantityReceived = receivedItem.QuantityReceived;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                }

                procurement.Status = "received";
                procurement.ReceivedDate = DateTime.UtcNow;
                procurement.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Procurement request {RequestId} received", requestId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error receiving procurement request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<bool> DistributeProcurementAsync(Guid requestId, List<ProcurementDistribution> distributions)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var procurement = await _context.ProcurementRequests
                    .Include(pr => pr.Items)
                    .Include(pr => pr.Distributions)
                    .FirstOrDefaultAsync(pr => pr.Id == requestId);

                if (procurement == null || procurement.Status != "received")
                {
                    return false;
                }

                // Clear existing distributions
                _context.ProcurementDistributions.RemoveRange(procurement.Distributions);

                // Add new distributions
                foreach (var distribution in distributions)
                {
                    distribution.Id = Guid.NewGuid();
                    distribution.ProcurementRequestId = requestId;
                    distribution.CreatedAt = DateTime.UtcNow;
                    distribution.UpdatedAt = DateTime.UtcNow;
                    
                    _context.ProcurementDistributions.Add(distribution);

                    // Update inventory at destination branches
                    await _inventoryService.UpdateInventoryAsync(
                        distribution.BranchId,
                        distribution.ProcurementItemId,
                        distribution.QuantityReceived,
                        $"Procurement distribution from {procurement.RequestNumber}");
                }

                procurement.Status = "completed";
                procurement.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Procurement request {RequestId} distributed to branches", requestId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error distributing procurement request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetProcurementStatsAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.ProcurementRequests
                .Where(pr => (pr.RequestingBranchId == branchId || pr.ApprovingBranchId == branchId));

            if (fromDate.HasValue)
                query = query.Where(pr => pr.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(pr => pr.CreatedAt <= toDate.Value);

            var procurements = await query.ToListAsync();

            var totalRequests = procurements.Count;
            var pendingRequests = procurements.Count(pr => pr.Status == "pending");
            var approvedRequests = procurements.Count(pr => pr.Status == "approved");
            var receivedRequests = procurements.Count(pr => pr.Status == "received");
            var completedRequests = procurements.Count(pr => pr.Status == "completed");
            var cancelledRequests = procurements.Count(pr => pr.Status == "cancelled");

            var totalAmount = procurements.Where(pr => pr.Status == "completed")
                                       .Sum(pr => pr.TotalAmount);

            var requestedByBranch = procurements.Count(pr => pr.RequestingBranchId == branchId);
            var approvedByBranch = procurements.Count(pr => pr.ApprovingBranchId == branchId);

            return new Dictionary<string, object>
            {
                ["total_requests"] = totalRequests,
                ["pending_requests"] = pendingRequests,
                ["approved_requests"] = approvedRequests,
                ["received_requests"] = receivedRequests,
                ["completed_requests"] = completedRequests,
                ["cancelled_requests"] = cancelledRequests,
                ["total_amount"] = totalAmount,
                ["requested_by_branch"] = requestedByBranch,
                ["approved_by_branch"] = approvedByBranch,
                ["period_start"] = fromDate?.ToString("yyyy-MM-dd") ?? "all time",
                ["period_end"] = toDate?.ToString("yyyy-MM-dd") ?? "present"
            };
        }

        public async Task<IEnumerable<ProcurementRequest>> GetCentralProcurementRequestsAsync(Guid tenantId)
        {
            var mainBranch = await _context.Branches
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.IsMainBranch);

            if (mainBranch == null)
                return Enumerable.Empty<ProcurementRequest>();

            return await _context.ProcurementRequests
                .Include(pr => pr.RequestingBranch)
                .Include(pr => pr.RequestedByUser)
                .Include(pr => pr.Items)
                .ThenInclude(item => item.Product)
                .Where(pr => pr.TenantId == tenantId && 
                           pr.Type == "central" &&
                           pr.ApprovingBranchId == mainBranch.Id)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();
        }

        private async Task<string> GenerateProcurementNumberAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.ProcurementRequests
                .CountAsync(pr => pr.CreatedAt.Date == DateTime.UtcNow.Date) + 1;
            
            return $"PRC{today}{count:D4}";
        }
    }

    // DTOs for procurement operations
    public class CreateProcurementRequest
    {
        public Guid RequestingBranchId { get; set; }
        public string Type { get; set; } = "branch"; // central or branch
        public Guid RequestedByUserId { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? SupplierId { get; set; }
        public string? Notes { get; set; }
        public List<CreateProcurementItem> Items { get; set; } = new();
    }

    public class CreateProcurementItem
    {
        public Guid ProductId { get; set; }
        public int QuantityRequested { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class ApproveProcurementItem
    {
        public Guid ProcurementItemId { get; set; }
        public int QuantityApproved { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ReceiveProcurementItem
    {
        public Guid ProcurementItemId { get; set; }
        public int QuantityReceived { get; set; }
    }
}
