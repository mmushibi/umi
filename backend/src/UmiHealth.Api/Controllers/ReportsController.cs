using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsService _reportsService;

        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("sales")]
        public async Task<ActionResult<SalesReportDto>> GetSalesReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? branchId = null,
            [FromQuery] string? groupBy = "day")
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _reportsService.GetSalesReportAsync(tenantId, startDate, endDate, branchId, groupBy);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate sales report." });
            }
        }

        [HttpGet("inventory")]
        public async Task<ActionResult<InventoryReportDto>> GetInventoryReport(
            [FromQuery] Guid? branchId = null,
            [FromQuery] string? category = null,
            [FromQuery] bool? lowStock = null,
            [FromQuery] bool? expiring = null)
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _reportsService.GetInventoryReportAsync(tenantId, branchId, category, lowStock, expiring);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate inventory report." });
            }
        }

        [HttpGet("patients")]
        public async Task<ActionResult<PatientsReportDto>> GetPatientsReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? groupBy = "month")
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _reportsService.GetPatientsReportAsync(tenantId, startDate, endDate, groupBy);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate patients report." });
            }
        }

        [HttpGet("prescriptions")]
        public async Task<ActionResult<PrescriptionsReportDto>> GetPrescriptionsReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? branchId = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _reportsService.GetPrescriptionsReportAsync(tenantId, startDate, endDate, branchId, status);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate prescriptions report." });
            }
        }

        [HttpGet("financial")]
        public async Task<ActionResult<FinancialReportDto>> GetFinancialReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? branchId = null,
            [FromQuery] string? reportType = "summary")
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _reportsService.GetFinancialReportAsync(tenantId, startDate, endDate, branchId, reportType);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate financial report." });
            }
        }

        [HttpGet("analytics/dashboard")]
        public async Task<ActionResult<DashboardAnalyticsDto>> GetDashboardAnalytics(
            [FromQuery] Guid? branchId = null,
            [FromQuery] int? periodDays = 30)
        {
            try
            {
                var tenantId = GetTenantId();
                var analytics = await _reportsService.GetDashboardAnalyticsAsync(tenantId, branchId, periodDays);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate dashboard analytics." });
            }
        }

        [HttpGet("analytics/trends")]
        public async Task<ActionResult<TrendsAnalyticsDto>> GetTrendsAnalytics(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? branchId = null,
            [FromQuery] string? metric = "sales")
        {
            try
            {
                var tenantId = GetTenantId();
                var trends = await _reportsService.GetTrendsAnalyticsAsync(tenantId, startDate, endDate, branchId, metric);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate trends analytics." });
            }
        }

        [HttpGet("export")]
        public async Task<FileResult> ExportReport(
            [FromQuery] string reportType,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string format = "pdf",
            [FromQuery] Guid? branchId = null)
        {
            try
            {
                var tenantId = GetTenantId();
                var fileBytes = await _reportsService.ExportReportAsync(tenantId, reportType, startDate, endDate, format, branchId);
                
                var contentType = format.ToLower() switch
                {
                    "pdf" => "application/pdf",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "csv" => "text/csv",
                    _ => "application/pdf"
                };

                var fileName = $"{reportType}_report_{DateTime.Now:yyyyMMdd}.{format}";
                
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to export report.", ex);
            }
        }

        [HttpGet("performance")]
        public async Task<ActionResult<PerformanceReportDto>> GetPerformanceReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] Guid? branchId = null,
            [FromQuery] Guid? userId = null)
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _reportsService.GetPerformanceReportAsync(tenantId, startDate, endDate, branchId, userId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate performance report." });
            }
        }

        [HttpGet("audits")]
        public async Task<ActionResult<AuditReportDto>> GetAuditReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? action = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? entityType = null)
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _reportsService.GetAuditReportAsync(tenantId, startDate, endDate, action, userId, entityType);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate audit report." });
            }
        }

        private Guid GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                throw new UnauthorizedAccessException("Tenant information not found");

            return Guid.Parse(tenantIdClaim);
        }
    }
}
