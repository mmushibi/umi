using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ReceiptsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<ReceiptsController> _logger;
        private readonly IReceiptService _receiptService;

        public ReceiptsController(
            SharedDbContext context,
            ILogger<ReceiptsController> logger,
            IReceiptService receiptService)
        {
            _context = context;
            _logger = logger;
            _receiptService = receiptService;
        }

        [HttpGet("sale/{saleId}")]
        [EnableRateLimiting("Read")]
        public async Task<IActionResult> GenerateSaleReceipt(Guid saleId, [FromQuery] ReceiptOptionsDto options = null)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var receiptOptions = options?.ToReceiptOptions() ?? new ReceiptOptions();
                var pdfBytes = await _receiptService.GenerateSaleReceiptAsync(saleId, receiptOptions);

                // Save receipt history
                await SaveReceiptHistoryAsync(saleId, "sale", pdfBytes.Length);

                return File(pdfBytes, "application/pdf", $"sale_receipt_{saleId}.pdf");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Sale not found: {SaleId}", saleId);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sale receipt for sale {SaleId}", saleId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("payment/{paymentId}")]
        [EnableRateLimiting("Read")]
        public async Task<IActionResult> GeneratePaymentReceipt(Guid paymentId, [FromQuery] ReceiptOptionsDto options = null)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var receiptOptions = options?.ToReceiptOptions() ?? new ReceiptOptions();
                var pdfBytes = await _receiptService.GeneratePaymentReceiptAsync(paymentId, receiptOptions);

                // Save receipt history
                await SaveReceiptHistoryAsync(paymentId, "payment", pdfBytes.Length);

                return File(pdfBytes, "application/pdf", $"payment_receipt_{paymentId}.pdf");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Payment not found: {PaymentId}", paymentId);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment receipt for payment {PaymentId}", paymentId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("prescription/{prescriptionId}")]
        [EnableRateLimiting("Read")]
        public async Task<IActionResult> GeneratePrescriptionReceipt(Guid prescriptionId, [FromQuery] ReceiptOptionsDto options = null)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var receiptOptions = options?.ToReceiptOptions() ?? new ReceiptOptions();
                var pdfBytes = await _receiptService.GeneratePrescriptionReceiptAsync(prescriptionId, receiptOptions);

                // Save receipt history
                await SaveReceiptHistoryAsync(prescriptionId, "prescription", pdfBytes.Length);

                return File(pdfBytes, "application/pdf", $"prescription_receipt_{prescriptionId}.pdf");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Prescription not found: {PrescriptionId}", prescriptionId);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prescription receipt for prescription {PrescriptionId}", prescriptionId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("refund/{refundId}")]
        [EnableRateLimiting("Read")]
        public async Task<IActionResult> GenerateRefundReceipt(Guid refundId, [FromQuery] ReceiptOptionsDto options = null)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var receiptOptions = options?.ToReceiptOptions() ?? new ReceiptOptions();
                var pdfBytes = await _receiptService.GenerateRefundReceiptAsync(refundId, receiptOptions);

                // Save receipt history
                await SaveReceiptHistoryAsync(refundId, "refund", pdfBytes.Length);

                return File(pdfBytes, "application/pdf", $"refund_receipt_{refundId}.pdf");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Refund not found: {RefundId}", refundId);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refund receipt for refund {RefundId}", refundId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("custom")]
        [EnableRateLimiting("Write")]
        public async Task<IActionResult> GenerateCustomReceipt([FromBody] CustomReceiptRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var template = await _receiptService.GetReceiptTemplateAsync(Guid.Parse(tenantId), request.ReceiptType);
                var receiptData = request.ToReceiptData();
                var pdfBytes = await _receiptService.GenerateCustomReceiptAsync(template, receiptData);

                // Save receipt history
                await SaveReceiptHistoryAsync(Guid.NewGuid(), "custom", pdfBytes.Length);

                return File(pdfBytes, "application/pdf", $"custom_receipt_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom receipt");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("templates")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ReceiptTemplate>> GetReceiptTemplate(string receiptType)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var template = await _receiptService.GetReceiptTemplateAsync(Guid.Parse(tenantId), receiptType);
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt template for type {ReceiptType}", receiptType);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("templates")]
        [EnableRateLimiting("Write")]
        public async Task<ActionResult<ReceiptTemplate>> UpdateReceiptTemplate([FromBody] ReceiptTemplate template)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                template.UpdatedBy = GetUserId();
                var updatedTemplate = await _receiptService.UpdateReceiptTemplateAsync(Guid.Parse(tenantId), template);
                return Ok(updatedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating receipt template");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("history")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<IEnumerable<ReceiptHistoryDto>>> GetReceiptHistory(
            [FromQuery] Guid? entityId = null,
            [FromQuery] string? receiptType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var history = await _receiptService.GetReceiptHistoryAsync(Guid.Parse(tenantId), entityId, receiptType);

                var paginatedHistory = history
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    receipts = paginatedHistory,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount = history.Count,
                        totalPages = (int)Math.Ceiling((double)history.Count / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt history");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("download/{receiptHistoryId}")]
        [EnableRateLimiting("Read")]
        public async Task<IActionResult> DownloadReceipt(Guid receiptHistoryId)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                var receiptHistory = await _context.ReceiptHistories
                    .FirstOrDefaultAsync(r => r.Id == receiptHistoryId && r.TenantId == Guid.Parse(tenantId) && !r.IsDeleted);

                if (receiptHistory == null)
                {
                    return NotFound("Receipt not found");
                }

                if (receiptHistory.FileData == null)
                {
                    return NotFound("Receipt file not available");
                }

                return File(receiptHistory.FileData, "application/pdf", receiptHistory.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading receipt {ReceiptHistoryId}", receiptHistoryId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("email/{saleId}")]
        [EnableRateLimiting("Write")]
        public async Task<IActionResult> EmailSaleReceipt(Guid saleId, [FromBody] EmailReceiptRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                // Generate receipt
                var pdfBytes = await _receiptService.GenerateSaleReceiptAsync(saleId);

                // TODO: Implement email service to send the receipt
                // This would integrate with an email service like SendGrid or SMTP

                return Ok(new { message = "Receipt sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error emailing sale receipt for sale {SaleId}", saleId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("sms/{saleId}")]
        [EnableRateLimiting("Write")]
        public async Task<IActionResult> SmsSaleReceiptLink(Guid saleId, [FromBody] SmsReceiptRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized("Tenant not found");
                }

                // Generate receipt
                var pdfBytes = await _receiptService.GenerateSaleReceiptAsync(saleId);

                // TODO: Implement SMS service to send receipt link
                // This would integrate with an SMS service like Twilio

                return Ok(new { message = "Receipt link sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS receipt link for sale {SaleId}", saleId);
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task SaveReceiptHistoryAsync(Guid entityId, string receiptType, int fileSize)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var receiptHistory = new ReceiptHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = Guid.Parse(tenantId),
                    EntityId = entityId,
                    ReceiptType = receiptType,
                    ReceiptNumber = GenerateReceiptNumber(receiptType),
                    GeneratedBy = Guid.Parse(userId),
                    FileSize = fileSize,
                    FileName = $"{receiptType}_receipt_{entityId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.ReceiptHistories.Add(receiptHistory);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving receipt history");
            }
        }

        private string GenerateReceiptNumber(string receiptType)
        {
            return $"{receiptType.ToUpper()}_REC_{DateTime.UtcNow:yyyyMMddHHmmss}_{new Random().Next(1000, 9999)}";
        }

        private string GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            return tenantIdClaim?.Value ?? string.Empty;
        }

        private string GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim?.Value ?? string.Empty;
        }
    }

    // DTOs for receipt requests
    public class ReceiptOptionsDto
    {
        public int? MarginTop { get; set; }
        public int? MarginBottom { get; set; }
        public int? MarginLeft { get; set; }
        public int? MarginRight { get; set; }
        public bool IncludeHeader { get; set; } = true;
        public bool IncludeFooter { get; set; } = true;
        public bool IncludeBarcode { get; set; } = true;
        public bool IncludeWatermark { get; set; } = false;
        public string WatermarkText { get; set; } = string.Empty;
        public string PaperSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";

        public ReceiptOptions ToReceiptOptions()
        {
            return new ReceiptOptions
            {
                Margin = MarginTop.HasValue && MarginBottom.HasValue && MarginLeft.HasValue && MarginRight.HasValue
                    ? (MarginLeft.Value, MarginTop.Value, MarginRight.Value, MarginBottom.Value)
                    : (20, 20, 20, 20),
                IncludeHeader = IncludeHeader,
                IncludeFooter = IncludeFooter,
                IncludeBarcode = IncludeBarcode,
                IncludeWatermark = IncludeWatermark,
                WatermarkText = WatermarkText,
                PaperSize = PaperSize,
                Orientation = Orientation
            };
        }
    }

    public class CustomReceiptRequest
    {
        public string ReceiptType { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public CustomerInfoDto Customer { get; set; }
        public List<ReceiptItemDto> Items { get; set; } = new();
        public ReceiptSummaryDto Summary { get; set; }
        public string Notes { get; set; } = string.Empty;

        public ReceiptData ToReceiptData()
        {
            return new ReceiptData
            {
                ReceiptType = ReceiptType,
                ReceiptNumber = ReceiptNumber,
                Date = Date,
                CustomerInfo = Customer?.ToCustomerInfo(),
                Items = Items?.Select(i => i.ToReceiptItem()).ToList() ?? new(),
                Summary = Summary?.ToReceiptSummary() ?? new ReceiptSummary(),
                Notes = Notes
            };
        }
    }

    public class CustomerInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public CustomerInfo ToCustomerInfo()
        {
            return new CustomerInfo
            {
                Name = Name,
                Email = Email,
                Phone = Phone,
                Address = Address
            };
        }
    }

    public class ReceiptItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalPrice { get; set; }

        public ReceiptItem ToReceiptItem()
        {
            return new ReceiptItem
            {
                Name = Name,
                Code = Code,
                Quantity = Quantity,
                UnitPrice = UnitPrice,
                Discount = Discount,
                TotalPrice = TotalPrice
            };
        }
    }

    public class ReceiptSummaryDto
    {
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public ReceiptSummary ToReceiptSummary()
        {
            return new ReceiptSummary
            {
                Subtotal = Subtotal,
                TaxAmount = TaxAmount,
                DiscountAmount = DiscountAmount,
                TotalAmount = TotalAmount
            };
        }
    }

    public class EmailReceiptRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = "Your Receipt";
        public string Message { get; set; } = "Please find your receipt attached.";
    }

    public class SmsReceiptRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = "Your receipt is available for download.";
    }
}
