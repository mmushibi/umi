# Umi Health Pharmacy POS System - Setup Script
# Multi-tenant Pharmacy Point of Sale System Setup Automation
# Author: Umi Health Development Team
# Version: 1.0

param(
    [switch]$InstallDocker,
    [switch]$SetupDatabase,
    [switch]$CreateBackend,
    [switch]$RunTests,
    [switch]$StartDev,
    [switch]$Help,
    [string]$Environment = "Development"
)

# Color scheme for output
$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Header = "Magenta"
}

# Project configuration
$ProjectConfig = @{
    ProjectName = "UmiHealth"
    SolutionName = "UmiHealth.sln"
    BackendPath = "backend"
    FrontendPath = "frontend"
    DatabaseName = "umi_health_pharmacy"
    DockerComposeFile = "docker-compose.yml"
    PostmanCollection = "postman/Umi_Health_API.postman_collection.json"
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Show-Banner {
    Write-ColorOutput @"
========================================
    UMI HEALTH PHARMACY POS SYSTEM
    Multi-Tenant Pharmacy Management
========================================
"@ -Color "Header"
    Write-ColorOutput "Setup Script v1.0" -Color "Info"
    Write-ColorOutput "Environment: $Environment" -Color "Info"
    Write-Host ""
}

function Show-Help {
    Write-ColorOutput "Umi Health Pharmacy POS Setup Script Usage:" -Color "Header"
    Write-Host ""
    Write-ColorOutput "Available Parameters:" -Color "Info"
    Write-Host "  -InstallDocker    : Install Docker Desktop"
    Write-Host "  -SetupDatabase    : Setup PostgreSQL database"
    Write-Host "  -CreateBackend    : Create C# backend project structure"
    Write-Host "  -RunTests         : Run all tests"
    Write-Host "  -StartDev         : Start development environment"
    Write-Host "  -Help             : Show this help message"
    Write-Host "  -Environment      : Set environment (Default: Development)"
    Write-Host ""
    Write-ColorOutput "Examples:" -Color "Info"
    Write-Host "  .\setup-umi-health.ps1 -InstallDocker -SetupDatabase -CreateBackend"
    Write-Host "  .\setup-umi-health.ps1 -StartDev"
    Write-Host "  .\setup-umi-health.ps1 -RunTests"
    Write-Host ""
}

function Test-Prerequisites {
    Write-ColorOutput "Checking system prerequisites..." -Color "Info"
    
    $issues = @()
    
    # Check PowerShell version
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        $issues += "PowerShell 7+ required. Current: $($PSVersionTable.PSVersion)"
    }
    
    # Check .NET SDK
    try {
        $dotnetVersion = & dotnet --version 2>$null
        if ($dotnetVersion -notmatch "^8\.") {
            $issues += ".NET 8.0 SDK required. Current: $dotnetVersion"
        }
    } catch {
        $issues += ".NET SDK not found. Please install .NET 8.0 SDK"
    }
    
    # Check Node.js
    try {
        $nodeVersion = & node --version 2>$null
        if ($nodeVersion -notmatch "^v[1][8-9]\.") {
            $issues += "Node.js 18+ required. Current: $nodeVersion"
        }
    } catch {
        $issues += "Node.js not found. Please install Node.js 18+"
    }
    
    # Check Git
    try {
        & git --version >$null 2>&1
    } catch {
        $issues += "Git not found. Please install Git"
    }
    
    if ($issues.Count -gt 0) {
        Write-ColorOutput "Prerequisites check failed:" -Color "Error"
        $issues | ForEach-Object { Write-ColorOutput "  - $_" -Color "Warning" }
        return $false
    }
    
    Write-ColorOutput "All prerequisites satisfied!" -Color "Success"
    return $true
}

