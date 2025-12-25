# Frontend Integration Guide

## Overview
This guide shows how to integrate the frontend with the Umi Health backend API.

## Setup

### 1. Include API Client and Auth Manager

Add to your main HTML file (in `<head>` or before other scripts):

```html
<!-- API Configuration -->
<script>
    window.API_BASE_URL = 'http://localhost:5000/api/v1';
</script>

<!-- API Client -->
<script src="/js/api-client.js"></script>

<!-- Authentication Manager -->
<script src="/js/auth-manager.js"></script>
```

### 2. Update HTML Forms

#### Login Form
Replace your login form submission:

```html
<form id="loginForm">
    <input type="email" id="email" name="email" required>
    <input type="password" id="password" name="password" required>
    <input type="text" id="tenantSubdomain" name="tenantSubdomain" required>
    <button type="submit">Login</button>
</form>

<script>
document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const tenantSubdomain = document.getElementById('tenantSubdomain').value;
    
    const result = await authManager.login(email, password, tenantSubdomain);
    
    if (result.success) {
        // Redirect to dashboard
        window.location.href = '/portals/admin/home.html';
    } else {
        // Show error
        alert(result.message);
    }
});
</script>
```

### 3. Update Dashboard Code

#### Get User Profile
```javascript
// Instead of localStorage
const user = authManager.getCurrentUser();
console.log('Current user:', user);
console.log('User ID:', authManager.getUserId());
console.log('Tenant ID:', authManager.getTenantId());
console.log('Branch ID:', authManager.getBranchId());
```

#### Check Authentication
```javascript
if (!authManager.isAuthenticated()) {
    window.location.href = '/signin.html';
}
```

#### Check Permissions
```javascript
if (authManager.hasRole('Admin')) {
    // Show admin features
}

if (authManager.hasPermission('manage_inventory')) {
    // Show inventory management
}
```

### 4. Update Data Fetching

#### Before (using LocalStorage)
```javascript
// Old way
function loadInventory() {
    const inventory = JSON.parse(localStorage.getItem('inventory') || '[]');
    displayInventory(inventory);
}
```

#### After (using API)
```javascript
// New way
async function loadInventory() {
    const branchId = authManager.getBranchId();
    const response = await apiClient.getInventory(branchId);
    
    if (response.success) {
        displayInventory(response.data);
        
        // Handle pagination
        if (response.pagination) {
            updatePagination(response.pagination);
        }
    } else {
        showError(response.message);
    }
}

// Call on page load
document.addEventListener('DOMContentLoaded', loadInventory);
```

### 5. Update Patient Management

#### Load Patients
```javascript
async function loadPatients(page = 1) {
    const response = await apiClient.getPatients(page, 50);
    
    if (response.success) {
        const patients = response.data;
        const pagination = response.pagination;
        
        // Display patients
        displayPatients(patients);
        
        // Update pagination controls
        updatePaginationControls(pagination);
    } else {
        showError(response.message);
    }
}
```

#### Search Patients
```javascript
async function searchPatients(searchTerm) {
    const response = await apiClient.searchPatients(searchTerm);
    
    if (response.success) {
        displayPatients(response.data);
    } else {
        showError(response.message);
    }
}

// Add to search input
document.getElementById('patientSearch').addEventListener('input', (e) => {
    if (e.target.value.length >= 2) {
        searchPatients(e.target.value);
    }
});
```

#### Create Patient
```javascript
async function createPatient(patientData) {
    const response = await apiClient.createPatient(patientData);
    
    if (response.success) {
        showSuccess('Patient created successfully');
        loadPatients();
    } else {
        showError(response.message);
    }
}

// In form submit handler
document.getElementById('patientForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const formData = new FormData(e.target);
    const patientData = {
        firstName: formData.get('firstName'),
        lastName: formData.get('lastName'),
        dateOfBirth: formData.get('dob'),
        gender: formData.get('gender'),
        phone: formData.get('phone'),
        email: formData.get('email'),
        address: formData.get('address')
    };
    
    await createPatient(patientData);
});
```

### 6. Update Inventory Management

#### Get Branch Inventory
```javascript
async function loadBranchInventory() {
    const branchId = authManager.getBranchId();
    const response = await apiClient.getInventoryByBranch(branchId);
    
    if (response.success) {
        displayInventory(response.data);
    }
}
```

#### Update Stock
```javascript
async function updateProductStock(productId, newQuantity) {
    const branchId = authManager.getBranchId();
    const response = await apiClient.updateStock(productId, branchId, newQuantity);
    
    if (response.success) {
        showSuccess('Stock updated');
        loadBranchInventory();
    } else {
        showError(response.message);
    }
}
```

#### Check Low Stock
```javascript
async function checkLowStockProducts() {
    const branchId = authManager.getBranchId();
    const response = await apiClient.getLowStockProducts(branchId);
    
    if (response.success) {
        const lowStockProducts = response.data;
        displayLowStockAlert(lowStockProducts);
    }
}
```

### 7. Update Point of Sale

