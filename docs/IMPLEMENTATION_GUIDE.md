# Umi Health Implementation Guide

## Quick Start Implementation

This guide provides step-by-step instructions for implementing the complete Umi Health Pharmacy Management System with microservices architecture and multi-tenant support.

## Phase 1: Environment Setup

### 1.1 Prerequisites

```bash
# Required software versions
- Docker 20.10+
- Docker Compose 2.0+
- .NET 8.0 SDK (for local development)
- PostgreSQL 15+ (for local development)
- Redis 7+ (for local development)
```

### 1.2 Project Structure

```bash
umi/
├── backend/
│   ├── src/
│   │   ├── UmiHealth.API/              # Main API service (.NET 8.0)
│   │   ├── UmiHealth.Identity/          # Identity service
│   │   ├── UmiHealth.ApiGateway/        # API Gateway
│   │   ├── UmiHealth.BackgroundJobs/    # Background processing
│   │   └── UmiHealth.MinimalApi/        # Minimal API (.NET 10.0)
│   ├── Dockerfile
│   └── docker-compose.yml
├── portals/
│   ├── admin/                           # Admin portal
│   ├── pharmacist/                      # Pharmacist portal
│   ├── cashier/                         # Cashier portal
│   └── operations/                      # Operations portal
├── docs/                                # Documentation
├── database/
│   ├── migrations/
│   └── schema.sql
└── monitoring/
    ├── prometheus/
    └── grafana/
```

## Phase 2: Database Setup

### 2.1 PostgreSQL Configuration

```sql
-- Create shared schema
CREATE SCHEMA IF NOT EXISTS shared;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create tenant management tables
-- (Execute the SQL from MULTI_TENANT_ARCHITECTURE.md section 2.1)
```

### 2.2 Multi-Tenant Schema Setup

```sql
-- Function to create new tenant schema
CREATE OR REPLACE FUNCTION create_tenant_schema(tenant_id UUID, schema_name TEXT)
RETURNS VOID AS $$
DECLARE
    table_name TEXT;
    sql_statement TEXT;
BEGIN
    -- Create schema
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);
    
    -- Create tenant-specific tables
    FOR table_name IN 
        SELECT table_name FROM information_schema.tables 
        WHERE table_schema = 'tenant_template'
    LOOP
        sql_statement := format('CREATE TABLE %I.%I (LIKE tenant_template.%I INCLUDING ALL)', 
                               schema_name, table_name, table_name);
        EXECUTE sql_statement;
    END LOOP;
    
    -- Set up row-level security
    EXECUTE format('ALTER SCHEMA %I OWNER TO %I', schema_name, tenant_id);
END;
$$ LANGUAGE plpgsql;
```

## Phase 3: Microservices Implementation

### 3.1 API Gateway Setup (.NET 8.0)

```csharp
// UmiHealth.ApiGateway/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        // JWT configuration
    });

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

app.MapReverseProxy();

app.Run();
```

### 3.2 Identity Service Configuration

```csharp
// UmiHealth.Identity/Program.cs
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        // Configuration from appsettings
    };
});
```

### 3.3 Main API Service (.NET 8.0)

```csharp
// UmiHealth.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<UmiHealthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Multi-tenant support
builder.Services.AddScoped<TenantResolver>();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UmiHealthDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));

var app = builder.Build();

app.UseMiddleware<TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

## Phase 4: Docker Configuration

### 4.1 Main Dockerfile

```dockerfile
# backend/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/UmiHealth.API/UmiHealth.API.csproj", "src/UmiHealth.API/"]
RUN dotnet restore "src/UmiHealth.API/UmiHealth.API.csproj"
COPY . .
WORKDIR "/src/src/UmiHealth.API"
RUN dotnet build "UmiHealth.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UmiHealth.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UmiHealth.API.dll"]
```

### 4.2 Docker Compose Configuration

```yaml
# docker-compose.yml
version: '3.8'

