using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UmiHealth.Application.Services;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchInventoryService _inventoryService;
        private readonly IStockTransferService _transferService;
        private readonly IProcurementService _procurementService;
        private readonly IBranchPermissionService _permissionService;
        private readonly IBranchReportingService _reportingService;
        private readonly ITenantService _tenantService;

        public BranchController(
            IBranchInventoryService inventoryService,
            IStockTransferService transferService,
            IProcurementService procurementService,
            IBranchPermissionService permissionService,
            IBranchReportingService reportingService,
            ITenantService tenantService)
        {
            _inventoryService = inventoryService;
            _transferService = transferService;
            _procurementService = procurementService;
            _permissionService = permissionService;
            _reportingService = reportingService;
            _tenantService = tenantService;
        }

        // Branch Inventory Management
        [HttpGet("{branchId}/inventory")]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetBranchInventory(Guid branchId)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.INVENTORY_READ))
                return Forbid();

            var inventory = await _inventoryService.GetBranchInventoryAsync(branchId);
            return Ok(inventory);
        }

        [HttpGet("{branchId}/inventory/stats")]
        public async Task<ActionResult<Dictionary<string, object>>> GetInventoryStats(Guid branchId)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.INVENTORY_READ))
                return Forbid();

            var stats = await _inventoryService.GetInventoryStatsAsync(branchId);
            return Ok(stats);
        }

        [HttpGet("{branchId}/inventory/low-stock")]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetLowStockItems(Guid branchId)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.INVENTORY_READ))
                return Forbid();

            var items = await _inventoryService.GetLowStockItemsAsync(branchId);
            return Ok(items);
        }

        [HttpGet("{branchId}/inventory/expiring")]
        public async Task<ActionResult<IEnumerable<Inventory>>> GetExpiringItems(Guid branchId, [FromQuery] int days = 30)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.INVENTORY_READ))
                return Forbid();

            var items = await _inventoryService.GetExpiringItemsAsync(branchId, days);
            return Ok(items);
        }

        [HttpPut("{branchId}/inventory/{productId}")]
        public async Task<ActionResult<Inventory>> UpdateInventory(Guid branchId, Guid productId, [FromBody] UpdateInventoryRequest request)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.INVENTORY_WRITE))
                return Forbid();

            var inventory = await _inventoryService.UpdateInventoryAsync(branchId, productId, request.Quantity, request.Reason);
            return Ok(inventory);
        }

        // Stock Transfers
        [HttpPost("transfers")]
        public async Task<ActionResult<StockTransfer>> CreateTransferRequest([FromBody] CreateStockTransferRequest request)
        {
            if (!await HasPermissionAsync(request.SourceBranchId, BranchPermissions.STOCK_TRANSFER))
                return Forbid();

            var transfer = await _transferService.CreateTransferRequestAsync(request);
            return CreatedAtAction(nameof(GetTransfer), new { transferId = transfer.Id }, transfer);
        }

        [HttpGet("{branchId}/transfers/pending")]
        public async Task<ActionResult<IEnumerable<StockTransfer>>> GetPendingTransfers(Guid branchId)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.INVENTORY_READ))
                return Forbid();

            var transfers = await _transferService.GetPendingTransfersAsync(branchId);
            return Ok(transfers);
        }

        [HttpGet("{branchId}/transfers/history")]
        public async Task<ActionResult<IEnumerable<StockTransfer>>> GetTransferHistory(
            Guid branchId, 
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.INVENTORY_READ))
                return Forbid();

            var transfers = await _transferService.GetTransferHistoryAsync(branchId, fromDate, toDate);
            return Ok(transfers);
        }

        [HttpGet("transfers/{transferId}")]
        public async Task<ActionResult<StockTransfer>> GetTransfer(Guid transferId)
        {
            var transfer = await _transferService.GetTransferAsync(transferId);
            if (transfer == null)
                return NotFound();

            // Check if user has access to either source or destination branch
            if (!await HasPermissionAsync(transfer.SourceBranchId, BranchPermissions.INVENTORY_READ) &&
                !await HasPermissionAsync(transfer.DestinationBranchId, BranchPermissions.INVENTORY_READ))
                return Forbid();

            return Ok(transfer);
        }

        [HttpPost("transfers/{transferId}/approve")]
        public async Task<ActionResult> ApproveTransfer(Guid transferId, [FromBody] ApproveTransferRequest request)
        {
            var transfer = await _transferService.GetTransferAsync(transferId);
            if (transfer == null)
                return NotFound();

            if (!await HasPermissionAsync(transfer.DestinationBranchId, BranchPermissions.APPROVE_TRANSFERS))
                return Forbid();

            var success = await _transferService.ApproveTransferAsync(transferId, request.ApprovedByUserId, request.Notes);
            return success ? Ok() : BadRequest();
        }

        [HttpPost("transfers/{transferId}/reject")]
        public async Task<ActionResult> RejectTransfer(Guid transferId, [FromBody] RejectTransferRequest request)
        {
            var transfer = await _transferService.GetTransferAsync(transferId);
            if (transfer == null)
                return NotFound();

            if (!await HasPermissionAsync(transfer.DestinationBranchId, BranchPermissions.APPROVE_TRANSFERS))
                return Forbid();

            var success = await _transferService.RejectTransferAsync(transferId, request.RejectedByUserId, request.Reason);
            return success ? Ok() : BadRequest();
        }

        [HttpPost("transfers/{transferId}/complete")]
        public async Task<ActionResult> CompleteTransfer(Guid transferId, [FromBody] CompleteTransferRequest request)
        {
            var transfer = await _transferService.GetTransferAsync(transferId);
            if (transfer == null)
                return NotFound();

            if (!await HasPermissionAsync(transfer.SourceBranchId, BranchPermissions.INVENTORY_WRITE))
                return Forbid();

            var success = await _transferService.CompleteTransferAsync(transferId, request.Items);
            return success ? Ok() : BadRequest();
        }

        // Procurement
        [HttpPost("procurement")]
        public async Task<ActionResult<ProcurementRequest>> CreateProcurementRequest([FromBody] CreateProcurementRequest request)
        {
            if (!await HasPermissionAsync(request.RequestingBranchId, BranchPermissions.PROCUREMENT_WRITE))
                return Forbid();

            var procurement = await _procurementService.CreateProcurementRequestAsync(request);
            return CreatedAtAction(nameof(GetProcurementRequest), new { requestId = procurement.Id }, procurement);
        }

        [HttpGet("{branchId}/procurement/pending")]
        public async Task<ActionResult<IEnumerable<ProcurementRequest>>> GetPendingProcurementRequests(Guid branchId)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.PROCUREMENT_READ))
                return Forbid();

            var requests = await _procurementService.GetPendingRequestsAsync(branchId);
            return Ok(requests);
        }

        [HttpGet("{branchId}/procurement/history")]
        public async Task<ActionResult<IEnumerable<ProcurementRequest>>> GetProcurementHistory(
            Guid branchId, 
            [FromQuery] DateTime? fromDate = null, 
            [FromQuery] DateTime? toDate = null)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.PROCUREMENT_READ))
                return Forbid();

            var requests = await _procurementService.GetProcurementHistoryAsync(branchId, fromDate, toDate);
            return Ok(requests);
        }

        [HttpGet("procurement/{requestId}")]
        public async Task<ActionResult<ProcurementRequest>> GetProcurementRequest(Guid requestId)
        {
            var request = await _procurementService.GetProcurementRequestAsync(requestId);
            if (request == null)
                return NotFound();

            // Check if user has access to requesting or approving branch
            if (!await HasPermissionAsync(request.RequestingBranchId, BranchPermissions.PROCUREMENT_READ) &&
                (request.ApprovingBranchId == null || !await HasPermissionAsync(request.ApprovingBranchId.Value, BranchPermissions.PROCUREMENT_READ)))
                return Forbid();

            return Ok(request);
        }

        [HttpPost("procurement/{requestId}/approve")]
        public async Task<ActionResult> ApproveProcurementRequest(Guid requestId, [FromBody] ApproveProcurementRequest request)
        {
            var procurement = await _procurementService.GetProcurementRequestAsync(requestId);
            if (procurement == null)
                return NotFound();

            if (procurement.ApprovingBranchId == null || !await HasPermissionAsync(procurement.ApprovingBranchId.Value, BranchPermissions.PROCUREMENT_APPROVE))
                return Forbid();

            var success = await _procurementService.ApproveProcurementRequestAsync(requestId, request.ApprovedByUserId, request.Items, request.Notes);
            return success ? Ok() : BadRequest();
        }

        // Branch Permissions
        [HttpGet("{branchId}/permissions")]
        public async Task<ActionResult<IEnumerable<BranchPermission>>> GetBranchPermissions(Guid branchId)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.MANAGE_USERS))
                return Forbid();

            var permissions = await _permissionService.GetBranchPermissionsAsync(branchId);
            return Ok(permissions);
        }

        [HttpPost("{branchId}/permissions")]
        public async Task<ActionResult<BranchPermission>> GrantPermission(Guid branchId, [FromBody] GrantPermissionRequest request)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.MANAGE_USERS))
                return Forbid();

            request.BranchId = branchId;
            var permission = await _permissionService.GrantPermissionAsync(request);
            return CreatedAtAction(nameof(GetUserPermissions), new { userId = request.UserId }, permission);
        }

        [HttpPut("permissions/{permissionId}")]
        public async Task<ActionResult> UpdatePermission(Guid permissionId, [FromBody] UpdatePermissionRequest request)
        {
            var permission = await _permissionService.UpdatePermissionAsync(permissionId, request);
            return permission ? Ok() : NotFound();
        }

        [HttpDelete("permissions/{permissionId}")]
        public async Task<ActionResult> RevokePermission(Guid permissionId)
        {
            var success = await _permissionService.RevokePermissionAsync(permissionId);
            return success ? Ok() : NotFound();
        }

        [HttpGet("users/{userId}/permissions")]
        public async Task<ActionResult<IEnumerable<BranchPermission>>> GetUserPermissions(Guid userId)
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId);
            return Ok(permissions);
        }

        [HttpGet("users/{userId}/permissions/summary")]
        public async Task<ActionResult<Dictionary<string, object>>> GetUserPermissionSummary(Guid userId)
        {
            var summary = await _permissionService.GetUserPermissionSummaryAsync(userId);
            return Ok(summary);
        }

        // Branch Reports
        [HttpGet("{branchId}/reports")]
        public async Task<ActionResult<IEnumerable<BranchReport>>> GetBranchReports(
            Guid branchId, 
            [FromQuery] string? type = null, 
            [FromQuery] int limit = 50)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.VIEW_REPORTS))
                return Forbid();

            var reports = await _reportingService.GetBranchReportsAsync(branchId, type, limit);
            return Ok(reports);
        }

        [HttpPost("{branchId}/reports/sales")]
        public async Task<ActionResult<BranchReport>> GenerateSalesReport(
            Guid branchId, 
            [FromBody] GenerateReportRequest request)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.VIEW_REPORTS))
                return Forbid();

            var report = await _reportingService.GenerateSalesReportAsync(
                branchId, request.StartDate, request.EndDate, request.Period);
            return CreatedAtAction(nameof(GetReport), new { reportId = report.Id }, report);
        }

        [HttpPost("{branchId}/reports/inventory")]
        public async Task<ActionResult<BranchReport>> GenerateInventoryReport(
            Guid branchId, 
            [FromBody] GenerateReportRequest request)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.VIEW_REPORTS))
                return Forbid();

            var report = await _reportingService.GenerateInventoryReportAsync(branchId, request.StartDate, request.EndDate);
            return CreatedAtAction(nameof(GetReport), new { reportId = report.Id }, report);
        }

        [HttpPost("{branchId}/reports/financial")]
        public async Task<ActionResult<BranchReport>> GenerateFinancialReport(
            Guid branchId, 
            [FromBody] GenerateReportRequest request)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.VIEW_REPORTS))
                return Forbid();

            var report = await _reportingService.GenerateFinancialReportAsync(branchId, request.StartDate, request.EndDate);
            return CreatedAtAction(nameof(GetReport), new { reportId = report.Id }, report);
        }

        [HttpGet("{branchId}/dashboard")]
        public async Task<ActionResult<Dictionary<string, object>>> GetBranchDashboard(
            Guid branchId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (!await HasPermissionAsync(branchId, BranchPermissions.VIEW_REPORTS))
                return Forbid();

            var dashboard = await _reportingService.GetBranchDashboardAsync(branchId, fromDate, toDate);
            return Ok(dashboard);
        }

        [HttpGet("reports/{reportId}")]
        public async Task<ActionResult<BranchReport>> GetReport(Guid reportId)
        {
            var report = await _reportingService.GetReportAsync(reportId);
            if (report == null)
                return NotFound();

            if (!await HasPermissionAsync(report.BranchId, BranchPermissions.VIEW_REPORTS))
                return Forbid();

            return Ok(report);
        }

        [HttpGet("reports/{reportId}/export")]
        public async Task<ActionResult> ExportReport(Guid reportId, [FromQuery] string format = "pdf")
        {
            var report = await _reportingService.GetReportAsync(reportId);
            if (report == null)
                return NotFound();

            if (!await HasPermissionAsync(report.BranchId, BranchPermissions.VIEW_REPORTS))
                return Forbid();

            var data = await _reportingService.ExportReportAsync(reportId, format);
            var contentType = format.ToLower() switch
            {
                "pdf" => "application/pdf",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "csv" => "text/csv",
                _ => "application/octet-stream"
            };

            return File(data, contentType, $"{report.Name}.{format}");
        }

        [HttpDelete("reports/{reportId}")]
        public async Task<ActionResult> DeleteReport(Guid reportId)
        {
            var report = await _reportingService.GetReportAsync(reportId);
            if (report == null)
                return NotFound();

            if (!await HasPermissionAsync(report.BranchId, BranchPermissions.MANAGE_SETTINGS))
                return Forbid();

            var success = await _reportingService.DeleteReportAsync(reportId);
            return success ? Ok() : NotFound();
        }

        // Cross-branch comparison (for managers with appropriate permissions)
        [HttpGet("comparison")]
        public async Task<ActionResult<Dictionary<string, object>>> GetCrossBranchComparison(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var tenantId = HttpContext.GetCurrentTenantId();
            if (tenantId == null)
                return BadRequest("Tenant context not found");

            // Check if user has cross-branch reporting permissions
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var userPermissions = await _permissionService.GetUserPermissionsAsync(userId.Value);
            var hasCrossBranchAccess = userPermissions.Any(p => p.IsManager && p.CanViewReports);

            if (!hasCrossBranchAccess)
                return Forbid();

            var comparison = await _reportingService.GetCrossBranchComparisonAsync(tenantId.Value, startDate, endDate);
            return Ok(comparison);
        }

        // Helper methods
        private async Task<bool> HasPermissionAsync(Guid branchId, string permission)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return false;

            return await _permissionService.HasPermissionAsync(userId.Value, branchId, permission);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    // DTOs for API requests
    public class UpdateInventoryRequest
    {
        public int Quantity { get; set; }
        public string? Reason { get; set; }
    }

    public class ApproveTransferRequest
    {
        public Guid ApprovedByUserId { get; set; }
        public string? Notes { get; set; }
    }

    public class RejectTransferRequest
    {
        public Guid RejectedByUserId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class CompleteTransferRequest
    {
        public List<CompleteTransferItem> Items { get; set; } = new();
    }

    public class ApproveProcurementRequest
    {
        public Guid ApprovedByUserId { get; set; }
        public List<ApproveProcurementItem> Items { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class GenerateReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Period { get; set; } = "daily";
    }
}
