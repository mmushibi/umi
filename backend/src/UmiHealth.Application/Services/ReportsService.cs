using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Application.Services
{
    public class ReportsService : IReportsService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<ReportsService> _logger;

        public ReportsService(SharedDbContext context, ILogger<ReportsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SalesReportDto> GetSalesReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string groupBy = "day")
        {
            var query = _context.Sales
                .Include(s => s.Items)
                .Where(s => s.TenantId == tenantId && 
                           s.CreatedAt >= startDate && 
                           s.CreatedAt <= endDate);

            if (branchId.HasValue)
                query = query.Where(s => s.BranchId == branchId.Value);

            var sales = await query.ToListAsync();

            var groupedSales = groupBy.ToLower() switch
            {
                "day" => sales.GroupBy(s => s.CreatedAt.Date),
                "week" => sales.GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.CreatedAt.Date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday)),
                "month" => sales.GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month }),
                "year" => sales.GroupBy(s => s.CreatedAt.Year),
                _ => sales.GroupBy(s => s.CreatedAt.Date)
            };

            return new SalesReportDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                BranchId = branchId,
                GroupBy = groupBy,
                TotalSales = sales.Count,
                TotalRevenue = sales.Sum(s => s.TotalAmount),
                TotalTax = sales.Sum(s => s.TaxAmount),
                TotalDiscount = sales.Sum(s => s.DiscountAmount),
                AverageSaleValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                TopSellingProducts = sales.SelectMany(s => s.Items)
                    .GroupBy(si => si.ProductId)
                    .Select(g => new ProductSalesDto
                    {
                        ProductId = g.Key,
                        ProductName = g.FirstOrDefault()?.ProductName ?? "Unknown",
                        QuantitySold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.TotalPrice)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(10)
                    .ToList(),
                SalesByPeriod = groupedSales.Select(g => new SalesByPeriodDto
                {
                    Period = g.Key.ToString(),
                    SalesCount = g.Count(),
                    Revenue = g.Sum(s => s.TotalAmount)
                }).ToList(),
                PaymentMethods = sales.GroupBy(s => s.PaymentMethod)
                    .Select(g => new PaymentMethodStatsDto
                    {
                        Method = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(s => s.TotalAmount)
                    }).ToList()
            };
        }

        public async Task<InventoryReportDto> GetInventoryReportAsync(Guid tenantId, Guid? branchId = null, string category = null, bool? lowStock = null, bool? expiring = null)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.TenantId == tenantId);

            if (branchId.HasValue)
                query = query.Where(i => i.BranchId == branchId.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(i => i.Product.Category == category);

            if (lowStock == true)
                query = query.Where(i => i.QuantityAvailable <= i.Product.ReorderLevel);

            if (expiring == true)
                query = query.Where(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value <= DateTime.UtcNow.AddDays(90));

            var inventory = await query.ToListAsync();

            return new InventoryReportDto
            {
                BranchId = branchId,
                Category = category,
                LowStockOnly = lowStock,
                ExpiringOnly = expiring,
                TotalProducts = inventory.Count,
                TotalValue = inventory.Sum(i => i.QuantityOnHand * i.CostPrice),
                LowStockItems = inventory.Count(i => i.QuantityAvailable <= i.Product.ReorderLevel),
                ExpiringItems = inventory.Count(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value <= DateTime.UtcNow.AddDays(90)),
                OutOfStockItems = inventory.Count(i => i.QuantityAvailable == 0),
                InventoryItems = inventory.Select(i => new InventoryItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Category = i.Product.Category,
                    QuantityOnHand = i.QuantityOnHand,
                    QuantityReserved = i.QuantityReserved,
                    QuantityAvailable = i.QuantityAvailable,
                    ReorderLevel = i.Product.ReorderLevel,
                    CostPrice = i.CostPrice,
                    SellingPrice = i.SellingPrice,
                    TotalValue = i.QuantityOnHand * i.CostPrice,
                    BatchNumber = i.BatchNumber,
                    ExpiryDate = i.ExpiryDate,
                    Location = i.Location,
                    LastStockUpdate = i.LastStockUpdate
                }).ToList(),
                CategoryBreakdown = inventory.GroupBy(i => i.Product.Category)
                    .Select(g => new CategoryBreakdownDto
                    {
                        Category = g.Key,
                        ProductCount = g.Count(),
                        TotalValue = g.Sum(i => i.QuantityOnHand * i.CostPrice)
                    }).ToList()
            };
        }

        public async Task<PatientsReportDto> GetPatientsReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string groupBy = "month")
        {
            var query = _context.Patients
                .Where(p => p.TenantId == tenantId && 
                           p.CreatedAt >= startDate && 
                           p.CreatedAt <= endDate);

            var patients = await query.ToListAsync();

            var groupedPatients = groupBy.ToLower() switch
            {
                "day" => patients.GroupBy(p => p.CreatedAt.Date),
                "week" => patients.GroupBy(p => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(p.CreatedAt.Date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday)),
                "month" => patients.GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month }),
                "year" => patients.GroupBy(p => p.CreatedAt.Year),
                _ => patients.GroupBy(p => p.CreatedAt.Date)
            };

            return new PatientsReportDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                GroupBy = groupBy,
                TotalPatients = patients.Count,
                NewPatients = patients.Count,
                AgeDistribution = patients.GroupBy(p => CalculateAgeGroup(p.DateOfBirth!))
                    .Select(g => new AgeGroupDto
                    {
                        AgeGroup = g.Key,
                        Count = g.Count()
                    }).ToList(),
                GenderDistribution = patients.GroupBy(p => p.Gender!)
                    .Select(g => new GenderDto
                    {
                        Gender = g.Key,
                        Count = g.Count()
                    }).ToList(),
                PatientsByPeriod = groupedPatients.Select(g => new PatientsByPeriodDto
                {
                    Period = g.Key.ToString(),
                    NewPatients = g.Count()
                }).ToList()
            };
        }

        public async Task<PrescriptionsReportDto> GetPrescriptionsReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string status = null)
        {
            var query = _context.Prescriptions
                .Include(p => p.Items)
                .Include(p => p.Patient)
                .Where(p => p.TenantId == tenantId && 
                           p.CreatedAt >= startDate && 
                           p.CreatedAt <= endDate);

            if (branchId.HasValue)
                query = query.Where(p => p.BranchId == branchId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var prescriptions = await query.ToListAsync();

            return new PrescriptionsReportDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                BranchId = branchId,
                Status = status,
                TotalPrescriptions = prescriptions.Count,
                DispensedPrescriptions = prescriptions.Count(p => p.Status == "dispensed"),
                PendingPrescriptions = prescriptions.Count(p => p.Status == "pending"),
                TotalItems = prescriptions.Sum(p => p.Items.Count),
                TopMedications = prescriptions.SelectMany(p => p.Items)
                    .GroupBy(pi => pi.ProductId)
                    .Select(g => new MedicationStatsDto
                    {
                        ProductId = g.Key,
                        MedicationName = g.FirstOrDefault()?.ProductName ?? "Unknown",
                        PrescriptionCount = g.Count(),
                        TotalQuantity = g.Sum(pi => pi.Quantity)
                    })
                    .OrderByDescending(m => m.PrescriptionCount)
                    .Take(10)
                    .ToList(),
                PrescriptionsByStatus = prescriptions.GroupBy(p => p.Status)
                    .Select(g => new PrescriptionStatusDto
                    {
                        Status = g.Key,
                        Count = g.Count()
                    }).ToList()
            };
        }

        public async Task<FinancialReportDto> GetFinancialReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string reportType = "summary")
        {
            var salesQuery = _context.Sales
                .Where(s => s.TenantId == tenantId && 
                           s.CreatedAt >= startDate && 
                           s.CreatedAt <= endDate);

            if (branchId.HasValue)
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);

            var sales = await salesQuery.ToListAsync();
            var payments = await _context.Payments
                .Where(p => p.TenantId == tenantId && 
                           p.CreatedAt >= startDate && 
                           p.CreatedAt <= endDate &&
                           (branchId.HasValue ? sales.Select(s => s.Id).Contains(p.SaleId) : true))
                .ToListAsync();

            return new FinancialReportDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                BranchId = branchId,
                ReportType = reportType,
                Revenue = new RevenueDto
                {
                    GrossRevenue = sales.Sum(s => s.Subtotal),
                    TaxRevenue = sales.Sum(s => s.TaxAmount),
                    NetRevenue = sales.Sum(s => s.TotalAmount),
                    Discounts = sales.Sum(s => s.DiscountAmount)
                },
                Payments = payments.GroupBy(p => p.PaymentMethod)
                    .Select(g => new PaymentSummaryDto
                    {
                        Method = g.Key,
                        Amount = g.Sum(p => p.Amount),
                        Count = g.Count(),
                        Percentage = sales.Any() ? (g.Sum(p => p.Amount) / sales.Sum(s => s.TotalAmount)) * 100 : 0
                    }).ToList(),
                Profitability = new ProfitabilityDto
                {
                    GrossProfit = sales.Sum(s => s.Subtotal) * 0.3m, // Assuming 30% gross margin
                    NetProfit = sales.Sum(s => s.TotalAmount) * 0.2m, // Assuming 20% net margin
                    ProfitMargin = sales.Any() ? 20 : 0
                }
            };
        }

        public async Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(Guid tenantId, Guid? branchId = null, int? periodDays = 30)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-periodDays ?? 30);

            var salesQuery = _context.Sales
                .Where(s => s.TenantId == tenantId && s.CreatedAt >= startDate);

            if (branchId.HasValue)
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);

            var sales = await salesQuery.ToListAsync();
            var previousPeriodStart = startDate.AddDays(-periodDays ?? -30);
            var previousSales = await _context.Sales
                .Where(s => s.TenantId == tenantId && 
                           s.CreatedAt >= previousPeriodStart && 
                           s.CreatedAt < startDate)
                .ToListAsync();

            return new DashboardAnalyticsDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                BranchId = branchId,
                SalesMetrics = new SalesMetricsDto
                {
                    TotalSales = sales.Count,
                    TotalRevenue = sales.Sum(s => s.TotalAmount),
                    AverageSaleValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                    SalesGrowth = previousSales.Any() ? ((sales.Count - previousSales.Count) / (double)previousSales.Count) * 100 : 0,
                    RevenueGrowth = previousSales.Any() ? ((sales.Sum(s => s.TotalAmount) - previousSales.Sum(s => s.TotalAmount)) / previousSales.Sum(s => s.TotalAmount)) * 100 : 0
                },
                InventoryMetrics = new InventoryMetricsDto
                {
                    TotalProducts = await _context.Products.CountAsync(p => p.TenantId == tenantId && p.DeletedAt == null),
                    LowStockItems = await _context.Inventory.CountAsync(i => i.TenantId == tenantId && i.QuantityAvailable <= 5),
                    OutOfStockItems = await _context.Inventory.CountAsync(i => i.TenantId == tenantId && i.QuantityAvailable == 0),
                    ExpiringItems = await _context.Inventory.CountAsync(i => i.TenantId == tenantId && i.ExpiryDate.HasValue && i.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30))
                },
                PatientMetrics = new PatientMetricsDto
                {
                    TotalPatients = await _context.Patients.CountAsync(p => p.TenantId == tenantId),
                    NewPatients = await _context.Patients.CountAsync(p => p.TenantId == tenantId && p.CreatedAt >= startDate),
                    ActivePatients = await _context.Prescriptions
                        .Where(p => p.TenantId == tenantId && p.CreatedAt >= startDate)
                        .Select(p => p.PatientId)
                        .Distinct()
                        .CountAsync()
                },
                PrescriptionMetrics = new PrescriptionMetricsDto
                {
                    TotalPrescriptions = await _context.Prescriptions.CountAsync(p => p.TenantId == tenantId && p.CreatedAt >= startDate),
                    DispensedPrescriptions = await _context.Prescriptions.CountAsync(p => p.TenantId == tenantId && p.Status == "dispensed" && p.CreatedAt >= startDate),
                    PendingPrescriptions = await _context.Prescriptions.CountAsync(p => p.TenantId == tenantId && p.Status == "pending")
                }
            };
        }

        public async Task<TrendsAnalyticsDto> GetTrendsAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, string metric = "sales")
        {
            var query = metric.ToLower() switch
            {
                "sales" => (IQueryable<object>)_context.Sales.Where(s => s.TenantId == tenantId && s.CreatedAt >= startDate && s.CreatedAt <= endDate),
                "prescriptions" => (IQueryable<object>)_context.Prescriptions.Where(p => p.TenantId == tenantId && p.CreatedAt >= startDate && p.CreatedAt <= endDate),
                "patients" => (IQueryable<object>)_context.Patients.Where(p => p.TenantId == tenantId && p.CreatedAt >= startDate && p.CreatedAt <= endDate),
                _ => (IQueryable<object>)_context.Sales.Where(s => s.TenantId == tenantId && s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            };

            if (branchId.HasValue)
            {
                query = metric.ToLower() switch
                {
                    "sales" => query.OfType<Sale>().Where(s => s.BranchId == branchId.Value),
                    "prescriptions" => query.OfType<Prescription>().Where(p => p.BranchId == branchId.Value),
                    _ => query
                };
            }

            var data = await query.ToListAsync();

            return new TrendsAnalyticsDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                BranchId = branchId,
                Metric = metric,
                DataPoints = data.GroupBy(d => d.CreatedAt.Date)
                    .Select(g => new TrendDataPointDto
                    {
                        Date = g.Key,
                        Value = metric.ToLower() switch
                        {
                            "sales" => (object)g.OfType<Sale>().Sum(s => s.TotalAmount),
                            "prescriptions" => (object)g.OfType<Prescription>().Count(),
                            "patients" => (object)g.OfType<Patient>().Count(),
                            _ => (object)g.Count()
                        }
                    })
                    .OrderBy(dp => dp.Date)
                    .ToList()
            };
        }

        public async Task<byte[]> ExportReportAsync(Guid tenantId, string reportType, DateTime startDate, DateTime endDate, string format = "pdf", Guid? branchId = null)
        {
            // This is a placeholder implementation
            // In a real application, you would use libraries like:
            // - iTextSharp or PdfSharp for PDF generation
            // - EPPlus or ClosedXML for Excel generation
            // - CsvHelper for CSV generation
            
            var reportData = reportType.ToLower() switch
            {
                "sales" => await GetSalesReportAsync(tenantId, startDate, endDate, branchId),
                "inventory" => await GetInventoryReportAsync(tenantId, branchId),
                "patients" => await GetPatientsReportAsync(tenantId, startDate, endDate),
                "prescriptions" => await GetPrescriptionsReportAsync(tenantId, startDate, endDate, branchId),
                "financial" => await GetFinancialReportAsync(tenantId, startDate, endDate, branchId),
                _ => throw new ArgumentException($"Unsupported report type: {reportType}")
            };

            // For now, return a simple placeholder
            var content = $"Report: {reportType}\nPeriod: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}\nGenerated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        public async Task<PerformanceReportDto> GetPerformanceReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, Guid? branchId = null, Guid? userId = null)
        {
            var salesQuery = _context.Sales
                .Where(s => s.TenantId == tenantId && s.CreatedAt >= startDate && s.CreatedAt <= endDate);

            if (branchId.HasValue)
                salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);

            if (userId.HasValue)
                salesQuery = salesQuery.Where(s => s.CashierId == userId.Value);

            var sales = await salesQuery.ToListAsync();

            return new PerformanceReportDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                BranchId = branchId,
                UserId = userId,
                SalesPerformance = new SalesPerformanceDto
                {
                    TotalSales = sales.Count,
                    TotalRevenue = sales.Sum(s => s.TotalAmount),
                    AverageSaleValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                    SalesPerDay = sales.Any() ? sales.Count / (double)(endDate - startDate).Days : 0
                }
            };
        }

        public async Task<AuditReportDto> GetAuditReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string action = null, Guid? userId = null, string entityType = null)
        {
            // This is a placeholder implementation
            // In a real application, you would have an AuditLogs table to track all changes
            
            return new AuditReportDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                Action = action,
                UserId = userId,
                EntityType = entityType,
                TotalAudits = 0,
                AuditEntries = new List<AuditEntryDto>()
            };
        }

        private string CalculateAgeGroup(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return "Unknown";

            var age = DateTime.UtcNow.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value.Date > DateTime.UtcNow.AddYears(-age)) age--;

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
}