services:
  api-gateway:
    image: umihealth/api-gateway:latest
    ports:
      - "80:8080"
    depends_on:
      - umihealth-api
      - identity-service
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

  umihealth-api:
    image: umihealth/api:latest
    ports:
      - "5000:8080"
    depends_on:
      - postgres
      - redis
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=UmiHealth;Username=umihealth;Password=${POSTGRES_PASSWORD}

  identity-service:
    image: umihealth/identity:latest
    ports:
      - "5001:8080"
    depends_on:
      - postgres
      - redis

  postgres:
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=UmiHealth
      - POSTGRES_USER=umihealth
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
```

## Phase 5: Frontend Portals

### 5.1 Admin Portal Structure

```html
<!-- portals/admin/index.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Umi Health Admin Portal</title>
    <link rel="stylesheet" href="../shared/css/shared-styles.css">
    <link rel="stylesheet" href="css/admin-styles.css">
</head>
<body>
    <div id="app">
        <header class="admin-header">
            <h1>Umi Health Admin Portal</h1>
            <nav class="admin-nav">
                <a href="#dashboard">Dashboard</a>
                <a href="#users">User Management</a>
                <a href="#branches">Branch Management</a>
                <a href="#settings">System Settings</a>
            </nav>
        </header>
        
        <main class="admin-main">
            <section id="dashboard">
                <!-- Dashboard content -->
            </section>
        </main>
    </div>

    <script src="../shared/js/auth-manager.js"></script>
    <script src="js/admin-dashboard.js"></script>
</body>
</html>
```

### 5.2 Authentication Integration

```javascript
// shared/js/auth-manager.js
class AuthManager {
    constructor() {
        this.token = localStorage.getItem('jwt_token');
        this.user = JSON.parse(localStorage.getItem('current_user') || '{}');
    }

    async login(email, password) {
        try {
            const response = await fetch('/api/v1/auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email, password })
            });

            if (response.ok) {
                const data = await response.json();
                this.token = data.token;
                this.user = data.user;
                
                localStorage.setItem('jwt_token', this.token);
                localStorage.setItem('current_user', JSON.stringify(this.user));
                
                return true;
            }
        } catch (error) {
            console.error('Login failed:', error);
        }
        return false;
    }

    async logout() {
        localStorage.removeItem('jwt_token');
        localStorage.removeItem('current_user');
        window.location.href = '/public/admin-signin.html';
    }

    isAuthenticated() {
        return this.token && this.user.id;
    }

    getAuthHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`,
            'Content-Type': 'application/json'
        };
    }
}

window.authManager = new AuthManager();
```

## Phase 6: Monitoring and Logging

### 6.1 Prometheus Metrics

```csharp
// UmiHealth.API/Metrics/ApiMetrics.cs
public class ApiMetrics
{
    private static readonly Counter RequestCounter = Metrics
        .CreateCounter("umi_api_requests_total", "Total API requests", "method", "endpoint", "status");
    
    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("umi_api_request_duration_seconds", "API request duration");

    public static void RecordRequest(string method, string endpoint, string status, TimeSpan duration)
    {
        RequestCounter.WithLabels(method, endpoint, status).Inc();
        RequestDuration.Observe(duration.TotalSeconds);
    }
}
```

### 6.2 Serilog Configuration

```csharp
// UmiHealth.API/Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "UmiHealth.API")
    .WriteTo.Console()
    .WriteTo.File("logs/umihealth-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

## Phase 7: Deployment

### 7.1 Environment Configuration

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=UmiHealth;Username=umihealth;Password=${POSTGRES_PASSWORD}"
  },
  "Redis": {
    "ConnectionString": "redis:6379,password=${REDIS_PASSWORD}"
  },
  "JwtSettings": {
    "Secret": "${JWT_SECRET}",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com"
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

### 7.2 Production Deployment

```bash
# Deploy to production
docker-compose -f docker-compose.yml -f docker-compose.override.yml --profile production up -d

