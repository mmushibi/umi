# UmiHealth Infrastructure Layer

This project contains the core infrastructure components for the UmiHealth pharmacy management system, implementing a multi-tenant architecture with PostgreSQL, Redis caching, and file storage capabilities.

## Architecture Overview

### Data Layer Components

1. **PostgreSQL Multi-Tenant Database**
   - Shared database for tenant management and system-wide data
   - Individual tenant databases for isolated data storage
   - Entity Framework Core with schema-based separation

2. **Redis Caching Layer**
   - Distributed caching for performance optimization
   - Tenant-aware cache keys for data isolation
   - Configurable expiration policies

3. **File Storage System**
   - Local file storage with cloud-ready abstraction
   - Tenant-specific containers for data isolation
   - Configurable file size and extension restrictions

4. **Repository Pattern**
   - Generic repository interface for common CRUD operations
   - Tenant-aware repositories for multi-tenant data access
   - Entity Framework integration

## Key Components

### Database Contexts

- **SharedDbContext**: Manages shared schema tables (tenants, users, branches, subscriptions)
- **TenantDbContext**: Manages tenant-specific tables (patients, products, inventory, prescriptions, sales)
- **TenantDbContextFactory**: Creates tenant-specific database contexts

### Caching

- **IRedisCacheService**: Interface for Redis operations
- **RedisCacheService**: Implementation with error handling and serialization
- **CacheKeys**: Centralized cache key management with tenant isolation

### File Storage

- **IFileStorageService**: Interface for file operations
- **LocalFileStorageService**: Local file system implementation
- **StorageContainers**: Centralized container naming conventions

### Repositories

- **IRepository<T>**`: Generic repository interface
- **Repository<T>**`: Base repository implementation
- **ITenantRepository<T>**`: Tenant-aware repository interface
- **TenantRepository<T>**`: Tenant repository base class
- **Specific Repositories**: Tenant, User, Branch repositories with business logic

### Multi-Tenant Support

- **TenantMiddleware**: ASP.NET Core middleware for tenant identification
- **ITenantProvider**: Service for accessing current tenant context
- **TenantProvider**: Implementation with tenant validation

## Configuration

### Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=umihealth_shared;Username=postgres;Password=password;Port=5432",
    "Redis": "localhost:6379"
  }
}
```

### File Storage Settings

```json
{
  "FileStorage": {
    "BasePath": "./storage",
    "BaseUrl": "/storage",
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt", ".csv", ".xlsx", ".xls" ]
  }
}
```

### Usage

#### Service Registration

```csharp
// In Program.cs or Startup.cs
services.AddUmiHealthDataLayer(configuration);

// Add tenant middleware
app.UseTenantMiddleware();
```

#### Repository Usage

```csharp
public class ProductService
{
    private readonly ITenantRepository<Product> _productRepository;
    private readonly IRedisCacheService _cacheService;

    public ProductService(ITenantRepository<Product> productRepository, IRedisCacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<Product> GetProductAsync(string tenantId, string productId)
    {
        var cacheKey = CacheKeys.Product(tenantId, productId);
        var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey);
        
        if (cachedProduct != null)
            return cachedProduct;

        var product = await _productRepository.GetByTenantAndIdAsync(tenantId, productId);
        
        if (product != null)
        {
            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromHours(1));
        }

        return product;
    }
}
```

#### File Storage Usage

```csharp
public class DocumentService
{
    private readonly IFileStorageService _fileStorageService;

    public DocumentService(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task<string> UploadPatientDocumentAsync(string tenantId, string patientId, IFormFile file)
    {
        var container = StorageContainers.PatientDocuments(tenantId, patientId);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        
        using var stream = file.OpenReadStream();
        return await _fileStorageService.UploadFileAsync(container, fileName, stream, file.ContentType);
    }
}
```

## Database Migrations

### Shared Database

Run the `SharedDatabaseMigration.sql` script to create the shared database schema with:
- Tenant management tables
- User management
- Subscription management
- Super admin functionality
- System settings and analytics

### Tenant Database

Run the `TenantDatabaseMigration.sql` script on each tenant database to create:
- Patient management
- Product and inventory management
- Prescription handling
- Sales and payments
- Audit logging

## Security Features

- **Data Isolation**: Complete separation of tenant data at database level
- **Cache Isolation**: Tenant-specific cache keys prevent data leakage
- **File Isolation**: Tenant-specific storage containers
- **Soft Deletes**: Automatic soft delete filtering with global query filters
- **Audit Logging**: Comprehensive audit trail for all data operations

## Performance Optimizations

- **Caching Strategy**: Multi-level caching with configurable expiration
- **Database Indexing**: Optimized indexes for common query patterns
- **Connection Pooling**: Efficient database connection management
- **Async Operations**: All repository methods are fully async

## Monitoring and Health Checks

The infrastructure includes health checks for:
- PostgreSQL database connectivity
- Redis cache connectivity
- File storage accessibility

Access health checks at `/health` endpoint.

### Development Guidelines

1. **Always use tenant-aware repositories** for tenant-specific data
2. **Implement caching** for frequently accessed data
3. **Use appropriate cache keys** following `CacheKeys` conventions
4. **Handle file uploads** through storage service abstraction
5. **Follow repository pattern** for data access consistency

### Future Enhancements

- Cloud storage provider implementations (AWS S3, Azure Blob Storage)
- Advanced caching strategies (cache warming, invalidation)
- Database sharding support
- Read replica support
- Advanced audit logging and reporting
