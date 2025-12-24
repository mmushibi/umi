using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Application.DTOs;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(SharedDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid tenantId, Guid userId, int page = 1, int pageSize = 20, string? type = null, bool? unreadOnly = null)
        {
            var query = _context.Notifications
                .Where(n => n.TenantId == tenantId && n.UserId == userId && n.DeletedAt == null);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(n => n.Type == type);

            if (unreadOnly == true)
                query = query.Where(n => !n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                Data = n.Data ?? new Dictionary<string, object>(),
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                IsHighPriority = n.IsHighPriority,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            });
        }

        public async Task<NotificationDto> GetNotificationByIdAsync(Guid tenantId, Guid userId, Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && 
                                        n.TenantId == tenantId && 
                                        n.UserId == userId && 
                                        n.DeletedAt == null);

            if (notification == null)
                return null;

            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                Data = notification.Data ?? new Dictionary<string, object>(),
                ActionUrl = notification.ActionUrl,
                IsRead = notification.IsRead,
                IsHighPriority = notification.IsHighPriority,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            };
        }

        public async Task<bool> MarkAsReadAsync(Guid tenantId, Guid userId, Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && 
                                        n.TenantId == tenantId && 
                                        n.UserId == userId && 
                                        n.DeletedAt == null);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task MarkAllAsReadAsync(Guid tenantId, Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.TenantId == tenantId && 
                           n.UserId == userId && 
                           !n.IsRead && 
                           n.DeletedAt == null)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            _context.Notifications.UpdateRange(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteNotificationAsync(Guid tenantId, Guid userId, Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && 
                                        n.TenantId == tenantId && 
                                        n.UserId == userId && 
                                        n.DeletedAt == null);

            if (notification == null)
                return false;

            notification.DeletedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<NotificationDto> CreateNotificationAsync(Guid tenantId, Guid senderId, CreateNotificationRequest request)
        {
            if (!request.UserId.HasValue)
                throw new ArgumentException("UserId is required for direct notifications");

            var notification = new Notification
            {
                TenantId = tenantId,
                UserId = request.UserId.Value,
                SenderId = senderId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                Data = request.Data,
                ActionUrl = request.ActionUrl,
                IsHighPriority = request.IsHighPriority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return await GetNotificationByIdAsync(tenantId, request.UserId.Value, notification.Id);
        }

        public async Task BroadcastNotificationAsync(Guid tenantId, Guid senderId, BroadcastNotificationRequest request)
        {
            var targetUsers = await GetTargetUsersAsync(tenantId, request.UserIds, request.Roles, request.BranchIds);

            var notifications = targetUsers.Select(userId => new Notification
            {
                TenantId = tenantId,
                UserId = userId,
                SenderId = senderId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                Data = request.Data,
                ActionUrl = request.ActionUrl,
                IsHighPriority = request.IsHighPriority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid tenantId, Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.TenantId == tenantId && 
                               n.UserId == userId && 
                               !n.IsRead && 
                               n.DeletedAt == null);
        }

        public async Task<NotificationSettingsDto> GetNotificationSettingsAsync(Guid tenantId, Guid userId)
        {
            var settings = await _context.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.TenantId == tenantId && ns.UserId == userId);

            if (settings == null)
            {
                // Create default settings
                settings = new NotificationSettings
                {
                    TenantId = tenantId,
                    UserId = userId,
                    EmailEnabled = true,
                    SmsEnabled = false,
                    PushEnabled = true,
                    LowStockAlerts = true,
                    ExpiryAlerts = true,
                    PrescriptionAlerts = true,
                    PaymentAlerts = true,
                    SystemAlerts = true,
                    CustomAlerts = new Dictionary<string, bool>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.NotificationSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return new NotificationSettingsDto
            {
                EmailEnabled = settings.EmailEnabled,
                SmsEnabled = settings.SmsEnabled,
                PushEnabled = settings.PushEnabled,
                LowStockAlerts = settings.LowStockAlerts,
                ExpiryAlerts = settings.ExpiryAlerts,
                PrescriptionAlerts = settings.PrescriptionAlerts,
                PaymentAlerts = settings.PaymentAlerts,
                SystemAlerts = settings.SystemAlerts,
                CustomAlerts = settings.CustomAlerts ?? new Dictionary<string, bool>()
            };
        }

        public async Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(Guid tenantId, Guid userId, UpdateNotificationSettingsRequest request)
        {
            var settings = await _context.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.TenantId == tenantId && ns.UserId == userId);

            if (settings == null)
            {
                settings = new NotificationSettings
                {
                    TenantId = tenantId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.NotificationSettings.Add(settings);
            }

            settings.EmailEnabled = request.EmailEnabled;
            settings.SmsEnabled = request.SmsEnabled;
            settings.PushEnabled = request.PushEnabled;
            settings.LowStockAlerts = request.LowStockAlerts;
            settings.ExpiryAlerts = request.ExpiryAlerts;
            settings.PrescriptionAlerts = request.PrescriptionAlerts;
            settings.PaymentAlerts = request.PaymentAlerts;
            settings.SystemAlerts = request.SystemAlerts;
            settings.CustomAlerts = request.CustomAlerts ?? new Dictionary<string, bool>();
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetNotificationSettingsAsync(tenantId, userId);
        }

        public async Task SendTestNotificationAsync(Guid tenantId, Guid senderId, TestNotificationRequest request)
        {
            var targetUserId = request.UserId ?? senderId;

            var notification = new Notification
            {
                TenantId = tenantId,
                UserId = targetUserId,
                SenderId = senderId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                Data = new Dictionary<string, object> { { "isTest", true } },
                IsHighPriority = false,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task SendLowStockAlertAsync(Guid tenantId, Guid branchId, IEnumerable<LowStockItemDto> items)
        {
            var usersToNotify = await GetUsersForAlertAsync(tenantId, branchId, "low_stock");

            foreach (var user in usersToNotify)
            {
                var notification = new Notification
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Type = "low_stock",
                    Title = "Low Stock Alert",
                    Message = $"{items.Count()} items are running low on stock",
                    Data = new Dictionary<string, object>
                    {
                        { "branchId", branchId },
                        { "items", items.ToList() }
                    },
                    ActionUrl = "/inventory",
                    IsHighPriority = true,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendExpiryAlertAsync(Guid tenantId, Guid branchId, IEnumerable<ExpiringItemDto> items)
        {
            var usersToNotify = await GetUsersForAlertAsync(tenantId, branchId, "expiry");

            foreach (var user in usersToNotify)
            {
                var notification = new Notification
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Type = "expiry",
                    Title = "Product Expiry Alert",
                    Message = $"{items.Count()} products are expiring soon",
                    Data = new Dictionary<string, object>
                    {
                        { "branchId", branchId },
                        { "items", items.ToList() }
                    },
                    ActionUrl = "/inventory",
                    IsHighPriority = true,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendPrescriptionAlertAsync(Guid tenantId, Guid prescriptionId, string alertType)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.TenantId == tenantId);

            if (prescription == null) return;

            var usersToNotify = await GetUsersForAlertAsync(tenantId, prescription.BranchId, "prescription");

            foreach (var user in usersToNotify)
            {
                var notification = new Notification
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Type = "prescription",
                    Title = $"Prescription {alertType}",
                    Message = $"Prescription for {prescription.Patient.FirstName} {prescription.Patient.LastName} is {alertType}",
                    Data = new Dictionary<string, object>
                    {
                        { "prescriptionId", prescriptionId },
                        { "patientId", prescription.PatientId },
                        { "alertType", alertType }
                    },
                    ActionUrl = $"/prescriptions/{prescriptionId}",
                    IsHighPriority = alertType == "overdue",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendPaymentAlertAsync(Guid tenantId, Guid saleId, string paymentStatus)
        {
            var sale = await _context.Sales
                .Include(s => s.Patient)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.TenantId == tenantId);

            if (sale == null) return;

            var usersToNotify = await GetUsersForAlertAsync(tenantId, sale.BranchId, "payment");

            foreach (var user in usersToNotify)
            {
                var notification = new Notification
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Type = "payment",
                    Title = $"Payment {paymentStatus}",
                    Message = $"Payment for sale {sale.SaleNumber} is {paymentStatus}",
                    Data = new Dictionary<string, object>
                    {
                        { "saleId", saleId },
                        { "saleNumber", sale.SaleNumber },
                        { "paymentStatus", paymentStatus },
                        { "amount", sale.TotalAmount }
                    },
                    ActionUrl = $"/sales/{saleId}",
                    IsHighPriority = paymentStatus == "failed",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendSystemAlertAsync(Guid tenantId, string alertType, string message, Dictionary<string, object>? data = null)
        {
            var adminUsers = await _context.Users
                .Where(u => u.TenantId == tenantId && 
                           u.IsActive && 
                           (u.Role == "admin" || u.Role == "super_admin"))
                .ToListAsync();

            foreach (var user in adminUsers)
            {
                var notification = new Notification
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Type = "system",
                    Title = $"System Alert: {alertType}",
                    Message = message,
                    Data = data ?? new Dictionary<string, object>(),
                    ActionUrl = "/admin/system",
                    IsHighPriority = true,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<List<User>> GetTargetUsersAsync(Guid tenantId, List<Guid>? userIds, List<string>? roles, List<Guid>? branchIds)
        {
            var query = _context.Users
                .Where(u => u.TenantId == tenantId && u.IsActive);

            if (userIds != null && userIds.Any())
                query = query.Where(u => userIds.Contains(u.Id));

            if (roles != null && roles.Any())
                query = query.Where(u => roles.Contains(u.Role));

            if (branchIds != null && branchIds.Any())
                query = query.Where(u => u.BranchId.HasValue && branchIds.Contains(u.BranchId.Value));

            return await query.ToListAsync();
        }

        private async Task<List<User>> GetUsersForAlertAsync(Guid tenantId, Guid branchId, string alertType)
        {
            var settings = await _context.NotificationSettings
                .Where(ns => ns.TenantId == tenantId)
                .ToListAsync();

            var alertEnabled = alertType switch
            {
                "low_stock" => settings.Any(s => s.LowStockAlerts),
                "expiry" => settings.Any(s => s.ExpiryAlerts),
                "prescription" => settings.Any(s => s.PrescriptionAlerts),
                "payment" => settings.Any(s => s.PaymentAlerts),
                _ => settings.Any(s => s.SystemAlerts)
            };

            if (!alertEnabled)
                return new List<User>();

            var query = _context.Users
                .Where(u => u.TenantId == tenantId && u.IsActive);

            // Include users at the specific branch and admins
            query = query.Where(u => 
                (u.BranchId == branchId) || 
                u.Role == "admin" || 
                u.Role == "super_admin" ||
                u.Role == "pharmacist");

            return await query.ToListAsync();
        }
    }
}
