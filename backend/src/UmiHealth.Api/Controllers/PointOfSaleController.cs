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
    public class PointOfSaleController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PointOfSaleController> _logger;

        public PointOfSaleController(SharedDbContext context, ILogger<PointOfSaleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] bool? inStock = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId))
                {
                    return Unauthorized("Tenant or branch not found");
                }

                var query = _context.Products.Where(p => p.DeletedAt == null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => 
                        p.Name.Contains(search) || 
                        p.Sku.Contains(search) ||
                        (p.GenericName != null && p.GenericName.Contains(search)) ||
                        (p.Manufacturer != null && p.Manufacturer.Contains(search)));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get inventory information for each product
                var productIds = products.Select(p => p.Id).ToList();
                var inventories = await _context.Inventories
                    .Where(i => productIds.Contains(i.ProductId) && i.BranchId == Guid.Parse(branchId) && i.DeletedAt == null)
                    .ToDictionaryAsync(i => i.ProductId, i => i);

                var productWithInventory = products.Select(p => new
                {
                    p.Id,
                    p.Sku,
                    p.Name,
                    p.GenericName,
                    p.Category,
                    p.Description,
                    p.Manufacturer,
                    p.Strength,
                    p.Form,
                    p.RequiresPrescription,
                    p.ControlledSubstance,
                    p.Barcode,
                    p.Status,
                    Inventory = inventories.ContainsKey(p.Id) ? inventories[p.Id] : null,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList();

                if (inStock.HasValue)
                {
                    productWithInventory = productWithInventory
                        .Where(p => p.Inventory != null && (inStock.Value ? p.Inventory.QuantityOnHand > 0 : p.Inventory.QuantityOnHand <= 0))
                        .ToList();
                }

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    products = productWithInventory,
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
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("products/{id}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId))
                {
                    return Unauthorized("Tenant or branch not found");
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

                if (product == null)
                {
                    return NotFound();
                }

                // Get inventory information
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == id && i.BranchId == Guid.Parse(branchId) && i.DeletedAt == null);

                return Ok(new
                {
                    product,
                    inventory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("inventory")]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetInventory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] bool? lowStock = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId))
                {
                    return Unauthorized("Tenant or branch not found");
                }

                var query = _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.BranchId == Guid.Parse(branchId) && i.DeletedAt == null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(i => 
                        i.Product.Name.Contains(search) || 
                        i.Product.Sku.Contains(search) ||
                        i.BatchNumber.Contains(search));
                }

                if (lowStock.HasValue && lowStock.Value)
                {
                    query = query.Where(i => i.QuantityOnHand <= i.ReorderLevel);
                }

                var inventory = await query
                    .OrderBy(i => i.Product.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    inventory,
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
                _logger.LogError(ex, "Error retrieving inventory");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("checkout")]
        public async Task<ActionResult<CheckoutResult>> Checkout(CheckoutRequest request)
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
                    // Validate inventory availability
                    foreach (var item in request.Items)
                    {
                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == Guid.Parse(branchId) && i.DeletedAt == null);
                        
                        if (inventory == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Product {item.ProductId} not found in inventory");
                        }

                        if (inventory.QuantityOnHand < item.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Insufficient stock for product {inventory.Product.Name}. Available: {inventory.QuantityOnHand}, Requested: {item.Quantity}");
                        }
                    }

                    // Create sale
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
                        Items = request.Items.Select(item => new SaleItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            DiscountAmount = item.DiscountAmount,
                            TotalPrice = item.TotalPrice,
                            CreatedAt = DateTime.UtcNow
                        }).ToList(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Sales.Add(sale);

                    // Update inventory
                    foreach (var item in request.Items)
                    {
                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == Guid.Parse(branchId) && i.DeletedAt == null);
                        
                        inventory.QuantityOnHand -= item.Quantity;
                        inventory.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    // Create payment if payment info is provided
                    Payment? payment = null;
                    if (!string.IsNullOrEmpty(request.PaymentMethod) && request.AmountPaid > 0)
                    {
                        payment = new Payment
                        {
                            Id = Guid.NewGuid(),
                            BranchId = Guid.Parse(branchId),
                            PaymentNumber = await GeneratePaymentNumberAsync(),
                            SaleId = sale.Id,
                            Amount = request.AmountPaid,
                            PaymentMethod = request.PaymentMethod,
                            PaymentDate = DateTime.UtcNow,
                            Status = "pending",
                            ReferenceNumber = request.ReferenceNumber,
                            Notes = "Created during checkout",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Payments.Add(payment);
                        await _context.SaveChangesAsync();

                        // Process payment
                        var processingResult = await ProcessPaymentMethodAsync(payment);
                        payment.Status = processingResult.Success ? "completed" : "failed";
                        payment.Notes = processingResult.Message;
                        payment.UpdatedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        // Update sale payment status
                        if (processingResult.Success && request.AmountPaid >= request.TotalAmount)
                        {
                            sale.PaymentStatus = "paid";
                            sale.Status = "completed";
                        }
                        else if (processingResult.Success && request.AmountPaid > 0)
                        {
                            sale.PaymentStatus = "partial";
                        }
                        
                        sale.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    return Ok(new CheckoutResult
                    {
                        Sale = sale,
                        Payment = payment,
                        Success = true,
                        Message = "Checkout completed successfully"
                    });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var categories = await _context.Products
                    .Where(p => p.DeletedAt == null && p.Category != null)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<PosStats>> GetPosStats()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var branchId = User.FindFirst("BranchId")?.Value;
                
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId))
                {
                    return Unauthorized("Tenant or branch not found");
                }

                var totalProducts = await _context.Products.CountAsync(p => p.DeletedAt == null);
                var inStockProducts = await _context.Inventories
                    .CountAsync(i => i.BranchId == Guid.Parse(branchId) && i.QuantityOnHand > 0 && i.DeletedAt == null);
                var lowStockProducts = await _context.Inventories
                    .CountAsync(i => i.BranchId == Guid.Parse(branchId) && i.QuantityOnHand <= i.ReorderLevel && i.DeletedAt == null);
                
                var todaySales = await _context.Sales
                    .Where(s => s.BranchId == Guid.Parse(branchId) && s.SaleDate.Date == DateTime.UtcNow.Date && s.DeletedAt == null)
                    .CountAsync();
                
                var todayRevenue = await _context.Sales
                    .Where(s => s.BranchId == Guid.Parse(branchId) && s.SaleDate.Date == DateTime.UtcNow.Date && s.DeletedAt == null)
                    .SumAsync(s => s.TotalAmount);

                return Ok(new PosStats
                {
                    TotalProducts = totalProducts,
                    InStockProducts = inStockProducts,
                    LowStockProducts = lowStockProducts,
                    TodaySales = todaySales,
                    TodayRevenue = todayRevenue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving POS stats");
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

        private async Task<string> GeneratePaymentNumberAsync()
        {
            var prefix = "PAY";
            var year = DateTime.UtcNow.Year.ToString();
            var random = new Random();
            
            for (int i = 0; i < 10; i++)
            {
                var suffix = random.Next(1000, 9999).ToString();
                var paymentNumber = $"{prefix}{year}{suffix}";
                
                var exists = await _context.Payments.AnyAsync(p => p.PaymentNumber == paymentNumber);
                if (!exists)
                {
                    return paymentNumber;
                }
            }

            throw new Exception("Unable to generate unique payment number");
        }

        private async Task<PaymentProcessingResult> ProcessPaymentMethodAsync(Payment payment)
        {
            // Simulate different payment processing methods
            await Task.Delay(500); // Simulate processing time

            switch (payment.PaymentMethod.ToLower())
            {
                case "cash":
                    return new PaymentProcessingResult { Success = true, Message = "Cash payment processed successfully" };
                
                case "card":
                    return new PaymentProcessingResult { Success = true, Message = "Card payment processed successfully" };
                
                case "mobile":
                    return new PaymentProcessingResult { Success = true, Message = "Mobile payment processed successfully" };
                
                default:
                    return new PaymentProcessingResult { Success = false, Message = "Unsupported payment method" };
            }
        }
    }

    public class CheckoutRequest
    {
        public Guid? PatientId { get; set; }
        public List<CheckoutItemRequest> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class CheckoutItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CheckoutResult
    {
        public Sale Sale { get; set; } = null!;
        public Payment? Payment { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PosStats
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TodaySales { get; set; }
        public decimal TodayRevenue { get; set; }
    }

    public class PaymentProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
