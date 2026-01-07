# Umi Health API Documentation

## Overview

The Umi Health API provides comprehensive functionality for pharmacy management, including patient records, prescriptions, inventory, sales, and multi-tenant operations.

## Base URL

- **Production**: `https://api.umihealth.com`
- **Staging**: `https://staging-api.umihealth.com`
- **Development**: `https://dev-api.umihealth.com`

## Authentication Overview

All API requests require authentication using JWT (JSON Web Tokens) in the Authorization header:

```http
Authorization: Bearer <your-jwt-token>
```

### Token Claims

- `nameid`: User ID
- `email`: User email
- `role`: User role (SuperAdmin, Admin, Pharmacist, Cashier, Operations)
- `tenant_id`: Tenant ID (for multi-tenancy)
- `branch_id`: Branch ID (for multi-branch access)
- `permissions`: User permissions array

## Rate Limiting

- **Standard**: 100 requests per minute per user
- **Burst**: 20 requests per second
- Rate limit headers are included in all responses

## API Endpoints

### Authentication

#### POST /api/v1/auth/login

Authenticates a user and returns JWT tokens.

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "password123",
  "rememberMe": true
}
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4=",
  "expiresIn": 900,
  "user": {
    "id": "user-guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Pharmacist",
    "tenantId": "tenant-guid",
    "branchId": "branch-guid"
  }
}
```

#### POST /api/v1/auth/refresh

Refreshes an access token using a refresh token.

**Request Body:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4="
}
```

#### POST /api/v1/auth/logout

Logs out a user and invalidates the refresh token.

**Headers:** `Authorization: Bearer <token>`

### Patients

#### GET /api/v1/patients

Retrieves a paginated list of patients.

**Parameters:**

- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 50)
- `search` (string, optional): Search term for patient names
- `branchId` (guid, optional): Filter by branch

**Headers:** `Authorization: Bearer <token>`

**Response:**

```json
{
  "data": [
    {
      "id": "patient-guid",
      "patientNumber": "P001234",
      "firstName": "Jane",
      "lastName": "Smith",
      "dateOfBirth": "1985-06-15",
      "gender": "F",
      "phone": "+260123456789",
      "email": "jane.smith@email.com",
      "address": "123 Main St, Lusaka, Zambia",
      "status": "active",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalItems": 150,
    "totalPages": 3
  }
}
```

#### POST /api/v1/patients

Creates a new patient record.

**Request Body:**

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "dateOfBirth": "1990-03-20",
  "gender": "M",
  "phone": "+260987654321",
  "email": "john.doe@email.com",
  "address": "456 Oak Ave, Lusaka, Zambia",
  "emergencyContact": {
    "name": "Jane Doe",
    "relationship": "Spouse",
    "phone": "+260987654322"
  }
}
```

#### GET /api/v1/patients/{id}

Retrieves a specific patient by ID.

#### PUT /api/v1/patients/{id}

Updates a patient record.

#### DELETE /api/v1/patients/{id}

Deletes a patient record (requires Admin role).

### Prescriptions

#### GET /api/v1/prescriptions

Retrieves prescriptions with filtering options.

**Parameters:**

- `patientId` (guid, optional): Filter by patient
- `status` (string, optional): Filter by status (pending, dispensed, expired)
- `dateFrom` (date, optional): Filter by date range
- `dateTo` (date, optional): Filter by date range

#### POST /api/v1/prescriptions

Creates a new prescription.

**Request Body:**

```json
{
  "patientId": "patient-guid",
  "prescriberId": "prescriber-guid",
  "datePrescribed": "2024-01-15",
  "notes": "Patient presents with bacterial infection",
  "diagnosis": "Upper respiratory infection",
  "items": [
    {
      "productId": "product-guid",
      "productName": "Amoxicillin 500mg",
      "dosage": "500mg",
      "frequency": "3 times daily",
      "duration": "7 days",
      "quantity": 21,
      "instructions": "Take with food"
    }
  ]
}
```

#### POST /api/v1/prescriptions/{id}/dispense

Dispenses a prescription (requires Pharmacist role).

### Inventory

#### GET /api/v1/inventory

Retrieves inventory items with filtering.

**Parameters:**

- `branchId` (guid, optional): Filter by branch
- `categoryId` (guid, optional): Filter by category
- `lowStock` (boolean, optional): Show only low stock items
- `expiring` (boolean, optional): Show only expiring items

#### POST /api/v1/inventory/adjust

Adjusts inventory quantities.

**Request Body:**

```json
{
  "productId": "product-guid",
  "branchId": "branch-guid",
  "adjustmentType": "increase", // increase, decrease, transfer
  "quantity": 50,
  "reason": "Stock receipt from supplier",
  "referenceNumber": "PO-2024-001"
}
```

### Sales

#### GET /api/v1/sales

Retrieves sales records.

#### POST /api/v1/sales

Creates a new sale.

**Request Body:**

```json
{
  "patientId": "patient-guid",
  "cashierId": "cashier-guid",
  "items": [
    {
      "productId": "product-guid",
      "quantity": 2,
      "unitPrice": 25.50,
      "discount": 0.00
    }
  ],
  "paymentMethod": "cash",
  "prescriptionIds": ["prescription-guid"]
}
```

### Multi-Tenant Operations

#### GET /api/v1/tenants

Retrieves tenant information (requires SuperAdmin or Operations role).

#### POST /api/v1/tenants

Creates a new tenant (requires SuperAdmin role).

**Request Body:**

```json
{
  "name": "Umi Health Pharmacy",
  "subdomain": "umihealth",
  "databaseName": "umihealth",
  "subscriptionPlan": "premium",
  "maxBranches": 5,
  "maxUsers": 50,
  "settings": {
    "timezone": "Africa/Lusaka",
    "currency": "ZMW",
    "dateFormat": "dd/MM/yyyy"
  },
  "billingInfo": {
    "companyName": "MediCare Ltd",
    "taxId": "ZMW-123456789",
    "billingAddress": "123 Business Ave, Lusaka"
  }
}
```

#### GET /api/v1/branches

Retrieves branch information.

#### POST /api/v1/branches

Creates a new branch.

### Reports

#### GET /api/v1/reports/sales-analytics

Retrieves sales analytics data.

**Parameters:**

- `period` (string): daily, weekly, monthly, yearly
- `branchId` (guid, optional): Filter by branch
- `dateFrom` (date, optional): Start date
- `dateTo` (date, optional): End date

**Response:**

```json
{
  "period": "monthly",
  "data": [
    {
      "date": "2026-01-01",
      "totalSales": 12500.00,
      "totalPrescriptions": 45,
      "uniquePatients": 32,
      "averageTransactionValue": 278.00
    }
  ],
  "summary": {
    "totalSales": 12500.00,
    "totalPrescriptions": 45,
    "growthRate": 12.5
  }
}
```

## Error Handling

### Standard Error Response Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input data",
    "details": [
      {
        "field": "email",
        "message": "Invalid email format"
      }
    ]
  },
  "timestamp": "2026-01-07T10:30:00Z",
  "path": "/api/v1/patients",
  "requestId": "req-guid"
}
```

