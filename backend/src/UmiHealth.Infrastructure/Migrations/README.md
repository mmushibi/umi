# UmiHealth Database Migrations

This directory contains database migrations for the UmiHealth application.

## Migration Files

### InitialCreate (20240101000000_InitialCreate.cs)
- **Type**: Code-based migration
- **Description**: Creates the initial database schema with core entities
- **Entities Created**:
  - Tenants
  - Branches  
  - Users
  - Related indexes and constraints

### InitialCreate.sql
- **Type**: SQL script migration
- **Description**: SQL version of the initial migration for direct execution
- **Usage**: Can be run directly with psql or other PostgreSQL clients

### UmiHealthDbContextModelSnapshot.cs
- **Type**: EF Core model snapshot
- **Description**: Current state of the DbContext model used for comparison when generating new migrations

## Running Migrations

### Option 1: Using PowerShell Script
```powershell
# From the backend directory
.\scripts\run-migration.ps1

# With custom connection string
.\scripts\run-migration.ps1 -ConnectionString "Host=localhost;Database=umihealth;Username=postgres;Password=root"
```

### Option 2: Using psql directly
```bash
psql -h localhost -U postgres -d umihealth -f src/UmiHealth.Infrastructure/Migrations/InitialCreate.sql
# Or with full path if psql is not in PATH:
"C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -U postgres -d umihealth -f src/UmiHealth.Infrastructure/Migrations/InitialCreate.sql
```

### Option 3: Using Entity Framework Core (when build issues are resolved)
```bash
dotnet ef database update --project src/UmiHealth.Infrastructure --context UmiHealthDbContext
```

## Database Schema

### Core Tables

#### Tenants
- Multi-tenant architecture support
- Contains tenant configuration and subscription information
- Unique constraint on subdomain

#### Branches
- Pharmacy branches/locations
- Linked to tenants with foreign key relationship
- Unique constraint on code per tenant

#### Users
- Application users
- Multi-tenant with soft delete support
- Unique constraints on email and username per tenant

### Key Features

- **Multi-tenancy**: All tenant-specific tables include TenantId
- **Soft Delete**: All entities include IsDeleted, DeletedAt, DeletedBy fields
- **Audit Trail**: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy on all entities
- **UUID Primary Keys**: All tables use UUID primary keys with PostgreSQL uuid-ossp extension
- **Timestamps**: Automatic timestamp updates via triggers
- **Indexes**: Optimized indexes for common query patterns

## Creating New Migrations

When the build issues are resolved, new migrations can be created using:

```bash
dotnet ef migrations add MigrationName --project src/UmiHealth.Infrastructure --context UmiHealthDbContext
```

## Troubleshooting

### Build Issues
Currently the project has build issues preventing EF Core migrations from working directly. The manual SQL migration is provided as a workaround.

### Connection Issues
Ensure PostgreSQL is running and the connection string is correct. Default connection:
- Host: localhost
- Database: umihealth  
- Username: postgres
- Password: root

### Permission Issues
Ensure the PostgreSQL user has permission to create tables, indexes, and triggers.
