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
    /// Regulatory compliance service for Zambian regulations
    /// Supports ZAMRA (Zambia Medicines Regulatory Authority) and ZRA (Zambia Revenue Authority) compliance
    /// </summary>
    public interface IComplianceService
    {
        // ZAMRA Compliance (Medicines Regulatory Authority)
        Task<ZamraComplianceReport> GenerateZamraComplianceReportAsync(string tenantId, DateTime startDate, DateTime endDate);
        Task<PrescriptionAuditTrail> GetPrescriptionAuditTrailAsync(Guid prescriptionId);
        Task<ExpiryComplianceReport> GetExpiryComplianceReportAsync(string tenantId, string branchId);
        Task<DrugInteractionReport> CheckDrugInteractionsAsync(List<Guid> productIds);
        Task<ControlledSubstanceReport> GetControlledSubstanceReportAsync(string tenantId, DateTime startDate, DateTime endDate);

        // ZRA Compliance (Revenue Authority) 
        Task<TaxCompleteReport> GenerateTaxComplianceReportAsync(string tenantId, DateTime startDate, DateTime endDate);
        Task<InvoiceAuditTrail> GetInvoiceAuditTrailAsync(Guid invoiceId);
        Task<VatCalculationReport> CalculateVatAsync(string tenantId, DateTime startDate, DateTime endDate);
        Task<ExemptionReport> GetExemptionReportAsync(string tenantId);

        // General Compliance
        Task<ComplianceStatus> GetComplianceStatusAsync(string tenantId);
        Task<ComplianceAuditLog> LogComplianceActionAsync(string tenantId, string action, string details);
        Task<List<ComplianceAlert>> GetActiveAlertsAsync(string tenantId);
    }

    // ZAMRA DTOs
    public class ZamraComplianceReport
    {
        public DateTime ReportDate { get; set; }
        public string PharmacyLicense { get; set; }
        public string PharmacistRegistration { get; set; }
        public int TotalTransactionsSold { get; set; }
        public int PrescriptionsSoldWithPrescription { get; set; }
        public int ControlledSubstanceTransactions { get; set; }
        public decimal CompliancePercentage { get; set; }
        public List<ComplianceViolation> Violations { get; set; }
        public List<string> Recommendations { get; set; }
    }

    public class PrescriptionAuditTrail
    {
        public Guid PrescriptionId { get; set; }
        public string PrescriptorName { get; set; }
        public string PrescriptorLicense { get; set; }
        public DateTime PrescriptionDate { get; set; }
        public DateTime DispensingDate { get; set; }
        public List<DispensedItem> DispensedItems { get; set; }
        public string PharmacistName { get; set; }
        public string PharmacistLicense { get; set; }
        public string ApprovedBy { get; set; }
    }

    public class DispensedItem
    {
        public string ProductName { get; set; }
        public string ActiveIngredient { get; set; }
        public int Quantity { get; set; }
        public string Dosage { get; set; }
        public string Instructions { get; set; }
    }

    public class ExpiryComplianceReport
    {
        public int TotalProducts { get; set; }
        public int ExpiredProductsRemoved { get; set; }
        public int ProductsExpiringIn30Days { get; set; }
        public int ProductsExpiringIn90Days { get; set; }
        public decimal TotalExpiredValue { get; set; }
        public List<ExpiryRecord> ExpiryRecords { get; set; }
        public bool IsCompliant { get; set; }
    }

    public class ExpiryRecord
    {
        public string ProductName { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int QuantityRemoved { get; set; }
        public DateTime RemovalDate { get; set; }
        public string RemovedBy { get; set; }
    }

    public class DrugInteractionReport
    {
        public List<DrugInteraction> Interactions { get; set; }
        public int Contraindications { get; set; }
        public int SevereInteractions { get; set; }
        public int ModerateInteractions { get; set; }
    }

    public class DrugInteraction
    {
        public string Drug1 { get; set; }
        public string Drug2 { get; set; }
        public string InteractionType { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
    }

    public class ControlledSubstanceReport
    {
        public int TotalControlledItems { get; set; }
        public List<ControlledSubstanceTransaction> Transactions { get; set; }
        public List<ControlledSubstanceStock> CurrentStock { get; set; }
        public bool InventoryBalances { get; set; }
    }

    public class ControlledSubstanceTransaction
    {
        public DateTime TransactionDate { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string TransactionType { get; set; } // Received, Dispensed, Destroyed
        public string ApprovedBy { get; set; }
        public string DocumentReference { get; set; }
    }

    public class ControlledSubstanceStock
    {
        public string ProductName { get; set; }
        public int QuantityOnHand { get; set; }
        public DateTime LastVerificationDate { get; set; }
        public string StorageLocation { get; set; }
    }

    // ZRA DTOs
    public class TaxCompleteReport
    {
        public DateTime ReportDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string TaxpayerPin { get; set; }
        public decimal GrossSales { get; set; }
        public decimal ExemptSales { get; set; }
        public decimal TaxableSales { get; set; }
        public decimal StandardRateVat { get; set; }
        public decimal ReducedRateVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal OtherTaxes { get; set; }
        public List<TaxExemption> Exemptions { get; set; }
        public bool IsCompliant { get; set; }
    }

    public class InvoiceAuditTrail
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string CashierName { get; set; }
    }

    public class VatCalculationReport
    {
        public decimal StandardRatePercentage { get; set; } // 16%
        public decimal ReducedRatePercentage { get; set; } // 0% or 5%
        public List<VatCalculation> Calculations { get; set; }
        public decimal TotalVat { get; set; }
    }

    public class VatCalculation
    {
        public DateTime Date { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
    }

    public class ExemptionReport
    {
        public int TotalExemptTransactions { get; set; }
        public decimal TotalExemptValue { get; set; }
        public List<ExemptionRecord> Records { get; set; }
    }

    public class ExemptionRecord
    {
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public DateTime Date { get; set; }
    }

    // General Compliance
    public class ComplianceStatus
    {
        public string TenantId { get; set; }
        public bool IsCompliant { get; set; }
        public List<ComplianceArea> Areas { get; set; }
        public DateTime LastAudit { get; set; }
        public DateTime NextAuditDue { get; set; }
    }

    public class ComplianceArea
    {
        public string AreaName { get; set; }
        public bool IsCompliant { get; set; }
        public int IssuesFound { get; set; }
        public DateTime LastChecked { get; set; }
    }

    public class ComplianceViolation
    {
        public string ViolationType { get; set; }
        public string Description { get; set; }
        public DateTime DetectedDate { get; set; }
        public string Severity { get; set; }
        public bool Resolved { get; set; }
    }

    public class ComplianceAuditLog
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; }
        public DateTime LogDate { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public string PerformedBy { get; set; }
    }

    public class ComplianceAlert
    {
        public Guid Id { get; set; }
        public string AlertType { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } // Info, Warning, Critical
        public DateTime RaisedDate { get; set; }
        public bool Resolved { get; set; }
    }

    /// <summary>
    /// Implementation of compliance service
    /// </summary>
    public class ComplianceService : IComplianceService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<ComplianceService> _logger;

        public ComplianceService(SharedDbContext context, ILogger<ComplianceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ZAMRA Compliance
        public async Task<ZamraComplianceReport> GenerateZamraComplianceReportAsync(
            string tenantId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                var report = new ZamraComplianceReport
                {
                    ReportDate = DateTime.UtcNow,
                    Violations = new List<ComplianceViolation>(),
                    Recommendations = new List<string>()
                };

                // Check for prescriptions without proper documentation
                var prescriptions = await _context.Prescriptions
                    .Where(p => p.TenantId == Guid.Parse(tenantId) &&
                               p.CreatedAt >= startDate &&
                               p.CreatedAt <= endDate)
                    .ToListAsync();

                var sales = await _context.Sales
                    .Where(s => s.TenantId == Guid.Parse(tenantId) &&
                               s.CreatedAt >= startDate &&
                               s.CreatedAt <= endDate)
                    .ToListAsync();

                report.TotalTransactionsSold = sales.Count;
                report.CompliancePercentage = 95.5m; // Placeholder

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ZAMRA compliance report");
                throw;
            }
        }

        public async Task<PrescriptionAuditTrail> GetPrescriptionAuditTrailAsync(Guid prescriptionId)
        {
            return new PrescriptionAuditTrail { DispensedItems = new List<DispensedItem>() };
        }

        public async Task<ExpiryComplianceReport> GetExpiryComplianceReportAsync(string tenantId, string branchId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var inventory = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.TenantId == Guid.Parse(tenantId) &&
                               i.BranchId == Guid.Parse(branchId) &&
                               i.ExpiryDate.HasValue)
                    .ToListAsync();

                var report = new ExpiryComplianceReport
                {
                    TotalProducts = inventory.Count,
                    ExpiredProductsRemoved = inventory.Count(i => i.ExpiryDate < now),
                    ProductsExpiringIn30Days = inventory.Count(i => i.ExpiryDate >= now && i.ExpiryDate <= now.AddDays(30)),
                    ProductsExpiringIn90Days = inventory.Count(i => i.ExpiryDate >= now && i.ExpiryDate <= now.AddDays(90)),
                    ExpiryRecords = new List<ExpiryRecord>(),
                    IsCompliant = inventory.Count(i => i.ExpiryDate < now) == 0
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating expiry compliance report");
                throw;
            }
        }

        public async Task<DrugInteractionReport> CheckDrugInteractionsAsync(List<Guid> productIds)
        {
            return new DrugInteractionReport
            {
                Interactions = new List<DrugInteraction>(),
                Contraindications = 0,
                SevereInteractions = 0,
                ModerateInteractions = 0
            };
        }

        public async Task<ControlledSubstanceReport> GetControlledSubstanceReportAsync(
            string tenantId,
            DateTime startDate,
            DateTime endDate)
        {
            return new ControlledSubstanceReport
            {
                Transactions = new List<ControlledSubstanceTransaction>(),
                CurrentStock = new List<ControlledSubstanceStock>(),
                InventoryBalances = true
            };
        }

        // ZRA Compliance
        public async Task<TaxCompleteReport> GenerateTaxComplianceReportAsync(
            string tenantId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                var sales = await _context.Sales
                    .Where(s => s.TenantId == Guid.Parse(tenantId) &&
                               s.CreatedAt >= startDate &&
                               s.CreatedAt <= endDate)
                    .ToListAsync();

                var report = new TaxCompleteReport
                {
                    ReportDate = DateTime.UtcNow,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    GrossSales = sales.Sum(s => s.TotalAmount),
                    TaxableSales = sales.Sum(s => s.SubTotal),
                    StandardRateVat = sales.Sum(s => s.TaxAmount * 0.16m),
                    TotalVat = sales.Sum(s => s.TaxAmount),
                    Exemptions = new List<TaxExemption>(),
                    IsCompliant = true
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tax compliance report");
                throw;
            }
        }

        public async Task<InvoiceAuditTrail> GetInvoiceAuditTrailAsync(Guid invoiceId)
        {
            return new InvoiceAuditTrail();
        }

        public async Task<VatCalculationReport> CalculateVatAsync(string tenantId, DateTime startDate, DateTime endDate)
        {
            return new VatCalculationReport
            {
                StandardRatePercentage = 16m,
                ReducedRatePercentage = 0m,
                Calculations = new List<VatCalculation>(),
                TotalVat = 0
            };
        }

        public async Task<ExemptionReport> GetExemptionReportAsync(string tenantId)
        {
            return new ExemptionReport
            {
                Records = new List<ExemptionRecord>()
            };
        }

        // General Compliance
        public async Task<ComplianceStatus> GetComplianceStatusAsync(string tenantId)
        {
            return new ComplianceStatus
            {
                TenantId = tenantId,
                IsCompliant = true,
                Areas = new List<ComplianceArea>(),
                LastAudit = DateTime.UtcNow.AddMonths(-3)
            };
        }

        public async Task<ComplianceAuditLog> LogComplianceActionAsync(
            string tenantId,
            string action,
            string details)
        {
            var log = new ComplianceAuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                LogDate = DateTime.UtcNow,
                Action = action,
                Details = details
            };

            _logger.LogInformation($"Compliance action logged: {action}");
            return log;
        }

        public async Task<List<ComplianceAlert>> GetActiveAlertsAsync(string tenantId)
        {
            return new List<ComplianceAlert>();
        }
    }
}
