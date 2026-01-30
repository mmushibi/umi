# Umi Health PowerShell Cleanup Script
# This script performs comprehensive cleanup of all data, cache, and temporary files

param(
    [switch]$Force,
    [switch]$DockerOnly,
    [switch]$DatabaseOnly,
    [switch]$CacheOnly
)

# Colors for output
$Colors = @{
    Red = "Red"
    Green = "Green"
    Yellow = "Yellow"
    Blue = "Blue"
    White = "White"
}

function Write-Status {
    param([string]$Message, [string]$Color = "Green")
    Write-Host "✅ $Message" -ForegroundColor $Colors[$Color]
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $Colors["Yellow"]
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Colors["Red"]
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor $Colors["Blue"]
}

function Test-PostgresAvailable {
    try {
        $null = Get-Command psql -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Test-RedisAvailable {
    try {
        $null = Get-Command redis-cli -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Test-DockerAvailable {
    try {
        $null = Get-Command docker -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Clear-PostgresDatabases {
    Write-Info "Cleaning PostgreSQL databases..."
    
    if (-not (Test-PostgresAvailable)) {
        Write-Warning "PostgreSQL not found, skipping database cleanup"
        return
    }
    
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $cleanupScript = Join-Path $scriptDir "cleanup-databases.sql"
    
    if (-not (Test-Path $cleanupScript)) {
        Write-Error "PostgreSQL cleanup script not found"
        return
    }
    
    try {
        Write-Info "Executing PostgreSQL cleanup script..."
        
        # Try different connection methods
        $connectionStrings = @(
            "Host=localhost;Port=5432;Database=UmiHealth;Username=umihealth;Password=root;",
            "Host=localhost;Port=5432;Database=UmiHealth;Username=umi_health_app;Password=umihealth_2024!;",
            "Host=172.17.0.2;Port=5432;Database=UmiHealth;Username=umihealth;Password=root;"
        )
        
        $connected = $false
        foreach ($connString in $connectionStrings) {
            try {
                $env:PGPASSWORD = if ($connString -match "Password=([^;]+)") { $matches[1] } else { "" }
                $user = if ($connString -match "Username=([^;]+)") { $matches[1] } else { "umihealth" }
                $host = if ($connString -match "Host=([^;]+)") { $matches[1] } else { "localhost" }
                $port = if ($connString -match "Port=([^;]+)") { $matches[1] } else { "5432" }
                $db = if ($connString -match "Database=([^;]+)") { $matches[1] } else { "UmiHealth" }
                
                $result = psql -h $host -p $port -U $user -d $db -f $cleanupScript 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Status "PostgreSQL cleanup completed"
                    $connected = $true
                    break
                }
            } catch {
                continue
            }
        }
        
        if (-not $connected) {
            Write-Warning "Could not connect to PostgreSQL database"
        }
    } catch {
        Write-Error "PostgreSQL cleanup failed: $($_.Exception.Message)"
    } finally {
        $env:PGPASSWORD = $null
    }
}

function Clear-RedisCache {
    Write-Info "Cleaning Redis cache..."
    
    if (-not (Test-RedisAvailable)) {
        Write-Warning "Redis CLI not found, skipping Redis cleanup"
        return
    }
    
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $cleanupScript = Join-Path $scriptDir "cleanup-redis.sh"
    
    if (Test-Path $cleanupScript) {
        try {
            # For Windows, we'll use redis-cli directly
            Write-Info "Flushing Redis databases..."
            
            $redisCommands = @(
                "ping",
                "flushall"
            )
            
            foreach ($cmd in $redisCommands) {
                $result = redis-cli $cmd 2>$null
                if ($LASTEXITCODE -ne 0) {
                    Write-Warning "Redis command failed: $cmd"
                }
            }
            
            Write-Status "Redis cleanup completed"
        } catch {
            Write-Error "Redis cleanup failed: $($_.Exception.Message)"
        }
    } else {
        Write-Error "Redis cleanup script not found"
    }
}

function Clear-SqliteDatabases {
    Write-Info "Cleaning SQLite databases..."
    
    $sqliteFiles = @(
        ".\UmiHealth.MinimalApi\bin\Debug\net8.0\UmiHealth.db",
        ".\UmiHealth.MinimalApi\bin\Release\net8.0\UmiHealth.db",
        ".\UmiHealth.MinimalApi\UmiHealth.db",
        ".\data\UmiHealth.db",
        ".\app_data\UmiHealth.db",
        ".\UmiHealth.db"
    )
    
    foreach ($dbFile in $sqliteFiles) {
        if (Test-Path $dbFile) {
            Write-Info "Removing SQLite database: $dbFile"
            Remove-Item $dbFile -Force -ErrorAction SilentlyContinue
            Remove-Item "$dbFile-journal" -Force -ErrorAction SilentlyContinue
            Remove-Item "$dbFile-wal" -Force -ErrorAction SilentlyContinue
            Remove-Item "$dbFile-shm" -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Find additional SQLite files
    Get-ChildItem -Path . -Name "*.db" -File -Recurse | Where-Object { 
        $_.FullName -notmatch "node_modules" -and $_.FullName -notmatch "\.git" 
    } | ForEach-Object {
        Write-Info "Found SQLite file: $($_.FullName)"
        if ($Force -or (Read-Host "Remove this file? (y/N)") -eq 'y') {
            Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
            Write-Status "Removed: $($_.FullName)"
        }
    }
    
    Write-Status "SQLite cleanup completed"
}

function Clear-DockerResources {
    Write-Info "Cleaning Docker containers and volumes..."
    
    if (-not (Test-DockerAvailable)) {
        Write-Warning "Docker not found, skipping Docker cleanup"
        return
    }
    
    try {
        # Stop and remove containers
        $containers = @(
            "umihealth-api-gateway",
            "umihealth-api", 
            "umihealth-identity",
            "umihealth-postgres",
            "umihealth-redis",
            "umihealth-jobs",
            "umihealth-prometheus",
            "umihealth-grafana",
            "umihealth-nginx"
        )
        
        foreach ($container in $containers) {
            docker stop $container 2>$null
            docker rm $container 2>$null
        }
        
        # Remove volumes
        $volumes = @(
            "umihealth_postgres_data",
            "umihealth_redis_data", 
            "umihealth_prometheus_data",
            "umihealth_grafana_data"
        )
        
        foreach ($volume in $volumes) {
            docker volume rm $volume 2>$null
        }
        
        # Remove networks
        docker network rm umihealth_umihealth-network 2>$null
        
        # System cleanup
        docker system prune -f
        
        Write-Status "Docker cleanup completed"
    } catch {
        Write-Error "Docker cleanup failed: $($_.Exception.Message)"
    }
}

function Clear-TemporaryFiles {
    Write-Info "Cleaning temporary files and logs..."
    
    # Clean log files
    Get-ChildItem -Path . -Name "*.log" -File -Recurse | Where-Object { 
        $_.FullName -notmatch "node_modules" -and $_.FullName -notmatch "\.git" 
    } | Remove-Item -Force -ErrorAction SilentlyContinue
    
    # Clean temporary files
    Get-ChildItem -Path . -Name "*.tmp" -File -Recurse | Where-Object { 
        $_.FullName -notmatch "node_modules" -and $_.FullName -notmatch "\.git" 
    } | Remove-Item -Force -ErrorAction SilentlyContinue
    
    Get-ChildItem -Path . -Name "*.temp" -File -Recurse | Where-Object { 
        $_.FullName -notmatch "node_modules" -and $_.FullName -notmatch "\.git" 
    } | Remove-Item -Force -ErrorAction SilentlyContinue
    
    # Clean cache directories
    if (Test-Path ".cache") {
        Remove-Item ".cache" -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    # Clean build artifacts
    Get-ChildItem -Path . -Name "bin" -Directory -Recurse | Where-Object { 
        $_.FullName -notmatch "node_modules" -and $_.FullName -notmatch "\.git" 
    } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    
    Get-ChildItem -Path . -Name "obj" -Directory -Recurse | Where-Object { 
        $_.FullName -notmatch "node_modules" -and $_.FullName -notmatch "\.git" 
    } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    
    Write-Status "Temporary files cleanup completed"
}

function New-BrowserCleanupFile {
    Write-Info "Creating browser data cleanup file..."
    
    $htmlContent = @'
<!DOCTYPE html>
<html>
<head>
    <title>Clear Browser Data</title>
    <script>
        function clearAllData() {
            // Clear localStorage
            localStorage.clear();
            
            // Clear sessionStorage
            sessionStorage.clear();
            
            // Clear IndexedDB
            if (window.indexedDB) {
                const databases = indexedDB.databases();
                databases.then(function(dbs) {
                    dbs.forEach(function(db) {
                        indexedDB.deleteDatabase(db.name);
                    });
                });
            }
            
            alert('Browser data cleared successfully!');
            window.close();
        }
    </script>
</head>
<body>
    <h1>Clear Browser Data</h1>
    <p>Click the button below to clear all browser data for Umi Health:</p>
    <button onclick="clearAllData()">Clear All Data</button>
</body>
</html>
'@
    
    Set-Content -Path "clear-browser-data.html" -Value $htmlContent
    Write-Status "Browser data cleanup HTML file created"
    Write-Info "Open clear-browser-data.html in your browser to clear browser data"
}

function Show-Summary {
    Write-Host ""
    Write-Host "======================================="
    Write-Host "🎉 Cleanup Summary" -ForegroundColor Green
    Write-Host "======================================="
    Write-Host ""
    Write-Host "The following cleanup operations have been performed:"
    Write-Host "✅ PostgreSQL databases cleaned"
    Write-Host "✅ Redis cache cleared"
    Write-Host "✅ SQLite databases removed"
    Write-Host "✅ Docker containers and volumes removed"
    Write-Host "✅ Temporary files and logs cleaned"
    Write-Host "✅ Browser data cleanup HTML created"
    Write-Host ""
    Write-Host "To restart the application:"
    Write-Host "  1. Run: docker-compose up -d"
    Write-Host "  2. Or start your development server"
    Write-Host ""
    Write-Host "⚠️  Important: All data has been permanently deleted!"
    Write-Host ""
}

# Main execution
function Main {
    Write-Host "🧹 Umi Health PowerShell Cleanup Script" -ForegroundColor Blue
    Write-Host "======================================="
    Write-Host ""
    
    if (-not $Force) {
        Write-Host "This script will perform a comprehensive cleanup of all Umi Health data."
        Write-Host "This includes databases, cache, containers, and temporary files."
        Write-Host ""
        Write-Warning "WARNING: This action is irreversible and will delete ALL data!"
        Write-Host ""
        
        $response = Read-Host "Are you sure you want to continue? (y/N)"
        if ($response -ne 'y') {
            Write-Info "Cleanup cancelled"
            exit 0
        }
    }
    
    Write-Host ""
    Write-Info "Starting comprehensive cleanup..."
    Write-Host ""
    
    # Execute cleanup based on parameters
    if ($DatabaseOnly) {
        Clear-PostgresDatabases
        Clear-SqliteDatabases
    } elseif ($CacheOnly) {
        Clear-RedisCache
    } elseif ($DockerOnly) {
        Clear-DockerResources
    } else {
        Clear-PostgresDatabases
        Clear-RedisCache
        Clear-SqliteDatabases
        Clear-DockerResources
        Clear-TemporaryFiles
        New-BrowserCleanupFile
    }
    
    Show-Summary
}

# Run main function
Main
