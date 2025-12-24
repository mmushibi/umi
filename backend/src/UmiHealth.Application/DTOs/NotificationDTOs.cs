using System;
using System.Collections.Generic;

namespace UmiHealth.Application.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public bool IsHighPriority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class CreateNotificationRequest
    {
        public Guid? UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
        public string? ActionUrl { get; set; }
        public bool IsHighPriority { get; set; } = false;
    }

    public class BroadcastNotificationRequest
    {
        public List<Guid>? UserIds { get; set; }
        public List<string>? Roles { get; set; }
        public List<Guid>? BranchIds { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
        public string? ActionUrl { get; set; }
        public bool IsHighPriority { get; set; } = false;
    }

    public class TestNotificationRequest
    {
        public Guid? UserId { get; set; }
        public string Type { get; set; } = "test";
        public string Title { get; set; } = "Test Notification";
        public string Message { get; set; } = "This is a test notification";
    }

    public class NotificationSettingsDto
    {
        public bool EmailEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool PushEnabled { get; set; }
        public bool LowStockAlerts { get; set; }
        public bool ExpiryAlerts { get; set; }
        public bool PrescriptionAlerts { get; set; }
        public bool PaymentAlerts { get; set; }
        public bool SystemAlerts { get; set; }
        public Dictionary<string, bool> CustomAlerts { get; set; } = new();
    }

    public class UpdateNotificationSettingsRequest
    {
        public bool EmailEnabled { get; set; }
        public bool SmsEnabled { get; set; }
        public bool PushEnabled { get; set; }
        public bool LowStockAlerts { get; set; }
        public bool ExpiryAlerts { get; set; }
        public bool PrescriptionAlerts { get; set; }
        public bool PaymentAlerts { get; set; }
        public bool SystemAlerts { get; set; }
        public Dictionary<string, bool>? CustomAlerts { get; set; }
    }

    public class LowStockItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }

    public class ExpiringItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int DaysUntilExpiry { get; set; }
    }
}
