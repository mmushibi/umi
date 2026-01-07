# Production Deployment & Testing Guide

## Overview
This guide walks through the complete process to prepare, test, and deploy Umi Health to production.

---

## Phase 1: Configure GitHub Secrets

### Step 1.1: Gather Secret Values
Use the [GITHUB_SECRETS_CHECKLIST.md](../GITHUB_SECRETS_CHECKLIST.md) to collect all required secret values.

**Quick reference:**
- Azure secrets: Get from Azure Portal (App registrations, ACR, Web App)
- Database connection: Format as `Host=db.prod.com;Port=5432;Database=umihealth;Username=user;Password=pass`
- JWT secret: Generate with 32+ random characters
- Encryption key: Generate as base64-encoded 256-bit value
- Email/SMTP: Get from SendGrid or your email provider

### Step 1.2: Add Secrets to GitHub

#### Option A: Using GitHub CLI (Recommended)
```powershell
# Authenticate with GitHub (first time only)
gh auth login

# Run the secrets setup helper script
pwsh .\scripts\setup-github-secrets.ps1 -Owner mmushibi -Repo umi

# Or manually add each secret:
gh secret set ACR_LOGIN_SERVER --repo mmushibi/umi
gh secret set ACR_USERNAME --repo mmushibi/umi
gh secret set ACR_PASSWORD --repo mmushibi/umi
# ... continue for all 19 secrets
```

#### Option B: Using GitHub Web UI
1. Go to: `https://github.com/mmushibi/umi/settings/secrets/actions`
2. Click **New repository secret**
3. Enter secret name and value
4. Repeat for all 19 secrets (see checklist)

### Step 1.3: Verify Secrets
```powershell
# List all configured secrets
gh secret list --repo mmushibi/umi

# Expected output:
# ACR_LOGIN_SERVER      Updated 2026-01-07
# ACR_USERNAME          Updated 2026-01-07
# ... (all 19 secrets)
```

---

## Phase 2: Update Application Configuration

### Step 2.1: Verify Environment Variable Support
The following files have been updated to support environment variables:

- `backend/src/UmiHealth.Api/Program.cs` — Now loads configuration from:
  1. `appsettings.json` (defaults)
  2. `appsettings.{Environment}.json` (environment-specific)
  3. `appsettings.Security.json` (security settings)
  4. **Environment variables (highest priority)**

### Step 2.2: Environment Variable Mapping

The application reads environment variables in this format:

```
Environment Variable          → appsettings.json Path
DB_HOST                       → ConnectionStrings:DefaultConnection (Host part)
DB_PORT                       → ConnectionStrings:DefaultConnection (Port part)
DB_NAME                       → ConnectionStrings:DefaultConnection (Database)
DB_USER                       → ConnectionStrings:DefaultConnection (Username)
DB_PASSWORD                   → ConnectionStrings:DefaultConnection (Password)
JWT_SECRET                    → Jwt:Key
JWT_ISSUER                    → Jwt:Issuer
JWT_AUDIENCE                  → Jwt:Audience
ENCRYPTION_KEY                → Encryption:Key
POSTGRES_PASSWORD             → Infrastructure config (database)
REDIS_PASSWORD                → Infrastructure config (caching)
```

### Step 2.3: Verify Configuration on Startup

The application validates critical settings on startup. If any are missing:

```csharp
// JWT_SECRET validation
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET is not configured or too short (min 32 chars).");
}

// Database connection validation
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "DATABASE_CONNECTION is not configured.");
}
```

---

## Phase 3: Local Testing

### Prerequisites
- Docker & Docker Desktop running
- .NET 8 SDK installed
- Newman CLI for Postman tests (`npm install -g newman`)
- Environment variables configured (see below)

### Step 3.1: Prepare Local Environment

Create a `.env.local` file in the project root:

```bash
# Database
POSTGRES_PASSWORD=your_secure_postgres_password_123
DB_HOST=localhost
DB_PORT=5432
DB_NAME=umihealth
DB_USER=umihealth

# JWT
JWT_SECRET=your_jwt_secret_key_at_least_32_characters_long_secure_random_123456
JWT_ISSUER=UmiHealth
JWT_AUDIENCE=UmiHealthUsers

# Redis
REDIS_PASSWORD=your_secure_redis_password_123

# Encryption
ENCRYPTION_KEY=WP0XY7vLnNz7zYmK6jXhV5pQrS2tUvWxYzAbCdEfGhIjKlMnOpQrStUvWxYzAb

# Email (optional for development)
SMTP_SERVER=mailhog
SMTP_PORT=1025
SMTP_USERNAME=test
SMTP_PASSWORD=test

# Monitoring
GRAFANA_USER=admin
GRAFANA_PASSWORD=admin123
```

**Note:** `.env.local` is excluded from git (see `.gitignore`). Never commit real passwords.

### Step 3.2: Load Environment Variables

