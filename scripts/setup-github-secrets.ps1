#!/usr/bin/env pwsh

<#
.SYNOPSIS
GitHub Secrets Bulk Loader for Umi Health Production Deployment
.DESCRIPTION
This script helps you populate required GitHub secrets for the Umi Health project.
It displays all required secrets and provides step-by-step instructions.
.NOTES
GitHub CLI must be installed: https://cli.github.com
Authentication: Run 'gh auth login' before using this script
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Owner = "mmushibi",
    
    [Parameter(Mandatory=$false)]
    [string]$Repo = "umi"
)

Write-Host "🔐 Umi Health GitHub Secrets Configurator" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if GitHub CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "❌ GitHub CLI is not installed." -ForegroundColor Red
    Write-Host "Install from: https://cli.github.com" -ForegroundColor Yellow
    exit 1
}

# Check authentication
$ghUser = gh auth status 2>&1 | Select-String "Logged in to" | ForEach-Object { $_.ToString() }
if (-not $ghUser) {
    Write-Host "❌ Not authenticated with GitHub CLI." -ForegroundColor Red
    Write-Host "Run: gh auth login" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Authenticated with GitHub CLI" -ForegroundColor Green
Write-Host "Repository: $Owner/$Repo" -ForegroundColor Green
Write-Host ""

# Define required secrets with descriptions
$requiredSecrets = @(
    @{
        Name = "ACR_LOGIN_SERVER"
        Description = "Azure Container Registry login server"
        Example = "myacr.azurecr.io"
        Required = $true
    },
    @{
        Name = "ACR_USERNAME"
        Description = "ACR Service Principal Client ID"
        Example = "service-principal-id"
        Required = $true
    },
    @{
        Name = "ACR_PASSWORD"
        Description = "ACR Service Principal Client Secret"
        Example = "service-principal-secret"
        Required = $true
    },
    @{
        Name = "CR_PAT"
        Description = "GitHub Container Registry Personal Access Token"
        Example = "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
        Required = $true
    },
    @{
        Name = "AZURE_PUBLISH_PROFILE"
        Description = "Azure Web App Publish Profile XML"
        Example = "<publishData>...</publishData>"
        Required = $true
    },
    @{
        Name = "AZURE_WEBAPP_NAME"
        Description = "Azure Web App name"
        Example = "umihealth-prod"
        Required = $true
    },
    @{
        Name = "AZURE_WEBAPP_DEFAULT_HOSTNAME"
        Description = "Azure Web App hostname"
        Example = "umihealth-prod.azurewebsites.net"
        Required = $true
    },
    @{
        Name = "DATABASE_CONNECTION"
        Description = "Production database connection string"
        Example = "Host=db.prod.com;Port=5432;Database=umihealth;Username=user;Password=pass"
        Required = $true
    },
    @{
        Name = "JWT_SECRET"
        Description = "JWT signing secret (min 32 characters)"
        Example = "your_secure_random_string_min_32_chars_long"
        Required = $true
    },
    @{
        Name = "JWT_ISSUER"
        Description = "JWT issuer claim"
        Example = "UmiHealth"
        Required = $false
    },
    @{
        Name = "JWT_AUDIENCE"
        Description = "JWT audience claim"
        Example = "UmiHealthUsers"
        Required = $false
    },
    @{
        Name = "POSTGRES_PASSWORD"
        Description = "PostgreSQL admin password"
        Example = "SecurePassword123!"
        Required = $true
    },
    @{
        Name = "REDIS_PASSWORD"
        Description = "Redis server password"
        Example = "SecurePassword123!"
        Required = $true
    },
    @{
        Name = "ENCRYPTION_KEY"
        Description = "AES-256 encryption key (base64, 256-bit)"
        Example = "WP0XY7vLnNz7zYmK6jXhV5pQrS2tUvWxYzAbCdEfGhIjKlMnOpQrStUvWxYzAb"
        Required = $true
    },
    @{
        Name = "SMTP_SERVER"
        Description = "SMTP server hostname"
        Example = "smtp.sendgrid.net"
        Required = $false
    },
    @{
        Name = "SMTP_PORT"
        Description = "SMTP port number"
        Example = "587"
        Required = $false
    },
    @{
        Name = "SMTP_USERNAME"
        Description = "SMTP username or API key"
        Example = "apikey"
        Required = $false
    },
    @{
        Name = "SMTP_PASSWORD"
        Description = "SMTP password or API key"
        Example = "your_sendgrid_api_key"
        Required = $false
    },
    @{
        Name = "GRAFANA_USER"
        Description = "Grafana admin username"
        Example = "admin"
        Required = $false
    },
    @{
        Name = "GRAFANA_PASSWORD"
        Description = "Grafana admin password"
        Example = "SecurePassword123!"
        Required = $false
    },
    @{
        Name = "ADMIN_PASSWORD"
        Description = "Default admin user password for tests"
        Example = "SecurePassword123!"
        Required = $true
    }
)

Write-Host "📋 Required Secrets to Configure: $($requiredSecrets.Count)" -ForegroundColor Cyan
Write-Host ""

# Display current secrets
Write-Host "🔍 Checking current secrets..." -ForegroundColor Yellow
$existingSecrets = gh secret list --repo "$Owner/$Repo" 2>&1 | ConvertFrom-Csv -Delimiter "`t" -Header "Name", "Updated"

Write-Host ""
Write-Host "✅ Existing Secrets:" -ForegroundColor Green
if ($existingSecrets) {
    $existingSecrets | ForEach-Object { Write-Host "  - $($_.Name)" }
} else {
    Write-Host "  (None configured yet)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "❌ Secrets NOT Yet Configured:" -ForegroundColor Red
$missingSecrets = $requiredSecrets | Where-Object { 
    -not ($existingSecrets | Where-Object { $_.Name -eq $_.Name })
}

if ($missingSecrets) {
    $missingSecrets | ForEach-Object { 
        $required = if ($_.Required) { "[REQUIRED]" } else { "[OPTIONAL]" }
        Write-Host "  - $($_.Name) $required" -ForegroundColor $(if ($_.Required) { "Red" } else { "Yellow" })
        Write-Host "    Description: $($_.Description)" -ForegroundColor Gray
        Write-Host "    Example: $($_.Example)" -ForegroundColor Gray
    }
} else {
    Write-Host "  (All secrets configured!)" -ForegroundColor Green
}

Write-Host ""
Write-Host "📖 Manual Configuration Instructions:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Option A: Using GitHub CLI (Fastest)"
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "For each missing secret, run:"
Write-Host ""
Write-Host "  gh secret set SECRET_NAME --repo $Owner/$Repo"
Write-Host "  (Paste value when prompted)"
Write-Host ""

Write-Host "Option B: Using GitHub Web UI"
Write-Host "==============================" -ForegroundColor Cyan
Write-Host "1. Go to: https://github.com/$Owner/$Repo/settings/secrets/actions"
Write-Host "2. Click 'New repository secret'"
Write-Host "3. Enter secret name and value"
Write-Host "4. Click 'Add secret'"
Write-Host ""

Write-Host "Example Commands to Add Secrets:"
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "# Generate secure random values first:" -ForegroundColor Gray
Write-Host '$key = [byte[]]::new(32)' -ForegroundColor Gray
Write-Host '[System.Security.Cryptography.RNGCryptoServiceProvider]::new().GetBytes($key)' -ForegroundColor Gray
Write-Host '[Convert]::ToBase64String($key)  # Use this for encryption keys' -ForegroundColor Gray
Write-Host ""
Write-Host "# Add secrets:" -ForegroundColor Gray
Write-Host "gh secret set JWT_SECRET --repo $Owner/$Repo" -ForegroundColor Yellow
Write-Host "gh secret set DATABASE_CONNECTION --repo $Owner/$Repo" -ForegroundColor Yellow
Write-Host "gh secret set ENCRYPTION_KEY --repo $Owner/$Repo" -ForegroundColor Yellow
Write-Host "# ... and so on for each secret" -ForegroundColor Yellow
Write-Host ""

# Validation
Write-Host "✅ Validation:" -ForegroundColor Cyan
Write-Host ""
Write-Host "After adding secrets, verify they are set:"
Write-Host "  gh secret list --repo $Owner/$Repo"
Write-Host ""

Write-Host "📚 Additional Resources:" -ForegroundColor Cyan
Write-Host "  - Secrets Checklist: GITHUB_SECRETS_CHECKLIST.md"
Write-Host "  - Configuration Guide: CONFIGURATION_SECRETS.md"
Write-Host "  - CI/CD Workflow: .github/workflows/ci-cd.yml"
Write-Host ""

Write-Host "🚀 Next Steps:" -ForegroundColor Green
Write-Host "  1. Gather all secret values using the checklist"
Write-Host "  2. Add secrets using GitHub CLI or Web UI"
Write-Host "  3. Verify all secrets are set"
Write-Host "  4. Push a commit to trigger CI/CD"
Write-Host "  5. Review workflow execution: https://github.com/$Owner/$Repo/actions"
Write-Host ""
