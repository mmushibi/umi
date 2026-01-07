# GitHub Actions Secrets Verification Checklist

## How to Add Secrets to GitHub Repository

1. Go to your repository on GitHub
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter the secret name (key) and value
5. Click **Add secret**

---

## Required Secrets Checklist

### ✅ Production Deployment Secrets
Use this checklist to verify all required secrets are configured before deploying to production.

#### Azure Container Registry (ACR)
- [ ] `ACR_LOGIN_SERVER`
  - **Description:** Azure Container Registry login server
  - **Format:** FQDN (e.g., `myacr.azurecr.io`)
  - **How to get:** `az acr show --name <acr-name> --query loginServer --output tsv`

- [ ] `ACR_USERNAME`
  - **Description:** Service Principal Client ID or username for ACR access
  - **Format:** String (UUID for service principal)
  - **How to get:** `az ad sp show --id <app-id> --query appId --output tsv`

- [ ] `ACR_PASSWORD`
  - **Description:** Service Principal Client Secret or password for ACR access
  - **Format:** Secure string
  - **How to get:** Create or retrieve from Azure Portal → App registrations → Certificates & secrets

#### GitHub Container Registry (GHCR)
- [ ] `CR_PAT`
  - **Description:** GitHub Container Registry Personal Access Token
  - **Format:** Token (starts with `ghp_`)
  - **How to get:** GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens
  - **Required scopes:** `read:packages`, `write:packages`, `delete:packages`

#### Azure Web App Deployment
- [ ] `AZURE_PUBLISH_PROFILE`
  - **Description:** Azure Web App publish profile (XML)
  - **Format:** Base64-encoded XML or raw XML
  - **How to get:** Azure Portal → Web App → Download publish profile (`.PublishSettings` file)
  - **Note:** Contains sensitive credentials; store as secret only

- [ ] `AZURE_WEBAPP_NAME`
  - **Description:** Name of the Azure Web App
  - **Format:** String (e.g., `umihealth-prod`)
  - **How to get:** Azure Portal → Web App → Overview → Name

- [ ] `AZURE_WEBAPP_DEFAULT_HOSTNAME`
  - **Description:** Default HTTPS hostname of the Web App
  - **Format:** FQDN (e.g., `umihealth-prod.azurewebsites.net`)
  - **How to get:** Azure Portal → Web App → Overview → Default domain

#### Database & Connection Strings
- [ ] `DATABASE_CONNECTION`
  - **Description:** Production database connection string
  - **Format:** Connection string
  - **Example:** `Host=db.prod.com;Port=5432;Database=umihealth;Username=pguser;Password=SECURE_PASSWORD`
  - **How to generate:** Use secure password (min 20 chars, uppercase, lowercase, numbers, symbols)

#### JWT & Authentication
- [ ] `JWT_SECRET`
  - **Description:** JWT signing secret key
  - **Format:** Cryptographically secure string (min 32 characters)
  - **How to generate:** 
    ```powershell
    # PowerShell
    [Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Minimum 0 -Maximum 256) }))
    ```
  - **Security:** Must be kept confidential; used to sign and verify JWT tokens

- [ ] `JWT_ISSUER`
  - **Description:** JWT issuer claim (non-sensitive)
  - **Format:** String (e.g., `UmiHealth`)
  - **Default:** `UmiHealth` (can be left as default)

- [ ] `JWT_AUDIENCE`
  - **Description:** JWT audience claim (non-sensitive)
  - **Format:** String (e.g., `UmiHealthUsers`)
  - **Default:** `UmiHealthUsers` (can be left as default)

#### Infrastructure Secrets
- [ ] `POSTGRES_PASSWORD`
  - **Description:** PostgreSQL admin password
  - **Format:** Secure password (min 16 chars)
  - **How to generate:** Use a password manager or secure random generator

- [ ] `REDIS_PASSWORD`
  - **Description:** Redis server password
  - **Format:** Secure password (min 16 chars)
  - **How to generate:** Use a password manager or secure random generator

- [ ] `ENCRYPTION_KEY`
  - **Description:** AES-256 encryption key (base64-encoded)
  - **Format:** Base64 string (256-bit = 32 bytes → 44 chars in base64)
  - **How to generate:**
    ```powershell
    # PowerShell
    $key = [byte[]]::new(32)
    [System.Security.Cryptography.RNGCryptoServiceProvider]::new().GetBytes($key)
    [Convert]::ToBase64String($key)
    ```

#### Email / SMTP Secrets
- [ ] `SMTP_SERVER`
  - **Description:** SMTP server hostname
  - **Format:** FQDN (e.g., `smtp.sendgrid.net`)
  - **How to get:** SendGrid → Settings → SMTP Relay or your email provider

- [ ] `SMTP_PORT`
  - **Description:** SMTP port number
  - **Format:** Integer (usually 587 for TLS, 465 for SSL)
  - **Default:** 587

- [ ] `SMTP_USERNAME`
  - **Description:** SMTP username or API key
  - **Format:** String (often `apikey` for SendGrid)
  - **How to get:** SendGrid → Settings → API Keys

