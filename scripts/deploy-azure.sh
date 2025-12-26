#!/bin/bash

# Azure Deployment Script for UmiHealth
# This script deploys the complete UmiHealth stack to Azure

set -e

# Configuration
RESOURCE_GROUP="umihealth-rg"
LOCATION="eastus"
ACR_NAME="umihealthacr"
AKS_CLUSTER_NAME="umihealth-aks"
NAMESPACE="umihealth"

echo "🚀 Starting Azure Deployment for UmiHealth..."

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI is not installed. Please install it first."
    exit 1
fi

# Login to Azure (if not already logged in)
echo "🔐 Checking Azure authentication..."
az account show > /dev/null 2>&1 || {
    echo "Please login to Azure:"
    az login
}

# Create Resource Group
echo "📦 Creating resource group..."
az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION

# Create Azure Container Registry
echo "📋 Creating Azure Container Registry..."
az acr create \
    --resource-group $RESOURCE_GROUP \
    --name $ACR_NAME \
    --sku Basic \
    --admin-enabled true

# Get ACR credentials
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "loginServer" --output tsv)
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "username" --output tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "passwords[0].value" --output tsv)

echo "🔑 ACR Login Server: $ACR_LOGIN_SERVER"

# Create AKS Cluster
echo "☸️ Creating AKS Cluster..."
az aks create \
    --resource-group $RESOURCE_GROUP \
    --name $AKS_CLUSTER_NAME \
    --node-count 3 \
    --enable-addons monitoring \
    --attach-acr $ACR_NAME \
    --generate-ssh-keys \
    --network-plugin azure \
    --network-service-cidr 10.0.0.0/16 \
    --dns-service-ip 10.0.0.10 \
    --service-cidr 10.0.0.0/16

# Get AKS credentials
echo "🔧 Getting AKS credentials..."
az aks get-credentials \
    --resource-group $RESOURCE_GROUP \
    --name $AKS_CLUSTER_NAME

# Create namespace
echo "📂 Creating Kubernetes namespace..."
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# Create Azure Database for PostgreSQL
echo "🐘 Creating Azure Database for PostgreSQL..."
POSTGRES_SERVER_NAME="umihealth-postgres-$(date +%s)"
az postgres server create \
    --resource-group $RESOURCE_GROUP \
    --name $POSTGRES_SERVER_NAME \
    --location $LOCATION \
    --admin-user umihealth \
    --admin-password $(openssl rand -base64 32) \
    --sku-name B_Gen5_2 \
    --version 15

# Configure PostgreSQL firewall
echo "🔥 Configuring PostgreSQL firewall..."
az postgres server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $POSTGRES_SERVER_NAME \
    --name "AllowAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0

# Create Azure Cache for Redis
echo "🔴 Creating Azure Cache for Redis..."
REDIS_CACHE_NAME="umihealth-redis-$(date +%s)"
az redis create \
    --resource-group $RESOURCE_GROUP \
    --name $REDIS_CACHE_NAME \
    --location $LOCATION \
    --sku Basic \
    --vm-size C0

# Get Redis connection string
REDIS_CONNECTION_STRING=$(az redis show \
    --resource-group $RESOURCE_GROUP \
    --name $REDIS_CACHE_NAME \
    --query "primaryKey" \
    --output tsv)

# Create Application Insights
echo "📊 Creating Application Insights..."
APP_INSIGHTS_NAME="umihealth-insights-$(date +%s)"
az monitor app-insights component create \
    --resource-group $RESOURCE_GROUP \
    --app $APP_INSIGHTS_NAME \
    --location $LOCATION \
    --application-type web

# Get Application Insights Instrumentation Key
APP_INSIGHTS_KEY=$(az monitor app-insights component show \
    --resource-group $RESOURCE_GROUP \
    --app $APP_INSIGHTS_NAME \
    --query "instrumentationKey" \
    --output tsv)

# Create Kubernetes secrets
echo "🔐 Creating Kubernetes secrets..."
kubectl create secret generic azure-secrets \
    --from-literal=acr-login-server=$ACR_LOGIN_SERVER \
    --from-literal=acr-username=$ACR_USERNAME \
    --from-literal=acr-password=$ACR_PASSWORD \
    --from-literal=postgres-connection=$(az postgres server show \
        --resource-group $RESOURCE_GROUP \
        --name $POSTGRES_SERVER_NAME \
        --query "fullyQualifiedDomainName" \
        --output tsv) \
    --from-literal=redis-connection=$REDIS_CONNECTION_STRING \
    --from-literal=app-insights-key=$APP_INSIGHTS_KEY \
    --namespace $NAMESPACE \
    --dry-run=client -o yaml | kubectl apply -f -

# Create Kubernetes ConfigMaps
echo "📝 Creating Kubernetes ConfigMaps..."
kubectl create configmap umihealth-config \
    --from-literal=aspnetcore-environment=Production \
    --from-literal=jwt-issuer=UmiHealth \
    --from-literal=jwt-audience=UmiHealthUsers \
    --namespace $NAMESPACE \
    --dry-run=client -o yaml | kubectl apply -f -

# Deploy to Kubernetes
echo "🚀 Deploying to Kubernetes..."
# Apply Kubernetes manifests
if [ -d "k8s" ]; then
    kubectl apply -f k8s/ --namespace $NAMESPACE
else
    echo "⚠️  k8s directory not found. Please create Kubernetes manifests."
fi

# Wait for deployments
echo "⏳ Waiting for deployments to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment --all --namespace $NAMESPACE

# Get external IP
echo "🌐 Getting external IP..."
EXTERNAL_IP=$(kubectl get service umihealth-api --namespace $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

echo "✅ Deployment completed successfully!"
echo ""
echo "📋 Deployment Summary:"
echo "Resource Group: $RESOURCE_GROUP"
echo "AKS Cluster: $AKS_CLUSTER_NAME"
echo "ACR Registry: $ACR_LOGIN_SERVER"
echo "PostgreSQL Server: $POSTGRES_SERVER_NAME"
echo "Redis Cache: $REDIS_CACHE_NAME"
echo "Application Insights: $APP_INSIGHTS_NAME"
echo ""
echo "🌐 Access URLs:"
echo "API: http://$EXTERNAL_IP"
echo "Kubernetes Dashboard: az aks browse --resource-group $RESOURCE_GROUP --name $AKS_CLUSTER_NAME"
echo ""
echo "🔧 Next Steps:"
echo "1. Update your DNS to point to the external IP"
echo "2. Configure SSL certificates"
echo "3. Set up monitoring alerts"
echo "4. Run database migrations"
