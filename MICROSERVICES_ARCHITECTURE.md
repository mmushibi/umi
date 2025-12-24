# Umi Health Microservices Architecture

## Overview

This document outlines the complete microservices architecture for the Umi Health pharmacy POS system, implementing all eight core services as outlined in the strategic development plan.

## Microservices Layer Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                 Microservices Layer                         │
│  Tenant Service │ User Service │ Pharmacy Service │ POS API  │
│  Inventory API  │ Reports API │ Payment Service │ Notification│
└─────────────────────────────────────────────────────────────┘
```

## 1. Tenant Service

**Controller**: `TenantsController.cs`  
**Service**: `TenantService.cs`  
**Purpose**: Multi-tenant management with branch support

### Tenant Service Key Features

- Multi-tenant isolation and management
- Branch creation and management
- Subscription plan management
- Tenant-specific settings and compliance
- Billing and compliance tracking

### Tenant Service API Endpoints

```http
GET    /api/v1/tenants/{id}                    # Get tenant details
GET    /api/v1/tenants/by-subdomain/{subdomain} # Get by subdomain
POST   /api/v1/tenants                          # Create tenant
PUT    /api/v1/tenants/{id}                    # Update tenant
DELETE /api/v1/tenants/{id}                    # Delete tenant
GET    /api/v1/tenants/{id}/branches           # List branches
POST   /api/v1/tenants/{id}/branches           # Add branch
GET    /api/v1/tenants/{id}/status             # Get tenant status
```

## 2. User Service

**Controller**: `AuthController.cs`  
**Service**: `AuthenticationService.cs`  
**Purpose**: Authentication, authorization, and user management

### User Service Key Features

- JWT-based authentication with refresh tokens
- Role-based access control (RBAC)
- Multi-tenant user isolation
- Branch-level permissions
- User profile management

### User Service API Endpoints

```http
POST   /api/v1/auth/login                      # User login
POST   /api/v1/auth/register                   # User registration
POST   /api/v1/auth/refresh                    # Refresh token
POST   /api/v1/auth/logout                     # Logout
GET    /api/v1/auth/me                         # Get current user
GET    /api/v1/auth/subscription-status        # Get subscription status
GET    /api/v1/auth/check-setup                # Check tenant setup
```

## 3. Pharmacy Service

**Controller**: `PharmacyController.cs`  
**Service**: `PharmacyService.cs`  
**Purpose**: Pharmacy-specific operations and compliance

### Pharmacy Service Key Features

- Pharmacy settings and configuration
- Supplier management
- Procurement and purchasing
- ZAMRA compliance reporting
- License and regulatory management

### Pharmacy Service API Endpoints

```http
GET    /api/v1/pharmacy/settings               # Get pharmacy settings
PUT    /api/v1/pharmacy/settings               # Update settings
GET    /api/v1/pharmacy/suppliers              # List suppliers
POST   /api/v1/pharmacy/suppliers              # Create supplier
GET    /api/v1/pharmacy/suppliers/{id}         # Get supplier
PUT    /api/v1/pharmacy/suppliers/{id}        # Update supplier
DELETE /api/v1/pharmacy/suppliers/{id}        # Delete supplier
GET    /api/v1/pharmacy/procurement            # List procurement orders
POST   /api/v1/pharmacy/procurement            # Create procurement order
POST   /api/v1/pharmacy/procurement/{id}/receive # Receive order
GET    /api/v1/pharmacy/compliance/reports     # Compliance reports
```

## 4. POS API

**Controller**: `PointOfSaleController.cs`  
**Service**: Integrated with existing POS logic  
**Purpose**: Point of sale transactions and operations

### POS API Key Features

- Product search and retrieval
- Sale processing
- Receipt generation
- Return processing
- Real-time inventory updates

### POS API Endpoints

```http
GET    /api/v1/pos/products                    # Get POS products
POST   /api/v1/pos/sales                       # Create sale
GET    /api/v1/pos/sales                       # List sales
GET    /api/v1/pos/sales/{id}                  # Get sale details
POST   /api/v1/pos/payments                    # Process payment
GET    /api/v1/pos/receipts/{id}               # Generate receipt
POST   /api/v1/pos/returns                     # Process returns
```

## 5. Inventory API

**Controller**: `InventoryController.cs`  
**Service**: `InventoryService.cs`  
**Purpose**: Stock management and tracking

### Inventory API Key Features

- Product management
- Stock level monitoring
- Batch tracking
- Expiry management
- Inter-branch transfers

### Inventory API Endpoints

```http
GET    /api/v1/inventory                       # Get inventory
GET    /api/v1/inventory/{id}                  # Get product
POST   /api/v1/inventory/products              # Add product
PUT    /api/v1/inventory/products/{id}         # Update product
DELETE /api/v1/inventory/products/{id}         # Delete product
POST   /api/v1/inventory/stock-transfer        # Transfer stock
GET    /api/v1/inventory/low-stock             # Low stock alerts
GET    /api/v1/inventory/expiring              # Expiry alerts
```

## 6. Reports API

**Controller**: `ReportsController.cs`  
**Service**: `ReportsService.cs`  
**Purpose**: Analytics and business intelligence

### Reports API Key Features

- Sales reporting and analytics
- Inventory reports
- Patient analytics
- Financial reporting
- Performance metrics
- Trend analysis

### Reports API Endpoints

```http
GET    /api/v1/reports/sales                   # Sales reports
GET    /api/v1/reports/inventory               # Inventory reports
GET    /api/v1/reports/patients                # Patient reports
GET    /api/v1/reports/prescriptions           # Prescription reports
GET    /api/v1/reports/financial               # Financial reports
GET    /api/v1/reports/analytics/dashboard      # Dashboard analytics
GET    /api/v1/reports/analytics/trends        # Trends analysis
GET    /api/v1/reports/export                  # Export reports
GET    /api/v1/reports/performance             # Performance reports
GET    /api/v1/reports/audits                  # Audit reports
```

## 7. Payment Service

**Controller**: `PaymentsController.cs`  
**Service**: Integrated with existing payment logic  
**Purpose**: Payment processing and mobile money integration

### Payment Service Key Features

- Multiple payment methods
- Mobile money integration (MTN, Airtel, Zamtel)
- Payment processing
- Transaction history
- Refund processing

### Payment Service API Endpoints

```http
POST   /api/v1/payments/process                # Process payment
GET    /api/v1/payments/methods                # Get payment methods
POST   /api/v1/payments/mobile-money            # Mobile money payment
GET    /api/v1/payments/transactions            # Transaction history
POST   /api/v1/payments/refunds                # Process refund
GET    /api/v1/payments/{id}                   # Get payment details
```

## 8. Notification Service

**Controller**: `NotificationsController.cs`  
**Service**: `NotificationService.cs`  
**Purpose**: Alerts and communications

### Notification Service Key Features

- Real-time notifications
- Email, SMS, and push notifications
- Alert management
- User notification preferences
- System alerts and compliance notifications

### Notification Service API Endpoints

```http
GET    /api/v1/notifications                   # Get notifications
GET    /api/v1/notifications/{id}              # Get notification
POST   /api/v1/notifications/{id}/mark-read    # Mark as read
POST   /api/v1/notifications/mark-all-read     # Mark all as read
DELETE /api/v1/notifications/{id}              # Delete notification
POST   /api/v1/notifications                   # Create notification
POST   /api/v1/notifications/broadcast          # Broadcast notification
GET    /api/v1/notifications/unread-count      # Unread count
GET    /api/v1/notifications/settings          # Get settings
PUT    /api/v1/notifications/settings          # Update settings
POST   /api/v1/notifications/test              # Test notification
```

## Data Transfer Objects (DTOs)

### Pharmacy DTOs (`PharmacyDTOs.cs`)

- `PharmacySettingsDto`
- `SupplierDto`
- `ProcurementOrderDto`
- `ProcurementItemDto`
- `ComplianceReportDto`
- `ExpiringProductDto`

### Reports DTOs (`ReportsDTOs.cs`)

- `SalesReportDto`
- `InventoryReportDto`
- `PatientsReportDto`
- `PrescriptionsReportDto`
- `FinancialReportDto`
- `DashboardAnalyticsDto`
- `TrendsAnalyticsDto`
- `PerformanceReportDto`
- `AuditReportDto`

### Notifications DTOs (`NotificationsDTOs.cs`)

- `NotificationDto`
- `NotificationSettingsDto`
- `LowStockItemDto`
- `ExpiringItemDto`

## Multi-Tenant Architecture

### Tenant Isolation Strategy

- **Database Level**: Row-Level Security (RLS) with PostgreSQL
- **Application Level**: Tenant context middleware
- **API Level**: Tenant-based routing and filtering

### Branch Support

- Hierarchical tenant structure (Organization → Branches)
- Branch-specific data isolation
- Cross-branch operations with permissions
- Centralized procurement with branch distribution

## Security & Compliance

### Authentication & Authorization

- JWT tokens with 15-minute expiry
- Refresh tokens with 7-day expiry
- Role-based access control
- Branch-level permissions

### Zambian Compliance

- ZAMRA regulatory compliance
- Controlled substance tracking
- Audit trail maintenance
- Data privacy and protection

## Technology Stack

- **Framework**: .NET 8.0
- **Architecture**: Clean Architecture with CQRS
- **Database**: PostgreSQL 15+ with RLS
- **Authentication**: JWT with refresh tokens
- **Caching**: Redis
- **Logging**: Serilog with structured logging
- **API Documentation**: Swagger/OpenAPI 3.0

## Integration Points

### External Integrations

- Mobile Money Providers (MTN Mobile Money, Airtel Money, Zamtel Money)
- ZAMRA Systems
- Payment Gateways
- SMS/Email Services

### Internal Communication

- Service-to-service communication via HTTP
- Event-driven architecture for notifications
- Shared database with tenant isolation
- Distributed caching with Redis

## Deployment Considerations

### Containerization

- Docker containers for each service
- Kubernetes orchestration
- Service mesh for inter-service communication
- Load balancing and auto-scaling

### Monitoring & Observability

- Application monitoring with Application Insights
- Infrastructure monitoring
- Centralized logging
- Health checks and metrics

## Next Steps

1. **Database Schema Implementation**: Create PostgreSQL schema with RLS policies
2. **Service Registration**: Configure dependency injection and service discovery
3. **API Gateway**: Implement API gateway for routing and load balancing
4. **Testing**: Comprehensive unit and integration testing
5. **Documentation**: Complete OpenAPI documentation
6. **Deployment**: Set up CI/CD pipeline and production deployment

---

*Document Version: 1.0*  
*Last Updated: December 2024*  
*Architecture Status: Implemented*
