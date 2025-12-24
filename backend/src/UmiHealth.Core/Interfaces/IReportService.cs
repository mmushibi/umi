namespace UmiHealth.Core.Interfaces;

public interface IReportService
{
    // Sales Reports
    Task<SalesReport> GetSalesReportAsync(Guid tenantId, SalesReportRequest request, CancellationToken cancellationToken = default);
    Task<SalesSummary> GetSalesSummaryAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // Inventory Reports
    Task<InventoryReport> GetInventoryReportAsync(Guid tenantId, Guid branchId, InventoryReportRequest request, CancellationToken cancellationToken = default);
    Task<LowStockReport> GetLowStockReportAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
    Task<ExpiryReport> GetExpiryReportAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);

    // Patient Reports
    Task<PatientReport> GetPatientReportAsync(Guid tenantId, PatientReportRequest request, CancellationToken cancellationToken = default);

    // Analytics
    Task<DashboardAnalytics> GetDashboardAnalyticsAsync(Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
    Task<TrendAnalysis> GetTrendAnalysisAsync(Guid tenantId, TrendAnalysisRequest request, CancellationToken cancellationToken = default);
}

public record SalesReportRequest(
    DateTime StartDate,
    DateTime EndDate,
    Guid? BranchId,
    string? GroupBy, // Day, Week, Month, Year
    string? ProductCategory,
    string? PaymentMethod
);

public record SalesReport(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalSales,
    decimal TotalTax,
    decimal TotalDiscount,
    int TotalTransactions,
    decimal AverageTransactionValue,
    List<SalesByPeriod> SalesByPeriod,
    List<TopSellingProduct> TopProducts,
    List<SalesByCategory> SalesByCategories,
    List<SalesByPaymentMethod> SalesByPaymentMethods
);

public record SalesByPeriod(
    DateTime Period,
    decimal Sales,
    int Transactions,
    decimal AverageValue
);

public record TopSellingProduct(
    string ProductName,
    int QuantitySold,
    decimal Revenue
);

public record SalesByCategory(
    string Category,
    decimal Sales,
    int Transactions,
    decimal Percentage
);

public record SalesByPaymentMethod(
    string PaymentMethod,
    decimal Amount,
    int Transactions,
    decimal Percentage
);

public record SalesSummary(
    decimal TotalSales,
    decimal TotalTax,
    int TotalTransactions,
    decimal AverageTransactionValue,
    decimal GrowthPercentage
);

public record InventoryReportRequest(
    Guid? BranchId,
    string? Category,
    bool? LowStockOnly,
    bool? ExpiredOnly
);

public record InventoryReport(
    Guid BranchId,
    string BranchName,
    DateTime ReportDate,
    int TotalProducts,
    int TotalQuantity,
    decimal TotalValue,
    int LowStockItems,
    int ExpiredItems,
    List<InventoryItem> Items
);

public record InventoryItem(
    string ProductName,
    string Category,
    int QuantityOnHand,
    int ReorderLevel,
    decimal UnitPrice,
    decimal TotalValue,
    DateTime? ExpiryDate,
    bool IsLowStock,
    bool IsExpired
);

public record LowStockReport(
    Guid BranchId,
    string BranchName,
    DateTime ReportDate,
    List<LowStockItem> Items
);

public record LowStockItem(
    string ProductName,
    int CurrentStock,
    int ReorderLevel,
    int SuggestedOrder,
    decimal UnitPrice
);

public record ExpiryReport(
    Guid BranchId,
    string BranchName,
    DateTime ReportDate,
    List<ExpiringItem> Items
);

public record ExpiringItem(
    string ProductName,
    int Quantity,
    DateTime ExpiryDate,
    int DaysUntilExpiry,
    decimal Value
);

public record PatientReportRequest(
    DateTime? StartDate,
    DateTime? EndDate,
    string? Gender,
    string? AgeGroup,
    string? BloodType,
    string? InsuranceProvider
);

public record PatientReport(
    DateTime ReportDate,
    int TotalPatients,
    int NewPatients,
    int ActivePatients,
    List<PatientDemographics> Demographics,
    List<PatientByAgeGroup> AgeGroups,
    List<PatientsByInsurance> InsuranceProviders
);

public record PatientDemographics(
    string Gender,
    int Count,
    decimal Percentage
);

public record PatientByAgeGroup(
    string AgeGroup,
    int Count,
    decimal Percentage
);

public record PatientsByInsurance(
    string InsuranceProvider,
    int Count,
    decimal Percentage
);

public record DashboardAnalytics(
    DateTime LastUpdated,
    SalesSummary TodaySales,
    SalesSummary MonthSales,
    InventorySummary Inventory,
    PatientSummary Patients,
    List<RecentActivity> RecentActivities
);

public record InventorySummary(
    int TotalProducts,
    int LowStockItems,
    int ExpiredItems,
    decimal TotalValue
);

public record PatientSummary(
    int TotalPatients,
    int NewPatientsThisMonth,
    int ActivePrescriptions
);

public record RecentActivity(
    DateTime Timestamp,
    string ActivityType,
    string Description,
    string User
);

public record TrendAnalysisRequest(
    string Metric, // Sales, Patients, Prescriptions, Inventory
    string Period, // Daily, Weekly, Monthly, Yearly
    DateTime StartDate,
    DateTime EndDate,
    Guid? BranchId
);

public record TrendAnalysis(
    string Metric,
    string Period,
    DateTime StartDate,
    DateTime EndDate,
    List<TrendDataPoint> DataPoints,
    TrendSummary Summary
);

public record TrendDataPoint(
    DateTime Date,
    decimal Value,
    int Count
);

public record TrendSummary(
    decimal AverageValue,
    decimal MinValue,
    decimal MaxValue,
    decimal GrowthRate,
    decimal Volatility
);
