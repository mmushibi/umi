using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using UmiHealth.Shared.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace UmiHealth.Application.Services
{
    public class PdfGenerator : IPdfGenerator
    {
        private readonly ILogger<PdfGenerator> _logger;

        public PdfGenerator(ILogger<PdfGenerator> logger)
        {
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GeneratePdfAsync(ReceiptTemplate template, ReceiptData data, ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating PDF with template {TemplateId}", template.Id);

                options ??= new ReceiptOptions();

                var document = new ReceiptPdfDocument(template, data, options);
                return await Task.FromResult(document.GeneratePdf());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF with template {TemplateId}", template.Id);
                throw;
            }
        }

        public async Task<byte[]> GeneratePdfFromHtmlAsync(string html, string css = "", ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating PDF from HTML");

                options ??= new ReceiptOptions();

                var document = new HtmlPdfDocument(html, css, options);
                return await Task.FromResult(document.GeneratePdf());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF from HTML");
                throw;
            }
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(InvoiceData data, ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating invoice PDF");

                options ??= new ReceiptOptions();

                var document = new InvoicePdfDocument(data, options);
                return await Task.FromResult(document.GeneratePdf());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF");
                throw;
            }
        }

        public async Task<byte[]> GenerateReportPdfAsync(ReportData data, ReceiptOptions options = null)
        {
            try
            {
                _logger.LogInformation("Generating report PDF");

                options ??= new ReceiptOptions();

                var document = new ReportPdfDocument(data, options);
                return await Task.FromResult(document.GeneratePdf());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report PDF");
                throw;
            }
        }
    }

    public class ReceiptPdfDocument : IDocument
    {
        private readonly ReceiptTemplate _template;
        private readonly ReceiptData _data;
        private readonly ReceiptOptions _options;

        public ReceiptPdfDocument(ReceiptTemplate template, ReceiptData data, ReceiptOptions options)
        {
            _template = template;
            _data = data;
            _options = options;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"{_data.ReceiptType.ToUpper()} Receipt - {_data.ReceiptNumber}",
            Author = "UmiHealth Pharmacy System",
            Subject = $"{_data.ReceiptType} Receipt",
            Keywords = "receipt, pharmacy, umihealth",
            CreationDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        public DocumentSettings GetSettings() => new DocumentSettings
        {
            PaperSize = PaperSize.A4,
            Margin = _options.Margin ?? (20, 20, 20, 20),
            PageOrientation = PageOrientation.Portrait
        };

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));
                    
                    page.Header()
                        .Element(ComposeHeader);

                    page.Content()
                        .Element(ComposeContent);

                    page.Footer()
                        .Element(ComposeFooter);
                });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(_data.TenantInfo?.Name ?? "Pharmacy").Bold().FontSize(14);
                    column.Item().Text(_data.TenantInfo?.Address ?? "").FontSize(9);
                    column.Item().Text($"Phone: {_data.TenantInfo?.Phone ?? ""}").FontSize(9);
                    column.Item().Text($"Email: {_data.TenantInfo?.Email ?? ""}").FontSize(9);
                });

                row.RelativeItem().AlignRight().Column(column =>
                {
                    column.Item().Text($"{_data.ReceiptType.ToUpper()} RECEIPT").Bold().FontSize(12);
                    column.Item().Text($"Receipt #: {_data.ReceiptNumber}").FontSize(9);
                    column.Item().Text($"Date: {_data.Date:yyyy-MM-dd HH:mm}").FontSize(9);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                // Customer Information
                if (_data.CustomerInfo != null)
                {
                    column.Item().Element(ComposeCustomerInfo);
                }

                // Items Table
                if (_data.Items?.Any() == true)
                {
                    column.Item().PaddingVertical(10).Element(ComposeItemsTable);
                }

                // Payment Information
                if (_data.Payments?.Any() == true)
                {
                    column.Item().Element(ComposePaymentInfo);
                }

                // Summary
                column.Item().Element(ComposeSummary);

                // Medical Information (for prescriptions)
                if (_data.MedicalInfo != null)
                {
                    column.Item().Element(ComposeMedicalInfo);
                }

                // Notes
                if (!string.IsNullOrEmpty(_data.Notes))
                {
                    column.Item().PaddingTop(10).Text($"Notes: {_data.Notes}").FontSize(9);
                }
            });
        }

        private void ComposeCustomerInfo(IContainer container)
        {
            container.Padding(10).Background("#f5f5f5").Column(column =>
            {
                column.Item().Text("Customer Information").Bold().FontSize(10);
                column.Item().Text($"Name: {_data.CustomerInfo.Name}").FontSize(9);
                column.Item().Text($"Phone: {_data.CustomerInfo.Phone}").FontSize(9);
                
                if (!string.IsNullOrEmpty(_data.CustomerInfo.Email))
                    column.Item().Text($"Email: {_data.CustomerInfo.Email}").FontSize(9);
                
                if (!string.IsNullOrEmpty(_data.CustomerInfo.Address))
                    column.Item().Text($"Address: {_data.CustomerInfo.Address}").FontSize(9);
            });
        }

        private void ComposeItemsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(60);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Item").Bold();
                    header.Cell().Element(CellStyle).Text("Qty").Bold();
                    header.Cell().Element(CellStyle).Text("Price").Bold();
                    header.Cell().Element(CellStyle).Text("Discount").Bold();
                    header.Cell().Element(CellStyle).Text("Total").Bold();
                });

                foreach (var item in _data.Items)
                {
                    table.Cell().Element(CellStyle).Text(item.Name);
                    table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                    table.Cell().Element(CellStyle).Text(item.UnitPrice.ToString("C"));
                    table.Cell().Element(CellStyle).Text(item.Discount.ToString("C"));
                    table.Cell().Element(CellStyle).Text(item.TotalPrice.ToString("C"));
                }
            });
        }

        private void ComposePaymentInfo(IContainer container)
        {
            container.PaddingTop(10).Column(column =>
            {
                column.Item().Text("Payment Method(s)").Bold().FontSize(10);
                
                foreach (var payment in _data.Payments)
                {
                    column.Item().Text($"{payment.Method}: {payment.Amount:C} ({payment.Reference})").FontSize(9);
                }
            });
        }

        private void ComposeSummary(IContainer container)
        {
            container.PaddingTop(20).AlignRight().Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal:").FontSize(9);
                    row.ConstantColumn(80).AlignRight().Text(_data.Summary.Subtotal.ToString("C")).FontSize(9);
                });
                
                if (_data.Summary.TaxAmount > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Tax:").FontSize(9);
                        row.ConstantColumn(80).AlignRight().Text(_data.Summary.TaxAmount.ToString("C")).FontSize(9);
                    });
                }
                
                if (_data.Summary.DiscountAmount > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Discount:").FontSize(9);
                        row.ConstantColumn(80).AlignRight().Text(_data.Summary.DiscountAmount.ToString("C")).FontSize(9);
                    });
                }
                
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total:").Bold().FontSize(10);
                    row.ConstantColumn(80).AlignRight().Text(_data.Summary.TotalAmount.ToString("C")).Bold().FontSize(10);
                });
                
                if (_data.Summary.PaidAmount > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Paid:").FontSize(9);
                        row.ConstantColumn(80).AlignRight().Text(_data.Summary.PaidAmount.ToString("C")).FontSize(9);
                    });
                }
                
                if (_data.Summary.Balance != 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Balance:").FontSize(9);
                        row.ConstantColumn(80).AlignRight().Text(_data.Summary.Balance.ToString("C")).FontSize(9);
                    });
                }
            });
        }

        private void ComposeMedicalInfo(IContainer container)
        {
            container.PaddingTop(10).Column(column =>
            {
                column.Item().Text("Medical Information").Bold().FontSize(10);
                column.Item().Text($"Prescription #: {_data.MedicalInfo.PrescriptionNumber}").FontSize(9);
                
                if (!string.IsNullOrEmpty(_data.MedicalInfo.Diagnosis))
                    column.Item().Text($"Diagnosis: {_data.MedicalInfo.Diagnosis}").FontSize(9);
                
                if (!string.IsNullOrEmpty(_data.MedicalInfo.Notes))
                    column.Item().Text($"Notes: {_data.MedicalInfo.Notes}").FontSize(9);
                
                if (!string.IsNullOrEmpty(_data.MedicalInfo.DispensingInstructions))
                    column.Item().Text($"Dispensing Instructions: {_data.MedicalInfo.DispensingInstructions}").FontSize(9);
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                if (_data.StaffInfo != null)
                {
                    column.Item().Text($"Served by: {_data.StaffInfo.Name} ({_data.StaffInfo.Role})").FontSize(9);
                }
                
                column.Item().Text("Thank you for your business!").FontSize(9);
                
                if (!string.IsNullOrEmpty(_data.Barcode))
                {
                    column.Item().PaddingTop(5).Text(_data.Barcode).FontFamily(Fonts.Courier).FontSize(12);
                }
            });
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor("#e0e0e0")
                .Padding(5)
                .AlignCenter()
                .AlignMiddle();
        }
    }

    public class HtmlPdfDocument : IDocument
    {
        private readonly string _html;
        private readonly string _css;
        private readonly ReceiptOptions _options;

        public HtmlPdfDocument(string html, string css, ReceiptOptions options)
        {
            _html = html;
            _css = css;
            _options = options;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = "Generated Document",
            Author = "UmiHealth Pharmacy System",
            CreationDate = DateTime.UtcNow
        };

        public DocumentSettings GetSettings() => new DocumentSettings
        {
            PaperSize = PaperSize.A4,
            Margin = _options.Margin ?? (20, 20, 20, 20),
            PageOrientation = PageOrientation.Portrait
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));
                page.Content().Padding(20).Column(column =>
                {
                    column.Item().Text("HTML to PDF conversion is not implemented in this basic version.").FontSize(12);
                    column.Item().Text("Please use a proper HTML-to-PDF library like PuppeteerSharp or DinkToPdf.").FontSize(10);
                });
            });
        }
    }

    public class InvoicePdfDocument : IDocument
    {
        private readonly InvoiceData _data;
        private readonly ReceiptOptions _options;

        public InvoicePdfDocument(InvoiceData data, ReceiptOptions options)
        {
            _data = data;
            _options = options;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Invoice {_data.InvoiceNumber}",
            Author = "UmiHealth Pharmacy System",
            CreationDate = DateTime.UtcNow
        };

        public DocumentSettings GetSettings() => new DocumentSettings
        {
            PaperSize = PaperSize.A4,
            Margin = _options.Margin ?? (20, 20, 20, 20),
            PageOrientation = PageOrientation.Portrait
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));
                page.Content().Padding(20).Column(column =>
                {
                    column.Item().Text("INVOICE").Bold().FontSize(16);
                    column.Item().Text($"Invoice #: {_data.InvoiceNumber}").FontSize(12);
                    column.Item().Text($"Date: {_data.InvoiceDate:yyyy-MM-dd}").FontSize(12);
                    column.Item().Text($"Due Date: {_data.DueDate:yyyy-MM-dd}").FontSize(12);
                });
            });
        }
    }

    public class ReportPdfDocument : IDocument
    {
        private readonly ReportData _data;
        private readonly ReceiptOptions _options;

        public ReportPdfDocument(ReportData data, ReceiptOptions options)
        {
            _data = data;
            _options = options;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = _data.Title,
            Author = "UmiHealth Pharmacy System",
            CreationDate = DateTime.UtcNow
        };

        public DocumentSettings GetSettings() => new DocumentSettings
        {
            PaperSize = PaperSize.A4,
            Margin = _options.Margin ?? (20, 20, 20, 20),
            PageOrientation = PageOrientation.Portrait
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));
                page.Content().Padding(20).Column(column =>
                {
                    column.Item().Text(_data.Title).Bold().FontSize(16);
                    column.Item().Text($"Generated: {_data.GeneratedDate:yyyy-MM-dd HH:mm}").FontSize(12);
                    column.Item().Text($"Period: {_data.StartDate:yyyy-MM-dd} to {_data.EndDate:yyyy-MM-dd}").FontSize(12);
                });
            });
        }
    }
}