# Verify deployment
docker-compose ps
curl http://localhost/health
```

## Phase 8: Testing

### 8.1 Integration Tests

```csharp
// UmiHealth.MinimalApi.Tests/MultiTenantIntegrationTests.cs
[TestClass]
public class MultiTenantIntegrationTests
{
    [TestMethod]
    public async Task CreateTenant_ShouldCreateTenantSchema()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tenantRequest = new CreateTenantRequest {
            Name = "Test Pharmacy",
            Subdomain = "test-pharmacy"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tenants", tenantRequest);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        Assert.IsNotNull(tenant);
        Assert.AreEqual(tenantRequest.Name, tenant.Name);
    }
}
```

### 8.2 API Testing

```bash
# Run Postman tests
./api-testing/scripts/run-postman-tests.ps1

# Generate API documentation
./api-testing/scripts/generate-api-docs.ps1
```

## Phase 9: Security Configuration

### 9.1 JWT Configuration

```csharp
// UmiHealth.Identity/Services/JwtService.cs
public class JwtService
{
    public string GenerateToken(ApplicationUser user, string tenantId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role),
            new Claim("tenant_id", tenantId),
            new Claim("branch_id", user.BranchId?.ToString() ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_jwtOptions.Expiration),
            signingCredentials: new SigningCredentials(_jwtOptions.SigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 9.2 Rate Limiting

```csharp
// UmiHealth.ApiGateway/Middleware/RateLimitMiddleware.cs
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var key = $"rate_limit_{clientId}";
        
        var counter = await _cache.GetOrCreateAsync(key, entry => {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return Task.FromResult(new RateCounter { Count = 0, ResetTime = DateTime.UtcNow.AddMinutes(1) });
        });

        if (counter.Count >= _options.LimitPerMinute)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        counter.Count++;
        await _next(context);
    }
}
```

## Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Check PostgreSQL service status
   - Verify connection string format
   - Ensure database exists

2. **Authentication Failures**
   - Verify JWT configuration
   - Check token expiration
   - Validate user permissions

3. **Multi-Tenant Issues**
   - Verify tenant resolution
   - Check schema permissions
   - Validate row-level security

### Debug Commands

```bash
# Check container logs
docker-compose logs umihealth-api

# Test database connection
docker-compose exec postgres psql -U umihealth -d UmiHealth

# Check Redis connection
docker-compose exec redis redis-cli ping

# Monitor system resources
docker stats
```

---

This implementation guide provides a comprehensive roadmap for deploying the complete Umi Health system with all modern features and best practices.
import { Request, Response, NextFunction } from 'express';
import { TenantService } from '../services/tenant-service';

export class TenantResolver {
  static async resolve(req: Request, res: Response, next: NextFunction) {
    try {
      // Method 1: Subdomain extraction
      const hostname = req.hostname;
      const subdomain = hostname.split('.')[0];
      
      // Method 2: Header fallback
      const tenantHeader = req.headers['x-tenant-id'] as string;
      
      // Resolve tenant
      const tenant = await TenantService.findByIdentifier({
        subdomain: subdomain !== 'www' ? subdomain : null,
        id: tenantHeader
      });
      
      if (!tenant) {
        return res.status(404).json({ error: 'Tenant not found' });
      }
      
      req.tenant = tenant;
      req.tenantId = tenant.id;
      
      next();
    } catch (error) {
      res.status(500).json({ error: 'Tenant resolution failed' });
    }
  }
}
```

### 2.3 Authentication Service

```typescript
// user-service/src/services/auth-service.ts
import jwt from 'jsonwebtoken';
import bcrypt from 'bcrypt';
import { User } from '../models/user';
import { Tenant } from '../models/tenant';

export class AuthService {
  async login(email: string, password: string, tenantId: string): Promise<AuthResponse> {
    // Find user in shared schema
    const user = await User.findOne({ 
      where: { 
        email, 
        tenantId, 
        isActive: true 
      },
      include: [Tenant]
    });
    
    if (!user || !await bcrypt.compare(password, user.passwordHash)) {
      throw new Error('Invalid credentials');
    }
    
    // Update last login
    user.lastLogin = new Date();
    await user.save();
    
    // Generate tokens
    const accessToken = this.generateAccessToken(user);
    const refreshToken = this.generateRefreshToken(user);
    
    return {
      user: this.sanitizeUser(user),
      accessToken,
      refreshToken,
      expiresIn: 3600
    };
  }
  
  private generateAccessToken(user: User): string {
    return jwt.sign(
      {
        userId: user.id,
        tenantId: user.tenantId,
        branchId: user.branchId,
        role: user.role,
        permissions: user.permissions
      },
      process.env.JWT_SECRET!,
      { expiresIn: '1h' }
    );
  }
  
  private generateRefreshToken(user: User): string {
    return jwt.sign(
      { userId: user.id, tenantId: user.tenantId },
      process.env.JWT_REFRESH_SECRET!,
      { expiresIn: '7d' }
    );
  }
}
```

## Phase 3: Service Implementation

### 3.1 Base Repository Pattern

```typescript
// shared/src/repositories/base-repository.ts
import { DatabaseConnection } from '../database/connection';
import { UserContext } from '../types/user-context';

export abstract class BaseRepository<T> {
  protected db = DatabaseConnection.getInstance();
  protected abstract tableName: string;
  protected abstract tenantSpecific: boolean;
  
  async findById(id: string, context: UserContext): Promise<T | null> {
    const query = this.buildBaseQuery(context);
    query.where.id = id;
    
    const result = await this.db.query(query);
    return result.rows[0] || null;
  }
  
  async findMany(criteria: any, context: UserContext): Promise<T[]> {
    const query = this.buildBaseQuery(context);
    query.where = { ...query.where, ...criteria };
    
    const result = await this.db.query(query);
    return result.rows;
  }
  
  async create(data: Partial<T>, context: UserContext): Promise<T> {
    const query = {
      text: `
        INSERT INTO ${this.getTableName(context)} 
        (${Object.keys(data).join(', ')}, tenant_id${this.tenantSpecific ? ', branch_id' : ''})
        VALUES (${Object.keys(data).map((_, i) => `$${i + 1}`).join(', ')}, $${Object.keys(data).length + 1}${this.tenantSpecific ? `, $${Object.keys(data).length + 2}` : ''})
        RETURNING *
      `,
      values: [
        ...Object.values(data),
        context.tenantId,
        ...(this.tenantSpecific ? [context.branchId] : [])
      ]
    };
    
    const result = await this.db.query(query);
    return result.rows[0];
  }
  
  protected buildBaseQuery(context: UserContext): any {
    const query: any = {
      text: `SELECT * FROM ${this.getTableName(context)}`,
      where: {}
    };
    
    // Always filter by tenant
    if (this.tenantSpecific) {
      query.where.tenantId = context.tenantId;
      
      // Filter by branch if not admin
      if (!context.canViewAllBranches && context.branchIds.length > 0) {
        query.where.branchId = { in: context.branchIds };
      }
    }
    
    return query;
  }
  
  protected getTableName(context: UserContext): string {
    if (this.tenantSpecific) {
      return `tenant_${context.tenantSchema}.${this.tableName}`;
    }
    return `shared.${this.tableName}`;
  }
}
```

### 3.2 Pharmacy Service Example

```typescript
// pharmacy-service/src/repositories/patient-repository.ts
import { BaseRepository } from '../../../shared/src/repositories/base-repository';
import { Patient } from '../models/patient';
import { UserContext } from '../../../shared/src/types/user-context';

export class PatientRepository extends BaseRepository<Patient> {
  protected tableName = 'tenant_patients';
  protected tenantSpecific = true;
  
  async findByPatientNumber(patientNumber: string, context: UserContext): Promise<Patient | null> {
    const query = this.buildBaseQuery(context);
    query.where.patientNumber = patientNumber;
    
    const result = await this.db.query(query);
    return result.rows[0] || null;
  }
  
  async searchPatients(searchTerm: string, context: UserContext): Promise<Patient[]> {
    const query = {
      text: `
        SELECT * FROM ${this.getTableName(context)}
        WHERE tenant_id = $1
          AND branch_id = ANY($2)
          AND (
            first_name ILIKE $3 OR
            last_name ILIKE $3 OR
            patient_number ILIKE $3 OR
            phone ILIKE $3
          )
          AND deleted_at IS NULL
        ORDER BY last_name, first_name
        LIMIT 50
      `,
      values: [
        context.tenantId,
        context.canViewAllBranches ? 
          await this.getAllBranchIds(context.tenantId) : 
          context.branchIds,
        `%${searchTerm}%`
      ]
    };
    
    const result = await this.db.query(query);
    return result.rows;
  }
}
```

### 3.3 Service Controller

```typescript
// pharmacy-service/src/controllers/patient-controller.ts
import { Request, Response } from 'express';
import { PatientService } from '../services/patient-service';
import { validatePatient } from '../validators/patient-validator';

export class PatientController {
  constructor(private patientService: PatientService) {}
  
  async createPatient(req: Request, res: Response): Promise<void> {
    try {
      // Validate input
      const validation = validatePatient(req.body);
      if (!validation.isValid) {
        res.status(400).json({ errors: validation.errors });
        return;
      }
      
      // Create patient
      const patient = await this.patientService.create({
        ...req.body,
        branchId: req.branchId
      }, req.userContext);
      
      res.status(201).json(patient);
    } catch (error) {
      res.status(500).json({ error: error.message });
    }
  }
  
  async getPatients(req: Request, res: Response): Promise<void> {
    try {
      const { search, page = 1, limit = 20 } = req.query;
      
      let patients;
      if (search) {
        patients = await this.patientService.search(
          search as string, 
          req.userContext
        );
      } else {
        patients = await this.patientService.findMany(
          {}, 
          req.userContext,
          { page: Number(page), limit: Number(limit) }
        );
      }
      
      res.json(patients);
    } catch (error) {
      res.status(500).json({ error: error.message });
    }
  }
}
```

## Phase 4: Frontend Integration

### 4.1 Multi-Tenant Client Configuration

```typescript
// frontend/src/config/tenant-config.ts
export interface TenantConfig {
  id: string;
  name: string;
  subdomain: string;
  theme: {
    primaryColor: string;
    accentColor: string;
    logo: string;
  };
  features: {
    multiBranch: boolean;
    advancedReporting: boolean;
    inventoryManagement: boolean;
    prescriptionManagement: boolean;
  };
  apiEndpoints: {
    gateway: string;
    services: Record<string, string>;
  };
}

export class TenantConfigLoader {
  static async load(): Promise<TenantConfig> {
    // Extract tenant from subdomain
    const subdomain = window.location.hostname.split('.')[0];
    
    // Load tenant configuration
    const response = await fetch(`/api/config/tenant/${subdomain}`);
    const config = await response.json();
    
    // Apply theme
    this.applyTheme(config.theme);
    
    return config;
  }
  
  private static applyTheme(theme: TenantConfig['theme']): void {
    document.documentElement.style.setProperty('--color-primary', theme.primaryColor);
    document.documentElement.style.setProperty('--color-accent', theme.accentColor);
    
    // Update logo
    const logoElement = document.querySelector('.logo img') as HTMLImageElement;
    if (logoElement) {
      logoElement.src = theme.logo;
    }
  }
}
```

### 4.2 API Client with Tenant Context

```typescript
// frontend/src/services/api-client.ts
import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';
import { UserContext } from '../types/user-context';

export class ApiClient {
  private client: AxiosInstance;
  private userContext: UserContext | null = null;
  
  constructor(baseURL: string) {
    this.client = axios.create({
      baseURL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    this.setupInterceptors();
  }
  
  private setupInterceptors(): void {
    // Request interceptor
    this.client.interceptors.request.use((config) => {
      // Add tenant context
      if (this.userContext) {
        config.headers['X-Tenant-ID'] = this.userContext.tenantId;
        config.headers['X-Branch-ID'] = this.userContext.branchId;
      }
      
      // Add auth token
      const token = localStorage.getItem('accessToken');
      if (token) {
        config.headers['Authorization'] = `Bearer ${token}`;
      }
      
      return config;
    });
    
    // Response interceptor
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        if (error.response?.status === 401) {
          // Handle token refresh
          await this.refreshToken();
          return this.client.request(error.config);
        }
        return Promise.reject(error);
      }
    );
  }
  
  setUserContext(context: UserContext): void {
    this.userContext = context;
  }
  
  async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.get(url, config);
    return response.data;
  }
  
  async post<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.post(url, data, config);
    return response.data;
  }
  
