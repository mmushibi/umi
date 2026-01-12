using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UmiHealth.API.Hubs;
using UmiHealth.Core.Interfaces;
using UmiHealth.Application.Services;
using CoreIPharmacyService = UmiHealth.Core.Interfaces.IPharmacyService;
using AppIPharmacyService = UmiHealth.Application.Services.IPharmacyService;
using System.Linq;

namespace UmiHealth.API.Controllers
{
    public class StatusUpdateRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PharmacistController : ControllerBase
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly CoreIPharmacyService _pharmacyService;
        private readonly IDataSyncService _dataSyncService;
        private readonly IHubContext<PharmacyHub> _hubContext;
        private readonly ILogger<PharmacistController> _logger;
        private readonly ISubscriptionFeatureService _subscriptionFeatureService;

        public PharmacistController(
            IPrescriptionService prescriptionService,
            CoreIPharmacyService pharmacyService,
            IDataSyncService dataSyncService,
            IHubContext<PharmacyHub> hubContext,
            ILogger<PharmacistController> logger,
            ISubscriptionFeatureService subscriptionFeatureService)
        {
            _prescriptionService = prescriptionService;
            _pharmacyService = pharmacyService;
            _dataSyncService = dataSyncService;
            _hubContext = hubContext;
            _logger = logger;
            _subscriptionFeatureService = subscriptionFeatureService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardData([FromQuery] Guid tenantId, [FromQuery] Guid branchId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // Check subscription feature access
                var featureCheck = await _subscriptionFeatureService.EnforceFeatureAccessAsync(HttpContext, "basic_analytics");
                if (featureCheck != null)
                {
                    return featureCheck;
                }

                var todayPrescriptionsTask = GetPrescriptionStatsAsync(tenantId, branchId);
                var pendingOrdersTask = GetPendingOrdersAsync(tenantId, branchId);
                var inventoryStatsTask = GetInventoryStatsAsync(tenantId, branchId);
                var patientsServedTask = GetPatientsServedAsync(tenantId, branchId);
                var totalPrescriptionsTask = GetTotalPrescriptionsAsync(tenantId, branchId);
                var filledTodayTask = GetFilledTodayAsync(tenantId, branchId);
                var expiringSoonTask = GetExpiringSoonAsync(tenantId, branchId);
                var revenueStatsTask = GetRevenueStatsAsync(tenantId, branchId);
                var recentActivityTask = GetRecentActivityAsync(tenantId, branchId);
                var alertsTask = GetAlertsAsync(tenantId, branchId);

                await Task.WhenAll(
                    todayPrescriptionsTask,
                    pendingOrdersTask,
                    inventoryStatsTask,
                    patientsServedTask,
                    totalPrescriptionsTask,
                    filledTodayTask,
                    expiringSoonTask,
                    revenueStatsTask,
                    recentActivityTask,
                    alertsTask
                );

                var dashboard = new
                {
                    Stats = new
                    {
                        TodayPrescriptions = await todayPrescriptionsTask,
                        PendingOrders = await pendingOrdersTask,
                        LowStockItems = await inventoryStatsTask,
                        PatientsServed = await patientsServedTask,
                        TotalPrescriptions = await totalPrescriptionsTask,
                        FilledToday = await filledTodayTask,
                        ExpiringSoon = await expiringSoonTask,
                        RevenueToday = await revenueStatsTask
                    },
                    RecentActivity = await recentActivityTask,
                    Alerts = await alertsTask,
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                return StatusCode(500, new { error = "Failed to fetch dashboard data" });
            }
        }

        [HttpGet("user-info")]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                var tenantId = User.FindFirst("TenantId")?.Value;
                
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId))
                {
                    return Unauthorized();
                }

                // This would typically fetch from user service/database
                var userInfo = new
                {
                    Id = Guid.Parse(userId),
                    TenantId = Guid.Parse(tenantId),
                    Name = User.FindFirst("name")?.Value ?? "Unknown User",
                    Email = User.FindFirst("email")?.Value ?? "unknown@example.com",
                    Role = User.FindFirst("role")?.Value ?? "Pharmacist",
                    FirstName = User.FindFirst("given_name")?.Value ?? "Unknown",
                    LastName = User.FindFirst("family_name")?.Value ?? "User"
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user info");
                return StatusCode(500, new { error = "Failed to fetch user information" });
            }
        }

        [HttpGet("pharmacy-info")]
        public async Task<IActionResult> GetPharmacyInfo([FromQuery] Guid tenantId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var settings = await _pharmacyService.GetSettingsAsync(tenantId);
                if (settings == null)
                {
                    return NotFound(new { error = "Pharmacy settings not found" });
                }

                var pharmacyInfo = new
                {
                    Name = settings.PharmacyName,
                    License = settings.PharmacyLicense,
                    Address = settings.Address,
                    Phone = settings.Phone,
                    Email = settings.Email,
                    Currency = settings.Currency,
                    TimeZone = settings.TimeZone
                };

                return Ok(pharmacyInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pharmacy info for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch pharmacy information" });
            }
        }

        [HttpPost("sync/trigger")]
        public async Task<IActionResult> TriggerSync([FromQuery] Guid tenantId, [FromQuery] Guid branchId, [FromQuery] string? entityType = null)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                if (string.IsNullOrEmpty(entityType))
                {
                    await _dataSyncService.SyncAllAsync(tenantId, branchId);
                }
                else
                {
                    await _dataSyncService.TriggerSyncAsync(entityType);
                }

                // Notify connected clients about the sync
                await _hubContext.Clients.Group($"tenant_{tenantId}_branch_{branchId}")
                    .SendAsync("SyncTriggered", new { entityType, timestamp = DateTime.UtcNow });

                return Ok(new { message = "Sync triggered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering sync for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                return StatusCode(500, new { error = "Failed to trigger sync" });
            }
        }

        [HttpGet("sync/status")]
        public async Task<IActionResult> GetSyncStatus([FromQuery] Guid tenantId, [FromQuery] Guid branchId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var status = await _dataSyncService.GetSyncStatusAsync(tenantId, branchId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sync status for tenant {TenantId}, branch {BranchId}", tenantId, branchId);
                return StatusCode(500, new { error = "Failed to fetch sync status" });
            }
        }

        private async Task<int> GetPrescriptionStatsAsync(Guid tenantId, Guid branchId)
        {
            var today = DateTime.Today;
            var endOfDay = today.AddDays(1);
            
            var prescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId, new PrescriptionFilter(
                null, // PatientId
                null, // DoctorId
                today, // StartDate
                endOfDay, // EndDate
                null, // Status
                null, // PrescriptionNumber
                null // PatientName
            ));
            
            // Filter by branch if prescriptions have branch information
            return prescriptions.Count();
        }

        private async Task<int> GetPendingOrdersAsync(Guid tenantId, Guid branchId)
        {
            // Get pending prescriptions that need to be filled
            var pendingPrescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId, new PrescriptionFilter(
                null, // PatientId
                null, // DoctorId
                null, // StartDate
                null, // EndDate
                "pending", // Status
                null, // PrescriptionNumber
                null // PatientName
            ));
            
            return pendingPrescriptions.Count();
        }

        private async Task<int> GetInventoryStatsAsync(Guid tenantId, Guid branchId)
        {
            var inventory = await _pharmacyService.GetInventoryAsync(tenantId, branchId);
            return inventory.Count(i => i.QuantityOnHand <= i.ReorderLevel);
        }

        private async Task<int> GetPatientsServedAsync(Guid tenantId, Guid branchId)
        {
            // Get patients served in the last 7 days
            var weekAgo = DateTime.Today.AddDays(-7);
            var prescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId, new PrescriptionFilter(
                null, // PatientId
                null, // DoctorId
                weekAgo, // StartDate
                null, // EndDate
                null, // Status
                null, // PrescriptionNumber
                null // PatientName
            ));
            
            // Count unique patients
            return prescriptions.Select(p => p.PatientId).Distinct().Count();
        }

        private async Task<int> GetTotalPrescriptionsAsync(Guid tenantId, Guid branchId)
        {
            var prescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId);
            return prescriptions.Count();
        }

        private async Task<int> GetFilledTodayAsync(Guid tenantId, Guid branchId)
        {
            var today = DateTime.Today;
            var endOfDay = today.AddDays(1);
            
            var prescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId, new PrescriptionFilter(
                null, // PatientId
                null, // DoctorId
                today, // StartDate
                endOfDay, // EndDate
                "filled", // Status
                null, // PrescriptionNumber
                null // PatientName
            ));
            
            return prescriptions.Count();
        }

        private async Task<int> GetExpiringSoonAsync(Guid tenantId, Guid branchId)
        {
            var inventory = await _pharmacyService.GetInventoryAsync(tenantId, branchId);
            var thirtyDaysFromNow = DateTime.Today.AddDays(30);
            
            return inventory.Count(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value <= thirtyDaysFromNow);
        }

        private async Task<decimal> GetRevenueStatsAsync(Guid tenantId, Guid branchId)
        {
            // This would typically come from a sales/payment service
            // For now, return a placeholder in Zambian Kwacha
            await Task.CompletedTask;
            return 3247.50m; // ZMW 3,247.50
        }

        private async Task<object> GetRecentActivityAsync(Guid tenantId, Guid branchId)
        {
            try
            {
                var activities = new List<object>();
                
                // Get recent prescriptions
                var recentPrescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId, new PrescriptionFilter(
                    null, // PatientId
                    null, // DoctorId
                    DateTime.Today.AddDays(-1), // StartDate
                    null, // EndDate
                    null, // Status
                    null, // PrescriptionNumber
                    null // PatientName
                ));

                foreach (var prescription in recentPrescriptions.Take(5))
                {
                    activities.Add(new
                    {
                        id = Guid.NewGuid(),
                        type = prescription.Status == "filled" ? "success" : "info",
                        icon = prescription.Status == "filled" ? "check_circle" : "prescriptions",
                        title = prescription.Status == "filled" ? "Prescription Filled" : "New Prescription",
                        description = $"Prescription #{prescription.Id.ToString().Substring(0, 8)} - Status: {prescription.Status}",
                        time = FormatRelativeTime(prescription.CreatedAt)
                    });
                }

                // Get low stock items
                var inventory = await _pharmacyService.GetInventoryAsync(tenantId, branchId);
                var lowStockItems = inventory.Where(i => i.QuantityOnHand <= i.ReorderLevel).Take(3);

                foreach (var item in lowStockItems)
                {
                    activities.Add(new
                    {
                        id = Guid.NewGuid(),
                        type = "warning",
                        icon = "warning",
                        title = "Low Stock Alert",
                        description = $"{item.Product?.Name} - Only {item.QuantityOnHand} units remaining",
                        time = FormatRelativeTime(item.UpdatedAt)
                    });
                }

                return activities.OrderByDescending(a => a.GetType().GetProperty("time")?.GetValue(a)).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activity");
                return new object[] { };
            }
        }

        private async Task<object> GetAlertsAsync(Guid tenantId, Guid branchId)
        {
            try
            {
                var alerts = new List<object>();

                // Check for expiring medications
                var inventory = await _pharmacyService.GetInventoryAsync(tenantId, branchId);
                var thirtyDaysFromNow = DateTime.Today.AddDays(30);
                var expiringItems = inventory.Where(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value <= thirtyDaysFromNow).ToList();

                if (expiringItems.Any())
                {
                    alerts.Add(new
                    {
                        id = Guid.NewGuid(),
                        type = "warning",
                        icon = "warning",
                        title = "Expiring Medications",
                        description = $"{expiringItems.Count} medication(s) expiring in the next 30 days"
                    });
                }

                // Check for critical stock levels
                var criticalStock = inventory.Where(i => i.QuantityOnHand <= i.ReorderLevel / 2).ToList();
                if (criticalStock.Any())
                {
                    alerts.Add(new
                    {
                        id = Guid.NewGuid(),
                        type = "warning",
                        icon = "inventory_2",
                        title = "Critical Stock Levels",
                        description = $"{criticalStock.Count} medication(s) below critical threshold"
                    });
                }

                // Check for pending prescriptions
                var pendingPrescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId, new PrescriptionFilter(
                    null, // PatientId
                    null, // DoctorId
                    null, // StartDate
                    null, // EndDate
                    "pending", // Status
                    null, // PrescriptionNumber
                    null // PatientName
                ));

                if (pendingPrescriptions.Count() > 10)
                {
                    alerts.Add(new
                    {
                        id = Guid.NewGuid(),
                        type = "info",
                        icon = "info",
                        title = "High Prescription Volume",
                        description = $"{pendingPrescriptions.Count()} prescriptions pending fulfillment"
                    });
                }

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alerts");
                return new object[] { };
            }
        }

        [HttpGet("prescriptions")]
        public async Task<IActionResult> GetPrescriptions([FromQuery] Guid tenantId, [FromQuery] Guid? branchId = null)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                var prescriptions = await _prescriptionService.GetPrescriptionsAsync(tenantId);
                
                // Map to response format
                var response = prescriptions.Select(p => new
                {
                    id = p.Id.ToString(),
                    patientName = $"{p.Patient?.FirstName} {p.Patient?.LastName}",
                    patientDOB = p.Patient?.DateOfBirth.ToString("yyyy-MM-dd"),
                    patientPhone = p.Patient?.Phone,
                    allergies = p.Patient?.Allergies,
                    date = p.CreatedAt.ToString("yyyy-MM-dd"),
                    status = p.Status?.ToLower() ?? "unknown",
                    priority = "medium",
                    insuranceStatus = "pending",
                    inventoryStatus = "in-stock", // Would come from inventory service
                    medSync = false,
                    riskFactors = new string[0], // Would come from clinical service
                    workflow = new
                    {
                        submitted = true,
                        verified = p.Status == "verified",
                        filled = p.Status == "filled",
                        dispensed = p.Status == "dispensed"
                    },
                    cdsAlerts = new object[0], // Would come from clinical decision support
                    insuranceProvider = p.Patient?.InsuranceProvider,
                    memberId = p.Patient?.InsuranceNumber,
                    copay = "0.00", // Would come from insurance service
                    authRequired = false, // Would come from insurance service
                    medications = p.Items?.Select(m => new
                    {
                        name = m.Product?.Name ?? "Unknown",
                        dosage = m.Dosage,
                        quantity = m.Quantity.ToString(),
                        daysSupply = $"{m.Duration} {m.DurationUnit}",
                        instructions = m.Instructions,
                        refills = "0", // Would come from prescription service
                        ndc = m.Product?.NdcCode ?? "",
                        inventoryStatus = "in-stock", // Would come from inventory service
                        stockCount = 100 // Would come from inventory service
                    }).Cast<object>().ToList() ?? new List<object>(),
                    prescriber = $"{p.Doctor?.FirstName} {p.Doctor?.LastName}",
                    prescriberLicense = "", // Would come from doctor profile
                    prescriberDEA = "", // Would come from doctor profile
                    prescriberPhone = p.Doctor?.Phone
                });

                return Ok(new { prescriptions = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prescriptions for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch prescriptions" });
            }
        }

        [HttpGet("prescribers")]
        public async Task<IActionResult> GetPrescribers([FromQuery] Guid tenantId)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // This would typically fetch from a prescriber service/database
                // For now, return sample data that matches the expected format
                var prescribers = new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                        fullName = "Sarah Johnson",
                        title = "MD",
                        npi = "1234567890",
                        dea = "AB1234567",
                        phone = "(555) 987-6543",
                        fax = "(555) 987-6544",
                        address = "123 Medical Center Dr",
                        city = "Lusaka",
                        state = "Lusaka",
                        zip = "10101"
                    },
                    new
                    {
                        id = Guid.NewGuid(),
                        fullName = "Robert Chen",
                        title = "MD",
                        npi = "0987654321",
                        dea = "CD7654321",
                        phone = "(555) 345-6789",
                        fax = "(555) 345-6790",
                        address = "456 Health Plaza",
                        city = "Kitwe",
                        state = "Central",
                        zip = "10102"
                    }
                };

                return Ok(new { prescribers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prescribers for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to fetch prescribers" });
            }
        }

        [HttpPost("prescriptions")]
        public async Task<IActionResult> CreatePrescription([FromQuery] Guid tenantId, [FromBody] object prescriptionData)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // This would create the prescription in the database
                // For now, return success response
                var newPrescriptionId = Guid.NewGuid();
                
                return Ok(new 
                { 
                    id = newPrescriptionId.ToString(),
                    message = "Prescription created successfully",
                    status = "submitted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to create prescription" });
            }
        }

        [HttpPut("prescriptions/{prescriptionId}/status")]
        public async Task<IActionResult> UpdatePrescriptionStatus([FromQuery] Guid tenantId, string prescriptionId, [FromBody] StatusUpdateRequest request)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // This would update the prescription status in the database
                // For now, return success response
                return Ok(new 
                { 
                    message = $"Prescription {prescriptionId} status updated to {request.NewStatus}",
                    prescriptionId = prescriptionId,
                    newStatus = request.NewStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prescription status for tenant {TenantId}, prescription {PrescriptionId}", tenantId, prescriptionId);
                return StatusCode(500, new { error = "Failed to update prescription status" });
            }
        }

        [HttpPost("compounds")]
        public async Task<IActionResult> CreateCompound([FromQuery] Guid tenantId, [FromBody] object compoundData)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // This would create compound in database
                // For now, return success response
                var newCompoundId = Guid.NewGuid();
                
                return Ok(new 
                { 
                    id = newCompoundId.ToString(),
                    message = "Compound created successfully",
                    status = "created"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating compound for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to create compound" });
            }
        }

        [HttpPost("drug-interactions")]
        public async Task<IActionResult> CheckDrugInteractions([FromQuery] Guid tenantId, [FromBody] object interactionData)
        {
            try
            {
                var tenantIdFromClaims = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantIdFromClaims) || Guid.Parse(tenantIdFromClaims) != tenantId)
                {
                    return Forbid();
                }

                // This would check drug interactions using clinical decision support
                // For now, return sample interaction results
                var interactions = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        type = "drug-interaction",
                        severity = "warning",
                        icon = "warning",
                        title = "Potential Drug Interaction",
                        description = "Moderate interaction detected between medications",
                        recommendation = "Monitor patient closely and consider alternative therapy"
                    }
                };

                return Ok(new { interactions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking drug interactions for tenant {TenantId}", tenantId);
                return StatusCode(500, new { error = "Failed to check drug interactions" });
            }
        }

        private string FormatRelativeTime(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var diff = now - dateTime;
            var minutes = (int)diff.TotalMinutes;
            var hours = (int)diff.TotalHours;
            var days = (int)diff.TotalDays;

            if (minutes < 1) return "Just now";
            if (minutes < 60) return $"{minutes} minute{(minutes > 1 ? "s" : "")} ago";
            if (hours < 24) return $"{hours} hour{(hours > 1 ? "s" : "")} ago";
            if (days < 7) return $"{days} day{(days > 1 ? "s" : "")} ago";
            
            return dateTime.ToString("MMM dd, yyyy");
        }
    }
}
