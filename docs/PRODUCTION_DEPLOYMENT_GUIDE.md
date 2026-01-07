# Umi Health Production Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the Umi Health Pharmacy Management System to production environments using Docker containers and modern DevOps practices.

## Prerequisites

### Infrastructure Requirements
- **Minimum Server Specs**: 4 CPU cores, 8GB RAM, 100GB SSD storage
- **Recommended Specs**: 8 CPU cores, 16GB RAM, 200GB SSD storage
- **Operating System**: Ubuntu 20.04+ or CentOS 8+
- **Docker**: Version 20.10+
- **Docker Compose**: Version 2.0+

### Network Requirements
- **Ports**: 80, 443, 5000, 5001, 5432, 6379, 3000, 9090
- **SSL Certificate**: Valid SSL certificate for HTTPS
- **Domain**: Configured domain name with DNS records

## Environment Configuration

### 1. Environment Variables

Create `.env` file in the project root:

```bash
# Database Configuration
POSTGRES_PASSWORD=your_secure_password
POSTGRES_USER=umihealth
POSTGRES_DB=UmiHealth

# Redis Configuration
REDIS_PASSWORD=your_redis_password

# JWT Configuration
JWT_SECRET=your_jwt_secret_key_minimum_32_characters
JWT_ISSUER=https://yourdomain.com
JWT_AUDIENCE=https://yourdomain.com

# Email Configuration
SMTP_SERVER=smtp.yourprovider.com
SMTP_PORT=587
SMTP_USERNAME=your_email@yourdomain.com
SMTP_PASSWORD=your_email_password

# Monitoring
GRAFANA_USER=admin
GRAFANA_PASSWORD=your_grafana_password
```

### 2. SSL Certificate Setup

```bash
# Create SSL directory
mkdir -p nginx/ssl

# Copy your SSL certificates
cp your-cert.pem nginx/ssl/
cp your-key.pem nginx/ssl/
```

## Deployment Steps

### 1. Server Preparation

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Create application directory
sudo mkdir -p /opt/umihealth
sudo chown $USER:$USER /opt/umihealth
cd /opt/umihealth
```

### 2. Application Deployment

```bash
# Clone repository
git clone https://github.com/mmushibi/umi.git .

# Configure environment
cp appsettings.Security.json.example appsettings.Security.json
# Edit the file with your production settings

# Build and start services
docker-compose -f docker-compose.yml -f docker-compose.override.yml --profile production up -d

# Verify deployment
docker-compose ps
```

### 3. Database Initialization

```bash
# Run database migrations
docker-compose exec umihealth-api dotnet ef database update

# Create initial admin user
docker-compose exec identity-service dotnet run --project CreateAdminUser
```

## Monitoring and Maintenance

### Health Checks

Monitor service health using the built-in endpoints:

```bash
# API Gateway
curl https://yourdomain.com/health

# Individual services
curl https://yourdomain.com/api/health
curl https://yourdomain.com/identity/health
```

### Log Management

```bash
# View application logs
docker-compose logs -f umihealth-api

# View all service logs
docker-compose logs -f

# Rotate logs (add to crontab)
0 2 * * * docker system prune -f
```

### Backup Strategy

#### Database Backup

```bash
# Create backup script
cat > backup.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker-compose exec -T postgres pg_dump -U umihealth UmiHealth > backup_${DATE}.sql
gzip backup_${DATE}.sql
EOF

chmod +x backup.sh

# Schedule daily backups (crontab)
0 1 * * * /opt/umihealth/backup.sh
```

#### Volume Backup

```bash
# Backup all volumes
docker run --rm -v umihealth_postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres_backup.tar.gz -C /data .
docker run --rm -v umihealth_redis_data:/data -v $(pwd):/backup alpine tar czf /backup/redis_backup.tar.gz -C /data .
```

## Scaling and Performance

### Horizontal Scaling

```bash
# Scale API services
docker-compose up -d --scale umihealth-api=3

# Add load balancer configuration
# Update nginx/nginx.conf for multiple upstream servers
```

### Performance Optimization

```bash
# Optimize PostgreSQL
# Edit postgresql.conf in the container or use custom config

# Redis optimization
# Update redis.conf for memory management

# Application optimization
# Monitor performance metrics in Grafana
```

## Security Configuration

### Firewall Setup

```bash
# Configure UFW firewall
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

### SSL/TLS Configuration

```bash
# Use Let's Encrypt for free SSL certificates
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d yourdomain.com
```

### Security Headers

Update `nginx/nginx.conf` to include security headers:

```nginx
add_header X-Frame-Options "SAMEORIGIN" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header X-Content-Type-Options "nosniff" always;
add_header Referrer-Policy "no-referrer-when-downgrade" always;
add_header Content-Security-Policy "default-src 'self' http: https: data: blob: 'unsafe-inline'" always;
```

## Troubleshooting

### Common Issues

#### Service Won't Start
```bash
# Check logs
docker-compose logs service-name

# Check resource usage
docker stats

# Restart services
docker-compose restart
```

#### Database Connection Issues
```bash
# Verify database is running
docker-compose exec postgres pg_isready -U umihealth

# Check connection string
docker-compose exec umihealth-api env | grep ConnectionStrings
```

#### Performance Issues
```bash
# Monitor resource usage
docker stats
htop
iotop

# Check slow queries
docker-compose exec postgres psql -U umihealth -c "SELECT query, mean_time, calls FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;"
```

## Disaster Recovery

### Recovery Procedures

1. **Database Recovery**:
```bash
# Restore from backup
gunzip backup_YYYYMMDD_HHMMSS.sql.gz
docker-compose exec -T postgres psql -U umihealth UmiHealth < backup_YYYYMMDD_HHMMSS.sql
```

2. **Volume Recovery**:
```bash
# Restore volumes
docker run --rm -v umihealth_postgres_data:/data -v $(pwd):/backup alpine tar xzf /backup/postgres_backup.tar.gz -C /data
```

3. **Full System Recovery**:
```bash
# Re-deploy from scratch with data restoration
docker-compose down
# Restore volumes
docker-compose up -d
```

## Maintenance Schedule

### Daily Tasks
- Monitor system health
- Check backup completion
- Review error logs

### Weekly Tasks
- Update security patches
- Review performance metrics
- Clean up old logs and backups

### Monthly Tasks
- Update application versions
- Security audit
- Performance tuning

## Support and Monitoring

### Monitoring Dashboard
- Grafana: `https://yourdomain.com:3000`
- Prometheus: `https://yourdomain.com:9090`

### Alert Configuration
Set up alerts in Grafana for:
- High CPU/Memory usage
- Database connection issues
- Application errors
- SSL certificate expiration

### Contact Information
- Technical Support: support@umihealth.com
- Emergency Contact: emergency@umihealth.com
- Documentation: [docs/](docs/)

---

**Note**: This guide should be adapted based on your specific infrastructure requirements and security policies. Always test deployment procedures in a staging environment before applying to production.