### Common Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| VALIDATION_ERROR | 400 | Request validation failed |
| UNAUTHORIZED | 401 | Authentication required or invalid |
| FORBIDDEN | 403 | Insufficient permissions |
| NOT_FOUND | 404 | Resource not found |
| CONFLICT | 409 | Resource already exists |
| RATE_LIMITED | 429 | Too many requests |
| INTERNAL_ERROR | 500 | Server error |

## Data Models

### Patient Model

```json
{
  "id": "guid",
  "patientNumber": "string",
  "firstName": "string",
  "lastName": "string",
  "dateOfBirth": "date",
  "gender": "M|F|O",
  "phone": "string",
  "email": "string",
  "address": "string",
  "emergencyContact": {
    "name": "string",
    "relationship": "string",
    "phone": "string"
  },
  "medicalHistory": {},
  "allergies": [],
  "status": "active|inactive",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### Product Model

```json
{
  "id": "guid",
  "sku": "string",
  "name": "string",
  "genericName": "string",
  "category": "string",
  "description": "string",
  "manufacturer": "string",
  "strength": "string",
  "form": "tablet|capsule|liquid|cream",
  "requiresPrescription": boolean,
  "controlledSubstance": boolean,
  "pricing": {
    "costPrice": decimal,
    "sellingPrice": decimal,
    "vatRate": decimal
  },
  "status": "active|inactive|discontinued"
}
```

## SDK and Client Libraries

### JavaScript/TypeScript

```bash
npm install @umihealth/api-client
```

```javascript
import { UmiHealthApi } from '@umihealth/api-client';

const api = new UmiHealthApi({
  baseUrl: 'https://api.umihealth.com',
  apiKey: 'your-api-key'
});

const patients = await api.patients.list({
  page: 1,
  pageSize: 50
});
```

### C #

```bash
dotnet add package UmiHealth.Api.Client
```

```csharp
using UmiHealth.Api.Client;

var client = new UmiHealthClient("https://api.umihealth.com", "your-api-key");
var patients = await client.Patients.GetAsync(page: 1, pageSize: 50);
```

## Webhooks

Umi Health supports webhooks for real-time notifications:

### Supported Events

- `patient.created`
- `prescription.created`
- `prescription.dispensed`
- `sale.completed`
- `inventory.low_stock`
- `inventory.expiring`

### Webhook Configuration

```json
{
  "url": "https://your-app.com/webhooks/umihealth",
  "events": ["prescription.created", "sale.completed"],
  "secret": "your-webhook-secret"
}
```

## Testing

### Sandbox Environment

For testing, use the sandbox environment:

- **URL**: `https://sandbox-api.umihealth.com`
- **Test Credentials**: Available in developer portal

### Postman Collection

Download our Postman collection from:
[https://docs.umihealth.com/postman-collection](https://docs.umihealth.com/postman-collection)

## Support

- **API Documentation**: [https://docs.umihealth.com](https://docs.umihealth.com)
- **Developer Portal**: [https://developers.umihealth.com](https://developers.umihealth.com)
- **Support Email**: <api-support@umihealth.com>
- **Status Page**: [https://status.umihealth.com](https://status.umihealth.com)

## Changelog

### v2.1.0 (2026-01-07)

- Added multi-tenant support
- Enhanced security headers
- Added webhook support
- Improved error responses

### v2.0.0 (2026-01-07)

- Complete API redesign
- Added JWT authentication
- Enhanced pagination
- Added real-time updates

---

*Last updated: January 7, 2026*
