using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace UmiHealth.MinimalApi.Services;

public class ReportService : IReportService
{
    private readonly UmiHealthDbContext _context;
    private readonly IAuditService _auditService;

    public ReportService(UmiHealthDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<(bool Success, string Message, Report? Report)> GenerateSalesReportAsync(string tenantId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sales = await _context.Sales
                .Where(s => s.TenantId == tenantId && s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .ToListAsync();

            var reportData = new
            {
                TotalSales = sales.Count,
                TotalRevenue = sales.Sum(s => s.TotalAmount),
                AverageSaleValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                TopSellingItems = new List<object>(), // TODO: Implement when SaleItems is available
                DailySales = sales.GroupBy(s => s.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Sales = g.Count(), Revenue = g.Sum(s => s.TotalAmount) })
                    .OrderBy(x => x.Date)
            };

            var report = new Report
            {
                TenantId = tenantId,
                ReportName = $"Sales Report ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})",
                ReportType = "sales",
                StartDate = startDate,
                EndDate = endDate,
                Description = "Comprehensive sales analysis report",
                Parameters = JsonSerializer.Serialize(reportData),
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                Status = "generated"
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            _auditService.LogSuperAdminAction(
                null,
                tenantId,
                "REPORT_GENERATED",
                "Report",
                new Dictionary<string, object> { ["ReportId"] = report.Id, ["Type"] = "Sales" }
            );

            return (true, "Sales report generated successfully", report);
        }
        catch (Exception ex)
        {
            return (false, $"Sales report generation error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, Report? Report)> GenerateInventoryReportAsync(string tenantId)
    {
        try
        {
            var inventory = await _context.Inventory
                .Where(i => i.TenantId == tenantId)
                .ToListAsync();

            var reportData = new
            {
                TotalItems = inventory.Count,
                TotalValue = inventory.Sum(i => i.SellingPrice * i.CurrentStock),
                LowStockItems = inventory.Where(i => i.CurrentStock <= i.MinStockLevel).ToList(),
                CategoryBreakdown = inventory.GroupBy(i => i.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count(), Value = g.Sum(i => i.SellingPrice * i.CurrentStock) })
                    .OrderByDescending(x => x.Value)
            };

            var report = new Report
            {
                TenantId = tenantId,
                ReportName = $"Inventory Report ({DateTime.UtcNow:yyyy-MM-dd})",
                ReportType = "inventory",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                Description = "Current inventory status and analysis",
                Parameters = JsonSerializer.Serialize(reportData),
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                Status = "generated"
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return (true, "Inventory report generated successfully", report);
        }
        catch (Exception ex)
        {
            return (false, $"Inventory report generation error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, Report? Report)> GeneratePatientReportAsync(string tenantId)
    {
        try
        {
            var patients = await _context.Patients
                .Where(p => p.TenantId == tenantId)
                .ToListAsync();

            var prescriptions = await _context.Prescriptions
                .Where(p => patients.Select(pat => pat.Id).Contains(p.PatientId))
                .ToListAsync();

            var reportData = new
            {
                TotalPatients = patients.Count,
                NewPatientsThisMonth = patients.Count(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
                PatientsWithAllergies = patients.Count(p => !string.IsNullOrWhiteSpace(p.Allergies)),
                TotalPrescriptions = prescriptions.Count,
                PrescriptionsThisMonth = prescriptions.Count(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
                AgeDistribution = patients.Where(p => !string.IsNullOrWhiteSpace(p.DateOfBirth))
                    .GroupBy(p => CalculateAge(DateTime.TryParse(p.DateOfBirth, out var dob) ? dob : (DateTime?)null))
                    .Select(g => new { AgeGroup = GetAgeGroup(g.Key), Count = g.Count() })
                    .OrderBy(x => x.AgeGroup),
                GenderDistribution = patients.GroupBy(p => p.Gender ?? "Unknown")
                    .Select(g => new { Gender = g.Key, Count = g.Count() })
                    .ToList()
            };

            var report = new Report
            {
                TenantId = tenantId,
                ReportName = $"Patient Report ({DateTime.UtcNow:yyyy-MM-dd})",
                ReportType = "patient",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                Description = "Patient demographics and activity report",
                Parameters = JsonSerializer.Serialize(reportData),
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                Status = "generated"
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return (true, "Patient report generated successfully", report);
        }
        catch (Exception ex)
        {
            return (false, $"Patient report generation error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, Report? Report)> GenerateFinancialReportAsync(string tenantId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sales = await _context.Sales
                .Where(s => s.TenantId == tenantId && s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .ToListAsync();

            var reportData = new
            {
                TotalRevenue = sales.Sum(s => s.TotalAmount),
                TotalSales = sales.Count,
                AverageTransactionValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                MonthlyBreakdown = sales.GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                    .Select(g => new { Month = $"{g.Key.Year}-{g.Key.Month:D2}", Revenue = g.Sum(s => s.TotalAmount) })
                    .OrderBy(x => x.Month)
            };

            var report = new Report
            {
                TenantId = tenantId,
                ReportName = $"Financial Report ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})",
                ReportType = "financial",
                StartDate = startDate,
                EndDate = endDate,
                Description = "Financial performance and analysis report",
                Parameters = JsonSerializer.Serialize(reportData),
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System",
                Status = "generated"
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return (true, "Financial report generated successfully", report);
        }
        catch (Exception ex)
        {
            return (false, $"Financial report generation error: {ex.Message}", null);
        }
    }

    public async Task<List<Report>> GetReportsByTenantAsync(string tenantId)
    {
        return await _context.Reports
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();
    }

    public async Task<Report?> GetReportByIdAsync(string reportId)
    {
        return await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId);
    }

    public async Task<(bool Success, string Message)> DeleteReportAsync(string reportId)
    {
        try
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return (false, "Report not found");
            }

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            return (true, "Report deleted successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Report deletion error: {ex.Message}");
        }
    }

    public async Task<byte[]> ExportReportToPdfAsync(string reportId)
    {
        // Placeholder for PDF export functionality
        var report = await GetReportByIdAsync(reportId);
        if (report == null)
            return Array.Empty<byte>();

        return System.Text.Encoding.UTF8.GetBytes($"PDF Report: {report.ReportName}");
    }

    public async Task<byte[]> ExportReportToExcelAsync(string reportId)
    {
        // Placeholder for Excel export functionality
        var report = await GetReportByIdAsync(reportId);
        if (report == null)
            return Array.Empty<byte>();

        return System.Text.Encoding.UTF8.GetBytes($"Excel Report: {report.ReportName}");
    }

    private int CalculateAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
            return 0;

        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }

    private string GetAgeGroup(int age)
    {
        return age switch
        {
            < 18 => "Under 18",
            < 30 => "18-29",
            < 45 => "30-44",
            < 60 => "45-59",
            _ => "60+"
        };
    }
}
