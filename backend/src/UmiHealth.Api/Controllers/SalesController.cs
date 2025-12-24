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
    public class SalesController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<SalesController> _logger;

        public SalesController(SharedDbContext context, ILogger<SalesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSales(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var query = _context.Sales.Where(s => s.DeletedAt == null);

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

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(s => s.Status == status);
                }

                var sales = await query
                    .Include(s => s.Items)
                    .OrderByDescending(s => s.SaleDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    sales,
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
                _logger.LogError(ex, "Error retrieving sales");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetSale(Guid id)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var sale = await _context.Sales
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

                if (sale == null)
                {
                    return NotFound();
                }

                return Ok(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sale {SaleId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Sale>> CreateSale(CreateSaleRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User, tenant, or branch not found");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var sale = new Sale
                    {
                        Id = Guid.NewGuid(),
                        BranchId = Guid.Parse(branchId),
                        SaleNumber = await GenerateSaleNumberAsync(),
                        PatientId = request.PatientId,
                        CashierId = Guid.Parse(userId),
                        SaleDate = DateTime.UtcNow,
                        Subtotal = request.Subtotal,
                        TaxAmount = request.TaxAmount,
                        DiscountAmount = request.DiscountAmount,
                        TotalAmount = request.TotalAmount,
                        PaymentMethod = request.PaymentMethod,
                        PaymentStatus = "pending",
                        Status = "pending",
                        Notes = request.Notes,
                        Items = request.Items?.Select(item => new SaleItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            DiscountAmount = item.DiscountAmount,
                            TotalPrice = item.TotalPrice,
                            CreatedAt = DateTime.UtcNow
                        }).ToList() ?? new List<SaleItem>(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Update inventory
                    foreach (var item in sale.Items)
                    {
                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == sale.BranchId && i.DeletedAt == null);
                        
                        if (inventory != null)
                        {
                            if (inventory.QuantityOnHand < item.Quantity)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest($"Insufficient inventory for product {item.ProductId}");
                            }
                            
                            inventory.QuantityOnHand -= item.Quantity;
                            inventory.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Inventory not found for product {item.ProductId}");
                        }
                    }

                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sale");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSale(Guid id, UpdateSaleRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var sale = await _context.Sales
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

                if (sale == null)
                {
                    return NotFound();
                }

                sale.Status = request.Status ?? sale.Status;
                sale.PaymentStatus = request.PaymentStatus ?? sale.PaymentStatus;
                sale.Notes = request.Notes ?? sale.Notes;
                sale.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sale {SaleId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<SalesStats>> GetSalesStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var query = _context.Sales.Where(s => s.DeletedAt == null);

                if (startDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.SaleDate <= endDate.Value);
                }

                var totalSales = await query.CountAsync();
                var totalRevenue = await query.SumAsync(s => s.TotalAmount);
                var completedSales = await query.CountAsync(s => s.Status == "completed");
                var pendingSales = await query.CountAsync(s => s.Status == "pending");
                
                var todaySales = await query
                    .Where(s => s.SaleDate.Date == DateTime.UtcNow.Date)
                    .CountAsync();
                
                var todayRevenue = await query
                    .Where(s => s.SaleDate.Date == DateTime.UtcNow.Date)
                    .SumAsync(s => s.TotalAmount);

                return Ok(new SalesStats
                {
                    TotalSales = totalSales,
                    TotalRevenue = totalRevenue,
                    CompletedSales = completedSales,
                    PendingSales = pendingSales,
                    TodaySales = todaySales,
                    TodayRevenue = todayRevenue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales stats");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<string> GenerateSaleNumberAsync()
        {
            var prefix = "SALE";
            var year = DateTime.UtcNow.Year.ToString();
            var random = new Random();
            
            for (int i = 0; i < 10; i++)
            {
                var suffix = random.Next(1000, 9999).ToString();
                var saleNumber = $"{prefix}{year}{suffix}";
                
                var exists = await _context.Sales.AnyAsync(s => s.SaleNumber == saleNumber);
                if (!exists)
                {
                    return saleNumber;
                }
            }

            throw new Exception("Unable to generate unique sale number");
        }
    }

    public class CreateSaleRequest
    {
        public Guid? PatientId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<CreateSaleItemRequest>? Items { get; set; }
    }

    public class CreateSaleItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class UpdateSaleRequest
    {
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Notes { get; set; }
    }

    public class SalesStats
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CompletedSales { get; set; }
        public int PendingSales { get; set; }
        public int TodaySales { get; set; }
        public decimal TodayRevenue { get; set; }
    }
}