function Install-DockerDesktop {
    Write-ColorOutput "Installing Docker Desktop..." -Color "Info"
    
    if (Get-Command "docker" -ErrorAction SilentlyContinue) {
        Write-ColorOutput "Docker is already installed." -Color "Success"
        return
    }
    
    try {
        # Download Docker Desktop
        $dockerUrl = "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe"
        $dockerInstaller = "$env:TEMP\DockerDesktopInstaller.exe"
        
        Write-ColorOutput "Downloading Docker Desktop..." -Color "Info"
        Invoke-WebRequest -Uri $dockerUrl -OutFile $dockerInstaller
        
        Write-ColorOutput "Installing Docker Desktop (this may take a while)..." -Color "Info"
        Start-Process -FilePath $dockerInstaller -Args "install --quiet --accept-license" -Wait
        
        Remove-Item $dockerInstaller -Force
        
        Write-ColorOutput "Docker Desktop installed successfully!" -Color "Success"
        Write-ColorOutput "Please restart your computer and run Docker Desktop manually." -Color "Warning"
    } catch {
        Write-ColorOutput "Failed to install Docker Desktop: $_" -Color "Error"
        Write-ColorOutput "Please install Docker Desktop manually from: https://www.docker.com/products/docker-desktop" -Color "Warning"
    }
}

function Create-BackendStructure {
    Write-ColorOutput "Creating C# backend project structure..." -Color "Info"
    
    $backendPath = $ProjectConfig.BackendPath
    $solutionName = $ProjectConfig.SolutionName
    
    # Create solution
    if (-not (Test-Path $solutionName)) {
        Write-ColorOutput "Creating solution..." -Color "Info"
        & dotnet new sln -n $ProjectConfig.ProjectName
    }
    
    # Create backend directory structure
    $directories = @(
        "$backendPath/src/UmiHealth.Api",
        "$backendPath/src/UmiHealth.Core",
        "$backendPath/src/UmiHealth.Infrastructure",
        "$backendPath/src/UmiHealth.Application",
        "$backendPath/src/UmiHealth.Domain",
        "$backendPath/tests/UmiHealth.Api.Tests",
        "$backendPath/tests/UmiHealth.Application.Tests",
        "$backendPath/tests/UmiHealth.Infrastructure.Tests"
    )
    
    $directories | ForEach-Object {
        if (-not (Test-Path $_)) {
            New-Item -ItemType Directory -Path $_ -Force
            Write-ColorOutput "Created directory: $_" -Color "Success"
        }
    }
    
    # Create projects
    $projects = @(
        @{ Path = "$backendPath/src/UmiHealth.Api"; Type = "webapi"; Name = "UmiHealth.Api" },
        @{ Path = "$backendPath/src/UmiHealth.Core"; Type = "classlib"; Name = "UmiHealth.Core" },
        @{ Path = "$backendPath/src/UmiHealth.Infrastructure"; Type = "classlib"; Name = "UmiHealth.Infrastructure" },
        @{ Path = "$backendPath/src/UmiHealth.Application"; Type = "classlib"; Name = "UmiHealth.Application" },
        @{ Path = "$backendPath/src/UmiHealth.Domain"; Type = "classlib"; Name = "UmiHealth.Domain" }
    )
    
    $projects | ForEach-Object {
        if (-not (Test-Path "$($_.Path)/$($_.Name).csproj")) {
            Write-ColorOutput "Creating project: $($_.Name)" -Color "Info"
            & dotnet new $($_.Type) -n $_.Name -o $_.Path
            & dotnet sln add "$($_.Path)/$($_.Name).csproj"
        }
    }
    
    # Create test projects
    $testProjects = @(
        @{ Path = "$backendPath/tests/UmiHealth.Api.Tests"; Type = "xunit"; Name = "UmiHealth.Api.Tests" },
        @{ Path = "$backendPath/tests/UmiHealth.Application.Tests"; Type = "xunit"; Name = "UmiHealth.Application.Tests" },
        @{ Path = "$backendPath/tests/UmiHealth.Infrastructure.Tests"; Type = "xunit"; Name = "UmiHealth.Infrastructure.Tests" }
    )
    
    $testProjects | ForEach-Object {
        if (-not (Test-Path "$($_.Path)/$($_.Name).csproj")) {
            Write-ColorOutput "Creating test project: $($_.Name)" -Color "Info"
            & dotnet new $($_.Type) -n $_.Name -o $_.Path
            & dotnet sln add "$($_.Path)/$($_.Name).csproj"
        }
    }
    
    Write-ColorOutput "Backend structure created successfully!" -Color "Success"
}

