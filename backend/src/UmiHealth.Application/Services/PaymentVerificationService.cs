using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IPaymentVerificationService
    {
        Task<PaymentVerificationResult> VerifyPaymentAsync(string paymentReference, decimal amount, string paymentMethod);
        Task<bool> ProcessRefundAsync(string chargeId, string reason);
        Task<IEnumerable<PaymentTransactionDto>> GetPaymentHistoryAsync(Guid tenantId, int year, int month);
        Task<PaymentSummaryDto> GetPaymentSummaryAsync(Guid tenantId);
    }

    public class PaymentVerificationService : IPaymentVerificationService
    {
        private readonly SharedDbContext _context;
        private readonly IAdditionalUserService _additionalUserService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaymentVerificationService> _logger;

        public PaymentVerificationService(
            SharedDbContext context,
            IAdditionalUserService additionalUserService,
            INotificationService notificationService,
            ILogger<PaymentVerificationService> logger)
        {
            _context = context;
            _additionalUserService = additionalUserService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<PaymentVerificationResult> VerifyPaymentAsync(string paymentReference, decimal amount, string paymentMethod)
        {
            try
            {
                // Find the charge associated with this payment reference
                var charge = await _context.AdditionalUserCharges
                    .Include(c => c.Tenant)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.PaymentReference == paymentReference);

                if (charge == null)
                {
                    return new PaymentVerificationResult
                    {
                        Success = false,
                        Message = "Payment reference not found"
                    };
                }

                // Verify amount matches
                if (Math.Abs(charge.ChargeAmount - amount) > 0.01m)
                {
                    return new PaymentVerificationResult
                    {
                        Success = false,
                        Message = $"Amount mismatch. Expected: K{charge.ChargeAmount:F2}, Received: K{amount:F2}"
                    };
                }

                // Check if payment is already processed
                if (charge.Status == "paid")
                {
                    return new PaymentVerificationResult
                    {
                        Success = false,
                        Message = "Payment has already been processed"
                    };
                }

                // Update charge status
                charge.Status = "paid";
                charge.PaymentMethod = paymentMethod;
                charge.PaymentDate = DateTime.UtcNow;
                charge.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = charge.TenantId,
                    UserId = charge.UserId,
                    ChargeId = charge.Id,
                    TransactionReference = paymentReference,
                    Amount = amount,
                    Currency = "ZMW",
                    PaymentMethod = paymentMethod,
                    Status = "completed",
                    TransactionDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Notify tenant about successful payment
                await NotifyPaymentSuccessAsync(charge, transaction);

                // Log the payment verification
                _logger.LogInformation($"Payment verified successfully. Reference: {paymentReference}, Amount: K{amount:F2}, Tenant: {charge.Tenant.Name}");

                return new PaymentVerificationResult
                {
                    Success = true,
                    Message = "Payment verified and processed successfully",
                    TransactionId = transaction.Id,
                    ChargeId = charge.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying payment with reference: {paymentReference}");
                return new PaymentVerificationResult
                {
                    Success = false,
                    Message = "An error occurred while verifying payment"
                };
            }
        }

        public async Task<bool> ProcessRefundAsync(string chargeId, string reason)
        {
            try
            {
                var charge = await _context.AdditionalUserCharges
                    .Include(c => c.Tenant)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id.ToString() == chargeId);

                if (charge == null)
                {
                    return false;
                }

                if (charge.Status != "paid")
                {
                    return false; // Can only refund paid charges
                }

                // Create refund transaction
                var refundTransaction = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = charge.TenantId,
                    UserId = charge.UserId,
                    ChargeId = charge.Id,
                    TransactionReference = $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}",
                    Amount = -charge.ChargeAmount, // Negative amount for refund
                    Currency = "ZMW",
                    PaymentMethod = charge.PaymentMethod,
                    Status = "refunded",
                    TransactionDate = DateTime.UtcNow,
                    RefundReason = reason,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(refundTransaction);

                // Update charge status
                charge.Status = "refunded";
                charge.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Notify about refund
                await NotifyRefundProcessedAsync(charge, refundTransaction);

                _logger.LogInformation($"Refund processed for charge {chargeId}. Amount: K{charge.ChargeAmount:F2}, Reason: {reason}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing refund for charge: {chargeId}");
                return false;
            }
        }

        public async Task<IEnumerable<PaymentTransactionDto>> GetPaymentHistoryAsync(Guid tenantId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            return await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId && 
                           pt.TransactionDate >= startDate && 
                           pt.TransactionDate < endDate)
                .Include(pt => pt.User)
                .Include(pt => pt.Charge)
                .OrderByDescending(pt => pt.TransactionDate)
                .Select(pt => new PaymentTransactionDto
                {
                    Id = pt.Id,
                    TransactionReference = pt.TransactionReference,
                    Amount = pt.Amount,
                    Currency = pt.Currency,
                    PaymentMethod = pt.PaymentMethod,
                    Status = pt.Status,
                    TransactionDate = pt.TransactionDate,
                    RefundReason = pt.RefundReason,
                    UserEmail = pt.User.Email,
                    UserName = $"{pt.User.FirstName} {pt.User.LastName}",
                    ChargeBillingMonth = pt.Charge.BillingMonth
                })
                .ToListAsync();
        }

        public async Task<PaymentSummaryDto> GetPaymentSummaryAsync(Guid tenantId)
        {
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var nextMonth = currentMonth.AddMonths(1);

            var charges = await _context.AdditionalUserCharges
                .Where(c => c.TenantId == tenantId)
                .ToListAsync();

            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.TenantId == tenantId)
                .ToListAsync();

            var currentMonthCharges = charges.Count(c => c.BillingMonth == currentMonth);
            var currentMonthPaid = charges.Count(c => c.BillingMonth == currentMonth && c.Status == "paid");
            var currentMonthPending = charges.Count(c => c.BillingMonth == currentMonth && c.Status == "pending_payment");
            var currentMonthTotal = charges
                .Where(c => c.BillingMonth == currentMonth)
                .Sum(c => c.ChargeAmount);

            var totalPaid = transactions
                .Where(pt => pt.Status == "completed" && pt.Amount > 0)
                .Sum(pt => pt.Amount);

            var totalRefunded = transactions
                .Where(pt => pt.Status == "refunded")
                .Sum(pt => Math.Abs(pt.Amount));

            return new PaymentSummaryDto
            {
                CurrentMonthCharges = currentMonthCharges,
                CurrentMonthPaid = currentMonthPaid,
                CurrentMonthPending = currentMonthPending,
                CurrentMonthTotalAmount = currentMonthTotal,
                TotalPaidToDate = totalPaid,
                TotalRefundedToDate = totalRefunded,
                NetPaymentsToDate = totalPaid - totalRefunded,
                OutstandingBalance = currentMonthTotal - (charges
                    .Where(c => c.BillingMonth == currentMonth && c.Status == "paid")
                    .Sum(c => c.ChargeAmount))
            };
        }

        private async Task NotifyPaymentSuccessAsync(AdditionalUserCharge charge, PaymentTransaction transaction)
        {
            var tenantAdmins = await _context.Users
                .Where(u => u.TenantId == charge.TenantId && 
                           u.IsActive && 
                           (u.Role == "admin" || u.Role == "super_admin"))
                .ToListAsync();

            foreach (var admin in tenantAdmins)
            {
                await _notificationService.CreateNotificationAsync(
                    charge.TenantId,
                    admin.Id,
                    new CreateNotificationRequest
                    {
                        Type = "payment_success",
                        Title = "Payment Received",
                        Message = $"Payment of K{charge.ChargeAmount:F2} for additional user {charge.User.FirstName} {charge.User.LastName} has been received and verified.",
                        Data = new Dictionary<string, object>
                        {
                            { "chargeId", charge.Id },
                            { "transactionId", transaction.Id },
                            { "amount", charge.ChargeAmount },
                            { "paymentMethod", charge.PaymentMethod },
                            { "userEmail", charge.User.Email }
                        },
                        ActionUrl = $"/admin/additional-users/charges/{charge.Id}",
                        IsHighPriority = false
                    });
            }

            // Also notify operations team
            await NotifyOperationsTeamAsync(charge, "payment_received", "Payment Received", 
                $"Payment of K{charge.ChargeAmount:F2} received from {charge.Tenant.Name} for additional user.");
        }

        private async Task NotifyRefundProcessedAsync(AdditionalUserCharge charge, PaymentTransaction refundTransaction)
        {
            var tenantAdmins = await _context.Users
                .Where(u => u.TenantId == charge.TenantId && 
                           u.IsActive && 
                           (u.Role == "admin" || u.Role == "super_admin"))
                .ToListAsync();

            foreach (var admin in tenantAdmins)
            {
                await _notificationService.CreateNotificationAsync(
                    charge.TenantId,
                    admin.Id,
                    new CreateNotificationRequest
                    {
                        Type = "payment_refunded",
                        Title = "Payment Refunded",
                        Message = $"Refund of K{Math.Abs(refundTransaction.Amount):F2} has been processed for additional user {charge.User.FirstName} {charge.User.LastName}. Reason: {refundTransaction.RefundReason}",
                        Data = new Dictionary<string, object>
                        {
                            { "chargeId", charge.Id },
                            { "refundTransactionId", refundTransaction.Id },
                            { "refundAmount", Math.Abs(refundTransaction.Amount) },
                            { "refundReason", refundTransaction.RefundReason }
                        },
                        ActionUrl = $"/admin/additional-users/charges/{charge.Id}",
                        IsHighPriority = true
                    });
            }

            // Also notify operations team
            await NotifyOperationsTeamAsync(charge, "payment_refunded", "Payment Refunded", 
                $"Refund of K{Math.Abs(refundTransaction.Amount):F2} processed for {charge.Tenant.Name}. Reason: {refundTransaction.RefundReason}");
        }

        private async Task NotifyOperationsTeamAsync(AdditionalUserCharge charge, string notificationType, string title, string message)
        {
            var operationsUsers = await _context.Users
                .Where(u => u.IsActive && (u.Role == "operations" || u.Role == "super_admin"))
                .ToListAsync();

            foreach (var opsUser in operationsUsers)
            {
                await _notificationService.CreateNotificationAsync(
                    opsUser.TenantId,
                    opsUser.Id,
                    new CreateNotificationRequest
                    {
                        Type = notificationType,
                        Title = title,
                        Message = message,
                        Data = new Dictionary<string, object>
                        {
                            { "tenantId", charge.TenantId },
                            { "tenantName", charge.Tenant.Name },
                            { "chargeId", charge.Id },
                            { "amount", charge.ChargeAmount }
                        },
                        ActionUrl = $"/operations/additional-users/charges/{charge.Id}",
                        IsHighPriority = notificationType == "payment_refunded"
                    });
            }
        }
    }

    // DTOs
    public class PaymentVerificationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? TransactionId { get; set; }
        public Guid? ChargeId { get; set; }
    }

    public class PaymentTransactionDto
    {
        public Guid Id { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string? RefundReason { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime ChargeBillingMonth { get; set; }
    }

    public class PaymentSummaryDto
    {
        public int CurrentMonthCharges { get; set; }
        public int CurrentMonthPaid { get; set; }
        public int CurrentMonthPending { get; set; }
        public decimal CurrentMonthTotalAmount { get; set; }
        public decimal TotalPaidToDate { get; set; }
        public decimal TotalRefundedToDate { get; set; }
        public decimal NetPaymentsToDate { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    // Additional entity for payment transactions
    public class PaymentTransaction
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public Guid ChargeId { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public DateTime TransactionDate { get; set; }
        public string? RefundReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual AdditionalUserCharge Charge { get; set; } = null!;
    }
}
