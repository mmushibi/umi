using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Api.Models;

namespace UmiHealth.Api.Controllers
{
    /// <summary>
    /// Regulatory Compliance API endpoints
    /// Provides ZAMRA (medicines), ZRA (tax), and general compliance reporting
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,ComplianceOfficer")]
    public class ComplianceController : ControllerBase
    {
        private readonly IComplianceService _complianceService;
        private readonly ILogger<ComplianceController> _logger;

        public ComplianceController(
            IComplianceService complianceService,
            ILogger<ComplianceController> logger)
        {
            _complianceService = complianceService;
            _logger = logger;
        }

        /// <summary>
        /// Get ZAMRA compliance report (Medicines Regulatory Authority)
        /// </summary>
        [HttpGet("zamra-report")]
        [ProducesResponseType(typeof(ApiResponse<ZamraComplianceReport>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetZamraReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var report = await _complianceService.GenerateZamraComplianceReportAsync(
                    tenantId, startDate.Value, endDate.Value);

                return Ok(ApiResponseHelper.Success(report, "ZAMRA compliance report generated"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ZAMRA report");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to generate ZAMRA report"));
            }
        }

        /// <summary>
        /// Get prescription audit trail for ZAMRA
        /// </summary>
        [HttpGet("prescription-audit/{prescriptionId}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionAuditTrail>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPrescriptionAuditTrail(Guid prescriptionId)
        {
            try
            {
                var trail = await _complianceService.GetPrescriptionAuditTrailAsync(prescriptionId);
                return Ok(ApiResponseHelper.Success(trail, "Prescription audit trail retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving prescription audit trail");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to retrieve audit trail"));
            }
        }

        /// <summary>
        /// Get expiry compliance report
        /// </summary>
        [HttpGet("expiry-compliance")]
        [ProducesResponseType(typeof(ApiResponse<ExpiryComplianceReport>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExpiryCompliance([FromQuery] string branchId)
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(branchId))
                    return BadRequest(ApiResponseHelper.ValidationError("Missing required parameters"));

                var report = await _complianceService.GetExpiryComplianceReportAsync(tenantId, branchId);
                return Ok(ApiResponseHelper.Success(report, "Expiry compliance report retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiry compliance");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to retrieve expiry compliance"));
            }
        }

        /// <summary>
        /// Check drug interactions for patient safety
        /// </summary>
        [HttpPost("check-interactions")]
        [ProducesResponseType(typeof(ApiResponse<DrugInteractionReport>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckDrugInteractions([FromBody] CheckInteractionsRequest request)
        {
            try
            {
                if (request?.ProductIds == null || request.ProductIds.Count == 0)
                    return BadRequest(ApiResponseHelper.ValidationError("ProductIds are required"));

                var report = await _complianceService.CheckDrugInteractionsAsync(request.ProductIds);
                return Ok(ApiResponseHelper.Success(report, "Drug interactions checked"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking drug interactions");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to check drug interactions"));
            }
        }

        /// <summary>
        /// Get controlled substance report for ZAMRA
        /// </summary>
        [HttpGet("controlled-substances")]
        [ProducesResponseType(typeof(ApiResponse<ControlledSubstanceReport>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetControlledSubstanceReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var report = await _complianceService.GetControlledSubstanceReportAsync(
                    tenantId, startDate.Value, endDate.Value);

                return Ok(ApiResponseHelper.Success(report, "Controlled substance report retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving controlled substance report");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to retrieve controlled substance report"));
            }
        }

        /// <summary>
        /// Get ZRA tax compliance report (Revenue Authority)
        /// </summary>
        [HttpGet("zra-tax-report")]
        [ProducesResponseType(typeof(ApiResponse<TaxCompleteReport>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetZraTaxReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var report = await _complianceService.GenerateTaxComplianceReportAsync(
                    tenantId, startDate.Value, endDate.Value);

                return Ok(ApiResponseHelper.Success(report, "ZRA tax compliance report generated"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ZRA tax report");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to generate tax report"));
            }
        }

        /// <summary>
        /// Get invoice audit trail for ZRA
        /// </summary>
        [HttpGet("invoice-audit/{invoiceId}")]
        [ProducesResponseType(typeof(ApiResponse<InvoiceAuditTrail>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInvoiceAuditTrail(Guid invoiceId)
        {
            try
            {
                var trail = await _complianceService.GetInvoiceAuditTrailAsync(invoiceId);
                return Ok(ApiResponseHelper.Success(trail, "Invoice audit trail retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice audit trail");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to retrieve audit trail"));
            }
        }

        /// <summary>
        /// Calculate VAT for tax period
        /// </summary>
        [HttpGet("vat-calculation")]
        [ProducesResponseType(typeof(ApiResponse<VatCalculationReport>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CalculateVat(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                startDate ??= DateTime.UtcNow.AddMonths(-1);
                endDate ??= DateTime.UtcNow;

                var report = await _complianceService.CalculateVatAsync(tenantId, startDate.Value, endDate.Value);
                return Ok(ApiResponseHelper.Success(report, "VAT calculation completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating VAT");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to calculate VAT"));
            }
        }

        /// <summary>
        /// Get tax exemption details
        /// </summary>
        [HttpGet("exemptions")]
        [ProducesResponseType(typeof(ApiResponse<ExemptionReport>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExemptions()
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var report = await _complianceService.GetExemptionReportAsync(tenantId);
                return Ok(ApiResponseHelper.Success(report, "Exemptions retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving exemptions");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to retrieve exemptions"));
            }
        }

        /// <summary>
        /// Get overall compliance status
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(ApiResponse<ComplianceStatus>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetComplianceStatus()
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var status = await _complianceService.GetComplianceStatusAsync(tenantId);
                return Ok(ApiResponseHelper.Success(status, "Compliance status retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving compliance status");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to retrieve compliance status"));
            }
        }

        /// <summary>
        /// Get active compliance alerts
        /// </summary>
        [HttpGet("alerts")]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.List<ComplianceAlert>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveAlerts()
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());

                var alerts = await _complianceService.GetActiveAlertsAsync(tenantId);
                return Ok(ApiResponseHelper.Success(alerts, "Active alerts retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving compliance alerts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseHelper.ServerError("Failed to retrieve alerts"));
            }
        }
    }

    public class CheckInteractionsRequest
    {
        public System.Collections.Generic.List<Guid> ProductIds { get; set; }
    }
}
