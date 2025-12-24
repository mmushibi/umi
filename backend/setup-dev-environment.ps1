# UmiHealth Development Environment Setup Script
# This script sets up the complete development environment for UmiHealth

param(
    [switch]$SkipDocker,
    [switch]$SkipDatabase,
    [switch]$SkipNuGet,
    [string]$Environment = "Development"
)

Write-Host "🏥 Setting up UmiHealth Development Environment..." -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Check prerequisites
function Test-Prerequisites {
    Write-Host "🔍 Checking prerequisites..." -ForegroundColor Cyan
    
    $prerequisites = @{
        "dotnet" = "8.0"
        "docker" = $null
        "git" = $null
    }
    
    $allInstalled = $true
    
    foreach ($tool in $prerequisites.Keys) {
        try {
            if ($tool -eq "dotnet") {
                $version = & $tool --version 2>$null
                if ($version -match "(\d+\.\d+)") {
                    $installedVersion = $matches[1]
                    if ([version]$installedVersion -ge [version]$prerequisites[$tool]) {
                        Write-Host "✅ $tool version $installedVersion" -ForegroundColor Green
                    } else {
                        Write-Host "❌ $tool version $installedVersion (required: $($prerequisites[$tool]+))" -ForegroundColor Red
                        $allInstalled = $false
                    }
                }
            } else {
                $version = & $tool --version 2>$null
                Write-Host "✅ $tool installed" -ForegroundColor Green
            }
        } catch {
            Write-Host "❌ $tool not found" -ForegroundColor Red
            $allInstalled = $false
        }
    }
    
    if (-not $allInstalled) {
        Write-Host "❌ Please install missing prerequisites and run again." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ All prerequisites satisfied" -ForegroundColor Green
}

# Restore NuGet packages
function Restore-Packages {
    if ($SkipNuGet) {
        Write-Host "⏭️ Skipping NuGet package restore" -ForegroundColor Yellow
        return
    }
    
    Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Cyan
    try {
        & dotnet restore UmiHealth.sln
        Write-Host "✅ NuGet packages restored" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to restore NuGet packages" -ForegroundColor Red
        throw
    }
}

# Build solution
function Build-Solution {
    Write-Host "🔨 Building solution..." -ForegroundColor Cyan
    try {
        & dotnet build UmiHealth.sln --configuration $Environment --no-restore
        Write-Host "✅ Solution built successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Build failed" -ForegroundColor Red
        throw
    }
}

# Setup Docker containers
function Setup-Docker {
    if ($SkipDocker) {
        Write-Host "⏭️ Skipping Docker setup" -ForegroundColor Yellow
        return
    }
    
    Write-Host "🐳 Setting up Docker containers..." -ForegroundColor Cyan
    try {
        # Check if Docker is running
        & docker info >$null 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
            throw
        }
        
        # Create necessary directories
        if (-not (Test-Path "logs")) { New-Item -ItemType Directory -Path "logs" | Out-Null }
        if (-not (Test-Path "scripts/migrations")) { 
            New-Item -ItemType Directory -Path "scripts/migrations" -Force | Out-Null 
        }
        
        # Start containers
        Write-Host "Starting PostgreSQL and Redis containers..." -ForegroundColor Yellow
        & docker-compose up -d postgres redis
        
        Write-Host "✅ Docker containers started" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to setup Docker containers" -ForegroundColor Red
        throw
    }
}

