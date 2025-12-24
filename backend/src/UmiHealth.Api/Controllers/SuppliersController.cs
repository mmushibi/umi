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
    public class SuppliersController : ControllerBase
    {
        private readonly IProcurementService _procurementService;
        private readonly ILogger<SuppliersController> _logger;

        public SuppliersController(IProcurementService procurementService, ILogger<SuppliersController> logger)
        {
            _procurementService = procurementService;
            _logger = logger;
        }

        [HttpGet]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<PagedResponse<SupplierDto>>>> GetSuppliers(
            [FromQuery] SupplierFilterDto filter,
            [FromQuery] PagedRequest pagedRequest)
        {
            try
            {
                var tenantId = GetTenantId();
                var suppliers = await _procurementService.GetSuppliersAsync(tenantId, new(
                    filter.Search,
                    filter.IsActive,
                    filter.City,
                    filter.Country
                ));

                var pagedResponse = PagedResponse<SupplierDto>.Create(
                    suppliers.Select(s => new SupplierDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ContactPerson = s.ContactPerson,
                        Email = s.Email,
                        Phone = s.Phone,
                        Address = s.Address,
                        City = s.City,
                        Country = s.Country,
                        PostalCode = s.PostalCode,
                        TaxId = s.TaxId,
                        PaymentTerms = s.PaymentTerms,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    }),
                    pagedRequest.PageNumber,
                    pagedRequest.PageSize,
                    suppliers.Count
                );

                return Ok(ApiResponse<PagedResponse<SupplierDto>>.SuccessResult(pagedResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers");
                return BadRequest(ApiResponse<PagedResponse<SupplierDto>>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("{id}")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplier(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var supplier = await _procurementService.GetSupplierAsync(id, tenantId);
                
                if (supplier == null)
                {
                    return NotFound(ApiResponse<SupplierDto>.ErrorResult("Supplier not found"));
                }

                var supplierDto = new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    City = supplier.City,
                    Country = supplier.Country,
                    PostalCode = supplier.PostalCode,
                    TaxId = supplier.TaxId,
                    PaymentTerms = supplier.PaymentTerms,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt,
                    UpdatedAt = supplier.UpdatedAt
                };

                return Ok(ApiResponse<SupplierDto>.SuccessResult(supplierDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
                return BadRequest(ApiResponse<SupplierDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPost]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier([FromBody] CreateSupplierDto request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var supplier = await _procurementService.CreateSupplierAsync(tenantId, userId, new(
                    request.Name,
                    request.ContactPerson,
                    request.Email,
                    request.Phone,
                    request.Address,
                    request.City,
                    request.Country,
                    request.PostalCode,
                    request.TaxId,
                    request.PaymentTerms
                ));

                var supplierDto = new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    City = supplier.City,
                    Country = supplier.Country,
                    PostalCode = supplier.PostalCode,
                    TaxId = supplier.TaxId,
                    PaymentTerms = supplier.PaymentTerms,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt,
                    UpdatedAt = supplier.UpdatedAt
                };

                return Ok(ApiResponse<SupplierDto>.SuccessResult(supplierDto, "Supplier created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return BadRequest(ApiResponse<SupplierDto>.ErrorResult(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<SupplierDto>>> UpdateSupplier(Guid id, [FromBody] UpdateSupplierDto request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var supplier = await _procurementService.UpdateSupplierAsync(id, tenantId, userId, new(
                    request.Name,
                    request.ContactPerson,
                    request.Email,
                    request.Phone,
                    request.Address,
                    request.City,
                    request.Country,
                    request.PostalCode,
                    request.TaxId,
                    request.PaymentTerms,
                    request.IsActive
                ));

                var supplierDto = new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    City = supplier.City,
                    Country = supplier.Country,
                    PostalCode = supplier.PostalCode,
                    TaxId = supplier.TaxId,
                    PaymentTerms = supplier.PaymentTerms,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt,
                    UpdatedAt = supplier.UpdatedAt
                };

                return Ok(ApiResponse<SupplierDto>.SuccessResult(supplierDto, "Supplier updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
                return BadRequest(ApiResponse<SupplierDto>.ErrorResult(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [EnableRateLimiting("Write")]
        [Authorize(Policy = "RequirePharmacistRole")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSupplier(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var result = await _procurementService.DeleteSupplierAsync(id, tenantId, userId);
                
                if (!result)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Supplier not found"));
                }

                return Ok(ApiResponse<bool>.SuccessResult(true, "Supplier deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
                return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
            }
        }

        [HttpGet("{id}/purchase-orders")]
        [EnableRateLimiting("Read")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PurchaseOrderDto>>>> GetSupplierPurchaseOrders(
            Guid id,
            [FromQuery] PurchaseOrderFilterDto filter,
            [FromQuery] PagedRequest pagedRequest)
        {
            try
            {
                var tenantId = GetTenantId();
                var purchaseOrders = await _procurementService.GetPurchaseOrdersBySupplierAsync(tenantId, id, new(
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
                _logger.LogError(ex, "Error retrieving purchase orders for supplier {SupplierId}", id);
                return BadRequest(ApiResponse<PagedResponse<PurchaseOrderDto>>.ErrorResult(ex.Message));
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
    }
}
