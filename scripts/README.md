# Umi Health Data Cleanup Scripts

This directory contains comprehensive cleanup scripts for removing all data, cache, and temporary files from the Umi Health application.

## 🚨 WARNING: IRREVERSIBLE DATA DESTRUCTION

These scripts will **PERMANENTLY DELETE** all data including:
- Database records (patients, prescriptions, inventory, etc.)
- User accounts and authentication data
- Cache and session data
- Docker containers and volumes
- Temporary files and logs
- Browser local storage

**Use only when you want to completely reset the application to a clean state.**

## Available Scripts

### 1. Master Cleanup Scripts (Recommended)

#### `master-cleanup.sh` (Linux/macOS)
Performs comprehensive cleanup of all systems:
```bash
chmod +x master-cleanup.sh
./master-cleanup.sh
```

#### `cleanup.ps1` (Windows PowerShell)
Performs comprehensive cleanup with additional options:
```powershell
# Full cleanup
.\cleanup.ps1

# Force cleanup without confirmation
.\cleanup.ps1 -Force

# Cleanup only databases
.\cleanup.ps1 -DatabaseOnly

# Cleanup only cache
.\cleanup.ps1 -CacheOnly

# Cleanup only Docker resources
.\cleanup.ps1 -DockerOnly
```

### 2. Individual Component Scripts

#### `cleanup-databases.sql` (PostgreSQL)
Cleans all PostgreSQL database data:
```bash
psql -h localhost -U umihealth -d UmiHealth -f cleanup-databases.sql
```

#### `cleanup-redis.sh` (Linux/macOS)
Clears Redis cache:
```bash
chmod +x cleanup-redis.sh
./cleanup-redis.sh
```

#### `cleanup-sqlite.sh` (Linux/macOS)
Removes SQLite database files:
```bash
chmod +x cleanup-sqlite.sh
./cleanup-sqlite.sh
```

#### `cleanup-docker-volumes.sh` (Linux/macOS)
Removes Docker containers and volumes:
```bash
chmod +x cleanup-docker-volumes.sh
./cleanup-docker-volumes.sh
```

## What Gets Cleaned

### Database Cleanup
- ✅ All patient records
- ✅ All prescription data
- ✅ All inventory items
- ✅ All user accounts (except superadmin)
- ✅ All tenant data
- ✅ All audit logs
- ✅ All transaction records
- ✅ All session tokens

### Cache Cleanup
- ✅ Redis all databases
- ✅ Session cache
- ✅ Application cache
- ✅ Temporary cache files

### Docker Cleanup
- ✅ All Umi Health containers
- ✅ All data volumes
- ✅ All networks
- ✅ All images (optional)

### File System Cleanup
- ✅ SQLite database files
- ✅ Log files
- ✅ Temporary files
- ✅ Build artifacts
- ✅ Cache directories

### Browser Cleanup
- ✅ localStorage
- ✅ sessionStorage
- ✅ IndexedDB
- ✅ Browser cache

## Environment Variables

You can configure cleanup behavior with these environment variables:

```bash
# PostgreSQL Configuration
export PGHOST=localhost
export PGPORT=5432
export PGUSER=umihealth
export PGPASSWORD=root
export PGDATABASE=UmiHealth

# Redis Configuration
export REDIS_HOST=localhost
export REDIS_PORT=6379
export REDIS_PASSWORD=
```

## Prerequisites

### System Requirements
- **Linux/macOS**: Bash shell, PostgreSQL client, Redis CLI, Docker
- **Windows**: PowerShell, PostgreSQL client, Redis CLI, Docker

### Database Access
- PostgreSQL must be running and accessible
- Redis must be running (if applicable)
- Docker must be running (for container cleanup)

## Safety Features

### Confirmation Prompts
All scripts require confirmation before proceeding with destructive operations.

### Backup Recommendations
**ALWAYS create a backup before running cleanup:**

```bash
# PostgreSQL backup
pg_dump -h localhost -U umihealth UmiHealth > backup.sql

# Docker volumes backup
docker run --rm -v umihealth_postgres_data:/data -v $(pwd):/backup ubuntu tar czf /backup/postgres_backup.tar.gz -C /data .
```

### Dry Run Mode
Some scripts support dry-run mode to preview what would be deleted:
```bash
./cleanup-docker-volumes.sh --dry-run
```

## Recovery

After cleanup, you can restart the application:

```bash
# Using Docker Compose
docker-compose up -d

# Or start development server
cd UmiHealth.MinimalApi
dotnet run
```

## Troubleshooting

### Connection Issues
If database cleanup fails:
1. Check PostgreSQL is running: `pg_isready`
2. Verify connection string in `appsettings.json`
3. Check network connectivity

### Permission Issues
If scripts fail with permission errors:
```bash
# Linux/macOS
chmod +x *.sh

# Windows PowerShell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Docker Issues
If Docker cleanup fails:
1. Check Docker is running: `docker info`
2. Stop containers manually: `docker stop $(docker ps -q)`
3. Remove volumes manually: `docker volume rm $(docker volume ls -q)`

## Customization

You can modify the cleanup scripts to:
- Preserve specific data
- Add custom cleanup steps
- Change database connection parameters
- Modify file patterns for cleanup

## Support

For issues or questions:
1. Check the troubleshooting section
2. Verify all prerequisites are met
3. Ensure proper permissions
4. Review script logs for error messages

---

**Remember: These scripts are designed for complete data removal. Use with caution and always create backups before running!**