  async put<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.put(url, data, config);
    return response.data;
  }
  
  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.delete(url, config);
    return response.data;
  }
  
  private async refreshToken(): Promise<void> {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }
    
    const response = await fetch('/api/v1/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    });
    
    if (response.ok) {
      const { accessToken } = await response.json();
      localStorage.setItem('accessToken', accessToken);
    } else {
      // Redirect to login
      window.location.href = '/login';
    }
  }
}
```

## Phase 5: Deployment Setup

### 5.1 Docker Configuration

```dockerfile
# Dockerfile for API Gateway
FROM node:18-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci --only=production

# Copy source code
COPY . .

# Build application
RUN npm run build

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start application
CMD ["npm", "start"]
```

### 5.2 Docker Compose for Development

```yaml
# docker-compose.dev.yml
version: '3.8'

services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: umi_health_dev
      POSTGRES_USER: dev_user
      POSTGRES_PASSWORD: dev_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_dev_data:/var/lib/postgresql/data
      - ./database/init.sql:/docker-entrypoint-initdb.d/init.sql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_dev_data:/data

  api-gateway:
    build: ./gateway
    ports:
      - "8080:8080"
    environment:
      - NODE_ENV=development
      - DATABASE_URL=postgresql://dev_user:dev_password@postgres:5432/umi_health_dev
      - REDIS_URL=redis://redis:6379
      - JWT_SECRET=dev_jwt_secret
    depends_on:
      - postgres
      - redis
    volumes:
      - ./gateway:/app
      - /app/node_modules

  tenant-service:
    build: ./services/tenant-service
    ports:
      - "3001:3001"
    environment:
      - NODE_ENV=development
      - DATABASE_URL=postgresql://dev_user:dev_password@postgres:5432/umi_health_dev
    depends_on:
      - postgres
    volumes:
      - ./services/tenant-service:/app
      - /app/node_modules

  pharmacy-service:
    build: ./services/pharmacy-service
    ports:
      - "3003:3003"
    environment:
      - NODE_ENV=development
      - DATABASE_URL=postgresql://dev_user:dev_password@postgres:5432/umi_health_dev
    depends_on:
      - postgres
    volumes:
      - ./services/pharmacy-service:/app
      - /app/node_modules

