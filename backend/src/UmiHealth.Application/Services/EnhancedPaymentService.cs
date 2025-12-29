using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IEnhancedPaymentService
    {
        // Bulk Payment Processing
        Task<BulkPaymentResult> ProcessBulkPaymentsAsync(BulkPaymentRequest request);
        Task<byte[]> GenerateBulkPaymentTemplateAsync();
        Task<List<BulkPaymentValidation>> ValidateBulkPaymentFileAsync(Stream fileStream);

        // Recurring Payments
        Task<RecurringPaymentResult> CreateRecurringPaymentAsync(RecurringPaymentRequest request);
        Task<List<RecurringPaymentDto>> GetRecurringPaymentsAsync(Guid tenantId);
        Task<bool> CancelRecurringPaymentAsync(Guid recurringPaymentId);
        Task ProcessScheduledRecurringPaymentsAsync();

        // Multi-Currency Support
        Task<CurrencyConversionResult> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<List<ExchangeRateDto>> GetExchangeRatesAsync();
        Task UpdateExchangeRatesAsync();

        // Payment Plans
        Task<PaymentPlanResult> CreatePaymentPlanAsync(PaymentPlanRequest request);
        Task<List<PaymentPlanDto>> GetPaymentPlansAsync(Guid tenantId);
        Task<PaymentPlanInstallmentResult> ProcessInstallmentPaymentAsync(Guid installmentId, decimal amount);

        // Advanced Analytics
        Task<PaymentAnalyticsDto> GetAdvancedPaymentAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<List<PaymentTrendDto>> GetPaymentTrendsAsync(Guid tenantId, int months);
        Task<PaymentMethodDistributionDto> GetPaymentMethodDistributionAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    }

    public class EnhancedPaymentService : IEnhancedPaymentService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<EnhancedPaymentService> _logger;
        private readonly IPaymentVerificationService _paymentVerificationService;
        private readonly INotificationService _notificationService;

        public EnhancedPaymentService(
            SharedDbContext context,
            ILogger<EnhancedPaymentService> logger,
            IPaymentVerificationService paymentVerificationService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _paymentVerificationService = paymentVerificationService;
            _notificationService = notificationService;
        }

        #region Bulk Payment Processing

        public async Task<BulkPaymentResult> ProcessBulkPaymentsAsync(BulkPaymentRequest request)
        {
            var result = new BulkPaymentResult
            {
                TotalPayments = request.Payments.Count,
                ProcessedPayments = 0,
                FailedPayments = 0,
                Results = new List<BulkPaymentItemResult>()
            };

            foreach (var payment in request.Payments)
            {
                try
                {
                    var verificationResult = await _paymentVerificationService.VerifyPaymentAsync(
                        payment.Reference,
                        payment.Amount,
                        payment.PaymentMethod);

                    var itemResult = new BulkPaymentItemResult
                    {
                        Reference = payment.Reference,
                        Success = verificationResult.Success,
                        Message = verificationResult.Message,
                        TransactionId = verificationResult.TransactionId
                    };

                    result.Results.Add(itemResult);

                    if (verificationResult.Success)
                    {
                        result.ProcessedPayments++;
                        result.TotalAmount += payment.Amount;
                    }
                    else
                    {
                        result.FailedPayments++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing bulk payment item: {payment.Reference}");
                    result.FailedPayments++;
                    result.Results.Add(new BulkPaymentItemResult
                    {
                        Reference = payment.Reference,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return result;
        }

        public async Task<byte[]> GenerateBulkPaymentTemplateAsync()
        {
            var template = new List<BulkPaymentTemplate>
            {
                new BulkPaymentTemplate
                {
                    Reference = "REF001",
                    Amount = 150.00m,
                    PaymentMethod = "mobile_money",
                    CustomerName = "John Doe",
                    CustomerEmail = "john@example.com",
                    PhoneNumber = "+260123456789",
                    Description = "Payment for services"
                }
            };

            var csv = "Reference,Amount,PaymentMethod,CustomerName,CustomerEmail,PhoneNumber,Description\n";
            foreach (var item in template)
            {
                csv += $"{item.Reference},{item.Amount},{item.PaymentMethod},{item.CustomerName},{item.CustomerEmail},{item.PhoneNumber},{item.Description}\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        public async Task<List<BulkPaymentValidation>> ValidateBulkPaymentFileAsync(Stream fileStream)
        {
            var validations = new List<BulkPaymentValidation>();
            
            using (var reader = new StreamReader(fileStream))
            {
                var header = await reader.ReadLineAsync();
                var lineNumber = 2;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var fields = line.Split(',');

                    if (fields.Length < 7)
                    {
                        validations.Add(new BulkPaymentValidation
                        {
                            LineNumber = lineNumber,
                            IsValid = false,
                            Message = "Insufficient columns"
                        });
                        continue;
                    }

                    var validation = new BulkPaymentValidation
                    {
                        LineNumber = lineNumber,
                        Reference = fields[0]?.Trim(),
                        Amount = decimal.TryParse(fields[1], out var amount) ? amount : 0,
                        PaymentMethod = fields[2]?.Trim(),
                        CustomerName = fields[3]?.Trim(),
                        CustomerEmail = fields[4]?.Trim(),
                        PhoneNumber = fields[5]?.Trim(),
                        Description = fields[6]?.Trim()
                    };

                    // Validate fields
                    validation.IsValid = !string.IsNullOrWhiteSpace(validation.Reference) &&
                                       validation.Amount > 0 &&
                                       !string.IsNullOrWhiteSpace(validation.PaymentMethod) &&
                                       !string.IsNullOrWhiteSpace(validation.CustomerName);

                    if (!validation.IsValid)
                    {
                        validation.Message = "Missing required fields or invalid amount";
                    }

                    validations.Add(validation);
                    lineNumber++;
                }
            }

            return validations;
        }

        #endregion

        #region Recurring Payments

        public async Task<RecurringPaymentResult> CreateRecurringPaymentAsync(RecurringPaymentRequest request)
        {
            var recurringPayment = new RecurringPayment
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CustomerId = request.CustomerId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethod,
                Frequency = request.Frequency,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                NextPaymentDate = CalculateNextPaymentDate(request.StartDate, request.Frequency),
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.RecurringPayments.Add(recurringPayment);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created recurring payment {recurringPayment.Id} for tenant {request.TenantId}");

            return new RecurringPaymentResult
            {
                Success = true,
                RecurringPaymentId = recurringPayment.Id,
                NextPaymentDate = recurringPayment.NextPaymentDate,
                Message = "Recurring payment created successfully"
            };
        }

        public async Task<List<RecurringPaymentDto>> GetRecurringPaymentsAsync(Guid tenantId)
        {
            return await _context.RecurringPayments
                .Where(rp => rp.TenantId == tenantId && rp.IsActive)
                .Include(rp => rp.Customer)
                .Select(rp => new RecurringPaymentDto
                {
                    Id = rp.Id,
                    CustomerId = rp.CustomerId,
                    CustomerName = rp.Customer.FirstName + " " + rp.Customer.LastName,
                    Amount = rp.Amount,
                    Currency = rp.Currency,
                    PaymentMethod = rp.PaymentMethod,
                    Frequency = rp.Frequency,
                    StartDate = rp.StartDate,
                    EndDate = rp.EndDate,
                    NextPaymentDate = rp.NextPaymentDate,
                    Description = rp.Description,
                    IsActive = rp.IsActive,
                    CreatedAt = rp.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> CancelRecurringPaymentAsync(Guid recurringPaymentId)
        {
            var recurringPayment = await _context.RecurringPayments.FindAsync(recurringPaymentId);
            if (recurringPayment == null)
                return false;

            recurringPayment.IsActive = false;
            recurringPayment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cancelled recurring payment {recurringPaymentId}");
            return true;
        }

        public async Task ProcessScheduledRecurringPaymentsAsync()
        {
            var duePayments = await _context.RecurringPayments
                .Where(rp => rp.IsActive && rp.NextPaymentDate <= DateTime.UtcNow)
                .Include(rp => rp.Customer)
                .ToListAsync();

            foreach (var recurringPayment in duePayments)
            {
                try
                {
                    var reference = $"REC-{recurringPayment.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                    var verificationResult = await _paymentVerificationService.VerifyPaymentAsync(
                        reference,
                        recurringPayment.Amount,
                        recurringPayment.PaymentMethod);

                    if (verificationResult.Success)
                    {
                        // Update next payment date
                        recurringPayment.NextPaymentDate = CalculateNextPaymentDate(
                            recurringPayment.NextPaymentDate.Value, 
                            recurringPayment.Frequency);
                        recurringPayment.LastPaymentDate = DateTime.UtcNow;
                        recurringPayment.UpdatedAt = DateTime.UtcNow;

                        // Send notification
                        await _notificationService.CreateNotificationAsync(
                            recurringPayment.TenantId,
                            recurringPayment.CustomerId,
                            new CreateNotificationRequest
                            {
                                Type = "recurring_payment_processed",
                                Title = "Recurring Payment Processed",
                                Message = $"Your recurring payment of {recurringPayment.Amount:C} has been processed successfully.",
                                Data = new Dictionary<string, object>
                                {
                                    { "recurringPaymentId", recurringPayment.Id },
                                    { "amount", recurringPayment.Amount },
                                    { "nextPaymentDate", recurringPayment.NextPaymentDate }
                                }
                            });
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to process recurring payment {recurringPayment.Id}: {verificationResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing recurring payment {recurringPayment.Id}");
                }
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Multi-Currency Support

        public async Task<CurrencyConversionResult> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            if (fromCurrency == toCurrency)
            {
                return new CurrencyConversionResult
                {
                    OriginalAmount = amount,
                    ConvertedAmount = amount,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    ExchangeRate = 1.0m,
                    ConvertedAt = DateTime.UtcNow
                };
            }

            var exchangeRate = await GetExchangeRateAsync(fromCurrency, toCurrency);
            if (exchangeRate == 0)
            {
                throw new InvalidOperationException($"Exchange rate not available for {fromCurrency} to {toCurrency}");
            }

            var convertedAmount = amount * exchangeRate;

            return new CurrencyConversionResult
            {
                OriginalAmount = amount,
                ConvertedAmount = convertedAmount,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                ExchangeRate = exchangeRate,
                ConvertedAt = DateTime.UtcNow
            };
        }

        public async Task<List<ExchangeRateDto>> GetExchangeRatesAsync()
        {
            return await _context.ExchangeRates
                .Where(er => er.IsActive && er.ValidUntil > DateTime.UtcNow)
                .Select(er => new ExchangeRateDto
                {
                    FromCurrency = er.FromCurrency,
                    ToCurrency = er.ToCurrency,
                    Rate = er.Rate,
                    UpdatedAt = er.UpdatedAt,
                    ValidUntil = er.ValidUntil
                })
                .ToListAsync();
        }

        public async Task UpdateExchangeRatesAsync()
        {
            // This would typically integrate with an external exchange rate API
            // For now, we'll use mock data
            var exchangeRates = new List<ExchangeRate>
            {
                new ExchangeRate { FromCurrency = "USD", ToCurrency = "ZMW", Rate = 21.50m, ValidUntil = DateTime.UtcNow.AddDays(1) },
                new ExchangeRate { FromCurrency = "EUR", ToCurrency = "ZMW", Rate = 23.75m, ValidUntil = DateTime.UtcNow.AddDays(1) },
                new ExchangeRate { FromCurrency = "GBP", ToCurrency = "ZMW", Rate = 27.20m, ValidUntil = DateTime.UtcNow.AddDays(1) }
            };

            foreach (var rate in exchangeRates)
            {
                var existingRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(er => er.FromCurrency == rate.FromCurrency && er.ToCurrency == rate.ToCurrency);

                if (existingRate != null)
                {
                    existingRate.Rate = rate.Rate;
                    existingRate.UpdatedAt = DateTime.UtcNow;
                    existingRate.ValidUntil = rate.ValidUntil;
                }
                else
                {
                    _context.ExchangeRates.Add(rate);
                }
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Payment Plans

        public async Task<PaymentPlanResult> CreatePaymentPlanAsync(PaymentPlanRequest request)
        {
            var paymentPlan = new PaymentPlan
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CustomerId = request.CustomerId,
                TotalAmount = request.TotalAmount,
                Currency = request.Currency,
                NumberOfInstallments = request.NumberOfInstallments,
                InstallmentAmount = request.TotalAmount / request.NumberOfInstallments,
                Frequency = request.Frequency,
                StartDate = request.StartDate,
                Description = request.Description,
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            // Create installments
            for (int i = 1; i <= request.NumberOfInstallments; i++)
            {
                var installment = new PaymentPlanInstallment
                {
                    Id = Guid.NewGuid(),
                    PaymentPlanId = paymentPlan.Id,
                    InstallmentNumber = i,
                    Amount = paymentPlan.InstallmentAmount,
                    DueDate = CalculateInstallmentDueDate(request.StartDate, i, request.Frequency),
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                paymentPlan.Installments.Add(installment);
            }

            _context.PaymentPlans.Add(paymentPlan);
            await _context.SaveChangesAsync();

            return new PaymentPlanResult
            {
                Success = true,
                PaymentPlanId = paymentPlan.Id,
                Message = "Payment plan created successfully"
            };
        }

        public async Task<List<PaymentPlanDto>> GetPaymentPlansAsync(Guid tenantId)
        {
            return await _context.PaymentPlans
                .Where(pp => pp.TenantId == tenantId)
                .Include(pp => pp.Customer)
                .Include(pp => pp.Installments)
                .Select(pp => new PaymentPlanDto
                {
                    Id = pp.Id,
                    CustomerId = pp.CustomerId,
                    CustomerName = pp.Customer.FirstName + " " + pp.Customer.LastName,
                    TotalAmount = pp.TotalAmount,
                    Currency = pp.Currency,
                    NumberOfInstallments = pp.NumberOfInstallments,
                    InstallmentAmount = pp.InstallmentAmount,
                    Frequency = pp.Frequency,
                    StartDate = pp.StartDate,
                    Status = pp.Status,
                    PaidInstallments = pp.Installments.Count(i => i.Status == "paid"),
                    TotalInstallments = pp.Installments.Count,
                    CreatedAt = pp.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<PaymentPlanInstallmentResult> ProcessInstallmentPaymentAsync(Guid installmentId, decimal amount)
        {
            var installment = await _context.PaymentPlanInstallments
                .Include(i => i.PaymentPlan)
                .FirstOrDefaultAsync(i => i.Id == installmentId);

            if (installment == null)
            {
                return new PaymentPlanInstallmentResult
                {
                    Success = false,
                    Message = "Installment not found"
                };
            }

            if (installment.Status != "pending")
            {
                return new PaymentPlanInstallmentResult
                {
                    Success = false,
                    Message = "Installment already processed"
                };
            }

            if (Math.Abs(amount - installment.Amount) > 0.01m)
            {
                return new PaymentPlanInstallmentResult
                {
                    Success = false,
                    Message = $"Amount mismatch. Expected: {installment.Amount:C}, Received: {amount:C}"
                };
            }

            installment.Status = "paid";
            installment.PaidAmount = amount;
            installment.PaidDate = DateTime.UtcNow;
            installment.UpdatedAt = DateTime.UtcNow;

            // Check if all installments are paid
            var allInstallments = await _context.PaymentPlanInstallments
                .Where(i => i.PaymentPlanId == installment.PaymentPlanId)
                .ToListAsync();

            if (allInstallments.All(i => i.Status == "paid"))
            {
                var paymentPlan = await _context.PaymentPlans.FindAsync(installment.PaymentPlanId);
                paymentPlan.Status = "completed";
                paymentPlan.CompletedAt = DateTime.UtcNow;
                paymentPlan.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new PaymentPlanInstallmentResult
            {
                Success = true,
                Message = "Installment payment processed successfully"
            };
        }

        #endregion

        #region Advanced Analytics

        public async Task<PaymentAnalyticsDto> GetAdvancedPaymentAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate)
                .ToListAsync();

            var totalRevenue = transactions.Where(t => t.Status == "completed" && t.Amount > 0).Sum(t => t.Amount);
            var totalTransactions = transactions.Count(t => t.Status == "completed");
            var averageTransactionValue = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

            var paymentMethods = transactions
                .Where(t => t.Status == "completed")
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new PaymentMethodAnalytics
                {
                    PaymentMethod = g.Key,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount),
                    Percentage = totalRevenue > 0 ? (g.Sum(t => t.Amount) / totalRevenue) * 100 : 0,
                    AverageAmount = g.Average(t => t.Amount),
                    SuccessfulTransactions = g.Count(t => t.Status == "completed"),
                    FailedTransactions = 0,
                    SuccessRate = 100
                })
                .ToList();

            var dailyRevenue = transactions
                .Where(t => t.Status == "completed")
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
                .ToList();

            return new PaymentAnalyticsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                AverageTransactionValue = averageTransactionValue,
                PaymentMethods = paymentMethods,
                DailyRevenue = dailyRevenue
            };
        }

        public async Task<List<PaymentTrendDto>> GetPaymentTrendsAsync(Guid tenantId, int months)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);
            var endDate = DateTime.UtcNow;

            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate)
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
                    TotalAmount = monthTransactions.Where(t => t.Status == "completed").Sum(t => t.Amount),
                    TransactionCount = monthTransactions.Count(t => t.Status == "completed"),
                    AverageAmount = monthTransactions.Any() ? 
                        monthTransactions.Where(t => t.Status == "completed").Average(t => t.Amount) : 0
                });
            }

            return trends;
        }

        public async Task<PaymentMethodDistributionDto> GetPaymentMethodDistributionAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate <= endDate &&
                           pt.Status == "completed")
                .ToListAsync();

            var totalAmount = transactions.Sum(t => t.Amount);
            var totalCount = transactions.Count;

            var distribution = transactions
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new PaymentMethodDistribution
                {
                    PaymentMethod = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count(),
                    Percentage = totalAmount > 0 ? (g.Sum(t => t.Amount) / totalAmount) * 100 : 0,
                    AverageAmount = g.Average(t => t.Amount)
                })
                .OrderByDescending(d => d.Amount)
                .ToList();

            return new PaymentMethodDistributionDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalAmount = totalAmount,
                TotalCount = totalCount,
                Distribution = distribution
            };
        }

        #endregion

        #region Helper Methods

        private DateTime? CalculateNextPaymentDate(DateTime lastPaymentDate, string frequency)
        {
            return frequency.ToLower() switch
            {
                "daily" => lastPaymentDate.AddDays(1),
                "weekly" => lastPaymentDate.AddDays(7),
                "monthly" => lastPaymentDate.AddMonths(1),
                "quarterly" => lastPaymentDate.AddMonths(3),
                "yearly" => lastPaymentDate.AddYears(1),
                _ => null
            };
        }

        private DateTime CalculateInstallmentDueDate(DateTime startDate, int installmentNumber, string frequency)
        {
            return frequency.ToLower() switch
            {
                "weekly" => startDate.AddDays((installmentNumber - 1) * 7),
                "monthly" => startDate.AddMonths(installmentNumber - 1),
                "quarterly" => startDate.AddMonths((installmentNumber - 1) * 3),
                _ => startDate.AddDays(installmentNumber - 1)
            };
        }

        private async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            var exchangeRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(er => 
                    er.FromCurrency == fromCurrency && 
                    er.ToCurrency == toCurrency && 
                    er.IsActive && 
                    er.ValidUntil > DateTime.UtcNow);

            return exchangeRate?.Rate ?? 0;
        }

        #endregion
    }

    // Supporting DTOs and Entities
    public class BulkPaymentRequest
    {
        public List<BulkPaymentItem> Payments { get; set; } = new();
        public Guid ProcessedBy { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class BulkPaymentItem
    {
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BulkPaymentResult
    {
        public int TotalPayments { get; set; }
        public int ProcessedPayments { get; set; }
        public int FailedPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public List<BulkPaymentItemResult> Results { get; set; } = new();
    }

    public class BulkPaymentItemResult
    {
        public string Reference { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? TransactionId { get; set; }
    }

    public class BulkPaymentTemplate
    {
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BulkPaymentValidation
    {
        public int LineNumber { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class RecurringPaymentRequest
    {
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty; // daily, weekly, monthly, quarterly, yearly
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
    }

    public class RecurringPaymentResult
    {
        public bool Success { get; set; }
        public Guid RecurringPaymentId { get; set; }
        public DateTime? NextPaymentDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RecurringPaymentDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NextPaymentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CurrencyConversionResult
    {
        public decimal OriginalAmount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; }
        public DateTime ConvertedAt { get; set; }
    }

    public class ExchangeRateDto
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime ValidUntil { get; set; }
    }

    public class PaymentPlanRequest
    {
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public int NumberOfInstallments { get; set; }
        public string Frequency { get; set; } = string.Empty; // weekly, monthly, quarterly
        public DateTime StartDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
    }

    public class PaymentPlanResult
    {
        public bool Success { get; set; }
        public Guid PaymentPlanId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PaymentPlanDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int NumberOfInstallments { get; set; }
        public decimal InstallmentAmount { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int PaidInstallments { get; set; }
        public int TotalInstallments { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentPlanInstallmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PaymentTrendDto
    {
        public string Period { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class PaymentMethodDistributionDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public List<PaymentMethodDistribution> Distribution { get; set; } = new();
    }

    public class PaymentMethodDistribution
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public decimal AverageAmount { get; set; }
    }

    // Additional Entities
    public class RecurringPayment
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NextPaymentDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
    }

    public class ExchangeRate
    {
        public Guid Id { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime ValidUntil { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PaymentPlan
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public int NumberOfInstallments { get; set; }
        public decimal InstallmentAmount { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = string.Empty; // active, completed, cancelled
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid CreatedBy { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
        public virtual ICollection<PaymentPlanInstallment> Installments { get; set; } = new List<PaymentPlanInstallment>();
    }

    public class PaymentPlanInstallment
    {
        public Guid Id { get; set; }
        public Guid PaymentPlanId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; } = string.Empty; // pending, paid, overdue
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual PaymentPlan PaymentPlan { get; set; } = null!;
    }
}
