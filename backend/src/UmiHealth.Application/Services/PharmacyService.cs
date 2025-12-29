using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Application.DTOs;
using UmiHealth.Application.DTOs.Pharmacy;
using UmiHealth.Domain.Entities;
using UmiHealth.Core.Entities;
using UmiHealth.Persistence.Data;

namespace UmiHealth.Application.Services
{
    public class PharmacyService : IPharmacyService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<PharmacyService> _logger;

        public PharmacyService(SharedDbContext context, ILogger<PharmacyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PharmacySettingsDto> GetPharmacySettingsAsync(Guid tenantId)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                throw new KeyNotFoundException($"Tenant {tenantId} not found");

            var mainBranch = await _context.Branches
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.IsMainBranch);

            return new PharmacySettingsDto
            {
                TenantId = tenantId,
                PharmacyName = tenant.Name,
                LicenseNumber = mainBranch?.LicenseNumber,
                Address = mainBranch?.Address,
                Phone = mainBranch?.Phone,
                Email = mainBranch?.Email,
                OperatingHours = mainBranch?.OperatingHours ?? new Dictionary<string, object>(),
                ZamraSettings = tenant.ComplianceSettings?.ContainsKey("zamra") == true 
                    ? (Dictionary<string, object>)tenant.ComplianceSettings["zamra"] 
                    : new Dictionary<string, object>(),
                TaxSettings = tenant.ComplianceSettings?.ContainsKey("tax") == true 
                    ? (Dictionary<string, object>)tenant.ComplianceSettings["tax"] 
                    : new Dictionary<string, object>()
            };
        }

        public async Task<PharmacySettingsDto> UpdatePharmacySettingsAsync(Guid tenantId, UpdatePharmacySettingsRequest request)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                throw new KeyNotFoundException($"Tenant {tenantId} not found");

            var mainBranch = await _context.Branches
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.IsMainBranch);

            // Update tenant settings
            if (!string.IsNullOrEmpty(request.PharmacyName))
                tenant.Name = request.PharmacyName;

            if (request.ZamraSettings != null)
            {
                tenant.ComplianceSettings ??= new Dictionary<string, object>();
                tenant.ComplianceSettings["zamra"] = request.ZamraSettings;
            }

            if (request.TaxSettings != null)
            {
                tenant.ComplianceSettings ??= new Dictionary<string, object>();
                tenant.ComplianceSettings["tax"] = request.TaxSettings;
            }

            // Update main branch settings
            if (mainBranch != null)
            {
                if (!string.IsNullOrEmpty(request.LicenseNumber))
                    mainBranch.LicenseNumber = request.LicenseNumber;

                if (!string.IsNullOrEmpty(request.Address))
                    mainBranch.Address = request.Address;

                if (!string.IsNullOrEmpty(request.Phone))
                    mainBranch.Phone = request.Phone;

                if (!string.IsNullOrEmpty(request.Email))
                    mainBranch.Email = request.Email;

                if (request.OperatingHours != null)
                    mainBranch.OperatingHours = request.OperatingHours;

                _context.Branches.Update(mainBranch);
            }

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            return await GetPharmacySettingsAsync(tenantId);
        }

        public async Task<IEnumerable<SupplierDto>> GetSuppliersAsync(Guid tenantId)
        {
            var suppliers = await _context.Set<UmiHealth.Core.Entities.Supplier>()
                .Where(s => s.TenantId == tenantId && s.DeletedAt == null)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return suppliers.Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                LicenseNumber = s.LicenseNumber,
                PaymentTerms = s.PaymentTerms ?? new Dictionary<string, object>(),
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });
        }

        public async Task<SupplierDto> GetSupplierByIdAsync(Guid tenantId, Guid supplierId)
        {
            var supplier = await _context.Set<UmiHealth.Core.Entities.Supplier>()
                .FirstOrDefaultAsync(s => s.Id == supplierId && s.TenantId == tenantId && s.DeletedAt == null);

            if (supplier == null)
                return null;

            return new SupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                LicenseNumber = supplier.LicenseNumber,
                PaymentTerms = supplier.PaymentTerms ?? new Dictionary<string, object>(),
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt,
                UpdatedAt = supplier.UpdatedAt
            };
        }

        public async Task<SupplierDto> CreateSupplierAsync(Guid tenantId, CreateSupplierRequest request)
        {
            var supplier = new UmiHealth.Core.Entities.Supplier
            {
                TenantId = tenantId,
                Name = request.Name,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                LicenseNumber = request.LicenseNumber,
                PaymentTerms = request.PaymentTerms,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<UmiHealth.Core.Entities.Supplier>().Add(supplier);
            await _context.SaveChangesAsync();

            return await GetSupplierByIdAsync(tenantId, supplier.Id);
        }

        public async Task<SupplierDto> UpdateSupplierAsync(Guid tenantId, Guid supplierId, UpdateSupplierRequest request)
        {
            var supplier = await _context.Set<UmiHealth.Core.Entities.Supplier>()
                .FirstOrDefaultAsync(s => s.Id == supplierId && s.TenantId == tenantId && s.DeletedAt == null);

            if (supplier == null)
                return null;

            supplier.Name = request.Name;
            supplier.ContactPerson = request.ContactPerson;
            supplier.Phone = request.Phone;
            supplier.Email = request.Email;
            supplier.Address = request.Address;
            supplier.LicenseNumber = request.LicenseNumber;
            supplier.PaymentTerms = request.PaymentTerms;
            supplier.IsActive = request.IsActive;
            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Set<UmiHealth.Core.Entities.Supplier>().Update(supplier);
            await _context.SaveChangesAsync();

            return await GetSupplierByIdAsync(tenantId, supplierId);
        }

        public async Task<bool> DeleteSupplierAsync(Guid tenantId, Guid supplierId)
        {
            var supplier = await _context.Set<UmiHealth.Core.Entities.Supplier>()
                .FirstOrDefaultAsync(s => s.Id == supplierId && s.TenantId == tenantId && s.DeletedAt == null);

            if (supplier == null)
                return false;

            supplier.DeletedAt = DateTime.UtcNow;
            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Set<UmiHealth.Core.Entities.Supplier>().Update(supplier);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<ProcurementOrderDto>> GetProcurementOrdersAsync(Guid tenantId)
        {
            var orders = await _context.ProcurementOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items)
                .Where(o => o.TenantId == tenantId && o.DeletedAt == null)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => new ProcurementOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                SupplierId = o.SupplierId,
                SupplierName = o.Supplier?.Name,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                ActualDeliveryDate = o.ActualDeliveryDate,
                Notes = o.Notes,
                Items = o.Items.Select(i => new ProcurementItemDto
                {
                    Id = i.Id,
                    ProductName = i.ProductName,
                    GenericName = i.GenericName,
                    Strength = i.Strength,
                    DosageForm = i.DosageForm,
                    QuantityOrdered = i.QuantityOrdered,
                    QuantityReceived = i.QuantityReceived,
                    UnitCost = i.UnitCost,
                    TotalCost = i.QuantityOrdered * i.UnitCost,
                    Manufacturer = i.Manufacturer,
                    BatchNumber = i.BatchNumber,
                    ExpiryDate = i.ExpiryDate
                }).ToList(),
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToList();
        }

        public async Task<ProcurementOrderDto> CreateProcurementOrderAsync(Guid tenantId, CreateProcurementOrderRequest request)
        {
            var order = new ProcurementOrder
            {
                TenantId = tenantId,
                SupplierId = request.SupplierId,
                OrderNumber = await GenerateOrderNumberAsync(tenantId),
                Status = "pending",
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                Notes = request.Notes,
                DeliveryInstructions = request.DeliveryInstructions,
                Items = request.Items.Select(i => new ProcurementOrderItem
                {
                    ProductName = i.ProductName,
                    GenericName = i.GenericName,
                    Strength = i.Strength,
                    DosageForm = i.DosageForm,
                    QuantityOrdered = i.Quantity,
                    QuantityReceived = 0,
                    UnitCost = i.UnitCost,
                    Manufacturer = i.Manufacturer,
                    ExpiryDate = i.ExpiryDate,
                    BatchNumber = i.BatchNumber
                }).ToList(),
                TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitCost),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProcurementOrders.Add(order);
            await _context.SaveChangesAsync();

            var orders = await GetProcurementOrdersAsync(tenantId);
            return orders.FirstOrDefault(o => o.Id == order.Id);
        }

        public async Task<ProcurementOrderDto> ReceiveProcurementOrderAsync(Guid tenantId, Guid orderId, ReceiveProcurementOrderRequest request)
        {
            var order = await _context.ProcurementOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId && o.DeletedAt == null);

            if (order == null)
                return null;

            // Update order status and delivery information
            order.Status = "received";
            order.ActualDeliveryDate = request.ReceivedDate;
            order.Notes = request.Notes;
            order.UpdatedAt = DateTime.UtcNow;

            // Update received quantities for items
            foreach (var receiveItem in request.Items)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == receiveItem.OrderItemId);
                if (orderItem != null)
                {
                    orderItem.QuantityReceived = receiveItem.ReceivedQuantity;
                    orderItem.ActualUnitCost = receiveItem.ActualUnitCost;
                    orderItem.BatchNumber = receiveItem.BatchNumber;
                    orderItem.ExpiryDate = receiveItem.ExpiryDate;

                    // Add to inventory
                    await AddToInventoryAsync(tenantId, orderItem, receiveItem);
                }
            }

            _context.ProcurementOrders.Update(order);
            await _context.SaveChangesAsync();

            var orders = await GetProcurementOrdersAsync(tenantId);
            return orders.FirstOrDefault(o => o.Id == orderId);
        }

        public async Task<ComplianceReportDto> GetComplianceReportAsync(Guid tenantId, DateTime startDate, DateTime endDate)
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Items)
                .Where(p => p.TenantId == tenantId && 
                           p.CreatedAt >= startDate && 
                           p.CreatedAt <= endDate)
                .ToListAsync();

            var controlledSubstances = await _context.Products
                .Where(p => p.TenantId == tenantId && p.IsControlledSubstance && p.DeletedAt == null)
                .ToListAsync();

            var expiringProducts = await _context.Inventory
                .Include(i => i.Product)
                .Where(i => i.TenantId == tenantId && 
                           i.ExpiryDate.HasValue && 
                           i.ExpiryDate.Value <= DateTime.UtcNow.AddDays(90))
                .ToListAsync();

            return new ComplianceReportDto
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                TotalPrescriptions = prescriptions.Count,
                ControlledSubstanceTransactions = prescriptions
                    .SelectMany(p => p.Items)
                    .Count(pi => controlledSubstances.Any(cs => cs.Id == pi.ProductId)),
                ExpiringProductsCount = expiringProducts.Count,
                ExpiringProducts = expiringProducts.Select(i => new ExpiringProductDto
                {
                    ProductName = i.Product?.Name,
                    BatchNumber = i.BatchNumber,
                    ExpiryDate = i.ExpiryDate,
                    Quantity = i.QuantityOnHand,
                    BranchId = i.BranchId
                }).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }

        private async Task<string> GenerateOrderNumberAsync(Guid tenantId)
        {
            var year = DateTime.UtcNow.Year;
            var orderCount = await _context.ProcurementOrders
                .CountAsync(o => o.TenantId == tenantId && o.CreatedAt.Year == year);

            return $"PO{year}{(orderCount + 1).ToString("D4")}";
        }

        private async Task AddToInventoryAsync(Guid tenantId, ProcurementOrderItem orderItem, ReceiveItemRequest receiveItem)
        {
            // Find or create product
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && 
                                       p.Name == orderItem.ProductName && 
                                       p.Strength == orderItem.Strength &&
                                       p.DosageForm == orderItem.DosageForm);

            if (product == null)
            {
                product = new UmiHealth.Core.Entities.Product
                {
                    TenantId = tenantId,
                    Name = orderItem.ProductName,
                    GenericName = orderItem.GenericName,
                    Strength = orderItem.Strength,
                    DosageForm = orderItem.DosageForm,
                    Manufacturer = orderItem.Manufacturer,
                    UnitCost = orderItem.ActualUnitCost,
                    SellingPrice = orderItem.ActualUnitCost * 1.3m, // 30% markup
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            // Add to inventory (assuming main branch for now)
            var mainBranch = await _context.Branches
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.IsMainBranch);

            if (mainBranch != null)
            {
                var inventory = new UmiHealth.Core.Entities.Inventory
                {
                    TenantId = tenantId,
                    BranchId = mainBranch.Id,
                    ProductId = product.Id,
                    QuantityOnHand = receiveItem.ReceivedQuantity,
                    QuantityReserved = 0,
                    BatchNumber = receiveItem.BatchNumber,
                    ExpiryDate = receiveItem.ExpiryDate,
                    CostPrice = receiveItem.ActualUnitCost,
                    SellingPrice = product.SellingPrice,
                    Location = "Main Store", // Default location since not in DTO
                    LastStockUpdate = DateTime.UtcNow
                };

                _context.Inventories.Add(inventory);
                await _context.SaveChangesAsync();
            }
        }
    }
}
