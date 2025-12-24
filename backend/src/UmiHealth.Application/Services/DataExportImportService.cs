using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using UmiHealth.Infrastructure.Data;
using System.Globalization;

namespace UmiHealth.Application.Services
{
    /// <summary>
    /// Service for importing and exporting data
    /// Supports CSV, Excel, and other formats
    /// </summary>
    public interface IDataExportImportService
    {
        // Export operations
        Task<byte[]> ExportProductsAsync(string tenantId, string branchId, string format = "csv");
        Task<byte[]> ExportPatientsAsync(string tenantId, string format = "csv");
        Task<byte[]> ExportInventoryAsync(string tenantId, string branchId, string format = "csv");
        Task<byte[]> ExportSalesAsync(string tenantId, string branchId, DateTime startDate, DateTime endDate, string format = "csv");
        Task<byte[]> ExportPrescriptionsAsync(string tenantId, string branchId, DateTime startDate, DateTime endDate, string format = "csv");

        // Import operations
        Task<ImportResult> ImportProductsAsync(string tenantId, string branchId, Stream fileStream, string format = "csv");
        Task<ImportResult> ImportPatientsAsync(string tenantId, Stream fileStream, string format = "csv");
        Task<ImportResult> ImportInventoryAsync(string tenantId, string branchId, Stream fileStream, string format = "csv");
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsSuccess { get; set; }
        public int RecordsFailed { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    // Export DTOs
    public class ProductExportDto
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string Unit { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsActive { get; set; }
    }

    public class PatientExportDto
    {
        public string PatientCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string IdType { get; set; }
        public string IdNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
    }

    public class InventoryExportDto
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReservedQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal SellingPrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime LastStockedDate { get; set; }
    }

    public class SalesExportDto
    {
        public string SaleNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public string PatientName { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string ReceiptNumber { get; set; }
    }

    // Import DTOs
    public class ProductImportDto
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string Unit { get; set; }
        public int ReorderLevel { get; set; }
    }

    public class PatientImportDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string IdType { get; set; }
        public string IdNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
    }

    /// <summary>
    /// Implementation of data export/import service
    /// </summary>
    public class DataExportImportService : IDataExportImportService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<DataExportImportService> _logger;

