# PowerShell script to create demo data in SQLite database
# This script creates demo users for Admin, Cashier, Pharmacist, and Operations portals

param(
    [string]$DatabasePath = "UmiHealth.MinimalApi/umihealth.db"
)

Write-Host "Creating demo data for Umi Health..." -ForegroundColor Green

# Check if database exists
if (-not (Test-Path $DatabasePath)) {
    Write-Host "Database file not found at: $DatabasePath" -ForegroundColor Red
    exit 1
}

try {
    # Load SQLite assembly (if available)
    # For now, we'll create a simple approach using dotnet ef or direct SQL
    
    # Create SQL commands
    $sqlCommands = @"
-- Clear existing data
DELETE FROM user_roles;
DELETE FROM users;
DELETE FROM tenants;

-- Create demo tenant
INSERT INTO tenants (Id, Name, Email, Status, SubscriptionPlan, CreatedAt) VALUES 
('demo-tenant-001', 'Umi Health Demo Pharmacy', 'demo@umihealth.com', 'active', 'Enterprise', datetime('now'));

-- Create demo users
INSERT INTO users (Id, Username, Email, Password, FirstName, LastName, Role, Status, TenantId, CreatedAt) VALUES
-- Admin User
('user-admin-001', 'admin', 'admin@demo.umihealth.com', 'Demo123!', 'John', 'Administrator', 'Admin', 'active', 'demo-tenant-001', datetime('now')),

-- Cashier User
('user-cashier-001', 'cashier', 'cashier@demo.umihealth.com', 'Demo123!', 'Sarah', 'Cashier', 'Cashier', 'active', 'demo-tenant-001', datetime('now')),

-- Pharmacist User
('user-pharmacist-001', 'pharmacist', 'pharmacist@demo.umihealth.com', 'Demo123!', 'Dr. Michael', 'Pharmacist', 'Pharmacist', 'active', 'demo-tenant-001', datetime('now')),

-- Operations User
('user-operations-001', 'operations', 'operations@demo.umihealth.com', 'Demo123!', 'Lisa', 'Operations', 'Operations', 'active', 'demo-tenant-001', datetime('now')),

-- SuperAdmin User
('user-superadmin-001', 'superadmin', 'superadmin@umihealth.com', 'Demo123!', 'Super', 'Administrator', 'SuperAdmin', 'active', 'demo-tenant-001', datetime('now'));
"@

    # Save SQL to temporary file
    $tempSqlFile = [System.IO.Path]::GetTempFileName()
    $sqlCommands | Out-File -FilePath $tempSqlFile -Encoding UTF8
    
    Write-Host "Attempting to execute SQL commands..." -ForegroundColor Yellow
    
    # Try to find and execute sqlite3
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
                $result = & $path $DatabasePath ".read $tempSqlFile" 2>&1
                if ($LASTEXITCODE -eq 0) {
                    $sqliteExecuted = $true
                    break
                }
            }
        } catch {
            continue
        }
    }
    
    if (-not $sqliteExecuted) {
        Write-Host "SQLite3 not found. Creating demo data via API..." -ForegroundColor Yellow
        
        # Fallback: Create demo users via API calls
        $baseUrl = "http://localhost:5001/api/v1"
        
        $demoUsers = @(
            @{ username="admin"; email="admin@demo.umihealth.com"; password="Demo123!"; firstName="John"; lastName="Administrator"; role="Admin" },
            @{ username="cashier"; email="cashier@demo.umihealth.com"; password="Demo123!"; firstName="Sarah"; lastName="Cashier"; role="Cashier" },
            @{ username="pharmacist"; email="pharmacist@demo.umihealth.com"; password="Demo123!"; firstName="Dr. Michael"; lastName="Pharmacist"; role="Pharmacist" },
            @{ username="operations"; email="operations@demo.umihealth.com"; password="Demo123!"; firstName="Lisa"; lastName="Operations"; role="Operations" },
            @{ username="superadmin"; email="superadmin@umihealth.com"; password="Demo123!"; firstName="Super"; lastName="Administrator"; role="SuperAdmin" }
        )
        
        foreach ($user in $demoUsers) {
            try {
                $body = @{
                    email = $user.email
                    password = $user.password
                    pharmacyName = "Umi Health Demo Pharmacy"
                    adminFullName = "$($user.firstName) $($user.lastName)"
                } | ConvertTo-Json
                
                $response = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -ContentType "application/json" -Body $body -ErrorAction Stop
                Write-Host "Created user: $($user.username)" -ForegroundColor Green
            } catch {
                Write-Host "Failed to create user $($user.username): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    # Clean up temp file
    Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
    
    Write-Host "Demo data creation completed!" -ForegroundColor Green
    Write-Host "Demo Accounts:" -ForegroundColor Cyan
    Write-Host "Username: admin, Password: Demo123! (Admin Portal)" -ForegroundColor White
    Write-Host "Username: cashier, Password: Demo123! (Cashier Portal)" -ForegroundColor White
    Write-Host "Username: pharmacist, Password: Demo123! (Pharmacist Portal)" -ForegroundColor White
    Write-Host "Username: operations, Password: Demo123! (Operations Portal)" -ForegroundColor White
    Write-Host "Username: superadmin, Password: Demo123! (Super Admin Portal)" -ForegroundColor White
    
} catch {
    Write-Host "Error creating demo data: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