```powershell
# Load variables into current session
$env:POSTGRES_PASSWORD = "your_secure_postgres_password_123"
$env:JWT_SECRET = "your_jwt_secret_key_at_least_32_characters_long_secure_random_123456"
# ... continue for all variables

# Or load from file:
Get-Content .env.local | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') {
        [Environment]::SetEnvironmentVariable($matches[1], $matches[2])
    }
}
```

### Step 3.3: Start Docker Environment

```powershell
# Start all services (database, redis, API, identity service, etc.)
docker-compose up -d

# Wait for services to be healthy (30-60 seconds)
docker ps

# Expected running containers:
# - umihealth-postgres
# - umihealth-redis
# - umihealth-api
# - umihealth-identity
# - umihealth-background-jobs
# - umihealth-prometheus
# - umihealth-grafana
```

### Step 3.4: Verify API Health

```powershell
# Test health endpoint
curl -k https://localhost:7123/health

# Expected response:
# {
#   "status": "healthy",
#   "timestamp": "2026-01-07T...",
#   "services": {
#     "database": "healthy",
#     "redis": "healthy"
#   }
# }
```

### Step 3.5: Run Postman Tests

```powershell
# Run the test script with auto-authentication
pwsh .\api-testing\scripts\run-postman-tests.ps1 `
  -Environment Development `
  -BaseUrl https://localhost:7123

# Or with explicit token
$token = "your_jwt_token_from_login"
pwsh .\api-testing\scripts\run-postman-tests.ps1 `
  -Environment Development `
  -BaseUrl https://localhost:7123 `
  -AccessToken $token
```

**Expected output:**
```
🚀 Umi Health Postman Test Runner
=================================

📦 Collection: api-testing/postman/collections/UmiHealth_API_Collection.postman_collection.json
🌍 Environment: Development
🎯 Target URL: https://localhost:7123

🧪 Running Postman collection...

Umi Health API Collection

✓ Authentication
  ✓ Login
  ✓ Register
  
✓ Tenants
  ✓ Get Tenants
  ✓ Create Tenant
  
... (all 18 endpoints)

📊 Test Results Summary
======================
Total Tests: 18
Passed: 18
Failed: 0

✅ All tests passed!
```

### Step 3.6: Review Test Results

Test results are saved to: `test-results/postman-results-{timestamp}.json`

```powershell
# View latest results
Get-ChildItem test-results/ -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object {
    Get-Content $_.FullName | ConvertFrom-Json | Format-List
}
```

---

## Phase 4: Deploy to Staging

### Step 4.1: Push Code to Repository

```powershell
# Commit configuration changes (excludes .env files automatically)
git add .
git commit -m "chore: update configuration for production environment variables"
git push origin main
```

### Step 4.2: Trigger CI/CD Pipeline

The GitHub Actions workflow is automatically triggered on push to `main` or `develop` branches.

**Workflow file:** `.github/workflows/ci-cd.yml`

**Pipeline stages:**
1. `build-test` — Builds and tests .NET code
2. `build-and-push` — Builds Docker images and pushes to registry
3. `deploy-azure` — Deploys to Azure Web App
4. `postman-smoke-tests` — Runs API tests against deployed environment

### Step 4.3: Monitor Deployment

```powershell
# Watch the workflow in real-time
gh run watch -R mmushibi/umi

# Or view in browser
# https://github.com/mmushibi/umi/actions

# Check logs for a specific run
gh run view -R mmushibi/umi {run_id} --log

# Check Postman test results
gh run view -R mmushibi/umi {run_id} --log | grep -A 20 "postman-smoke-tests"
```

### Step 4.4: Verify Deployment

```powershell
# Get the deployment URL from Azure or GitHub
$stagingUrl = "https://umihealth-staging.azurewebsites.net"

# Test health endpoint
curl -k $stagingUrl/health

# Run Postman tests against staging
pwsh .\api-testing\scripts\run-postman-tests.ps1 `
  -Environment Staging `
  -BaseUrl $stagingUrl
```

### Step 4.5: Review Logs

```powershell
# View application logs
az webapp log tail --name umihealth-staging --resource-group umi-health-rg

# View specific service logs (if using Azure Monitor)
az monitor log-analytics query \
  --workspace-id YOUR_WORKSPACE_ID \
  --analytics-query "AppServiceConsoleLogs | where Level == 'Error'"
```

---

## Phase 5: Deploy to Production

### Prerequisites Checklist

Before deploying to production, verify:

- [ ] All GitHub secrets are configured (19 total)
- [ ] Local testing passed (all 18 API endpoints)
- [ ] Staging deployment successful
- [ ] No errors in CI/CD logs
- [ ] Database backups taken
- [ ] Monitoring and alerting configured
- [ ] Incident response plan documented
- [ ] Team notified of deployment

### Step 5.1: Create Production Deployment

```powershell
# Option A: Automatic (via CI/CD on main branch)
git checkout main
git pull
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
# Workflow automatically deploys to production

