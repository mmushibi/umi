using System;
using System.Threading.Tasks;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Application.Services
{
    public interface IPdfGenerator
    {
        Task<byte[]> GeneratePdfAsync(ReceiptTemplate template, ReceiptData data, ReceiptOptions options = null);
        Task<byte[]> GeneratePdfFromHtmlAsync(string html, string css = "", ReceiptOptions options = null);
        Task<byte[]> GenerateInvoicePdfAsync(InvoiceData data, ReceiptOptions options = null);
        Task<byte[]> GenerateReportPdfAsync(ReportData data, ReceiptOptions options = null);
    }
}
