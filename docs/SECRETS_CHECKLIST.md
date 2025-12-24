Secrets Checklist

Purpose: list required repository and cloud secrets for CI/CD, runtime, and monitoring.

Repository-level (GitHub Actions)
- DATABASE_CONNECTION: Full Postgres connection string (including username/password). Used by EF Core migrations and runtime services.
- ACR_LOGIN_SERVER: e.g. myregistry.azurecr.io (if using ACR). Optional if using GHCR.
- ACR_USERNAME / ACR_PASSWORD: Credentials for ACR (or use service principal). Alternatively set `CR_PAT` for GHCR.
- CR_PAT: Personal access token for GitHub Container Registry (GHCR) when publishing from workflows.
- AZURE_CREDENTIALS: Service Principal JSON used by `azure/login` action. Create via `az ad sp create-for-rbac --name "umihealth-deploy" --role contributor --scopes /subscriptions/<id>/resourceGroups/<rg>` and store the JSON output.
- AZURE_SUBSCRIPTION_ID: Azure subscription ID used for deployments.
- AZURE_RESOURCE_GROUP: Target resource group name for production deployments.
- AZURE_WEBAPP_NAME: Production App Service name (if using App Service deployment).
- ACR_RESOURCE: ACR name (if using ACR builds) and ACR_RESOURCE_GROUP.
- APPINSIGHTS_INSTRUMENTATIONKEY: Application Insights key for telemetry.
- REDIS_CONNECTION: Redis connection URL (if used).
- STORAGE_CONNECTION_STRING: Blob storage connection used for exports/backups.
- SMTP__HOST, SMTP__PORT, SMTP__USERNAME, SMTP__PASSWORD: SMTP credentials for email delivery.
- JWT__PRIVATE_KEY / JWT__PUBLIC_KEY or JWKS_URL: Keys or endpoint used for signing/verification of JWTs.
- SENTRY_DSN (optional): Sentry DSN for error reporting.
- THIRD_PARTY_API_KEYS (e.g., payments, SMS): any external provider keys (PAYMENTS_API_KEY, SMS_API_KEY).

CI/CD / Pipeline secrets (Azure Pipelines / GitHub Actions)
- dockerRegistryServiceConnection (Azure Pipelines): service connection name for container registry.
- DOCKER_USERNAME / DOCKER_PASSWORD (if pushing to custom registry from pipeline).
- DB_MIGRATION_USER / DB_MIGRATION_PASSWORD (if migration user differs from runtime user).

Secrets Storage Recommendations
- Prefer Azure Key Vault for production secrets and grant the app managed identity access. Store only a small set of pipeline secrets in GitHub and use Key Vault references for runtime.
- Limit scope of PATs and service principals; give least privilege needed.
- Rotate secrets regularly and document rotation steps in the runbook.

How to set GitHub Actions secrets (example):
- Using GitHub CLI:
  gh secret set DATABASE_CONNECTION --body "$env:DATABASE_CONNECTION" --repo owner/repo
- Manual: Settings → Secrets → Actions → New repository secret

How to set Azure Web App app settings / Key Vault reference (example):
- Using Azure CLI (app settings):
  az webapp config appsettings set --resource-group $RG --name $WEBAPP --settings "DATABASE_CONNECTION=$DB_CONN"
- To reference Key Vault secrets from App Service, use Key Vault references with `@Microsoft.KeyVault(SecretUri=...)`.

Checklist before first production deploy
- Ensure `AZURE_CREDENTIALS` is set in GitHub secrets and points to a service principal with rights to the target resource group.
- Confirm `DATABASE_CONNECTION` points to the production database and that a safe migration window is approved.
- Ensure backups/snapshots are scheduled for the production DB and tested restore documented.
- Validate App Insights and monitoring secrets are present (APPINSIGHTS_INSTRUMENTATIONKEY).
- Ensure `ACR` or `GHCR` credentials are set and pipeline has permission to push images.
- Ensure the monitoring alerting channels (PagerDuty, Ops Slack, Email) are configured with their API keys/secrets.

Notes
- Never commit secrets to source control. Use secrets management (Key Vault, GitHub Actions secrets, or environment-specific secure stores).
- For multi-tenant deployments, secrets that differ per-tenant should be stored and accessed per-tenant, not in the repo secrets.
