# Optimized Dockerfile for UmiHealth API with network resilience
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Configure environment for better reliability
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV NUGET_XMLDOC_MODE=skip
ENV NUGET_CREDENTIALPROVIDERS_PATH=/tmp/nuget-credential-providers

# Create NuGet config with fallback sources
RUN echo '<?xml version="1.0" encoding="utf-8"?>' > /tmp/NuGet.Config && \
    echo '<configuration>' >> /tmp/NuGet.Config && \
    echo '  <packageSources>' >> /tmp/NuGet.Config && \
    echo '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />' >> /tmp/NuGet.Config && \
    echo '  </packageSources>' >> /tmp/NuGet.Config && \
    echo '  <config>' >> /tmp/NuGet.Config && \
    echo '    <add key="globalPackagesFolder" value="/root/.nuget/packages" />' >> /tmp/NuGet.Config && \
    echo '  </config>' >> /tmp/NuGet.Config && \
    echo '</configuration>' >> /tmp/NuGet.Config

# Copy project files first for better caching
COPY ["src/UmiHealth.API/UmiHealth.API.csproj", "src/UmiHealth.API/"]
COPY ["src/UmiHealth.Core/UmiHealth.Core.csproj", "src/UmiHealth.Core/"]
COPY ["src/UmiHealth.Infrastructure/UmiHealth.Infrastructure.csproj", "src/UmiHealth.Infrastructure/"]
COPY ["src/UmiHealth.Application/UmiHealth.Application.csproj", "src/UmiHealth.Application/"]
COPY ["src/UmiHealth.Domain/UmiHealth.Domain.csproj", "src/UmiHealth.Domain/"]
COPY ["src/UmiHealth.Shared/UmiHealth.Shared.csproj", "src/UmiHealth.Shared/"]
COPY ["src/UmiHealth.Identity/UmiHealth.Identity.csproj", "src/UmiHealth.Identity/"]
COPY ["src/UmiHealth.Persistence/UmiHealth.Persistence.csproj", "src/UmiHealth.Persistence/"]

# Restore with retry logic and custom config
RUN dotnet restore src/UmiHealth.API/UmiHealth.API.csproj \
    --configfile /tmp/NuGet.Config \
    --verbosity minimal \
    --force || \
    dotnet restore src/UmiHealth.API/UmiHealth.API.csproj \
    --configfile /tmp/NuGet.Config \
    --verbosity minimal

# Copy the rest of the source code
COPY src/ ./src/

# Build the application
WORKDIR /src/src/UmiHealth.API
RUN dotnet build UmiHealth.API.csproj -c Release -o /app/build --no-restore || \
    dotnet build UmiHealth.API.csproj -c Release -o /app/build --no-restore

# Publish the application
RUN dotnet publish UmiHealth.API.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore || \
    dotnet publish UmiHealth.API.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create non-root user for security
RUN addgroup --system --gid 1001 appuser && \
    adduser --system --uid 1001 --ingroup appuser appuser

# Set ownership and permissions
RUN chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "UmiHealth.API.dll"]
