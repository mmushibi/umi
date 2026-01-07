# Configuration & Secrets Management Guide

## Overview
This guide provides instructions for securely managing configuration and secrets across development, staging, and production environments using environment variables and GitHub Secrets.

## Required Secrets for Production

### GitHub Actions CI/CD Secrets
Add the following secrets to your GitHub repository settings (`Settings > Secrets and variables > Actions`):

| Secret Name | Description | Format | Example |
|---|---|---|---|
| `ACR_LOGIN_SERVER` | Azure Container Registry login server | FQDN | `myacr.azurecr.io` |
| `ACR_USERNAME` | ACR username | String | Service principal client ID |
| `ACR_PASSWORD` | ACR password | String | Service principal client secret |
| `CR_PAT` | GitHub Container Registry Personal Access Token | Token | `ghp_xxxxxxxxxxxx...` |
| `AZURE_PUBLISH_PROFILE` | Azure Web App publish profile XML | Base64/XML | Export from Azure Portal |
| `AZURE_WEBAPP_NAME` | Azure Web App name | String | `umihealth-prod` |
| `AZURE_WEBAPP_DEFAULT_HOSTNAME` | Azure Web App hostname | FQDN | `umihealth-prod.azurewebsites.net` |
| `DATABASE_CONNECTION` | Production database connection string | String | `Host=db.prod.com;Database=umihealth;Username=...;Password=...` |
| `JWT_SECRET` | JWT signing secret | String (min 32 chars) | Cryptographically secure random string |
| `JWT_ISSUER` | JWT issuer claim | String | `UmiHealth` |
| `JWT_AUDIENCE` | JWT audience claim | String | `UmiHealthUsers` |
| `POSTGRES_PASSWORD` | PostgreSQL admin password | String | Secure random password |
| `REDIS_PASSWORD` | Redis server password | String | Secure random password |
| `SMTP_SERVER` | SMTP server for email | FQDN | `smtp.sendgrid.net` |
| `SMTP_PORT` | SMTP port | Integer | `587` |
| `SMTP_USERNAME` | SMTP username | String | SendGrid API key or username |
| `SMTP_PASSWORD` | SMTP password | String | SendGrid API key or password |
| `GRAFANA_USER` | Grafana admin username | String | `admin` |
| `GRAFANA_PASSWORD` | Grafana admin password | String | Secure random password |

### Local Development (.env or Environment Variables)

Create a `.env` file (not committed) in the root of the project:

```bash
# Database
POSTGRES_PASSWORD=your_local_postgres_password
DATABASE_CONNECTION=Host=localhost;Port=5432;Database=umihealth;Username=umihealth;Password=your_local_postgres_password

# JWT
JWT_SECRET=your_local_jwt_secret_min_32_chars_long_secure_random
JWT_ISSUER=UmiHealth
JWT_AUDIENCE=UmiHealthUsers

# Redis
REDIS_PASSWORD=your_local_redis_password

# Email (if using local SMTP stub, these can be dummy values)
SMTP_SERVER=mailhog
SMTP_PORT=1025
SMTP_USERNAME=test
SMTP_PASSWORD=test

# Monitoring
GRAFANA_USER=admin
GRAFANA_PASSWORD=your_local_grafana_password
```

Then load it before running docker-compose:

```bash
# PowerShell
Get-Content .env | ForEach-Object {
    if ($_ -match '^\s*([^=]+)=(.*)$') {
        [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
    }
}
docker-compose up

# Bash
export $(cat .env | xargs)
docker-compose up
```

## Application Configuration

### appsettings.json (Safe - No Secrets)
Contains only non-sensitive defaults and reads secrets from environment variables.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=${DB_HOST:localhost};Port=${DB_PORT:5432};Database=${DB_NAME:umihealth};Username=${DB_USER:postgres};Password=${DB_PASSWORD}"
  },
  "Jwt": {
    "Key": "${JWT_SECRET}",
    "Issuer": "${JWT_ISSUER:UmiHealth}",
    "Audience": "${JWT_AUDIENCE:UmiHealthUsers}"
  }
}
```

**Note:** The .NET application should use the `IConfiguration` interface to read from environment variables using the pattern above.

### appsettings.Security.json (Safe - Placeholder Only)

The encryption key placeholder in `appsettings.Security.json`:

```json
{
  "Encryption": {
    "Key": "${ENCRYPTION_KEY}"
  }
}
```

**DO NOT commit real encryption keys.** Generate one and store in GitHub Secrets / Azure Key Vault:

```powershell
# Generate a secure base64 key (256-bit = 32 bytes)
$key = [byte[]]::new(32)
[System.Security.Cryptography.RNGCryptoServiceProvider]::new().GetBytes($key)
$base64Key = [Convert]::ToBase64String($key)
Write-Host "Generated Key: $base64Key"
```

Add `ENCRYPTION_KEY` to GitHub Secrets with the generated value.

## Docker Compose & Kubernetes Deployment

### For docker-compose.yml (Development/Staging)

Environment variables are passed at runtime:

```bash
docker-compose up \
  -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
  -e JWT_SECRET="$JWT_SECRET" \
  -e REDIS_PASSWORD="$REDIS_PASSWORD" \
  -e SMTP_SERVER="$SMTP_SERVER" \
  -e GRAFANA_PASSWORD="$GRAFANA_PASSWORD"
