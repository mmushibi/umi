using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Application.Services
{
    public interface INotificationService
    {
        Task<bool> SendNotificationAsync(Guid userId, string title, string message, string type = "info", CancellationToken cancellationToken = default);
        Task<bool> SendTenantNotificationAsync(Guid tenantId, string title, string message, string type = "info", CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);
        Task<bool> MarkNotificationAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
        Task<bool> MarkAllNotificationsAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public class NotificationService : INotificationService
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(UmiHealthDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> SendNotificationAsync(Guid userId, string title, string message, string type = "info", CancellationToken cancellationToken = default)
        {
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Sent notification {NotificationId} to user {UserId}", notification.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SendTenantNotificationAsync(Guid tenantId, string title, string message, string type = "info", CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all active users in the tenant
                var users = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.IsActive)
                    .ToListAsync(cancellationToken);

                var notifications = users.Select(user => new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Sent tenant notification to {Count} users in tenant {TenantId}", 
                    notifications.Count, tenantId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tenant notification to tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return Enumerable.Empty<Notification>();
            }
        }

        public async Task<bool> MarkNotificationAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

                if (notification == null)
                    return false;

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Marked notification {NotificationId} as read", notificationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
                return false;
            }
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync(cancellationToken);

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", 
                    unreadNotifications.Count, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count for user {UserId}", userId);
                return 0;
            }
        }
    }
}
