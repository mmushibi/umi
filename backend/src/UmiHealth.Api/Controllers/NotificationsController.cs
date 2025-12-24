using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Application.DTOs;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? type = null,
            [FromQuery] bool? unreadOnly = null)
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                var notifications = await _notificationService.GetUserNotificationsAsync(tenantId, userId, page, pageSize, type, unreadOnly);
                
                return Ok(new NotificationListResponse
                {
                    Notifications = notifications,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve notifications." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetNotification(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                var notification = await _notificationService.GetNotificationByIdAsync(tenantId, userId, id);
                
                if (notification == null)
                    return NotFound();

                return Ok(notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve notification." });
            }
        }

        [HttpPost("{id}/mark-read")]
        public async Task<ActionResult> MarkAsRead(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                var success = await _notificationService.MarkAsReadAsync(tenantId, userId, id);
                
                if (!success)
                    return NotFound();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to mark notification as read." });
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                await _notificationService.MarkAllAsReadAsync(tenantId, userId);
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to mark all notifications as read." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                var success = await _notificationService.DeleteNotificationAsync(tenantId, userId, id);
                
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete notification." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var senderId = GetUserId();
                var tenantId = GetTenantId();
                var notification = await _notificationService.CreateNotificationAsync(tenantId, senderId, request);
                
                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to create notification." });
            }
        }

        [HttpPost("broadcast")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
        {
            try
            {
                var senderId = GetUserId();
                var tenantId = GetTenantId();
                await _notificationService.BroadcastNotificationAsync(tenantId, senderId, request);
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to broadcast notification." });
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                var count = await _notificationService.GetUnreadCountAsync(tenantId, userId);
                
                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get unread count." });
            }
        }

        [HttpGet("settings")]
        public async Task<ActionResult<NotificationSettingsDto>> GetNotificationSettings()
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                var settings = await _notificationService.GetNotificationSettingsAsync(tenantId, userId);
                
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get notification settings." });
            }
        }

        [HttpPut("settings")]
        public async Task<ActionResult<NotificationSettingsDto>> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsRequest request)
        {
            try
            {
                var userId = GetUserId();
                var tenantId = GetTenantId();
                var settings = await _notificationService.UpdateNotificationSettingsAsync(tenantId, userId, request);
                
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update notification settings." });
            }
        }

        [HttpPost("test")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> TestNotification([FromBody] TestNotificationRequest request)
        {
            try
            {
                var senderId = GetUserId();
                var tenantId = GetTenantId();
                await _notificationService.SendTestNotificationAsync(tenantId, senderId, request);
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to send test notification." });
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found");

            return Guid.Parse(userIdClaim);
        }

        private Guid GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                throw new UnauthorizedAccessException("Tenant information not found");

            return Guid.Parse(tenantIdClaim);
        }
    }

    public class NotificationListResponse
    {
        public IEnumerable<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();
        public int Page { get; set; }
        public int PageSize { get; set; }
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
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
        public string? ActionUrl { get; set; }
        public List<Guid>? UserIds { get; set; } // If null, broadcast to all users
        public List<string>? Roles { get; set; } // If provided, broadcast to users with these roles
        public List<Guid>? BranchIds { get; set; } // If provided, broadcast to users in these branches
        public bool IsHighPriority { get; set; } = false;
    }

    public class UpdateNotificationSettingsRequest
    {
        public bool EmailEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = false;
        public bool PushEnabled { get; set; } = true;
        public bool LowStockAlerts { get; set; } = true;
        public bool ExpiryAlerts { get; set; } = true;
        public bool PrescriptionAlerts { get; set; } = true;
        public bool PaymentAlerts { get; set; } = true;
        public bool SystemAlerts { get; set; } = true;
        public Dictionary<string, bool>? CustomAlerts { get; set; }
    }

    public class TestNotificationRequest
    {
        public string Type { get; set; } = "info";
        public string Title { get; set; } = "Test Notification";
        public string Message { get; set; } = "This is a test notification";
        public Guid? UserId { get; set; }
    }
}
