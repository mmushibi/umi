# Multi-stage build for UmiHealth Identity Service
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["UmiHealth.Identity/UmiHealth.Identity.csproj", "UmiHealth.Identity/"]
COPY ["UmiHealth.Core/UmiHealth.Core.csproj", "UmiHealth.Core/"]
COPY ["UmiHealth.Infrastructure/UmiHealth.Infrastructure.csproj", "UmiHealth.Infrastructure/"]
COPY ["UmiHealth.Application/UmiHealth.Application.csproj", "UmiHealth.Application/"]
COPY ["UmiHealth.Domain/UmiHealth.Domain.csproj", "UmiHealth.Domain/"]
COPY ["UmiHealth.Shared/UmiHealth.Shared.csproj", "UmiHealth.Shared/"]

RUN dotnet restore "UmiHealth.Identity/UmiHealth.Identity.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/UmiHealth.Identity"
RUN dotnet build "UmiHealth.Identity.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "UmiHealth.Identity.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN addgroup --system app && adduser --system --group app

# Copy published files
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R app:app /app
USER app

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "UmiHealth.Identity.dll"]
