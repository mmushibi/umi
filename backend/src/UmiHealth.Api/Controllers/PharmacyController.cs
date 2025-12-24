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
    public class PharmacyController : ControllerBase
    {
        private readonly IPharmacyService _pharmacyService;

        public PharmacyController(IPharmacyService pharmacyService)
        {
            _pharmacyService = pharmacyService;
        }

        [HttpGet("settings")]
        public async Task<ActionResult<PharmacySettingsDto>> GetPharmacySettings()
        {
            try
            {
                var tenantId = GetTenantId();
                var settings = await _pharmacyService.GetPharmacySettingsAsync(tenantId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve pharmacy settings." });
            }
        }

        [HttpPut("settings")]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<PharmacySettingsDto>> UpdatePharmacySettings([FromBody] UpdatePharmacySettingsRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var settings = await _pharmacyService.UpdatePharmacySettingsAsync(tenantId, request);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update pharmacy settings." });
            }
        }

        [HttpGet("suppliers")]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
        {
            try
            {
                var tenantId = GetTenantId();
                var suppliers = await _pharmacyService.GetSuppliersAsync(tenantId);
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve suppliers." });
            }
        }

        [HttpPost("suppliers")]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var supplier = await _pharmacyService.CreateSupplierAsync(tenantId, request);
                return CreatedAtAction(nameof(GetSuppliers), new { }, supplier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to create supplier." });
            }
        }

        [HttpGet("suppliers/{id}")]
        public async Task<ActionResult<SupplierDto>> GetSupplier(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var supplier = await _pharmacyService.GetSupplierByIdAsync(tenantId, id);
                if (supplier == null)
                    return NotFound();

                return Ok(supplier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve supplier." });
            }
        }

        [HttpPut("suppliers/{id}")]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<SupplierDto>> UpdateSupplier(Guid id, [FromBody] UpdateSupplierRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var supplier = await _pharmacyService.UpdateSupplierAsync(tenantId, id, request);
                if (supplier == null)
                    return NotFound();

                return Ok(supplier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update supplier." });
            }
        }

        [HttpDelete("suppliers/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> DeleteSupplier(Guid id)
        {
            try
            {
                var tenantId = GetTenantId();
                var success = await _pharmacyService.DeleteSupplierAsync(tenantId, id);
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete supplier." });
            }
        }

        [HttpGet("procurement")]
        public async Task<ActionResult<IEnumerable<ProcurementOrderDto>>> GetProcurementOrders()
        {
            try
            {
                var tenantId = GetTenantId();
                var orders = await _pharmacyService.GetProcurementOrdersAsync(tenantId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve procurement orders." });
            }
        }

        [HttpPost("procurement")]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<ProcurementOrderDto>> CreateProcurementOrder([FromBody] CreateProcurementOrderRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var order = await _pharmacyService.CreateProcurementOrderAsync(tenantId, request);
                return CreatedAtAction(nameof(GetProcurementOrders), new { }, order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to create procurement order." });
            }
        }

        [HttpPost("procurement/{id}/receive")]
        [Authorize(Roles = "admin,pharmacist")]
        public async Task<ActionResult<ProcurementOrderDto>> ReceiveProcurementOrder(Guid id, [FromBody] ReceiveProcurementOrderRequest request)
        {
            try
            {
                var tenantId = GetTenantId();
                var order = await _pharmacyService.ReceiveProcurementOrderAsync(tenantId, id, request);
                if (order == null)
                    return NotFound();

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to receive procurement order." });
            }
        }

        [HttpGet("compliance/reports")]
        public async Task<ActionResult<ComplianceReportDto>> GetComplianceReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var tenantId = GetTenantId();
                var report = await _pharmacyService.GetComplianceReportAsync(tenantId, startDate, endDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to generate compliance report." });
            }
        }

        private Guid GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                throw new UnauthorizedAccessException("Tenant information not found");

            return Guid.Parse(tenantIdClaim);
        }
    }

    public class UpdatePharmacySettingsRequest
    {
        public string? PharmacyName { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public Dictionary<string, object>? OperatingHours { get; set; }
        public Dictionary<string, object>? ZamraSettings { get; set; }
        public Dictionary<string, object>? TaxSettings { get; set; }
    }

    public class CreateSupplierRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public Dictionary<string, object>? PaymentTerms { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSupplierRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public Dictionary<string, object>? PaymentTerms { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateProcurementOrderRequest
    {
        public Guid SupplierId { get; set; }
        public List<ProcurementItemRequest> Items { get; set; } = new();
        public DateTime ExpectedDeliveryDate { get; set; }
        public string? Notes { get; set; }
        public Dictionary<string, object>? DeliveryInstructions { get; set; }
    }

    public class ProcurementItemRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string DosageForm { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? Manufacturer { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
    }

    public class ReceiveProcurementOrderRequest
    {
        public List<ReceiveItemRequest> Items { get; set; } = new();
        public string? ReceivedBy { get; set; }
        public string? Notes { get; set; }
        public DateTime ReceivedDate { get; set; }
    }

    public class ReceiveItemRequest
    {
        public Guid OrderItemId { get; set; }
        public int QuantityReceived { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal ActualUnitCost { get; set; }
        public string? Location { get; set; }
    }
}
