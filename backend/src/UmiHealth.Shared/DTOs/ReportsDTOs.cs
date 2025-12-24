namespace UmiHealth.Shared.DTOs;

public class SalesReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalDiscount { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public List<SalesByPeriodDto> SalesByPeriod { get; set; } = new();
    public List<TopSellingProductDto> TopProducts { get; set; } = new();
    public List<SalesByCategoryDto> SalesByCategories { get; set; } = new();
    public List<SalesByPaymentMethodDto> SalesByPaymentMethods { get; set; } = new();
}

public class SalesByPeriodDto
{
    public DateTime Period { get; set; }
    public decimal Sales { get; set; }
    public int Transactions { get; set; }
    public decimal AverageValue { get; set; }
}

public class TopSellingProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesByCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public int Transactions { get; set; }
    public decimal Percentage { get; set; }
}

public class SalesByPaymentMethodDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Transactions { get; set; }
    public decimal Percentage { get; set; }
}

public class InventoryReportDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockItems { get; set; }
    public int ExpiredItems { get; set; }
    public List<InventoryItemDto> Items { get; set; } = new();
}

public class InventoryItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsExpired { get; set; }
}

public class LowStockReportDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public List<LowStockItemDto> Items { get; set; } = new();
}

public class LowStockItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public int SuggestedOrder { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ExpiryReportDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public List<ExpiringItemDto> Items { get; set; } = new();
}

public class ExpiringItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public decimal Value { get; set; }
}

public class PatientReportDto
{
    public DateTime ReportDate { get; set; }
    public int TotalPatients { get; set; }
    public int NewPatients { get; set; }
    public int ActivePatients { get; set; }
    public List<PatientDemographicsDto> Demographics { get; set; } = new();
    public List<PatientByAgeGroupDto> AgeGroups { get; set; } = new();
    public List<PatientsByInsuranceDto> InsuranceProviders { get; set; } = new();
}

public class PatientDemographicsDto
{
    public string Gender { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class PatientByAgeGroupDto
{
    public string AgeGroup { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class PatientsByInsuranceDto
{
    public string InsuranceProvider { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class DashboardAnalyticsDto
{
    public DateTime LastUpdated { get; set; }
    public SalesSummaryDto TodaySales { get; set; } = new();
    public SalesSummaryDto MonthSales { get; set; } = new();
    public InventorySummaryDto Inventory { get; set; } = new();
    public PatientSummaryDto Patients { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class SalesSummaryDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalTax { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public decimal GrowthPercentage { get; set; }
}

public class InventorySummaryDto
{
    public int TotalProducts { get; set; }
    public int LowStockItems { get; set; }
    public int ExpiredItems { get; set; }
    public decimal TotalValue { get; set; }
}

public class PatientSummaryDto
{
    public int TotalPatients { get; set; }
    public int NewPatientsThisMonth { get; set; }
    public int ActivePrescriptions { get; set; }
}

public class RecentActivityDto
{
    public DateTime Timestamp { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}

public class TrendAnalysisDto
{
    public string Metric { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<TrendDataPointDto> DataPoints { get; set; } = new();
    public TrendSummaryDto Summary { get; set; } = new();
}

public class TrendDataPointDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class TrendSummaryDto
{
    public decimal AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal GrowthRate { get; set; }
    public decimal Volatility { get; set; }
}

public class SalesReportRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? BranchId { get; set; }
    public string? GroupBy { get; set; }
    public string? ProductCategory { get; set; }
    public string? PaymentMethod { get; set; }
}

public class InventoryReportRequestDto
{
    public Guid? BranchId { get; set; }
    public string? Category { get; set; }
    public bool? LowStockOnly { get; set; }
    public bool? ExpiredOnly { get; set; }
}

public class PatientReportRequestDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Gender { get; set; }
    public string? AgeGroup { get; set; }
    public string? BloodType { get; set; }
    public string? InsuranceProvider { get; set; }
}

public class TrendAnalysisRequestDto
{
    public string Metric { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? BranchId { get; set; }
}
