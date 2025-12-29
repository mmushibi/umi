using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IAdvancedReportingService
    {
        Task<PaymentAnalyticsReport> GeneratePaymentAnalyticsReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<byte[]> ExportToPdfAsync(PaymentAnalyticsReport report);
        Task<byte[]> ExportToExcelAsync(PaymentAnalyticsReport report);
        Task<List<PaymentTrendDto>> GetPaymentTrendsAsync(Guid tenantId, int months);
        Task<List<RevenueByPeriodDto>> GetRevenueByPeriodAsync(Guid tenantId, DateTime startDate, DateTime endDate, string period);
        Task<TaxReportDto> GenerateTaxReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<CustomerPaymentReportDto> GenerateCustomerPaymentReportAsync(Guid tenantId, Guid customerId, DateTime startDate, DateTime endDate);
        Task<RefundReportDto> GenerateRefundReportAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<PaymentMethodPerformanceDto> GetPaymentMethodPerformanceAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<List<FailedPaymentAnalysisDto>> AnalyzeFailedPaymentsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }

    public class AdvancedReportingService : IAdvancedReportingService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<AdvancedReportingService> _logger;

        public AdvancedReportingService(
            SharedDbContext context,
            ILogger<AdvancedReportingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaymentAnalyticsReport> GeneratePaymentAnalyticsReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate)
                .Include(pt => pt.User)
                .Include(pt => pt.Charge)
                .ToListAsync();

            var completedTransactions = transactions.Where(t => t.Status == "completed").ToList();
            var refundedTransactions = transactions.Where(t => t.Status == "refunded").ToList();
            var failedTransactions = transactions.Where(t => t.Status == "failed").ToList();

            var totalRevenue = completedTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalRefunds = refundedTransactions.Sum(t => Math.Abs(t.Amount));
            var netRevenue = totalRevenue - totalRefunds;

            var report = new PaymentAnalyticsReport
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                GeneratedAt = DateTime.UtcNow,
                
                // Summary Metrics
                TotalRevenue = totalRevenue,
                TotalRefunds = totalRefunds,
                NetRevenue = netRevenue,
                TotalTransactions = completedTransactions.Count,
                AverageTransactionValue = completedTransactions.Any() ? totalRevenue / completedTransactions.Count : 0,
                
                // Payment Method Distribution
                PaymentMethodDistribution = completedTransactions
                    .GroupBy(t => t.PaymentMethod)
                    .Select(g => new PaymentMethodDistribution
                    {
                        PaymentMethod = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(t => t.Amount),
                        Percentage = totalRevenue > 0 ? (g.Sum(t => t.Amount) / totalRevenue) * 100 : 0,
                        AverageAmount = g.Average(t => t.Amount)
                    })
                    .OrderByDescending(d => d.Amount)
                    .ToList(),
                
                // Daily Revenue Trends
                DailyRevenue = completedTransactions
                    .GroupBy(t => t.TransactionDate.Date)
                    .Select(g => new DailyRevenueDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        AverageTransactionValue = g.Average(t => t.Amount),
                        PaymentMethodBreakdown = g.GroupBy(t => t.PaymentMethod)
                            .ToDictionary(x => x.Key, x => x.Sum(t => t.Amount))
                    })
                    .OrderBy(d => d.Date)
                    .ToList(),
                
                // Top Customers
                TopCustomers = completedTransactions
                    .GroupBy(t => new { t.UserId, t.User.FirstName, t.User.LastName, t.User.Email })
                    .Select(g => new CustomerPaymentSummary
                    {
                        CustomerId = g.Key.UserId,
                        CustomerName = $"{g.Key.FirstName} {g.Key.LastName}",
                        CustomerEmail = g.Key.Email,
                        TotalAmount = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        AverageAmount = g.Average(t => t.Amount),
                        LastPaymentDate = g.Max(t => t.TransactionDate)
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .Take(20)
                    .ToList(),
                
                // Failed Payments Analysis
                FailedPayments = failedTransactions.Select(t => new FailedPaymentDto
                {
                    TransactionId = t.Id.ToString(),
                    FailedAt = t.TransactionDate,
                    Amount = t.Amount,
                    PaymentMethod = t.PaymentMethod,
                    FailureReason = "Transaction failed", // Would come from actual failure reason field
                    CustomerName = $"{t.User.FirstName} {t.User.LastName}",
                    PhoneNumber = t.User.PhoneNumber
                }).ToList(),
                
                // Refunds Analysis
                Refunds = refundedTransactions.Select(t => new RefundAnalyticsDto
                {
                    RefundId = t.Id.ToString(),
                    RefundDate = t.TransactionDate,
                    RefundAmount = Math.Abs(t.Amount),
                    OriginalTransactionId = t.TransactionReference,
                    Reason = t.RefundReason ?? "No reason provided",
                    PaymentMethod = t.PaymentMethod,
                    CustomerName = $"{t.User.FirstName} {t.User.LastName}"
                }).ToList()
            };

            return report;
        }

        public async Task<byte[]> ExportToPdfAsync(PaymentAnalyticsReport report)
        {
            // This would typically use a PDF library like iTextSharp or PdfSharp
            // For now, we'll create a simple text representation
            var sb = new StringBuilder();
            
            sb.AppendLine("PAYMENT ANALYTICS REPORT");
            sb.AppendLine($"Tenant ID: {report.TenantId}");
            sb.AppendLine($"Period: {report.StartDate:dd MMM yyyy} - {report.EndDate:dd MMM yyyy}");
            sb.AppendLine($"Generated: {report.GeneratedAt:dd MMM yyyy HH:mm:ss}");
            sb.AppendLine(new string('=', 50));
            
            sb.AppendLine("\nSUMMARY METRICS");
            sb.AppendLine($"Total Revenue: {report.TotalRevenue:C}");
            sb.AppendLine($"Total Refunds: {report.TotalRefunds:C}");
            sb.AppendLine($"Net Revenue: {report.NetRevenue:C}");
            sb.AppendLine($"Total Transactions: {report.TotalTransactions:N0}");
            sb.AppendLine($"Average Transaction Value: {report.AverageTransactionValue:C}");
            
            sb.AppendLine("\nPAYMENT METHOD DISTRIBUTION");
            foreach (var method in report.PaymentMethodDistribution)
            {
                sb.AppendLine($"{method.PaymentMethod}: {method.Count:N0} transactions, {method.Amount:C} ({method.Percentage:F1}%)");
            }
            
            sb.AppendLine("\nTOP CUSTOMERS");
            foreach (var customer in report.TopCustomers.Take(10))
            {
                sb.AppendLine($"{customer.CustomerName}: {customer.TotalAmount:C} ({customer.TransactionCount:N0} transactions)");
            }
            
            sb.AppendLine("\nDAILY REVENUE SUMMARY");
            foreach (var daily in report.DailyRevenue.Take(30))
            {
                sb.AppendLine($"{daily.Date:dd MMM}: {daily.Revenue:C} ({daily.TransactionCount:N0} transactions)");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportToExcelAsync(PaymentAnalyticsReport report)
        {
            var csv = new StringBuilder();
            
            // Summary sheet
            csv.AppendLine("METRIC,VALUE");
            csv.AppendLine($"Total Revenue,{report.TotalRevenue}");
            csv.AppendLine($"Total Refunds,{report.TotalRefunds}");
            csv.AppendLine($"Net Revenue,{report.NetRevenue}");
            csv.AppendLine($"Total Transactions,{report.TotalTransactions}");
            csv.AppendLine($"Average Transaction Value,{report.AverageTransactionValue}");
            csv.AppendLine();
            
            // Payment Methods sheet
            csv.AppendLine("PAYMENT METHOD,TRANSACTIONS,AMOUNT,PERCENTAGE,AVERAGE AMOUNT");
            foreach (var method in report.PaymentMethodDistribution)
            {
                csv.AppendLine($"{method.PaymentMethod},{method.Count},{method.Amount},{method.Percentage:F2},{method.AverageAmount}");
            }
            csv.AppendLine();
            
            // Daily Revenue sheet
            csv.AppendLine("DATE,REVENUE,TRANSACTIONS,AVERAGE AMOUNT");
            foreach (var daily in report.DailyRevenue)
            {
                csv.AppendLine($"{daily.Date:yyyy-MM-dd},{daily.Revenue},{daily.TransactionCount},{daily.AverageTransactionValue}");
            }
            csv.AppendLine();
            
            // Top Customers sheet
            csv.AppendLine("CUSTOMER NAME,EMAIL,TOTAL AMOUNT,TRANSACTIONS,AVERAGE AMOUNT,LAST PAYMENT");
            foreach (var customer in report.TopCustomers)
            {
                csv.AppendLine($"{customer.CustomerName},{customer.CustomerEmail},{customer.TotalAmount},{customer.TransactionCount},{customer.AverageAmount},{customer.LastPaymentDate:yyyy-MM-dd}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<List<PaymentTrendDto>> GetPaymentTrendsAsync(Guid tenantId, int months)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);
            var endDate = DateTime.UtcNow;

            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed")
                .ToListAsync();

            var trends = new List<PaymentTrendDto>();

            for (int i = 0; i < months; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthTransactions = transactions
                    .Where(t => t.TransactionDate.Date >= monthStart.Date && t.TransactionDate.Date <= monthEnd.Date)
                    .ToList();

                trends.Add(new PaymentTrendDto
                {
                    Period = monthStart.ToString("MMM yyyy"),
                    Year = monthStart.Year,
                    Month = monthStart.Month,
                    TotalAmount = monthTransactions.Sum(t => t.Amount),
                    TransactionCount = monthTransactions.Count,
                    AverageAmount = monthTransactions.Any() ? 
                        monthTransactions.Average(t => t.Amount) : 0
                });
            }

            return trends;
        }

        public async Task<List<RevenueByPeriodDto>> GetRevenueByPeriodAsync(Guid tenantId, DateTime startDate, DateTime endDate, string period)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed")
                .ToListAsync();

            var groupedData = period.ToLower() switch
            {
                "daily" => transactions.GroupBy(t => t.TransactionDate.Date),
                "weekly" => transactions.GroupBy(t => 
                {
                    var date = t.TransactionDate.Date;
                    var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
                    return date.AddDays(-diff);
                }),
                "monthly" => transactions.GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month }),
                "quarterly" => transactions.GroupBy(t => 
                {
                    var quarter = (t.TransactionDate.Month - 1) / 3 + 1;
                    return new { t.TransactionDate.Year, Quarter = quarter };
                }),
                _ => transactions.GroupBy(t => t.TransactionDate.Date)
            };

            return groupedData.Select(g => new RevenueByPeriodDto
            {
                Period = g.Key.ToString(),
                Revenue = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                AverageAmount = g.Average(t => t.Amount),
                PaymentMethodBreakdown = g.GroupBy(t => t.PaymentMethod)
                    .ToDictionary(x => x.Key, x => x.Sum(t => t.Amount))
            }).OrderBy(r => r.Period).ToList();
        }

        public async Task<TaxReportDto> GenerateTaxReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed")
                .ToListAsync();

            var totalRevenue = transactions.Sum(t => t.Amount);
            var taxRate = 0.16m; // 16% tax rate - should be configurable
            var totalTax = totalRevenue * taxRate;

            return new TaxReportDto
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = totalRevenue,
                TaxRate = taxRate,
                TotalTax = totalTax,
                NetRevenue = totalRevenue - totalTax,
                TransactionCount = transactions.Count,
                TaxableTransactions = transactions.Count, // All transactions are taxable in this example
                ExemptTransactions = 0,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<CustomerPaymentReportDto> GenerateCustomerPaymentReportAsync(Guid tenantId, Guid customerId, DateTime startDate, DateTime endDate)
        {
            var customer = await _context.Users.FindAsync(customerId);
            if (customer == null)
                throw new ArgumentException("Customer not found");

            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.UserId == customerId &&
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate)
                .Include(pt => pt.Charge)
                .ToListAsync();

            var completedTransactions = transactions.Where(t => t.Status == "completed").ToList();
            var refundedTransactions = transactions.Where(t => t.Status == "refunded").ToList();

            return new CustomerPaymentReportDto
            {
                CustomerId = customerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                CustomerEmail = customer.Email,
                StartDate = startDate,
                EndDate = endDate,
                TotalAmount = completedTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalRefunds = refundedTransactions.Sum(t => Math.Abs(t.Amount)),
                NetAmount = completedTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount) - refundedTransactions.Sum(t => Math.Abs(t.Amount)),
                TransactionCount = completedTransactions.Count,
                RefundCount = refundedTransactions.Count,
                AverageAmount = completedTransactions.Any() ? completedTransactions.Average(t => t.Amount) : 0,
                FirstPaymentDate = completedTransactions.Any() ? completedTransactions.Min(t => t.TransactionDate) : (DateTime?)null,
                LastPaymentDate = completedTransactions.Any() ? completedTransactions.Max(t => t.TransactionDate) : (DateTime?)null,
                PaymentMethods = completedTransactions
                    .GroupBy(t => t.PaymentMethod)
                    .Select(g => new PaymentMethodUsage
                    {
                        PaymentMethod = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(t => t.Amount)
                    })
                    .ToList(),
                Transactions = completedTransactions.Select(t => new TransactionDetail
                {
                    TransactionId = t.Id,
                    TransactionDate = t.TransactionDate,
                    Amount = t.Amount,
                    PaymentMethod = t.PaymentMethod,
                    Status = t.Status,
                    Reference = t.TransactionReference,
                    Description = t.Charge?.BillingMonth.ToString("MMM yyyy") ?? "Payment"
                }).OrderByDescending(t => t.TransactionDate).ToList()
            };
        }

        public async Task<RefundReportDto> GenerateRefundReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var refundTransactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "refunded")
                .Include(pt => pt.User)
                .Include(pt => pt.Charge)
                .ToListAsync();

            return new RefundReportDto
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalRefunds = refundTransactions.Sum(t => Math.Abs(t.Amount)),
                RefundCount = refundTransactions.Count,
                AverageRefundAmount = refundTransactions.Any() ? refundTransactions.Average(t => Math.Abs(t.Amount)) : 0,
                RefundsByReason = refundTransactions
                    .Where(t => !string.IsNullOrWhiteSpace(t.RefundReason))
                    .GroupBy(t => t.RefundReason)
                    .Select(g => new RefundReasonSummary
                    {
                        Reason = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(t => Math.Abs(t.Amount))
                    })
                    .OrderByDescending(r => r.Amount)
                    .ToList(),
                RefundsByPaymentMethod = refundTransactions
                    .GroupBy(t => t.PaymentMethod)
                    .Select(g => new RefundMethodSummary
                    {
                        PaymentMethod = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(t => Math.Abs(t.Amount))
                    })
                    .OrderByDescending(r => r.Amount)
                    .ToList(),
                Refunds = refundTransactions.Select(t => new RefundDetail
                {
                    RefundId = t.Id,
                    RefundDate = t.TransactionDate,
                    Amount = Math.Abs(t.Amount),
                    Reason = t.RefundReason ?? "No reason provided",
                    PaymentMethod = t.PaymentMethod,
                    CustomerName = $"{t.User.FirstName} {t.User.LastName}",
                    OriginalTransactionId = t.TransactionReference
                }).OrderByDescending(r => r.RefundDate).ToList()
            };
        }

        public async Task<PaymentMethodPerformanceDto> GetPaymentMethodPerformanceAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate)
                .ToListAsync();

            var paymentMethods = transactions
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new PaymentMethodPerformance
                {
                    PaymentMethod = g.Key,
                    TotalTransactions = g.Count(),
                    SuccessfulTransactions = g.Count(t => t.Status == "completed"),
                    FailedTransactions = g.Count(t => t.Status == "failed"),
                    RefundedTransactions = g.Count(t => t.Status == "refunded"),
                    TotalAmount = g.Where(t => t.Status == "completed" && t.Amount > 0).Sum(t => t.Amount),
                    AverageAmount = g.Where(t => t.Status == "completed").Any() ? 
                        g.Where(t => t.Status == "completed").Average(t => t.Amount) : 0,
                    SuccessRate = g.Any() ? (double)g.Count(t => t.Status == "completed") / g.Count() * 100 : 0
                })
                .OrderByDescending(p => p.TotalAmount)
                .ToList();

            return new PaymentMethodPerformanceDto
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                PaymentMethods = paymentMethods,
                OverallSuccessRate = paymentMethods.Any() ? 
                    paymentMethods.Average(p => p.SuccessRate) : 0
            };
        }

        public async Task<List<FailedPaymentAnalysisDto>> AnalyzeFailedPaymentsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var failedTransactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "failed")
                .Include(pt => pt.User)
                .ToListAsync();

            return failedTransactions
                .GroupBy(t => new { t.PaymentMethod, Hour = t.TransactionDate.Hour })
                .Select(g => new FailedPaymentAnalysisDto
                {
                    PaymentMethod = g.Key.PaymentMethod,
                    HourOfDay = g.Key.Hour,
                    FailureCount = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount),
                    AverageAmount = g.Average(t => t.Amount),
                    FailureRate = (double)g.Count() / failedTransactions.Count * 100
                })
                .OrderByDescending(a => a.FailureCount)
                .ToList();
        }
    }

    // Supporting DTOs
    public class PaymentAnalyticsReport
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        
        // Summary Metrics
        public decimal TotalRevenue { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
        
        // Detailed Data
        public List<PaymentMethodDistribution> PaymentMethodDistribution { get; set; } = new();
        public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
        public List<CustomerPaymentSummary> TopCustomers { get; set; } = new();
        public List<FailedPaymentDto> FailedPayments { get; set; } = new();
        public List<RefundAnalyticsDto> Refunds { get; set; } = new();
    }

    public class CustomerPaymentSummary
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }

    public class RevenueByPeriodDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageAmount { get; set; }
        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new();
    }

    public class TaxReportDto
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TotalTax { get; set; }
        public decimal NetRevenue { get; set; }
        public int TransactionCount { get; set; }
        public int TaxableTransactions { get; set; }
        public int ExemptTransactions { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class CustomerPaymentReportDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetAmount { get; set; }
        public int TransactionCount { get; set; }
        public int RefundCount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public List<PaymentMethodUsage> PaymentMethods { get; set; } = new();
        public List<TransactionDetail> Transactions { get; set; } = new();
    }

    public class PaymentMethodUsage
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class TransactionDetail
    {
        public Guid TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class RefundReportDto
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRefunds { get; set; }
        public int RefundCount { get; set; }
        public decimal AverageRefundAmount { get; set; }
        public List<RefundReasonSummary> RefundsByReason { get; set; } = new();
        public List<RefundMethodSummary> RefundsByPaymentMethod { get; set; } = new();
        public List<RefundDetail> Refunds { get; set; } = new();
    }

    public class RefundReasonSummary
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class RefundMethodSummary
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class RefundDetail
    {
        public Guid RefundId { get; set; }
        public DateTime RefundDate { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string OriginalTransactionId { get; set; } = string.Empty;
    }

    public class PaymentMethodPerformanceDto
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<PaymentMethodPerformance> PaymentMethods { get; set; } = new();
        public double OverallSuccessRate { get; set; }
    }

    public class PaymentMethodPerformance
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public int RefundedTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public double SuccessRate { get; set; }
    }

    public class FailedPaymentAnalysisDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int HourOfDay { get; set; }
        public int FailureCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public double FailureRate { get; set; }
    }
}
