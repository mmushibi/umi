# UmiHealth System Administration Guide

## Table of Contents
1. [Introduction](#introduction)
2. [System Architecture](#system-architecture)
3. [Installation and Setup](#installation-and-setup)
4. [Configuration Management](#configuration-management)
5. [User and Access Management](#user-and-access-management)
6. [Database Administration](#database-administration)
7. [Security Management](#security-management)
8. [Backup and Recovery](#backup-and-recovery)
9. [Performance Monitoring](#performance-monitoring)
10. [Troubleshooting](#troubleshooting)

---

## Introduction

This System Administration Guide provides comprehensive instructions for managing the UmiHealth pharmacy management system. It is designed for IT administrators, system engineers, and technical support staff responsible for system deployment, maintenance, and optimization.

### Target Audience
- System Administrators
- IT Managers
- Database Administrators
- Network Engineers
- Security Officers
- DevOps Engineers

### Scope
This guide covers:
- System deployment and configuration
- User management and security
- Database administration
- Performance optimization
- Backup and disaster recovery
- Troubleshooting and maintenance

---

## System Architecture

### Multi-Tenant Architecture
- **Tenant Isolation**: Data separation between pharmacies
- **Shared Infrastructure**: Common services and resources
- **Scalable Design**: Horizontal scaling capabilities
- **Security Boundaries**: Role-based access controls

### Core Components
- **API Gateway**: Request routing and load balancing
- **Application Services**: Business logic and data processing
- **Identity Service**: Authentication and authorization
- **Database Layer**: Data persistence and management
- **Background Jobs**: Scheduled tasks and processing
- **Monitoring Stack**: Performance and health monitoring

### Deployment Models
- **On-Premises**: Full control over infrastructure
- **Cloud-Based**: Managed services and scalability
- **Hybrid**: Combination of on-premises and cloud
- **Multi-Cloud**: Multiple cloud providers

---

## Installation and Setup

### Prerequisites
#### Hardware Requirements
- **CPU**: Minimum 4 cores, recommended 8+ cores
- **RAM**: Minimum 16GB, recommended 32GB+
- **Storage**: Minimum 500GB SSD, recommended 1TB+
- **Network**: 1Gbps connection, redundant connections

#### Software Requirements
- **Operating System**: Windows Server 2019+, Linux (Ubuntu 20.04+)
- **Database**: PostgreSQL 15+
- **Container Runtime**: Docker 20.10+, Docker Compose 2.0+
- **Web Server**: Nginx 1.20+ or Apache 2.4+

### Installation Steps
#### 1. Environment Preparation
```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.12.2/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

#### 2. Application Deployment
```bash
# Clone repository
git clone https://github.com/umihealth/umihealth.git
cd umihealth

# Configure environment
cp .env.example .env
nano .env  # Edit configuration

# Deploy services
docker-compose up -d
```

#### 3. Database Setup
```bash
# Connect to PostgreSQL
docker exec -it umihealth-postgres psql -U umihealth -d UmiHealth

# Run migrations
dotnet ef database update --project src/UmiHealth.Infrastructure --startup-project src/UmiHealth.Api

# Seed initial data
dotnet run --project src/UmiHealth.Api --environment Development --seed-data
```

### Service Configuration
#### API Gateway Configuration
```nginx
upstream api_backend {
    server umihealth-api:8080;
}

server {
    listen 80;
    server_name api.umihealth.com;
    
    location / {
        proxy_pass http://api_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

#### Application Settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=UmiHealth;Username=umihealth;Password=your_password"
  },
  "JwtSettings": {
    "Secret": "your_jwt_secret_key",
    "Issuer": "UmiHealth",
    "Audience": "UmiHealthUsers"
  },
  "Redis": {
    "ConnectionString": "redis:6379"
  }
}
```

---

## Configuration Management

### Environment Variables
#### Production Environment
```bash
# Database Configuration
POSTGRES_PASSWORD=secure_password_here
POSTGRES_USER=umihealth
POSTGRES_DB=UmiHealth

# JWT Configuration
JWT_SECRET=your_super_secret_jwt_key_at_least_32_characters
JWT_ISSUER=UmiHealth
JWT_AUDIENCE=UmiHealthUsers

# Redis Configuration
REDIS_PASSWORD=redis_password_here

# Email Configuration
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your_email@gmail.com
SMTP_PASSWORD=your_app_password
```

#### Development Environment
```bash
# Enable development features
ASPNETCORE_ENVIRONMENT=Development
ENABLE_SWAGGER=true
ENABLE_DEBUG_LOGGING=true

# Test configuration
USE_TEST_DATABASE=true
MOCK_EXTERNAL_SERVICES=true
```

### Configuration Files
#### Application Configuration
- **appsettings.json**: Base configuration
- **appsettings.Production.json**: Production overrides
- **appsettings.Development.json**: Development overrides
- **appsettings.Security.json**: Security settings

#### Docker Configuration
- **docker-compose.yml**: Service orchestration
- **docker-compose.override.yml**: Development overrides
- **Dockerfile**: Service build configuration

---

## User and Access Management

### User Administration
#### Creating System Users
1. Navigate to **Admin → User Management**
2. Click **"Add User"**
3. Enter user details:
   - Personal information
   - Contact details
   - Professional credentials
   - Role assignments
   - Branch permissions
4. Set authentication methods
5. Configure security settings
6. Send welcome notification

#### Role Management
##### System Roles
- **Super Admin**: Full system access
- **Admin**: Organization-level access
- **IT Admin**: Technical administration
- **Compliance Officer**: Regulatory oversight
- **Auditor**: Read-only access for audits

##### Permission Matrix
| Role | Users | Inventory | Reports | Settings | Security |
|-------|--------|-----------|---------|----------|----------|
| Super Admin | ✓ | ✓ | ✓ | ✓ | ✓ |
| Admin | ✓ | ✓ | ✓ | ✓ | ✗ |
| IT Admin | ✓ | ✓ | ✓ | ✓ | ✓ |
| Compliance | ✗ | ✓ | ✓ | ✗ | ✓ |
| Auditor | ✗ | ✓ | ✓ | ✗ | ✗ |

### Access Control
#### Authentication Methods
- **Username/Password**: Traditional authentication
- **Two-Factor Authentication**: Enhanced security
- **SSO Integration**: Corporate identity providers
- **API Keys**: Programmatic access
- **Certificate-Based**: High-security environments

#### Session Management
- **Session Timeout**: Configurable inactivity limits
- **Concurrent Sessions**: Limit simultaneous logins
- **Device Management**: Trusted device registration
- **Session Monitoring**: Real-time session tracking

---

## Database Administration

### Database Management
#### Connection Management
```sql
-- Connect to database
psql -h localhost -p 5432 -U umihealth -d UmiHealth

-- List all databases
\l

-- Switch to specific database
\c UmiHealth
```

#### User Management
```sql
-- Create new database user
CREATE USER app_user WITH PASSWORD 'secure_password';

-- Grant permissions
GRANT CONNECT ON DATABASE UmiHealth TO app_user;
GRANT USAGE ON SCHEMA public TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_user;

-- Create role-based permissions
CREATE ROLE pharmacist_role;
GRANT SELECT ON prescriptions TO pharmacist_role;
GRANT SELECT ON patients TO pharmacist_role;
```

### Performance Optimization
#### Index Management
```sql
-- Create performance indexes
CREATE INDEX idx_prescriptions_patient_id ON prescriptions(patient_id);
CREATE INDEX idx_prescriptions_date_created ON prescriptions(date_created);
CREATE INDEX idx_inventory_product_id ON inventory(product_id);

-- Analyze query performance
EXPLAIN ANALYZE SELECT * FROM prescriptions WHERE patient_id = 123;
```

#### Query Optimization
```sql
-- Optimize slow queries
CREATE MATERIALIZED VIEW daily_sales AS
SELECT 
    DATE(created_at) as sale_date,
    COUNT(*) as total_sales,
    SUM(total_amount) as total_revenue
FROM sales 
GROUP BY DATE(created_at);

-- Refresh materialized view
REFRESH MATERIALIZED VIEW daily_sales;
```

### Maintenance Tasks
#### Regular Maintenance
```sql
-- Update table statistics
ANALYZE;

-- Rebuild indexes
REINDEX DATABASE UmiHealth;

-- Vacuum tables
VACUUM ANALYZE prescriptions;
```

---

## Security Management

### Security Configuration
#### Authentication Security
```json
{
  "Authentication": {
    "PasswordPolicy": {
      "MinLength": 12,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireNumbers": true,
      "RequireSpecialChars": true,
      "MaxAge": 90
    },
    "SessionSettings": {
      "TimeoutMinutes": 30,
      "MaxConcurrentSessions": 3,
      "RequireReauth": true
    }
  }
}
```

#### Network Security
```nginx
# Security headers configuration
add_header X-Frame-Options DENY;
add_header X-Content-Type-Options nosniff;
add_header X-XSS-Protection "1; mode=block";
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";

# Rate limiting
limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
limit_req_zone $binary_remote_addr zone=auth:10m rate=5r/m;
```

### Security Monitoring
#### Log Analysis
```bash
# Monitor authentication logs
tail -f /var/log/umihealth/auth.log | grep "failed"

# Monitor API access logs
tail -f /var/log/umihealth/api.log | grep "401\|403\|404"

# Security event monitoring
grep "SECURITY" /var/log/umihealth/application.log
```

#### Intrusion Detection
- **Failed Login Attempts**: Monitor and block suspicious IPs
- **Unusual Access Patterns**: Detect anomalous behavior
- **Data Access Monitoring**: Track sensitive data access
- **API Abuse Detection**: Monitor for automated attacks

---

## Backup and Recovery

### Backup Strategy
#### Database Backups
```bash
#!/bin/bash
# Automated database backup script
BACKUP_DIR="/backups/database"
DATE=$(date +%Y%m%d_%H%M%S)
DB_NAME="UmiHealth"

# Create backup directory
mkdir -p $BACKUP_DIR

# Perform database backup
pg_dump -h localhost -p 5432 -U umihealth -d $DB_NAME > $BACKUP_DIR/backup_$DATE.sql

# Compress backup
gzip $BACKUP_DIR/backup_$DATE.sql

# Remove old backups (keep 30 days)
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: backup_$DATE.sql.gz"
```

#### Application Backups
```bash
# Backup application files
tar -czf /backups/application/app_backup_$(date +%Y%m%d).tar.gz \
    /opt/umihealth/ \
    --exclude=node_modules \
    --exclude=logs \
    --exclude=temp
```

### Recovery Procedures
#### Database Recovery
```bash
# Restore from backup
gunzip -c /backups/database/backup_20240101_120000.sql.gz | \
psql -h localhost -p 5432 -U umihealth -d UmiHealth

# Verify restore
psql -h localhost -p 5432 -U umihealth -d UmiHealth -c "SELECT COUNT(*) FROM users;"
```

#### Disaster Recovery
1. **Assessment**: Evaluate damage and impact
2. **Isolation**: Prevent further damage
3. **Recovery**: Restore from backups
4. **Verification**: Validate system integrity
5. **Communication**: Notify stakeholders

---

## Performance Monitoring

### System Metrics
#### Application Performance
- **Response Times**: API endpoint performance
- **Throughput**: Requests per second
- **Error Rates**: Failed request percentages
- **Resource Usage**: CPU, memory, disk I/O

#### Database Performance
- **Query Performance**: Slow query identification
- **Connection Pooling**: Database connection efficiency
- **Index Usage**: Index effectiveness analysis
- **Lock Contention**: Database locking issues

### Monitoring Tools
#### Prometheus Configuration
```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'umihealth-api'
    static_configs:
      - targets: ['localhost:8080']
    metrics_path: '/metrics'
```

#### Grafana Dashboards
- **System Overview**: Overall system health
- **Application Metrics**: Performance indicators
- **Database Metrics**: Database performance
- **Business Metrics**: KPIs and analytics

### Alert Configuration
#### Critical Alerts
- **System Down**: Service unavailable
- **High Error Rate**: Error percentage > 5%
- **Slow Response**: Response time > 2 seconds
- **Database Issues**: Connection failures

#### Warning Alerts
- **High Memory Usage**: Memory > 80%
- **Disk Space Low**: Available space < 20%
- **High CPU Usage**: CPU > 90% for 5 minutes

---

## Troubleshooting

### Common Issues

#### Application Issues
##### Service Won't Start
```bash
# Check service status
docker-compose ps

# Check service logs
docker-compose logs umihealth-api

# Check port availability
netstat -tulpn | grep :8080

# Restart services
docker-compose restart
```

##### Slow Performance
```bash
# Check system resources
top
htop
iostat -x 1

# Check database performance
pg_stat_activity

# Analyze slow queries
SELECT query, mean_time, calls FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;
```

#### Database Issues
##### Connection Problems
```bash
# Check database status
docker exec -it umihealth-postgres pg_isready

# Check connection logs
docker logs umihealth-postgres

# Test connection
psql -h localhost -p 5432 -U umihealth -d UmiHealth
```

##### Performance Issues
```sql
-- Check active connections
SELECT count(*) FROM pg_stat_activity;

-- Check long-running queries
SELECT pid, now() - pg_stat_activity.query_start AS duration, query 
FROM pg_stat_activity 
WHERE (now() - pg_stat_activity.query_start) > interval '5 minutes';

-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables 
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Diagnostic Commands
```bash
# System health check
curl -f http://localhost:8080/health

# Database connectivity test
docker exec umihealth-api curl -f http://localhost:8080/health/db

# External service connectivity
curl -f https://api.stripe.com/v1/charges

# Memory usage analysis
free -h
df -h
```

### Support Procedures
#### Issue Escalation
1. **Level 1**: Basic troubleshooting
2. **Level 2**: Advanced diagnostics
3. **Level 3**: System architecture issues
4. **Level 4**: Vendor/external dependencies

#### Documentation
- **Issue Tracking**: Record all incidents
- **Resolution Documentation**: Document solutions
- **Knowledge Base**: Build troubleshooting database
- **Post-Mortem**: Analyze major incidents

---

## Appendix

### Configuration Templates
#### Production Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=UmiHealth;Username=umihealth;Password=${POSTGRES_PASSWORD}"
  }
}
```

### Monitoring Scripts
#### Health Check Script
```bash
#!/bin/bash
# System health monitoring script
SERVICES=("umihealth-api" "umihealth-identity" "postgres" "redis")

for service in "${SERVICES[@]}"; do
    if docker ps --format "table {{.Names}}" | grep -q "$service"; then
        echo "✓ $service is running"
    else
        echo "✗ $service is not running"
        # Send alert
        curl -X POST "https://hooks.slack.com/your-webhook" \
             -d '{"text":"'$service' is down on '$(hostname)'"}'
    fi
done
```

### Contact Information
- **Technical Support**: techsupport@umihealth.com
- **Emergency Hotline**: +260-XXX-XXXXXXX (24/7)
- **Security Team**: security@umihealth.com
- **Documentation**: docs.umihealth.com

---

**Document Version**: 1.0  
**Last Updated**: January 2024  
**Next Review**: Quarterly or as system changes occur