function Setup-PostgreSQL {
    Write-ColorOutput "Setting up PostgreSQL database..." -Color "Info"
    
    # Check if Docker is running
    try {
        & docker version >$null 2>&1
    } catch {
        Write-ColorOutput "Docker is not running. Please start Docker Desktop." -Color "Error"
        return
    }
    
    # Create docker-compose.yml for PostgreSQL
    $dockerComposeContent = @"
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: umi_health_postgres
    environment:
      POSTGRES_DB: $($ProjectConfig.DatabaseName)
      POSTGRES_USER: umi_admin
      POSTGRES_PASSWORD: umi_secure_password_2024
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./database/init.sql:/docker-entrypoint-initdb.d/init.sql
    networks:
      - umi_network

  redis:
    image: redis:7-alpine
    container_name: umi_health_redis
    ports:
      - "6379:6379"
    networks:
      - umi_network

  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: umi_health_pgadmin
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@umihealth.com
      PGADMIN_DEFAULT_PASSWORD: admin123
    ports:
      - "5050:80"
    depends_on:
      - postgres
    networks:
      - umi_network

volumes:
  postgres_data:

networks:
  umi_network:
    driver: bridge
"@
    
    Set-Content -Path $ProjectConfig.DockerComposeFile -Value $dockerComposeContent
    Write-ColorOutput "Created docker-compose.yml" -Color "Success"
    
    # Create database init script
    $initScriptPath = "database/init.sql"
    if (-not (Test-Path "database")) {
        New-Item -ItemType Directory -Path "database" -Force
    }
    
    $initScriptContent = @"
-- Umi Health Pharmacy POS Database Initialization
-- Multi-tenant pharmacy management system

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create schemas for multi-tenancy
CREATE SCHEMA IF NOT EXISTS shared;
CREATE SCHEMA IF NOT EXISTS tenant_data;

-- Shared tables (system-wide)
CREATE TABLE shared.tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    subdomain VARCHAR(100) UNIQUE NOT NULL,
    database_name VARCHAR(100) UNIQUE NOT NULL,
    status VARCHAR(50) DEFAULT 'active',
    subscription_plan VARCHAR(50) DEFAULT 'basic',
    max_branches INTEGER DEFAULT 1,
    max_users INTEGER DEFAULT 10,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE shared.branches (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES shared.tenants(id),
    name VARCHAR(255) NOT NULL,
    code VARCHAR(50) NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE shared.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES shared.tenants(id),
    branch_id UUID REFERENCES shared.branches(id),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    role VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    last_login TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Sample tenant data
INSERT INTO shared.tenants (name, subdomain, database_name, subscription_plan, max_branches, max_users) VALUES
('Demo Pharmacy', 'demo', 'umi_demo', 'premium', 5, 50),
('Test Pharmacy', 'test', 'umi_test', 'basic', 1, 10);

-- Sample branch data
INSERT INTO shared.branches (tenant_id, name, code, address, phone, email) VALUES
((SELECT id FROM shared.tenants WHERE subdomain = 'demo'), 'Main Branch', 'BR001', '123 Main St, Lusaka', '+260 977 123 456', 'demo@umihealth.com'),
((SELECT id FROM shared.tenants WHERE subdomain = 'test'), 'Test Branch', 'BR001', '456 Test St, Lusaka', '+260 977 987 654', 'test@umihealth.com');

-- Sample users
INSERT INTO shared.users (tenant_id, branch_id, email, password_hash, first_name, last_name, role) VALUES
((SELECT id FROM shared.tenants WHERE subdomain = 'demo'), (SELECT id FROM shared.branches WHERE code = 'BR001' AND tenant_id = (SELECT id FROM shared.tenants WHERE subdomain = 'demo')), 'admin@demo.com', '$2a$11$Q9zKzKzKzKzKzKzKzKzKzO', 'Admin', 'User', 'admin'),
((SELECT id FROM shared.tenants WHERE subdomain = 'demo'), (SELECT id FROM shared.branches WHERE code = 'BR001' AND tenant_id = (SELECT id FROM shared.tenants WHERE subdomain = 'demo')), 'pharmacist@demo.com', '$2a$11$Q9zKzKzKzKzKzKzKzKzKzO', 'Pharmacist', 'User', 'pharmacist');

COMMIT;
"@
    
    Set-Content -Path $initScriptPath -Value $initScriptContent
    Write-ColorOutput "Created database initialization script" -Color "Success"
    
    # Start PostgreSQL container
    Write-ColorOutput "Starting PostgreSQL containers..." -Color "Info"
    & docker-compose up -d postgres redis pgadmin
    
    Write-ColorOutput "PostgreSQL setup completed!" -Color "Success"
    Write-ColorOutput "Database: $($ProjectConfig.DatabaseName)" -Color "Info"
    Write-ColorOutput "PgAdmin: http://localhost:5050 (admin@umihealth.com / admin123)" -Color "Info"
}

function Create-AppSettings {
    Write-ColorOutput "Creating application settings..." -Color "Info
    
    $appSettingsPath = "$($ProjectConfig.BackendPath)/src/UmiHealth.Api/appsettings.json"
    $appSettingsDevPath = "$($ProjectConfig.BackendPath)/src/UmiHealth.Api/appsettings.Development.json"
    
    $appSettingsContent = @{
        Logging = @{
            LogLevel = @{
                Default = "Information"
                Microsoft.AspNetCore = "Warning"
            }
        }
        ConnectionStrings = @{
            DefaultConnection = "Host=localhost;Database=$($ProjectConfig.DatabaseName);Username=umi_admin;Password=umi_secure_password_2024"
            RedisConnection = "localhost:6379"
        }
        Jwt = @{
            Key = "umi_health_jwt_secret_key_2024_very_long_and_secure"
            Issuer = "UmiHealth"
            Audience = "UmiHealthUsers"
            ExpiryMinutes = 60
        }
        MultiTenancy = @{
            Enabled = $true
            DefaultSchema = "tenant_data"
        }
        AllowedHosts = "*"
        CORS = @{
            AllowedOrigins = @("http://localhost:3000", "http://localhost:8080")
        }
    }
    
    $appSettingsDevContent = @{
        Logging = @{
            LogLevel = @{
                Default = "Debug"
                Microsoft.AspNetCore = "Information"
            }
        }
        ConnectionStrings = @{
            DefaultConnection = "Host=localhost;Database=$($ProjectConfig.DatabaseName);Username=umi_admin;Password=umi_secure_password_2024"
            RedisConnection = "localhost:6379"
        }
    }
    
    $appSettingsPath | ForEach-Object {
        if (-not (Test-Path $_)) {
            $appSettingsContent | ConvertTo-Json -Depth 10 | Set-Content $_
            Write-ColorOutput "Created: $_" -Color "Success"
        }
    }
    
    $appSettingsDevPath | ForEach-Object {
        if (-not (Test-Path $_)) {
            $appSettingsDevContent | ConvertTo-Json -Depth 10 | Set-Content $_
            Write-ColorOutput "Created: $_" -Color "Success"
        }
    }
}

function Install-BackendPackages {
    Write-ColorOutput "Installing NuGet packages..." -Color "Info
    
    $packages = @(
        # API packages
        @{ Project = "UmiHealth.Api"; Packages = @("Microsoft.AspNetCore.Authentication.JwtBearer", "Microsoft.AspNetCore.Cors", "Swashbuckle.AspNetCore", "Serilog.AspNetCore", "Serilog.Sinks.PostgreSQL") },
        
        # Infrastructure packages
        @{ Project = "UmiHealth.Infrastructure"; Packages = @("Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.Design", "Npgsql.EntityFrameworkCore.PostgreSQL", "StackExchange.Redis", "Dapper") },
        
        # Application packages
        @{ Project = "UmiHealth.Application"; Packages = @("MediatR", "FluentValidation", "AutoMapper") },
        
        # Core packages
        @{ Project = "UmiHealth.Core"; Packages = @("System.IdentityModel.Tokens.Jwt", "Microsoft.Extensions.Identity.Core") }
    )
    
    $packages | ForEach-Object {
        $projectPath = "$($ProjectConfig.BackendPath)/src/$($_.Project)"
        $_.Packages | ForEach-Object {
            Write-ColorOutput "Installing $_ for $($_.Project)..." -Color "Info"
            & dotnet add "$projectPath/$($_.Project).csproj" package $_
        }
    }
    
    Write-ColorOutput "NuGet packages installed!" -Color "Success"
}

function Create-PostmanCollection {
    Write-ColorOutput "Creating Postman collection..." -Color "Info
    
    $postmanDir = "postman"
    if (-not (Test-Path $postmanDir)) {
        New-Item -ItemType Directory -Path $postmanDir -Force
    }
    
    $postmanCollection = @{
        info = @{
            name = "Umi Health Pharmacy POS API"
            description = "Multi-tenant Pharmacy Point of Sale System API"
            schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
        }
        auth = @{
            type = "bearer"
            bearer = @{
                type = "token"
                token = "{{jwt_token}}"
            }
        }
        variable = @(
            @{ name = "base_url"; value = "http://localhost:5000/api/v1" }
            @{ name = "jwt_token"; value = "" }
        )
    }
    
    Set-Content -Path $ProjectConfig.PostmanCollection -Value ($postmanCollection | ConvertTo-Json -Depth 10)
    Write-ColorOutput "Created Postman collection: $($ProjectConfig.PostmanCollection)" -Color "Success"
}

function Run-AllTests {
    Write-ColorOutput "Running all tests..." -Color "Info
    
    $testProjects = @(
        "$($ProjectConfig.BackendPath)/tests/UmiHealth.Api.Tests",
        "$($ProjectConfig.BackendPath)/tests/UmiHealth.Application.Tests",
        "$($ProjectConfig.BackendPath)/tests/UmiHealth.Infrastructure.Tests"
    )
    
    $testProjects | ForEach-Object {
        if (Test-Path "$_/$_ -replace '.*/', '' -replace '-.*', '' -replace '/', ''Tests.csproj") {
            Write-ColorOutput "Running tests in $_..." -Color "Info"
            & dotnet test $_ --no-build --verbosity normal
        }
    }
    
    Write-ColorOutput "Test run completed!" -Color "Success"
}

function Start-Development {
    Write-ColorOutput "Starting development environment..." -Color "Info"
    
    # Start Docker containers
    if (Test-Path $ProjectConfig.DockerComposeFile) {
        Write-ColorOutput "Starting Docker containers..." -Color "Info"
        & docker-compose up -d
    }
    
    # Start backend API
    $apiPath = "$($ProjectConfig.BackendPath)/src/UmiHealth.Api"
    if (Test-Path $apiPath) {
        Write-ColorOutput "Starting API server..." -Color "Info"
        Start-Job -ScriptBlock {
            param($path)
            Set-Location $path
            & dotnet run --environment Development
        } -ArgumentList $apiPath
    }
    
    Write-ColorOutput "Development environment started!" -Color "Success"
    Write-ColorOutput "API: http://localhost:5000" -Color "Info"
    Write-ColorOutput "Swagger: http://localhost:5000/swagger" -Color "Info"
    Write-ColorOutput "PgAdmin: http://localhost:5050" -Color "Info"
}

function Main {
    Show-Banner
    
    if ($Help) {
        Show-Help
        return
    }
    
    if (-not (Test-Prerequisites)) {
        Write-ColorOutput "Please install missing prerequisites and try again." -Color "Error"
        return
    }
    
    try {
        if ($InstallDocker) {
            Install-DockerDesktop
        }
        
        if ($CreateBackend) {
            Create-BackendStructure
            Create-AppSettings
            Install-BackendPackages
            Create-PostmanCollection
        }
        
        if ($SetupDatabase) {
            Setup-PostgreSQL
        }
        
        if ($RunTests) {
            Run-AllTests
        }
        
        if ($StartDev) {
            Start-Development
        }
        
        if (-not ($InstallDocker -or $CreateBackend -or $SetupDatabase -or $RunTests -or $StartDev)) {
            Show-Help
        }
        
    } catch {
        Write-ColorOutput "Setup failed: $_" -Color "Error"
        exit 1
    }
    
    Write-ColorOutput "Setup completed successfully!" -Color "Success"
}

# Run main function
Main