volumes:
  postgres_dev_data:
  redis_dev_data:
```

### 5.3 Kubernetes Deployment

```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: umi-health

---
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: umi-health-config
  namespace: umi-health
data:
  NODE_ENV: "production"
  REDIS_URL: "redis://redis-service:6379"

---
# k8s/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: umi-health-secrets
  namespace: umi-health
type: Opaque
data:
  DATABASE_URL: <base64-encoded-db-url>
  JWT_SECRET: <base64-encoded-jwt-secret>

---
# k8s/api-gateway-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
  namespace: umi-health
spec:
  replicas: 3
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
    spec:
      containers:
      - name: api-gateway
        image: umi-health/api-gateway:latest
        ports:
        - containerPort: 8080
        envFrom:
        - configMapRef:
            name: umi-health-config
        - secretRef:
            name: umi-health-secrets
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5

---
# k8s/api-gateway-service.yaml
apiVersion: v1
kind: Service
metadata:
  name: api-gateway-service
  namespace: umi-health
spec:
  selector:
    app: api-gateway
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

## Phase 6: Testing Strategy

### 6.1 Unit Testing

```typescript
// tests/unit/patient-service.test.ts
import { PatientService } from '../../src/services/patient-service';
import { PatientRepository } from '../../src/repositories/patient-repository';
import { UserContext } from '../../src/types/user-context';

jest.mock('../../src/repositories/patient-repository');

describe('PatientService', () => {
  let patientService: PatientService;
  let mockRepository: jest.Mocked<PatientRepository>;
  let mockContext: UserContext;

  beforeEach(() => {
    mockRepository = new PatientRepository() as jest.Mocked<PatientRepository>;
    patientService = new PatientService(mockRepository);
    
    mockContext = {
      userId: 'user-123',
      tenantId: 'tenant-456',
      branchId: 'branch-789',
      tenantSchema: 'demo',
      role: 'pharmacist',
      branchIds: ['branch-789'],
      canViewAllBranches: false
    };
  });

  describe('create', () => {
    it('should create a patient successfully', async () => {
      const patientData = {
        firstName: 'John',
        lastName: 'Doe',
        dateOfBirth: '1990-01-01',
        phone: '+260977123456'
      };

      const expectedPatient = {
        id: 'patient-123',
        ...patientData,
        tenantId: mockContext.tenantId,
        branchId: mockContext.branchId
      };

      mockRepository.create.mockResolvedValue(expectedPatient);

      const result = await patientService.create(patientData, mockContext);

      expect(mockRepository.create).toHaveBeenCalledWith(patientData, mockContext);
      expect(result).toEqual(expectedPatient);
    });
  });
});
```

