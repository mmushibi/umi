using System;
using System.Collections.Generic;

namespace UmiHealth.Application.DTOs.Queue
{
    // Base patient DTO
    public class QueuePatientDto
    {
        public Guid Id { get; set; }
        public string QueueNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Complaint { get; set; } = string.Empty;
        public DateTime JoinTime { get; set; }
        public string Status { get; set; } = "waiting"; // waiting, serving, completed
        public string Priority { get; set; } = "normal"; // normal, urgent, emergency
        public Guid? AssignedProviderId { get; set; }
        public string? AssignedProviderName { get; set; }
        public int? Position { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? CompleteTime { get; set; }
        public string? Notes { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    // Request DTOs
    public class AddPatientRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Complaint { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal";
        public string? Notes { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public Guid? AssignedProviderId { get; set; }
    }

    public class UpdatePositionRequest
    {
        public int NewPosition { get; set; }
    }

    public class UpdatePriorityRequest
    {
        public string Priority { get; set; } = string.Empty;
    }

    public class AssignProviderRequest
    {
        public Guid ProviderId { get; set; }
    }

    public class BulkOperationRequest
    {
        public List<Guid> PatientIds { get; set; } = new();
    }

    public class DailyReportRequest
    {
        public DateTime Date { get; set; }
    }

    // Response DTOs
    public class QueueDataResponse
    {
        public List<QueuePatientDto> Current { get; set; } = new();
        public List<QueuePatientDto> Completed { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class QueueStatsResponse
    {
        public int WaitingCount { get; set; }
        public int ServingCount { get; set; }
        public int CompletedCount { get; set; }
        public double AverageWaitTime { get; set; }
        public int TotalPatientsToday { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class QueuePatientResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public QueuePatientDto? Patient { get; set; }
    }

    public class QueueHistoryResponse
    {
        public List<QueueHistoryItemDto> History { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class QueueHistoryItemDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty; // add, serve, remove, complete, update_priority, etc.
        public string PatientName { get; set; } = string.Empty;
        public string QueueNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public Guid? PatientId { get; set; }
    }

    public class EmergencyQueueResponse
    {
        public List<QueuePatientDto> EmergencyPatients { get; set; } = new();
        public int Count { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ProvidersResponse
    {
        public List<ProviderDto> Providers { get; set; } = new();
    }

    public class ProviderDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int CurrentPatientCount { get; set; }
        public string? Department { get; set; }
    }

    // Filter DTOs
    public class QueueHistoryFilters
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Action { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class QueueExportFilters
    {
        public string Format { get; set; } = "pdf"; // pdf, csv
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
    }

    // Analytics DTOs
    public class QueueAnalyticsResponse
    {
        public QueueWaitTimeAnalytics WaitTimeAnalytics { get; set; } = new();
        public QueueVolumeAnalytics VolumeAnalytics { get; set; } = new();
        public QueueProviderAnalytics ProviderAnalytics { get; set; } = new();
        public List<HourlyQueueData> HourlyData { get; set; } = new();
        public List<DailyQueueData> DailyData { get; set; } = new();
    }

    public class QueueWaitTimeAnalytics
    {
        public double AverageWaitTime { get; set; }
        public double MedianWaitTime { get; set; }
        public double MinWaitTime { get; set; }
        public double MaxWaitTime { get; set; }
        public double TargetWaitTime { get; set; }
        public double PerformancePercentage { get; set; }
    }

    public class QueueVolumeAnalytics
    {
        public int TotalPatients { get; set; }
        public int PatientsServed { get; set; }
        public int PatientsWaiting { get; set; }
        public double ServiceRate { get; set; }
        public List<string> PeakHours { get; set; } = new();
    }

    public class QueueProviderAnalytics
    {
        public List<ProviderPerformanceDto> ProviderPerformance { get; set; } = new();
        public double AveragePatientsPerProvider { get; set; }
        public double AverageServiceTimePerProvider { get; set; }
    }

    public class ProviderPerformanceDto
    {
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public int PatientsServed { get; set; }
        public double AverageServiceTime { get; set; }
        public double SatisfactionScore { get; set; }
        public bool IsPerformingWell { get; set; }
    }

    public class HourlyQueueData
    {
        public int Hour { get; set; }
        public int PatientCount { get; set; }
        public double AverageWaitTime { get; set; }
        public int PatientsServed { get; set; }
    }

    public class DailyQueueData
    {
        public DateTime Date { get; set; }
        public int TotalPatients { get; set; }
        public double AverageWaitTime { get; set; }
        public int PatientsServed { get; set; }
        public double SatisfactionScore { get; set; }
    }

    // Notification DTOs
    public class QueueNotificationRequest
    {
        public Guid PatientId { get; set; }
        public string NotificationType { get; set; } = string.Empty; // sms, whatsapp, email
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    public class QueueNotificationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? NotificationId { get; set; }
    }

    // Settings DTOs
    public class QueueSettingsDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid BranchId { get; set; }
        public double TargetWaitTime { get; set; } // in minutes
        public double AutoEscalateWaitTime { get; set; } // in minutes
        public int MaxQueueSize { get; set; }
        public bool EnableAutoNotifications { get; set; }
        public bool EnableSoundNotifications { get; set; }
        public bool EnableSmsNotifications { get; set; }
        public bool EnableWhatsAppNotifications { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public string? DefaultNotificationMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class UpdateQueueSettingsRequest
    {
        public double TargetWaitTime { get; set; }
        public double AutoEscalateWaitTime { get; set; }
        public int MaxQueueSize { get; set; }
        public bool EnableAutoNotifications { get; set; }
        public bool EnableSoundNotifications { get; set; }
        public bool EnableSmsNotifications { get; set; }
        public bool EnableWhatsAppNotifications { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public string? DefaultNotificationMessage { get; set; }
    }
}
