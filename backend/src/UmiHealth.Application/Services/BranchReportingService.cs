using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IBranchReportingService
    {
        Task<BranchReport> GenerateSalesReportAsync(Guid branchId, DateTime startDate, DateTime endDate, string period = "daily");
        Task<BranchReport> GenerateInventoryReportAsync(Guid branchId, DateTime startDate, DateTime endDate);
        Task<BranchReport> GenerateFinancialReportAsync(Guid branchId, DateTime startDate, DateTime endDate);
        Task<BranchReport> GeneratePatientReportAsync(Guid branchId, DateTime startDate, DateTime endDate);
        Task<BranchReport> GeneratePrescriptionReportAsync(Guid branchId, DateTime startDate, DateTime endDate);
        Task<Dictionary<string, object>> GetBranchDashboardAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<BranchReport>> GetBranchReportsAsync(Guid branchId, string? type = null, int limit = 50);
        Task<BranchReport?> GetReportAsync(Guid reportId);
        Task<bool> DeleteReportAsync(Guid reportId);
        Task<byte[]> ExportReportAsync(Guid reportId, string format = "pdf");
        Task<Dictionary<string, object>> GetCrossBranchComparisonAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }

    public class BranchReportingService : IBranchReportingService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<BranchReportingService> _logger;

        public BranchReportingService(
            SharedDbContext context,
            ILogger<BranchReportingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BranchReport> GenerateSalesReportAsync(Guid branchId, DateTime startDate, DateTime endDate, string period = "daily")
        {
            var sales = await _context.Sales
                .Include(s => s.Items)
                .Where(s => s.BranchId == branchId && 
                           s.CreatedAt >= startDate && 
                           s.CreatedAt <= endDate)
                .ToListAsync();

            var totalSales = sales.Count;
            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var averageSale = totalSales > 0 ? totalRevenue / totalSales : 0;

            var salesByPeriod = GroupSalesByPeriod(sales, period);
            var topProducts = GetTopSellingProducts(sales);
            var paymentMethods = GetPaymentMethodBreakdown(sales);

            var reportData = new Dictionary<string, object>
            {
                ["total_sales"] = totalSales,
                ["total_revenue"] = totalRevenue,
                ["average_sale"] = averageSale,
                ["sales_by_period"] = salesByPeriod,
                ["top_products"] = topProducts,
                ["payment_methods"] = paymentMethods,
                ["period_start"] = startDate,
                ["period_end"] = endDate
            };

            var report = new BranchReport
            {
                Id = Guid.NewGuid(),
                TenantId = _context.GetCurrentTenantId(),
                BranchId = branchId,
                Name = $"Sales Report {period}",
                Type = "sales",
                Period = period,
                StartDate = startDate,
                EndDate = endDate,
                Data = reportData,
                Metrics = CalculateSalesMetrics(sales, startDate, endDate),
                Status = "completed",
                GeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        public async Task<BranchReport> GenerateInventoryReportAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Branch)
                .Where(i => i.BranchId == branchId)
                .ToListAsync();

            var totalItems = inventory.Count;
            var totalValue = inventory.Where(i => i.CostPrice.HasValue)
                                   .Sum(i => i.QuantityOnHand * i.CostPrice!.Value);
            var lowStockItems = inventory.Count(i => i.QuantityOnHand <= i.ReorderLevel && i.ReorderLevel > 0);
            var outOfStockItems = inventory.Count(i => i.QuantityOnHand == 0);
            var expiringItems = inventory.Count(i => i.ExpiryDate.HasValue && 
                                                   i.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                                                   i.QuantityOnHand > 0);

            var inventoryByCategory = inventory
                .GroupBy(i => i.Product.Category)
                .ToDictionary(g => g.Key ?? "Uncategorized", g => new
                {
                    count = g.Count(),
                    total_quantity = g.Sum(i => i.QuantityOnHand),
                    total_value = g.Where(i => i.CostPrice.HasValue).Sum(i => i.QuantityOnHand * i.CostPrice!.Value)
                });

            var reportData = new Dictionary<string, object>
            {
                ["total_items"] = totalItems,
                ["total_value"] = totalValue,
                ["low_stock_items"] = lowStockItems,
                ["out_of_stock_items"] = outOfStockItems,
                ["expiring_items"] = expiringItems,
                ["inventory_by_category"] = inventoryByCategory,
                ["period_start"] = startDate,
                ["period_end"] = endDate
            };

            var report = new BranchReport
            {
                Id = Guid.NewGuid(),
                TenantId = _context.GetCurrentTenantId(),
                BranchId = branchId,
                Name = "Inventory Report",
                Type = "inventory",
                Period = "monthly",
                StartDate = startDate,
                EndDate = endDate,
                Data = reportData,
                Metrics = CalculateInventoryMetrics(inventory),
                Status = "completed",
                GeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        public async Task<BranchReport> GenerateFinancialReportAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            var sales = await _context.Sales
                .Where(s => s.BranchId == branchId && 
                           s.CreatedAt >= startDate && 
                           s.CreatedAt <= endDate)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.BranchId == branchId && 
                           p.CreatedAt >= startDate && 
                           p.CreatedAt <= endDate)
                .ToListAsync();

            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var totalPayments = payments.Where(p => p.Status == "completed").Sum(p => p.Amount);
            var pendingPayments = payments.Where(p => p.Status == "pending").Sum(p => p.Amount);
            var refunds = payments.Where(p => p.Status == "refunded").Sum(p => p.Amount);

            var revenueByDay = sales
                .GroupBy(s => s.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

            var paymentsByMethod = payments
                .GroupBy(p => p.PaymentMethod)
                .ToDictionary(g => g.Key, g => new
                {
                    total = g.Sum(p => p.Amount),
                    count = g.Count(),
                    completed = g.Count(p => p.Status == "completed")
                });

            var reportData = new Dictionary<string, object>
            {
                ["total_revenue"] = totalRevenue,
                ["total_payments"] = totalPayments,
                ["pending_payments"] = pendingPayments,
                ["refunds"] = refunds,
                ["revenue_by_day"] = revenueByDay,
                ["payments_by_method"] = paymentsByMethod,
                ["period_start"] = startDate,
                ["period_end"] = endDate
            };

            var report = new BranchReport
            {
                Id = Guid.NewGuid(),
                TenantId = _context.GetCurrentTenantId(),
                BranchId = branchId,
                Name = "Financial Report",
                Type = "financial",
                Period = "monthly",
                StartDate = startDate,
                EndDate = endDate,
                Data = reportData,
                Metrics = CalculateFinancialMetrics(sales, payments),
                Status = "completed",
                GeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        public async Task<BranchReport> GeneratePatientReportAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            var patients = await _context.Patients
                .Where(p => p.BranchId == branchId && 
                           p.CreatedAt >= startDate && 
                           p.CreatedAt <= endDate)
                .ToListAsync();

            var totalPatients = patients.Count;
            var newPatients = patients.Count(p => p.CreatedAt >= startDate);
            var activePatients = patients.Count(p => p.Status == "active");
            var patientsByGender = patients
                .GroupBy(p => p.Gender ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var patientsByAge = patients
                .Where(p => p.DateOfBirth.HasValue)
                .GroupBy(p => CalculateAgeGroup(p.DateOfBirth!.Value))
                .ToDictionary(g => g.Key, g => g.Count());

            var reportData = new Dictionary<string, object>
            {
                ["total_patients"] = totalPatients,
                ["new_patients"] = newPatients,
                ["active_patients"] = activePatients,
                ["patients_by_gender"] = patientsByGender,
                ["patients_by_age_group"] = patientsByAge,
                ["period_start"] = startDate,
                ["period_end"] = endDate
            };

            var report = new BranchReport
            {
                Id = Guid.NewGuid(),
                TenantId = _context.GetCurrentTenantId(),
                BranchId = branchId,
                Name = "Patient Report",
                Type = "patients",
                Period = "monthly",
                StartDate = startDate,
                EndDate = endDate,
                Data = reportData,
                Metrics = CalculatePatientMetrics(patients),
                Status = "completed",
                GeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        public async Task<BranchReport> GeneratePrescriptionReportAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Items)
                .Where(p => p.BranchId == branchId && 
                           p.CreatedAt >= startDate && 
                           p.CreatedAt <= endDate)
                .ToListAsync();

            var totalPrescriptions = prescriptions.Count;
            var dispensedPrescriptions = prescriptions.Count(p => p.Status == "dispensed");
            var pendingPrescriptions = prescriptions.Count(p => p.Status == "pending");

            var prescriptionsByStatus = prescriptions
                .GroupBy(p => p.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var prescriptionsByDay = prescriptions
                .GroupBy(p => p.DatePrescribed.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var topMedications = prescriptions
                .SelectMany(p => p.Items)
                .GroupBy(item => item.GetType().GetProperty("ProductId")?.GetValue(item))
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var reportData = new Dictionary<string, object>
            {
                ["total_prescriptions"] = totalPrescriptions,
                ["dispensed_prescriptions"] = dispensedPrescriptions,
                ["pending_prescriptions"] = pendingPrescriptions,
                ["prescriptions_by_status"] = prescriptionsByStatus,
                ["prescriptions_by_day"] = prescriptionsByDay,
                ["top_medications"] = topMedications,
                ["period_start"] = startDate,
                ["period_end"] = endDate
            };

            var report = new BranchReport
            {
                Id = Guid.NewGuid(),
                TenantId = _context.GetCurrentTenantId(),
                BranchId = branchId,
                Name = "Prescription Report",
                Type = "prescriptions",
                Period = "monthly",
                StartDate = startDate,
                EndDate = endDate,
                Data = reportData,
                Metrics = CalculatePrescriptionMetrics(prescriptions),
                Status = "completed",
                GeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BranchReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        public async Task<Dictionary<string, object>> GetBranchDashboardAsync(Guid branchId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = toDate ?? DateTime.UtcNow;

            var tasks = new[]
            {
                GetSalesSummaryAsync(branchId, startDate, endDate),
                GetInventorySummaryAsync(branchId),
                GetPatientSummaryAsync(branchId, startDate, endDate),
                GetPrescriptionSummaryAsync(branchId, startDate, endDate),
                GetRecentTransfersAsync(branchId, 5)
            };

            var results = await Task.WhenAll(tasks);

            return new Dictionary<string, object>
            {
                ["sales_summary"] = results[0],
                ["inventory_summary"] = results[1],
                ["patient_summary"] = results[2],
                ["prescription_summary"] = results[3],
                ["recent_transfers"] = results[4],
                ["period_start"] = startDate,
                ["period_end"] = endDate,
                ["last_updated"] = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<BranchReport>> GetBranchReportsAsync(Guid branchId, string? type = null, int limit = 50)
        {
            var query = _context.BranchReports
                .Include(r => r.Branch)
                .Where(r => r.BranchId == branchId);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(r => r.Type == type);

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<BranchReport?> GetReportAsync(Guid reportId)
        {
            return await _context.BranchReports
                .Include(r => r.Branch)
                .Include(r => r.GeneratedByUser)
                .FirstOrDefaultAsync(r => r.Id == reportId);
        }

        public async Task<bool> DeleteReportAsync(Guid reportId)
        {
            var report = await _context.BranchReports.FindAsync(reportId);
            if (report == null)
                return false;

            _context.BranchReports.Remove(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Report {ReportId} deleted", reportId);
            return true;
        }

        public async Task<byte[]> ExportReportAsync(Guid reportId, string format = "pdf")
        {
            var report = await GetReportAsync(reportId);
            if (report == null)
                throw new InvalidOperationException("Report not found");

            // Implementation would depend on your PDF/Excel generation library
            // This is a placeholder that returns a simple text representation
            var content = $"Report: {report.Name}\n" +
                         $"Type: {report.Type}\n" +
                         $"Period: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}\n" +
                         $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}\n\n" +
                         $"Data: {System.Text.Json.JsonSerializer.Serialize(report.Data)}";

            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        public async Task<Dictionary<string, object>> GetCrossBranchComparisonAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var branches = await _context.Branches
                .Where(b => b.TenantId == tenantId)
                .ToListAsync();

            var comparisonTasks = branches.Select(async branch => new
            {
                branch_id = branch.Id,
                branch_name = branch.Name,
                sales_summary = await GetSalesSummaryAsync(branch.Id, startDate, endDate),
                inventory_summary = await GetInventorySummaryAsync(branch.Id)
            });

            var results = await Task.WhenAll(comparisonTasks);

            return new Dictionary<string, object>
            {
                ["branches"] = results,
                ["period_start"] = startDate,
                ["period_end"] = endDate,
                ["total_branches"] = branches.Count
            };
        }

        // Helper methods
        private Dictionary<string, object> GroupSalesByPeriod(List<Sale> sales, string period)
        {
            return period.ToLower() switch
            {
                "daily" => sales.GroupBy(s => s.CreatedAt.Date)
                               .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Sum(s => s.TotalAmount)),
                "weekly" => sales.GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.CreatedAt.Date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
                               .ToDictionary(g => $"Week {g.Key}", g => g.Sum(s => s.TotalAmount)),
                "monthly" => sales.GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                                 .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Sum(s => s.TotalAmount)),
                _ => sales.GroupBy(s => s.CreatedAt.Date)
                         .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Sum(s => s.TotalAmount))
            };
        }

        private List<object> GetTopSellingProducts(List<Sale> sales)
        {
            return sales.SelectMany(s => s.Items)
                      .GroupBy(item => item.GetType().GetProperty("ProductId")?.GetValue(item))
                      .Select(g => new { ProductId = g.Key, Quantity = g.Count(), Revenue = g.Sum(item => (decimal?)item.GetType().GetProperty("TotalPrice")?.GetValue(item) ?? 0) })
                      .OrderByDescending(x => x.Revenue)
                      .Take(10)
                      .Cast<object>()
                      .ToList();
        }

        private Dictionary<string, object> GetPaymentMethodBreakdown(List<Sale> sales)
        {
            return sales.GroupBy(s => s.PaymentMethod ?? "Unknown")
                      .ToDictionary(g => g.Key, g => new { Count = g.Count(), Total = g.Sum(s => s.TotalAmount) })
                      .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }

        private Dictionary<string, object> CalculateSalesMetrics(List<Sale> sales, DateTime startDate, DateTime endDate)
        {
            var days = (endDate - startDate).Days + 1;
            return new Dictionary<string, object>
            {
                ["sales_per_day"] = sales.Count / (double)days,
                ["revenue_per_day"] = sales.Sum(s => s.TotalAmount) / days,
                ["average_transaction_value"] = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                ["growth_rate"] = CalculateGrowthRate(sales)
            };
        }

        private Dictionary<string, object> CalculateInventoryMetrics(List<Inventory> inventory)
        {
            return new Dictionary<string, object>
            {
                ["turnover_rate"] = CalculateInventoryTurnover(inventory),
                ["stock_accuracy"] = CalculateStockAccuracy(inventory),
                ["carrying_cost"] = inventory.Where(i => i.CostPrice.HasValue).Sum(i => i.QuantityOnHand * i.CostPrice!.Value)
            };
        }

        private Dictionary<string, object> CalculateFinancialMetrics(List<Sale> sales, List<Payment> payments)
        {
            return new Dictionary<string, object>
            {
                ["profit_margin"] = CalculateProfitMargin(sales),
                ["payment_success_rate"] = payments.Any() ? (double)payments.Count(p => p.Status == "completed") / payments.Count * 100 : 0,
                ["average_payment_amount"] = payments.Any() ? payments.Average(p => p.Amount) : 0
            };
        }

        private Dictionary<string, object> CalculatePatientMetrics(List<Patient> patients)
        {
            return new Dictionary<string, object>
            {
                ["retention_rate"] = CalculatePatientRetention(patients),
                ["average_age"] = patients.Where(p => p.DateOfBirth.HasValue).Average(p => CalculateAge(p.DateOfBirth!.Value)),
                ["gender_distribution"] = patients.GroupBy(p => p.Gender ?? "Unknown").ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private Dictionary<string, object> CalculatePrescriptionMetrics(List<Prescription> prescriptions)
        {
            return new Dictionary<string, object>
            {
                ["dispense_rate"] = prescriptions.Any() ? (double)prescriptions.Count(p => p.Status == "dispensed") / prescriptions.Count * 100 : 0,
                ["average_items_per_prescription"] = prescriptions.Any() ? prescriptions.Average(p => p.Items.Count) : 0,
                ["processing_time"] = CalculateAverageProcessingTime(prescriptions)
            };
        }

        private async Task<Dictionary<string, object>> GetSalesSummaryAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            var sales = await _context.Sales
                .Where(s => s.BranchId == branchId && s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["total_sales"] = sales.Count,
                ["total_revenue"] = sales.Sum(s => s.TotalAmount),
                ["average_sale"] = sales.Any() ? sales.Average(s => s.TotalAmount) : 0
            };
        }

        private async Task<Dictionary<string, object>> GetInventorySummaryAsync(Guid branchId)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.BranchId == branchId)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["total_items"] = inventory.Count,
                ["low_stock_count"] = inventory.Count(i => i.QuantityOnHand <= i.ReorderLevel && i.ReorderLevel > 0),
                ["out_of_stock_count"] = inventory.Count(i => i.QuantityOnHand == 0),
                ["total_value"] = inventory.Where(i => i.CostPrice.HasValue).Sum(i => i.QuantityOnHand * i.CostPrice!.Value)
            };
        }

        private async Task<Dictionary<string, object>> GetPatientSummaryAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            var patients = await _context.Patients
                .Where(p => p.BranchId == branchId && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["new_patients"] = patients.Count,
                ["active_patients"] = patients.Count(p => p.Status == "active")
            };
        }

        private async Task<Dictionary<string, object>> GetPrescriptionSummaryAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            var prescriptions = await _context.Prescriptions
                .Where(p => p.BranchId == branchId && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["total_prescriptions"] = prescriptions.Count,
                ["dispensed_prescriptions"] = prescriptions.Count(p => p.Status == "dispensed"),
                ["pending_prescriptions"] = prescriptions.Count(p => p.Status == "pending")
            };
        }

        private async Task<List<object>> GetRecentTransfersAsync(Guid branchId, int limit)
        {
            var transfers = await _context.StockTransfers
                .Include(st => st.SourceBranch)
                .Include(st => st.DestinationBranch)
                .Where(st => (st.SourceBranchId == branchId || st.DestinationBranchId == branchId))
                .OrderByDescending(st => st.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return transfers.Cast<object>().ToList();
        }

        // Additional helper methods for calculations
        private double CalculateGrowthRate(List<Sale> sales)
        {
            // Simple growth rate calculation - would need more sophisticated logic
            return 0.0;
        }

        private double CalculateInventoryTurnover(List<Inventory> inventory)
        {
            // Simplified calculation - would need historical data
            return inventory.Any() ? inventory.Average(i => i.QuantityOnHand) : 0;
        }

        private double CalculateStockAccuracy(List<Inventory> inventory)
        {
            // Would need actual count data
            return 95.0; // Placeholder
        }

        private double CalculateProfitMargin(List<Sale> sales)
        {
            // Would need cost data
            return 25.0; // Placeholder
        }

        private double CalculatePatientRetention(List<Patient> patients)
        {
            // Would need historical activity data
            return 85.0; // Placeholder
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        private string CalculateAgeGroup(DateTime dateOfBirth)
        {
            var age = CalculateAge(dateOfBirth);
            return age switch
            {
                < 18 => "Under 18",
                < 30 => "18-29",
                < 45 => "30-44",
                < 60 => "45-59",
                _ => "60+"
            };
        }

        private double CalculateAverageProcessingTime(List<Prescription> prescriptions)
        {
            var processedPrescriptions = prescriptions.Where(p => p.DispensedDate.HasValue).ToList();
            if (!processedPrescriptions.Any()) return 0;

            return processedPrescriptions
                .Average(p => (p.DispensedDate!.Value - p.DatePrescribed).TotalHours);
        }
    }
}