```

Or via a `.env.production` file:

```bash
docker-compose --env-file .env.production up -d
```

### For Kubernetes (Production)

Use Kubernetes Secrets to manage sensitive data:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: umihealth-secrets
  namespace: umihealth
type: Opaque
stringData:
  db-password: "{{ secrets.POSTGRES_PASSWORD }}"
  jwt-secret: "{{ secrets.JWT_SECRET }}"
  redis-password: "{{ secrets.REDIS_PASSWORD }}"
  encryption-key: "{{ secrets.ENCRYPTION_KEY }}"
```

Reference in Deployments:

```yaml
env:
  - name: POSTGRES_PASSWORD
    valueFrom:
      secretKeyRef:
        name: umihealth-secrets
        key: db-password
```

## GitHub Actions CI/CD Integration

The `.github/workflows/ci-cd.yml` reads secrets and injects them into environment variables during build and deployment:

```yaml
build-and-push:
  env:
    JWT_SECRET: ${{ secrets.JWT_SECRET }}
    JWT_ISSUER: ${{ secrets.JWT_ISSUER }}
    POSTGRES_PASSWORD: ${{ secrets.POSTGRES_PASSWORD }}
  steps:
    - name: Build Docker image
      run: |
        docker build -t myimage:${{ github.sha }} .
```

## Security Best Practices

1. **Never commit secrets** — Use `.gitignore` to exclude:
   - `.env`
   - `.env.*.local`
   - `appsettings.Production.json`
   - `appsettings.Production.*.json`

2. **Rotate secrets regularly** — Update GitHub Secrets and redeploy.

3. **Use least-privilege** — Grant service principals and accounts only required permissions.

4. **Audit access** — Review GitHub Actions logs and deployment records.

5. **Use Azure Key Vault** (Optional for production) — Store secrets in Key Vault and reference from GitHub Actions:

```yaml
- name: Get secrets from Azure Key Vault
  uses: Azure/get-keyvault-secrets@v1
  with:
    keyvault: "MyKeyVault"
    secrets: "JwtSecret,DatabasePassword,EncryptionKey"
  id: keyvault
```

## Testing Configuration

### Validate Environment Variables in Your Application

Add a startup validation in your Program.cs or Startup.cs:

```csharp
var jwtSecret = configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT_SECRET environment variable is not configured.");
}

var dbConnection = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(dbConnection))
{
    throw new InvalidOperationException("DATABASE_CONNECTION environment variable is not configured.");
}
```

### Verify Secrets in CI

```yaml
- name: Verify required secrets
  run: |
    if [ -z "${{ secrets.JWT_SECRET }}" ]; then
      echo "ERROR: JWT_SECRET is not set" && exit 1
    fi
    if [ -z "${{ secrets.DATABASE_CONNECTION }}" ]; then
      echo "ERROR: DATABASE_CONNECTION is not set" && exit 1
    fi
    echo "All required secrets are present."
```

## Troubleshooting

**Q: Environment variables not being read in appsettings.json?**
A: .NET does not natively expand environment variables in JSON. Use `IConfiguration` with environment variable provider:

```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var jwtSecret = config["Jwt:Key"]; // Reads from env var JWT_SECRET
```

**Q: How do I test locally with different environments?**
A: Create environment-specific `.env` files:

```bash
# Load development config
export $(cat .env.development | xargs)
docker-compose -f docker-compose.yml up

# Load staging config
export $(cat .env.staging | xargs)
docker-compose -f docker-compose.unified.yml up
```

**Q: How do I rotate secrets without downtime?**
A: Update GitHub Secrets, trigger a new deployment (via commit or manual trigger), and Kubernetes/Docker will pick up the new values on restart.