- [ ] `SMTP_PASSWORD`
  - **Description:** SMTP password or API key
  - **Format:** Secure string
  - **How to get:** SendGrid → Settings → API Keys (generate new key)

#### Monitoring & Analytics
- [ ] `GRAFANA_USER`
  - **Description:** Grafana admin username (non-sensitive)
  - **Format:** String (e.g., `admin`)
  - **Default:** `admin` (can use default)

- [ ] `GRAFANA_PASSWORD`
  - **Description:** Grafana admin password
  - **Format:** Secure password (min 12 chars)
  - **How to generate:** Use a password manager

#### Application-Specific Secrets
- [ ] `ADMIN_PASSWORD`
  - **Description:** Default admin user password for automated tests
  - **Format:** Secure password
  - **Used for:** Postman smoke tests in CI/CD
  - **Note:** Should be changed on first login in production

---

## Verification Script

Run this GitHub Actions workflow step to verify all required secrets are present:

```yaml
- name: Verify all required secrets
  run: |
    REQUIRED_SECRETS=(
      "ACR_LOGIN_SERVER"
      "ACR_USERNAME"
      "ACR_PASSWORD"
      "CR_PAT"
      "AZURE_PUBLISH_PROFILE"
      "AZURE_WEBAPP_NAME"
      "AZURE_WEBAPP_DEFAULT_HOSTNAME"
      "DATABASE_CONNECTION"
      "JWT_SECRET"
      "POSTGRES_PASSWORD"
      "REDIS_PASSWORD"
      "ENCRYPTION_KEY"
      "SMTP_SERVER"
      "SMTP_PORT"
      "SMTP_USERNAME"
      "SMTP_PASSWORD"
      "GRAFANA_USER"
      "GRAFANA_PASSWORD"
      "ADMIN_PASSWORD"
    )
    
    MISSING=()
    for secret in "${REQUIRED_SECRETS[@]}"; do
      if [ -z "$(eval echo \$$secret)" ]; then
        MISSING+=("$secret")
      fi
    done
    
    if [ ${#MISSING[@]} -gt 0 ]; then
      echo "❌ Missing required secrets:"
      printf '%s\n' "${MISSING[@]}"
      exit 1
    fi
    
    echo "✅ All required secrets are configured."
```

---

## Security Best Practices

### During Secret Creation
- ✅ Use **cryptographically secure random generators** for passwords and keys
- ✅ Use **minimum length requirements**: 
  - Passwords: 16+ characters
  - API keys/secrets: 32+ characters
  - Encryption keys: 256-bit (32 bytes) for AES-256
- ✅ Include **uppercase, lowercase, numbers, and symbols** in passwords
- ✅ **Never reuse** secrets across environments

### After Secret Creation
- ✅ **Store in GitHub Secrets** immediately (not in .env files or committed to repo)
- ✅ **Audit access** via GitHub Secrets page (shows who accessed each secret)
- ✅ **Enable branch protection** to prevent secret exposure via commits
- ✅ **Rotate secrets regularly** (every 90 days recommended)
- ✅ **Revoke compromised secrets** immediately

### CI/CD Integration
- ✅ **Use secrets in workflow steps** via `${{ secrets.SECRET_NAME }}`
- ✅ **Never print secrets** to logs (GitHub will mask them automatically)
- ✅ **Validate secrets exist** before using them in deployments
- ✅ **Use service principals** with least-privilege roles in Azure

---

## Common Issues & Troubleshooting

### Issue: "Secret not available in workflow"
**Solution:** Secrets are only available to workflows on the `main` or `develop` branch (by default). Verify your branch protection rules.

### Issue: "Invalid ACR credentials"
**Solution:** Ensure the service principal has `AcrPush` and `AcrPull` roles in IAM.

### Issue: "Database connection timeout"
**Solution:** Verify the database server is accessible from the deployment environment. Check firewall rules and network ACLs.

### Issue: "JWT token validation fails"
**Solution:** Ensure `JWT_SECRET` is identical across all services. Re-generate and update all references if in doubt.

### Issue: "Postman smoke tests fail with 401 Unauthorized"
**Solution:** Verify `ADMIN_PASSWORD` is correct and the admin user exists in production. Check token generation step in CI workflow.

---

## Post-Deployment Validation

After deploying with secrets, verify:

```bash
# 1. Check if API is healthy
curl -f https://your-api-hostname/health

# 2. Test authentication
curl -X POST https://your-api-hostname/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@umihealth.com",
    "password": "YOUR_ADMIN_PASSWORD",
    "tenantSubdomain": "PROD"
  }'

# 3. Run Postman smoke tests
bash api-testing/scripts/run-postman-tests.sh Production https://your-api-hostname "TOKEN_FROM_AUTH"
```

---

## Related Documentation

- [CONFIGURATION_SECRETS.md](./CONFIGURATION_SECRETS.md) - Full configuration and secrets management guide
- [GitHub Actions Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/key-vault/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