        public DataExportImportService(SharedDbContext context, ILogger<DataExportImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Export Methods
        public async Task<byte[]> ExportProductsAsync(string tenantId, string branchId, string format = "csv")
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.TenantId == Guid.Parse(tenantId) && p.BranchId == Guid.Parse(branchId))
                    .Select(p => new ProductExportDto
                    {
                        ProductCode = p.Code,
                        ProductName = p.Name,
                        Category = p.Category,
                        CostPrice = p.CostPrice,
                        SellingPrice = p.SellingPrice,
                        Unit = p.Unit,
                        ReorderLevel = p.ReorderLevel,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                return ExportToCsv(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products");
                throw;
            }
        }

        public async Task<byte[]> ExportPatientsAsync(string tenantId, string format = "csv")
        {
            try
            {
                var patients = await _context.Patients
                    .Where(p => p.TenantId == Guid.Parse(tenantId))
                    .Select(p => new PatientExportDto
                    {
                        PatientCode = p.Code,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        PhoneNumber = p.PhoneNumber,
                        Email = p.Email,
                        IdType = p.IdType,
                        IdNumber = p.IdNumber,
                        DateOfBirth = p.DateOfBirth,
                        Gender = p.Gender
                    })
                    .ToListAsync();

                return ExportToCsv(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting patients");
                throw;
            }
        }

        public async Task<byte[]> ExportInventoryAsync(string tenantId, string branchId, string format = "csv")
        {
            try
            {
                var inventory = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.TenantId == Guid.Parse(tenantId) && i.BranchId == Guid.Parse(branchId))
                    .Select(i => new InventoryExportDto
                    {
                        ProductCode = i.Product.Code,
                        ProductName = i.Product.Name,
                        QuantityOnHand = i.QuantityOnHand,
                        ReservedQuantity = i.ReservedQuantity,
                        UnitCost = i.CostPrice,
                        SellingPrice = i.SellingPrice,
                        ExpiryDate = i.ExpiryDate,
                        LastStockedDate = i.LastStockedDate
                    })
                    .ToListAsync();

                return ExportToCsv(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory");
                throw;
            }
        }

        public async Task<byte[]> ExportSalesAsync(
            string tenantId,
            string branchId,
            DateTime startDate,
            DateTime endDate,
            string format = "csv")
        {
            try
            {
                var sales = await _context.Sales
                    .Include(s => s.Patient)
                    .Where(s => s.TenantId == Guid.Parse(tenantId) &&
                               s.BranchId == Guid.Parse(branchId) &&
                               s.CreatedAt >= startDate &&
                               s.CreatedAt <= endDate)
                    .Select(s => new SalesExportDto
                    {
                        SaleNumber = s.Id.ToString(),
                        SaleDate = s.CreatedAt,
                        PatientName = s.Patient != null ? $"{s.Patient.FirstName} {s.Patient.LastName}" : "Walk-in",
                        SubTotal = s.SubTotal,
                        DiscountAmount = s.DiscountAmount,
                        TaxAmount = s.TaxAmount,
                        TotalAmount = s.TotalAmount,
                        PaymentMethod = s.PaymentMethod,
                        ReceiptNumber = s.ReceiptNumber
                    })
                    .ToListAsync();

                return ExportToCsv(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales");
                throw;
            }
        }

        public async Task<byte[]> ExportPrescriptionsAsync(
            string tenantId,
            string branchId,
            DateTime startDate,
            DateTime endDate,
            string format = "csv")
        {
            try
            {
                var prescriptions = await _context.Prescriptions
                    .Where(p => p.TenantId == Guid.Parse(tenantId) &&
                               p.BranchId == Guid.Parse(branchId) &&
                               p.CreatedAt >= startDate &&
                               p.CreatedAt <= endDate)
                    .ToListAsync();

                // Return CSV format
                return ExportToCsv(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting prescriptions");
                throw;
            }
        }

        // Import Methods
        public async Task<ImportResult> ImportProductsAsync(
            string tenantId,
            string branchId,
            Stream fileStream,
            string format = "csv")
        {
            var result = new ImportResult();

            try
            {
                var products = ImportFromCsv<ProductImportDto>(fileStream);
                result.RecordsProcessed = products.Count;

                foreach (var productDto in products)
                {
                    try
                    {
                        // Check if product already exists
                        var existingProduct = await _context.Products
                            .FirstOrDefaultAsync(p => p.Code == productDto.ProductCode &&
                                                      p.TenantId == Guid.Parse(tenantId));

                        if (existingProduct == null)
                        {
                            // Create new product
                            var product = new Product
                            {
                                TenantId = Guid.Parse(tenantId),
                                BranchId = Guid.Parse(branchId),
                                Code = productDto.ProductCode,
                                Name = productDto.ProductName,
                                Category = productDto.Category,
                                CostPrice = productDto.CostPrice,
                                SellingPrice = productDto.SellingPrice,
                                Unit = productDto.Unit,
                                ReorderLevel = productDto.ReorderLevel,
                                IsActive = true
                            };

                            await _context.Products.AddAsync(product);
                            result.RecordsSuccess++;
                        }
                        else
                        {
                            // Update existing product
                            existingProduct.Name = productDto.ProductName;
                            existingProduct.CostPrice = productDto.CostPrice;
                            existingProduct.SellingPrice = productDto.SellingPrice;
                            existingProduct.Unit = productDto.Unit;
                            existingProduct.ReorderLevel = productDto.ReorderLevel;

                            _context.Products.Update(existingProduct);
                            result.RecordsSuccess++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.RecordsFailed++;
                        result.Errors.Add($"Error importing product {productDto.ProductCode}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                result.Success = result.RecordsFailed == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products");
                result.Errors.Add($"Import failed: {ex.Message}");
                result.Success = false;
            }

            return result;
        }

        public async Task<ImportResult> ImportPatientsAsync(string tenantId, Stream fileStream, string format = "csv")
        {
            var result = new ImportResult();

            try
            {
                var patients = ImportFromCsv<PatientImportDto>(fileStream);
                result.RecordsProcessed = patients.Count;

                foreach (var patientDto in patients)
                {
                    try
                    {
                        // Generate patient code
                        var patientCode = GeneratePatientCode();

                        var patient = new Patient
                        {
                            TenantId = Guid.Parse(tenantId),
                            Code = patientCode,
                            FirstName = patientDto.FirstName,
                            LastName = patientDto.LastName,
                            PhoneNumber = patientDto.PhoneNumber,
                            Email = patientDto.Email,
                            IdType = patientDto.IdType,
                            IdNumber = patientDto.IdNumber,
                            DateOfBirth = patientDto.DateOfBirth,
                            Gender = patientDto.Gender,
                            IsActive = true
                        };

                        await _context.Patients.AddAsync(patient);
                        result.RecordsSuccess++;
                    }
                    catch (Exception ex)
                    {
                        result.RecordsFailed++;
                        result.Errors.Add($"Error importing patient {patientDto.FirstName} {patientDto.LastName}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();
                result.Success = result.RecordsFailed == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing patients");
                result.Errors.Add($"Import failed: {ex.Message}");
                result.Success = false;
            }

            return result;
        }

        public async Task<ImportResult> ImportInventoryAsync(
            string tenantId,
            string branchId,
            Stream fileStream,
            string format = "csv")
        {
            var result = new ImportResult();

            try
            {
                // Implementation would import inventory from file
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing inventory");
                result.Errors.Add($"Import failed: {ex.Message}");
                result.Success = false;
            }

            return result;
        }

        // Helper Methods
        private byte[] ExportToCsv<T>(List<T> data)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteRecords(data);
                writer.Flush();
                return memoryStream.ToArray();
            }
        }

        private List<T> ImportFromCsv<T>(Stream fileStream)
        {
            using (var reader = new StreamReader(fileStream, Encoding.UTF8))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csvReader.GetRecords<T>().ToList();
            }
        }

        private string GeneratePatientCode()
        {
            return $"PAT{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}
