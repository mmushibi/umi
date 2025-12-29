using System;
using UmiHealth.Core.Entities;

namespace UmiHealth.Domain.Entities
{
    /// <summary>
    /// Queue patient entity for managing patient queue
    /// </summary>
    public class QueuePatient : TenantEntity
    {
        public string QueueNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Complaint { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal"; // normal, urgent, emergency
        public string Status { get; set; } = "waiting"; // waiting, serving, completed, cancelled
        public DateTime JoinTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? AssignedProviderId { get; set; }
        public string? AssignedProviderName { get; set; }
        public string? Notes { get; set; }
        public int? WaitTimeMinutes { get; set; }
        public string? ServiceType { get; set; }
        public bool IsEmergency { get; set; } = false;
        public int Position { get; set; }
        public DateTime? EstimatedServiceTime { get; set; }
        public string? Token { get; set; }
    }

    /// <summary>
    /// Queue history for tracking queue operations
    /// </summary>
    public class QueueHistory : TenantEntity
    {
        public string QueuePatientId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // add, serve, complete, cancel, remove
        public string ActionBy { get; set; } = string.Empty;
        public string ActionByRole { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; }
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? Notes { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Queue settings for configuration
    /// </summary>
    public class QueueSettings : TenantEntity
    {
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Queue notifications for patient communication
    /// </summary>
    public class QueueNotification : TenantEntity
    {
        public string QueuePatientId { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty; // sms, whatsapp, email, in_app
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, sent, failed, delivered
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }
        public string? ExternalId { get; set; }
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Queue analytics for performance tracking
    /// </summary>
    public class QueueAnalytics : TenantEntity
    {
        public DateTime Date { get; set; }
        public int TotalPatients { get; set; }
        public int AverageWaitTime { get; set; }
        public int AverageServiceTime { get; set; }
        public int CompletedPatients { get; set; }
        public int CancelledPatients { get; set; }
        public int EmergencyPatients { get; set; }
        public int UrgentPatients { get; set; }
        public int NormalPatients { get; set; }
        public string PeakHour { get; set; } = string.Empty;
        public int PeakHourPatients { get; set; }
        public double ServiceEfficiency { get; set; }
        public double PatientSatisfaction { get; set; }
        public string? BranchId { get; set; }
        public string? ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Hourly queue data for detailed analytics
    /// </summary>
    public class HourlyQueueData : TenantEntity
    {
        public DateTime Hour { get; set; }
        public int NewPatients { get; set; }
        public int ServedPatients { get; set; }
        public int CompletedPatients { get; set; }
        public int CancelledPatients { get; set; }
        public int AverageWaitTime { get; set; }
        public int AverageServiceTime { get; set; }
        public int CurrentQueueLength { get; set; }
        public int AvailableProviders { get; set; }
        public double ServiceRate { get; set; }
        public string? BranchId { get; set; }
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Provider performance metrics
    /// </summary>
    public class ProviderPerformance : TenantEntity
    {
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int PatientsServed { get; set; }
        public int AverageServiceTime { get; set; }
        public int AverageWaitTime { get; set; }
        public double ServiceEfficiency { get; set; }
        public double PatientSatisfaction { get; set; }
        public int EmergencyPatients { get; set; }
        public int UrgentPatients { get; set; }
        public int NormalPatients { get; set; }
        public double Revenue { get; set; }
        public string? BranchId { get; set; }
        public string? Specialization { get; set; }
        public string? Metadata { get; set; }
    }
}
