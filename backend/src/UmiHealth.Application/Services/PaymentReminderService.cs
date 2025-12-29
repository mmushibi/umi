using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public interface IPaymentReminderService
    {
        Task<PaymentReminderResult> CreatePaymentReminderAsync(PaymentReminderRequest request);
        Task<List<PaymentReminderDto>> GetPaymentRemindersAsync(Guid tenantId);
        Task<bool> UpdatePaymentReminderAsync(Guid reminderId, PaymentReminderUpdateRequest request);
        Task<bool> DeletePaymentReminderAsync(Guid reminderId);
        Task ProcessScheduledRemindersAsync();
        Task<PaymentReminderTemplateResult> CreateReminderTemplateAsync(PaymentReminderTemplateRequest request);
        Task<List<PaymentReminderTemplateDto>> GetReminderTemplatesAsync(Guid tenantId);
        Task<PaymentLinkResult> GeneratePaymentLinkAsync(PaymentLinkRequest request);
        Task<string> SendPaymentReminderAsync(PaymentReminder reminder);
    }

    public class PaymentReminderService : IPaymentReminderService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PaymentReminderService> _logger;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;

        public PaymentReminderService(
            SharedDbContext context,
            ILogger<PaymentReminderService> logger,
            IConfiguration configuration,
            INotificationService notificationService,
            IEmailService emailService,
            ISmsService smsService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _notificationService = notificationService;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task<PaymentReminderResult> CreatePaymentReminderAsync(PaymentReminderRequest request)
        {
            var reminder = new PaymentReminder
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CustomerId = request.CustomerId,
                ChargeId = request.ChargeId,
                Amount = request.Amount,
                Currency = request.Currency,
                DueDate = request.DueDate,
                ReminderType = request.ReminderType,
                ReminderFrequency = request.ReminderFrequency,
                ReminderDaysBefore = request.ReminderDaysBefore,
                TemplateId = request.TemplateId,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.PaymentReminders.Add(reminder);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created payment reminder {reminder.Id} for tenant {request.TenantId}");

            return new PaymentReminderResult
            {
                Success = true,
                ReminderId = reminder.Id,
                Message = "Payment reminder created successfully"
            };
        }

        public async Task<List<PaymentReminderDto>> GetPaymentRemindersAsync(Guid tenantId)
        {
            return await _context.PaymentReminders
                .Where(pr => pr.TenantId == tenantId)
                .Include(pr => pr.Customer)
                .Include(pr => pr.Charge)
                .Include(pr => pr.Template)
                .Select(pr => new PaymentReminderDto
                {
                    Id = pr.Id,
                    CustomerId = pr.CustomerId,
                    CustomerName = pr.Customer.FirstName + " " + pr.Customer.LastName,
                    CustomerEmail = pr.Customer.Email,
                    CustomerPhone = pr.Customer.PhoneNumber,
                    ChargeId = pr.ChargeId,
                    Amount = pr.Amount,
                    Currency = pr.Currency,
                    DueDate = pr.DueDate,
                    ReminderType = pr.ReminderType,
                    ReminderFrequency = pr.ReminderFrequency,
                    ReminderDaysBefore = pr.ReminderDaysBefore,
                    TemplateId = pr.TemplateId,
                    TemplateName = pr.Template.Name,
                    IsEnabled = pr.IsEnabled,
                    LastSentDate = pr.LastSentDate,
                    NextReminderDate = pr.NextReminderDate,
                    CreatedAt = pr.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> UpdatePaymentReminderAsync(Guid reminderId, PaymentReminderUpdateRequest request)
        {
            var reminder = await _context.PaymentReminders.FindAsync(reminderId);
            if (reminder == null)
                return false;

            reminder.ReminderType = request.ReminderType ?? reminder.ReminderType;
            reminder.ReminderFrequency = request.ReminderFrequency ?? reminder.ReminderFrequency;
            reminder.ReminderDaysBefore = request.ReminderDaysBefore ?? reminder.ReminderDaysBefore;
            reminder.TemplateId = request.TemplateId ?? reminder.TemplateId;
            reminder.IsEnabled = request.IsEnabled ?? reminder.IsEnabled;
            reminder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated payment reminder {reminderId}");
            return true;
        }

        public async Task<bool> DeletePaymentReminderAsync(Guid reminderId)
        {
            var reminder = await _context.PaymentReminders.FindAsync(reminderId);
            if (reminder == null)
                return false;

            _context.PaymentReminders.Remove(reminder);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted payment reminder {reminderId}");
            return true;
        }

        public async Task ProcessScheduledRemindersAsync()
        {
            var dueReminders = await _context.PaymentReminders
                .Where(pr => pr.IsEnabled && 
                           pr.NextReminderDate <= DateTime.UtcNow &&
                           (!pr.LastSentDate.HasValue || 
                            pr.LastSentDate.Value < DateTime.UtcNow.AddDays(-1))) // Prevent spam
                .Include(pr => pr.Customer)
                .Include(pr => pr.Charge)
                .Include(pr => pr.Template)
                .ToListAsync();

            foreach (var reminder in dueReminders)
            {
                try
                {
                    await SendPaymentReminderAsync(reminder);
                    
                    // Update next reminder date
                    reminder.LastSentDate = DateTime.UtcNow;
                    reminder.NextReminderDate = CalculateNextReminderDate(reminder);
                    reminder.UpdatedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending payment reminder {reminder.Id}");
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<string> SendPaymentReminderAsync(PaymentReminder reminder)
        {
            var message = ProcessTemplate(reminder.Template, reminder);
            
            if (reminder.ReminderType == "email")
            {
                await _emailService.SendEmailAsync(
                    reminder.Customer.Email,
                    reminder.Template.Subject,
                    message);
            }
            else if (reminder.ReminderType == "sms")
            {
                await _smsService.SendSmsAsync(
                    reminder.Customer.PhoneNumber,
                    message);
            }
            else if (reminder.ReminderType == "both")
            {
                await _emailService.SendEmailAsync(
                    reminder.Customer.Email,
                    reminder.Template.Subject,
                    message);
                
                await _smsService.SendSmsAsync(
                    reminder.Customer.PhoneNumber,
                    message);
            }

            _logger.LogInformation($"Sent {reminder.ReminderType} payment reminder to {reminder.Customer.Email}");
            return message;
        }

        public async Task<PaymentReminderTemplateResult> CreateReminderTemplateAsync(PaymentReminderTemplateRequest request)
        {
            var template = new PaymentReminderTemplate
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Name = request.Name,
                Subject = request.Subject,
                EmailTemplate = request.EmailTemplate,
                SmsTemplate = request.SmsTemplate,
                TemplateType = request.TemplateType,
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            // If this is set as default, unset other defaults
            if (request.IsDefault)
            {
                var existingDefaults = await _context.PaymentReminderTemplates
                    .Where(t => t.TenantId == request.TenantId && t.IsDefault)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }

            _context.PaymentReminderTemplates.Add(template);
            await _context.SaveChangesAsync();

            return new PaymentReminderTemplateResult
            {
                Success = true,
                TemplateId = template.Id,
                Message = "Reminder template created successfully"
            };
        }

        public async Task<List<PaymentReminderTemplateDto>> GetReminderTemplatesAsync(Guid tenantId)
        {
            return await _context.PaymentReminderTemplates
                .Where(t => t.TenantId == tenantId)
                .OrderBy(t => t.Name)
                .Select(t => new PaymentReminderTemplateDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Subject = t.Subject,
                    EmailTemplate = t.EmailTemplate,
                    SmsTemplate = t.SmsTemplate,
                    TemplateType = t.TemplateType,
                    IsDefault = t.IsDefault,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<PaymentLinkResult> GeneratePaymentLinkAsync(PaymentLinkRequest request)
        {
            var paymentLink = new PaymentLink
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CustomerId = request.CustomerId,
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
                MaxUses = request.MaxUses,
                CurrentUses = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.PaymentLinks.Add(paymentLink);
            await _context.SaveChangesAsync();

            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://umihealth.com";
            var linkUrl = $"{baseUrl}/pay/{paymentLink.Id}";

            return new PaymentLinkResult
            {
                Success = true,
                PaymentLinkId = paymentLink.Id,
                PaymentUrl = linkUrl,
                ExpiresAt = paymentLink.ExpiresAt,
                Message = "Payment link generated successfully"
            };
        }

        #region Helper Methods

        private DateTime? CalculateNextReminderDate(PaymentReminder reminder)
        {
            if (reminder.DueDate <= DateTime.UtcNow)
                return null; // Payment is already due

            var reminderDate = reminder.DueDate.AddDays(-reminder.ReminderDaysBefore);

            return reminder.ReminderFrequency.ToLower() switch
            {
                "daily" => reminderDate.AddDays(1),
                "weekly" => reminderDate.AddDays(7),
                "monthly" => reminderDate.AddMonths(1),
                _ => reminderDate
            };
        }

        private string ProcessTemplate(PaymentReminderTemplate template, PaymentReminder reminder)
        {
            var message = reminder.ReminderType == "sms" ? template.SmsTemplate : template.EmailTemplate;
            
            return message
                .Replace("{{CustomerName}}", $"{reminder.Customer.FirstName} {reminder.Customer.LastName}")
                .Replace("{{Amount}}", $"{reminder.Amount:C}")
                .Replace("{{Currency}}", reminder.Currency)
                .Replace("{{DueDate}}", reminder.DueDate.ToString("dd MMM yyyy"))
                .Replace("{{DaysUntilDue}}", Math.Max(0, (reminder.DueDate - DateTime.UtcNow).Days).ToString())
                .Replace("{{ChargeReference}}", reminder.Charge?.PaymentReference ?? "N/A")
                .Replace("{{TenantName}}", reminder.Charge?.Tenant?.Name ?? "Your Organization");
        }

        #endregion
    }

    // Supporting DTOs and Entities
    public class PaymentReminderRequest
    {
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? ChargeId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public DateTime DueDate { get; set; }
        public string ReminderType { get; set; } = string.Empty; // email, sms, both
        public string ReminderFrequency { get; set; } = "once"; // once, daily, weekly, monthly
        public int ReminderDaysBefore { get; set; } = 3;
        public Guid? TemplateId { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class PaymentReminderUpdateRequest
    {
        public string? ReminderType { get; set; }
        public string? ReminderFrequency { get; set; }
        public int? ReminderDaysBefore { get; set; }
        public Guid? TemplateId { get; set; }
        public bool? IsEnabled { get; set; }
    }

    public class PaymentReminderResult
    {
        public bool Success { get; set; }
        public Guid ReminderId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PaymentReminderDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public Guid? ChargeId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string ReminderType { get; set; } = string.Empty;
        public string ReminderFrequency { get; set; } = string.Empty;
        public int ReminderDaysBefore { get; set; }
        public Guid? TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime? LastSentDate { get; set; }
        public DateTime? NextReminderDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentReminderTemplateRequest
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string EmailTemplate { get; set; } = string.Empty;
        public string SmsTemplate { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty; // payment_due, overdue, payment_reminder
        public bool IsDefault { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class PaymentReminderTemplateResult
    {
        public bool Success { get; set; }
        public Guid TemplateId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PaymentReminderTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string EmailTemplate { get; set; } = string.Empty;
        public string SmsTemplate { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentLinkRequest
    {
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string Description { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUses { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class PaymentLinkResult
    {
        public bool Success { get; set; }
        public Guid PaymentLinkId { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Additional Entities
    public class PaymentReminder
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? ChargeId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public DateTime DueDate { get; set; }
        public string ReminderType { get; set; } = string.Empty; // email, sms, both
        public string ReminderFrequency { get; set; } = "once"; // once, daily, weekly, monthly
        public int ReminderDaysBefore { get; set; } = 3;
        public Guid? TemplateId { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastSentDate { get; set; }
        public DateTime? NextReminderDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
        public virtual AdditionalUserCharge? Charge { get; set; }
        public virtual PaymentReminderTemplate? Template { get; set; }
    }

    public class PaymentReminderTemplate
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string EmailTemplate { get; set; } = string.Empty;
        public string SmsTemplate { get; set; } = string.Empty;
        public string TemplateType { get; set; } = string.Empty; // payment_due, overdue, payment_reminder
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
    }

    public class PaymentLink
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZMW";
        public string Description { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUses { get; set; }
        public int CurrentUses { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }

        // Navigation properties
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual User Customer { get; set; } = null!;
    }

    // Service interfaces for email and SMS
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }
}
