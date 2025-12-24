using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TransactionsOperationsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<TransactionsOperationsController> _logger;

        public TransactionsOperationsController(SharedDbContext context, ILogger<TransactionsOperationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? tenantId = null,
            [FromQuery] string? transactionType = null)
        {
            try
            {
                var query = _context.Sales
                    .Include(s => s.Items)
                    .ThenInclude(si => si.Product)
                    .Include(s => s.Payments)
                    .Where(s => s.DeletedAt == null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => 
                        s.SaleNumber.Contains(search) ||
                        (s.PatientId.HasValue && s.PatientId.ToString().Contains(search)));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(tenantId))
                {
                    var tenantGuid = Guid.Parse(tenantId);
                    query = query.Where(s => s.BranchId == tenantGuid);
                }

                if (!string.IsNullOrEmpty(transactionType))
                {
                    // Filter by payment method or other criteria
                    query = query.Where(s => s.PaymentMethod == transactionType);
                }

                var transactions = await query
                    .OrderByDescending(s => s.SaleDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.Id,
                        s.SaleNumber,
                        s.BranchId,
                        s.PatientId,
                        s.CashierId,
                        s.SaleDate,
                        s.Subtotal,
                        s.TaxAmount,
                        s.DiscountAmount,
                        s.TotalAmount,
                        s.PaymentMethod,
                        s.Status,
                        s.PaymentStatus,
                        Items = s.Items.Select(si => new
                        {
                            si.Id,
                            si.ProductId,
                            ProductName = si.Product != null ? si.Product.Name : "Unknown Product",
                            si.Quantity,
                            si.UnitPrice,
                            si.DiscountAmount,
                            si.TotalPrice
                        }).ToList(),
                        Payments = s.Payments.Select(p => new
                        {
                            p.Id,
                            p.PaymentNumber,
                            p.Amount,
                            p.PaymentMethod,
                            p.PaymentDate,
                            p.Status,
                            p.ReferenceNumber
                        }).ToList()
                    })
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    transactions,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTransaction(Guid id)
        {
            try
            {
                var transaction = await _context.Sales
                    .Include(s => s.Items)
                    .ThenInclude(si => si.Product)
                    .Include(s => s.Payments)
                    .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

                if (transaction == null)
                {
                    return NotFound();
                }

                var result = new
                {
                    transaction.Id,
                    transaction.SaleNumber,
                    transaction.BranchId,
                    transaction.PatientId,
                    transaction.CashierId,
                    transaction.SaleDate,
                    transaction.Subtotal,
                    transaction.TaxAmount,
                    transaction.DiscountAmount,
                    transaction.TotalAmount,
                    transaction.PaymentMethod,
                    transaction.Status,
                    transaction.PaymentStatus,
                    transaction.Notes,
                    Items = transaction.Items.Select(si => new
                    {
                        si.Id,
                        si.ProductId,
                        ProductName = si.Product != null ? si.Product.Name : "Unknown Product",
                        ProductSku = si.Product != null ? si.Product.Sku : "",
                        si.Quantity,
                        si.UnitPrice,
                        si.DiscountAmount,
                        si.TotalPrice
                    }).ToList(),
                    Payments = transaction.Payments.Select(p => new
                    {
                        p.Id,
                        p.PaymentNumber,
                        p.Amount,
                        p.PaymentMethod,
                        p.PaymentDate,
                        p.Status,
                        p.ReferenceNumber,
                        p.Notes
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction {TransactionId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<TransactionStats>> GetTransactionStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? tenantId = null)
        {
            try
            {
                var query = _context.Sales.Where(s => s.DeletedAt == null);

                if (startDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(tenantId))
                {
                    var tenantGuid = Guid.Parse(tenantId);
                    query = query.Where(s => s.BranchId == tenantGuid);
                }

                var totalTransactions = await query.CountAsync();
                var totalRevenue = await query.SumAsync(s => s.TotalAmount);
                var completedTransactions = await query.CountAsync(s => s.Status == "completed");
                var pendingTransactions = await query.CountAsync(s => s.Status == "pending");
                var cancelledTransactions = await query.CountAsync(s => s.Status == "cancelled");
                
                var todayTransactions = await query
                    .Where(s => s.SaleDate.Date == DateTime.UtcNow.Date)
                    .CountAsync();
                
                var todayRevenue = await query
                    .Where(s => s.SaleDate.Date == DateTime.UtcNow.Date)
                    .SumAsync(s => s.TotalAmount);

                var paymentMethodStats = await query
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new { Method = g.Key, Count = g.Count(), Total = g.Sum(s => s.TotalAmount) })
                    .ToListAsync();

                var dailyStats = await query
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(s => s.TotalAmount) })
                    .OrderByDescending(g => g.Date)
                    .Take(30)
                    .ToListAsync();

                return Ok(new TransactionStats
                {
                    TotalTransactions = totalTransactions,
                    TotalRevenue = totalRevenue,
                    CompletedTransactions = completedTransactions,
                    PendingTransactions = pendingTransactions,
                    CancelledTransactions = cancelledTransactions,
                    TodayTransactions = todayTransactions,
                    TodayRevenue = todayRevenue,
                    PaymentMethodStats = paymentMethodStats,
                    DailyStats = dailyStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction stats");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class TransactionStats
    {
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CompletedTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int CancelledTransactions { get; set; }
        public int TodayTransactions { get; set; }
        public decimal TodayRevenue { get; set; }
        public List<object> PaymentMethodStats { get; set; } = new();
        public List<object> DailyStats { get; set; } = new();
    }
}
