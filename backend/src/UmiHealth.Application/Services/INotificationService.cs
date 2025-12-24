using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Application.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid tenantId, Guid userId, int page = 1, int pageSize = 20, string? type = null, bool? unreadOnly = null);
        Task<NotificationDto> GetNotificationByIdAsync(Guid tenantId, Guid userId, Guid notificationId);
        Task<bool> MarkAsReadAsync(Guid tenantId, Guid userId, Guid notificationId);
        Task MarkAllAsReadAsync(Guid tenantId, Guid userId);
        Task<bool> DeleteNotificationAsync(Guid tenantId, Guid userId, Guid notificationId);
        Task<NotificationDto> CreateNotificationAsync(Guid tenantId, Guid senderId, CreateNotificationRequest request);
        Task BroadcastNotificationAsync(Guid tenantId, Guid senderId, BroadcastNotificationRequest request);
        Task<int> GetUnreadCountAsync(Guid tenantId, Guid userId);
        Task<NotificationSettingsDto> GetNotificationSettingsAsync(Guid tenantId, Guid userId);
        Task<NotificationSettingsDto> UpdateNotificationSettingsAsync(Guid tenantId, Guid userId, UpdateNotificationSettingsRequest request);
        Task SendTestNotificationAsync(Guid tenantId, Guid senderId, TestNotificationRequest request);
        
        // System-triggered notifications
        Task SendLowStockAlertAsync(Guid tenantId, Guid branchId, IEnumerable<LowStockItemDto> items);
        Task SendExpiryAlertAsync(Guid tenantId, Guid branchId, IEnumerable<ExpiringItemDto> items);
        Task SendPrescriptionAlertAsync(Guid tenantId, Guid prescriptionId, string alertType);
        Task SendPaymentAlertAsync(Guid tenantId, Guid saleId, string paymentStatus);
        Task SendSystemAlertAsync(Guid tenantId, string alertType, string message, Dictionary<string, object>? data = null);
    }
}
