using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UmiHealth.Application.Services;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IProcurementService _procurementService;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(IProcurementService procurementService, ILogger<PurchaseOrdersController> logger)
        {
            _procurementService = procurementService;
            _logger = logger;
        }

        [HttpGet]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PurchaseOrderDto>>>> GetPurchaseOrders(
            [FromQuery] PurchaseOrderFilterDto filter,
            [FromQuery] PagedRequest pagedRequest)
        {
            try
            {
                var tenantId = GetTenantId();
                var branchId = GetBranchId();

                var purchaseOrders = await _procurementService.GetPurchaseOrdersAsync(tenantId, branchId, new(
                    filter.SupplierId,
                    filter.Status,
                    filter.StartDate,
                    filter.EndDate
                ));

                var pagedResponse = PagedResponse<PurchaseOrderDto>.Create(
                    purchaseOrders.Select(po => new PurchaseOrderDto
                    {
                        Id = po.Id,
                        SupplierId = po.SupplierId,
                        SupplierName = po.Supplier?.Name,
                        BranchId = po.BranchId,
                        OrderNumber = po.OrderNumber,
                        OrderDate = po.OrderDate,
                        ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                        ReceivedDate = po.ReceivedDate,
                        Status = po.Status,
                        Subtotal = po.Subtotal,
                        TaxAmount = po.TaxAmount,
                        ShippingCost = po.ShippingCost,
                        TotalAmount = po.TotalAmount,
                        Notes = po.Notes,
                        CreatedAt = po.CreatedAt,
                        UpdatedAt = po.UpdatedAt
                    }),
                    pagedRequest.PageNumber,
                    pagedRequest.PageSize,
                    purchaseOrders.Count
                );

                return Ok(ApiResponse<PagedResponse<PurchaseOrderDto>>.SuccessResult(pagedResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase orders");
                return BadRequest(ApiResponse<PagedResponse<PurchaseOrderDto>>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("{id}")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetPurchaseOrder(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var purchaseOrder = await _procurementService.GetPurchaseOrderAsync(id, tenantId);
                
                if (purchaseOrder == null)
                {
                    return NotFound(ApiResponse<PurchaseOrderDto>.ErrorResult("Purchase order not found"));
                }

                var purchaseOrderDto = new PurchaseOrderDto
                {
                    Id = purchaseOrder.Id,
                    SupplierId = purchaseOrder.SupplierId,
                    SupplierName = purchaseOrder.Supplier?.Name,
                    BranchId = purchaseOrder.BranchId,
                    OrderNumber = purchaseOrder.OrderNumber,
                    OrderDate = purchaseOrder.OrderDate,
                    ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                    ReceivedDate = purchaseOrder.ReceivedDate,
                    Status = purchaseOrder.Status,
                    Subtotal = purchaseOrder.Subtotal,
                    TaxAmount = purchaseOrder.TaxAmount,
                    ShippingCost = purchaseOrder.ShippingCost,
                    TotalAmount = purchaseOrder.TotalAmount,
                    Notes = purchaseOrder.Notes,
                    CreatedAt = purchaseOrder.CreatedAt,
                    UpdatedAt = purchaseOrder.UpdatedAt
                };

                return Ok(ApiResponse<PurchaseOrderDto>.SuccessResult(purchaseOrderDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase order {PurchaseOrderId}", id);
                return BadRequest(ApiResponse<PurchaseOrderDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPost]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();
                var branchId = GetBranchId();

                var purchaseOrder = await _procurementService.CreatePurchaseOrderAsync(tenantId, branchId, userId, new(
                    request.SupplierId,
                    request.ExpectedDeliveryDate,
                    request.Items,
                    request.Notes
                ));

                var purchaseOrderDto = new PurchaseOrderDto
                {
                    Id = purchaseOrder.Id,
                    SupplierId = purchaseOrder.SupplierId,
                    SupplierName = purchaseOrder.Supplier?.Name,
                    BranchId = purchaseOrder.BranchId,
                    OrderNumber = purchaseOrder.OrderNumber,
                    OrderDate = purchaseOrder.OrderDate,
                    ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                    ReceivedDate = purchaseOrder.ReceivedDate,
                    Status = purchaseOrder.Status,
                    Subtotal = purchaseOrder.Subtotal,
                    TaxAmount = purchaseOrder.TaxAmount,
                    ShippingCost = purchaseOrder.ShippingCost,
                    TotalAmount = purchaseOrder.TotalAmount,
                    Notes = purchaseOrder.Notes,
                    CreatedAt = purchaseOrder.CreatedAt,
                    UpdatedAt = purchaseOrder.UpdatedAt
                };

                return Ok(ApiResponse<PurchaseOrderDto>.SuccessResult(purchaseOrderDto, "Purchase order created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating purchase order");
                return BadRequest(ApiResponse<PurchaseOrderDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> UpdatePurchaseOrder(Guid id, [FromBody] UpdatePurchaseOrderDto request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var purchaseOrder = await _procurementService.UpdatePurchaseOrderAsync(id, tenantId, userId, new(
                    request.ExpectedDeliveryDate,
                    request.Notes
                ));

                var purchaseOrderDto = new PurchaseOrderDto
                {
                    Id = purchaseOrder.Id,
                    SupplierId = purchaseOrder.SupplierId,
                    SupplierName = purchaseOrder.Supplier?.Name,
                    BranchId = purchaseOrder.BranchId,
                    OrderNumber = purchaseOrder.OrderNumber,
                    OrderDate = purchaseOrder.OrderDate,
                    ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                    ReceivedDate = purchaseOrder.ReceivedDate,
                    Status = purchaseOrder.Status,
                    Subtotal = purchaseOrder.Subtotal,
                    TaxAmount = purchaseOrder.TaxAmount,
                    ShippingCost = purchaseOrder.ShippingCost,
                    TotalAmount = purchaseOrder.TotalAmount,
                    Notes = purchaseOrder.Notes,
                    CreatedAt = purchaseOrder.CreatedAt,
                    UpdatedAt = purchaseOrder.UpdatedAt
                };

                return Ok(ApiResponse<PurchaseOrderDto>.SuccessResult(purchaseOrderDto, "Purchase order updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating purchase order {PurchaseOrderId}", id);
                return BadRequest(ApiResponse<PurchaseOrderDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("{id}/approve")]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> ApprovePurchaseOrder(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var purchaseOrder = await _procurementService.ApprovePurchaseOrderAsync(id, tenantId, userId);

                var purchaseOrderDto = new PurchaseOrderDto
                {
                    Id = purchaseOrder.Id,
                    SupplierId = purchaseOrder.SupplierId,
                    SupplierName = purchaseOrder.Supplier?.Name,
                    BranchId = purchaseOrder.BranchId,
                    OrderNumber = purchaseOrder.OrderNumber,
                    OrderDate = purchaseOrder.OrderDate,
                    ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                    ReceivedDate = purchaseOrder.ReceivedDate,
                    Status = purchaseOrder.Status,
                    Subtotal = purchaseOrder.Subtotal,
                    TaxAmount = purchaseOrder.TaxAmount,
                    ShippingCost = purchaseOrder.ShippingCost,
                    TotalAmount = purchaseOrder.TotalAmount,
                    Notes = purchaseOrder.Notes,
                    CreatedAt = purchaseOrder.CreatedAt,
                    UpdatedAt = purchaseOrder.UpdatedAt
                };

                return Ok(ApiResponse<PurchaseOrderDto>.SuccessResult(purchaseOrderDto, "Purchase order approved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving purchase order {PurchaseOrderId}", id);
                return BadRequest(ApiResponse<PurchaseOrderDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPost("{id}/receive")]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> ReceivePurchaseOrder(Guid id, [FromBody] ReceivePurchaseOrderDto request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var purchaseOrder = await _procurementService.ReceivePurchaseOrderAsync(id, tenantId, userId, new(
                    request.Items
                ));

                var purchaseOrderDto = new PurchaseOrderDto
                {
                    Id = purchaseOrder.Id,
                    SupplierId = purchaseOrder.SupplierId,
                    SupplierName = purchaseOrder.Supplier?.Name,
                    BranchId = purchaseOrder.BranchId,
                    OrderNumber = purchaseOrder.OrderNumber,
                    OrderDate = purchaseOrder.OrderDate,
                    ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                    ReceivedDate = purchaseOrder.ReceivedDate,
                    Status = purchaseOrder.Status,
                    Subtotal = purchaseOrder.Subtotal,
                    TaxAmount = purchaseOrder.TaxAmount,
                    ShippingCost = purchaseOrder.ShippingCost,
                    TotalAmount = purchaseOrder.TotalAmount,
                    Notes = purchaseOrder.Notes,
                    CreatedAt = purchaseOrder.CreatedAt,
                    UpdatedAt = purchaseOrder.UpdatedAt
                };

                return Ok(ApiResponse<PurchaseOrderDto>.SuccessResult(purchaseOrderDto, "Purchase order received successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving purchase order {PurchaseOrderId}", id);
                return BadRequest(ApiResponse<PurchaseOrderDto>.ErrorResult(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<bool>>> DeletePurchaseOrder(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var result = await _procurementService.DeletePurchaseOrderAsync(id, tenantId, userId);
                
                if (!result)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Purchase order not found"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true, "Purchase order deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase order {PurchaseOrderId}", id);
                return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("{id}/items")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseOrderItemDto>>>> GetPurchaseOrderItems(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var items = await _procurementService.GetPurchaseOrderItemsAsync(id, tenantId);

                var result = items.Select(item => new PurchaseOrderItemDto
                {
                    Id = item.Id,
                    PurchaseOrderId = item.PurchaseOrderId,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name,
                    ProductBarcode = item.Product?.Barcode,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TotalCost = item.TotalCost,
                    QuantityReceived = item.QuantityReceived,
                    BatchNumber = item.BatchNumber,
                    ExpiryDate = item.ExpiryDate,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                });

                return Ok(ApiResponse<IEnumerable<PurchaseOrderItemDto>>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase order items for {PurchaseOrderId}", id);
                return BadRequest(ApiResponse<IEnumerable<PurchaseOrderItemDto>>.ErrorResult(ex.Message));
            }
        }

        private Guid GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            return tenantIdClaim != null ? Guid.Parse(tenantIdClaim.Value) : Guid.Empty;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
        }

        private Guid? GetBranchId()
        {
            var branchIdClaim = User.FindFirst("branch_id");
            return branchIdClaim != null && Guid.TryParse(branchIdClaim.Value, out var branchId) ? branchId : null;
        }
    }
}
