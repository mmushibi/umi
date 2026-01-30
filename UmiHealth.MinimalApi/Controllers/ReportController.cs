using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmiHealth.MinimalApi.Services;
using UmiHealth.MinimalApi.Models;
using System.Security.Claims;

namespace UmiHealth.MinimalApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IAuditService _auditService;

    public ReportController(IReportService reportService, IAuditService auditService)
    {
        _reportService = reportService;
        _auditService = auditService;
    }

    [HttpGet("{reportId}")]
    public async Task<IActionResult> GetReport(string reportId)
    {
        var report = await _reportService.GetReportByIdAsync(reportId);
        if (report == null)
        {
            return NotFound(new { success = false, message = "Report not found" });
        }

        return Ok(new { success = true, report });
    }

    [HttpGet]
    public async Task<IActionResult> GetReports()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var reports = await _reportService.GetReportsByTenantAsync(tenantId);
        return Ok(new { success = true, reports });
    }

    [HttpPost("sales")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> GenerateSalesReport([FromBody] ReportDateRangeRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var result = await _reportService.GenerateSalesReportAsync(tenantId, request.StartDate, request.EndDate);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, report = result.Report });
    }

    [HttpPost("inventory")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> GenerateInventoryReport()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var result = await _reportService.GenerateInventoryReportAsync(tenantId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, report = result.Report });
    }

    [HttpPost("patient")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> GeneratePatientReport()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var result = await _reportService.GeneratePatientReportAsync(tenantId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, report = result.Report });
    }

    [HttpPost("financial")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateFinancialReport([FromBody] ReportDateRangeRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var result = await _reportService.GenerateFinancialReportAsync(tenantId, request.StartDate, request.EndDate);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, report = result.Report });
    }

    [HttpDelete("{reportId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReport(string reportId)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var report = await _reportService.GetReportByIdAsync(reportId);
        if (report == null || report.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Report not found" });
        }

        var result = await _reportService.DeleteReportAsync(reportId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpGet("{reportId}/export/pdf")]
    public async Task<IActionResult> ExportToPdf(string reportId)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var report = await _reportService.GetReportByIdAsync(reportId);
        if (report == null || report.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Report not found" });
        }

        var pdfData = await _reportService.ExportReportToPdfAsync(reportId);
        return File(pdfData, "application/pdf", $"{report.Title}.pdf");
    }

    [HttpGet("{reportId}/export/excel")]
    public async Task<IActionResult> ExportToExcel(string reportId)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var report = await _reportService.GetReportByIdAsync(reportId);
        if (report == null || report.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Report not found" });
        }

        var excelData = await _reportService.ExportReportToExcelAsync(reportId);
        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{report.Title}.xlsx");
    }
}

public class ReportDateRangeRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
