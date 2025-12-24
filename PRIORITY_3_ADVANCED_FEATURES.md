# Umi Health - Advanced Features Implementation Guide

## Priority 3: Advanced Features - Complete Implementation

This guide documents the complete implementation of Priority 3 (Advanced Features) including real-time updates, analytics, data export/import, and regulatory compliance.

---

## Table of Contents

1. [Real-Time Updates (SignalR)](#1-real-time-updates)
2. [Analytics & Reporting](#2-analytics--reporting)
3. [Data Export/Import](#3-data-exportimport)
4. [Regulatory Compliance (ZAMRA & ZRA)](#4-regulatory-compliance)
5. [API Endpoints Summary](#5-api-endpoints-summary)
6. [Frontend Integration](#6-frontend-integration)
7. [Deployment Considerations](#7-deployment-considerations)

---

## 1. Real-Time Updates (SignalR)

### Architecture

Real-time bidirectional communication using ASP.NET Core SignalR with support for inventory, sales, and notification updates.

### Components Created

#### RealtimeHubs.cs (1,200+ lines)
- **InventoryHub**: Stock level updates, product-specific tracking, branch-level notifications
- **SalesHub**: Real-time sale confirmations, dashboard metrics, branch-specific aggregations
- **NotificationHub**: User-specific alerts, notifications, system announcements

### Key Features

#### Inventory Hub
```csharp
// Client connects and joins inventory tracking groups
await Groups.AddToGroupAsync(ConnectionId, $"inventory_branch_{branchId}");
await Groups.AddToGroupAsync(ConnectionId, $"product_{productId}");

// Broadcast stock updates to relevant clients
await Clients.Group($"inventory_branch_{branchId}")
    .SendAsync("StockUpdated", productId, newQuantity);

// Notify when stock is low
await Clients.Group($"product_{productId}")
    .SendAsync("LowStockAlert", productName, quantityThreshold);
```

#### Sales Hub
```csharp
// Real-time sale notifications
await Clients.Group($"dashboard_{branchId}")
    .SendAsync("SaleCompleted", new {
        saleId = sale.Id,
        amount = sale.TotalAmount,
        timestamp = DateTime.UtcNow
    });

// Payment received notifications
await Clients.Group($"branch_{branchId}")
    .SendAsync("PaymentReceived", payment);
```

#### Notification Hub
```csharp
// User-specific notifications
await Clients.Group($"user_{userId}")
    .SendAsync("NotificationReceived", new {
        title = "Payment Alert",
        message = "Payment of K" + amount + " received",
        timestamp = DateTime.UtcNow
    });
```

### Real-Time Notification Service

Interface: `IRealtimeNotificationService`
Implementation: `RealtimeNotificationService`

**Key Methods:**
- `NotifyInventoryUpdateAsync()` - Stock changes
- `NotifyLowStockAsync()` - Low stock alerts
- `NotifyExpiringProductAsync()` - Expiry warnings
- `NotifySaleCompletedAsync()` - Sale confirmations
- `NotifyPaymentReceivedAsync()` - Payment notifications
- `NotifyUserAsync()` - Generic user notifications
- `UpdateDashboardAsync()` - Live metrics

### SignalR Group Organization

Groups follow hierarchical structure:
- `inventory_branch_{branchId}` - Inventory updates for specific branch
- `sales_branch_{branchId}` - Sales for specific branch
- `product_{productId}` - Updates for specific product
- `user_{userId}` - User-specific notifications
- `dashboard_{branchId}` - Dashboard metrics
- `notifications_tenant_{tenantId}` - Tenant-level notifications

### Configuration

Register in Program.cs:
```csharp
builder.Services.AddSignalR()
    .AddHubOptions<InventoryHub>(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    })
    .AddHubOptions<SalesHub>(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    })
    .AddHubOptions<NotificationHub>(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    });

app.MapHub<InventoryHub>("/hubs/inventory");
app.MapHub<SalesHub>("/hubs/sales");
app.MapHub<NotificationHub>("/hubs/notifications");
```

---

## 2. Analytics & Reporting

### AnalyticsService Overview

Interface: `IAnalyticsService`
Implementation: `AnalyticsService` (850+ lines)

Comprehensive business intelligence platform providing:

### Sales Analytics
```csharp
public async Task<SalesAnalytics> GetSalesAnalyticsAsync(
    string tenantId, 
    string branchId, 
    DateTime startDate, 
    DateTime endDate)
```

Returns:
- Total sales volume
- Average transaction value
- Transaction count
- Discount analysis
- Tax breakdown
- Customer metrics
- Payment method breakdown

### Inventory Analytics
```csharp
public async Task<InventoryAnalytics> GetInventoryAnalyticsAsync(
    string tenantId, 
    string branchId)
```

Returns:
- Total product count
- Low stock product count
- Out of stock products
- Overstocked items
- Total inventory value
- Stock movement metrics

### Product Performance
```csharp
public async Task<ProductPerformanceAnalytics> GetProductPerformanceAsync(
    string tenantId, 
    string branchId, 
    int topN = 10)
```

Returns:
- Top performing products by revenue
- Bottom performers (slow movers)
- Sales count and profit margins
- Average product revenue

### Daily Trends
```csharp
public async Task<DailyTrendAnalytics> GetDailyTrendsAsync(
    string tenantId, 
    string branchId, 
    int days = 30)
```

Returns:
- Daily sales trends
- Growth percentage
- Trend direction (Upward/Downward/Stable)

### Patient Analytics
```csharp
public async Task<PatientAnalytics> GetPatientAnalyticsAsync(
    string tenantId)
```

Returns:
- Total patients
- Active patients
- New patients this month
- Repeat customer percentage
- Customer lifetime value

### Dashboard Metrics (KPIs)
```csharp
public async Task<DashboardMetrics> GetDashboardMetricsAsync(
    string tenantId, 
    string branchId)
```

Real-time KPIs:
- Today's sales
- This week's sales
- This month's sales
- Transaction count
- Low stock count
- Expiring products count
- Average transaction value
- Top products
- Sales by hour

### Branch Comparison
```csharp
public async Task<ComparisonAnalytics> CompareBranchesAsync(
    string tenantId, 
    DateTime startDate, 
    DateTime endDate)
```

Returns:
- Branch-by-branch performance
- Growth comparisons
- Best performing branch
- Tenant averages

### Forecasting (ML-Ready)
```csharp
public async Task<SalesForecast> GetSalesForecastAsync(
    string tenantId, 
    string branchId, 
    int forecastDays = 30)

public async Task<InventoryForecast> GetInventoryForecastAsync(
    string tenantId, 
    string branchId)
```

---

## 3. Data Export/Import

### DataExportImportService Overview

Interface: `IDataExportImportService`
Implementation: `DataExportImportService` (400+ lines)

Supports CSV and Excel formats for data interchange.

### Export Operations

#### Export Products
```csharp
public async Task<byte[]> ExportProductsAsync(
    string tenantId, 
    string branchId, 
    string format = "csv")
```

Exports: Product code, name, category, pricing, units, reorder levels

#### Export Patients
```csharp
public async Task<byte[]> ExportPatientsAsync(
    string tenantId, 
    string format = "csv")
```

Exports: Patient demographics, contact info, ID numbers, DOB, gender

#### Export Inventory
```csharp
public async Task<byte[]> ExportInventoryAsync(
    string tenantId, 
    string branchId, 
    string format = "csv")
```

Exports: Stock levels, costs, selling prices, expiry dates, movement history

#### Export Sales
```csharp
public async Task<byte[]> ExportSalesAsync(
    string tenantId, 
    string branchId, 
    DateTime startDate, 
    DateTime endDate, 
    string format = "csv")
```

Exports: Receipt numbers, amounts, discounts, taxes, payment methods

#### Export Prescriptions
```csharp
public async Task<byte[]> ExportPrescriptionsAsync(
    string tenantId, 
    string branchId, 
    DateTime startDate, 
    DateTime endDate, 
    string format = "csv")
```

### Import Operations

#### Import Products
```csharp
public async Task<ImportResult> ImportProductsAsync(
    string tenantId, 
    string branchId, 
    Stream fileStream, 
    string format = "csv")
```

Creates new products or updates existing ones from CSV/Excel

Returns ImportResult with:
- Records processed
- Success count
- Failure count
- Error messages
- Warning messages

#### Import Patients
```csharp
public async Task<ImportResult> ImportPatientsAsync(
    string tenantId, 
    Stream fileStream, 
    string format = "csv")
```

Bulk patient registration with auto-generated codes

#### Import Inventory
```csharp
public async Task<ImportResult> ImportInventoryAsync(
    string tenantId, 
    string branchId, 
    Stream fileStream, 
    string format = "csv")
```

Bulk inventory updates and transfers

### CSV Format Examples

#### Products CSV
```
ProductCode,ProductName,Category,CostPrice,SellingPrice,Unit,ReorderLevel
PARACETAMOL-500,Paracetamol 500mg,Analgesics,0.50,1.50,tablet,100
AMOXICILLIN-500,Amoxicillin 500mg,Antibiotics,2.00,5.00,capsule,50
```

#### Patients CSV
```
FirstName,LastName,PhoneNumber,Email,IdType,IdNumber,DateOfBirth,Gender
John,Doe,+260961234567,john@example.com,NRC,123456789,1985-05-15,Male
Jane,Smith,+260967654321,jane@example.com,Passport,AB123456,1990-08-20,Female
```

---

## 4. Regulatory Compliance

### ZAMRA (Medicines Regulatory Authority)

#### ComplianceService - ZAMRA Methods

```csharp
public async Task<ZamraComplianceReport> GenerateZamraComplianceReportAsync(
    string tenantId, 
    DateTime startDate, 
    DateTime endDate)
```

**Report Contents:**
- Pharmacy license information
- Pharmacist registration status
- Total transactions sold
- Prescriptions sold with prescription
- Controlled substance transactions
- Compliance percentage
- Violations found
- Recommendations

#### Prescription Audit Trail
```csharp
public async Task<PrescriptionAuditTrail> GetPrescriptionAuditTrailAsync(
    Guid prescriptionId)
```

Tracks:
- Prescriptor details and license
- Dispensing date and pharmacist
- All dispensed items with dosages
- Approval chain

#### Expiry Compliance
```csharp
public async Task<ExpiryComplianceReport> GetExpiryComplianceReportAsync(
    string tenantId, 
    string branchId)
```

Monitors:
- Expired products removed
- Products expiring in 30 days
- Products expiring in 90 days
- Expiry records with removal dates

#### Controlled Substances
```csharp
public async Task<ControlledSubstanceReport> GetControlledSubstanceReportAsync(
    string tenantId, 
    DateTime startDate, 
    DateTime endDate)
```

Tracks:
- Controlled substance transactions
- Current stock levels
- Inventory balance verification
- Storage locations

#### Drug Interactions
```csharp
public async Task<DrugInteractionReport> CheckDrugInteractionsAsync(
    List<Guid> productIds)
```

Checks for:
- Drug-drug interactions
- Contraindications
- Severity levels
- Clinical recommendations

### ZRA (Zambia Revenue Authority)

#### Tax Compliance Report
```csharp
public async Task<TaxCompleteReport> GenerateTaxComplianceReportAsync(
    string tenantId, 
    DateTime startDate, 
    DateTime endDate)
```

**Report Contents:**
- Gross sales
- Exempt sales
- Taxable sales
- VAT (16% standard rate)
- Other taxes
- Exemptions breakdown
- Compliance status

#### VAT Calculation
```csharp
public async Task<VatCalculationReport> CalculateVatAsync(
    string tenantId, 
    DateTime startDate, 
    DateTime endDate)
```

Returns:
- Standard rate (16%)
- Reduced rate (0% or 5%)
- Daily/weekly/monthly calculations
- Total VAT owed

#### Invoice Audit Trail
```csharp
public async Task<InvoiceAuditTrail> GetInvoiceAuditTrailAsync(
    Guid invoiceId)
```

Tracks:
- Invoice number and date
- Customer details
- Gross amount and VAT
- Net amount
- Payment method and date
- Cashier responsible

#### Tax Exemptions
```csharp
public async Task<ExemptionReport> GetExemptionReportAsync(
    string tenantId)
```

Documents:
- Categories of exempt goods
- Exemption amounts
- Exemption reasons
- Transaction dates

### General Compliance

#### Compliance Status
```csharp
public async Task<ComplianceStatus> GetComplianceStatusAsync(
    string tenantId)
```

Returns:
- Overall compliance status
- Compliance by area (ZAMRA, ZRA, etc.)
- Issues found
- Last audit date
- Next audit due

#### Compliance Alerts
```csharp
public async Task<List<ComplianceAlert>> GetActiveAlertsAsync(
    string tenantId)
```

Alert types:
- Expired products alert
- Expiry warnings (30/90 day)
- Tax compliance alerts
- Prescription documentation alerts

#### Audit Logging
```csharp
public async Task<ComplianceAuditLog> LogComplianceActionAsync(
    string tenantId, 
    string action, 
    string details)
```

Logs all compliance-related actions for audit trail.

---

## 5. API Endpoints Summary

### Reports Controller (`/api/reports`)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/sales-analytics` | Sales metrics and breakdown |
| GET | `/daily-trends` | Daily trend analysis |
| GET | `/product-performance` | Top/bottom products |
| GET | `/payment-methods` | Payment method breakdown |
| GET | `/inventory-analytics` | Inventory status |
| GET | `/expiry-report` | Expiry tracking |
| GET | `/patient-analytics` | Patient metrics |
| GET | `/dashboard-metrics` | KPIs (real-time) |
| GET | `/branch-comparison` | Branch performance comparison |
| GET | `/sales-forecast` | Predictive sales forecast |
| GET | `/inventory-forecast` | Stock prediction |
| GET | `/export/sales` | Export sales data |

### Compliance Controller (`/api/compliance`)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/zamra-report` | ZAMRA compliance report |
| GET | `/prescription-audit/{id}` | Prescription audit trail |
| GET | `/expiry-compliance` | Expiry compliance status |
| POST | `/check-interactions` | Drug interaction checker |
| GET | `/controlled-substances` | Controlled substance tracking |
| GET | `/zra-tax-report` | ZRA tax compliance |
| GET | `/invoice-audit/{id}` | Invoice audit trail |
| GET | `/vat-calculation` | VAT calculation |
| GET | `/exemptions` | Tax exemptions |
| GET | `/status` | Overall compliance status |
| GET | `/alerts` | Active compliance alerts |

### Real-Time Hubs

| Hub | Endpoint | Purpose |
|-----|----------|---------|
| InventoryHub | `/hubs/inventory` | Real-time stock updates |
| SalesHub | `/hubs/sales` | Real-time sales notifications |
| NotificationHub | `/hubs/notifications` | General user notifications |

---

## 6. Frontend Integration

### Analytics Dashboard Implementation

```javascript
// Connect to analytics SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/sales", {
        accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .build();

connection.on("DashboardUpdated", (metrics) => {
    updateDashboard(metrics);
});

await connection.start();

// Fetch analytics data
const analytics = await apiClient.getRequest(
    `/api/reports/dashboard-metrics?branchId=${branchId}`
);

// Real-time stock updates
const inventoryConnection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/inventory", {
        accessTokenFactory: () => token
    })
    .build();

inventoryConnection.on("StockUpdated", (productId, quantity) => {
    updateInventoryUI(productId, quantity);
});

inventoryConnection.on("LowStockAlert", (productName, threshold) => {
    showAlert(`Low stock alert: ${productName} below ${threshold}`);
});
```

### Export Data
```javascript
// Export sales data as CSV
const response = await fetch(
    `/api/reports/export/sales?branchId=${branchId}&format=csv`,
    { headers: { Authorization: `Bearer ${token}` } }
);

const blob = await response.blob();
downloadFile(blob, `sales_${new Date().toISOString()}.csv`);
```

### Check Compliance Status
```javascript
// Get ZAMRA compliance status
const compliance = await apiClient.getRequest(
    `/api/compliance/zamra-report?startDate=${startDate}&endDate=${endDate}`
);

displayComplianceStatus(compliance);

// Check drug interactions
const interactions = await apiClient.postRequest(
    `/api/compliance/check-interactions`,
    { productIds: selectedProducts }
);
```

---

## 7. Deployment Considerations

### Prerequisites

1. **ASP.NET Core 8.0**
2. **PostgreSQL 15+** with Row-Level Security
3. **Redis** (optional, for SignalR backplane in distributed scenarios)
4. **CSV/Excel Libraries**:
   - CsvHelper
   - EPPlus (for Excel support)

### Installation

Add to backend project:
```bash
dotnet add package CsvHelper
dotnet add package EPPlus
dotnet add package NPOI  # Alternative Excel library
```

### Configuration

Program.cs setup:
```csharp
// Add analytics services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IDataExportImportService, DataExportImportService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();

// SignalR configuration
builder.Services.AddSignalR();

app.MapHub<InventoryHub>("/hubs/inventory");
app.MapHub<SalesHub>("/hubs/sales");
app.MapHub<NotificationHub>("/hubs/notifications");
```

### Performance Tuning

1. **Analytics Queries**:
   - Use database indexes on `TenantId`, `BranchId`, `CreatedAt`
   - Implement query caching for frequently accessed reports
   - Use background jobs for heavy computations

2. **Real-Time Updates**:
   - Configure SignalR message buffer limits
   - Use group-based broadcasting for scalability
   - Monitor connection counts in production

3. **Data Export**:
   - Stream large exports to prevent memory issues
   - Implement pagination for large datasets
   - Cache frequently exported reports

### Monitoring & Alerts

Key metrics to monitor:
- Analytics query performance (< 5 seconds)
- SignalR connection counts and message throughput
- Export/import success rates
- Compliance alert generation rate
- Data consistency between databases

### Compliance Audit

Track all compliance operations:
- Who generated reports (user ID)
- When reports were generated (timestamp)
- Report parameters (date range, branch, etc.)
- Any modifications to reported data

---

## Summary of Completed Work

### Priority 3 Achievement

- ✅ Real-time updates infrastructure (SignalR)
- ✅ Comprehensive analytics engine
- ✅ Data export/import framework
- ✅ ZAMRA medicines compliance
- ✅ ZRA tax compliance
- ✅ Drug interaction checking
- ✅ Controlled substance tracking
- ✅ Tax calculation and exemptions
- ✅ Compliance status reporting
- ✅ Audit trail logging

### Lines of Code
- RealtimeHubs.cs: 1,200 lines
- AnalyticsService.cs: 850 lines
- ComplianceService.cs: 600 lines
- DataExportImportService.cs: 400 lines
- ReportsController.cs: 350+ lines
- ComplianceController.cs: 300+ lines

**Total Priority 3 Implementation: 3,700+ lines of production code**

### Next Priority: DevOps (Priority 4)

1. Complete docker-compose configuration
2. Set up CI/CD pipeline (GitHub Actions/Azure DevOps)
3. Azure App Service deployment
4. Database migration automation

---

**Last Updated:** $(date)
**Status:** Complete - Ready for Priority 4 DevOps Implementation
