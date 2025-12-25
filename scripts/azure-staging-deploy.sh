#!/usr/bin/env bash
# Azure staging deployment script
# Usage: ./azure-staging-deploy.sh <resourceGroup> <location> <acrName> <appServicePlan> <webAppName> <postgresServerName> <redisName>

set -euo pipefail

RESOURCE_GROUP=${1:-umihealth-staging-rg}
LOCATION=${2:-eastus}
ACR_NAME=${3:-umihealthacr}
APP_PLAN=${4:-umihealth-plan}
WEBAPP=${5:-umihealth-staging}
POSTGRES=${6:-umihealth-pg}
REDIS=${7:-umihealth-redis}

echo "Creating resource group: $RESOURCE_GROUP in $LOCATION"
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "Creating ACR: $ACR_NAME"
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Standard --admin-enabled true

ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query loginServer -o tsv)

echo "ACR login server: $ACR_LOGIN_SERVER"

echo "Build and push images to ACR"
# Assumes Docker is logged in to ACR or will use az acr build
az acr build --registry $ACR_NAME --image "$ACR_LOGIN_SERVER/umihealth-api:latest" --file backend/Dockerfile backend/
az acr build --registry $ACR_NAME --image "$ACR_LOGIN_SERVER/umihealth-identity:latest" --file backend/UmiHealth.Identity/Dockerfile backend/UmiHealth.Identity/
az acr build --registry $ACR_NAME --image "$ACR_LOGIN_SERVER/umihealth-jobs:latest" --file backend/UmiHealth.Jobs/Dockerfile backend/UmiHealth.Jobs/

echo "Creating App Service Plan: $APP_PLAN"
az appservice plan create --name $APP_PLAN --resource-group $RESOURCE_GROUP --is-linux --sku B1

echo "Creating Web App: $WEBAPP"
az webapp create --resource-group $RESOURCE_GROUP --plan $APP_PLAN --name $WEBAPP --deployment-container-image-name $ACR_LOGIN_SERVER/umihealth-api:latest || true

echo "Configure Web App to use ACR image"
az webapp config container set --name $WEBAPP --resource-group $RESOURCE_GROUP --docker-custom-image-name $ACR_LOGIN_SERVER/umihealth-api:latest --docker-registry-server-url https://$ACR_LOGIN_SERVER

echo "Create Azure Database for PostgreSQL server: $POSTGRES"
az postgres server create --resource-group $RESOURCE_GROUP --name $POSTGRES --location $LOCATION --admin-user umihealth --admin-password "$(openssl rand -base64 16)" --sku-name B_Gen5_1 --version 15

echo "Create Azure Cache for Redis: $REDIS"
az redis create --resource-group $RESOURCE_GROUP --name $REDIS --sku Standard --vm-size c1 --location $LOCATION

echo "Deployment complete. Outputs:"
az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP
az webapp show --name $WEBAPP --resource-group $RESOURCE_GROUP --query defaultHostName -o tsv

