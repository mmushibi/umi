using System;
using System.Collections.Generic;

namespace UmiHealth.Shared.DTOs
{
    // Receipt Generation DTOs
    public class ReceiptOptions
    {
        public (int Left, int Top, int Right, int Bottom)? Margin { get; set; } = (20, 20, 20, 20);
        public bool IncludeHeader { get; set; } = true;
        public bool IncludeFooter { get; set; } = true;
        public bool IncludeBarcode { get; set; } = true;
        public bool IncludeWatermark { get; set; } = false;
        public string WatermarkText { get; set; } = string.Empty;
        public string PaperSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";
    }

    public class ReceiptData
    {
        public string ReceiptType { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TenantInfo TenantInfo { get; set; }
        public BranchInfo BranchInfo { get; set; }
        public CustomerInfo CustomerInfo { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
        public List<PaymentInfo> Payments { get; set; } = new();
        public ReceiptSummary Summary { get; set; }
        public StaffInfo StaffInfo { get; set; }
        public MedicalInfo MedicalInfo { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string RelatedDocument { get; set; } = string.Empty;
    }

    public class ReceiptTemplate
    {
        public Guid Id { get; set; }
        public string ReceiptType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HtmlTemplate { get; set; } = string.Empty;
        public string CssStyles { get; set; } = string.Empty;
        public string HeaderContent { get; set; } = string.Empty;
        public string FooterContent { get; set; } = string.Empty;
        public string Layout { get; set; } = "standard";
        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public Guid TenantId { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TenantInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
    }

    public class BranchInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
    }

    public class CustomerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
    }

    public class ReceiptItem
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalPrice { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
    }

    public class PaymentInfo
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ReceiptSummary
    {
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class StaffInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
    }

    public class MedicalInfo
    {
        public string PrescriptionNumber { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string DispensingInstructions { get; set; } = string.Empty;
    }

    public class ReceiptHistoryDto
    {
        public Guid Id { get; set; }
        public string ReceiptType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public Guid GeneratedBy { get; set; }
        public int FileSize { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    // Invoice and Report DTOs for PDF generation
    public class InvoiceData
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public CustomerInfo Customer { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
        public ReceiptSummary Summary { get; set; }
        public TenantInfo Company { get; set; }
    }

    public class ReportData
    {
        public string Title { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ReportSection> Sections { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ReportSection
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<ReportTable> Tables { get; set; } = new();
        public List<ReportChart> Charts { get; set; } = new();
    }

    public class ReportTable
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }

    public class ReportChart
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // bar, line, pie, etc.
        public List<ChartDataPoint> Data { get; set; } = new();
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    // Receipt Configuration DTOs
    public class ReceiptConfigurationDto
    {
        public Guid TenantId { get; set; }
        public bool EnableReceiptGeneration { get; set; } = true;
        public bool AutoGenerateReceipts { get; set; } = true;
        public bool SaveReceiptHistory { get; set; } = true;
        public bool EnableEmailReceipts { get; set; } = false;
        public bool EnableSmsReceipts { get; set; } = false;
        public int ReceiptRetentionDays { get; set; } = 365;
        public List<string> EnabledReceiptTypes { get; set; } = new();
        public Dictionary<string, ReceiptTypeConfiguration> TypeConfigurations { get; set; } = new();
    }

    public class ReceiptTypeConfiguration
    {
        public bool IsEnabled { get; set; } = true;
        public string TemplateId { get; set; } = string.Empty;
        public bool AutoGenerate { get; set; } = true;
        public bool SaveHistory { get; set; } = true;
        public bool AllowEmail { get; set; } = false;
        public bool AllowSms { get; set; } = false;
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    // Receipt Analytics DTOs
    public class ReceiptAnalyticsDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalReceipts { get; set; }
        public Dictionary<string, int> ReceiptsByType { get; set; } = new();
        public List<DailyReceiptCountDto> DailyReceiptCounts { get; set; } = new();
        public List<TopReceiptGeneratorDto> TopGenerators { get; set; } = new();
        public decimal TotalFileSize { get; set; }
        public int AverageFileSize { get; set; }
    }

    public class DailyReceiptCountDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public Dictionary<string, int> BreakdownByType { get; set; } = new();
    }

    public class TopReceiptGeneratorDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public Dictionary<string, int> ReceiptsByType { get; set; } = new();
    }

    // Receipt Template Management DTOs
    public class CreateReceiptTemplateRequest
    {
        public string ReceiptType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HtmlTemplate { get; set; } = string.Empty;
        public string CssStyles { get; set; } = string.Empty;
        public string HeaderContent { get; set; } = string.Empty;
        public string FooterContent { get; set; } = string.Empty;
        public string Layout { get; set; } = "standard";
        public bool IsDefault { get; set; } = false;
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class UpdateReceiptTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HtmlTemplate { get; set; } = string.Empty;
        public string CssStyles { get; set; } = string.Empty;
        public string HeaderContent { get; set; } = string.Empty;
        public string FooterContent { get; set; } = string.Empty;
        public string Layout { get; set; } = "standard";
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class ReceiptTemplatePreviewRequest
    {
        public string ReceiptType { get; set; } = string.Empty;
        public string HtmlTemplate { get; set; } = string.Empty;
        public string CssStyles { get; set; } = string.Empty;
        public ReceiptData SampleData { get; set; }
    }

    public class ReceiptTemplatePreviewResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public byte[] PdfPreview { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
