# Umi Health API Test Runner Script
# This script runs comprehensive API tests using Newman (Postman CLI)

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("development", "staging", "production")]
    [string]$Environment = "development",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("authentication", "tenant-management", "pharmacy-operations", "pos", "patient-management", "all")]
    [string]$Collection = "all",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./test-results",
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReports,
    
    [Parameter(Mandatory=$false)]
    [switch]$Parallel
)

# Configuration
$ApiBaseUrl = switch ($Environment) {
    "development" { "https://localhost:7123" }
    "staging" { "https://staging-api.umihealth.com" }
    "production" { "https://api.umihealth.com" }
}

$CollectionsPath = "./postman/collections"
$EnvironmentsPath = "./postman/environments"
$OutputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    $OutputPath = New-Item -Path "./test-results" -ItemType Directory -Force
}

Write-Host "🚀 Starting API Test Runner" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Collection: $Collection" -ForegroundColor Cyan
Write-Host "Output Path: $OutputPath" -ForegroundColor Cyan
Write-Host ""

# Check if Newman is installed
function Test-Newman {
    try {
        $null = Get-Command newman -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

# Install Newman if not available
function Install-Newman {
    Write-Host "📦 Installing Newman..." -ForegroundColor Yellow
    try {
        npm install -g newman
        npm install -g newman-reporter-html
        npm install -g newman-reporter-junit
        Write-Host "✅ Newman installed successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ Failed to install Newman: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Run Newman collection
function Invoke-NewmanTest {
    param(
        [string]$CollectionFile,
        [string]$EnvironmentFile,
        [string]$ReportName
    )
    
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $reportPath = "$OutputPath/$ReportName-$timestamp"
    
    $newmanArgs = @(
        "run", $CollectionFile,
        "-e", $EnvironmentFile,
        "--bail", "false",
        "--suppress-exit-code",
        "--color", "on",
        "--delay-request", "100"
    )
    
    if ($GenerateReports) {
        $newmanArgs += @(
            "--reporters", "cli,html,junit",
            "--reporter-html-export", "$reportPath.html",
            "--reporter-junit-export", "$reportPath.xml"
        )
    }
    
    Write-Host "🧪 Running tests for $ReportName..." -ForegroundColor Blue
    
    try {
        $process = Start-Process -FilePath "newman" -ArgumentList $newmanArgs -Wait -PassThru -NoNewWindow
        $exitCode = $process.ExitCode
        
        if ($exitCode -eq 0) {
            Write-Host "✅ Tests completed successfully for $ReportName" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Tests completed with failures for $ReportName (Exit Code: $exitCode)" -ForegroundColor Yellow
        }
        
        return $exitCode
    }
    catch {
        Write-Host "❌ Failed to run tests for $ReportName: $($_.Exception.Message)" -ForegroundColor Red
        return -1
    }
}

# Main execution
try {
    # Check Newman installation
    if (-not (Test-Newman)) {
        Write-Host "Newman not found. Installing..." -ForegroundColor Yellow
        if (-not (Install-Newman)) {
            Write-Host "❌ Cannot proceed without Newman" -ForegroundColor Red
            exit 1
        }
    }
    
    # Create output directory
    if (-not (Test-Path $OutputPath)) {
        New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
    }
    
    # Determine environment file
    $environmentFile = "$EnvironmentsPath/$($Environment.Substring(0,1).ToUpper() + $Environment.Substring(1)).postman_environment.json"
    if (-not (Test-Path $environmentFile)) {
        Write-Host "❌ Environment file not found: $environmentFile" -ForegroundColor Red
        exit 1
    }
    
    # Run tests based on collection selection
    $collectionsToRun = @()
    
    switch ($Collection) {
        "all" {
            $collectionsToRun = @(
                @{ File = "$CollectionsPath/Authentication.postman_collection.json"; Name = "Authentication" },
                @{ File = "$CollectionsPath/TenantManagement.postman_collection.json"; Name = "TenantManagement" },
                @{ File = "$CollectionsPath/PharmacyOperations.postman_collection.json"; Name = "PharmacyOperations" },
                @{ File = "$CollectionsPath/PointOfSale.postman_collection.json"; Name = "PointOfSale" },
                @{ File = "$CollectionsPath/PatientManagement.postman_collection.json"; Name = "PatientManagement" }
            )
        }
        "authentication" {
            $collectionsToRun = @(@{ File = "$CollectionsPath/Authentication.postman_collection.json"; Name = "Authentication" })
        }
        "tenant-management" {
            $collectionsToRun = @(@{ File = "$CollectionsPath/TenantManagement.postman_collection.json"; Name = "TenantManagement" })
        }
        "pharmacy-operations" {
            $collectionsToRun = @(@{ File = "$CollectionsPath/PharmacyOperations.postman_collection.json"; Name = "PharmacyOperations" })
        }
        "pos" {
            $collectionsToRun = @(@{ File = "$CollectionsPath/PointOfSale.postman_collection.json"; Name = "PointOfSale" })
        }
        "patient-management" {
            $collectionsToRun = @(@{ File = "$CollectionsPath/PatientManagement.postman_collection.json"; Name = "PatientManagement" })
        }
    }
    
    # Run collections
    $totalExitCode = 0
    $testResults = @()
    
    if ($Parallel -and $collectionsToRun.Count -gt 1) {
        Write-Host "🔄 Running tests in parallel..." -ForegroundColor Blue
        $jobs = @()
        
        foreach ($collection in $collectionsToRun) {
            if (Test-Path $collection.File) {
                $job = Start-Job -ScriptBlock {
                    param($CollectionFile, $EnvironmentFile, $ReportName, $OutputPath, $GenerateReports)
                    
                    $newmanArgs = @(
                        "run", $CollectionFile,
                        "-e", $EnvironmentFile,
                        "--bail", "false",
                        "--suppress-exit-code",
                        "--color", "off",
                        "--delay-request", "100"
                    )
                    
                    if ($GenerateReports) {
                        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
                        $reportPath = "$OutputPath/$ReportName-$timestamp"
                        $newmanArgs += @(
                            "--reporters", "json",
                            "--reporter-json-export", "$reportPath.json"
                        )
                    }
                    
                    $result = @{
                        Name = $ReportName
                        ExitCode = -1
                        Output = ""
                    }
                    
                    try {
                        $output = & newman $newmanArgs 2>&1
                        $result.ExitCode = $LASTEXITCODE
                        $result.Output = $output -join "`n"
                    }
                    catch {
                        $result.Output = $_.Exception.Message
                    }
                    
                    return $result
                } -ArgumentList $collection.File, $environmentFile, $collection.Name, $OutputPath, $GenerateReports
                
                $jobs += $job
            } else {
                Write-Host "⚠️  Collection file not found: $($collection.File)" -ForegroundColor Yellow
            }
        }
        
        # Wait for all jobs to complete
        foreach ($job in $jobs) {
            $result = Receive-Job -Job $job -Wait
            $testResults += $result
            Remove-Job -Job $job
            
            if ($result.ExitCode -ne 0) {
                $totalExitCode = $result.ExitCode
            }
            
            if ($result.ExitCode -eq 0) {
                Write-Host "✅ $($result.Name) tests completed" -ForegroundColor Green
            } else {
                Write-Host "⚠️  $($result.Name) tests failed (Exit Code: $($result.ExitCode))" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "🔄 Running tests sequentially..." -ForegroundColor Blue
        
        foreach ($collection in $collectionsToRun) {
            if (Test-Path $collection.File) {
                $exitCode = Invoke-NewmanTest -CollectionFile $collection.File -EnvironmentFile $environmentFile -ReportName $collection.Name
                $testResults += @{ Name = $collection.Name; ExitCode = $exitCode }
                
                if ($exitCode -ne 0) {
                    $totalExitCode = $exitCode
                }
            } else {
                Write-Host "⚠️  Collection file not found: $($collection.File)" -ForegroundColor Yellow
            }
        }
    }
    
    # Generate summary report
    $summaryFile = "$OutputPath/test-summary-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $summary = @{
        Environment = $Environment
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
        Collections = $testResults
        TotalExitCode = $totalExitCode
        Success = if ($totalExitCode -eq 0) { $true } else { $false }
    }
    
    $summary | ConvertTo-Json -Depth 10 | Out-File -FilePath $summaryFile -Encoding UTF8
    
    Write-Host ""
    Write-Host "📊 Test Summary" -ForegroundColor Cyan
    Write-Host "================" -ForegroundColor Cyan
    Write-Host "Environment: $Environment" -ForegroundColor White
    Write-Host "Collections Run: $($testResults.Count)" -ForegroundColor White
    Write-Host "Success: $(-join $testResults.Where({$_.ExitCode -eq 0}).Count)/$($testResults.Count)" -ForegroundColor White
    Write-Host "Summary Report: $summaryFile" -ForegroundColor White
    
    if ($GenerateReports) {
        Write-Host ""
        Write-Host "📄 Reports generated in: $OutputPath" -ForegroundColor Green
    }
    
    Write-Host ""
    if ($totalExitCode -eq 0) {
        Write-Host "🎉 All tests completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Some tests failed. Check the reports for details." -ForegroundColor Yellow
    }
    
    exit $totalExitCode
}
catch {
    Write-Host "❌ Fatal error during test execution: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
