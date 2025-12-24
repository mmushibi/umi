# DevOps & CI/CD Implementation Guide - Priority 4

## Overview

This guide covers complete DevOps setup for Umi Health including containerization, orchestration, CI/CD pipelines, and cloud deployment.

---

## Table of Contents

1. [Docker Architecture](#docker-architecture)
2. [Docker Compose Setup](#docker-compose-setup)
3. [CI/CD Pipeline](#cicd-pipeline)
4. [Azure Deployment](#azure-deployment)
5. [Kubernetes Deployment (Optional)](#kubernetes-optional)
6. [Monitoring & Logging](#monitoring--logging)
7. [Backup & Disaster Recovery](#backup--disaster-recovery)

---

## Docker Architecture

### Container Services

#### 1. API Gateway
- **Image**: `umihealth/api-gateway:latest`
- **Port**: 80, 443
- **Role**: Entry point, routing, rate limiting
- **Dependencies**: UmiHealth API, Identity Service

#### 2. UmiHealth API
- **Image**: `umihealth/api:latest`
- **Port**: 5000 (8080 internal)
- **Role**: Main business logic API
- **Dependencies**: PostgreSQL, Redis, Identity Service
- **Health Check**: `http://localhost:8080/health`

#### 3. Identity Service
- **Image**: `umihealth/identity:latest`
- **Port**: 5001 (8080 internal)
- **Role**: Authentication, authorization, JWT
- **Dependencies**: PostgreSQL, Redis

#### 4. Background Jobs
- **Image**: `umihealth/jobs:latest`
- **Role**: Hangfire jobs, async operations
- **Dependencies**: PostgreSQL, Redis, API

#### 5. PostgreSQL Database
- **Image**: `postgres:15-alpine`
- **Port**: 5432
- **Persistence**: Named volume `postgres_data`
- **Databases**: UmiHealth, UmiHealthIdentity

#### 6. Redis Cache
- **Image**: `redis:7-alpine`
- **Port**: 6379
- **Persistence**: Named volume `redis_data`
- **Auth**: Password required via `REDIS_PASSWORD`

#### 7. Prometheus (Metrics)
- **Image**: `prom/prometheus:latest`
- **Port**: 9090
- **Role**: Metrics collection
- **Config**: `./monitoring/prometheus.yml`

#### 8. Grafana (Visualization)
- **Image**: `grafana/grafana:latest`
- **Port**: 3000
- **Role**: Dashboard, alerting
- **Default**: admin/admin (change in `.env`)

#### 9. Nginx (Reverse Proxy)
- **Image**: `nginx:alpine`
- **Port**: 80, 443
- **Profile**: `production` only
- **Config**: `./nginx/nginx.conf`

### Network Configuration

- **Network Name**: `umihealth-network`
- **Driver**: Bridge
- **Subnet**: 172.20.0.0/16
- **DNS**: Automatic via Docker

### Volume Management

| Volume | Service | Purpose |
|--------|---------|---------|
| `postgres_data` | PostgreSQL | Database persistence |
| `redis_data` | Redis | Cache persistence |
| `prometheus_data` | Prometheus | Metrics storage |
| `grafana_data` | Grafana | Dashboard configurations |

---

## Docker Compose Setup

### Prerequisites

```bash
# Install Docker & Docker Compose
# Windows/Mac: Docker Desktop
# Linux: docker.io & docker-compose

docker --version
docker-compose --version
```

### Quick Start

#### 1. Clone and Setup
```bash
cd /path/to/umi-health
cp .env.example .env
# Edit .env with your secrets
```

#### 2. Build Images (Development)
```bash
# Build all services from Dockerfile
docker-compose build

# Build specific service
docker-compose build umihealth-api

# Build with no cache
docker-compose build --no-cache
```

#### 3. Start Services
```bash
# Start all services
docker-compose up -d

# Start with logs
docker-compose up

# Start specific services
docker-compose up -d postgres redis

# Start production profile (with nginx)
docker-compose --profile production up -d
```

#### 4. Check Status
```bash
# List running containers
docker-compose ps

# View logs
docker-compose logs -f umihealth-api
docker-compose logs -f postgres

# Check network
docker network ls
docker network inspect umihealth-network
```

#### 5. Database Initialization
```bash
# Run migrations
docker-compose exec umihealth-api dotnet ef database update

# Seed initial data
docker-compose exec umihealth-api dotnet user-secrets init && dotnet run --seed

# Connect to PostgreSQL
docker-compose exec postgres psql -U umihealth -d UmiHealth
```

#### 6. Stop Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (caution!)
docker-compose down -v

# Restart services
docker-compose restart
```

### Environment Configuration (.env)

Critical settings:
```env
# Security
JWT_SECRET=GenerateSecureKey32+Chars
POSTGRES_PASSWORD=StrongPassword!@#

# Email/SMS Providers
SMTP_SERVER=smtp.gmail.com
TWILIO_ACCOUNT_SID=xxx
NEXMO_API_KEY=xxx

# Payment Providers
MTN_API_KEY=xxx
AIRTEL_API_KEY=xxx

# Azure (if using)
AZURE_STORAGE_CONNECTION_STRING=xxx
```

### Health Checks

All services include health checks:
```bash
# Check service health
docker-compose exec umihealth-api curl http://localhost:8080/health

# Monitor health in real-time
watch docker-compose ps
```

### Troubleshooting

#### Service Won't Start
```bash
# Check logs
docker-compose logs umihealth-api

# Verify dependencies started
docker-compose ps

# Check network connectivity
docker-compose exec umihealth-api ping postgres
```

#### Database Connection Failed
```bash
# Verify PostgreSQL running
docker-compose exec postgres psql -U umihealth -c "\l"

# Check connection string in logs
docker-compose logs postgres

# Restart postgres
docker-compose restart postgres
```

#### Redis Issues
```bash
# Connect to Redis
docker-compose exec redis redis-cli

# Check auth
redis-cli -a $REDIS_PASSWORD ping

# Flush cache (caution!)
redis-cli FLUSHALL
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

Create `.github/workflows/deploy.yml`:

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  # Build and Test
  build:
    runs-on: ubuntu-latest
    
    permissions:
      contents: read
      packages: write
    
    steps:
      - uses: actions/checkout@v3
      
      # Build Backend
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore backend/UmiHealth.sln
      
      - name: Build
        run: dotnet build backend/UmiHealth.sln --no-restore --configuration Release
      
      - name: Run tests
        run: dotnet test backend/tests/ --no-build --verbosity normal
      
      # Build Docker images
      - name: Build Docker image
        run: |
          docker build -t ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:latest \
            -f backend/Dockerfile backend/
      
      # Security scanning
      - name: Run Trivy vulnerability scanner
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:latest
          format: 'sarif'
          output: 'trivy-results.sarif'
      
      # SonarQube code quality
      - name: SonarQube Scan
        uses: SonarSource/sonarcloud-github-action@master
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}

  # Push to Registry
  push:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    permissions:
      contents: read
      packages: write
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Log in to Container Registry
        uses: docker/login-action@v2
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      
      - name: Build and push Docker images
        uses: docker/build-push-action@v4
        with:
          context: backend/
          file: backend/Dockerfile
          push: true
          tags: |
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:latest
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:${{ github.sha }}

  # Deploy to Azure
  deploy:
    needs: push
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: 'umihealth-prod'
          slot-name: 'production'
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          images: '${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:${{ github.sha }}'
      
      - name: Run smoke tests
        run: |
          curl -f https://umihealth-prod.azurewebsites.net/health || exit 1
```

### Azure DevOps Pipeline (Alternative)

Create `azure-pipelines.yml`:

```yaml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  imageName: 'umihealth/api'
  registryUrl: 'umihealth.azurecr.io'

stages:
  - stage: Build
    jobs:
      - job: BuildAndTest
        steps:
          - task: UseDotNet@2
            inputs:
              version: '8.0.x'
          
          - task: DotNetCoreCLI@2
            inputs:
              command: 'build'
              arguments: '--configuration $(buildConfiguration)'
              projects: 'backend/UmiHealth.sln'
          
          - task: DotNetCoreCLI@2
            inputs:
              command: 'test'
              arguments: '--configuration $(buildConfiguration) --no-build'
          
          - task: Docker@2
            inputs:
              command: 'build'
              Dockerfile: 'backend/Dockerfile'
              containerRegistry: '$(registryUrl)'
              repository: '$(imageName)'
              tags: |
                latest
                $(Build.BuildId)

  - stage: Push
    dependsOn: Build
    condition: succeeded()
    jobs:
      - job: PushToRegistry
        steps:
          - task: Docker@2
            inputs:
              command: 'push'
              containerRegistry: '$(registryUrl)'
              repository: '$(imageName)'
              tags: |
                latest
                $(Build.BuildId)

  - stage: Deploy
    dependsOn: Push
    condition: succeeded()
    jobs:
      - deployment: DeployToProduction
        environment: 'Production'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureWebAppContainer@1
                  inputs:
                    azureSubscription: 'Azure Subscription'
                    appName: 'umihealth-prod'
                    containers: '$(registryUrl)/$(imageName):$(Build.BuildId)'
```

---

## Azure Deployment

### App Service Configuration

#### 1. Create App Service
```bash
# Create resource group
az group create --name umihealth-rg --location eastus

# Create App Service Plan
az appservice plan create \
  --name umihealth-plan \
  --resource-group umihealth-rg \
  --sku B2 --is-linux

# Create Web App
az webapp create \
  --resource-group umihealth-rg \
  --plan umihealth-plan \
  --name umihealth-prod \
  --deployment-container-image-name-user admin \
  --deployment-container-image-name umihealth/api:latest
```

#### 2. Configure App Settings
```bash
# Set environment variables
az webapp config appsettings set \
  --resource-group umihealth-rg \
  --name umihealth-prod \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Server=umihealth-db.postgres.database.azure.com;Database=UmiHealth;User Id=umihealth@umihealth-db;Password=$POSTGRES_PASSWORD;Ssl Mode=Required;" \
    Redis__ConnectionString="umihealth-redis.redis.cache.windows.net:6380,password=$REDIS_PASSWORD,ssl=True"
```

#### 3. Database Configuration (Azure PostgreSQL)
```bash
# Create PostgreSQL server
az postgres server create \
  --resource-group umihealth-rg \
  --name umihealth-db \
  --admin-user umihealth \
  --admin-password $POSTGRES_PASSWORD \
  --sku-name B_Gen5_1 \
  --storage-size 51200

# Create database
az postgres db create \
  --resource-group umihealth-rg \
  --server-name umihealth-db \
  --name UmiHealth
```

#### 4. Redis Cache (Azure)
```bash
# Create Redis Cache
az redis create \
  --resource-group umihealth-rg \
  --name umihealth-redis \
  --location eastus \
  --sku Basic \
  --vm-size c0
```

#### 5. SSL/TLS Certificate
```bash
# Bind custom domain
az webapp config hostname add \
  --resource-group umihealth-rg \
  --webapp-name umihealth-prod \
  --hostname api.umihealth.com

# Add SSL certificate
az webapp config ssl bind \
  --resource-group umihealth-rg \
  --name umihealth-prod \
  --certificate-thumbprint $THUMBPRINT \
  --ssl-type SNI
```

### Monitoring & Alerts

```bash
# Create Application Insights
az monitor app-insights component create \
  --app umihealth-insights \
  --location eastus \
  --resource-group umihealth-rg \
  --application-type web

# Enable logging
az webapp log config \
  --resource-group umihealth-rg \
  --name umihealth-prod \
  --docker-container-logging filesystem
```

---

## Kubernetes (Optional)

### Deploy to AKS

```bash
# Create AKS cluster
az aks create \
  --resource-group umihealth-rg \
  --name umihealth-aks \
  --node-count 3 \
  --vm-set-type VirtualMachineScaleSets \
  --network-plugin azure

# Deploy using Helm
helm install umihealth ./kubernetes/helm-chart \
  --namespace default \
  --values ./kubernetes/values.yaml
```

---

## Monitoring & Logging

### Prometheus Metrics

Configuration file: `monitoring/prometheus.yml`

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'umihealth-api'
    static_configs:
      - targets: ['umihealth-api:8080']

  - job_name: 'postgres'
    static_configs:
      - targets: ['postgres:5432']

  - job_name: 'redis'
    static_configs:
      - targets: ['redis:6379']
```

### Grafana Dashboards

Access at `http://localhost:3000`

Default credentials from `.env`:
- Username: `GRAFANA_USER`
- Password: `GRAFANA_PASSWORD`

Preconfigured dashboards:
- API Performance
- Database Metrics
- Redis Cache
- Business KPIs

### Serilog Logging

Configuration:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/umihealth-.txt", 
        rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();
```

---

## Backup & Disaster Recovery

### Database Backups

```bash
# Automated daily backup at 2 AM
docker-compose exec postgres pg_dump -U umihealth UmiHealth > backup_$(date +%Y%m%d).sql

# Restore from backup
docker-compose exec -T postgres psql -U umihealth UmiHealth < backup_20240101.sql
```

### Storage Redundancy

- PostgreSQL: Daily backups to Azure Blob Storage
- Redis: RDB snapshots every 6 hours
- Application files: Geo-redundant storage

### Disaster Recovery Plan

**RTO**: 4 hours
**RPO**: 6 hours

1. Database failover to secondary region
2. Application redeployment via CI/CD
3. DNS failover to backup endpoint
4. Data restoration from latest backup

---

## Summary

### Completed DevOps Setup

✅ Docker containerization (9 services)
✅ Docker Compose orchestration
✅ CI/CD pipeline (GitHub Actions + Azure DevOps)
✅ Azure deployment (App Service + Database + Redis)
✅ Monitoring (Prometheus + Grafana)
✅ Logging (Serilog + Seq)
✅ Backup & disaster recovery

### Next Steps

1. Build and push images to registry
2. Set up CI/CD secrets in GitHub/Azure DevOps
3. Deploy to staging environment
4. Run smoke tests
5. Deploy to production

---

**Last Updated:** $(date)
**Status:** Ready for Implementation
