using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmiHealth.MinimalApi.Services;
using UmiHealth.MinimalApi.Models;
using System.Security.Claims;

namespace UmiHealth.MinimalApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public InventoryController(IInventoryService inventoryService, IAuditService auditService)
    {
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    [HttpGet("{inventoryId}")]
    public async Task<IActionResult> GetInventory(string inventoryId)
    {
        var inventory = await _inventoryService.GetInventoryByIdAsync(inventoryId);
        if (inventory == null)
        {
            return NotFound(new { success = false, message = "Inventory item not found" });
        }

        return Ok(new { success = true, inventory });
    }

    [HttpGet]
    public async Task<IActionResult> GetInventory()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var inventory = await _inventoryService.GetInventoryByTenantAsync(tenantId);
        return Ok(new { success = true, inventory });
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockItems([FromQuery] int threshold = 10)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var items = await _inventoryService.GetLowStockItemsAsync(tenantId, threshold);
        return Ok(new { success = true, items });
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchInventory([FromBody] SearchRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var items = await _inventoryService.SearchInventoryAsync(tenantId, request.SearchTerm);
        return Ok(new { success = true, items });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> CreateInventory([FromBody] Inventory inventory)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        inventory.TenantId = tenantId;
        var result = await _inventoryService.CreateInventoryItemAsync(inventory);
        
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, inventory = result.Inventory });
    }

    [HttpPut("{inventoryId}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> UpdateInventory(string inventoryId, [FromBody] Inventory inventory)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var existingItem = await _inventoryService.GetInventoryByIdAsync(inventoryId);
        if (existingItem == null || existingItem.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Inventory item not found" });
        }

        var result = await _inventoryService.UpdateInventoryAsync(inventoryId, inventory);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message, inventory = result.Inventory });
    }

    [HttpDelete("{inventoryId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteInventory(string inventoryId)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var existingItem = await _inventoryService.GetInventoryByIdAsync(inventoryId);
        if (existingItem == null || existingItem.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Inventory item not found" });
        }

        var result = await _inventoryService.DeleteInventoryAsync(inventoryId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost("{inventoryId}/stock")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> UpdateStock(string inventoryId, [FromBody] StockUpdateRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var existingItem = await _inventoryService.GetInventoryByIdAsync(inventoryId);
        if (existingItem == null || existingItem.TenantId != tenantId)
        {
            return NotFound(new { success = false, message = "Inventory item not found" });
        }

        var result = await _inventoryService.UpdateStockAsync(inventoryId, request.Quantity, request.Operation);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost("batch-stock")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> BatchUpdateStock([FromBody] BatchStockUpdateRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var result = await _inventoryService.BatchUpdateStockAsync(request.Updates);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpGet("expiry-check")]
    public async Task<IActionResult> CheckExpiryDates([FromQuery] int daysThreshold = 30)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Unauthorized();
        }

        var result = await _inventoryService.CheckExpiryDatesAsync(tenantId, daysThreshold);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }
}

public class StockUpdateRequest
{
    public int Quantity { get; set; }
    public string Operation { get; set; } = string.Empty; // "add", "subtract", "set"
}

public class BatchStockUpdateRequest
{
    public Dictionary<string, (int Quantity, string Operation)> Updates { get; set; } = new();
}
