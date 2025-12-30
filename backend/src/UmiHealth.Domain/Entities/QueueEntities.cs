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
        public string Name { get; set; } = string.Empty; // Added for compatibility
        public int Age { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Complaint { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal"; // normal, urgent, emergency
        public string Status { get; set; } = "waiting"; // waiting, serving, completed, cancelled
        public DateTime JoinTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? CompleteTime { get; set; } // Added for compatibility
        public string? AssignedProviderId { get; set; }
        public string? AssignedProviderName { get; set; }
        public User? AssignedProvider { get; set; } // Added for compatibility
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
        public DateTime Timestamp { get; set; } // Added for compatibility
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? Notes { get; set; }
        public string Details { get; set; } = string.Empty; // Added for compatibility
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Metadata { get; set; }
        public string PatientName { get; set; } = string.Empty; // Added for compatibility
        public string QueueNumber { get; set; } = string.Empty; // Added for compatibility
        public User? User { get; set; } // Added for compatibility
        public Guid? PatientId { get; set; } // Added for compatibility
    }

    /// <summary>
    /// Queue settings for configuration
    /// </summary>
    public class QueueSettings : TenantEntity
    {
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
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
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

    /// <summary>
    /// Refund request entity for managing refund requests
    /// </summary>
    public class RefundRequestEntity : TenantEntity
    {
        public Guid PaymentId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, approved, rejected, completed, approval_failed
        public string ReferenceNumber { get; set; } = string.Empty;
        public Guid RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
        public Guid? RefundId { get; set; }
        public DateTime? RefundProcessedAt { get; set; }
        public string? FailureReason { get; set; }
        public bool RequiresApproval { get; set; }
        public bool AutoApprove { get; set; }
        public decimal MaxRefundAmount { get; set; }
        public int RefundWindowDays { get; set; }

        public virtual Payment Payment { get; set; } = null!;
        public virtual Patient Customer { get; set; } = null!;
    }
}
