using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Persistence.Data;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IReceiptService
    {
        Task<byte[]> GenerateSaleReceiptAsync(Guid saleId, ReceiptOptions options = null);
        Task<byte[]> GeneratePaymentReceiptAsync(Guid paymentId, ReceiptOptions options = null);
        Task<byte[]> GeneratePrescriptionReceiptAsync(Guid prescriptionId, ReceiptOptions options = null);
        Task<byte[]> GenerateRefundReceiptAsync(Guid refundId, ReceiptOptions options = null);
        Task<byte[]> GenerateCustomReceiptAsync(ReceiptTemplate template, ReceiptData data);
        Task<ReceiptTemplate> GetReceiptTemplateAsync(Guid tenantId, string receiptType);
        Task<ReceiptTemplate> UpdateReceiptTemplateAsync(Guid tenantId, ReceiptTemplate template);
        Task<List<ReceiptHistoryDto>> GetReceiptHistoryAsync(Guid tenantId, Guid? entityId = null, string? receiptType = null);
    }

    public class ReceiptService : IReceiptService
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<ReceiptService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPdfGenerator _pdfGenerator;

        public ReceiptService(
            SharedDbContext context,
            ILogger<ReceiptService> logger,
            IConfiguration configuration,
            IPdfGenerator pdfGenerator)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _pdfGenerator = pdfGenerator;
        }

        public async Task<byte[]> GenerateSaleReceiptAsync(Guid saleId, ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating sale receipt for sale {SaleId}", saleId);

                var sale = await _context.Sales
                    .Include(s => s.Items)
                    .Include(s => s.Patient)
                    .Include(s => s.Cashier)
                    .FirstOrDefaultAsync(s => s.Id == saleId);

                if (sale == null)
                {
                    throw new ArgumentException($"Sale not found: {saleId}");
                }

                var payments = await _context.Payments
                    .Where(p => p.SaleId == saleId && p.Status == "completed")
                    .ToListAsync();

                var receiptData = new ReceiptData
                {
                    ReceiptType = "sale",
                    ReceiptNumber = sale.SaleNumber,
                    Date = sale.SaleDate,
                    TenantInfo = await GetTenantInfoAsync(sale.BranchId),
                    BranchInfo = await GetBranchInfoAsync(sale.BranchId),
                    CustomerInfo = sale.Patient != null ? new CustomerInfo
                    {
                        Name = $"{sale.Patient.FirstName} {sale.Patient.LastName}",
                        Email = sale.Patient.Email,
                        Phone = sale.Patient.PhoneNumber,
                        Address = sale.Patient.Address
                    } : null,
                    Items = sale.Items.Select(item => new ReceiptItem
                    {
                        Name = item.Product?.Name ?? "Unknown Product",
                        Code = item.Product?.Sku ?? "",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Discount = item.DiscountAmount,
                        TotalPrice = item.TotalPrice,
                        Category = item.Product?.Category ?? ""
                    }).ToList(),
                    Payments = payments.Select(p => new PaymentInfo
                    {
                        Method = p.PaymentMethod,
                        Amount = p.Amount,
                        Reference = p.ReferenceNumber,
                        Status = p.Status
                    }).ToList(),
                    Summary = new ReceiptSummary
                    {
                        Subtotal = sale.Subtotal,
                        TaxAmount = sale.TaxAmount,
                        DiscountAmount = sale.DiscountAmount,
                        TotalAmount = sale.TotalAmount,
                        PaidAmount = payments.Sum(p => p.Amount),
                        Balance = sale.TotalAmount - payments.Sum(p => p.Amount)
                    },
                    StaffInfo = new StaffInfo
                    {
                        Name = sale.Cashier?.FullName ?? "System",
                        Role = "Cashier"
                    },
                    Notes = sale.Notes,
                    Barcode = sale.SaleNumber
                };

                var template = await GetReceiptTemplateAsync(sale.TenantId, "sale");
                return await _pdfGenerator.GeneratePdfAsync(template, receiptData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sale receipt for sale {SaleId}", saleId);
                throw;
            }
        }

        public async Task<byte[]> GeneratePaymentReceiptAsync(Guid paymentId, ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating payment receipt for payment {PaymentId}", paymentId);

                var payment = await _context.Payments
                    .Include(p => p.Sale)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    throw new ArgumentException($"Payment not found: {paymentId}");
                }

                var receiptData = new ReceiptData
                {
                    ReceiptType = "payment",
                    ReceiptNumber = payment.PaymentNumber,
                    Date = payment.PaymentDate,
                    TenantInfo = await GetTenantInfoAsync(payment.BranchId),
                    BranchInfo = await GetBranchInfoAsync(payment.BranchId),
                    Payments = new List<PaymentInfo>
                    {
                        new PaymentInfo
                        {
                            Method = payment.PaymentMethod,
                            Amount = payment.Amount,
                            Reference = payment.ReferenceNumber,
                            Status = payment.Status
                        }
                    },
                    Summary = new ReceiptSummary
                    {
                        TotalAmount = payment.Amount,
                        PaidAmount = payment.Status == "completed" ? payment.Amount : 0
                    },
                    Notes = payment.Notes,
                    Barcode = payment.PaymentNumber
                };

                if (payment.Sale != null)
                {
                    receiptData.RelatedDocument = payment.Sale.SaleNumber;
                    receiptData.CustomerInfo = payment.Sale.PatientId.HasValue ? 
                        await GetCustomerInfoAsync(payment.Sale.PatientId.Value) : null;
                }

                var template = await GetReceiptTemplateAsync(payment.TenantId, "payment");
                return await _pdfGenerator.GeneratePdfAsync(template, receiptData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment receipt for payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<byte[]> GeneratePrescriptionReceiptAsync(Guid prescriptionId, ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating prescription receipt for prescription {PrescriptionId}", prescriptionId);

                var prescription = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Doctor)
                    .Include(p => p.Items)
                    .ThenInclude(pi => pi.Product)
                    .FirstOrDefaultAsync(p => p.Id == prescriptionId);

                if (prescription == null)
                {
                    throw new ArgumentException($"Prescription not found: {prescriptionId}");
                }

                var receiptData = new ReceiptData
                {
                    ReceiptType = "prescription",
                    ReceiptNumber = prescription.PrescriptionNumber,
                    Date = prescription.PrescriptionDate,
                    TenantInfo = await GetTenantInfoAsync(prescription.BranchId),
                    BranchInfo = await GetBranchInfoAsync(prescription.BranchId),
                    CustomerInfo = prescription.Patient != null ? new CustomerInfo
                    {
                        Name = $"{prescription.Patient.FirstName} {prescription.Patient.LastName}",
                        Email = prescription.Patient.Email,
                        Phone = prescription.Patient.PhoneNumber,
                        Address = prescription.Patient.Address,
                        DateOfBirth = prescription.Patient.DateOfBirth,
                        PatientNumber = prescription.Patient.PatientNumber
                    } : null,
                    Items = prescription.Items.Select(item => new ReceiptItem
                    {
                        Name = item.Product?.Name ?? "Unknown Medication",
                        Code = item.Product?.Sku ?? "",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        Instructions = item.Instructions,
                        Category = item.Product?.Category ?? "",
                        Strength = item.Product?.Strength,
                        Form = item.Product?.Form
                    }).ToList(),
                    StaffInfo = prescription.Doctor != null ? new StaffInfo
                    {
                        Name = prescription.Doctor.FullName,
                        Role = "Doctor",
                        LicenseNumber = prescription.Doctor.LicenseNumber
                    } : null,
                    MedicalInfo = new MedicalInfo
                    {
                        PrescriptionNumber = prescription.PrescriptionNumber,
                        Diagnosis = prescription.Diagnosis,
                        Notes = prescription.Notes,
                        DispensingInstructions = prescription.DispensingInstructions
                    },
                    Barcode = prescription.PrescriptionNumber
                };

                var template = await GetReceiptTemplateAsync(prescription.TenantId, "prescription");
                return await _pdfGenerator.GeneratePdfAsync(template, receiptData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prescription receipt for prescription {PrescriptionId}", prescriptionId);
                throw;
            }
        }

        public async Task<byte[]> GenerateRefundReceiptAsync(Guid refundId, ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating refund receipt for refund {RefundId}", refundId);

                var refund = await _context.Payments
                    .Include(p => p.Sale)
                    .FirstOrDefaultAsync(p => p.Id == refundId && p.PaymentMethod == "refund");

                if (refund == null)
                {
                    throw new ArgumentException($"Refund not found: {refundId}");
                }

                var receiptData = new ReceiptData
                {
                    ReceiptType = "refund",
                    ReceiptNumber = refund.PaymentNumber,
                    Date = refund.PaymentDate,
                    TenantInfo = await GetTenantInfoAsync(refund.BranchId),
                    BranchInfo = await GetBranchInfoAsync(refund.BranchId),
                    CustomerInfo = refund.Sale?.PatientId.HasValue ? 
                        await GetCustomerInfoAsync(refund.Sale.PatientId.Value) : null,
                    Summary = new ReceiptSummary
                    {
                        TotalAmount = Math.Abs(refund.Amount),
                        RefundAmount = Math.Abs(refund.Amount)
                    },
                    Notes = refund.Notes,
                    Barcode = refund.PaymentNumber
                };

                var template = await GetReceiptTemplateAsync(refund.TenantId, "refund");
                return await _pdfGenerator.GeneratePdfAsync(template, receiptData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refund receipt for refund {RefundId}", refundId);
                throw;
            }
        }

        public async Task<byte[]> GenerateCustomReceiptAsync(ReceiptTemplate template, ReceiptData data)
        {
            try
            {
                _logger.LogInformation("Generating custom receipt with template {TemplateId}", template.Id);
                return await _pdfGenerator.GeneratePdfAsync(template, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom receipt");
                throw;
            }
        }

        public async Task<ReceiptTemplate> GetReceiptTemplateAsync(Guid tenantId, string receiptType)
        {
            try
            {
                var template = await _context.ReceiptTemplates
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ReceiptType == receiptType && !t.IsDeleted);

                if (template == null)
                {
                    // Return default template
                    template = GetDefaultTemplate(receiptType);
                }

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt template for tenant {TenantId}, type {ReceiptType}", tenantId, receiptType);
                return GetDefaultTemplate(receiptType);
            }
        }

        public async Task<ReceiptTemplate> UpdateReceiptTemplateAsync(Guid tenantId, ReceiptTemplate template)
        {
            try
            {
                var existingTemplate = await _context.ReceiptTemplates
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ReceiptType == template.ReceiptType && !t.IsDeleted);

                if (existingTemplate != null)
                {
                    existingTemplate.HtmlTemplate = template.HtmlTemplate;
                    existingTemplate.CssStyles = template.CssStyles;
                    existingTemplate.HeaderContent = template.HeaderContent;
                    existingTemplate.FooterContent = template.FooterContent;
                    existingTemplate.Layout = template.Layout;
                    existingTemplate.UpdatedAt = DateTime.UtcNow;
                    existingTemplate.UpdatedBy = template.UpdatedBy;

                    _context.ReceiptTemplates.Update(existingTemplate);
                }
                else
                {
                    template.TenantId = tenantId;
                    template.Id = Guid.NewGuid();
                    template.CreatedAt = DateTime.UtcNow;
                    template.UpdatedAt = DateTime.UtcNow;
                    template.IsDeleted = false;

                    _context.ReceiptTemplates.Add(template);
                }

                await _context.SaveChangesAsync();
                return existingTemplate ?? template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating receipt template for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<List<ReceiptHistoryDto>> GetReceiptHistoryAsync(Guid tenantId, Guid? entityId = null, string? receiptType = null)
        {
            try
            {
                var query = _context.ReceiptHistories
                    .Where(r => r.TenantId == tenantId && !r.IsDeleted);

                if (entityId.HasValue)
                {
                    query = query.Where(r => r.EntityId == entityId.Value);
                }

                if (!string.IsNullOrEmpty(receiptType))
                {
                    query = query.Where(r => r.ReceiptType == receiptType);
                }

                return await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReceiptHistoryDto
                    {
                        Id = r.Id,
                        ReceiptType = r.ReceiptType,
                        EntityId = r.EntityId,
                        ReceiptNumber = r.ReceiptNumber,
                        GeneratedAt = r.CreatedAt,
                        GeneratedBy = r.GeneratedBy,
                        FileSize = r.FileSize,
                        DownloadUrl = $"/api/v1/receipts/download/{r.Id}"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt history for tenant {TenantId}", tenantId);
                throw;
            }
        }

        private async Task<TenantInfo> GetTenantInfoAsync(Guid? branchId)
        {
            if (!branchId.HasValue) return null;

            var branch = await _context.Branches.FindAsync(branchId.Value);
            if (branch == null) return null;

            var tenant = await _context.Tenants.FindAsync(branch.TenantId);
            if (tenant == null) return null;

            return new TenantInfo
            {
                Name = tenant.Name,
                Address = tenant.Address,
                Phone = tenant.Phone,
                Email = tenant.Email,
                Website = tenant.Website,
                LogoUrl = tenant.LogoUrl,
                TaxNumber = tenant.TaxNumber,
                LicenseNumber = tenant.LicenseNumber
            };
        }

        private async Task<BranchInfo> GetBranchInfoAsync(Guid? branchId)
        {
            if (!branchId.HasValue) return null;

            var branch = await _context.Branches.FindAsync(branchId.Value);
            if (branch == null) return null;

            return new BranchInfo
            {
                Name = branch.Name,
                Address = branch.Address,
                Phone = branch.Phone,
                Email = branch.Email,
                Manager = branch.ManagerName
            };
        }

        private async Task<CustomerInfo> GetCustomerInfoAsync(Guid patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return null;

            return new CustomerInfo
            {
                Name = $"{patient.FirstName} {patient.LastName}",
                Email = patient.Email,
                Phone = patient.PhoneNumber,
                Address = patient.Address,
                DateOfBirth = patient.DateOfBirth,
                PatientNumber = patient.PatientNumber
            };
        }

        private ReceiptTemplate GetDefaultTemplate(string receiptType)
        {
            return receiptType.ToLower() switch
            {
                "sale" => new ReceiptTemplate
                {
                    Id = Guid.NewGuid(),
                    ReceiptType = "sale",
                    Name = "Default Sale Receipt",
                    HtmlTemplate = GetDefaultSaleReceiptTemplate(),
                    CssStyles = GetDefaultReceiptStyles(),
                    Layout = "standard",
                    IsDefault = true
                },
                "payment" => new ReceiptTemplate
                {
                    Id = Guid.NewGuid(),
                    ReceiptType = "payment",
                    Name = "Default Payment Receipt",
                    HtmlTemplate = GetDefaultPaymentReceiptTemplate(),
                    CssStyles = GetDefaultReceiptStyles(),
                    Layout = "standard",
                    IsDefault = true
                },
                "prescription" => new ReceiptTemplate
                {
                    Id = Guid.NewGuid(),
                    ReceiptType = "prescription",
                    Name = "Default Prescription Receipt",
                    HtmlTemplate = GetDefaultPrescriptionReceiptTemplate(),
                    CssStyles = GetDefaultReceiptStyles(),
                    Layout = "standard",
                    IsDefault = true
                },
                "refund" => new ReceiptTemplate
                {
                    Id = Guid.NewGuid(),
                    ReceiptType = "refund",
                    Name = "Default Refund Receipt",
                    HtmlTemplate = GetDefaultRefundReceiptTemplate(),
                    CssStyles = GetDefaultReceiptStyles(),
                    Layout = "standard",
                    IsDefault = true
                },
                _ => GetDefaultTemplate("sale")
            };
        }

        private string GetDefaultSaleReceiptTemplate()
        {
            return @"
<div class='receipt'>
    <div class='header'>
        <div class='company-info'>
            <h2>{{TenantInfo.Name}}</h2>
            <p>{{TenantInfo.Address}}</p>
            <p>Phone: {{TenantInfo.Phone}}</p>
            <p>Email: {{TenantInfo.Email}}</p>
        </div>
        <div class='receipt-details'>
            <h3>SALE RECEIPT</h3>
            <p>Receipt #: {{ReceiptNumber}}</p>
            <p>Date: {{Date:yyyy-MM-dd HH:mm}}</p>
        </div>
    </div>
    
    <div class='customer-info'>
        <h4>Customer Information</h4>
        <p>Name: {{CustomerInfo.Name}}</p>
        <p>Phone: {{CustomerInfo.Phone}}</p>
    </div>
    
    <div class='items'>
        <table>
            <thead>
                <tr>
                    <th>Item</th>
                    <th>Qty</th>
                    <th>Price</th>
                    <th>Discount</th>
                    <th>Total</th>
                </tr>
            </thead>
            <tbody>
                {{#each Items}}
                <tr>
                    <td>{{Name}}</td>
                    <td>{{Quantity}}</td>
                    <td>{{UnitPrice:C}}</td>
                    <td>{{Discount:C}}</td>
                    <td>{{TotalPrice:C}}</td>
                </tr>
                {{/each}}
            </tbody>
        </table>
    </div>
    
    <div class='summary'>
        <table>
            <tr><td>Subtotal:</td><td>{{Summary.Subtotal:C}}</td></tr>
            <tr><td>Tax:</td><td>{{Summary.TaxAmount:C}}</td></tr>
            <tr><td>Discount:</td><td>{{Summary.DiscountAmount:C}}</td></tr>
            <tr><td><strong>Total:</strong></td><td><strong>{{Summary.TotalAmount:C}}</strong></td></tr>
            <tr><td>Paid:</td><td>{{Summary.PaidAmount:C}}</td></tr>
            <tr><td>Balance:</td><td>{{Summary.Balance:C}}</td></tr>
        </table>
    </div>
    
    <div class='payments'>
        <h4>Payment Method(s)</h4>
        {{#each Payments}}
        <p>{{Method}}: {{Amount:C}} ({{Reference}})</p>
        {{/each}}
    </div>
    
    <div class='footer'>
        <p>Served by: {{StaffInfo.Name}} ({{StaffInfo.Role}})</p>
        <p>Thank you for your business!</p>
        <div class='barcode'>{{Barcode}}</div>
    </div>
</div>";
        }

        private string GetDefaultPaymentReceiptTemplate()
        {
            return @"
<div class='receipt'>
    <div class='header'>
        <div class='company-info'>
            <h2>{{TenantInfo.Name}}</h2>
            <p>{{TenantInfo.Address}}</p>
            <p>Phone: {{TenantInfo.Phone}}</p>
        </div>
        <div class='receipt-details'>
            <h3>PAYMENT RECEIPT</h3>
            <p>Receipt #: {{ReceiptNumber}}</p>
            <p>Date: {{Date:yyyy-MM-dd HH:mm}}</p>
        </div>
    </div>
    
    <div class='payment-info'>
        <h4>Payment Details</h4>
        {{#each Payments}}
        <p>Method: {{Method}}</p>
        <p>Amount: {{Amount:C}}</p>
        <p>Reference: {{Reference}}</p>
        <p>Status: {{Status}}</p>
        {{/each}}
    </div>
    
    <div class='summary'>
        <p><strong>Total Amount: {{Summary.TotalAmount:C}}</strong></p>
    </div>
    
    <div class='footer'>
        <p>Thank you for your payment!</p>
        <div class='barcode'>{{Barcode}}</div>
    </div>
</div>";
        }

        private string GetDefaultPrescriptionReceiptTemplate()
        {
            return @"
<div class='receipt'>
    <div class='header'>
        <div class='company-info'>
            <h2>{{TenantInfo.Name}}</h2>
            <p>{{TenantInfo.Address}}</p>
            <p>Phone: {{TenantInfo.Phone}}</p>
        </div>
        <div class='receipt-details'>
            <h3>PRESCRIPTION RECEIPT</h3>
            <p>Prescription #: {{ReceiptNumber}}</p>
            <p>Date: {{Date:yyyy-MM-dd HH:mm}}</p>
        </div>
    </div>
    
    <div class='patient-info'>
        <h4>Patient Information</h4>
        <p>Name: {{CustomerInfo.Name}}</p>
        <p>Patient #: {{CustomerInfo.PatientNumber}}</p>
        <p>Phone: {{CustomerInfo.Phone}}</p>
        <p>DOB: {{CustomerInfo.DateOfBirth:yyyy-MM-dd}}</p>
    </div>
    
    <div class='doctor-info'>
        <h4>Prescribed by</h4>
        <p>{{StaffInfo.Name}} ({{StaffInfo.Role}})</p>
        <p>License: {{StaffInfo.LicenseNumber}}</p>
    </div>
    
    <div class='medical-info'>
        <h4>Medical Information</h4>
        <p>Diagnosis: {{MedicalInfo.Diagnosis}}</p>
        <p>Notes: {{MedicalInfo.Notes}}</p>
    </div>
    
    <div class='medications'>
        <h4>Medications</h4>
        <table>
            <thead>
                <tr>
                    <th>Medication</th>
                    <th>Strength</th>
                    <th>Form</th>
                    <th>Quantity</th>
                    <th>Instructions</th>
                </tr>
            </thead>
            <tbody>
                {{#each Items}}
                <tr>
                    <td>{{Name}}</td>
                    <td>{{Strength}}</td>
                    <td>{{Form}}</td>
                    <td>{{Quantity}}</td>
                    <td>{{Instructions}}</td>
                </tr>
                {{/each}}
            </tbody>
        </table>
    </div>
    
    <div class='footer'>
        <p>Dispensing Instructions: {{MedicalInfo.DispensingInstructions}}</p>
        <div class='barcode'>{{Barcode}}</div>
    </div>
</div>";
        }

        private string GetDefaultRefundReceiptTemplate()
        {
            return @"
<div class='receipt'>
    <div class='header'>
        <div class='company-info'>
            <h2>{{TenantInfo.Name}}</h2>
            <p>{{TenantInfo.Address}}</p>
            <p>Phone: {{TenantInfo.Phone}}</p>
        </div>
        <div class='receipt-details'>
            <h3>REFUND RECEIPT</h3>
            <p>Receipt #: {{ReceiptNumber}}</p>
            <p>Date: {{Date:yyyy-MM-dd HH:mm}}</p>
        </div>
    </div>
    
    <div class='refund-info'>
        <h4>Refund Details</h4>
        <p><strong>Refund Amount: {{Summary.RefundAmount:C}}</strong></p>
        <p>Notes: {{Notes}}</p>
    </div>
    
    <div class='footer'>
        <p>Refund processed successfully</p>
        <div class='barcode'>{{Barcode}}</div>
    </div>
</div>";
        }

        private string GetDefaultReceiptStyles()
        {
            return @"
.receipt {
    font-family: Arial, sans-serif;
    max-width: 400px;
    margin: 0 auto;
    padding: 20px;
    border: 1px solid #ddd;
}

.header {
    text-align: center;
    margin-bottom: 20px;
    border-bottom: 2px solid #333;
    padding-bottom: 10px;
}

.company-info h2 {
    margin: 0;
    color: #333;
}

.company-info p {
    margin: 2px 0;
    font-size: 12px;
}

.receipt-details h3 {
    margin: 10px 0 5px 0;
    color: #333;
}

.customer-info, .payment-info, .doctor-info, .medical-info {
    margin: 15px 0;
    padding: 10px;
    background: #f9f9f9;
    border-radius: 3px;
}

.items table, .summary table {
    width: 100%;
    border-collapse: collapse;
    margin: 10px 0;
}

.items th, .items td, .summary td {
    padding: 5px;
    text-align: left;
    border-bottom: 1px solid #ddd;
}

.items th {
    background: #f5f5f5;
    font-weight: bold;
}

.summary td:last-child {
    text-align: right;
    font-weight: bold;
}

.footer {
    margin-top: 20px;
    text-align: center;
    border-top: 1px solid #ddd;
    padding-top: 10px;
}

.barcode {
    font-family: 'Courier New', monospace;
    font-size: 20px;
    margin-top: 10px;
}
";
        }
    }
}