# Option B: Manual (if needed)
gh workflow run ci-cd.yml -r main -f environment=production
```

### Step 5.2: Verify Production Deployment

```powershell
$prodUrl = "https://api.umihealth.com"

# Test health endpoint
curl -k $prodUrl/health

# Run full Postman suite
pwsh .\api-testing\scripts\run-postman-tests.ps1 `
  -Environment Production `
  -BaseUrl $prodUrl

# Check Azure Application Insights
az monitor app-insights query \
  --app YOUR_APP_INSIGHTS_NAME \
  --analytics-query "requests | summarize Count=count() by resultCode"
```

### Step 5.3: Monitor Production

```powershell
# Set up real-time alerts (Azure Monitor)
az monitor metrics alert create \
  --name "UmiHealth-HighErrorRate" \
  --resource-group umi-health-rg \
  --scopes "/subscriptions/{sub-id}/resourceGroups/umi-health-rg/providers/Microsoft.Web/sites/umihealth-prod" \
  --condition "avg FailedRequests > 10"

# Monitor logs
az webapp log tail --name umihealth-prod --resource-group umi-health-rg

# Check uptime
# Use external service: https://status.io, https://uptimerobot.com, etc.
```

### Step 5.4: Post-Deployment Validation

Run a full validation suite:

```powershell
# 1. API Health
$healthCheck = curl -k https://api.umihealth.com/health | ConvertFrom-Json
if ($healthCheck.status -ne "healthy") {
    Write-Error "Health check failed!"
}

# 2. Authentication
$loginResponse = curl -X POST https://api.umihealth.com/api/v1/auth/login `
  -Headers @{"Content-Type" = "application/json"} `
  -Body '{"email":"admin@umihealth.com","password":"***"}'
if (-not $loginResponse.data.token) {
    Write-Error "Authentication failed!"
}

# 3. Database connectivity
$dbCheck = curl -k https://api.umihealth.com/api/v1/health/db | ConvertFrom-Json
if ($dbCheck.status -ne "healthy") {
    Write-Error "Database connection failed!"
}

# 4. Full Postman test suite
pwsh .\api-testing\scripts\run-postman-tests.ps1 `
  -Environment Production `
  -BaseUrl https://api.umihealth.com
```

---

## Troubleshooting

### Issue: "JWT_SECRET not configured"
**Solution:**
```powershell
# Verify environment variable is set
$env:JWT_SECRET
# If empty, set it:
$env:JWT_SECRET = "your_32_char_secret_here"
```

### Issue: "DATABASE_CONNECTION not configured"
**Solution:**
```powershell
# Verify connection string
$env:DATABASE_CONNECTION
# Format should be:
# Host=db.server.com;Port=5432;Database=umihealth;Username=user;Password=pass
```

### Issue: Docker containers fail to start
**Solution:**
```powershell
# Check environment variables are loaded
dir env: | Where-Object {$_.Name -match "POSTGRES|JWT|REDIS"}

# Load from .env.local file:
Get-Content .env.local | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') {
        [Environment]::SetEnvironmentVariable($matches[1], $matches[2])
    }
}

# Rebuild containers
docker-compose down
docker-compose up -d --build
```

### Issue: API returns 401 Unauthorized
**Solution:**
```powershell
# Verify JWT_SECRET is identical across all services
# Check if token is being sent:
curl -k -H "Authorization: Bearer $token" https://api.umihealth.com/api/v1/users
```

### Issue: Postman tests timeout
**Solution:**
```powershell
# Increase timeout values
pwsh .\api-testing\scripts\run-postman-tests.ps1 `
  -Environment Development `
  -BaseUrl https://localhost:7123 `
  # Note: Adjust timeout-request in script if needed
```

---

## Rollback Plan

If production deployment fails:

```powershell
# 1. Stop current deployment
az webapp stop --name umihealth-prod --resource-group umi-health-rg

# 2. Restore from previous version
az webapp config container set \
  --name umihealth-prod \
  --resource-group umi-health-rg \
  --docker-custom-image-name {PREVIOUS_IMAGE_TAG}

# 3. Start application
az webapp start --name umihealth-prod --resource-group umi-health-rg

# 4. Verify health
curl https://api.umihealth.com/health

# 5. Notify team and investigate
```

---

## Related Documentation

- [GITHUB_SECRETS_CHECKLIST.md](../GITHUB_SECRETS_CHECKLIST.md) — Required secrets reference
- [CONFIGURATION_SECRETS.md](../CONFIGURATION_SECRETS.md) — Configuration management guide
- [POSTMAN_TEST_REPORT.md](../api-testing/POSTMAN_TEST_REPORT.md) — API test documentation
- [.github/workflows/ci-cd.yml](../.github/workflows/ci-cd.yml) — CI/CD workflow definition

---

**Status:** ✅ Ready for production deployment  
**Last Updated:** January 7, 2026
