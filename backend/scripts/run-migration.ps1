# UmiHealth Database Migration Script
# This script runs the initial database migration

param(
    [string]$ConnectionString = "Host=localhost;Database=umihealth;Username=postgres;Password=root",
    [string]$MigrationFile = "src\UmiHealth.Infrastructure\Migrations\InitialCreate.sql"
)

Write-Host "Starting UmiHealth Database Migration..." -ForegroundColor Green

# Set PostgreSQL path
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

# Check if psql is available
if (-not (Test-Path $psqlPath)) {
    Write-Host "Error: PostgreSQL client (psql) not found at $psqlPath. Please install PostgreSQL." -ForegroundColor Red
    exit 1
}

try {
    $psqlVersion = & $psqlPath --version
    Write-Host "Found PostgreSQL client: $psqlVersion" -ForegroundColor Green
}
catch {
    Write-Host "Error: PostgreSQL client (psql) not found. Please install PostgreSQL." -ForegroundColor Red
    exit 1
}

# Check if migration file exists
$fullPath = Join-Path (Get-Location) $MigrationFile
if (-not (Test-Path $fullPath)) {
    Write-Host "Error: Migration file not found at $fullPath" -ForegroundColor Red
    exit 1
}

Write-Host "Running migration from: $fullPath" -ForegroundColor Yellow

# Set PGPASSWORD environment variable for authentication
$env:PGPASSWORD = "root"

try {
    # Run the migration
    & $psqlPath -h localhost -U postgres -d umihealth -f $fullPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migration completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
catch {
    Write-Host "Error running migration: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Clean up environment variable
    Remove-Item env:PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host "Database migration process completed." -ForegroundColor Green
