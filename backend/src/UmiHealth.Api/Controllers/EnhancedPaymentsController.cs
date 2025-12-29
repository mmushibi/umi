using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnhancedPaymentsController : ControllerBase
    {
        private readonly IEnhancedPaymentService _enhancedPaymentService;
        private readonly ILogger<EnhancedPaymentsController> _logger;

        public EnhancedPaymentsController(
            IEnhancedPaymentService enhancedPaymentService,
            ILogger<EnhancedPaymentsController> logger)
        {
            _enhancedPaymentService = enhancedPaymentService;
            _logger = logger;
        }

        #region Bulk Payment Processing

        [HttpPost("bulk-process")]
        public async Task<ActionResult<BulkPaymentResult>> ProcessBulkPayments([FromBody] BulkPaymentRequest request)
        {
            try
            {
                var result = await _enhancedPaymentService.ProcessBulkPaymentsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk payments");
                return StatusCode(500, new { message = "Error processing bulk payments", error = ex.Message });
            }
        }

        [HttpGet("bulk-template")]
        public async Task<IActionResult> DownloadBulkPaymentTemplate()
        {
            try
            {
                var templateBytes = await _enhancedPaymentService.GenerateBulkPaymentTemplateAsync();
                return File(templateBytes, "text/csv", "bulk_payment_template.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating bulk payment template");
                return StatusCode(500, new { message = "Error generating template", error = ex.Message });
            }
        }

        [HttpPost("bulk-validate")]
        public async Task<ActionResult<List<BulkPaymentValidation>>> ValidateBulkPaymentFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                using var stream = file.OpenReadStream();
                var validations = await _enhancedPaymentService.ValidateBulkPaymentFileAsync(stream);
                return Ok(validations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating bulk payment file");
                return StatusCode(500, new { message = "Error validating file", error = ex.Message });
            }
        }

        #endregion

        #region Recurring Payments

        [HttpPost("recurring")]
        public async Task<ActionResult<RecurringPaymentResult>> CreateRecurringPayment([FromBody] RecurringPaymentRequest request)
        {
            try
            {
                var result = await _enhancedPaymentService.CreateRecurringPaymentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recurring payment");
                return StatusCode(500, new { message = "Error creating recurring payment", error = ex.Message });
            }
        }

        [HttpGet("recurring/{tenantId}")]
        public async Task<ActionResult<List<RecurringPaymentDto>>> GetRecurringPayments(Guid tenantId)
        {
            try
            {
                var payments = await _enhancedPaymentService.GetRecurringPaymentsAsync(tenantId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recurring payments for tenant {tenantId}");
                return StatusCode(500, new { message = "Error retrieving recurring payments", error = ex.Message });
            }
        }

        [HttpPut("recurring/{recurringPaymentId}/cancel")]
        public async Task<ActionResult> CancelRecurringPayment(Guid recurringPaymentId)
        {
            try
            {
                var result = await _enhancedPaymentService.CancelRecurringPaymentAsync(recurringPaymentId);
                if (!result)
                    return NotFound(new { message = "Recurring payment not found" });

                return Ok(new { message = "Recurring payment cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling recurring payment {recurringPaymentId}");
                return StatusCode(500, new { message = "Error cancelling recurring payment", error = ex.Message });
            }
        }

        [HttpPost("recurring/process-scheduled")]
        public async Task<ActionResult> ProcessScheduledRecurringPayments()
        {
            try
            {
                await _enhancedPaymentService.ProcessScheduledRecurringPaymentsAsync();
                return Ok(new { message = "Scheduled recurring payments processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled recurring payments");
                return StatusCode(500, new { message = "Error processing scheduled payments", error = ex.Message });
            }
        }

        #endregion

        #region Multi-Currency Support

        [HttpPost("currency/convert")]
        public async Task<ActionResult<CurrencyConversionResult>> ConvertCurrency([FromBody] CurrencyConversionRequest request)
        {
            try
            {
                var result = await _enhancedPaymentService.ConvertCurrencyAsync(
                    request.Amount, 
                    request.FromCurrency, 
                    request.ToCurrency);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting currency");
                return StatusCode(500, new { message = "Error converting currency", error = ex.Message });
            }
        }

        [HttpGet("currency/exchange-rates")]
        public async Task<ActionResult<List<ExchangeRateDto>>> GetExchangeRates()
        {
            try
            {
                var rates = await _enhancedPaymentService.GetExchangeRatesAsync();
                return Ok(rates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rates");
                return StatusCode(500, new { message = "Error retrieving exchange rates", error = ex.Message });
            }
        }

        [HttpPost("currency/exchange-rates/update")]
        public async Task<ActionResult> UpdateExchangeRates()
        {
            try
            {
                await _enhancedPaymentService.UpdateExchangeRatesAsync();
                return Ok(new { message = "Exchange rates updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exchange rates");
                return StatusCode(500, new { message = "Error updating exchange rates", error = ex.Message });
            }
        }

        #endregion

        #region Payment Plans

        [HttpPost("payment-plans")]
        public async Task<ActionResult<PaymentPlanResult>> CreatePaymentPlan([FromBody] PaymentPlanRequest request)
        {
            try
            {
                var result = await _enhancedPaymentService.CreatePaymentPlanAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment plan");
                return StatusCode(500, new { message = "Error creating payment plan", error = ex.Message });
            }
        }

        [HttpGet("payment-plans/{tenantId}")]
        public async Task<ActionResult<List<PaymentPlanDto>>> GetPaymentPlans(Guid tenantId)
        {
            try
            {
                var plans = await _enhancedPaymentService.GetPaymentPlansAsync(tenantId);
                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment plans for tenant {tenantId}");
                return StatusCode(500, new { message = "Error retrieving payment plans", error = ex.Message });
            }
        }

        [HttpPost("payment-plans/installments/{installmentId}/pay")]
        public async Task<ActionResult<PaymentPlanInstallmentResult>> ProcessInstallmentPayment(
            Guid installmentId, 
            [FromBody] InstallmentPaymentRequest request)
        {
            try
            {
                var result = await _enhancedPaymentService.ProcessInstallmentPaymentAsync(installmentId, request.Amount);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing installment payment {installmentId}");
                return StatusCode(500, new { message = "Error processing installment payment", error = ex.Message });
            }
        }

        #endregion

        #region Advanced Analytics

        [HttpGet("analytics/{tenantId}")]
        public async Task<ActionResult<PaymentAnalyticsDto>> GetPaymentAnalytics(
            Guid tenantId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var analytics = await _enhancedPaymentService.GetAdvancedPaymentAnalyticsAsync(tenantId, startDate, endDate);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment analytics for tenant {tenantId}");
                return StatusCode(500, new { message = "Error retrieving analytics", error = ex.Message });
            }
        }

        [HttpGet("analytics/{tenantId}/trends")]
        public async Task<ActionResult<List<PaymentTrendDto>>> GetPaymentTrends(
            Guid tenantId,
            [FromQuery] int months = 12)
        {
            try
            {
                var trends = await _enhancedPaymentService.GetPaymentTrendsAsync(tenantId, months);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment trends for tenant {tenantId}");
                return StatusCode(500, new { message = "Error retrieving trends", error = ex.Message });
            }
        }

        [HttpGet("analytics/{tenantId}/payment-method-distribution")]
        public async Task<ActionResult<PaymentMethodDistributionDto>> GetPaymentMethodDistribution(
            Guid tenantId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var distribution = await _enhancedPaymentService.GetPaymentMethodDistributionAsync(tenantId, startDate, endDate);
                return Ok(distribution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment method distribution for tenant {tenantId}");
                return StatusCode(500, new { message = "Error retrieving distribution", error = ex.Message });
            }
        }

        #endregion
    }

    // Additional Request DTOs
    public class CurrencyConversionRequest
    {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
    }

    public class InstallmentPaymentRequest
    {
        public decimal Amount { get; set; }
    }
}
