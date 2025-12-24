using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UmiHealth.Core.Interfaces;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.API.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IPharmacyService _pharmacyService;

    public ProductsController(IPharmacyService pharmacyService)
    {
        _pharmacyService = pharmacyService;
    }

    [HttpPost]
    [EnableRateLimiting("Write")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto request)
    {
        try
        {
            var tenantId = GetTenantId();
            var product = await _pharmacyService.CreateProductAsync(tenantId, new(
                request.Name,
                request.Description,
                request.GenericName,
                request.Brand,
                request.Category,
                request.DosageForm,
                request.Strength,
                request.Manufacturer,
                request.NdcCode,
                request.Barcode,
                request.RequiresPrescription,
                request.IsControlledSubstance,
                request.StorageConditions,
                request.UnitPrice,
                request.ImageUrl
            ));

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                GenericName = product.GenericName,
                Brand = product.Brand,
                Category = product.Category,
                DosageForm = product.DosageForm,
                Strength = product.Strength,
                Manufacturer = product.Manufacturer,
                NdcCode = product.NdcCode,
                Barcode = product.Barcode,
                RequiresPrescription = product.RequiresPrescription,
                IsControlledSubstance = product.IsControlledSubstance,
                StorageConditions = product.StorageConditions,
                UnitPrice = product.UnitPrice,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Ok(ApiResponse<ProductDto>.SuccessResult(productDto, "Product created successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProductDto>.ErrorResult(ex.Message));
        }
    }

    [HttpGet]
    [EnableRateLimiting("Read")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProductDto>>>> ListProducts(
        [FromQuery] ProductFilterDto filter,
        [FromQuery] PagedRequest pagedRequest)
    {
        try
        {
            var tenantId = GetTenantId();
            var products = await _pharmacyService.GetProductsAsync(tenantId, new(
                filter.Category,
                filter.Brand,
                filter.RequiresPrescription,
                filter.IsActive,
                filter.Search
            ));

            var pagedResponse = PagedResponse<ProductDto>.Create(
                products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    GenericName = p.GenericName,
                    Brand = p.Brand,
                    Category = p.Category,
                    DosageForm = p.DosageForm,
                    Strength = p.Strength,
                    Manufacturer = p.Manufacturer,
                    NdcCode = p.NdcCode,
                    Barcode = p.Barcode,
                    RequiresPrescription = p.RequiresPrescription,
                    IsControlledSubstance = p.IsControlledSubstance,
                    StorageConditions = p.StorageConditions,
                    UnitPrice = p.UnitPrice,
                    ImageUrl = p.ImageUrl,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }),
                pagedRequest.PageNumber,
                pagedRequest.PageSize,
                products.Count
            );

            return Ok(ApiResponse<PagedResponse<ProductDto>>.SuccessResult(pagedResponse));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PagedResponse<ProductDto>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("{id}")]
    [EnableRateLimiting("Read")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var product = await _pharmacyService.GetProductAsync(id, tenantId);
            if (product == null)
            {
                return NotFound(ApiResponse<ProductDto>.ErrorResult("Product not found"));
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                GenericName = product.GenericName,
                Brand = product.Brand,
                Category = product.Category,
                DosageForm = product.DosageForm,
                Strength = product.Strength,
                Manufacturer = product.Manufacturer,
                NdcCode = product.NdcCode,
                Barcode = product.Barcode,
                RequiresPrescription = product.RequiresPrescription,
                IsControlledSubstance = product.IsControlledSubstance,
                StorageConditions = product.StorageConditions,
                UnitPrice = product.UnitPrice,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Ok(ApiResponse<ProductDto>.SuccessResult(productDto));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProductDto>.ErrorResult(ex.Message));
        }
    }

    [HttpPut("{id}")]
    [EnableRateLimiting("Write")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] UpdateProductDto request)
    {
        try
        {
            var product = await _pharmacyService.UpdateProductAsync(id, new(
                request.Name,
                request.Description,
                request.GenericName,
                request.Brand,
                request.Category,
                request.DosageForm,
                request.Strength,
                request.Manufacturer,
                request.RequiresPrescription,
                request.IsControlledSubstance,
                request.StorageConditions,
                request.UnitPrice,
                request.ImageUrl
            ));

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                GenericName = product.GenericName,
                Brand = product.Brand,
                Category = product.Category,
                DosageForm = product.DosageForm,
                Strength = product.Strength,
                Manufacturer = product.Manufacturer,
                NdcCode = product.NdcCode,
                Barcode = product.Barcode,
                RequiresPrescription = product.RequiresPrescription,
                IsControlledSubstance = product.IsControlledSubstance,
                StorageConditions = product.StorageConditions,
                UnitPrice = product.UnitPrice,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Ok(ApiResponse<ProductDto>.SuccessResult(productDto, "Product updated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProductDto>.ErrorResult(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    [EnableRateLimiting("Write")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(Guid id)
    {
        try
        {
            var result = await _pharmacyService.DeleteProductAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Product not found"));
            }

            return Ok(ApiResponse<bool>.SuccessResult(true, "Product deleted successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
        }
    }

    private Guid GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("tenant_id");
        return tenantIdClaim != null ? Guid.Parse(tenantIdClaim.Value) : Guid.Empty;
    }
}
