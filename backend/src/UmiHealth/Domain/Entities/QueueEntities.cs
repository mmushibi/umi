using System;
using System.ComponentModel.DataAnnotations;

namespace UmiHealth.Domain.Entities
{
    public class QueuePatient
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public Guid BranchId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string QueueNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Range(0, 150)]
        public int Age { get; set; }
        
        [StringLength(500)]
        public string Complaint { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string Status { get; set; } = "waiting"; // waiting, serving, completed
        
        [StringLength(20)]
        public string Priority { get; set; } = "normal"; // normal, urgent, emergency
        
        public Guid? AssignedProviderId { get; set; }
        
        public User? AssignedProvider { get; set; }
        
        public int? Position { get; set; }
        
        public DateTime JoinTime { get; set; }
        
        public DateTime? StartTime { get; set; }
        
        public DateTime? CompleteTime { get; set; }
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(200)]
        public string? Email { get; set; }
        
        [StringLength(50)]
        public string? CreatedBy { get; set; }
        
        [StringLength(50)]
        public string? UpdatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
    }

    public class QueueHistory
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public Guid BranchId { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // add, serve, remove, complete, update_priority, etc.
        
        public Guid PatientId { get; set; }
        
        [StringLength(200)]
        public string PatientName { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string QueueNumber { get; set; } = string.Empty;
        
        [Required]
        public Guid UserId { get; set; }
        
        public User? User { get; set; }
        
        [StringLength(1000)]
        public string Details { get; set; } = string.Empty;
    }

    public class QueueSettings
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public Guid BranchId { get; set; }
        
        public double TargetWaitTime { get; set; } = 15; // in minutes
        
        public double AutoEscalateWaitTime { get; set; } = 30; // in minutes
        
        public int MaxQueueSize { get; set; } = 50;
        
        public bool EnableAutoNotifications { get; set; } = false;
        
        public bool EnableSoundNotifications { get; set; } = true;
        
        public bool EnableSmsNotifications { get; set; } = false;
        
        public bool EnableWhatsAppNotifications { get; set; } = false;
        
        public bool EnableEmailNotifications { get; set; } = false;
        
        [StringLength(500)]
        public string? DefaultNotificationMessage { get; set; }
        
        [StringLength(50)]
        public string? CreatedBy { get; set; }
        
        [StringLength(50)]
        public string? UpdatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }

    public class QueueNotification
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public Guid BranchId { get; set; }
        
        [Required]
        public Guid PatientId { get; set; }
        
        public QueuePatient? Patient { get; set; }
        
        [Required]
        [StringLength(20)]
        public string NotificationType { get; set; } = string.Empty; // sms, whatsapp, email
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        public string? RecipientAddress { get; set; }
        
        public string Status { get; set; } = "pending"; // pending, sent, failed
        
        public DateTime SentAt { get; set; }
        
        public DateTime? DeliveredAt { get; set; }
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        
        public int RetryCount { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }

    public class QueueAnalytics
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public Guid BranchId { get; set; }
        
        public DateTime Date { get; set; }
        
        public int TotalPatients { get; set; }
        
        public int PatientsServed { get; set; }
        
        public int PatientsWaiting { get; set; }
        
        public double AverageWaitTime { get; set; }
        
        public double AverageServiceTime { get; set; }
        
        public double TargetWaitTime { get; set; }
        
        public double PerformancePercentage { get; set; }
        
        public int PeakHour { get; set; }
        
        public int PeakHourPatientCount { get; set; }
        
        public List<HourlyQueueData> HourlyData { get; set; } = new();
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }

    public class HourlyQueueData
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid QueueAnalyticsId { get; set; }
        
        public QueueAnalytics? QueueAnalytics { get; set; }
        
        public int Hour { get; set; }
        
        public int PatientCount { get; set; }
        
        public double AverageWaitTime { get; set; }
        
        public int PatientsServed { get; set; }
        
        public int PatientsAdded { get; set; }
        
        public int PatientsRemoved { get; set; }
    }

    public class ProviderPerformance
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        public Guid BranchId { get; set; }
        
        [Required]
        public Guid ProviderId { get; set; }
        
        public User? Provider { get; set; }
        
        public DateTime Date { get; set; }
        
        public int PatientsServed { get; set; }
        
        public double AverageServiceTime { get; set; }
        
        public double SatisfactionScore { get; set; }
        
        public bool IsPerformingWell { get; set; }
        
        public int TotalPatientsAssigned { get; set; }
        
        public int PatientsCompleted { get; set; }
        
        public int PatientsNoShow { get; set; }
        
        public double RevenueGenerated { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}
