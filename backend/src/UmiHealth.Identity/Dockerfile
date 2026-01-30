# Use the official .NET 8.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers (relative to build context backend/src)
COPY ["UmiHealth.Identity/UmiHealth.Identity.csproj", "UmiHealth.Identity/"]
COPY ["UmiHealth.Application/UmiHealth.Application.csproj", "UmiHealth.Application/"]
COPY ["UmiHealth.Core/UmiHealth.Core.csproj", "UmiHealth.Core/"]
COPY ["UmiHealth.Domain/UmiHealth.Domain.csproj", "UmiHealth.Domain/"]
COPY ["UmiHealth.Infrastructure/UmiHealth.Infrastructure.csproj", "UmiHealth.Infrastructure/"]
COPY ["UmiHealth.Persistence/UmiHealth.Persistence.csproj", "UmiHealth.Persistence/"]
COPY ["UmiHealth.Shared/UmiHealth.Shared.csproj", "UmiHealth.Shared/"]

RUN dotnet restore "UmiHealth.Identity/UmiHealth.Identity.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/UmiHealth.Identity"
RUN dotnet build "UmiHealth.Identity.csproj" -c Release -o /app/build /p:TreatWarningsAsErrors=false

# Build runtime image
FROM build AS publish
RUN dotnet publish "UmiHealth.Identity.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:TreatWarningsAsErrors=false

# Build final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UmiHealth.Identity.dll"]
