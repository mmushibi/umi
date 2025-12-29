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
    public interface IPaymentPlanService
    {
        Task<PaymentPlanResult> CreatePaymentPlanAsync(PaymentPlanRequest request);
        Task<PaymentPlanResult> UpdatePaymentPlanAsync(Guid planId, PaymentPlanUpdateRequest request);
        Task<List<PaymentPlan>> GetPaymentPlansAsync(Guid tenantId, Guid? customerId = null);
        Task<PaymentPlan> GetPaymentPlanAsync(Guid planId);
        Task<List<Installment>> GetInstallmentsAsync(Guid planId);
        Task<InstallmentResult> ProcessInstallmentAsync(Guid installmentId, decimal amount, string paymentMethod);
        Task<List<OverdueInstallment>> GetOverdueInstallmentsAsync(Guid tenantId);
        Task<byte[]> GeneratePaymentScheduleAsync(Guid planId, string format = "pdf");
        Task<PaymentPlanAnalytics> GeneratePaymentPlanAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate);
        Task<bool> ValidatePaymentPlanEligibilityAsync(Guid tenantId, Guid customerId, decimal totalAmount);
    }

    public class PaymentPlanService : IPaymentPlanService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PaymentPlanService> _logger;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly IAuditTrailService _auditService;

        public PaymentPlanService(
            SharedDbContext context,
            ILogger<PaymentPlanService> logger,
            IPaymentService paymentService,
            INotificationService notificationService,
            IAuditTrailService auditService)
        {
            _context = context;
            _logger = logger;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<PaymentPlanResult> CreatePaymentPlanAsync(PaymentPlanRequest request)
        {
            try
            {
                // Validate eligibility
                var isEligible = await ValidatePaymentPlanEligibilityAsync(request.TenantId, request.CustomerId, request.TotalAmount);
                if (!isEligible)
                {
                    return new PaymentPlanResult
                    {
                        Success = false,
                        Error = "Customer is not eligible for payment plan"
                    };
                }

                // Calculate installment schedule
                var installments = CalculateInstallmentSchedule(request);
                
                // Create payment plan
                var paymentPlan = new PaymentPlan
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    CustomerId = request.CustomerId,
                    SaleId = request.SaleId,
                    PlanName = request.PlanName,
                    TotalAmount = request.TotalAmount,
                    DownPaymentAmount = request.DownPaymentAmount,
                    InstallmentCount = request.InstallmentCount,
                    InstallmentAmount = installments.First().Amount,
                    InstallmentFrequency = request.InstallmentFrequency,
                    StartDate = request.StartDate,
                    EndDate = installments.Last().DueDate,
                    Status = "active",
                    InterestRate = request.InterestRate,
                    LateFeeRate = request.LateFeeRate,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy
                };

                _context.PaymentPlans.Add(paymentPlan);
                await _context.SaveChangesAsync();

                // Create installments
                foreach (var installment in installments)
                {
                    installment.PaymentPlanId = paymentPlan.Id;
                    installment.TenantId = request.TenantId;
                    _context.Installments.Add(installment);
                }
                await _context.SaveChangesAsync();

                // Process down payment if applicable
                if (request.DownPaymentAmount > 0)
                {
                    await ProcessDownPaymentAsync(paymentPlan, request.DownPaymentAmount, request.DownPaymentMethod);
                }

                // Send confirmation
                await SendPaymentPlanConfirmationAsync(paymentPlan);

                // Log creation
                await _auditService.LogActivityAsync(new AuditLogEntry
                {
                    TenantId = request.TenantId,
                    UserId = request.CreatedBy,
                    Action = "PaymentPlanCreated",
                    EntityType = "PaymentPlan",
                    EntityId = paymentPlan.Id.ToString(),
                    Description = $"Payment plan created for {request.TotalAmount:C} with {request.InstallmentCount} installments"
                });

                return new PaymentPlanResult
                {
                    Success = true,
                    PaymentPlanId = paymentPlan.Id,
                    PlanNumber = paymentPlan.PlanNumber,
                    Installments = installments,
                    NextPaymentDate = installments.FirstOrDefault(i => i.Status == "pending")?.DueDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment plan");
                return new PaymentPlanResult
                {
                    Success = false,
                    Error = "Failed to create payment plan"
                };
            }
        }

        public async Task<PaymentPlanResult> UpdatePaymentPlanAsync(Guid planId, PaymentPlanUpdateRequest request)
        {
            try
            {
                var paymentPlan = await _context.PaymentPlans
                    .Include(pp => pp.Installments)
                    .FirstOrDefaultAsync(pp => pp.Id == planId);

                if (paymentPlan == null)
                {
                    return new PaymentPlanResult
                    {
                        Success = false,
                        Error = "Payment plan not found"
                    };
                }

                // Update allowed fields
                if (!string.IsNullOrEmpty(request.PlanName))
                {
                    paymentPlan.PlanName = request.PlanName;
                }

                if (request.InstallmentFrequency.HasValue)
                {
                    paymentPlan.InstallmentFrequency = request.InstallmentFrequency.Value;
                    // Recalculate future installments
                    await RecalculateInstallmentsAsync(paymentPlan);
                }

                paymentPlan.UpdatedAt = DateTime.UtcNow;
                paymentPlan.UpdatedBy = request.UpdatedBy;

                await _context.SaveChangesAsync();

                // Log update
                await _auditService.LogActivityAsync(new AuditLogEntry
                {
                    TenantId = paymentPlan.TenantId,
                    UserId = request.UpdatedBy,
                    Action = "PaymentPlanUpdated",
                    EntityType = "PaymentPlan",
                    EntityId = paymentPlan.Id.ToString(),
                    Description = $"Payment plan {paymentPlan.PlanNumber} updated"
                });

                return new PaymentPlanResult
                {
                    Success = true,
                    PaymentPlanId = paymentPlan.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update payment plan");
                return new PaymentPlanResult
                {
                    Success = false,
                    Error = "Failed to update payment plan"
                };
            }
        }

        public async Task<List<PaymentPlan>> GetPaymentPlansAsync(Guid tenantId, Guid? customerId = null)
        {
            var query = _context.PaymentPlans
                .Where(pp => pp.TenantId == tenantId)
                .Include(pp => pp.Customer)
                .Include(pp => pp.Installments);

            if (customerId.HasValue)
                query = query.Where(pp => pp.CustomerId == customerId.Value);

            return await query
                .OrderByDescending(pp => pp.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaymentPlan> GetPaymentPlanAsync(Guid planId)
        {
            return await _context.PaymentPlans
                .Include(pp => pp.Customer)
                .Include(pp => pp.Sale)
                .Include(pp => pp.Installments)
                    .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(pp => pp.Id == planId);
        }

        public async Task<List<Installment>> GetInstallmentsAsync(Guid planId)
        {
            return await _context.Installments
                .Where(i => i.PaymentPlanId == planId)
                .Include(i => i.Payments)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<InstallmentResult> ProcessInstallmentAsync(Guid installmentId, decimal amount, string paymentMethod)
        {
            try
            {
                var installment = await _context.Installments
                    .Include(i => i.PaymentPlan)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == installmentId);

                if (installment == null)
                {
                    return new InstallmentResult
                    {
                        Success = false,
                        Error = "Installment not found"
                    };
                }

                if (installment.Status == "paid")
                {
                    return new InstallmentResult
                    {
                        Success = false,
                        Error = "Installment already paid"
                    };
                }

                // Calculate late fees if applicable
                var lateFee = CalculateLateFee(installment);
                var totalAmount = amount + lateFee;

                // Process payment
                var paymentResult = await _paymentService.CreatePaymentAsync(new CreatePaymentRequest
                {
                    TenantId = installment.PaymentPlan.TenantId,
                    Amount = totalAmount,
                    PaymentMethod = paymentMethod,
                    ReferenceNumber = await GeneratePaymentReferenceAsync(),
                    Notes = $"Installment payment for plan {installment.PaymentPlan.PlanNumber}",
                    SaleId = installment.PaymentPlan.SaleId
                });

                if (!paymentResult.Success)
                {
                    return new InstallmentResult
                    {
                        Success = false,
                        Error = paymentResult.Error
                    };
                }

                // Update installment
                installment.PaidAmount += amount;
                installment.LateFeeAmount = lateFee;
                installment.PaidAt = DateTime.UtcNow;
                installment.Status = installment.PaidAmount >= installment.Amount ? "paid" : "partial";

                // Create installment payment record
                var installmentPayment = new InstallmentPayment
                {
                    Id = Guid.NewGuid(),
                    InstallmentId = installment.Id,
                    PaymentId = paymentResult.PaymentId.Value,
                    Amount = amount,
                    LateFee = lateFee,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = paymentMethod
                };

                _context.InstallmentPayments.Add(installmentPayment);

                // Check if payment plan is complete
                await CheckPaymentPlanCompletionAsync(installment.PaymentPlan);

                await _context.SaveChangesAsync();

                // Send confirmation
                await SendInstallmentConfirmationAsync(installment, amount, lateFee);

                return new InstallmentResult
                {
                    Success = true,
                    InstallmentId = installment.Id,
                    PaymentId = paymentResult.PaymentId,
                    RemainingBalance = installment.Amount - installment.PaidAmount,
                    LateFeeApplied = lateFee
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process installment");
                return new InstallmentResult
                {
                    Success = false,
                    Error = "Failed to process installment"
                };
            }
        }

        public async Task<List<OverdueInstallment>> GetOverdueInstallmentsAsync(Guid tenantId)
        {
            var overdueInstallments = await _context.Installments
                .Where(i => i.PaymentPlan.TenantId == tenantId && 
                           i.DueDate < DateTime.UtcNow && 
                           i.Status != "paid")
                .Include(i => i.PaymentPlan)
                    .ThenInclude(pp => pp.Customer)
                .Include(i => i.Payments)
                .ToListAsync();

            return overdueInstallments.Select(i => new OverdueInstallment
            {
                InstallmentId = i.Id,
                PaymentPlanId = i.PaymentPlanId,
                PlanNumber = i.PaymentPlan.PlanNumber,
                CustomerName = i.PaymentPlan.Customer.FullName,
                CustomerEmail = i.PaymentPlan.Customer.Email,
                DueDate = i.DueDate,
                Amount = i.Amount,
                PaidAmount = i.PaidAmount,
                OverdueDays = (DateTime.UtcNow - i.DueDate).Days,
                LateFee = CalculateLateFee(i),
                TotalDue = i.Amount - i.PaidAmount + CalculateLateFee(i)
            }).OrderBy(oi => oi.OverdueDays).ToList();
        }

        public async Task<byte[]> GeneratePaymentScheduleAsync(Guid planId, string format = "pdf")
        {
            var paymentPlan = await GetPaymentPlanAsync(planId);
            if (paymentPlan == null)
                throw new ArgumentException("Payment plan not found");

            var schedule = new PaymentSchedule
            {
                PlanNumber = paymentPlan.PlanNumber,
                CustomerName = paymentPlan.Customer.FullName,
                TotalAmount = paymentPlan.TotalAmount,
                DownPayment = paymentPlan.DownPaymentAmount,
                InstallmentCount = paymentPlan.InstallmentCount,
                InstallmentAmount = paymentPlan.InstallmentAmount,
                InterestRate = paymentPlan.InterestRate,
                StartDate = paymentPlan.StartDate,
                Installments = paymentPlan.Installments.OrderBy(i => i.DueDate).ToList()
            };

            return format.ToLower() switch
            {
                "pdf" => GeneratePdfSchedule(schedule),
                "excel" => GenerateExcelSchedule(schedule),
                _ => GeneratePdfSchedule(schedule)
            };
        }

        public async Task<PaymentPlanAnalytics> GeneratePaymentPlanAnalyticsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var paymentPlans = await _context.PaymentPlans
                .Where(pp => pp.TenantId == tenantId && 
                           pp.CreatedAt >= startDate && 
                           pp.CreatedAt <= endDate)
                .Include(pp => pp.Customer)
                .Include(pp => pp.Installments)
                    .ThenInclude(i => i.Payments)
                .ToListAsync();

            var totalPlans = paymentPlans.Count;
            var activePlans = paymentPlans.Count(pp => pp.Status == "active");
            var completedPlans = paymentPlans.Count(pp => pp.Status == "completed");
            var defaultedPlans = paymentPlans.Count(pp => pp.Status == "defaulted");

            var totalValue = paymentPlans.Sum(pp => pp.TotalAmount);
            var totalCollected = paymentPlans.Sum(pp => pp.Installments
                .Sum(i => i.Payments.Sum(p => p.Amount)));

            return new PaymentPlanAnalytics
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                TotalPlans = totalPlans,
                ActivePlans = activePlans,
                CompletedPlans = completedPlans,
                DefaultedPlans = defaultedPlans,
                TotalValue = totalValue,
                TotalCollected = totalCollected,
                OutstandingBalance = totalValue - totalCollected,
                CompletionRate = totalPlans > 0 ? (double)completedPlans / totalPlans * 100 : 0,
                AveragePlanValue = totalPlans > 0 ? totalValue / totalPlans : 0,
                PlansByCustomer = paymentPlans
                    .GroupBy(pp => pp.CustomerId)
                    .Select(g => new CustomerPlanSummary
                    {
                        CustomerId = g.Key,
                        CustomerName = g.FirstOrDefault()?.Customer?.FullName ?? "Unknown",
                        PlanCount = g.Count(),
                        TotalValue = g.Sum(pp => pp.TotalAmount),
                        OutstandingBalance = g.Sum(pp => pp.TotalAmount - pp.Installments.Sum(i => i.PaidAmount))
                    })
                    .OrderByDescending(cps => cps.TotalValue)
                    .Take(10)
                    .ToList(),
                MonthlyTrends = await GetMonthlyPlanTrendsAsync(tenantId, startDate, endDate)
            };
        }

        public async Task<bool> ValidatePaymentPlanEligibilityAsync(Guid tenantId, Guid customerId, decimal totalAmount)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                return false;

            // Check customer's payment history
            var completedPlans = await _context.PaymentPlans
                .CountAsync(pp => pp.TenantId == tenantId && 
                                   pp.CustomerId == customerId && 
                                   pp.Status == "completed");

            var defaultedPlans = await _context.PaymentPlans
                .CountAsync(pp => pp.TenantId == tenantId && 
                                   pp.CustomerId == customerId && 
                                   pp.Status == "defaulted");

            // Business rules for eligibility
            if (defaultedPlans > 0)
                return false; // No new plans if previous defaults

            if (completedPlans < 2 && totalAmount > 5000)
                return false; // Limit high-value plans for new customers

            if (customer.AccountBalance < 0)
                return false; // No outstanding negative balance

            return true;
        }

        private List<Installment> CalculateInstallmentSchedule(PaymentPlanRequest request)
        {
            var installments = new List<Installment>();
            var principalAmount = request.TotalAmount - request.DownPaymentAmount;
            var installmentAmount = principalAmount / request.InstallmentCount;
            
            var frequencyDays = request.InstallmentFrequency.ToLower() switch
            {
                "weekly" => 7,
                "biweekly" => 14,
                "monthly" => 30,
                "quarterly" => 90,
                _ => 30
            };

            for (int i = 1; i <= request.InstallmentCount; i++)
            {
                var dueDate = request.StartDate.AddDays(frequencyDays * i);
                
                installments.Add(new Installment
                {
                    Id = Guid.NewGuid(),
                    InstallmentNumber = i,
                    Amount = installmentAmount,
                    DueDate = dueDate,
                    Status = "pending",
                    PaidAmount = 0,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return installments;
        }

        private decimal CalculateLateFee(Installment installment)
        {
            if (installment.DueDate >= DateTime.UtcNow || installment.Status == "paid")
                return 0;

            var overdueDays = (DateTime.UtcNow - installment.DueDate).Days;
            var paymentPlan = _context.PaymentPlans
                .FirstOrDefault(pp => pp.Id == installment.PaymentPlanId);

            return paymentPlan?.LateFeeRate > 0 ? 
                installment.Amount * (paymentPlan.LateFeeRate / 100) * (overdueDays / 30m) : 0;
        }

        private async Task ProcessDownPaymentAsync(PaymentPlan paymentPlan, decimal amount, string paymentMethod)
        {
            var paymentResult = await _paymentService.CreatePaymentAsync(new CreatePaymentRequest
            {
                TenantId = paymentPlan.TenantId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                ReferenceNumber = await GeneratePaymentReferenceAsync(),
                Notes = $"Down payment for plan {paymentPlan.PlanNumber}",
                SaleId = paymentPlan.SaleId
            });

            if (paymentResult.Success)
            {
                paymentPlan.DownPaymentPaid = amount;
                paymentPlan.DownPaymentDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task RecalculateInstallmentsAsync(PaymentPlan paymentPlan)
        {
            var unpaidInstallments = paymentPlan.Installments
                .Where(i => i.Status == "pending")
                .ToList();

            // Recalculate due dates based on new frequency
            var frequencyDays = paymentPlan.InstallmentFrequency.ToLower() switch
            {
                "weekly" => 7,
                "biweekly" => 14,
                "monthly" => 30,
                "quarterly" => 90,
                _ => 30
            };

            for (int i = 0; i < unpaidInstallments.Count; i++)
            {
                unpaidInstallments[i].DueDate = DateTime.UtcNow.AddDays(frequencyDays * (i + 1));
            }

            await _context.SaveChangesAsync();
        }

        private async Task CheckPaymentPlanCompletionAsync(PaymentPlan paymentPlan)
        {
            var allPaid = paymentPlan.Installments.All(i => i.Status == "paid");
            if (allPaid)
            {
                paymentPlan.Status = "completed";
                paymentPlan.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send completion notification
                await SendPlanCompletionNotificationAsync(paymentPlan);
            }
        }

        private async Task<string> GeneratePaymentReferenceAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"PP-{timestamp}-{random}";
        }

        private async Task SendPaymentPlanConfirmationAsync(PaymentPlan paymentPlan)
        {
            await _notificationService.SendNotificationAsync(new NotificationMessage
            {
                TenantId = paymentPlan.TenantId,
                UserId = paymentPlan.CustomerId,
                Type = "PaymentPlanCreated",
                Title = "Payment Plan Created",
                Message = $"Your payment plan {paymentPlan.PlanNumber} has been created with {paymentPlan.InstallmentCount} installments of {paymentPlan.InstallmentAmount:C}",
                Data = new { planId = paymentPlan.Id, planNumber = paymentPlan.PlanNumber }
            });
        }

        private async Task SendInstallmentConfirmationAsync(Installment installment, decimal amount, decimal lateFee)
        {
            await _notificationService.SendNotificationAsync(new NotificationMessage
            {
                TenantId = installment.PaymentPlan.TenantId,
                UserId = installment.PaymentPlan.CustomerId,
                Type = "InstallmentPaymentReceived",
                Title = "Installment Payment Received",
                Message = $"Payment of {amount:C} received for installment #{installment.InstallmentNumber}" + 
                          (lateFee > 0 ? $" (including late fee of {lateFee:C})" : ""),
                Data = new { installmentId = installment.Id, amount, lateFee }
            });
        }

        private async Task SendPlanCompletionNotificationAsync(PaymentPlan paymentPlan)
        {
            await _notificationService.SendNotificationAsync(new NotificationMessage
            {
                TenantId = paymentPlan.TenantId,
                UserId = paymentPlan.CustomerId,
                Type = "PaymentPlanCompleted",
                Title = "Payment Plan Completed",
                Message = $"Congratulations! Your payment plan {paymentPlan.PlanNumber} has been fully paid",
                Data = new { planId = paymentPlan.Id, planNumber = paymentPlan.PlanNumber }
            });
        }

        private async Task<List<MonthlyPlanTrend>> GetMonthlyPlanTrendsAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var plans = await _context.PaymentPlans
                .Where(pp => pp.TenantId == tenantId && 
                           pp.CreatedAt >= startDate && 
                           pp.CreatedAt <= endDate)
                .ToListAsync();

            return plans
                .GroupBy(pp => new { pp.CreatedAt.Year, pp.CreatedAt.Month })
                .Select(g => new MonthlyPlanTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    PlansCreated = g.Count(),
                    TotalValue = g.Sum(pp => pp.TotalAmount),
                    CompletedPlans = g.Count(pp => pp.Status == "completed")
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();
        }

        private byte[] GeneratePdfSchedule(PaymentSchedule schedule)
        {
            var content = $"Payment Schedule - Plan {schedule.PlanNumber}\n\n";
            content += $"Customer: {schedule.CustomerName}\n";
            content += $"Total Amount: {schedule.TotalAmount:C}\n";
            content += $"Down Payment: {schedule.DownPayment:C}\n";
            content += $"Installments: {schedule.InstallmentCount} x {schedule.InstallmentAmount:C}\n\n";
            
            content += "Installment Schedule:\n";
            content += "No.\tDue Date\tAmount\tStatus\n";
            
            foreach (var installment in schedule.Installments)
            {
                content += $"{installment.InstallmentNumber}\t{installment.DueDate:yyyy-MM-dd}\t{installment.Amount:C}\t{installment.Status}\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        private byte[] GenerateExcelSchedule(PaymentSchedule schedule)
        {
            var csv = "Installment Number,Due Date,Amount,Status,Paid Amount\n";
            
            foreach (var installment in schedule.Installments)
            {
                csv += $"{installment.InstallmentNumber},{installment.DueDate:yyyy-MM-dd},{installment.Amount},{installment.Status},{installment.PaidAmount}\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(csv);
        }
    }

    // Supporting DTOs and Entities
    public class PaymentPlanRequest
    {
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? SaleId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DownPaymentAmount { get; set; }
        public string DownPaymentMethod { get; set; } = string.Empty;
        public int InstallmentCount { get; set; }
        public string InstallmentFrequency { get; set; } = "monthly"; // weekly, biweekly, monthly, quarterly
        public DateTime StartDate { get; set; }
        public decimal InterestRate { get; set; }
        public decimal LateFeeRate { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class PaymentPlanUpdateRequest
    {
        public string PlanName { get; set; } = string.Empty;
        public string? InstallmentFrequency { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    public class PaymentPlanResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? PaymentPlanId { get; set; }
        public string? PlanNumber { get; set; }
        public List<Installment>? Installments { get; set; }
        public DateTime? NextPaymentDate { get; set; }
    }

    public class InstallmentResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid InstallmentId { get; set; }
        public Guid? PaymentId { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal LateFeeApplied { get; set; }
    }

    public class OverdueInstallment
    {
        public Guid InstallmentId { get; set; }
        public Guid PaymentPlanId { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public int OverdueDays { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalDue { get; set; }
    }

    public class PaymentPlanAnalytics
    {
        public Guid TenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalPlans { get; set; }
        public int ActivePlans { get; set; }
        public int CompletedPlans { get; set; }
        public int DefaultedPlans { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal OutstandingBalance { get; set; }
        public double CompletionRate { get; set; }
        public decimal AveragePlanValue { get; set; }
        public List<CustomerPlanSummary> PlansByCustomer { get; set; } = new();
        public List<MonthlyPlanTrend> MonthlyTrends { get; set; } = new();
    }

    public class CustomerPlanSummary
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int PlanCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    public class MonthlyPlanTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int PlansCreated { get; set; }
        public decimal TotalValue { get; set; }
        public int CompletedPlans { get; set; }
    }

    public class PaymentSchedule
    {
        public string PlanNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }
        public int InstallmentCount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public List<Installment> Installments { get; set; } = new();
    }

    // Entity classes
    public class PaymentPlan : TenantEntity
    {
        public string PlanNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Guid? SaleId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DownPaymentAmount { get; set; }
        public decimal? DownPaymentPaid { get; set; }
        public DateTime? DownPaymentDate { get; set; }
        public int InstallmentCount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public string InstallmentFrequency { get; set; } = "monthly";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal InterestRate { get; set; }
        public decimal LateFeeRate { get; set; }
        public string Status { get; set; } = "active"; // active, completed, defaulted, cancelled
        public DateTime? CompletedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        public virtual Customer Customer { get; set; } = null!;
        public virtual Sale? Sale { get; set; }
        public virtual List<Installment> Installments { get; set; } = new();
    }

    public class Installment : TenantEntity
    {
        public Guid PaymentPlanId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal LateFeeAmount { get; set; }
        public string Status { get; set; } = "pending"; // pending, partial, paid, overdue
        public DateTime? PaidAt { get; set; }

        public virtual PaymentPlan PaymentPlan { get; set; } = null!;
        public virtual List<InstallmentPayment> Payments { get; set; } = new();
    }

    public class InstallmentPayment : TenantEntity
    {
        public Guid InstallmentId { get; set; }
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public decimal LateFee { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;

        public virtual Installment Installment { get; set; } = null!;
        public virtual Payment Payment { get; set; } = null!;
    }
}
