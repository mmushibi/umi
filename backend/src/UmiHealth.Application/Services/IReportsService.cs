using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IReportsService
    {
        Task<SalesReportDto> GetSalesReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string groupBy = "day");
        Task<InventoryReportDto> GetInventoryReportAsync(Guid tenantId, Guid? branchId = null, string category = null, bool? lowStock = null, bool? expiring = null);
        Task<PatientsReportDto> GetPatientsReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "month");
        Task<PrescriptionsReportDto> GetPrescriptionsReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string status = null);
        Task<FinancialReportDto> GetFinancialReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string reportType = "summary");
        Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(Guid tenantId, Guid? branchId = null, int? periodDays = 30);
        Task<TrendsAnalyticsDto> GetTrendsAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string metric = "sales");
        Task<byte[]> ExportReportAsync(Guid tenantId, string reportType, DateTime startDate, DateTime endDate, string format = "pdf", Guid? branchId = null);
        Task<PerformanceReportDto> GetPerformanceReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, Guid? userId = null);
        Task<AuditReportDto> GetAuditReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string action = null, Guid? userId = null, string entityType = null);
    }
}
