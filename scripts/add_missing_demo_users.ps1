# PowerShell script to add missing demo users to SQLite database
# This script adds admin and pharmacist users to the SQLite database used by the backend

param(
    [string]$DatabasePath = "UmiHealth.MinimalApi/umihealth.db"
)

Write-Host "Adding missing demo users to SQLite database..." -ForegroundColor Green

# Check if database exists
if (-not (Test-Path $DatabasePath)) {
    Write-Host "Database file not found at: $DatabasePath" -ForegroundColor Red
    exit 1
}

try {
    # SQL commands to add missing demo users
    $sqlCommands = @"
-- Add admin user if not exists
INSERT OR IGNORE INTO users (Id, Username, Email, Password, FirstName, LastName, Role, Status, TenantId, CreatedAt) VALUES 
('user-admin-001', 'admin', 'admin@demo.umihealth.com', 'Demo123!', 'John', 'Administrator', 'Admin', 'active', 'demo-tenant-001', datetime('now'));

-- Add pharmacist user if not exists  
INSERT OR IGNORE INTO users (Id, Username, Email, Password, FirstName, LastName, Role, Status, TenantId, CreatedAt) VALUES
('user-pharmacist-001', 'pharmacist', 'pharmacist@demo.umihealth.com', 'Demo123!', 'Dr. Michael', 'Pharmacist', 'Pharmacist', 'active', 'demo-tenant-001', datetime('now'));

-- Add tenant if not exists
INSERT OR IGNORE INTO tenants (Id, Name, Email, Status, SubscriptionPlan, CreatedAt) VALUES
('demo-tenant-001', 'Umi Health Demo Pharmacy', 'demo@umihealth.com', 'active', 'Enterprise', datetime('now'));
"@

    # Save SQL to temporary file
    $tempSqlFile = [System.IO.Path]::GetTempFileName()
    $sqlCommands | Out-File -FilePath $tempSqlFile -Encoding UTF8
    
    Write-Host "Executing SQL commands..." -ForegroundColor Yellow
    
    # Try different SQLite executables
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
        Write-Host "SQLite3 not found. Creating demo users via API..." -ForegroundColor Yellow
        
        # Fallback: Create demo users via API calls
        $baseUrl = "http://localhost:5001/api/v1"
        
        $demoUsers = @(
            @{ username="admin", email="admin@demo.umihealth.com", password="Demo123!", firstName="John", lastName="Administrator", role="Admin" },
            @{ username="pharmacist", email="pharmacist@demo.umihealth.com", password="Demo123!", firstName="Dr. Michael", lastName="Pharmacist", role="Pharmacist" }
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
    
    Write-Host "Demo users creation completed!" -ForegroundColor Green
    Write-Host "Demo Accounts:" -ForegroundColor Cyan
    Write-Host "Username: admin, Password: Demo123! (Admin Portal)" -ForegroundColor White
    Write-Host "Username: cashier, Password: Demo123! (Cashier Portal)" -ForegroundColor White
    Write-Host "Username: pharmacist, Password: Demo123! (Pharmacist Portal)" -ForegroundColor White
    
} catch {
    Write-Host "Error creating demo users: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
