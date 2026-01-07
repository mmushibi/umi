# Multi-stage Dockerfile for API Gateway
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/UmiHealth.ApiGateway/UmiHealth.ApiGateway.csproj", "src/UmiHealth.ApiGateway/"]
COPY ["src/UmiHealth.Core/UmiHealth.Core.csproj", "src/UmiHealth.Core/"]
COPY ["src/UmiHealth.Shared/UmiHealth.Shared.csproj", "src/UmiHealth.Shared/"]

RUN dotnet restore "src/UmiHealth.ApiGateway/UmiHealth.ApiGateway.csproj"

# Copy rest of source code
COPY . .

# Build application
WORKDIR "/src/src/UmiHealth.ApiGateway"
RUN dotnet build "UmiHealth.ApiGateway.csproj" -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "UmiHealth.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN addgroup --system gateway && adduser --system --group gateway

# Copy published files
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R gateway:gateway /app
USER gateway

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "UmiHealth.ApiGateway.dll"]
