# PowerShell script to populate Zambian demo data via API
# This script uses the backend API to add Zambian sample data to the demo accounts

param(
    [string]$BaseUrl = "http://localhost:5001/api/v1"
)

Write-Host "Populating Zambian demo data via API..." -ForegroundColor Green

# Helper function to make API calls
function Invoke-ApiCall {
    param(
        [string]$Endpoint,
        [string]$Method = "POST",
        [object]$Body,
        [string]$Token = $null
    )
    
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }
    
    try {
        $bodyJson = $null
        if ($Body) {
            $bodyJson = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/$Endpoint" -Method $Method -Headers $headers -Body $bodyJson -ErrorAction Stop
        return $response
    }
    catch {
        Write-Host "API Error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Get auth token for admin2
Write-Host "Getting authentication token..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin2"
    password = "Demo123!"
}

$loginResponse = Invoke-ApiCall -Endpoint "auth/login" -Body $loginBody
if (-not $loginResponse -or -not $loginResponse.success) {
    Write-Host "Failed to authenticate. Please ensure backend is running and admin2 user exists." -ForegroundColor Red
    exit 1
}

$token = $loginResponse.data.accessToken
Write-Host "Authentication successful!" -ForegroundColor Green

# Zambian Categories Data
$categories = @(
    @{ name = "Antibiotics"; description = "Antibiotic medications for treating infections" },
    @{ name = "Pain Relief"; description = "Pain relief and anti-inflammatory medications" },
    @{ name = "Antimalarials"; description = "Medications for malaria prevention and treatment" },
    @{ name = "HIV/AIDS"; description = "Antiretroviral and HIV-related medications" },
    @{ name = "Diabetes"; description = "Diabetes management medications and supplies" },
    @{ name = "Cardiovascular"; description = "Heart and blood pressure medications" },
    @{ name = "Vitamins & Supplements"; description = "Nutritional supplements and vitamins" },
    @{ name = "First Aid"; description = "First aid supplies and wound care" }
)

# Add categories
Write-Host "Adding Zambian categories..." -ForegroundColor Yellow
$categoryCount = 0
foreach ($category in $categories) {
    $response = Invoke-ApiCall -Endpoint "categories" -Body $category -Token $token
    if ($response -and $response.success) {
        $categoryCount++
        Write-Host "✓ Added category: $($category.name)" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to add category: $($category.name)" -ForegroundColor Red
    }
}

# Zambian Suppliers Data
$suppliers = @(
    @{ name = "PharmaMed Zambia Ltd"; contactPerson = "Mr. James Banda"; phone = "+260 211 234567"; email = "sales@pharmamed.co.zm"; address = "Plot 1234, Cairo Road"; city = "Lusaka"; country = "Zambia" },
    @{ name = "ZamPharm Distributors"; contactPerson = "Ms. Grace Mulenga"; phone = "+260 215 876543"; email = "orders@zampharm.zm"; address = "Building 45, Independence Avenue"; city = "Kitwe"; country = "Zambia" },
    @{ name = "Medical Supplies Ltd"; contactPerson = "Dr. Peter Mwila"; phone = "+260 213 456789"; email = "info@medsupplies.co.zm"; address = "Stand 567, Great East Road"; city = "Lusaka"; country = "Zambia" },
    @{ name = "Global Pharma Africa"; contactPerson = "Mrs. Esther Tembo"; phone = "+260 212 345678"; email = "zambia@globalpharma.africa"; address = "Shop 23, Manda Hill"; city = "Lusaka"; country = "Zambia" }
)

# Add suppliers
Write-Host "Adding Zambian suppliers..." -ForegroundColor Yellow
$supplierCount = 0
foreach ($supplier in $suppliers) {
    $response = Invoke-ApiCall -Endpoint "suppliers" -Body $supplier -Token $token
    if ($response -and $response.success) {
        $supplierCount++
        Write-Host "✓ Added supplier: $($supplier.name)" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to add supplier: $($supplier.name)" -ForegroundColor Red
    }
}

# Zambian Customers Data
$customers = @(
    @{ firstName = "John"; lastName = "Banda"; phone = "+260 976 123456"; email = "john.banda@email.com"; address = "House 23, Chilenje"; city = "Lusaka"; dateOfBirth = "1985-03-15"; gender = "Male" },
    @{ firstName = "Mary"; lastName = "Mulenga"; phone = "+260 977 234567"; email = "mary.mulenga@email.com"; address = "Flat 12, Rhodes Park"; city = "Lusaka"; dateOfBirth = "1992-07-22"; gender = "Female" },
    @{ firstName = "Joseph"; lastName = "Mwila"; phone = "+260 966 345678"; email = "jmwila@email.com"; address = "Plot 45, Kabulonga"; city = "Lusaka"; dateOfBirth = "1978-11-08"; gender = "Male" },
    @{ firstName = "Grace"; lastName = "Tembo"; phone = "+260 955 456789"; email = "grace.tembo@email.com"; address = "House 89, Woodlands"; city = "Lusaka"; dateOfBirth = "1989-05-30"; gender = "Female" },
    @{ firstName = "Peter"; lastName = "Phiri"; phone = "+260 974 567890"; email = "peter.phiri@email.com"; address = "Stand 23, Kalingalinga"; city = "Lusaka"; dateOfBirth = "1995-09-12"; gender = "Male" },
    @{ firstName = "Esther"; lastName = "Chanda"; phone = "+260 965 678901"; email = "e.chanda@email.com"; address = "Flat 5, Northmead"; city = "Lusaka"; dateOfBirth = "1982-12-25"; gender = "Female" },
    @{ firstName = "Michael"; lastName = "Kabwe"; phone = "+260 975 789012"; email = "mkabwe@email.com"; address = "House 156, Roma"; city = "Lusaka"; dateOfBirth = "1990-02-18"; gender = "Male" },
    @{ firstName = "Agnes"; lastName = "Sichone"; phone = "+260 956 890123"; email = "agnes.s@email.com"; address = "Plot 789, Ibex Hill"; city = "Lusaka"; dateOfBirth = "1987-08-14"; gender = "Female" }
)

# Add customers
Write-Host "Adding Zambian customers..." -ForegroundColor Yellow
$customerCount = 0
foreach ($customer in $customers) {
    $response = Invoke-ApiCall -Endpoint "customers" -Body $customer -Token $token
    if ($response -and $response.success) {
        $customerCount++
        Write-Host "✓ Added customer: $($customer.firstName) $($customer.lastName)" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to add customer: $($customer.firstName) $($customer.lastName)" -ForegroundColor Red
    }
}

# Zambian Inventory Data (sample items)
$inventoryItems = @(
    @{ name = "Amoxicillin 500mg"; description = "Amoxicillin capsules 500mg"; category = "Antibiotics"; supplier = "PharmaMed Zambia Ltd"; unitPrice = 45.50; stockQuantity = 150; reorderLevel = 50; barcode = "1234567890123" },
    @{ name = "Paracetamol 500mg"; description = "Paracetamol tablets 500mg"; category = "Pain Relief"; supplier = "Medical Supplies Ltd"; unitPrice = 15.00; stockQuantity = 500; reorderLevel = 100; barcode = "1234567890126" },
    @{ name = "Coartem 80/480mg"; description = "Artemether/Lumefantrine tablets"; category = "Antimalarials"; supplier = "ZamPharm Distributors"; unitPrice = 65.00; stockQuantity = 100; reorderLevel = 40; barcode = "1234567890129" },
    @{ name = "TDF/3TC/EFV 300/300/600mg"; description = "Antiretroviral combination therapy"; category = "HIV/AIDS"; supplier = "ZamPharm Distributors"; unitPrice = 450.00; stockQuantity = 60; reorderLevel = 20; barcode = "1234567890132" },
    @{ name = "Metformin 500mg"; description = "Metformin tablets 500mg"; category = "Diabetes"; supplier = "Medical Supplies Ltd"; unitPrice = 28.50; stockQuantity = 200; reorderLevel = 60; barcode = "1234567890135" },
    @{ name = "Amlodipine 10mg"; description = "Amlodipine tablets 10mg"; category = "Cardiovascular"; supplier = "PharmaMed Zambia Ltd"; unitPrice = 45.00; stockQuantity = 150; reorderLevel = 50; barcode = "1234567890138" },
    @{ name = "Vitamin C 500mg"; description = "Ascorbic acid tablets 500mg"; category = "Vitamins & Supplements"; supplier = "Global Pharma Africa"; unitPrice = 12.50; stockQuantity = 400; reorderLevel = 100; barcode = "1234567890141" },
    @{ name = "Band-Aid Assorted"; description = "Adhesive bandages assorted sizes"; category = "First Aid"; supplier = "Global Pharma Africa"; unitPrice = 35.00; stockQuantity = 50; reorderLevel = 20; barcode = "1234567890144" }
)

# Add inventory items
Write-Host "Adding Zambian inventory items..." -ForegroundColor Yellow
$inventoryCount = 0
foreach ($item in $inventoryItems) {
    $response = Invoke-ApiCall -Endpoint "inventory" -Body $item -Token $token
    if ($response -and $response.success) {
        $inventoryCount++
        Write-Host "✓ Added inventory item: $($item.name)" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to add inventory item: $($item.name)" -ForegroundColor Red
    }
}

# Summary
Write-Host "Zambian Demo Data Population Complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Categories Added: $categoryCount" -ForegroundColor White
Write-Host "Suppliers Added: $supplierCount" -ForegroundColor White
Write-Host "Customers Added: $customerCount" -ForegroundColor White
Write-Host "Inventory Items Added: $inventoryCount" -ForegroundColor White
Write-Host "======================================" -ForegroundColor Cyan

Write-Host "Demo accounts now populated with Zambian sample data!" -ForegroundColor Green
Write-Host "Login to see the populated data:" -ForegroundColor Yellow
Write-Host "Admin: admin2 / Demo123!" -ForegroundColor White
Write-Host "Cashier: cashier / Demo123!" -ForegroundColor White
Write-Host "Pharmacist: pharmacist / Demo123!" -ForegroundColor White

Write-Host "Zambian features included:" -ForegroundColor Yellow
Write-Host "- Zambian supplier companies (PharmaMed Zambia, ZamPharm, etc.)" -ForegroundColor Cyan
Write-Host "- Zambian customer names and phone numbers (+260 prefix)" -ForegroundColor Cyan
Write-Host "- Zambian locations (Lusaka, Kitwe, Cairo Road, etc.)" -ForegroundColor Cyan
Write-Host "- Relevant medications for Zambian healthcare (malaria, HIV, etc.)" -ForegroundColor Cyan
Write-Host "- Pricing in Zambian Kwacha context" -ForegroundColor Cyan
