# Multi-stage Dockerfile for UmiHealth API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Configure NuGet for better reliability
ENV NUGET_XMLDOC_MODE=skip
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Copy csproj files and restore dependencies with retry logic
COPY ["src/UmiHealth.API/UmiHealth.API.csproj", "src/UmiHealth.API/"]
COPY ["src/UmiHealth.Core/UmiHealth.Core.csproj", "src/UmiHealth.Core/"]
COPY ["src/UmiHealth.Infrastructure/UmiHealth.Infrastructure.csproj", "src/UmiHealth.Infrastructure/"]
COPY ["src/UmiHealth.Application/UmiHealth.Application.csproj", "src/UmiHealth.Application/"]
COPY ["src/UmiHealth.Domain/UmiHealth.Domain.csproj", "src/UmiHealth.Domain/"]
COPY ["src/UmiHealth.Shared/UmiHealth.Shared.csproj", "src/UmiHealth.Shared/"]
COPY ["src/UmiHealth.Identity/UmiHealth.Identity.csproj", "src/UmiHealth.Identity/"]
COPY ["src/UmiHealth.Persistence/UmiHealth.Persistence.csproj", "src/UmiHealth.Persistence/"]

# Restore with retry logic and increased timeout
RUN dotnet restore "src/UmiHealth.API/UmiHealth.API.csproj" \
    --source https://api.nuget.org/v3/index.json \
    --verbosity normal \
    --timeout 1200 || \
    dotnet restore "src/UmiHealth.API/UmiHealth.API.csproj" \
    --source https://api.nuget.org/v3/index.json \
    --verbosity normal

# Copy the rest of the source code
COPY . .

# Build the application with retry logic
WORKDIR "/src/src/UmiHealth.API"
RUN dotnet build "UmiHealth.API.csproj" -c Release -o /app/build --no-restore || \
    dotnet build "UmiHealth.API.csproj" -c Release -o /app/build --no-restore

# Publish the application
FROM build AS publish
RUN dotnet publish "UmiHealth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore || \
    dotnet publish "UmiHealth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Build the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy published files
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R nextjs:nodejs /app

# Switch to non-root user
USER nextjs

# Expose ports
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80

# Start the application
ENTRYPOINT ["dotnet", "UmiHealth.API.dll"]