#### Create Sale
```javascript
async function createSale(saleItems) {
    const tenantId = authManager.getTenantId();
    const branchId = authManager.getBranchId();
    const userId = authManager.getUserId();
    
    const saleData = {
        saleNumber: `SAL-${Date.now()}`,
        tenantId,
        branchId,
        cashierId: userId,
        items: saleItems,
        subtotal: calculateSubtotal(saleItems),
        taxAmount: calculateTax(saleItems),
        totalAmount: calculateTotal(saleItems)
    };
    
    const response = await apiClient.createSale(saleData);
    
    if (response.success) {
        const saleId = response.data.id;
        showSuccess('Sale created');
        return saleId;
    } else {
        showError(response.message);
        return null;
    }
}
```

#### Process Payment
```javascript
async function processPayment(saleId, paymentMethod, amount) {
    const response = await apiClient.processPayment(saleId, {
        paymentMethod,
        amount,
        reference: `PAY-${Date.now()}`
    });
    
    if (response.success) {
        showSuccess('Payment processed');
        await printReceipt(saleId);
    } else {
        showError(response.message);
    }
}
```

#### Get Receipt
```javascript
async function printReceipt(saleId) {
    const response = await apiClient.getReceipt(saleId);
    
    if (response.success) {
        const receiptData = response.data;
        displayReceipt(receiptData);
        window.print();
    }
}
```

### 8. Update Reports

#### Generate Sales Report
```javascript
async function generateSalesReport(startDate, endDate) {
    const branchId = authManager.getBranchId();
    const response = await apiClient.getSalesReport(branchId, startDate, endDate);
    
    if (response.success) {
        displaySalesReport(response.data);
        exportToCSV(response.data, 'sales-report.csv');
    }
}
```

#### Dashboard Analytics
```javascript
async function loadDashboard() {
    const branchId = authManager.getBranchId();
    const response = await apiClient.getDashboardAnalytics(branchId);
    
    if (response.success) {
        const analytics = response.data;
        updateDashboard(analytics);
    }
}
```

### 9. Error Handling

Create a global error handler:

```javascript
function showError(message) {
    // Show error toast/alert
    const errorDiv = document.createElement('div');
    errorDiv.className = 'alert alert-danger';
    errorDiv.textContent = message;
    errorDiv.style.position = 'fixed';
    errorDiv.style.top = '20px';
    errorDiv.style.right = '20px';
    errorDiv.style.zIndex = '9999';
    
    document.body.appendChild(errorDiv);
    
    setTimeout(() => {
        errorDiv.remove();
    }, 5000);
}

function showSuccess(message) {
    const successDiv = document.createElement('div');
    successDiv.className = 'alert alert-success';
    successDiv.textContent = message;
    successDiv.style.position = 'fixed';
    successDiv.style.top = '20px';
    successDiv.style.right = '20px';
    successDiv.style.zIndex = '9999';
    
    document.body.appendChild(successDiv);
    
    setTimeout(() => {
        successDiv.remove();
    }, 5000);
}
```

### 10. Subscribe to Auth Changes

```javascript
// Listen for auth changes
authManager.onAuthChange((auth) => {
    if (auth.isAuthenticated) {
        console.log('User logged in:', auth.user);
        updateUIForLoggedInUser(auth.user);
    } else {
        console.log('User logged out');
        redirectToLogin();
    }
});
```

### 11. Handle Token Expiration

The API client automatically handles token refresh. If a request fails with 401, it will:
1. Use the refresh token to get a new access token
2. Retry the original request
3. Update stored tokens

If refresh fails, the user is redirected to login.

## Environment Setup

### Development
```html
<script>
    window.API_BASE_URL = 'http://localhost:5000/api/v1';
</script>
```

### Production
```html
<script>
    window.API_BASE_URL = 'https://api.umihealth.com/api/v1';
</script>
```

## API Responses

All API responses follow this format:

**Success:**
```json
{
    "requestId": "uuid",
    "statusCode": 200,
    "message": "Success",
    "data": { ... },
    "pagination": {
        "pageNumber": 1,
        "pageSize": 50,
        "totalRecords": 100,
        "totalPages": 2,
        "hasNextPage": true,
        "hasPreviousPage": false
    },
    "timestamp": "2025-12-24T...",
    "elapsedMilliseconds": 45
}
```

**Error:**
```json
{
    "requestId": "uuid",
    "statusCode": 400,
    "status": "ValidationError",
    "message": "One or more validation errors occurred",
    "errors": {
        "email": ["Email is required"],
        "password": ["Password must be at least 8 characters"]
    },
    "path": "/api/v1/auth/login",
    "method": "POST",
    "timestamp": "2025-12-24T...",
    "elapsedMilliseconds": 12
}
```

## Testing the Integration

Use Postman or curl to test:

```bash
# Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@umihealth.com",
    "password": "password123",
    "tenantSubdomain": "umihealth"
  }'

# Get patients
curl -X GET http://localhost:5000/api/v1/patients \
  -H "Authorization: Bearer <token>"

# Create patient
curl -X POST http://localhost:5000/api/v1/patients \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "phone": "+260...",
    "email": "john@example.com"
  }'
```

## Common Issues

### CORS Errors
Ensure the backend has CORS configured correctly:
```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### Token Not Persisting
Clear browser cache and check localStorage permissions.

### 401 Unauthorized
- Check token in browser DevTools
- Verify token hasn't expired
- Check Authorization header format

## Next Steps

1. Update all form submissions to use API client
2. Replace all localStorage calls with API endpoints
3. Test each feature with the backend
4. Handle loading states during API calls
5. Add proper error handling to all forms
6. Implement pagination for list views
7. Add confirmation dialogs for destructive operations
