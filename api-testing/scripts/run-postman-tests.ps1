#!/usr/bin/env pwsh

<#
.SYNOPSIS
Postman Collection Test Runner for Umi Health API
.DESCRIPTION
Runs the Postman collection against a target environment and generates a test report
.PARAMETER Environment
The target environment: Development, Staging, or Production
.PARAMETER BaseUrl
The base URL of the API (e.g., https://localhost:7123)
.PARAMETER AccessToken
Optional JWT access token. If not provided, will attempt to authenticate.
.EXAMPLE
./run-postman-tests.ps1 -Environment Development -BaseUrl https://localhost:7123
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:7123",
    
    [Parameter(Mandatory=$false)]
    [string]$AccessToken = "",
    
    [Parameter(Mandatory=$false)]
    [string]$CollectionPath = "api-testing/postman/collections/UmiHealth_API_Collection.postman_collection.json",
    
    [Parameter(Mandatory=$false)]
    [string]$EnvironmentFile = "api-testing/postman/environments/$Environment.postman_environment.json"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Umi Health Postman Test Runner" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check if collection file exists
if (-not (Test-Path $CollectionPath)) {
    Write-Host "❌ Collection file not found: $CollectionPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $EnvironmentFile)) {
    Write-Host "⚠️  Environment file not found: $EnvironmentFile" -ForegroundColor Yellow
    Write-Host "Proceeding with collection defaults..." -ForegroundColor Yellow
} else {
    Write-Host "📦 Collection: $CollectionPath" -ForegroundColor Green
    Write-Host "🌍 Environment: $EnvironmentFile" -ForegroundColor Green
}

Write-Host "🎯 Target URL: $BaseUrl" -ForegroundColor Green
Write-Host ""

# If no token provided, attempt to get one
if ([string]::IsNullOrEmpty($AccessToken)) {
    Write-Host "🔐 No access token provided. Attempting to authenticate..." -ForegroundColor Yellow
    
    # Create temp file for environment with base URL
    $tempEnv = @{
        id = "temp-env"
        name = "Temporary Test Environment"
        values = @(
            @{
                key = "base_url"
                value = $BaseUrl
                type = "default"
                enabled = $true
            },
            @{
                key = "api_version"
                value = "v1"
                type = "default"
                enabled = $true
            }
        )
    }
    
    $tempEnvFile = Join-Path $env:TEMP "postman_temp_env.json"
    $tempEnv | ConvertTo-Json -Depth 10 | Set-Content $tempEnvFile
    
    try {
        # Attempt to login to get a token
        Write-Host "Calling login endpoint: $BaseUrl/api/v1/auth/login" -ForegroundColor Gray
        
        $loginResponse = Invoke-RestMethod `
            -Uri "$BaseUrl/api/v1/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Headers @{ "X-Tenant-Code" = "DEV001" } `
            -Body (ConvertTo-Json @{
                email = "admin@umihealth.com"
                password = "Admin123!"
                tenantSubdomain = "DEV001"
            }) `
            -TimeoutSec 10 `
            -ErrorAction SilentlyContinue
        
        if ($loginResponse -and $loginResponse.data -and $loginResponse.data.token) {
            $AccessToken = $loginResponse.data.token
            Write-Host "✅ Successfully authenticated. Token obtained." -ForegroundColor Green
        } else {
            Write-Host "⚠️  Could not authenticate automatically. Running tests without authentication." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️  Could not connect to $BaseUrl" -ForegroundColor Yellow
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "Running tests without authentication..." -ForegroundColor Yellow
    }
}

# Create environment file for newman
$newmanEnv = @{
    id = "newman-env"
    name = "Newman Test Environment"
    values = @(
        @{
            key = "base_url"
            value = $BaseUrl
            type = "default"
            enabled = $true
        },
        @{
            key = "api_version"
            value = "v1"
            type = "default"
            enabled = $true
        }
    )
}

if (-not [string]::IsNullOrEmpty($AccessToken)) {
    $newmanEnv.values += @{
        key = "access_token"
        value = $AccessToken
        type = "secret"
        enabled = $true
    }
}

$newmanEnvFile = Join-Path $env:TEMP "newman_env.json"
$newmanEnv | ConvertTo-Json -Depth 10 | Set-Content $newmanEnvFile

Write-Host "🧪 Running Postman collection..." -ForegroundColor Cyan
Write-Host ""

# Run newman
$resultsFile = Join-Path $env:TEMP "postman-results.json"

try {
    newman run $CollectionPath `
        --environment $newmanEnvFile `
        --reporters cli,json `
        --reporter-json-export $resultsFile `
        --timeout-request 30000 `
        --timeout-script 5000 `
        --suppress-exit-code
    
    $exitCode = $LASTEXITCODE
} catch {
    Write-Host "❌ Error running newman: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📊 Test Results Summary" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan

# Parse and display results
if (Test-Path $resultsFile) {
    $results = Get-Content $resultsFile | ConvertFrom-Json
    
    if ($results.run -and $results.run.stats) {
        $stats = $results.run.stats
        $totalTests = $stats.tests.total
        $passedTests = $stats.tests.passed
        $failedTests = $stats.tests.failed
        
        Write-Host "Total Tests: $totalTests" -ForegroundColor Cyan
        Write-Host "Passed: $passedTests" -ForegroundColor Green
        Write-Host "Failed: $failedTests" -ForegroundColor Red
        Write-Host ""
        
        if ($failedTests -gt 0) {
            Write-Host "❌ Failed Test Details:" -ForegroundColor Red
            $results.run.failures | ForEach-Object {
                Write-Host "  - $($_.source.name): $($_.error.message)" -ForegroundColor Red
            }
        }
    }
    
    # Copy results to project directory
    $outputDir = Join-Path (Get-Location) "test-results"
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir | Out-Null
    }
    
    $outputFile = Join-Path $outputDir "postman-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    Copy-Item $resultsFile $outputFile
    Write-Host ""
    Write-Host "📁 Full results saved to: $outputFile" -ForegroundColor Green
}

Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Some tests failed or had issues." -ForegroundColor Yellow
    exit 1
}
