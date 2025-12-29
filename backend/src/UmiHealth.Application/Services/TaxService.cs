using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface ITaxService
    {
        Task<decimal> CalculateTaxAsync(Guid tenantId, decimal amount, string taxCategory = "standard");
        Task<TaxCalculation> GetTaxBreakdownAsync(Guid tenantId, decimal amount, string taxCategory = "standard");
        Task<TaxReport> GenerateTaxReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<List<TaxRate>> GetTaxRatesAsync(Guid tenantId);
        Task<TaxRate> UpdateTaxRateAsync(Guid tenantId, string taxCategory, decimal rate);
        Task<List<TaxExemption>> GetTaxExemptionsAsync(Guid tenantId);
        Task<bool> IsTaxExemptAsync(Guid tenantId, Guid customerId);
        Task<TaxFilingReport> GenerateTaxFilingReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string filingPeriod);
    }

    public class TaxService : ITaxService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<TaxService> _logger;

        public TaxService(
            SharedDbContext context,
            ILogger<TaxService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> CalculateTaxAsync(Guid tenantId, decimal amount, string taxCategory = "standard")
        {
            var taxRate = await GetTaxRateAsync(tenantId, taxCategory);
            return amount * taxRate.Rate;
        }

        public async Task<TaxCalculation> GetTaxBreakdownAsync(Guid tenantId, decimal amount, string taxCategory = "standard")
        {
            var taxRate = await GetTaxRateAsync(tenantId, taxCategory);
            var taxAmount = amount * taxRate.Rate;

            return new TaxCalculation
            {
                TenantId = tenantId,
                Amount = amount,
                TaxCategory = taxCategory,
                TaxRate = taxRate.Rate,
                TaxAmount = taxAmount,
                TotalAmount = amount + taxAmount,
                IsTaxExempt = false,
                CalculatedAt = DateTime.UtcNow
            };
        }

        public async Task<TaxReport> GenerateTaxReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed")
                .Include(pt => pt.User)
                .ToListAsync();

            var taxableTransactions = transactions.Where(t => !t.IsTaxExempt).ToList();
            var exemptTransactions = transactions.Where(t => t.IsTaxExempt).ToList();

            var totalRevenue = taxableTransactions.Sum(t => t.Amount);
            var exemptRevenue = exemptTransactions.Sum(t => t.Amount);
            var totalTax = taxableTransactions.Sum(t => t.TaxAmount);

            return new TaxReport
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = totalRevenue,
                ExemptRevenue = exemptRevenue,
                TaxableRevenue = totalRevenue,
                TotalTax = totalTax,
                NetRevenue = totalRevenue + exemptRevenue,
                TaxableTransactions = taxableTransactions.Count,
                ExemptTransactions = exemptTransactions.Count,
                AverageTaxRate = totalRevenue > 0 ? (double)(totalTax / totalRevenue) * 100 : 0,
                GeneratedAt = DateTime.UtcNow,
                TaxBreakdownByCategory = await GetTaxBreakdownByCategoryAsync(tenantId, startDate, endDate),
                MonthlyTaxData = await GetMonthlyTaxDataAsync(tenantId, startDate, endDate)
            };
        }

        public async Task<List<TaxRate>> GetTaxRatesAsync(Guid tenantId)
        {
            return await _context.TaxRates
                .Where(tr => tr.TenantId == tenantId && tr.IsActive)
                .OrderBy(tr => tr.Category)
                .ToListAsync();
        }

        public async Task<TaxRate> UpdateTaxRateAsync(Guid tenantId, string taxCategory, decimal rate)
        {
            var existingRate = await _context.TaxRates
                .FirstOrDefaultAsync(tr => tr.TenantId == tenantId && tr.Category == taxCategory);

            if (existingRate != null)
            {
                existingRate.Rate = rate;
                existingRate.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existingRate = new TaxRate
                {
                    TenantId = tenantId,
                    Category = taxCategory,
                    Rate = rate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.TaxRates.Add(existingRate);
            }

            await _context.SaveChangesAsync();
            return existingRate;
        }

        public async Task<List<TaxExemption>> GetTaxExemptionsAsync(Guid tenantId)
        {
            return await _context.TaxExemptions
                .Where(te => te.TenantId == tenantId && te.IsActive)
                .Include(te => te.Customer)
                .ToListAsync();
        }

        public async Task<bool> IsTaxExemptAsync(Guid tenantId, Guid customerId)
        {
            return await _context.TaxExemptions
                .AnyAsync(te => te.TenantId == tenantId && 
                               te.CustomerId == customerId && 
                               te.IsActive &&
                               te.ExpiryDate >= DateTime.UtcNow);
        }

        public async Task<TaxFilingReport> GenerateTaxFilingReportAsync(Guid tenantId, DateTime startDate, DateTime endDate, string filingPeriod)
        {
            var report = await GenerateTaxReportAsync(tenantId, startDate, endDate);
            
            return new TaxFilingReport
            {
                TenantId = tenantId,
                FilingPeriod = filingPeriod,
                StartDate = startDate,
                EndDate = endDate,
                FilingDate = DateTime.UtcNow,
                TotalRevenue = report.TotalRevenue,
                TotalTax = report.TotalTax,
                TaxableTransactions = report.TaxableTransactions,
                FilingStatus = "ready",
                TaxBreakdown = report.TaxBreakdownByCategory,
                SupportingDocuments = await GetSupportingDocumentsAsync(tenantId, startDate, endDate)
            };
        }

        private async Task<TaxRate> GetTaxRateAsync(Guid tenantId, string taxCategory)
        {
            var taxRate = await _context.TaxRates
                .FirstOrDefaultAsync(tr => tr.TenantId == tenantId && 
                                         tr.Category == taxCategory && 
                                         tr.IsActive);

            return taxRate ?? new TaxRate { Rate = 0.16m }; // Default 16% tax rate
        }

        private async Task<List<TaxCategoryBreakdown>> GetTaxBreakdownByCategoryAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed" && !pt.IsTaxExempt)
                .ToListAsync();

            return transactions
                .GroupBy(t => t.TaxCategory)
                .Select(g => new TaxCategoryBreakdown
                {
                    Category = g.Key,
                    Revenue = g.Sum(t => t.Amount),
                    TaxAmount = g.Sum(t => t.TaxAmount),
                    TransactionCount = g.Count(),
                    AverageTaxRate = g.Any() ? (double)(g.Sum(t => t.TaxAmount) / g.Sum(t => t.Amount)) * 100 : 0
                })
                .ToList();
        }

        private async Task<List<MonthlyTaxData>> GetMonthlyTaxDataAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed")
                .ToListAsync();

            return transactions
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Select(g => new MonthlyTaxData
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(t => t.Amount),
                    TaxAmount = g.Sum(t => t.TaxAmount),
                    TransactionCount = g.Count(),
                    TaxableTransactions = g.Count(t => !t.IsTaxExempt)
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();
        }

        private async Task<List<SupportingDocument>> GetSupportingDocumentsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            // This would typically fetch actual documents from storage
            return new List<SupportingDocument>
            {
                new SupportingDocument
                {
                    Type = "PaymentTransactions",
                    Description = "All payment transactions for the period",
                    GeneratedAt = DateTime.UtcNow
                },
                new SupportingDocument
                {
                    Type = "TaxExemptions",
                    Description = "List of tax-exempt transactions",
                    GeneratedAt = DateTime.UtcNow
                }
            };
        }
    }

    // Supporting DTOs and Entities
    public class TaxCalculation
    {
        public Guid TenantId { get; set; }
        public decimal Amount { get; set; }
        public string TaxCategory { get; set; } = "standard";
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsTaxExempt { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class TaxReport
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ExemptRevenue { get; set; }
        public decimal TaxableRevenue { get; set; }
        public decimal TotalTax { get; set; }
        public decimal NetRevenue { get; set; }
        public int TaxableTransactions { get; set; }
        public int ExemptTransactions { get; set; }
        public double AverageTaxRate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<TaxCategoryBreakdown> TaxBreakdownByCategory { get; set; } = new();
        public List<MonthlyTaxData> MonthlyTaxData { get; set; } = new();
    }

    public class TaxCategoryBreakdown
    {
        public string Category { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal TaxAmount { get; set; }
        public int TransactionCount { get; set; }
        public double AverageTaxRate { get; set; }
    }

    public class MonthlyTaxData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public decimal TaxAmount { get; set; }
        public int TransactionCount { get; set; }
        public int TaxableTransactions { get; set; }
    }

    public class TaxFilingReport
    {
        public Guid TenantId { get; set; }
        public string FilingPeriod { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime FilingDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalTax { get; set; }
        public int TaxableTransactions { get; set; }
        public string FilingStatus { get; set; } = string.Empty;
        public List<TaxCategoryBreakdown> TaxBreakdown { get; set; } = new();
        public List<SupportingDocument> SupportingDocuments { get; set; } = new();
    }

    public class SupportingDocument
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    // Entity classes (would typically be in separate files)
    public class TaxRate : TenantEntity
    {
        public string Category { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Description { get; set; } = string.Empty;
    }

    public class TaxExemption : TenantEntity
    {
        public Guid CustomerId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string CertificateNumber { get; set; } = string.Empty;
        
        public virtual Customer Customer { get; set; } = null!;
    }
}