### 6.2 Integration Testing

```typescript
// tests/integration/api-gateway.test.ts
import request from 'supertest';
import { app } from '../src/app';
import { setupTestDatabase, cleanupTestDatabase } from './helpers/database';

describe('API Gateway Integration', () => {
  beforeAll(async () => {
    await setupTestDatabase();
  });

  afterAll(async () => {
    await cleanupTestDatabase();
  });

  describe('Tenant Resolution', () => {
    it('should resolve tenant from subdomain', async () => {
      const response = await request(app)
        .get('/api/v1/tenants/current')
        .set('Host', 'demo.umihealth.com')
        .expect(200);

      expect(response.body.tenant).toBeDefined();
      expect(response.body.tenant.subdomain).toBe('demo');
    });

    it('should resolve tenant from header', async () => {
      const response = await request(app)
        .get('/api/v1/tenants/current')
        .set('X-Tenant-ID', 'tenant-123')
        .expect(200);

      expect(response.body.tenant).toBeDefined();
      expect(response.body.tenant.id).toBe('tenant-123');
    });
  });
});
```

### 6.3 Load Testing

```typescript
// tests/load/api-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

export let options = {
  stages: [
    { duration: '2m', target: 100 }, // ramp up to 100 users
    { duration: '5m', target: 100 }, // stay at 100 users
    { duration: '2m', target: 200 }, // ramp up to 200 users
    { duration: '5m', target: 200 }, // stay at 200 users
    { duration: '2m', target: 0 },   // ramp down to 0 users
  ],
};

export default function () {
  const response = http.post('http://api-gateway:8080/api/v1/auth/login', 
    JSON.stringify({
      email: 'test@example.com',
      password: 'password123'
    }),
    {
      headers: {
        'Content-Type': 'application/json',
        'X-Tenant-ID': 'demo'
      }
    }
  );

  const success = check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });

  errorRate.add(!success);
  sleep(1);
}
```