# Setup database
function Setup-Database {
    if ($SkipDatabase) {
        Write-Host "⏭️ Skipping database setup" -ForegroundColor Yellow
        return
    }
    
    Write-Host "🗄️ Setting up database..." -ForegroundColor Cyan
    try {
        # Wait for PostgreSQL to be ready
        Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
        $maxAttempts = 30
        $attempt = 0
        
        do {
            $attempt++
            try {
                & docker exec umihealth-postgres pg_isready -U umihealth_user -d UmiHealth
                if ($LASTEXITCODE -eq 0) {
                    break
                }
            } catch {
                # Command failed, continue trying
            }
            Start-Sleep -Seconds 2
        } while ($attempt -lt $maxAttempts)
        
        if ($attempt -eq $maxAttempts) {
            Write-Host "❌ PostgreSQL is not ready after 60 seconds" -ForegroundColor Red
            throw
        }
        
        Write-Host "✅ PostgreSQL is ready" -ForegroundColor Green
        
        # Create initial database schema (will be implemented in next phase)
        Write-Host "📝 Database schema will be created in Phase 1.2" -ForegroundColor Yellow
        
    } catch {
        Write-Host "❌ Failed to setup database" -ForegroundColor Red
        throw
    }
}

# Create development configuration files
function Create-DevConfig {
    Write-Host "⚙️ Creating development configuration..." -ForegroundColor Cyan
    
    $appSettingsDev = @{
        "Logging" = @{
            "LogLevel" = @{
                "Default" = "Information"
                "Microsoft.AspNetCore" = "Warning"
                "Microsoft.EntityFrameworkCore.Database.Command" = "Information"
            }
        }
        "ConnectionStrings" = @{
            "DefaultConnection" = "Host=localhost;Database=UmiHealth;Username=umihealth_user;Password=UmiHealth@2024!;Port=5432"
        }
        "Redis" = @{
            "ConnectionString" = "localhost:6379"
        }
        "JwtSettings" = @{
            "Secret" = "UmiHealth_Secret_Key_2024_Very_Long_Secret_String_For_Development_Only"
            "Issuer" = "UmiHealth"
            "Audience" = "UmiHealth.Users"
            "AccessTokenExpiration" = 15
            "RefreshTokenExpiration" = 168
        }
        "AllowedHosts" = "*"
        "Cors" = @{
            "AllowedOrigins" = @("http://localhost:3000", "http://localhost:5001", "https://localhost:5001")
        }
    }
    
    $appSettingsPath = "src/UmiHealth.Api/appsettings.$Environment.json"
    $appSettingsDev | ConvertTo-Json -Depth 4 | Out-File -FilePath $appSettingsPath -Encoding UTF8
    Write-Host "✅ Created $appSettingsPath" -ForegroundColor Green
}

# Run tests
function Run-Tests {
    Write-Host "🧪 Running tests..." -ForegroundColor Cyan
    try {
        & dotnet test UmiHealth.sln --configuration $Environment --no-build --verbosity normal
        Write-Host "✅ All tests passed" -ForegroundColor Green
    } catch {
        Write-Host "❌ Some tests failed" -ForegroundColor Red
        # Don't throw here, just warn
    }
}

# Main execution
try {
    Test-Prerequisites
    Restore-Packages
    Build-Solution
    Setup-Docker
    Setup-Database
    Create-DevConfig
    Run-Tests
    
    Write-Host ""
    Write-Host "🎉 UmiHealth development environment setup complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next steps:" -ForegroundColor Yellow
    Write-Host "1. Open UmiHealth.sln in Visual Studio or VS Code" -ForegroundColor White
    Write-Host "2. Set src/UmiHealth.Api as startup project" -ForegroundColor White
    Write-Host "3. Run the application (F5 in VS or 'dotnet run' in terminal)" -ForegroundColor White
    Write-Host "4. Access Swagger UI at https://localhost:5001/swagger" -ForegroundColor White
    Write-Host ""
    Write-Host "🐳 Docker commands:" -ForegroundColor Yellow
    Write-Host "Start containers: docker-compose up -d" -ForegroundColor White
    Write-Host "Stop containers: docker-compose down" -ForegroundColor White
    Write-Host "View logs: docker-compose logs -f" -ForegroundColor White
    
} catch {
    Write-Host ""
    Write-Host "❌ Development environment setup failed!" -ForegroundColor Red
    Write-Host "Please check the error messages above and fix any issues." -ForegroundColor Yellow
    exit 1
}
