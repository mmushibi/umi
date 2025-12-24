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
    public class PaymentsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(SharedDbContext context, ILogger<PaymentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? status = null,
            [FromQuery] string? paymentMethod = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var query = _context.Payments.Where(p => p.DeletedAt == null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => 
                        p.PaymentNumber.Contains(search) ||
                        p.ReferenceNumber.Contains(search));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query = query.Where(p => p.PaymentMethod == paymentMethod);
                }

                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                return Ok(new
                {
                    payments,
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
                _logger.LogError(ex, "Error retrieving payments");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(Guid id)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

                if (payment == null)
                {
                    return NotFound();
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment {PaymentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> CreatePayment(CreatePaymentRequest request)
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

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    BranchId = Guid.Parse(branchId),
                    PaymentNumber = await GeneratePaymentNumberAsync(),
                    SaleId = request.SaleId,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Status = "pending",
                    ReferenceNumber = request.ReferenceNumber,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Update sale payment status if this is the full payment
                var sale = await _context.Sales.FindAsync(payment.SaleId);
                if (sale != null)
                {
                    var totalPaid = await _context.Payments
                        .Where(p => p.SaleId == sale.Id && p.Status == "completed" && p.DeletedAt == null)
                        .SumAsync(p => p.Amount);

                    if (totalPaid + payment.Amount >= sale.TotalAmount)
                    {
                        sale.PaymentStatus = "paid";
                        sale.Status = "completed";
                    }
                    else
                    {
                        sale.PaymentStatus = "partial";
                    }
                    
                    sale.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(Guid id, UpdatePaymentRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

                if (payment == null)
                {
                    return NotFound();
                }

                var oldStatus = payment.Status;
                payment.Status = request.Status ?? payment.Status;
                payment.ReferenceNumber = request.ReferenceNumber ?? payment.ReferenceNumber;
                payment.Notes = request.Notes ?? payment.Notes;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update sale payment status if payment status changed
                if (oldStatus != payment.Status && payment.SaleId.HasValue)
                {
                    var sale = await _context.Sales.FindAsync(payment.SaleId);
                    if (sale != null)
                    {
                        var totalPaid = await _context.Payments
                            .Where(p => p.SaleId == sale.Id && p.Status == "completed" && p.DeletedAt == null)
                            .SumAsync(p => p.Amount);

                        if (totalPaid >= sale.TotalAmount)
                        {
                            sale.PaymentStatus = "paid";
                            sale.Status = "completed";
                        }
                        else if (totalPaid > 0)
                        {
                            sale.PaymentStatus = "partial";
                        }
                        else
                        {
                            sale.PaymentStatus = "pending";
                        }
                        
                        sale.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment {PaymentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/process")]
        public async Task<IActionResult> ProcessPayment(Guid id)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

                if (payment == null)
                {
                    return NotFound();
                }

                if (payment.Status != "pending")
                {
                    return BadRequest("Payment can only be processed if status is pending");
                }

                // Simulate payment processing based on payment method
                var processingResult = await ProcessPaymentMethodAsync(payment);

                payment.Status = processingResult.Success ? "completed" : "failed";
                payment.Notes = processingResult.Message;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update sale payment status
                if (payment.SaleId.HasValue)
                {
                    var sale = await _context.Sales.FindAsync(payment.SaleId);
                    if (sale != null)
                    {
                        var totalPaid = await _context.Payments
                            .Where(p => p.SaleId == sale.Id && p.Status == "completed" && p.DeletedAt == null)
                            .SumAsync(p => p.Amount);

                        if (totalPaid >= sale.TotalAmount)
                        {
                            sale.PaymentStatus = "paid";
                            sale.Status = "completed";
                        }
                        else if (totalPaid > 0)
                        {
                            sale.PaymentStatus = "partial";
                        }
                        
                        sale.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { status = payment.Status, message = processingResult.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment {PaymentId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<PaymentStats>> GetPaymentStats(
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

                var query = _context.Payments.Where(p => p.DeletedAt == null);

                if (startDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(p => p.PaymentDate <= endDate.Value);
                }

                var totalPayments = await query.CountAsync();
                var totalAmount = await query.Where(p => p.Status == "completed").SumAsync(p => p.Amount);
                var completedPayments = await query.CountAsync(p => p.Status == "completed");
                var pendingPayments = await query.CountAsync(p => p.Status == "pending");
                var failedPayments = await query.CountAsync(p => p.Status == "failed");
                
                var todayPayments = await query
                    .Where(p => p.PaymentDate.Date == DateTime.UtcNow.Date)
                    .CountAsync();
                
                var todayAmount = await query
                    .Where(p => p.PaymentDate.Date == DateTime.UtcNow.Date && p.Status == "completed")
                    .SumAsync(p => p.Amount);

                return Ok(new PaymentStats
                {
                    TotalPayments = totalPayments,
                    TotalAmount = totalAmount,
                    CompletedPayments = completedPayments,
                    PendingPayments = pendingPayments,
                    FailedPayments = failedPayments,
                    TodayPayments = todayPayments,
                    TodayAmount = todayAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment stats");
                return StatusCode(500, "Internal server error");
            }
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
            await Task.Delay(1000); // Simulate processing time

            switch (payment.PaymentMethod.ToLower())
            {
                case "cash":
                    return new PaymentProcessingResult { Success = true, Message = "Cash payment processed successfully" };
                
                case "card":
                    // Simulate card processing
                    return new PaymentProcessingResult { Success = true, Message = "Card payment processed successfully" };
                
                case "mobile":
                    // Simulate mobile payment processing
                    return new PaymentProcessingResult { Success = true, Message = "Mobile payment processed successfully" };
                
                case "insurance":
                    // Simulate insurance claim processing
                    return new PaymentProcessingResult { Success = true, Message = "Insurance claim processed successfully" };
                
                default:
                    return new PaymentProcessingResult { Success = false, Message = "Unsupported payment method" };
            }
        }
    }

    public class CreatePaymentRequest
    {
        public Guid SaleId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdatePaymentRequest
    {
        public string? Status { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class PaymentStats
    {
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public int CompletedPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
        public int TodayPayments { get; set; }
        public decimal TodayAmount { get; set; }
    }

    public class PaymentProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