## Phase 7: Monitoring Setup

### 7.1 Prometheus Metrics

```typescript
// monitoring/metrics.ts
import { register, Counter, Histogram, Gauge } from 'prom-client';

export const metrics = {
  // HTTP metrics
  httpRequestsTotal: new Counter({
    name: 'http_requests_total',
    help: 'Total number of HTTP requests',
    labelNames: ['method', 'route', 'status_code', 'tenant_id']
  }),

  httpRequestDuration: new Histogram({
    name: 'http_request_duration_seconds',
    help: 'Duration of HTTP requests in seconds',
    labelNames: ['method', 'route', 'tenant_id'],
    buckets: [0.1, 0.3, 0.5, 0.7, 1, 3, 5, 7, 10]
  }),

  // Business metrics
  activeTenants: new Gauge({
    name: 'active_tenants_total',
    help: 'Number of active tenants'
  }),

  activeUsers: new Gauge({
    name: 'active_users_total',
    help: 'Number of active users',
    labelNames: ['tenant_id', 'role']
  }),

  prescriptionsProcessed: new Counter({
    name: 'prescriptions_processed_total',
    help: 'Total number of prescriptions processed',
    labelNames: ['tenant_id', 'branch_id', 'status']
  }),

  salesCompleted: new Counter({
    name: 'sales_completed_total',
    help: 'Total number of sales completed',
    labelNames: ['tenant_id', 'branch_id', 'payment_method']
  })
};

// Metrics middleware
export const metricsMiddleware = (req: Request, res: Response, next: NextFunction) => {
  const start = Date.now();
  
  res.on('finish', () => {
    const duration = (Date.now() - start) / 1000;
    
    metrics.httpRequestsTotal
      .labels(req.method, req.route?.path || req.path, res.statusCode.toString(), req.tenantId)
      .inc();
    
    metrics.httpRequestDuration
      .labels(req.method, req.route?.path || req.path, req.tenantId)
      .observe(duration);
  });
  
  next();
};
```

