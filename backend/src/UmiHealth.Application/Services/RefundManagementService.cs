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
    public interface IRefundManagementService
    {
        Task<RefundRequestResult> RequestRefundAsync(RefundRequest request);
        Task<RefundApprovalResult> ProcessRefundApprovalAsync(Guid refundRequestId, Guid approvedBy, bool approved, string? notes = null);
        Task<List<RefundRequest>> GetPendingRefundsAsync(Guid tenantId);
        Task<RefundPolicy> GetRefundPolicyAsync(Guid tenantId, string productCategory = null);
        Task<RefundPolicy> UpdateRefundPolicyAsync(Guid tenantId, RefundPolicy policy);
        Task<bool> ValidateRefundEligibilityAsync(Guid tenantId, Guid paymentId, decimal refundAmount);
        Task<List<RefundRequest>> GetRefundHistoryAsync(Guid tenantId, Guid? customerId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<RefundAnalytics> GenerateRefundAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<byte[]> ExportRefundReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string format = "pdf");
    }

    public class RefundManagementService : IRefundManagementService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<RefundManagementService> _logger;
        private readonly IPaymentService _paymentService;
        private readonly IAuditTrailService _auditService;
        private readonly INotificationService _notificationService;

        public RefundManagementService(
            SharedDbContext context,
            ILogger<RefundManagementService> logger,
            IPaymentService paymentService,
            IAuditTrailService auditService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _paymentService = paymentService;
            _auditService = auditService;
            _notificationService = notificationService;
        }

        public async Task<RefundRequestResult> RequestRefundAsync(RefundRequest request)
        {
            try
            {
                // Validate refund eligibility
                var isEligible = await ValidateRefundEligibilityAsync(request.TenantId, request.PaymentId, request.Amount);
                if (!isEligible)
                {
                    return new RefundRequestResult
                    {
                        Success = false,
                        Error = "Refund request does not meet eligibility criteria",
                        RequiresApproval = false
                    };
                }

                // Get refund policy
                var policy = await GetRefundPolicyAsync(request.TenantId, request.ProductCategory);
                
                // Create refund request
                var refundRequest = new RefundRequestEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    PaymentId = request.PaymentId,
                    CustomerId = request.CustomerId,
                    Amount = request.Amount,
                    Reason = request.Reason,
                    Description = request.Description,
                    ProductCategory = request.ProductCategory,
                    Status = "pending",
                    RequiresApproval = policy.RequiresApproval,
                    AutoApprove = policy.AutoApprove,
                    MaxRefundAmount = policy.MaxRefundAmount,
                    RefundWindowDays = policy.RefundWindowDays,
                    RequestedBy = request.RequestedBy,
                    RequestedAt = DateTime.UtcNow,
                    ReferenceNumber = await GenerateRefundReferenceAsync(request.TenantId)
                };

                _context.RefundRequests.Add(refundRequest);
                await _context.SaveChangesAsync();

                // Auto-approve if policy allows
                if (policy.AutoApprove && request.Amount <= policy.AutoApproveMaxAmount)
                {
                    var approvalResult = await ProcessRefundApprovalAsync(refundRequest.Id, request.RequestedBy, true, "Auto-approved by system");
                    if (!approvalResult.Success)
                    {
                        _logger.LogWarning("Auto-approval failed for refund request {RefundId}", refundRequest.Id);
                    }
                }
                else
                {
                    // Send notification for manual approval
                    await SendApprovalNotificationAsync(refundRequest);
                }

                // Log the request
                await _auditService.LogActivityAsync(new AuditLogEntry
                {
                    TenantId = request.TenantId,
                    UserId = request.RequestedBy,
                    Action = "RefundRequested",
                    EntityType = "RefundRequest",
                    EntityId = refundRequest.Id.ToString(),
                    Description = $"Refund request for {request.Amount:C} - {request.Reason}",
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent
                });

                return new RefundRequestResult
                {
                    Success = true,
                    RefundRequestId = refundRequest.Id,
                    ReferenceNumber = refundRequest.ReferenceNumber,
                    Status = refundRequest.Status,
                    RequiresApproval = refundRequest.RequiresApproval,
                    EstimatedProcessingTime = policy.EstimatedProcessingTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create refund request");
                return new RefundRequestResult
                {
                    Success = false,
                    Error = "Failed to process refund request"
                };
            }
        }

        public async Task<RefundApprovalResult> ProcessRefundApprovalAsync(Guid refundRequestId, Guid approvedBy, bool approved, string? notes = null)
        {
            try
            {
                var refundRequest = await _context.RefundRequests
                    .Include(rr => rr.Payment)
                    .FirstOrDefaultAsync(rr => rr.Id == refundRequestId);

                if (refundRequest == null)
                {
                    return new RefundApprovalResult
                    {
                        Success = false,
                        Error = "Refund request not found"
                    };
                }

                if (refundRequest.Status != "pending")
                {
                    return new RefundApprovalResult
                    {
                        Success = false,
                        Error = "Refund request has already been processed"
                    };
                }

                refundRequest.Status = approved ? "approved" : "rejected";
                refundRequest.ApprovedBy = approvedBy;
                refundRequest.ApprovedAt = DateTime.UtcNow;
                refundRequest.ApprovalNotes = notes;

                if (approved)
                {
                    // Process the actual refund
                    var refundResult = await ProcessRefundAsync(refundRequest);
                    if (refundResult.Success)
                    {
                        refundRequest.RefundId = refundResult.RefundId;
                        refundRequest.RefundProcessedAt = DateTime.UtcNow;
                        refundRequest.Status = "completed";
                    }
                    else
                    {
                        refundRequest.Status = "approval_failed";
                        refundRequest.FailureReason = refundResult.Error;
                    }
                }

                await _context.SaveChangesAsync();

                // Send notification to customer
                await SendRefundStatusNotificationAsync(refundRequest);

                // Log the approval
                await _auditService.LogActivityAsync(new AuditLogEntry
                {
                    TenantId = refundRequest.TenantId,
                    UserId = approvedBy,
                    Action = approved ? "RefundApproved" : "RefundRejected",
                    EntityType = "RefundRequest",
                    EntityId = refundRequest.Id.ToString(),
                    Description = $"Refund request {approved ? "approved" : "rejected"} for {refundRequest.Amount:C}",
                    AdditionalData = System.Text.Json.JsonSerializer.Serialize(new { notes })
                });

                return new RefundApprovalResult
                {
                    Success = approved ? refundRequest.Status == "completed" : true,
                    RefundRequestId = refundRequest.Id,
                    Status = refundRequest.Status,
                    RefundId = refundRequest.RefundId,
                    ProcessedAt = refundRequest.ApprovedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process refund approval");
                return new RefundApprovalResult
                {
                    Success = false,
                    Error = "Failed to process approval"
                };
            }
        }

        public async Task<List<RefundRequest>> GetPendingRefundsAsync(Guid tenantId)
        {
            return await _context.RefundRequests
                .Where(rr => rr.TenantId == tenantId && rr.Status == "pending")
                .Include(rr => rr.Payment)
                .Include(rr => rr.Customer)
                .OrderBy(rr => rr.RequestedAt)
                .ToListAsync();
        }

        public async Task<RefundPolicy> GetRefundPolicyAsync(Guid tenantId, string productCategory = null)
        {
            var policy = await _context.RefundPolicies
                .FirstOrDefaultAsync(rp => rp.TenantId == tenantId && 
                                         (string.IsNullOrEmpty(productCategory) || rp.ProductCategory == productCategory) &&
                                         rp.IsActive);

            // Return default policy if no specific policy found
            return policy ?? new RefundPolicy
            {
                TenantId = tenantId,
                ProductCategory = productCategory ?? "default",
                RequiresApproval = true,
                AutoApprove = false,
                AutoApproveMaxAmount = 100,
                MaxRefundAmount = decimal.MaxValue,
                RefundWindowDays = 30,
                EstimatedProcessingTime = "3-5 business days",
                IsActive = true
            };
        }

        public async Task<RefundPolicy> UpdateRefundPolicyAsync(Guid tenantId, RefundPolicy policy)
        {
            var existingPolicy = await _context.RefundPolicies
                .FirstOrDefaultAsync(rp => rp.TenantId == tenantId && 
                                         rp.ProductCategory == policy.ProductCategory);

            if (existingPolicy != null)
            {
                existingPolicy.RequiresApproval = policy.RequiresApproval;
                existingPolicy.AutoApprove = policy.AutoApprove;
                existingPolicy.AutoApproveMaxAmount = policy.AutoApproveMaxAmount;
                existingPolicy.MaxRefundAmount = policy.MaxRefundAmount;
                existingPolicy.RefundWindowDays = policy.RefundWindowDays;
                existingPolicy.EstimatedProcessingTime = policy.EstimatedProcessingTime;
                existingPolicy.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existingPolicy = new RefundPolicyEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductCategory = policy.ProductCategory,
                    RequiresApproval = policy.RequiresApproval,
                    AutoApprove = policy.AutoApprove,
                    AutoApproveMaxAmount = policy.AutoApproveMaxAmount,
                    MaxRefundAmount = policy.MaxRefundAmount,
                    RefundWindowDays = policy.RefundWindowDays,
                    EstimatedProcessingTime = policy.EstimatedProcessingTime,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.RefundPolicies.Add(existingPolicy);
            }

            await _context.SaveChangesAsync();
            
            policy.Id = existingPolicy.Id;
            return policy;
        }

        public async Task<bool> ValidateRefundEligibilityAsync(Guid tenantId, Guid paymentId, decimal refundAmount)
        {
            var payment = await _context.Payments
                .Include(p => p.RefundRequests)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenantId);

            if (payment == null)
                return false;

            // Check if payment is within refund window
            var policy = await GetRefundPolicyAsync(tenantId);
            if (payment.PaymentDate.AddDays(policy.RefundWindowDays) < DateTime.UtcNow)
                return false;

            // Check if refund amount exceeds payment amount
            var totalRefunded = payment.RefundRequests
                .Where(rr => rr.Status == "completed")
                .Sum(rr => rr.Amount);

            if (totalRefunded + refundAmount > payment.Amount)
                return false;

            // Check if refund amount exceeds maximum allowed
            if (refundAmount > policy.MaxRefundAmount)
                return false;

            return true;
        }

        public async Task<List<RefundRequest>> GetRefundHistoryAsync(Guid tenantId, Guid? customerId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.RefundRequests
                .Where(rr => rr.TenantId == tenantId)
                .Include(rr => rr.Payment)
                .Include(rr => rr.Customer)
                .AsQueryable();

            if (customerId.HasValue)
                query = query.Where(rr => rr.CustomerId == customerId.Value);

            if (startDate.HasValue)
                query = query.Where(rr => rr.RequestedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(rr => rr.RequestedAt <= endDate.Value);

            return await query
                .OrderByDescending(rr => rr.RequestedAt)
                .ToListAsync();
        }

        public async Task<RefundAnalytics> GenerateRefundAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var refundRequests = await _context.RefundRequests
                .Where(rr => rr.TenantId == tenantId && 
                           rr.RequestedAt >= startDate && 
                           rr.RequestedAt <= endDate)
                .Include(rr => rr.Payment)
                .Include(rr => rr.Customer)
                .ToListAsync();

            return new RefundAnalytics
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalRequests = refundRequests.Count,
                ApprovedRequests = refundRequests.Count(rr => rr.Status == "completed"),
                RejectedRequests = refundRequests.Count(rr => rr.Status == "rejected"),
                PendingRequests = refundRequests.Count(rr => rr.Status == "pending"),
                TotalRefundAmount = refundRequests.Where(rr => rr.Status == "completed").Sum(rr => rr.Amount),
                AverageRefundAmount = refundRequests.Where(rr => rr.Status == "completed").Any() ? 
                    refundRequests.Where(rr => rr.Status == "completed").Average(rr => rr.Amount) : 0,
                RefundsByReason = refundRequests
                    .Where(rr => rr.Status == "completed")
                    .GroupBy(rr => rr.Reason)
                    .Select(g => new RefundReasonAnalytics
                    {
                        Reason = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(rr => rr.Amount),
                        AverageAmount = g.Average(rr => rr.Amount)
                    })
                    .OrderByDescending(r => r.TotalAmount)
                    .ToList(),
                RefundsByProductCategory = refundRequests
                    .Where(rr => rr.Status == "completed")
                    .GroupBy(rr => rr.ProductCategory)
                    .Select(g => new RefundCategoryAnalytics
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(rr => rr.Amount),
                        AverageAmount = g.Average(rr => rr.Amount)
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .ToList(),
                ProcessingTime = refundRequests
                    .Where(rr => rr.Status == "completed" && rr.RefundProcessedAt.HasValue)
                    .Average(rr => (rr.RefundProcessedAt!.Value - rr.RequestedAt).TotalDays),
                TopRefundingCustomers = refundRequests
                    .Where(rr => rr.Status == "completed")
                    .GroupBy(rr => rr.CustomerId)
                    .Select(g => new CustomerRefundAnalytics
                    {
                        CustomerId = g.Key,
                        CustomerName = g.FirstOrDefault()?.Customer?.FullName ?? "Unknown",
                        RefundCount = g.Count(),
                        TotalRefundAmount = g.Sum(rr => rr.Amount),
                        AverageRefundAmount = g.Average(rr => rr.Amount)
                    })
                    .OrderByDescending(c => c.TotalRefundAmount)
                    .Take(10)
                    .ToList()
            };
        }

        public async Task<byte[]> ExportRefundReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string format = "pdf")
        {
            var analytics = await GenerateRefundAnalyticsAsync(tenantId, startDate, endDate);
            var refundRequests = await GetRefundHistoryAsync(tenantId, null, startDate, endDate);

            return format.ToLower() switch
            {
                "pdf" => GeneratePdfReport(analytics, refundRequests),
                "excel" => GenerateExcelReport(analytics, refundRequests),
                "csv" => GenerateCsvReport(refundRequests),
                _ => GeneratePdfReport(analytics, refundRequests)
            };
        }

        private async Task<string> GenerateRefundReferenceAsync(Guid tenantId)
        {
            var prefix = "REF";
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var sequence = await GetNextRefundSequenceAsync(tenantId, date);
            return $"{prefix}-{date}-{sequence:D4}";
        }

        private async Task<int> GetNextRefundSequenceAsync(Guid tenantId, string date)
        {
            var key = $"refund_seq_{tenantId}_{date}";
            // In a real implementation, you'd use a proper sequence generator
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _context.RefundRequests
                .CountAsync(rr => rr.TenantId == tenantId && rr.ReferenceNumber.Contains(date));
            return count + 1;
        }

        private async Task<RefundResult> ProcessRefundAsync(RefundRequestEntity refundRequest)
        {
            try
            {
                // Call payment service to process refund
                var refundData = new RefundRequest
                {
                    TenantId = refundRequest.TenantId,
                    PaymentId = refundRequest.PaymentId,
                    Amount = refundRequest.Amount,
                    Reason = refundRequest.Reason,
                    RequestedBy = refundRequest.ApprovedBy.Value
                };

                var result = await _paymentService.ProcessRefundAsync(refundData);
                
                return new RefundResult
                {
                    Success = result.Success,
                    RefundId = result.RefundId,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                return new RefundResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task SendApprovalNotificationAsync(RefundRequestEntity refundRequest)
        {
            // Send notification to approvers
            await _notificationService.SendNotificationAsync(new NotificationMessage
            {
                TenantId = refundRequest.TenantId,
                Type = "RefundApprovalRequired",
                Title = "Refund Approval Required",
                Message = $"A refund request of {refundRequest.Amount:C} requires your approval",
                Data = new { refundRequestId = refundRequest.Id }
            });
        }

        private async Task SendRefundStatusNotificationAsync(RefundRequestEntity refundRequest)
        {
            // Send notification to customer
            var status = refundRequest.Status == "completed" ? "approved" : "processed";
            await _notificationService.SendNotificationAsync(new NotificationMessage
            {
                TenantId = refundRequest.TenantId,
                UserId = refundRequest.CustomerId,
                Type = "RefundStatusUpdate",
                Title = $"Refund {status}",
                Message = $"Your refund request of {refundRequest.Amount:C} has been {(refundRequest.Status == "completed" ? "approved" : "processed")}",
                Data = new { refundRequestId = refundRequest.Id, status = refundRequest.Status }
            });
        }

        private byte[] GeneratePdfReport(RefundAnalytics analytics, List<RefundRequest> requests)
        {
            // PDF generation implementation
            var content = $"Refund Report - {analytics.StartDate:dd MMM yyyy} to {analytics.EndDate:dd MMM yyyy}\n\n";
            content += $"Total Requests: {analytics.TotalRequests}\n";
            content += $"Approved: {analytics.ApprovedRequests}\n";
            content += $"Rejected: {analytics.RejectedRequests}\n";
            content += $"Total Refund Amount: {analytics.TotalRefundAmount:C}\n";
            
            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        private byte[] GenerateExcelReport(RefundAnalytics analytics, List<RefundRequest> requests)
        {
            // Excel generation implementation
            var csv = "Date,Reference,Customer,Amount,Status,Reason\n";
            foreach (var request in requests)
            {
                csv += $"{request.RequestedAt:yyyy-MM-dd},{request.ReferenceNumber},{request.Customer?.FullName},{request.Amount},{request.Status},{request.Reason}\n";
            }
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        private byte[] GenerateCsvReport(List<RefundRequest> requests)
        {
            var csv = "Date,Reference,Customer,Amount,Status,Reason,ApprovedBy,ApprovedAt\n";
            foreach (var request in requests)
            {
                csv += $"{request.RequestedAt:yyyy-MM-dd},{request.ReferenceNumber},{request.Customer?.FullName},{request.Amount},{request.Status},{request.Reason},{request.ApprovedBy},{request.ApprovedAt:yyyy-MM-dd HH:mm:ss}\n";
            }
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }
    }

    // Supporting DTOs and Entities
    public class RefundRequest
    {
        public Guid TenantId { get; set; }
        public Guid PaymentId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public Guid RequestedBy { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }

    public class RefundRequestResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? RefundRequestId { get; set; }
        public string? ReferenceNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
        public string? EstimatedProcessingTime { get; set; }
    }

    public class RefundApprovalResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid RefundRequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? RefundId { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class RefundPolicy
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string ProductCategory { get; set; } = "default";
        public bool RequiresApproval { get; set; }
        public bool AutoApprove { get; set; }
        public decimal AutoApproveMaxAmount { get; set; }
        public decimal MaxRefundAmount { get; set; }
        public int RefundWindowDays { get; set; }
        public string EstimatedProcessingTime { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class RefundAnalytics
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int PendingRequests { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public decimal AverageRefundAmount { get; set; }
        public List<RefundReasonAnalytics> RefundsByReason { get; set; } = new();
        public List<RefundCategoryAnalytics> RefundsByProductCategory { get; set; } = new();
        public double ProcessingTime { get; set; }
        public List<CustomerRefundAnalytics> TopRefundingCustomers { get; set; } = new();
    }

    public class RefundReasonAnalytics
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class RefundCategoryAnalytics
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class CustomerRefundAnalytics
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int RefundCount { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public decimal AverageRefundAmount { get; set; }
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public Guid? RefundId { get; set; }
        public string? Error { get; set; }
    }

    // Entity classes
    public class RefundRequestEntity : TenantEntity
    {
        public Guid PaymentId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, approved, rejected, completed, approval_failed
        public string ReferenceNumber { get; set; } = string.Empty;
        public Guid RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
        public Guid? RefundId { get; set; }
        public DateTime? RefundProcessedAt { get; set; }
        public string? FailureReason { get; set; }
        public bool RequiresApproval { get; set; }
        public bool AutoApprove { get; set; }
        public decimal MaxRefundAmount { get; set; }
        public int RefundWindowDays { get; set; }

        public virtual Payment Payment { get; set; } = null!;
        public virtual Customer Customer { get; set; } = null!;
    }

    public class RefundPolicyEntity : TenantEntity
    {
        public string ProductCategory { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
        public bool AutoApprove { get; set; }
        public decimal AutoApproveMaxAmount { get; set; }
        public decimal MaxRefundAmount { get; set; }
        public int RefundWindowDays { get; set; }
        public string EstimatedProcessingTime { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class NotificationMessage
    {
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object Data { get; set; } = new();
    }
}
