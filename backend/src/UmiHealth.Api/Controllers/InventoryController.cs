using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealth.Application.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] InventoryQueryParameters parameters)
        {
            try
            {
                var tenantId = GetTenantId();
                var products = await _inventoryService.GetProductsAsync(tenantId, parameters);
                
                return Ok(new ProductListResponse
                {
                    Products = products,
                    TotalCount = products.Count,
                    Page = parameters.Page ?? 1,
                    PageSize = parameters.PageSize ?? 20
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve products." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var product = await _inventoryService.GetProductByIdAsync(tenantId, id);
                
                if (product == null)
                    return NotFound();

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve product." });
            }
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var product = await _inventoryService.CreateProductAsync(tenantId, userId, request);
                
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to create product." });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var product = await _inventoryService.UpdateProductAsync(tenantId, id, userId, request);
                
                if (product == null)
                    return NotFound();

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update product." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var success = await _inventoryService.DeleteProductAsync(tenantId, id, userId);
                
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete product." });
            }
        }

        [HttpPost("bulk-upload")]
        public async Task<ActionResult<BulkUploadResponse>> BulkUploadProducts([FromBody] BulkUploadRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var result = await _inventoryService.BulkUploadProductsAsync(tenantId, userId, request.Products);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to upload products." });
            }
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts()
        {
            try
            {
                var tenantId = GetTenantId();
                var products = await _inventoryService.GetLowStockProductsAsync(tenantId);
                
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve low stock products." });
            }
        }

        [HttpGet("expiring")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetExpiringProducts([FromQuery] int days = 30)
        {
            try
            {
                var tenantId = GetTenantId();
                var products = await _inventoryService.GetExpiringProductsAsync(tenantId, days);
                
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve expiring products." });
            }
        }

        [HttpPost("{id}/adjust-stock")]
        public async Task<ActionResult<ProductDto>> AdjustStock(Guid id, [FromBody] AdjustStockRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var userId = GetUserId();

                var product = await _inventoryService.AdjustStockAsync(tenantId, id, userId, request);
                
                if (product == null)
                    return NotFound();

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to adjust stock." });
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            try
            {
                var tenantId = GetTenantId();
                var categories = await _inventoryService.GetCategoriesAsync(tenantId);
                
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve categories." });
            }
        }

        [HttpGet("suppliers")]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
        {
            try
            {
                var tenantId = GetTenantId();
                var suppliers = await _inventoryService.GetSuppliersAsync(tenantId);
                
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve suppliers." });
            }
        }

        private Guid GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                throw new UnauthorizedAccessException("Tenant information not found");
            
            return Guid.Parse(tenantIdClaim);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User information not found");
            
            return Guid.Parse(userIdClaim);
        }
    }

    public class InventoryQueryParameters
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? Search { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public string? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public bool RequiresPrescription { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }

    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? Supplier { get; set; }
        public string? Sku { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public int? MinStockLevel { get; set; }
        public int? MaxStockLevel { get; set; }
        public string? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public bool? RequiresPrescription { get; set; }
        public bool? IsActive { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }

    public class AdjustStockRequest
    {
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Type { get; set; } = "adjustment"; // adjustment, sale, return, damage
    }

    public class BulkUploadRequest
    {
        public List<CreateProductRequest> Products { get; set; } = new();
    }

    public class ProductListResponse
    {
        public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class BulkUploadResponse
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<ProductDto> CreatedProducts { get; set; } = new();
    }

    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public string? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public bool RequiresPrescription { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? AdditionalInfo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class SupplierDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
