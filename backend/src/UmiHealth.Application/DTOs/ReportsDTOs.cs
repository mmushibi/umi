using System;
using System.Collections.Generic;

namespace UmiHealth.Application.DTOs
{
    // Sales Report DTOs
    public class SalesReportDto
    {
        public object Period { get; set; } = new();
        public Guid? BranchId { get; set; }
        public string GroupBy { get; set; } = string.Empty;
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal AverageSaleValue { get; set; }
        public List<ProductSalesDto> TopSellingProducts { get; set; } = new();
        public List<SalesByPeriodDto> SalesByPeriod { get; set; } = new();
        public List<PaymentMethodStatsDto> PaymentMethods { get; set; } = new();
    }

    public class ProductSalesDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesByPeriodDto
    {
        public string Period { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentMethodStatsDto
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    // Inventory Report DTOs
    public class InventoryReportDto
    {
        public Guid? BranchId { get; set; }
        public string? Category { get; set; }
        public bool? LowStockOnly { get; set; }
        public bool? ExpiringOnly { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockItems { get; set; }
        public int ExpiringItems { get; set; }
        public int OutOfStockItems { get; set; }
        public List<InventoryItemDto> InventoryItems { get; set; } = new();
        public List<CategoryBreakdownDto> CategoryBreakdown { get; set; } = new();
    }

    public class InventoryItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int QuantityAvailable { get; set; }
        public int ReorderLevel { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Location { get; set; }
        public DateTime LastStockUpdate { get; set; }
    }

    public class CategoryBreakdownDto
    {
        public string Category { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    // Patients Report DTOs
    public class PatientsReportDto
    {
        public object Period { get; set; } = new();
        public string GroupBy { get; set; } = string.Empty;
        public int TotalPatients { get; set; }
        public int NewPatients { get; set; }
        public List<AgeGroupDto> AgeDistribution { get; set; } = new();
        public List<GenderDto> GenderDistribution { get; set; } = new();
        public List<PatientsByPeriodDto> PatientsByPeriod { get; set; } = new();
    }

    public class AgeGroupDto
    {
        public string AgeGroup { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class GenderDto
    {
        public string Gender { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PatientsByPeriodDto
    {
        public string Period { get; set; } = string.Empty;
        public int NewPatients { get; set; }
    }

    // Prescriptions Report DTOs
    public class PrescriptionsReportDto
    {
        public object Period { get; set; } = new();
        public Guid? BranchId { get; set; }
        public string? Status { get; set; }
        public int TotalPrescriptions { get; set; }
        public int DispensedPrescriptions { get; set; }
        public int PendingPrescriptions { get; set; }
        public int TotalItems { get; set; }
        public List<MedicationStatsDto> TopMedications { get; set; } = new();
        public List<PrescriptionStatusDto> PrescriptionsByStatus { get; set; } = new();
    }

    public class MedicationStatsDto
    {
        public Guid ProductId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public int PrescriptionCount { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class PrescriptionStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // Financial Report DTOs
    public class FinancialReportDto
    {
        public object Period { get; set; } = new();
        public Guid? BranchId { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public RevenueDto Revenue { get; set; } = new();
        public List<PaymentSummaryDto> Payments { get; set; } = new();
        public ProfitabilityDto Profitability { get; set; } = new();
    }

    public class RevenueDto
    {
        public decimal GrossRevenue { get; set; }
        public decimal TaxRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal Discounts { get; set; }
    }

    public class PaymentSummaryDto
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class ProfitabilityDto
    {
        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
        public double ProfitMargin { get; set; }
    }

    // Dashboard Analytics DTOs
    public class DashboardAnalyticsDto
    {
        public object Period { get; set; } = new();
        public Guid? BranchId { get; set; }
        public SalesMetricsDto SalesMetrics { get; set; } = new();
        public InventoryMetricsDto InventoryMetrics { get; set; } = new();
        public PatientMetricsDto PatientMetrics { get; set; } = new();
        public PrescriptionMetricsDto PrescriptionMetrics { get; set; } = new();
    }

    public class SalesMetricsDto
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSaleValue { get; set; }
        public double SalesGrowth { get; set; }
        public double RevenueGrowth { get; set; }
    }

    public class InventoryMetricsDto
    {
        public int TotalProducts { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public int ExpiringItems { get; set; }
    }

    public class PatientMetricsDto
    {
        public int TotalPatients { get; set; }
        public int NewPatients { get; set; }
        public int ActivePatients { get; set; }
    }

    public class PrescriptionMetricsDto
    {
        public int TotalPrescriptions { get; set; }
        public int DispensedPrescriptions { get; set; }
        public int PendingPrescriptions { get; set; }
    }

    // Trends Analytics DTOs
    public class TrendsAnalyticsDto
    {
        public object Period { get; set; } = new();
        public Guid? BranchId { get; set; }
        public string Metric { get; set; } = string.Empty;
        public List<TrendDataPointDto> DataPoints { get; set; } = new();
    }

    public class TrendDataPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    // Performance Report DTOs
    public class PerformanceReportDto
    {
        public object Period { get; set; } = new();
        public Guid? BranchId { get; set; }
        public Guid? UserId { get; set; }
        public SalesPerformanceDto SalesPerformance { get; set; } = new();
    }

    public class SalesPerformanceDto
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSaleValue { get; set; }
        public double SalesPerDay { get; set; }
    }

    // Audit Report DTOs
    public class AuditReportDto
    {
        public object Period { get; set; } = new();
        public string? Action { get; set; }
        public Guid? UserId { get; set; }
        public string? EntityType { get; set; }
        public int TotalAudits { get; set; }
        public List<AuditEntryDto> AuditEntries { get; set; } = new();
    }

    public class AuditEntryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public Dictionary<string, object> OldValues { get; set; } = new();
        public Dictionary<string, object> NewValues { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
