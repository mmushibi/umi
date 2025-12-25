using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    /// <summary>
    /// Analytics and reporting service
    /// Provides comprehensive business intelligence and analytics
    /// </summary>
    public interface IAnalyticsService
    {
        // Sales Analytics
        Task<SalesAnalytics> GetSalesAnalyticsAsync(string tenantId, string branchId, DateTime startDate, DateTime endDate);
        Task<DailyTrendAnalytics> GetDailyTrendsAsync(string tenantId, string branchId, int days = 30);
        Task<ProductPerformanceAnalytics> GetProductPerformanceAsync(string tenantId, string branchId, int topN = 10);
        Task<PaymentMethodAnalytics> GetPaymentMethodAnalyticsAsync(string tenantId, string branchId, DateTime startDate, DateTime endDate);

        // Inventory Analytics
        Task<InventoryAnalytics> GetInventoryAnalyticsAsync(string tenantId, string branchId);
        Task<StockTrendAnalytics> GetStockTrendsAsync(string tenantId, string branchId, int days = 30);
        Task<ExpiryAnalytics> GetExpiryAnalyticsAsync(string tenantId, string branchId);
        Task<InventoryTurnoverAnalytics> GetInventoryTurnoverAsync(string tenantId, string branchId);

        // Patient Analytics
        Task<PatientAnalytics> GetPatientAnalyticsAsync(string tenantId);
        Task<PrescriptionAnalytics> GetPrescriptionAnalyticsAsync(string tenantId, string branchId, int days = 30);

        // Dashboard
        Task<DashboardMetrics> GetDashboardMetricsAsync(string tenantId, string branchId);
        Task<ComparisonAnalytics> CompareBranchesAsync(string tenantId, DateTime startDate, DateTime endDate);

        // Forecasting
        Task<SalesForecast> GetSalesForecastAsync(string tenantId, string branchId, int forecastDays = 30);
        Task<InventoryForecast> GetInventoryForecastAsync(string tenantId, string branchId);
    }

    // Analytics DTOs
    public class SalesAnalytics
    {
        public decimal TotalSales { get; set; }
        public decimal AverageSaleValue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalTax { get; set; }
        public int UniqueCustomers { get; set; }
        public decimal GrossProfitMargin { get; set; }
        public decimal NetProfitMargin { get; set; }
        public Dictionary<string, decimal> SalesByPaymentMethod { get; set; }
        public Dictionary<string, int> SalesByHour { get; set; }
        public Dictionary<string, decimal> SalesByDay { get; set; }
    }

    public class DailyTrendAnalytics
    {
        public List<DailyTrend> Trends { get; set; }
        public decimal AverageDailySales { get; set; }
        public decimal GrowthPercentage { get; set; }
        public string Trend { get; set; } // Upward, Downward, Stable
    }

    public class DailyTrend
    {
        public DateTime Date { get; set; }
        public decimal Sales { get; set; }
        public int Transactions { get; set; }
        public decimal AverageValue { get; set; }
    }

    public class ProductPerformanceAnalytics
    {
        public List<ProductPerformance> TopProducts { get; set; }
        public List<ProductPerformance> BottomProducts { get; set; }
        public decimal AverageProductRevenue { get; set; }
    }

    public class ProductPerformance
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int SalesCount { get; set; }
    }

    public class PaymentMethodAnalytics
    {
        public Dictionary<string, PaymentMethodStats> MethodStats { get; set; }
        public string MostUsedMethod { get; set; }
        public string HighestValueMethod { get; set; }
    }

    public class PaymentMethodStats
    {
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class InventoryAnalytics
    {
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int OverstockedProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal AverageStockLevel { get; set; }
        public decimal FastMovingPercentage { get; set; }
        public decimal SlowMovingPercentage { get; set; }
        public decimal DeadStockPercentage { get; set; }
    }

    public class StockTrendAnalytics
    {
        public List<StockTrend> Trends { get; set; }
        public decimal InventoryTurnovRatio { get; set; }
        public int DaysOfSupply { get; set; }
    }

    public class StockTrend
    {
        public DateTime Date { get; set; }
        public int QuantityOnHand { get; set; }
        public int QuantitySold { get; set; }
        public decimal Value { get; set; }
    }

    public class ExpiryAnalytics
    {
        public int ExpiredProducts { get; set; }
        public int ExpiringInMonth { get; set; }
        public int ExpiringInThreeMonths { get; set; }
        public decimal ExpiredValue { get; set; }
        public List<ExpiringProduct> ExpiringProducts { get; set; }
    }

    public class ExpiringProduct
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public decimal Value { get; set; }
    }

    public class InventoryTurnoverAnalytics
    {
        public decimal AnnualTurnoverRatio { get; set; }
        public int AverageDaysInInventory { get; set; }
        public Dictionary<string, decimal> CategoryTurnover { get; set; }
    }

    public class PatientAnalytics
    {
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public decimal RepeatCustomerPercentage { get; set; }
        public decimal AverageCustomerLifetimeValue { get; set; }
    }

    public class PrescriptionAnalytics
    {
        public int TotalPrescriptions { get; set; }
        public int DispensedPrescriptions { get; set; }
        public int PendingPrescriptions { get; set; }
        public decimal DispensalRate { get; set; }
        public List<PrescriptionTrend> Trends { get; set; }
    }

    public class PrescriptionTrend
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public int Dispensed { get; set; }
    }

    public class DashboardMetrics
    {
        public DateTime ReportDate { get; set; }
        public decimal TodaySales { get; set; }
        public decimal ThisWeekSales { get; set; }
        public decimal ThisMonthSales { get; set; }
        public int TodayTransactions { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringCount { get; set; }
        public int PendingPrescriptions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public Dictionary<string, decimal> TopProducts { get; set; }
        public Dictionary<string, int> SalesByHour { get; set; }
    }

    public class ComparisonAnalytics
    {
        public List<BranchComparison> BranchComparisons { get; set; }
        public BranchComparison TenantAverage { get; set; }
        public string BestPerformingBranch { get; set; }
    }

    public class BranchComparison
    {
        public string BranchId { get; set; }
        public string BranchName { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public int CustomerCount { get; set; }
        public decimal InventoryValue { get; set; }
        public decimal GrowthPercentage { get; set; }
    }

    public class SalesForecast
    {
        public List<ForecastPoint> Forecast { get; set; }
        public decimal ProjectedTotalSales { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public string Trend { get; set; } // Growth, Decline, Stable
    }

    public class ForecastPoint
    {
        public DateTime Date { get; set; }
        public decimal PredictedSales { get; set; }
        public decimal LowerBound { get; set; }
        public decimal UpperBound { get; set; }
    }

    public class InventoryForecast
    {
        public List<StockForecast> ProductForecasts { get; set; }
        public int ProductsLikelyToStockout { get; set; }
        public int ProductsOverstocked { get; set; }
    }

    public class StockForecast
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int CurrentStock { get; set; }
        public int ForecastedConsumption { get; set; }
        public int ProjectedStock { get; set; }
        public bool LikelyToStockout { get; set; }
        public int DaysUntilStockout { get; set; }
    }

    /// <summary>
    /// Analytics service implementation
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(SharedDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SalesAnalytics> GetSalesAnalyticsAsync(
            string tenantId,
            string branchId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                var sales = await _context.Sales
                    .Where(s => s.TenantId == Guid.Parse(tenantId) &&
                               s.BranchId == Guid.Parse(branchId) &&
                               s.CreatedAt >= startDate &&
                               s.CreatedAt <= endDate)
                    .Include(s => s.Items)
                    .Include(s => s.Payments)
                    .ToListAsync();

                var analytics = new SalesAnalytics
                {
                    TotalSales = sales.Sum(s => s.TotalAmount),
                    TotalTransactions = sales.Count,
                    AverageSaleValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                    TotalDiscounts = sales.Sum(s => s.DiscountAmount),
                    TotalTax = sales.Sum(s => s.TaxAmount),
                    UniqueCustomers = sales.Where(s => s.PatientId.HasValue).Select(s => s.PatientId).Distinct().Count()
                };

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales analytics");
                throw;
            }
        }

        public async Task<DailyTrendAnalytics> GetDailyTrendsAsync(
            string tenantId,
            string branchId,
            int days = 30)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var sales = await _context.Sales
                    .Where(s => s.TenantId == Guid.Parse(tenantId) &&
                               s.BranchId == Guid.Parse(branchId) &&
                               s.CreatedAt >= startDate)
                    .ToListAsync();

                var trends = sales
                    .GroupBy(s => s.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyTrend
                    {
                        Date = g.Key,
                        Sales = g.Sum(s => s.TotalAmount),
                        Transactions = g.Count(),
                        AverageValue = g.Average(s => s.TotalAmount)
                    })
                    .ToList();

                return new DailyTrendAnalytics
                {
                    Trends = trends,
                    AverageDailySales = trends.Any() ? trends.Average(t => t.Sales) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily trends");
                throw;
            }
        }

        public async Task<ProductPerformanceAnalytics> GetProductPerformanceAsync(
            string tenantId,
            string branchId,
            int topN = 10)
        {
            try
            {
                var saleItems = await _context.SaleItems
                    .Include(si => si.Product)
                    .Where(si => si.Sale.TenantId == Guid.Parse(tenantId) &&
                               si.Sale.BranchId == Guid.Parse(branchId))
                    .ToListAsync();

                var topProducts = saleItems
                    .GroupBy(si => si.ProductId)
                    .OrderByDescending(g => g.Sum(si => si.TotalPrice))
                    .Take(topN)
                    .Select(g => new ProductPerformance
                    {
                        ProductId = g.Key.ToString(),
                        ProductName = g.First().Product?.Name ?? "Unknown",
                        QuantitySold = g.Sum(si => si.Quantity),
                        Revenue = g.Sum(si => si.TotalPrice)
                    })
                    .ToList();

                return new ProductPerformanceAnalytics
                {
                    TopProducts = topProducts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product performance");
                throw;
            }
        }

        public async Task<PaymentMethodAnalytics> GetPaymentMethodAnalyticsAsync(
            string tenantId,
            string branchId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                var payments = await _context.Payments
                    .Where(p => p.TenantId == Guid.Parse(tenantId) &&
                               p.Sale.BranchId == Guid.Parse(branchId) &&
                               p.CreatedAt >= startDate &&
                               p.CreatedAt <= endDate)
                    .ToListAsync();

                var methodStats = payments
                    .GroupBy(p => p.PaymentMethod)
                    .ToDictionary(
                        g => g.Key,
                        g => new PaymentMethodStats
                        {
                            Count = g.Count(),
                            TotalAmount = g.Sum(p => p.Amount),
                            AverageAmount = g.Average(p => p.Amount)
                        }
                    );

                return new PaymentMethodAnalytics
                {
                    MethodStats = methodStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment method analytics");
                throw;
            }
        }

        public async Task<InventoryAnalytics> GetInventoryAnalyticsAsync(string tenantId, string branchId)
        {
            try
            {
                var inventories = await _context.Inventories
                    .Where(i => i.TenantId == Guid.Parse(tenantId) &&
                               i.BranchId == Guid.Parse(branchId))
                    .ToListAsync();

                var analytics = new InventoryAnalytics
                {
                    TotalProducts = inventories.Count,
                    LowStockProducts = inventories.Count(i => i.QuantityOnHand <= (i.Product?.ReorderLevel ?? 10)),
                    OutOfStockProducts = inventories.Count(i => i.QuantityOnHand == 0),
                    TotalInventoryValue = inventories.Sum(i => i.QuantityOnHand * (i.SellingPrice ?? 0))
                };

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory analytics");
                throw;
            }
        }

        public async Task<StockTrendAnalytics> GetStockTrendsAsync(string tenantId, string branchId, int days = 30)
        {
            return new StockTrendAnalytics { Trends = new List<StockTrend>() };
        }

        public async Task<ExpiryAnalytics> GetExpiryAnalyticsAsync(string tenantId, string branchId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var inventories = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.TenantId == Guid.Parse(tenantId) &&
                               i.BranchId == Guid.Parse(branchId))
                    .ToListAsync();

                var expiringProducts = inventories
                    .Where(i => i.ExpiryDate.HasValue)
                    .Where(i => i.ExpiryDate.Value > now)
                    .Where(i => i.ExpiryDate.Value <= now.AddMonths(3))
                    .Select(i => new ExpiringProduct
                    {
                        ProductName = i.Product?.Name ?? "Unknown",
                        Quantity = i.QuantityOnHand,
                        ExpiryDate = i.ExpiryDate.Value,
                        DaysUntilExpiry = (i.ExpiryDate.Value - now).Days,
                        Value = i.QuantityOnHand * (i.SellingPrice ?? 0)
                    })
                    .ToList();

                return new ExpiryAnalytics
                {
                    ExpiringInMonth = expiringProducts.Count(p => p.DaysUntilExpiry <= 30),
                    ExpiringInThreeMonths = expiringProducts.Count(),
                    ExpiringProducts = expiringProducts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiry analytics");
                throw;
            }
        }

        public async Task<InventoryTurnoverAnalytics> GetInventoryTurnoverAsync(string tenantId, string branchId)
        {
            return new InventoryTurnoverAnalytics { CategoryTurnover = new Dictionary<string, decimal>() };
        }

        public async Task<PatientAnalytics> GetPatientAnalyticsAsync(string tenantId)
        {
            try
            {
                var thisMonth = DateTime.UtcNow.AddMonths(-1);
                var patients = await _context.Patients
                    .Where(p => p.TenantId == Guid.Parse(tenantId))
                    .ToListAsync();

                var analytics = new PatientAnalytics
                {
                    TotalPatients = patients.Count,
                    NewPatientsThisMonth = patients.Count(p => p.CreatedAt >= thisMonth)
                };

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient analytics");
                throw;
            }
        }

        public async Task<PrescriptionAnalytics> GetPrescriptionAnalyticsAsync(
            string tenantId,
            string branchId,
            int days = 30)
        {
            return new PrescriptionAnalytics { Trends = new List<PrescriptionTrend>() };
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync(string tenantId, string branchId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var todaySales = await _context.Sales
                    .Where(s => s.TenantId == Guid.Parse(tenantId) &&
                               s.BranchId == Guid.Parse(branchId) &&
                               s.CreatedAt.Date == today)
                    .ToListAsync();

                var inventory = await _context.Inventories
                    .Where(i => i.TenantId == Guid.Parse(tenantId) &&
                               i.BranchId == Guid.Parse(branchId))
                    .ToListAsync();

                var metrics = new DashboardMetrics
                {
                    ReportDate = DateTime.UtcNow,
                    TodaySales = todaySales.Sum(s => s.TotalAmount),
                    TodayTransactions = todaySales.Count,
                    LowStockCount = inventory.Count(i => i.QuantityOnHand <= 10),
                    AverageTransactionValue = todaySales.Any() ? todaySales.Average(s => s.TotalAmount) : 0
                };

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard metrics");
                throw;
            }
        }

        public async Task<ComparisonAnalytics> CompareBranchesAsync(
            string tenantId,
            DateTime startDate,
            DateTime endDate)
        {
            return new ComparisonAnalytics { BranchComparisons = new List<BranchComparison>() };
        }

        public async Task<SalesForecast> GetSalesForecastAsync(
            string tenantId,
            string branchId,
            int forecastDays = 30)
        {
            return new SalesForecast { Forecast = new List<ForecastPoint>() };
        }

        public async Task<InventoryForecast> GetInventoryForecastAsync(string tenantId, string branchId)
        {
            return new InventoryForecast { ProductForecasts = new List<StockForecast>() };
        }
    }
}
