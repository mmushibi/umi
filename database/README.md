# Umi Health Database Schema

This directory contains the complete PostgreSQL database schema for the Umi Health Pharmacy Management System, designed with multi-tenant architecture.

## Files Overview

### Core Schema Files

- **`schema.sql`** - Main database schema with all tables and relationships
- **`indexes.sql`** - Performance optimization indexes
- **`triggers.sql`** - Database triggers for automation and audit logging
- **`row_level_security.sql`** - Multi-tenant security policies

### Migration Files

- **`migrations/001_initial_schema.sql`** - Initial schema creation
- **`migrations/002_add_seed_data.sql`** - Sample data for testing

## Database Architecture

### Multi-Tenant Design

The database uses a shared database, shared schema approach with row-level security (RLS) to ensure tenant isolation:

- **Tenant Isolation**: Each table has a `tenant_id` column for data segregation
- **Row-Level Security**: PostgreSQL RLS policies prevent cross-tenant data access
- **Context Setting**: Session variables control tenant context for queries

### Core Tables

1. **Tenant Management**
   - `tenants` - Organization/tenant information
   - `branches` - Physical locations per tenant

2. **User Management**
   - `users` - System users with role-based permissions
   - Role-based access control with branch isolation

3. **Product & Inventory**
   - `products` - Pharmacy products catalog
   - `suppliers` - Product suppliers
   - `inventory` - Stock levels per branch
   - `stock_movements` - Inventory transaction history

4. **Patient Management**
   - `patients` - Patient records with medical history
   - `prescriptions` - Medical prescriptions
   - `prescription_items` - Individual prescription line items

5. **Sales & Payments**
   - `sales` - Sales transactions
   - `sale_items` - Individual sale line items
   - `payments` - Payment processing records

6. **Audit & Logging**
   - `audit_logs` - Comprehensive audit trail

## Security Features

### Row-Level Security (RLS)

- **Tenant Isolation**: Users can only access data from their tenant
- **Branch Isolation**: Optional branch-level access control
- **Role-Based Access**: Different permissions for admin, pharmacist, cashier roles

### Audit Logging

- Automatic logging of all INSERT, UPDATE, DELETE operations
- Tracks user, timestamp, IP address, and data changes
- Configurable for compliance requirements

## Performance Optimizations

### Indexes

- Composite indexes for tenant-based queries
- Business logic indexes for common query patterns
- Full-text search indexes for product and patient search
- Unique constraints for business rule enforcement

### Triggers

- Automatic timestamp updates (`updated_at`)
- Inventory management automation
- Auto-generation of reference numbers
- Audit trail creation

## Setup Instructions

### 1. Database Creation

```sql
CREATE DATABASE umi_health;
\c umi_health;
```

### 2. Run Migrations

```bash
# Run initial schema
psql -d umi_health -f migrations/001_initial_schema.sql

# Run seed data (optional, for testing)
psql -d umi_health -f migrations/002_add_seed_data.sql
```

### 3. User Setup

```sql
-- Create application user
CREATE USER umi_health_app WITH PASSWORD 'your_secure_password';

-- Grant necessary permissions
GRANT CONNECT ON DATABASE umi_health TO umi_health_app;
GRANT USAGE ON SCHEMA public TO umi_health_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO umi_health_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO umi_health_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO umi_health_app;

-- Grant role membership
GRANT authenticated_users TO umi_health_app;
```

## Application Integration

### Setting Tenant Context

Before executing queries, set the tenant context:

```sql
SELECT set_tenant_context(
    'tenant-uuid-here', 
    'branch-uuid-here', 
    'user-role-here',
    '["permissions", "array"]'::JSONB
);
```

### Query Examples

```sql
-- All queries automatically filter by tenant
SELECT * FROM patients WHERE last_name = 'Mwangi';

-- Clear context when done
SELECT clear_tenant_context();
```

## Data Validation

### Business Rules

- **Patient Numbers**: Auto-generated with tenant prefix
- **Prescription Numbers**: Auto-generated per tenant per day
- **Sale Numbers**: Auto-generated per tenant per day
- **Inventory**: Automatic stock level management
- **Low Stock Alerts**: Configurable reorder levels

### Constraints

- Unique constraints for business identifiers
- Foreign key relationships for data integrity
- Check constraints for data validation

## Maintenance

### Regular Tasks

1. **Backup**: Regular database backups
2. **Archive**: Archive old audit logs
3. **Optimize**: Regular VACUUM and ANALYZE
4. **Monitor**: Monitor query performance

### Scaling Considerations

- Partitioning for large tables (audit_logs, stock_movements)
- Read replicas for reporting queries
- Connection pooling for high concurrency

## Testing

### Sample Data

The seed data includes:
- 1 sample tenant with 1 branch
- 8 sample products (prescription and OTC)
- 3 sample users (admin, pharmacist, cashier)
- 3 sample patients
- 3 sample prescriptions

### Test Queries

```sql
-- Test tenant isolation
SELECT COUNT(*) FROM patients; -- Should only show tenant's patients

-- Test inventory levels
SELECT p.name, i.quantity_on_hand 
FROM inventory i 
JOIN products p ON i.product_id = p.id 
WHERE i.quantity_on_hand <= i.reorder_level;
```

## Security Best Practices

1. **Connection Security**: Use SSL for database connections
2. **Password Security**: Strong passwords for all database users
3. **Access Control**: Principle of least privilege
4. **Audit Monitoring**: Regular review of audit logs
5. **Data Encryption**: Consider encryption for sensitive data

## Compliance

The schema is designed to support:
- **Data Privacy**: Patient data protection
- **Audit Requirements**: Complete audit trail
- **Regulatory Compliance**: Pharmacy management regulations
- **Multi-Tenancy**: Data isolation requirements
