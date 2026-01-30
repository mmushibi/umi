using UmiHealth.MinimalApi.Models;

namespace UmiHealth.MinimalApi.Services;

public interface IReportService
{
    Task<(bool Success, string Message, Report? Report)> GenerateSalesReportAsync(string tenantId, DateTime startDate, DateTime endDate);
    Task<(bool Success, string Message, Report? Report)> GenerateInventoryReportAsync(string tenantId);
    Task<(bool Success, string Message, Report? Report)> GeneratePatientReportAsync(string tenantId);
    Task<(bool Success, string Message, Report? Report)> GenerateFinancialReportAsync(string tenantId, DateTime startDate, DateTime endDate);
    Task<List<Report>> GetReportsByTenantAsync(string tenantId);
    Task<Report?> GetReportByIdAsync(string reportId);
    Task<(bool Success, string Message)> DeleteReportAsync(string reportId);
    Task<byte[]> ExportReportToPdfAsync(string reportId);
    Task<byte[]> ExportReportToExcelAsync(string reportId);
}
