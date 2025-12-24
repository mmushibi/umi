using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/operations")]
    [Authorize(Roles = "admin,super_admin,operations")]
    public class OperationsController : ControllerBase
    {
        private readonly IOperationsService _operationsService;

        public OperationsController(IOperationsService operationsService)
        {
            _operationsService = operationsService;
        }

        [HttpGet("dashboard/stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                var stats = await _operationsService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get dashboard stats." });
            }
        }

        [HttpGet("dashboard/recent-tenants")]
        public async Task<ActionResult<IEnumerable<RecentTenantDto>>> GetRecentTenants()
        {
            try
            {
                var tenants = await _operationsService.GetRecentTenantsAsync(10);
                return Ok(tenants);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get recent tenants." });
            }
        }

        [HttpGet("tenants")]
        public async Task<ActionResult<PagedResult<TenantDto>>> GetTenants(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var result = await _operationsService.GetTenantsAsync(page, pageSize, search, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get tenants." });
            }
        }

        [HttpPost("tenants")]
        [Authorize(Roles = "admin,super_admin")]
        public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantRequest request)
        {
            try
            {
                var tenant = await _operationsService.CreateTenantAsync(request);
                return CreatedAtAction(nameof(GetTenants), new { id = tenant.Id }, tenant);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to create tenant." });
            }
        }

        [HttpPut("tenants/{id}")]
        [Authorize(Roles = "admin,super_admin")]
        public async Task<ActionResult<TenantDto>> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
        {
            try
            {
                var tenant = await _operationsService.UpdateTenantAsync(id, request);
                if (tenant == null)
                    return NotFound();

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update tenant." });
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? tenantId = null)
        {
            try
            {
                var result = await _operationsService.GetUsersAsync(page, pageSize, search, status, tenantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get users." });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _operationsService.UpdateUserAsync(id, request);
                if (user == null)
                    return NotFound();

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update user." });
            }
        }

        [HttpGet("subscriptions")]
        public async Task<ActionResult<PagedResult<SubscriptionDto>>> GetSubscriptions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? tenantId = null)
        {
            try
            {
                var result = await _operationsService.GetSubscriptionsAsync(page, pageSize, search, status, tenantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get subscriptions." });
            }
        }

        [HttpPut("subscriptions/{id}")]
        public async Task<ActionResult<SubscriptionDto>> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request)
        {
            try
            {
                var subscription = await _operationsService.UpdateSubscriptionAsync(id, request);
                if (subscription == null)
                    return NotFound();

                return Ok(subscription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update subscription." });
            }
        }

        [HttpPost("subscriptions/{id}/upgrade")]
        public async Task<ActionResult<SubscriptionDto>> UpgradeSubscription(Guid id, [FromBody] UpgradeSubscriptionRequest request)
        {
            try
            {
                var subscription = await _operationsService.UpgradeSubscriptionAsync(id, request);
                if (subscription == null)
                    return NotFound();

                return Ok(subscription);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to upgrade subscription." });
            }
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? tenantId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _operationsService.GetTransactionsAsync(page, pageSize, search, status, tenantId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get transactions." });
            }
        }

        [HttpGet("transactions/{id}/receipt")]
        public async Task<ActionResult> DownloadTransactionReceipt(Guid id)
        {
            try
            {
                var receipt = await _operationsService.GenerateTransactionReceiptAsync(id);
                if (receipt == null)
                    return NotFound();

                return File(receipt.Content, "application/pdf", $"receipt-{id}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate receipt." });
            }
        }

        [HttpGet("sync/status")]
        public async Task<ActionResult<SyncStatusDto>> GetSyncStatus()
        {
            try
            {
                var status = await _operationsService.GetSyncStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get sync status." });
            }
        }

        [HttpPost("sync/trigger")]
        [Authorize(Roles = "admin,super_admin")]
        public async Task<ActionResult> TriggerSync([FromBody] TriggerSyncRequest request)
        {
            try
            {
                await _operationsService.TriggerSyncAsync(request.SyncType);
                return Ok(new { success = true, message = "Sync triggered successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to trigger sync." });
            }
        }
    }

    // DTOs
    public class DashboardStatsDto
    {
        public int TotalTenants { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TotalUsers { get; set; }
        public int ExpiringSoon { get; set; }
        public double MonthlyRevenue { get; set; }
        public double YearlyRevenue { get; set; }
        public int NewTenantsThisMonth { get; set; }
        public int NewUsersThisMonth { get; set; }
    }

    public class RecentTenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class UpdateSubscriptionRequest
    {
        public string Plan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? NextBilling { get; set; }
    }

    public class UpgradeSubscriptionRequest
    {
        public string TargetPlan { get; set; } = string.Empty;
        public bool ProRated { get; set; } = true;
    }

    public class SyncStatusDto
    {
        public string LastSyncTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int PendingRecords { get; set; }
        public int FailedRecords { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class TriggerSyncRequest
    {
        public string SyncType { get; set; } = "full"; // full, incremental, tenants, users, subscriptions
    }
}
