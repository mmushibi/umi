# Use the official .NET 8.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the API project file and restore
COPY ["UmiHealth.MinimalApi/UmiHealth.MinimalApi.csproj", "UmiHealth.MinimalApi/"]
RUN dotnet restore "UmiHealth.MinimalApi/UmiHealth.MinimalApi.csproj"

# Copy everything else and build
COPY UmiHealth.MinimalApi/. ./UmiHealth.MinimalApi/
WORKDIR "/src/UmiHealth.MinimalApi"
RUN dotnet build "UmiHealth.MinimalApi.csproj" -c Release -o /app/build --no-restore /p:TreatWarningsAsErrors=false

# Build runtime image
FROM build AS publish
RUN dotnet publish "UmiHealth.MinimalApi.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore /p:TreatWarningsAsErrors=false

# Build final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UmiHealth.MinimalApi.dll"]
