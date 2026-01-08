# Multi-stage build for UmiHealth API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["UmiHealth.Api/UmiHealth.API.csproj", "UmiHealth.Api/"]
COPY ["UmiHealth.Core/UmiHealth.Core.csproj", "UmiHealth.Core/"]
COPY ["UmiHealth.Infrastructure/UmiHealth.Infrastructure.csproj", "UmiHealth.Infrastructure/"]
COPY ["UmiHealth.Application/UmiHealth.Application.csproj", "UmiHealth.Application/"]
COPY ["UmiHealth.Domain/UmiHealth.Domain.csproj", "UmiHealth.Domain/"]
COPY ["UmiHealth.Shared/UmiHealth.Shared.csproj", "UmiHealth.Shared/"]
COPY ["UmiHealth.Identity/UmiHealth.Identity.csproj", "UmiHealth.Identity/"]

RUN dotnet restore "UmiHealth.Api/UmiHealth.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/UmiHealth.Api"
RUN dotnet build "UmiHealth.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "UmiHealth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN if ! getent group app >/dev/null 2>&1; then addgroup --system app; fi && \
    adduser --system --disabled-password --gecos "" app || \
    usermod -a -G app app 2>/dev/null || true

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
ENTRYPOINT ["dotnet", "UmiHealth.API.dll"]
