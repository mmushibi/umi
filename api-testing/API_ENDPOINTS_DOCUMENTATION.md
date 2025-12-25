# Umi Health API Endpoints Documentation

## Overview

This document provides a comprehensive overview of all available API endpoints in the Umi Health system. The API follows RESTful conventions and uses JSON for data exchange.

## Base Configuration

- **Base URL**: `https://localhost:7123` (Development)
- **API Version**: v1
- **Authentication**: Bearer Token (JWT)
- **Content-Type**: `application/json`
- **Tenant Header**: `X-Tenant-Code`

## Authentication Endpoints

### POST /api/v1/auth/login
Authenticates a user and returns JWT tokens.

**Headers:**
- `Content-Type: application/json`
- `X-Tenant-Code: {tenant_code}`

**Body:**
```json
{
  "email": "user@example.com",
  "password": "password123",
  "tenantSubdomain": "tenant001"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "jwt_token_here",
    "refreshToken": "refresh_token_here",
    "user": {
      "id": "user_id",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe"
    },
    "expiresAt": "2024-12-25T00:00:00Z"
  }
}
```

### POST /api/v1/auth/register
Registers a new user account.

### POST /api/v1/auth/refresh
Refreshes an expired JWT token.

### POST /api/v1/auth/logout
Logs out the current user and invalidates the token.

## Tenant Management

### GET /api/v1/tenants
Retrieves a paginated list of tenants.

**Query Parameters:**
- `page` (int): Page number (default: 1)
- `pageSize` (int): Items per page (default: 20)

### POST /api/v1/tenants
Creates a new tenant.

**Body:**
```json
{
  "name": "Pharmacy Name",
  "subdomain": "pharmacy001",
  "subscriptionPlan": "basic",
  "contactEmail": "contact@pharmacy.com",
  "phoneNumber": "+1234567890"
}
```

### GET /api/v1/tenants/{id}
Retrieves a specific tenant by ID.

### PUT /api/v1/tenants/{id}
Updates tenant information.

## Branch Management

### GET /api/v1/branch
Retrieves branches for a tenant.

**Query Parameters:**
- `tenantId` (string): Filter by tenant ID

### POST /api/v1/branch
Creates a new branch.

**Body:**
```json
{
  "name": "Main Branch",
  "address": "123 Main St, City, State 12345",
  "phoneNumber": "+1234567890",
  "email": "branch@pharmacy.com",
  "tenantId": "tenant_id"
}
```

## User Management

### GET /api/v1/users
Retrieves users, optionally filtered by branch.

**Query Parameters:**
- `branchId` (string, optional): Filter by branch ID

### POST /api/v1/users
Creates a new user.

**Body:**
```json
{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "roleId": "role_id",
  "branchId": "branch_id"
}
```

### PUT /api/v1/users/{id}
Updates user information.

## Roles & Permissions

### GET /api/v1/roles
Retrieves all available roles.

### POST /api/v1/roles
Creates a new role.

**Body:**
```json
{
  "name": "Pharmacist",
  "description": "Can manage prescriptions and inventory",
  "permissions": ["read:patients", "write:prescriptions", "manage:inventory"]
}
```

## Pharmacy Operations

### GET /api/v1/pharmacy/settings
Retrieves pharmacy settings for the current tenant.

### PUT /api/v1/pharmacy/settings
Updates pharmacy settings.

**Body:**
```json
{
  "pharmacyName": "Umi Health Pharmacy",
  "address": "456 Health Ave, Medical City, MC 67890",
  "phoneNumber": "+1987654321",
  "email": "pharmacy@umihealth.com"
}
```

## Inventory Management

### GET /api/v1/inventory
Retrieves inventory items with pagination and search.

**Query Parameters:**
- `page` (int): Page number
- `pageSize` (int): Items per page
- `search` (string): Search term for product names

### POST /api/v1/inventory
Creates a new inventory product.

**Body:**
```json
{
  "name": "Medication Name",
  "description": "Product description",
  "category": "Pain Relief",
  "price": 19.99,
  "quantity": 100,
  "sku": "MED-001"
}
```

### PUT /api/v1/inventory/{id}/stock
Updates stock levels for a product.

**Body:**
```json
{
  "quantity": 150,
  "operation": "add" // or "remove", "set"
}
```

## Products Catalog

### GET /api/v1/products
Retrieves product catalog with pagination.

### POST /api/v1/products
Creates a new product in the catalog.

**Body:**
```json
{
  "name": "Acetaminophen",
  "genericName": "Acetaminophen",
  "category": "Pain Relief",
  "strength": "500mg",
  "form": "Tablet",
  "price": 9.99
}
```

## Patient Management

### GET /api/v1/patients
Retrieves patients with pagination and search.

**Query Parameters:**
- `page` (int): Page number
- `pageSize` (int): Items per page
- `search` (string): Search term for patient names

### POST /api/v1/patients
Creates a new patient record.

**Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "dateOfBirth": "1980-01-01",
  "gender": "Male",
  "phoneNumber": "+1122334455",
  "email": "john.doe@email.com",
  "address": "789 Patient St, Health City, HC 11223"
}
```

### GET /api/v1/patients/{id}
Retrieves a specific patient by ID.

## Prescriptions

### GET /api/v1/prescriptions
Retrieves prescriptions, optionally filtered by patient.

**Query Parameters:**
- `patientId` (string, optional): Filter by patient ID
- `page` (int): Page number

### POST /api/v1/prescriptions
Creates a new prescription.

**Body:**
```json
{
  "patientId": "patient_id",
  "doctorId": "doctor_id",
  "medications": [
    {
      "productId": "product_id",
      "dosage": "1 tablet",
      "frequency": "every 6 hours",
      "duration": "7 days",
      "quantity": 28
    }
  ],
  "notes": "Take with food"
}
```

## Point of Sale

### POST /api/v1/pointofsale
Processes a new sale transaction.

**Body:**
```json
{
  "patientId": "patient_id",
  "items": [
    {
      "productId": "product_id",
      "quantity": 2,
      "unitPrice": 19.99
    }
  ],
  "paymentMethod": "cash",
  "cashierId": "cashier_id"
}
```

### GET /api/v1/pointofsale
Retrieves sales history.

**Query Parameters:**
- `page` (int): Page number
- `pageSize` (int): Items per page
- `dateFrom` (date): Start date filter
- `dateTo` (date): End date filter

## Sales & Transactions

### GET /api/v1/sales
Retrieves sales records.

### POST /api/v1/sales
Creates a new sale record.

## Payments

### GET /api/v1/payments
Retrieves payment records.

### POST /api/v1/payments
Processes a new payment.

**Body:**
```json
{
  "saleId": "sale_id",
  "amount": 39.98,
  "paymentMethod": "credit_card",
  "reference": "PAY-123456"
}
```

## Suppliers

### GET /api/v1/suppliers
Retrieves supplier list.

### POST /api/v1/suppliers
Creates a new supplier.

**Body:**
```json
{
  "name": "Medical Supplies Inc",
  "contactPerson": "Jane Smith",
  "phoneNumber": "+1998877665",
  "email": "contact@supplies.com",
  "address": "321 Supplier Blvd, Supply City, SC 44556"
}
```

## Purchase Orders

### GET /api/v1/purchaseorders
Retrieves purchase orders.

### POST /api/v1/purchaseorders
Creates a new purchase order.

**Body:**
```json
{
  "supplierId": "supplier_id",
  "items": [
    {
      "productId": "product_id",
      "quantity": 50,
      "unitPrice": 15.99
    }
  ],
  "expectedDeliveryDate": "2024-12-31"
}
```

## Notifications

### GET /api/v1/notifications
Retrieves user notifications.

### POST /api/v1/notifications
Creates a new notification.

**Body:**
```json
{
  "title": "Stock Alert",
  "message": "Product XYZ is running low on stock",
  "type": "warning",
  "recipientId": "user_id"
}
```

## Reports

### GET /api/v1/reports/sales
Generates sales report.

**Query Parameters:**
- `startDate` (date): Report start date
- `endDate` (date): Report end date

### GET /api/v1/reports/inventory
Generates inventory report.

### GET /api/v1/reports/financial
Generates financial report.

**Query Parameters:**
- `period` (string): Report period (daily, weekly, monthly, yearly)

## Background Jobs

### GET /api/v1/backgroundjobs
Retrieves background job status.

### POST /api/v1/backgroundjobs/trigger
Triggers a background job.

**Body:**
```json
{
  "jobType": "inventory_sync",
  "parameters": {}
}
```

## Operations Management

### GET /api/v1/operations/dashboard
Retrieves operations dashboard data.

### GET /api/v1/operations/health
Checks system health status.

## Subscriptions

### GET /api/v1/subscriptions
Retrieves subscription information.

### POST /api/v1/subscriptions
Creates a new subscription.

**Body:**
```json
{
  "tenantId": "tenant_id",
  "planId": "plan_id",
  "startDate": "2024-12-24",
  "billingCycle": "monthly"
}
```

## Account Management

### GET /api/v1/account
Retrieves current user account information.

### PUT /api/v1/account
Updates current user account.

### POST /api/v1/account/changepassword
Changes user password.

**Body:**
```json
{
  "currentPassword": "old_password",
  "newPassword": "new_password"
}
```

## API Gateway

### GET /api/v1/apigateway/status
Retrieves API gateway status.

### GET /api/v1/apigateway/routes
Retrieves configured service routes.

## Error Handling

The API uses standard HTTP status codes:

- `200` - Success
- `201` - Created
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `500` - Internal Server Error

Error responses follow this format:
```json
{
  "success": false,
  "message": "Error description",
  "errors": [
    {
      "field": "field_name",
      "message": "Field validation error"
    }
  ]
}
```

## Rate Limiting

API endpoints may have rate limiting applied. Exceeded limits will result in HTTP 429 responses.

## Testing with Postman

1. Import the `CompleteApi.postman_collection.json` collection
2. Import the `Complete.postman_environment.json` environment
3. Update environment variables with your specific values
4. Run the authentication endpoints first to get tokens
5. Use the auto-generated tokens for subsequent requests

## Common Headers

Most endpoints require these headers:
```
Authorization: Bearer {access_token}
Content-Type: application/json
X-Tenant-Code: {tenant_code}
```

---

**Last Updated**: December 24, 2024
**Version**: 1.0.0
