# Use the official .NET 8.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["src/UmiHealth.Identity/UmiHealth.Identity.csproj", "src/UmiHealth.Identity/"]
COPY ["src/UmiHealth.Application/UmiHealth.Application.csproj", "src/UmiHealth.Application/"]
COPY ["src/UmiHealth.Core/UmiHealth.Core.csproj", "src/UmiHealth.Core/"]
COPY ["src/UmiHealth.Domain/UmiHealth.Domain.csproj", "src/UmiHealth.Domain/"]
COPY ["src/UmiHealth.Infrastructure/UmiHealth.Infrastructure.csproj", "src/UmiHealth.Infrastructure/"]
COPY ["src/UmiHealth.Persistence/UmiHealth.Persistence.csproj", "src/UmiHealth.Persistence/"]
COPY ["src/UmiHealth.Shared/UmiHealth.Shared.csproj", "src/UmiHealth.Shared/"]

RUN dotnet restore "src/UmiHealth.Identity/UmiHealth.Identity.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/UmiHealth.Identity"
RUN dotnet build "UmiHealth.Identity.csproj" -c Release -o /app/build

# Build runtime image
FROM build AS publish
RUN dotnet publish "UmiHealth.Identity.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UmiHealth.Identity.dll"]
