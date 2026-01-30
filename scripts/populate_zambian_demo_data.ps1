# PowerShell script to populate Zambian demo data
# This script executes the SQL script to add Zambian sample data to the demo accounts

param(
    [string]$DatabasePath = "UmiHealth.MinimalApi/umihealth.db",
    [string]$SqlScriptPath = "scripts/populate_zambian_demo_data.sql"
)

Write-Host "Populating Zambian demo data..." -ForegroundColor Green

# Check if database exists
if (-not (Test-Path $DatabasePath)) {
    Write-Host "Database file not found at: $DatabasePath" -ForegroundColor Red
    exit 1
}

# Check if SQL script exists
if (-not (Test-Path $SqlScriptPath)) {
    Write-Host "SQL script not found at: $SqlScriptPath" -ForegroundColor Red
    exit 1
}

try {
    Write-Host "Reading SQL script..." -ForegroundColor Yellow
    $sqlContent = Get-Content -Path $SqlScriptPath -Raw
    
    Write-Host "Executing SQL commands..." -ForegroundColor Yellow
    
    # Try different SQLite executables
    $sqlitePaths = @(
        "sqlite3",
        "C:\Program Files\SQLite3\sqlite3.exe",
        "C:\Program Files (x86)\SQLite3\sqlite3.exe",
        "C:\ProgramData\chocolatey\bin\sqlite3.exe"
    )
    
    $sqliteExecuted = $false
    foreach ($path in $sqlitePaths) {
        try {
            if (Test-Path $path) {
                Write-Host "Using SQLite at: $path" -ForegroundColor Cyan
                
                # Execute SQL script
                $result = & $path $DatabasePath $sqlContent 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $sqliteExecuted = $true
                    Write-Host "SQL script executed successfully!" -ForegroundColor Green
                    break
                } else {
                    Write-Host "SQLite execution failed: $result" -ForegroundColor Yellow
                }
            }
        } catch {
            Write-Host "Failed to use SQLite at $path" -ForegroundColor Yellow
            continue
        }
    }
    
    if (-not $sqliteExecuted) {
        Write-Host "SQLite3 not found. Please install SQLite3 or add it to PATH." -ForegroundColor Red
        Write-Host "You can download SQLite3 from: https://www.sqlite.org/download.html" -ForegroundColor Yellow
        exit 1
    }
    
    # Verify data was inserted
    Write-Host "Verifying data insertion..." -ForegroundColor Yellow
    
    # Check counts
    $categoryCount = & $sqlitePaths[0] $DatabasePath "SELECT COUNT(*) FROM categories WHERE TenantId = 'demo-tenant-001';" 2>$null
    $supplierCount = & $sqlitePaths[0] $DatabasePath "SELECT COUNT(*) FROM suppliers WHERE TenantId = 'demo-tenant-001';" 2>$null
    $customerCount = & $sqlitePaths[0] $DatabasePath "SELECT COUNT(*) FROM customers WHERE TenantId = 'demo-tenant-001';" 2>$null
    $inventoryCount = & $sqlitePaths[0] $DatabasePath "SELECT COUNT(*) FROM inventory WHERE TenantId = 'demo-tenant-001';" 2>$null
    $salesCount = & $sqlitePaths[0] $DatabasePath "SELECT COUNT(*) FROM sales WHERE TenantId = 'demo-tenant-001';" 2>$null
    $prescriptionCount = & $sqlitePaths[0] $DatabasePath "SELECT COUNT(*) FROM prescriptions WHERE TenantId = 'demo-tenant-001';" 2>$null
    
    Write-Host "Zambian Demo Data Population Complete!" -ForegroundColor Green
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Categories: $categoryCount" -ForegroundColor White
    Write-Host "Suppliers: $supplierCount" -ForegroundColor White
    Write-Host "Customers: $customerCount" -ForegroundColor White
    Write-Host "Inventory Items: $inventoryCount" -ForegroundColor White
    Write-Host "Sales Records: $salesCount" -ForegroundColor White
    Write-Host "Prescriptions: $prescriptionCount" -ForegroundColor White
    Write-Host "======================================" -ForegroundColor Cyan
    
    Write-Host "Demo accounts now populated with Zambian sample data!" -ForegroundColor Green
    Write-Host "Login to see the populated data:" -ForegroundColor Yellow
    Write-Host "Admin: admin2 / Demo123!" -ForegroundColor White
    Write-Host "Cashier: cashier / Demo123!" -ForegroundColor White
    Write-Host "Pharmacist: pharmacist / Demo123!" -ForegroundColor White
    
} catch {
    Write-Host "Error populating demo data: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
