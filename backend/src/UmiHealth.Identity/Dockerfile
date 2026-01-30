# Use the official .NET 8.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers (relative to build context root)
COPY ["backend/src/UmiHealth.Identity/UmiHealth.Identity.csproj", "backend/src/UmiHealth.Identity/"]
COPY ["backend/src/UmiHealth.Application/UmiHealth.Application.csproj", "backend/src/UmiHealth.Application/"]
COPY ["backend/src/UmiHealth.Core/UmiHealth.Core.csproj", "backend/src/UmiHealth.Core/"]
COPY ["backend/src/UmiHealth.Domain/UmiHealth.Domain.csproj", "backend/src/UmiHealth.Domain/"]
COPY ["backend/src/UmiHealth.Infrastructure/UmiHealth.Infrastructure.csproj", "backend/src/UmiHealth.Infrastructure/"]
COPY ["backend/src/UmiHealth.Persistence/UmiHealth.Persistence.csproj", "backend/src/UmiHealth.Persistence/"]
COPY ["backend/src/UmiHealth.Shared/UmiHealth.Shared.csproj", "backend/src/UmiHealth.Shared/"]

RUN dotnet restore "backend/src/UmiHealth.Identity/UmiHealth.Identity.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/backend/src/UmiHealth.Identity"
RUN dotnet build "UmiHealth.Identity.csproj" -c Release -o /app/build /p:TreatWarningsAsErrors=false

# Build runtime image
FROM build AS publish
RUN dotnet publish "UmiHealth.Identity.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:TreatWarningsAsErrors=false

# Build final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UmiHealth.Identity.dll"]
