using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Application.DTOs.Queue;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public class QueueService : IQueueService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<QueueService> _logger;
        private readonly IQueueNotificationService _notificationService;

        public QueueService(
            SharedDbContext context,
            ILogger<QueueService> logger,
            IQueueNotificationService notificationService = null)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<QueueDataResponse> GetCurrentQueueAsync(Guid tenantId, Guid branchId)
        {
            try
            {
                var currentQueue = await _context.QueuePatients
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.Status != "completed")
                    .Include(q => q.AssignedProvider)
                    .OrderBy(q => q.Priority == "emergency" ? 0 : q.Priority == "urgent" ? 1 : 2)
                    .ThenBy(q => q.JoinTime)
                    .ToListAsync();

                var completedQueue = await _context.QueuePatients
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.Status == "completed")
                    .Include(q => q.AssignedProvider)
                    .OrderByDescending(q => q.CompleteTime)
                    .Take(50)
                    .ToListAsync();

                return new QueueDataResponse
                {
                    Current = currentQueue.Select(MapToQueuePatientDto).ToList(),
                    Completed = completedQueue.Select(MapToQueuePatientDto).ToList(),
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current queue for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task<QueueStatsResponse> GetQueueStatsAsync(Guid tenantId, Guid branchId)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var queuePatients = await _context.QueuePatients
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId)
                    .ToListAsync();

                var waitingCount = queuePatients.Count(q => q.Status == "waiting");
                var servingCount = queuePatients.Count(q => q.Status == "serving");
                var completedCount = queuePatients.Count(q => q.Status == "completed" && q.CompleteTime.HasValue && q.CompleteTime.Value.Date == today);
                var totalPatientsToday = queuePatients.Count(q => q.JoinTime.Date == today);

                // Calculate average wait time for completed patients today
                var completedToday = queuePatients.Where(q => 
                    q.Status == "completed" && 
                    q.CompleteTime.HasValue && 
                    q.CompleteTime.Value.Date == today &&
                    q.StartTime.HasValue).ToList();

                double averageWaitTime = 0;
                if (completedToday.Any())
                {
                    var totalWaitTime = completedToday.Sum(q => (q.StartTime!.Value - q.JoinTime).TotalMinutes);
                    averageWaitTime = totalWaitTime / completedToday.Count;
                }

                return new QueueStatsResponse
                {
                    WaitingCount = waitingCount,
                    ServingCount = servingCount,
                    CompletedCount = completedCount,
                    AverageWaitTime = Math.Round(averageWaitTime, 1),
                    TotalPatientsToday = totalPatientsToday,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue stats for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task<QueuePatientResponse> AddPatientToQueueAsync(Guid tenantId, Guid branchId, AddPatientRequest request, string userId)
        {
            try
            {
                // Check queue size limit
                var settings = await GetQueueSettingsAsync(tenantId, branchId);
                var currentQueueSize = await _context.QueuePatients
                    .CountAsync(q => q.TenantId == tenantId && q.BranchId == branchId && q.Status != "completed");

                if (settings != null && currentQueueSize >= settings.MaxQueueSize)
                {
                    return new QueuePatientResponse
                    {
                        Success = false,
                        Message = "Queue is at maximum capacity"
                    };
                }

                // Generate queue number
                var queueNumber = await GenerateQueueNumberAsync(tenantId, branchId);

                var queuePatient = new QueuePatient
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    BranchId = branchId,
                    QueueNumber = queueNumber,
                    Name = request.Name,
                    Age = request.Age,
                    Complaint = request.Complaint,
                    Priority = request.Priority,
                    Status = "waiting",
                    JoinTime = DateTime.UtcNow,
                    Notes = request.Notes,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    AssignedProviderId = request.AssignedProviderId,
                    Position = currentQueueSize + 1,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.QueuePatients.Add(queuePatient);
                await _context.SaveChangesAsync();

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "add", queuePatient.Id, queuePatient.Name, queueNumber, userId, $"Patient added to queue with priority: {request.Priority}");

                // Send notification if enabled
                if (settings?.EnableAutoNotifications == true)
                {
                    await SendQueueNotificationAsync(queuePatient, "added");
                }

                return new QueuePatientResponse
                {
                    Success = true,
                    Message = "Patient added to queue successfully",
                    Patient = MapToQueuePatientDto(queuePatient)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding patient to queue for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task ServePatientAsync(Guid tenantId, Guid branchId, Guid patientId, string userId)
        {
            try
            {
                var patient = await _context.QueuePatients
                    .FirstOrDefaultAsync(q => q.Id == patientId && q.TenantId == tenantId && q.BranchId == branchId);

                if (patient == null)
                {
                    throw new Exception("Patient not found in queue");
                }

                patient.Status = "serving";
                patient.StartTime = DateTime.UtcNow;
                patient.UpdatedBy = userId;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "serve", patientId, patient.Name, patient.QueueNumber, userId, "Patient started service");

                // Send notification
                await SendQueueNotificationAsync(patient, "serving");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving patient {PatientId} for tenant {TenantId}, branch {BranchId}", patientId, tenantId, branchId);
                throw;
            }
        }

        public async Task RemovePatientFromQueueAsync(Guid tenantId, Guid branchId, Guid patientId, string userId)
        {
            try
            {
                var patient = await _context.QueuePatients
                    .FirstOrDefaultAsync(q => q.Id == patientId && q.TenantId == tenantId && q.BranchId == branchId);

                if (patient == null)
                {
                    throw new Exception("Patient not found in queue");
                }

                _context.QueuePatients.Remove(patient);
                await _context.SaveChangesAsync();

                // Reorder remaining patients
                await ReorderQueueAsync(tenantId, branchId);

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "remove", patientId, patient.Name, patient.QueueNumber, userId, "Patient removed from queue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing patient {PatientId} from queue for tenant {TenantId}, branch {BranchId}", patientId, tenantId, branchId);
                throw;
            }
        }

        public async Task ClearQueueAsync(Guid tenantId, Guid branchId, string userId)
        {
            try
            {
                var currentPatients = await _context.QueuePatients
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.Status != "completed")
                    .ToListAsync();

                _context.QueuePatients.RemoveRange(currentPatients);
                await _context.SaveChangesAsync();

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "clear", Guid.Empty, "System", "CLEAR", userId, $"Queue cleared - {currentPatients.Count} patients removed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing queue for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task<QueuePatientResponse> CallNextPatientAsync(Guid tenantId, Guid branchId, string userId)
        {
            try
            {
                var nextPatient = await _context.QueuePatients
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.Status == "waiting")
                    .OrderBy(q => q.Priority == "emergency" ? 0 : q.Priority == "urgent" ? 1 : 2)
                    .ThenBy(q => q.JoinTime)
                    .FirstOrDefaultAsync();

                if (nextPatient == null)
                {
                    return new QueuePatientResponse
                    {
                        Success = false,
                        Message = "No patients waiting in queue"
                    };
                }

                await ServePatientAsync(tenantId, branchId, nextPatient.Id, userId);

                return new QueuePatientResponse
                {
                    Success = true,
                    Message = "Next patient called successfully",
                    Patient = MapToQueuePatientDto(nextPatient)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling next patient for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task CompletePatientServiceAsync(Guid tenantId, Guid branchId, Guid patientId, string userId)
        {
            try
            {
                var patient = await _context.QueuePatients
                    .FirstOrDefaultAsync(q => q.Id == patientId && q.TenantId == tenantId && q.BranchId == branchId);

                if (patient == null)
                {
                    throw new Exception("Patient not found in queue");
                }

                patient.Status = "completed";
                patient.CompleteTime = DateTime.UtcNow;
                patient.UpdatedBy = userId;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reorder remaining patients
                await ReorderQueueAsync(tenantId, branchId);

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "complete", patientId, patient.Name, patient.QueueNumber, userId, "Patient service completed");

                // Send notification
                await SendQueueNotificationAsync(patient, "completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing patient service {PatientId} for tenant {TenantId}, branch {BranchId}", patientId, tenantId, branchId);
                throw;
            }
        }

        public async Task UpdatePatientPositionAsync(Guid tenantId, Guid branchId, Guid patientId, int newPosition, string userId)
        {
            try
            {
                var patient = await _context.QueuePatients
                    .FirstOrDefaultAsync(q => q.Id == patientId && q.TenantId == tenantId && q.BranchId == branchId);

                if (patient == null)
                {
                    throw new Exception("Patient not found in queue");
                }

                var currentPatients = await _context.QueuePatients
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.Status == "waiting")
                    .OrderBy(q => q.Position)
                    .ToListAsync();

                // Update positions
                if (newPosition > 0 && newPosition <= currentPatients.Count)
                {
                    currentPatients.Remove(patient);
                    currentPatients.Insert(newPosition - 1, patient);

                    for (int i = 0; i < currentPatients.Count; i++)
                    {
                        currentPatients[i].Position = i + 1;
                        currentPatients[i].UpdatedBy = userId;
                        currentPatients[i].UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    // Log to history
                    await LogQueueActionAsync(tenantId, branchId, "update_position", patientId, patient.Name, patient.QueueNumber, userId, $"Position updated to {newPosition}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient position {PatientId} for tenant {TenantId}, branch {BranchId}", patientId, tenantId, branchId);
                throw;
            }
        }

        public async Task<QueueHistoryResponse> GetQueueHistoryAsync(Guid tenantId, Guid branchId, QueueHistoryFilters filters)
        {
            try
            {
                var query = _context.QueueHistory
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId);

                if (filters.DateFrom.HasValue)
                {
                    query = query.Where(q => q.Timestamp >= filters.DateFrom.Value);
                }

                if (filters.DateTo.HasValue)
                {
                    query = query.Where(q => q.Timestamp <= filters.DateTo.Value);
                }

                if (!string.IsNullOrEmpty(filters.Action))
                {
                    query = query.Where(q => q.Action == filters.Action);
                }

                var totalCount = await query.CountAsync();
                var history = await query
                    .OrderByDescending(q => q.Timestamp)
                    .Skip((filters.Page - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .ToListAsync();

                return new QueueHistoryResponse
                {
                    History = history.Select(MapToQueueHistoryItemDto).ToList(),
                    TotalCount = totalCount,
                    Page = filters.Page,
                    PageSize = filters.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue history for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task<EmergencyQueueResponse> GetEmergencyQueueAsync(Guid tenantId, Guid branchId)
        {
            try
            {
                var emergencyPatients = await _context.QueuePatients
                    .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.Priority == "emergency" && q.Status != "completed")
                    .Include(q => q.AssignedProvider)
                    .OrderBy(q => q.JoinTime)
                    .ToListAsync();

                return new EmergencyQueueResponse
                {
                    EmergencyPatients = emergencyPatients.Select(MapToQueuePatientDto).ToList(),
                    Count = emergencyPatients.Count,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting emergency queue for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task<ProvidersResponse> GetProvidersAsync(Guid tenantId, Guid branchId)
        {
            try
            {
                var providers = await _context.Users
                    .Where(u => u.TenantId == tenantId && 
                               (u.BranchId == branchId || u.BranchId == null) &&
                               u.Role.ToLower().Contains("doctor") || u.Role.ToLower().Contains("provider"))
                    .Select(u => new ProviderDto
                    {
                        Id = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Specialization = u.Specialization ?? "General",
                        IsAvailable = u.IsActive,
                        CurrentPatientCount = _context.QueuePatients.Count(q => q.AssignedProviderId == u.Id && q.Status == "serving"),
                        Department = u.Department
                    })
                    .ToListAsync();

                return new ProvidersResponse
                {
                    Providers = providers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting providers for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task AssignProviderAsync(Guid tenantId, Guid branchId, Guid patientId, Guid providerId, string userId)
        {
            try
            {
                var patient = await _context.QueuePatients
                    .FirstOrDefaultAsync(q => q.Id == patientId && q.TenantId == tenantId && q.BranchId == branchId);

                if (patient == null)
                {
                    throw new Exception("Patient not found in queue");
                }

                patient.AssignedProviderId = providerId;
                patient.UpdatedBy = userId;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "assign_provider", patientId, patient.Name, patient.QueueNumber, userId, $"Provider assigned: {providerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning provider {ProviderId} to patient {PatientId} for tenant {TenantId}, branch {BranchId}", providerId, patientId, tenantId, branchId);
                throw;
            }
        }

        public async Task UpdatePatientPriorityAsync(Guid tenantId, Guid branchId, Guid patientId, string priority, string userId)
        {
            try
            {
                var patient = await _context.QueuePatients
                    .FirstOrDefaultAsync(q => q.Id == patientId && q.TenantId == tenantId && q.BranchId == branchId);

                if (patient == null)
                {
                    throw new Exception("Patient not found in queue");
                }

                var oldPriority = patient.Priority;
                patient.Priority = priority;
                patient.UpdatedBy = userId;
                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reorder queue if priority changed
                if (oldPriority != priority)
                {
                    await ReorderQueueAsync(tenantId, branchId);
                }

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "update_priority", patientId, patient.Name, patient.QueueNumber, userId, $"Priority changed from {oldPriority} to {priority}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient priority {PatientId} for tenant {TenantId}, branch {BranchId}", patientId, tenantId, branchId);
                throw;
            }
        }

        public async Task BulkServePatientsAsync(Guid tenantId, Guid branchId, List<Guid> patientIds, string userId)
        {
            try
            {
                foreach (var patientId in patientIds)
                {
                    await ServePatientAsync(tenantId, branchId, patientId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk serving patients for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task BulkRemovePatientsAsync(Guid tenantId, Guid branchId, List<Guid> patientIds, string userId)
        {
            try
            {
                var patients = await _context.QueuePatients
                    .Where(q => patientIds.Contains(q.Id) && q.TenantId == tenantId && q.BranchId == branchId)
                    .ToListAsync();

                _context.QueuePatients.RemoveRange(patients);
                await _context.SaveChangesAsync();

                await ReorderQueueAsync(tenantId, branchId);

                // Log to history
                await LogQueueActionAsync(tenantId, branchId, "bulk_remove", Guid.Empty, "System", "BULK", userId, $"Bulk removed {patients.Count} patients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk removing patients for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task<byte[]> ExportQueueAsync(Guid tenantId, Guid branchId, QueueExportFilters filters)
        {
            try
            {
                // Implementation would depend on the export format
                // For now, return a simple CSV export
                var queueData = await GetCurrentQueueAsync(tenantId, branchId);
                
                var csv = "Queue Number,Name,Age,Complaint,Priority,Status,Join Time,Provider\n";
                foreach (var patient in queueData.Current)
                {
                    csv += $"{patient.QueueNumber},{patient.Name},{patient.Age},{patient.Complaint},{patient.Priority},{patient.Status},{patient.JoinTime:yyyy-MM-dd HH:mm:ss},{patient.AssignedProviderName}\n";
                }

                return System.Text.Encoding.UTF8.GetBytes(csv);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting queue for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task PrintDailyReportAsync(Guid tenantId, Guid branchId, DateTime date)
        {
            try
            {
                // Implementation would depend on printing infrastructure
                // For now, just log the request
                _logger.LogInformation("Daily report print requested for tenant {TenantId}, branch {BranchId}, date {Date}", tenantId, branchId, date);
                
                // In a real implementation, this would:
                // 1. Generate the report
                // 2. Send to printer service
                // 3. Log the result
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing daily report for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                throw;
            }
        }

        public async Task PrintQueueSlipAsync(Guid tenantId, Guid branchId, Guid patientId)
        {
            try
            {
                var patient = await _context.QueuePatients
                    .FirstOrDefaultAsync(q => q.Id == patientId && q.TenantId == tenantId && q.BranchId == branchId);

                if (patient == null)
                {
                    throw new Exception("Patient not found in queue");
                }

                // Implementation would depend on printing infrastructure
                _logger.LogInformation("Queue slip print requested for patient {PatientId}", patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing queue slip for patient {PatientId}", patientId);
                throw;
            }
        }

        // Helper methods
        private async Task<string> GenerateQueueNumberAsync(Guid tenantId, Guid branchId)
        {
            var today = DateTime.Today;
            var prefix = $"Q{today:yyyyMMdd}";
            
            var lastQueueNumber = await _context.QueuePatients
                .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.QueueNumber.StartsWith(prefix))
                .OrderByDescending(q => q.QueueNumber)
                .Select(q => q.QueueNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (!string.IsNullOrEmpty(lastQueueNumber))
            {
                var lastSequence = int.Parse(lastQueueNumber.Substring(prefix.Length));
                sequence = lastSequence + 1;
            }

            return $"{prefix}{sequence:D3}";
        }

        private async Task ReorderQueueAsync(Guid tenantId, Guid branchId)
        {
            var waitingPatients = await _context.QueuePatients
                .Where(q => q.TenantId == tenantId && q.BranchId == branchId && q.Status == "waiting")
                .OrderBy(q => q.Priority == "emergency" ? 0 : q.Priority == "urgent" ? 1 : 2)
                .ThenBy(q => q.JoinTime)
                .ToListAsync();

            for (int i = 0; i < waitingPatients.Count; i++)
            {
                waitingPatients[i].Position = i + 1;
            }

            await _context.SaveChangesAsync();
        }

        private async Task LogQueueActionAsync(Guid tenantId, Guid branchId, string action, Guid patientId, string patientName, string queueNumber, string userId, string details)
        {
            var history = new QueueHistory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BranchId = branchId,
                Timestamp = DateTime.UtcNow,
                Action = action,
                PatientId = patientId,
                PatientName = patientName,
                QueueNumber = queueNumber,
                UserId = userId,
                Details = details
            };

            _context.QueueHistory.Add(history);
            await _context.SaveChangesAsync();
        }

        private async Task<QueueSettingsDto?> GetQueueSettingsAsync(Guid tenantId, Guid branchId)
        {
            return await _context.QueueSettings
                .Where(q => q.TenantId == tenantId && q.BranchId == branchId)
                .Select(q => new QueueSettingsDto
                {
                    Id = q.Id,
                    TenantId = q.TenantId,
                    BranchId = q.BranchId,
                    TargetWaitTime = q.TargetWaitTime,
                    AutoEscalateWaitTime = q.AutoEscalateWaitTime,
                    MaxQueueSize = q.MaxQueueSize,
                    EnableAutoNotifications = q.EnableAutoNotifications,
                    EnableSoundNotifications = q.EnableSoundNotifications,
                    EnableSmsNotifications = q.EnableSmsNotifications,
                    EnableWhatsAppNotifications = q.EnableWhatsAppNotifications,
                    EnableEmailNotifications = q.EnableEmailNotifications,
                    DefaultNotificationMessage = q.DefaultNotificationMessage,
                    CreatedAt = q.CreatedAt,
                    UpdatedAt = q.UpdatedAt,
                    CreatedBy = q.CreatedBy,
                    UpdatedBy = q.UpdatedBy
                })
                .FirstOrDefaultAsync();
        }

        private async Task SendQueueNotificationAsync(QueuePatient patient, string action)
        {
            if (_notificationService == null) return;

            try
            {
                var request = new QueueNotificationRequest
                {
                    PatientId = patient.Id,
                    NotificationType = "sms", // Default to SMS
                    Message = GetNotificationMessage(action, patient)
                };

                await _notificationService.SendQueueNotificationAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue notification for patient {PatientId}", patient.Id);
            }
        }

        private string GetNotificationMessage(string action, QueuePatient patient)
        {
            return action switch
            {
                "added" => $"You have been added to the queue. Your queue number is {patient.QueueNumber}",
                "serving" => $"You are now being served. Queue number: {patient.QueueNumber}",
                "completed" => $"Your service has been completed. Thank you for visiting!",
                _ => $"Queue update for queue number: {patient.QueueNumber}"
            };
        }

        private QueuePatientDto MapToQueuePatientDto(QueuePatient patient)
        {
            return new QueuePatientDto
            {
                Id = patient.Id,
                QueueNumber = patient.QueueNumber,
                Name = patient.Name,
                Age = patient.Age,
                Complaint = patient.Complaint,
                JoinTime = patient.JoinTime,
                Status = patient.Status,
                Priority = patient.Priority,
                AssignedProviderId = patient.AssignedProviderId,
                AssignedProviderName = patient.AssignedProvider?.FirstName + " " + patient.AssignedProvider?.LastName,
                Position = patient.Position,
                StartTime = patient.StartTime,
                CompleteTime = patient.CompleteTime,
                Notes = patient.Notes,
                PhoneNumber = patient.PhoneNumber,
                Email = patient.Email
            };
        }

        private QueueHistoryItemDto MapToQueueHistoryItemDto(QueueHistory history)
        {
            return new QueueHistoryItemDto
            {
                Id = history.Id,
                Timestamp = history.Timestamp,
                Action = history.Action,
                PatientName = history.PatientName,
                QueueNumber = history.QueueNumber,
                UserName = history.User?.FirstName + " " + history.User?.LastName,
                Details = history.Details,
                PatientId = history.PatientId
            };
        }
    }
}