### 7.2 Health Checks

```typescript
// health/health-check.ts
import { DatabaseConnection } from '../database/connection';
import { RedisClient } from '../cache/redis-client';

export class HealthCheck {
  static async check(): Promise<HealthStatus> {
    const checks = await Promise.allSettled([
      this.checkDatabase(),
      this.checkRedis(),
      this.checkExternalServices()
    ]);

    const status = checks.every(check => check.status === 'fulfilled') ? 'healthy' : 'unhealthy';
    
    return {
      status,
      timestamp: new Date().toISOString(),
      checks: {
        database: checks[0].status === 'fulfilled' ? checks[0].value : { status: 'error' },
        redis: checks[1].status === 'fulfilled' ? checks[1].value : { status: 'error' },
        external: checks[2].status === 'fulfilled' ? checks[2].value : { status: 'error' }
      }
    };
  }

  private static async checkDatabase(): Promise<DatabaseHealth> {
    try {
      const start = Date.now();
      await DatabaseConnection.getInstance().query('SELECT 1');
      const responseTime = Date.now() - start;

      return {
        status: 'healthy',
        responseTime: `${responseTime}ms`,
        connections: await this.getConnectionCount()
      };
    } catch (error) {
      return {
        status: 'error',
        error: error.message
      };
    }
  }

  private static async checkRedis(): Promise<RedisHealth> {
    try {
      const start = Date.now();
      await RedisClient.getInstance().ping();
      const responseTime = Date.now() - start;

      return {
        status: 'healthy',
        responseTime: `${responseTime}ms`,
        memory: await RedisClient.getInstance().info('memory')
      };
    } catch (error) {
      return {
        status: 'error',
        error: error.message
      };
    }
  }
}
```

This implementation guide provides a comprehensive roadmap for building the multi-tenant Umi Health system with full branch support, security, and scalability.
