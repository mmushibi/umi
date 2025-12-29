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
    public interface IOfflineSyncService
    {
        Task<SyncResult> SyncPendingOperationsAsync(Guid tenantId, Guid userId);
        Task<List<OfflineOperation>> GetPendingOperationsAsync(Guid tenantId, Guid userId);
        Task QueueOperationAsync(OfflineOperation operation);
        Task<SyncStatus> GetSyncStatusAsync(Guid tenantId, Guid userId);
        Task<bool> IsOnlineAsync(Guid tenantId);
        Task<ConflictResolution> ResolveConflictAsync(Guid operationId, ConflictResolution resolution);
        Task<byte[]> GenerateOfflinePackageAsync(Guid tenantId, Guid userId, DateTime lastSyncDate);
        Task<SyncReport> GenerateSyncReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }

    public class OfflineSyncService : IOfflineSyncService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<OfflineSyncService> _logger;
        private readonly IPaymentService _paymentService;
        private readonly IAuditTrailService _auditService;

        public OfflineSyncService(
            SharedDbContext context,
            ILogger<OfflineSyncService> logger,
            IPaymentService paymentService,
            IAuditTrailService auditService)
        {
            _context = context;
            _logger = logger;
            _paymentService = paymentService;
            _auditService = auditService;
        }

        public async Task<SyncResult> SyncPendingOperationsAsync(Guid tenantId, Guid userId)
        {
            var result = new SyncResult
            {
                TenantId = tenantId,
                UserId = userId,
                StartTime = DateTime.UtcNow,
                SyncedOperations = new List<SyncedOperation>(),
                FailedOperations = new List<FailedOperation>(),
                Conflicts = new List<SyncConflict>()
            };

            try
            {
                var pendingOperations = await GetPendingOperationsAsync(tenantId, userId);
                
                foreach (var operation in pendingOperations.OrderBy(o => o.CreatedAt))
                {
                    try
                    {
                        var syncResult = await ProcessOperationAsync(operation);
                        
                        if (syncResult.Success)
                        {
                            result.SyncedOperations.Add(new SyncedOperation
                            {
                                OperationId = operation.Id,
                                OperationType = operation.OperationType,
                                EntityId = operation.EntityId,
                                ProcessedAt = DateTime.UtcNow
                            });

                            // Mark operation as synced
                            operation.Status = "synced";
                            operation.SyncedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            result.FailedOperations.Add(new FailedOperation
                            {
                                OperationId = operation.Id,
                                OperationType = operation.OperationType,
                                Error = syncResult.Error,
                                RetryCount = operation.RetryCount
                            });

                            // Update retry count
                            operation.RetryCount++;
                            operation.LastError = syncResult.Error;
                            operation.LastAttempt = DateTime.UtcNow;
                            
                            if (operation.RetryCount >= 3)
                            {
                                operation.Status = "failed";
                            }
                            
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to sync operation {OperationId}", operation.Id);
                        result.FailedOperations.Add(new FailedOperation
                        {
                            OperationId = operation.Id,
                            OperationType = operation.OperationType,
                            Error = ex.Message,
                            RetryCount = operation.RetryCount
                        });
                    }
                }

                result.EndTime = DateTime.UtcNow;
                result.Success = result.FailedOperations.Count == 0;

                // Log sync activity
                await _auditService.LogActivityAsync(new AuditLogEntry
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Action = "OfflineSyncCompleted",
                    EntityType = "SyncOperation",
                    Description = $"Synced {result.SyncedOperations.Count} operations, {result.FailedOperations.Count} failed",
                    IpAddress = "System",
                    UserAgent = "OfflineSyncService"
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync failed for tenant {TenantId}, user {UserId}", tenantId, userId);
                result.Success = false;
                result.Error = ex.Message;
                result.EndTime = DateTime.UtcNow;
                return result;
            }
        }

        public async Task<List<OfflineOperation>> GetPendingOperationsAsync(Guid tenantId, Guid userId)
        {
            return await _context.OfflineOperations
                .Where(oo => oo.TenantId == tenantId && 
                             oo.UserId == userId && 
                             oo.Status == "pending")
                .OrderBy(oo => oo.CreatedAt)
                .ToListAsync();
        }

        public async Task QueueOperationAsync(OfflineOperation operation)
        {
            operation.Id = Guid.NewGuid();
            operation.Status = "pending";
            operation.CreatedAt = DateTime.UtcNow;
            operation.RetryCount = 0;

            _context.OfflineOperations.Add(operation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Queued offline operation {OperationId} of type {OperationType}", 
                operation.Id, operation.OperationType);
        }

        public async Task<SyncStatus> GetSyncStatusAsync(Guid tenantId, Guid userId)
        {
            var pendingOperations = await GetPendingOperationsAsync(tenantId, userId);
            var lastSync = await _context.OfflineOperations
                .Where(oo => oo.TenantId == tenantId && oo.UserId == userId && oo.Status == "synced")
                .OrderByDescending(oo => oo.SyncedAt)
                .FirstOrDefaultAsync();

            return new SyncStatus
            {
                TenantId = tenantId,
                UserId = userId,
                IsOnline = await IsOnlineAsync(tenantId),
                PendingOperationsCount = pendingOperations.Count,
                LastSyncTime = lastSync?.SyncedAt,
                HasConflicts = pendingOperations.Any(o => o.Status == "conflict"),
                OldestPendingOperation = pendingOperations.MinBy(o => o.CreatedAt)?.CreatedAt
            };
        }

        public async Task<bool> IsOnlineAsync(Guid tenantId)
        {
            try
            {
                // Simple connectivity check - in production, you'd have more sophisticated checks
                var lastHeartbeat = await _context.TenantHeartbeats
                    .Where(th => th.TenantId == tenantId)
                    .OrderByDescending(th => th.Timestamp)
                    .FirstOrDefaultAsync();

                return lastHeartbeat?.Timestamp > DateTime.UtcNow.AddMinutes(-5);
            }
            catch
            {
                return false;
            }
        }

        public async Task<ConflictResolution> ResolveConflictAsync(Guid operationId, ConflictResolution resolution)
        {
            var operation = await _context.OfflineOperations.FindAsync(operationId);
            if (operation == null)
                throw new ArgumentException("Operation not found");

            try
            {
                if (resolution.Action == "overwrite")
                {
                    // Apply the offline operation, overwriting server data
                    var result = await ProcessOperationAsync(operation, forceOverwrite: true);
                    if (result.Success)
                    {
                        operation.Status = "synced";
                        operation.SyncedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                else if (resolution.Action == "discard")
                {
                    // Discard the offline operation
                    operation.Status = "discarded";
                    operation.ResolutionNotes = resolution.Notes;
                    await _context.SaveChangesAsync();
                }
                else if (resolution.Action == "merge")
                {
                    // Apply merge logic (would be more complex in reality)
                    var mergedData = await MergeDataAsync(operation, resolution.ServerData);
                    operation.Data = System.Text.Json.JsonSerializer.Serialize(mergedData);
                    operation.Status = "synced";
                    operation.SyncedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                resolution.Success = true;
                return resolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve conflict for operation {OperationId}", operationId);
                resolution.Success = false;
                resolution.Error = ex.Message;
                return resolution;
            }
        }

        public async Task<byte[]> GenerateOfflinePackageAsync(Guid tenantId, Guid userId, DateTime lastSyncDate)
        {
            var package = new OfflineDataPackage
            {
                TenantId = tenantId,
                UserId = userId,
                GeneratedAt = DateTime.UtcNow,
                LastSyncDate = lastSyncDate
            };

            // Get reference data needed for offline operations
            package.Products = await _context.Products
                .Where(p => p.TenantId == tenantId && p.IsActive)
                .ToListAsync();

            package.Customers = await _context.Customers
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .ToListAsync();

            package.PaymentMethods = await GetPaymentMethodsAsync(tenantId);
            package.TaxRates = await _context.TaxRates
                .Where(tr => tr.TenantId == tenantId && tr.IsActive)
                .ToListAsync();

            var json = System.Text.Json.JsonSerializer.Serialize(package);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public async Task<SyncReport> GenerateSyncReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var operations = await _context.OfflineOperations
                .Where(oo => oo.TenantId == tenantId && 
                           oo.CreatedAt >= startDate && 
                           oo.CreatedAt <= endDate)
                .ToListAsync();

            return new SyncReport
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalOperations = operations.Count,
                SyncedOperations = operations.Count(o => o.Status == "synced"),
                FailedOperations = operations.Count(o => o.Status == "failed"),
                PendingOperations = operations.Count(o => o.Status == "pending"),
                ConflictedOperations = operations.Count(o => o.Status == "conflict"),
                AverageSyncTime = operations.Where(o => o.SyncedAt.HasValue)
                    .Average(o => (o.SyncedAt!.Value - o.CreatedAt).TotalMinutes),
                OperationsByType = operations
                    .GroupBy(o => o.OperationType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                OperationsByUser = operations
                    .GroupBy(o => o.UserId)
                    .Select(g => new UserSyncStats
                    {
                        UserId = g.Key,
                        OperationCount = g.Count(),
                        SuccessRate = (double)g.Count(o => o.Status == "synced") / g.Count() * 100
                    })
                    .ToList()
            };
        }

        private async Task<OperationResult> ProcessOperationAsync(OfflineOperation operation, bool forceOverwrite = false)
        {
            try
            {
                switch (operation.OperationType.ToLower())
                {
                    case "create_payment":
                        return await ProcessCreatePaymentAsync(operation, forceOverwrite);
                    case "update_payment":
                        return await ProcessUpdatePaymentAsync(operation, forceOverwrite);
                    case "create_sale":
                        return await ProcessCreateSaleAsync(operation, forceOverwrite);
                    case "update_inventory":
                        return await ProcessUpdateInventoryAsync(operation, forceOverwrite);
                    default:
                        return new OperationResult { Success = false, Error = $"Unknown operation type: {operation.OperationType}" };
                }
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<OperationResult> ProcessCreatePaymentAsync(OfflineOperation operation, bool forceOverwrite)
        {
            var paymentData = System.Text.Json.JsonSerializer.Deserialize<CreatePaymentRequest>(operation.Data);
            if (paymentData == null)
                return new OperationResult { Success = false, Error = "Invalid payment data" };

            // Check for conflicts
            if (!forceOverwrite)
            {
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.ReferenceNumber == paymentData.ReferenceNumber);
                
                if (existingPayment != null)
                {
                    return new OperationResult 
                    { 
                        Success = false, 
                        Error = "Payment with this reference already exists",
                        IsConflict = true
                    };
                }
            }

            var result = await _paymentService.CreatePaymentAsync(paymentData);
            return new OperationResult { Success = result.Success, EntityId = result.PaymentId };
        }

        private async Task<OperationResult> ProcessUpdatePaymentAsync(OfflineOperation operation, bool forceOverwrite)
        {
            // Implementation for updating payments
            return new OperationResult { Success = true };
        }

        private async Task<OperationResult> ProcessCreateSaleAsync(OfflineOperation operation, bool forceOverwrite)
        {
            // Implementation for creating sales
            return new OperationResult { Success = true };
        }

        private async Task<OperationResult> ProcessUpdateInventoryAsync(OfflineOperation operation, bool forceOverwrite)
        {
            // Implementation for updating inventory
            return new OperationResult { Success = true };
        }

        private async Task<object> MergeDataAsync(OfflineOperation operation, string serverData)
        {
            // Complex merge logic would go here
            // For now, return the offline data
            return System.Text.Json.JsonSerializer.Deserialize<object>(operation.Data);
        }

        private async Task<List<PaymentMethod>> GetPaymentMethodsAsync(Guid tenantId)
        {
            // Return available payment methods for the tenant
            return new List<PaymentMethod>
            {
                new PaymentMethod { Id = Guid.NewGuid(), Name = "Cash", Code = "cash" },
                new PaymentMethod { Id = Guid.NewGuid(), Name = "Card", Code = "card" },
                new PaymentMethod { Id = Guid.NewGuid(), Name = "Mobile Money", Code = "mobile_money" }
            };
        }
    }

    // Supporting DTOs and Entities
    public class OfflineOperation : TenantEntity
    {
        public Guid UserId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, synced, failed, conflict, discarded
        public DateTime CreatedAt { get; set; }
        public DateTime? SyncedAt { get; set; }
        public DateTime? LastAttempt { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
        public string? ResolutionNotes { get; set; }
    }

    public class SyncResult
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public List<SyncedOperation> SyncedOperations { get; set; } = new();
        public List<FailedOperation> FailedOperations { get; set; } = new();
        public List<SyncConflict> Conflicts { get; set; } = new();
    }

    public class SyncedOperation
    {
        public Guid OperationId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }

    public class FailedOperation
    {
        public Guid OperationId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int RetryCount { get; set; }
    }

    public class SyncConflict
    {
        public Guid OperationId { get; set; }
        public string ConflictType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OfflineData { get; set; } = string.Empty;
        public string ServerData { get; set; } = string.Empty;
    }

    public class SyncStatus
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public bool IsOnline { get; set; }
        public int PendingOperationsCount { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public bool HasConflicts { get; set; }
        public DateTime? OldestPendingOperation { get; set; }
    }

    public class ConflictResolution
    {
        public string Action { get; set; } = string.Empty; // overwrite, discard, merge
        public string ServerData { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class OfflineDataPackage
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime LastSyncDate { get; set; }
        public List<Product> Products { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        public List<TaxRate> TaxRates { get; set; } = new();
    }

    public class SyncReport
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalOperations { get; set; }
        public int SyncedOperations { get; set; }
        public int FailedOperations { get; set; }
        public int PendingOperations { get; set; }
        public int ConflictedOperations { get; set; }
        public double AverageSyncTime { get; set; }
        public Dictionary<string, int> OperationsByType { get; set; } = new();
        public List<UserSyncStats> OperationsByUser { get; set; } = new();
    }

    public class UserSyncStats
    {
        public Guid UserId { get; set; }
        public int OperationCount { get; set; }
        public double SuccessRate { get; set; }
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public bool IsConflict { get; set; }
        public Guid? EntityId { get; set; }
    }

    public class PaymentMethod
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class TenantHeartbeat
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
