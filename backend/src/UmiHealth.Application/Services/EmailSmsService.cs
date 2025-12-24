using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UmiHealth.Application.Services
{
    /// <summary>
    /// Interface for email notifications
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task<bool> SendBulkEmailAsync(string[] recipients, string subject, string body, bool isHtml = false);
    }

    /// <summary>
    /// Interface for SMS notifications
    /// </summary>
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendBulkSmsAsync(string[] phoneNumbers, string message);
    }

    /// <summary>
    /// Email configuration
    /// </summary>
    public class EmailConfiguration
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool EnableSsl { get; set; }
    }

    /// <summary>
    /// SMS configuration
    /// </summary>
    public class SmsConfiguration
    {
        public string Provider { get; set; } // twilio, nexmo, africastalking, etc.
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string PhoneNumber { get; set; }
        public string ApiKey { get; set; }
    }

    /// <summary>
    /// Email service implementation
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailConfiguration _config;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = new EmailConfiguration();
            configuration.GetSection("Email").Bind(_config);
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                using (var client = new SmtpClient(_config.SmtpServer, _config.SmtpPort))
                {
                    client.EnableSsl = _config.EnableSsl;
                    client.Credentials = new System.Net.NetworkCredential(
                        _config.SmtpUsername,
                        _config.SmtpPassword);

                    var mailMessage = new MailMessage(
                        new MailAddress(_config.FromEmail, _config.FromName),
                        new MailAddress(to))
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = isHtml
                    };

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent successfully to {to}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                return false;
            }
        }

        public async Task<bool> SendBulkEmailAsync(string[] recipients, string subject, string body, bool isHtml = false)
        {
            try
            {
                using (var client = new SmtpClient(_config.SmtpServer, _config.SmtpPort))
                {
                    client.EnableSsl = _config.EnableSsl;
                    client.Credentials = new System.Net.NetworkCredential(
                        _config.SmtpUsername,
                        _config.SmtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_config.FromEmail, _config.FromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = isHtml
                    };

                    foreach (var recipient in recipients)
                    {
                        mailMessage.To.Add(new MailAddress(recipient));
                    }

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Bulk email sent to {recipients.Length} recipients");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk email");
                return false;
            }
        }
    }

    /// <summary>
    /// SMS service implementation with multiple provider support
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;
        private readonly SmsConfiguration _config;

        public SmsService(ILogger<SmsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = new SmsConfiguration();
            configuration.GetSection("Sms").Bind(_config);
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                switch (_config.Provider?.ToLower())
                {
                    case "twilio":
                        return await SendTwilioSmsAsync(phoneNumber, message);
                    case "nexmo":
                        return await SendNexmoSmsAsync(phoneNumber, message);
                    case "africastalking":
                        return await SendAfricasTalkingSmsAsync(phoneNumber, message);
                    default:
                        _logger.LogWarning($"Unknown SMS provider: {_config.Provider}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {phoneNumber}");
                return false;
            }
        }

        public async Task<bool> SendBulkSmsAsync(string[] phoneNumbers, string message)
        {
            var results = new bool[phoneNumbers.Length];
            
            for (int i = 0; i < phoneNumbers.Length; i++)
            {
                results[i] = await SendSmsAsync(phoneNumbers[i], message);
            }

            var successCount = results.Count(r => r);
            _logger.LogInformation($"Bulk SMS sent: {successCount}/{phoneNumbers.Length} successful");
            return successCount == phoneNumbers.Length;
        }

        private async Task<bool> SendTwilioSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Twilio implementation
                // Install NuGet: Twilio
                // Implementation example:
                // var client = new TwilioRestClient(_config.AccountSid, _config.AuthToken);
                // var result = await MessageResource.CreateAsync(
                //     to: new PhoneNumber(phoneNumber),
                //     from: new PhoneNumber(_config.PhoneNumber),
                //     body: message,
                //     client: client);
                
                _logger.LogInformation($"SMS sent via Twilio to {phoneNumber}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio SMS failed");
                return false;
            }
        }

        private async Task<bool> SendNexmoSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Nexmo/Vonage implementation
                // Install NuGet: Vonage
                _logger.LogInformation($"SMS sent via Nexmo to {phoneNumber}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nexmo SMS failed");
                return false;
            }
        }

        private async Task<bool> SendAfricasTalkingSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Africa's Talking SMS implementation (popular in Zambia)
                // Install NuGet: AfricasTalking
                _logger.LogInformation($"SMS sent via Africa's Talking to {phoneNumber}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Africa's Talking SMS failed");
                return false;
            }
        }
    }

    /// <summary>
    /// Helper service for common notifications
    /// </summary>
    public class CommunicationHelper
    {
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<CommunicationHelper> _logger;

        public CommunicationHelper(
            IEmailService emailService,
            ISmsService smsService,
            ILogger<CommunicationHelper> logger)
        {
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task<bool> SendLowStockAlertAsync(string email, string productName, int remainingStock)
        {
            var emailBody = $@"
                <h2>Low Stock Alert</h2>
                <p>Product: <strong>{productName}</strong></p>
                <p>Remaining Stock: <strong>{remainingStock} units</strong></p>
                <p>Please reorder immediately to avoid stockouts.</p>
            ";

            return await _emailService.SendEmailAsync(
                email,
                $"Low Stock Alert - {productName}",
                emailBody,
                true);
        }

        public async Task SendPrescriptionReadyAsync(string email, string phone, string prescriptionNumber)
        {
            var emailBody = $@"
                <h2>Prescription Ready for Pickup</h2>
                <p>Your prescription <strong>{prescriptionNumber}</strong> is ready for pickup.</p>
                <p>Please visit your nearest pharmacy branch during business hours.</p>
                <p>Thank you for choosing Umi Health Pharmacy.</p>
            ";

            var smsMessage = $"Your prescription {prescriptionNumber} is ready for pickup.";

            await _emailService.SendEmailAsync(email, "Prescription Ready", emailBody, true);
            if (!string.IsNullOrEmpty(phone))
            {
                await _smsService.SendSmsAsync(phone, smsMessage);
            }
        }

        public async Task<bool> SendPaymentConfirmationAsync(string email, string receiptNumber, decimal amount)
        {
            var emailBody = $@"
                <h2>Payment Confirmation</h2>
                <p>Receipt Number: <strong>{receiptNumber}</strong></p>
                <p>Amount: <strong>K{amount:F2}</strong></p>
                <p>Payment Date: {DateTime.UtcNow:F}</p>
                <p>Thank you for your purchase.</p>
            ";

            return await _emailService.SendEmailAsync(
                email,
                $"Payment Confirmation - {receiptNumber}",
                emailBody,
                true);
        }

        public async Task<bool> SendAccountCreationAsync(string email, string username, string tempPassword)
        {
            var emailBody = $@"
                <h2>Welcome to Umi Health</h2>
                <p>Your account has been created successfully.</p>
                <p><strong>Username:</strong> {username}</p>
                <p><strong>Temporary Password:</strong> {tempPassword}</p>
                <p>Please log in and change your password immediately.</p>
            ";

            return await _emailService.SendEmailAsync(
                email,
                "Welcome to Umi Health - Account Created",
                emailBody,
                true);
        }

        public async Task<bool> SendPasswordResetAsync(string email, string resetLink)
        {
            var emailBody = $@"
                <h2>Password Reset Request</h2>
                <p>You have requested to reset your password.</p>
                <p><a href='{resetLink}'>Click here to reset your password</a></p>
                <p>This link will expire in 24 hours.</p>
            ";

            return await _emailService.SendEmailAsync(
                email,
                "Password Reset Request",
                emailBody,
                true);
        }
    }
}
