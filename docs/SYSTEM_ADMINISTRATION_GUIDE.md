# Umi Health System Administration Guide

## Overview

This guide provides comprehensive information for system administrators responsible for deploying, maintaining, and securing the Umi Health Pharmacy Management System using modern microservices architecture and Docker containerization.

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Installation and Deployment](#installation-and-deployment)
3. [Database Management](#database-management)
4. [Security Configuration](#security-configuration)
5. [Monitoring and Maintenance](#monitoring-and-maintenance)
6. [Backup and Recovery](#backup-and-recovery)
7. [Troubleshooting](#troubleshooting)
8. [Performance Optimization](#performance-optimization)

## System Architecture

### Components Overview

```text
┌─────────────────────────────────────────────────────────────────┐
│                    Frontend Layer                        │
├─────────────────────────────────────────────────────────────────┤
│  Admin Portal │ Pharmacist Portal │ Cashier Portal │ Operations │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│              API Gateway (.NET 8.0)                     │
├─────────────────────────────────────────────────────────────────┤
│  Authentication │ Rate Limiting │ Routing │ Load Balancing    │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                Microservices Layer                    │
├─────────────────────────────────────────────────────────────────┤
│ Identity Service │ UmiHealth API │ Background Jobs │ Minimal API│
│ Tenant Management │ User Management │ Pharmacy Operations │     │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                 Data Layer                           │
├─────────────────────────────────────────────────────────────────┤
│ PostgreSQL 15 │ Redis 7 │ File Storage │ Monitoring      │
└─────────────────────────────────────────────────────────────────┘
```

### Technology Stack

- **Container Platform**: Docker & Docker Compose
- **Backend Framework**: .NET 8.0/.NET 10.0
- **Database**: PostgreSQL 15 with multi-tenant support
- **Cache**: Redis 7
- **Monitoring**: Prometheus + Grafana
- **Logging**: Serilog with structured logging

## Installation and Deployment

### Prerequisites

```bash
# Minimum system requirements
- CPU: 4 cores
- RAM: 8GB
- Storage: 100GB SSD
- OS: Ubuntu 20.04+ / CentOS 8+ / Windows Server 2019+

# Required software
- Docker 20.10+
- Docker Compose 2.0+
- Git
```

### Quick Deployment

```bash
# 1. Clone repository
git clone https://github.com/mmushibi/umi.git
cd umi

# 2. Configure environment
cp appsettings.Security.json.example appsettings.Security.json
# Edit with your configuration

# 3. Deploy services
docker-compose up -d

# 4. Verify deployment
docker-compose ps
curl http://localhost/health
```

### Production Deployment

```bash
# Deploy with production profile
docker-compose -f docker-compose.yml -f docker-compose.override.yml --profile production up -d

# Scale services as needed
docker-compose up -d --scale umihealth-api=3
```

## Database Management

### PostgreSQL Configuration

```bash
# Connect to database
docker-compose exec postgres psql -U umihealth -d UmiHealth

# Create new tenant
SELECT create_tenant_schema(
    'tenant-uuid',
    'tenant_schema_name'
);

# Backup database
docker-compose exec postgres pg_dump -U umihealth UmiHealth > backup.sql

# Restore database
docker-compose exec -T postgres psql -U umihealth UmiHealth < backup.sql
```

### Database Migrations

```bash
# Run migrations
docker-compose exec umihealth-api dotnet ef database update

# Generate new migration
docker-compose exec umihealth-api dotnet ef migrations add MigrationName

# Reset database (development only)
docker-compose exec umihealth-api dotnet ef database drop
docker-compose exec umihealth-api dotnet ef database update
```

### Multi-Tenant Management

```sql
-- List all tenants
SELECT id, name, subdomain, status FROM shared.tenants;

-- Create new tenant
INSERT INTO shared.tenants (name, subdomain, database_name, subscription_plan)
VALUES ('New Pharmacy', 'newpharmacy', 'newpharmacy_db', 'premium');

-- Add branch to tenant
INSERT INTO shared.branches (tenant_id, name, code, address)
VALUES ('tenant-uuid', 'Main Branch', 'MAIN', '123 Main St');
```

## Security Configuration

### JWT Configuration

```json
{
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com",
    "AccessTokenExpiration": "00:15:00",
    "RefreshTokenExpiration": "7.00:00:00"
  }
}
```

### SSL/TLS Setup

```bash
# Generate SSL certificates (development)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/ssl/private.key \
  -out nginx/ssl/certificate.crt

# Use Let's Encrypt (production)
sudo certbot --nginx -d yourdomain.com
```

### Firewall Configuration

```bash
# Ubuntu UFW
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable

# CentOS firewalld
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https
sudo firewall-cmd --reload
```

## Monitoring and Maintenance

### Health Monitoring

```bash
# Check all service health
curl http://localhost/health
curl http://localhost:5001/health
curl http://localhost:5000/health

# Monitor container resources
docker stats

# View service logs
docker-compose logs -f umihealth-api
docker-compose logs -f identity-service
```

### Prometheus Metrics

Access metrics at:
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000

Key metrics to monitor:
- `umi_api_requests_total` - Total API requests
- `umi_api_request_duration_seconds` - Request duration
- `process_cpu_seconds_total` - CPU usage
- `process_resident_memory_bytes` - Memory usage

### Log Management

```bash
# View application logs
docker-compose logs -f --tail=100 umihealth-api

# Rotate logs (add to crontab)
0 2 * * * docker system prune -f

# Export logs
docker-compose logs --no-color > application-logs.txt
```

## Backup and Recovery

### Automated Backup Script

```bash
#!/bin/bash
# backup.sh

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/backups"

# Create backup directory
mkdir -p $BACKUP_DIR

# Database backup
docker-compose exec -T postgres pg_dump -U umihealth UmiHealth > $BACKUP_DIR/db_backup_$DATE.sql

# Volume backups
docker run --rm -v umihealth_postgres_data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/postgres_$DATE.tar.gz -C /data .
docker run --rm -v umihealth_redis_data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/redis_$DATE.tar.gz -C /data .

# Clean old backups (keep 7 days)
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete
```

### Recovery Procedures

```bash
# Restore database
docker-compose exec -T postgres psql -U umihealth UmiHealth < backup.sql

# Restore volumes
docker run --rm -v umihealth_postgres_data:/data -v $(pwd):/backup alpine tar xzf postgres_backup.tar.gz -C /data

# Full system recovery
docker-compose down
# Restore volumes
docker-compose up -d
```

## Troubleshooting

### Common Issues

#### Service Won't Start
```bash
# Check logs
docker-compose logs service-name

# Check resource usage
docker stats

# Restart service
docker-compose restart service-name
```

#### Database Connection Issues
```bash
# Test database connection
docker-compose exec postgres pg_isready -U umihealth

# Check connection string
docker-compose exec umihealth-api env | grep ConnectionStrings
```

#### Authentication Issues
```bash
# Check JWT configuration
docker-compose exec identity-service env | grep Jwt

# Verify user in database
docker-compose exec postgres psql -U umihealth -c "SELECT * FROM shared.users WHERE email='user@example.com';"
```

### Debug Commands

```bash
# System information
docker-compose version
docker version
docker info

# Network diagnostics
docker network ls
docker network inspect umihealth_umihealth-network

# Container diagnostics
docker inspect container-name
docker top container-name
```

## Performance Optimization

### Database Optimization

```sql
-- Analyze query performance
SELECT query, mean_time, calls FROM pg_stat_statements 
ORDER BY mean_time DESC LIMIT 10;

-- Create indexes for performance
CREATE INDEX CONCURRENTLY idx_patients_tenant_id ON patients(tenant_id);
CREATE INDEX CONCURRENTLY idx_prescriptions_patient_id ON prescriptions(patient_id);

-- Update statistics
ANALYZE;
```

### Application Optimization

```bash
# Monitor application performance
curl http://localhost/metrics

# Check memory usage
docker stats --no-stream

# Optimize Docker resources
# Edit docker-compose.yml to add resource limits:
services:
  umihealth-api:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
```

### Caching Strategy

```bash
# Redis optimization
docker-compose exec redis redis-cli CONFIG SET maxmemory 512mb
docker-compose exec redis redis-cli CONFIG SET maxmemory-policy allkeys-lru

# Monitor Redis performance
docker-compose exec redis redis-cli INFO memory
docker-compose exec redis redis-cli INFO stats
```

## Maintenance Schedule

### Daily Tasks
- [ ] Monitor system health dashboards
- [ ] Check error logs for issues
- [ ] Verify backup completion
- [ ] Review resource utilization

### Weekly Tasks
- [ ] Apply security patches
- [ ] Review performance metrics
- [ ] Clean up old logs and temporary files
- [ ] Test backup restoration

### Monthly Tasks
- [ ] Update application versions
- [ ] Security audit and vulnerability scan
- [ ] Database maintenance and optimization
- [ ] Review and update documentation

---

For additional support or questions, contact the system administration team at admin@umihealth.com or refer to the [API Documentation](API_DOCUMENTATION.md) and [Implementation Guide](IMPLEMENTATION_GUIDE.md).
│  (Authentication, Rate Limiting, Routing, Load Balancing) │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                  Microservices Layer                        │
├─────────────────────────────────────────────────────────────────┤
│ Auth │ Users │ Pharmacy │ Inventory │ POS │ Reports │ Jobs │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                     Data Layer                             │
├─────────────────────────────────────────────────────────────────┤
│ PostgreSQL │ Redis Cache │ File Storage │ Search Engine  │
└─────────────────────────────────────────────────────────────────┘
```

### Multi-Tenant Architecture

- **Shared Database**: Common schema for tenants, users, subscriptions
- **Tenant-Specific Data**: Isolated data per tenant with RLS
- **Branch Hierarchy**: Multi-branch support within tenants
- **Resource Isolation**: Separate resources per tenant

## Installation and Deployment

### Prerequisites

- **Operating System**: Windows Server 2019+ or Ubuntu 20.04+
- **.NET Runtime**: .NET 8.0 Runtime
- **Database**: PostgreSQL 14+
- **Web Server**: IIS 10+ or Nginx
- **SSL Certificate**: Valid SSL certificate for HTTPS

### Docker Deployment (Recommended)

#### 1. Prepare Environment

```bash
# Clone repository
git clone https://github.com/umihealth/umi-health.git
cd umi-health

# Copy environment template
cp .env.example .env

# Edit environment variables
nano .env
```

#### 2. Configure Environment Variables

```bash
# Database Configuration
POSTGRES_PASSWORD=your_secure_password
POSTGRES_USER=umihealth
POSTGRES_DB=UmiHealth

# Application Configuration
JWT_SECRET=your_jwt_secret_key
JWT_ISSUER=UmiHealthApi
JWT_AUDIENCE=UmiHealthApi

# Redis Configuration
REDIS_PASSWORD=your_redis_password

# Email Configuration
SMTP_SERVER=smtp.yourprovider.com
SMTP_PORT=587
SMTP_USERNAME=your_email@domain.com
SMTP_PASSWORD=your_email_password
```

#### 3. Deploy Services

```bash
# Build and start all services
docker-compose up -d

# Verify services are running
docker-compose ps

# View logs
docker-compose logs -f
```

### Manual Deployment

#### 1. Database Setup

```sql
-- Create database and user
CREATE DATABASE umihealth;
CREATE USER umihealth WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE umihealth TO umihealth;

-- Run migrations
dotnet ef database update --project UmiHealth.Infrastructure
```

#### 2. Application Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=umihealth;Username=umihealth;Password=your_password"
  },
  "JwtSettings": {
    "Secret": "your_jwt_secret_key",
    "Issuer": "UmiHealthApi",
    "Audience": "UmiHealthApi",
    "AccessTokenExpiration": "00:15:00",
    "RefreshTokenExpiration": "7.00:00:00"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=your_redis_password"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.yourprovider.com",
    "SmtpPort": 587,
    "Username": "your_email@domain.com",
    "Password": "your_email_password",
    "FromEmail": "noreply@umihealth.com"
  }
}
```

#### 3. IIS Configuration

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\UmiHealth.Api.dll" 
                stdoutLogEnabled="false" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

## Database Management

### Schema Structure

- **Shared Schema**: System-wide tables (tenants, users, subscriptions)
- **Tenant Schema**: Tenant-specific data (patients, prescriptions, inventory)
- **Row-Level Security**: Automatic data isolation per tenant

### Maintenance Operations

#### Regular Maintenance

```sql
-- Update statistics
ANALYZE;

-- Reindex tables
REINDEX DATABASE umihealth;

-- Clean up old audit logs (older than 1 year)
DELETE FROM audit_logs WHERE created_at < NOW() - INTERVAL '1 year';

-- Vacuum database
VACUUM ANALYZE;
```

#### Performance Monitoring

```sql
-- Check slow queries
SELECT query, mean_time, calls, total_time
FROM pg_stat_statements
WHERE mean_time > 1000
ORDER BY mean_time DESC
LIMIT 10;

-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) as size
FROM pg_tables
WHERE schemaname NOT IN ('information_schema', 'pg_catalog')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Backup Strategy

```bash
#!/bin/bash
# Daily backup script
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups/umihealth"
DB_NAME="umihealth"

# Create backup directory
mkdir -p $BACKUP_DIR

# Perform backup
pg_dump -h localhost -U umihealth -d $DB_NAME > $BACKUP_DIR/umihealth_$DATE.sql

# Compress backup
gzip $BACKUP_DIR/umihealth_$DATE.sql

# Remove backups older than 30 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: umihealth_$DATE.sql.gz"
```

## Security Configuration

### Authentication and Authorization

#### JWT Configuration

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });
```

#### Role-Based Access Control

```csharp
// Policy definitions
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("PharmacistOrAdmin", policy => 
        policy.RequireRole("Pharmacist", "Admin"));
    
    options.AddPolicy("CanManageInventory", policy => 
        policy.RequireClaim("permission", "inventory:manage"));
});
```

### Security Headers Configuration

```csharp
// Security headers middleware
app.UseSecurityHeaders();

// Headers added:
// - Content-Security-Policy
// - X-Frame-Options: DENY
// - X-Content-Type-Options: nosniff
// - X-XSS-Protection: 1; mode=block
// - Strict-Transport-Security
// - Referrer-Policy
```

### Data Encryption

```csharp
// Entity-level encryption
public class Patient
{
    [Encrypted]
    public string PhoneNumber { get; set; }
    
    [Encrypted]
    public string Email { get; set; }
}

// Configuration for encryption
services.AddDataProtection()
    .PersistKeysToFileSystem("/keys")
    .UseCryptographicAlgorithms(
        new AuthenticatedEncryptorConfiguration());
```

### Network Security

```nginx
# Nginx security configuration
server {
    # Hide server version
    server_tokens off;
    
    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=limit:10m rate=10r/s;
    limit_req zone=limit burst=20 nodelay;
    
    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA384:ECDHE-RSA-AES128-GCM-SHA256;
    ssl_prefer_server_ciphers off;
}
```

## Monitoring and Maintenance

### Application Monitoring

#### Health Checks

```csharp
// Health check configuration
services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<ExternalServiceHealthCheck>("external-api");

// Health check endpoint
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
```

#### Logging Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/umihealth-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq-server:5341"
        }
      }
    ]
  }
}
```

### Performance Monitoring

#### Metrics Collection

```csharp
// Prometheus metrics
var counter = Metrics.CreateCounter("umihealth_requests_total", "Total requests");
var histogram = Metrics.CreateHistogram("umihealth_request_duration", "Request duration");

// Track requests
app.Use((context, next) =>
{
    counter.WithLabels(context.Request.Path).Inc();
    
    using var timer = histogram.WithLabels(context.Request.Path).NewTimer();
    await next(context);
});
```

#### Database Performance

```sql
-- Enable query logging
ALTER SYSTEM SET log_min_duration_statement = 1000; -- Log queries > 1s
ALTER SYSTEM SET log_statement = 'all';

-- Monitor connections
SELECT 
    state,
    count(*) as connection_count
FROM pg_stat_activity
GROUP BY state;
```

## Backup and Recovery

### Automated Backup Strategy

```bash
#!/bin/bash
# Comprehensive backup script
BACKUP_DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_PATH="/backups/umihealth"
S3_BUCKET="umihealth-backups"

# Database backup
pg_dump -h localhost -U umihealth -d umihealth | gzip > $BACKUP_PATH/db_$BACKUP_DATE.sql.gz

# File backup
tar -czf $BACKUP_PATH/files_$BACKUP_DATE.tar.gz /var/www/umihealth/uploads

# Configuration backup
tar -czf $BACKUP_PATH/config_$BACKUP_DATE.tar.gz /etc/umihealth

# Upload to S3
aws s3 cp $BACKUP_PATH/db_$BACKUP_DATE.sql.gz s3://$S3_BUCKET/database/
aws s3 cp $BACKUP_PATH/files_$BACKUP_DATE.tar.gz s3://$S3_BUCKET/files/
aws s3 cp $BACKUP_PATH/config_$BACKUP_DATE.tar.gz s3://$S3_BUCKET/config/

# Cleanup local files older than 7 days
find $BACKUP_PATH -name "*.gz" -mtime +7 -delete

echo "Backup completed: $BACKUP_DATE"
```

### Disaster Recovery

#### Recovery Procedures

1. **Assess Impact**: Determine scope of data loss
2. **Notify Stakeholders**: Inform affected users
3. **Isolate Systems**: Prevent further damage
4. **Restore from Backup**: Use most recent clean backup
5. **Verify Data**: Ensure data integrity
6. **Test Systems**: Verify all functionality
7. **Monitor**: Watch for issues post-recovery

#### Recovery Testing

```bash
# Test restore process
TEST_DB="umihealth_test"
BACKUP_FILE="/backups/umihealth/db_latest.sql.gz"

# Create test database
createdb $TEST_DB

# Restore backup
gunzip -c $BACKUP_FILE | psql -h localhost -U umihealth -d $TEST_DB

# Verify data integrity
psql -h localhost -U umihealth -d $TEST_DB -c "SELECT COUNT(*) FROM patients;"

# Clean up test database
dropdb $TEST_DB
```

## Troubleshooting

### Common Issues

#### Database Connection Issues

**Symptoms**: Application cannot connect to database
**Solutions**:

1. Check PostgreSQL service status
2. Verify connection string parameters
3. Check network connectivity
4. Review PostgreSQL logs
5. Test connection manually

```bash
# Check PostgreSQL status
systemctl status postgresql

# Test connection
psql -h localhost -U umihealth -d umihealth -c "SELECT 1;"
```

#### Performance Issues

**Symptoms**: Slow application response
**Solutions**:

1. Check system resources (CPU, memory, disk)
2. Analyze slow queries
3. Review application logs
4. Check database statistics
5. Monitor network latency

```sql
-- Find slow queries
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    rows
FROM pg_stat_statements
WHERE mean_time > 1000
ORDER BY mean_time DESC
LIMIT 10;
```

#### Memory Issues

**Symptoms**: Out of memory errors
**Solutions**:

1. Monitor memory usage patterns
2. Check for memory leaks
3. Adjust application pool settings
4. Optimize database queries
5. Increase system memory if needed

### Log Analysis

#### Application Logs

```bash
# View recent application logs
tail -f /var/log/umihealth/application.log

# Search for errors
grep "ERROR" /var/log/umihealth/application.log | tail -20

# Analyze request patterns
grep "POST /api/" /var/log/umihealth/access.log | awk '{print $7}' | sort | uniq -c
```

#### Database Logs

```bash
# PostgreSQL log location
/var/log/postgresql/postgresql-$(date +%Y-%m-%d).log

# View recent errors
tail -f /var/log/postgresql/postgresql.log | grep ERROR
```

## Performance Optimization

### Database Optimization

```sql
-- Create indexes for performance
CREATE INDEX CONCURRENTLY idx_patients_tenant_branch 
ON patients(tenant_id, branch_id);

CREATE INDEX CONCURRENTLY idx_prescriptions_patient_date 
ON prescriptions(patient_id, date_prescribed);

-- Update table statistics
ANALYZE patients;
ANALYZE prescriptions;
ANALYZE inventory;

-- Partition large tables
CREATE TABLE sales_2024_01 PARTITION OF sales
FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
```

### Application Optimization

```csharp
// Connection pooling
services.AddDbContext<UmiHealthDbContext>(options =>
    options.UseNpgsql(connectionString))
    .AddDbContextPool<UmiHealthDbContext>(options =>
        options.UseNpgsql(connectionString), 
        poolSize: 100);

// Response caching
services.AddResponseCaching(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.MaximumBodySize = 1024 * 1024 * 10; // 10MB
});

// Enable compression
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

### Caching Strategy

```csharp
// Redis caching
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379,password=your_password";
    options.InstanceName = "UmiHealth_";
});

// Cache usage
public class ProductService
{
    private readonly IDistributedCache _cache;
    
    public async Task<Product> GetProductAsync(Guid id)
    {
        var cacheKey = $"product_{id}";
        var cachedProduct = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedProduct))
        {
            var product = await _repository.GetByIdAsync(id);
            await _cache.SetStringAsync(cacheKey, 
                JsonSerializer.Serialize(product), 
                TimeSpan.FromHours(1));
            return product;
        }
        
        return JsonSerializer.Deserialize<Product>(cachedProduct);
    }
}
```

---

## Appendices

### Configuration Reference

Detailed configuration options for all system components.

### Security Checklist

- [ ] SSL certificates installed and valid
- [ ] Security headers configured
- [ ] Database encryption enabled
- [ ] Backup encryption configured
- [ ] Access controls implemented
- [ ] Audit logging enabled
- [ ] Intrusion detection configured
- [ ] Regular security updates applied

### Performance Monitoring

- [ ] Response time monitoring
- [ ] Database performance tracking
- [ ] Resource utilization monitoring
- [ ] Error rate monitoring
- [ ] User experience metrics
- [ ] Automated alerting configured

### Maintenance Schedule

- **Daily**: System health checks, log review
- **Weekly**: Performance analysis, security scan
- **Monthly**: Backup verification, updates
- **Quarterly**: Security audit, performance tuning
- **Annually**: Disaster recovery test, capacity planning

---

*Guide Version: 2.1*  
*Last Updated: January 15, 2024*  
*Next Review: January 15, 2025*
