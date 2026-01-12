# Use the official .NET 8.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the API project file and restore
COPY ["src/UmiHealth.API/UmiHealth.API.csproj", "src/UmiHealth.API/"]
RUN dotnet restore "src/UmiHealth.API/UmiHealth.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/UmiHealth.API"
RUN dotnet build "UmiHealth.API.csproj" -c Release -o /app/build --no-restore

# Build runtime image
FROM build AS publish
RUN dotnet publish "UmiHealth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Build final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UmiHealth.API.dll"]
