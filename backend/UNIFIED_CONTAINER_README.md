# UmiHealth Unified Docker Container

This setup combines the .NET API and PostgreSQL database into a single Docker container for simplified deployment and development.

## Files Created

- `Dockerfile.unified` - Multi-stage Dockerfile that builds both PostgreSQL and .NET API
- `docker-compose.unified.yml` - Simplified compose file using the unified container
- `docker-compose.unified.override.yml` - Development overrides with additional tools
- `docker/startup.sh` - Startup script that initializes both services
- `docker/init-db.sh` - Database initialization script
- `docker/health-check.sh` - Combined health check for both services

## Usage

### Production
```bash
docker-compose -f docker-compose.unified.yml up -d
```

### Development
```bash
docker-compose -f docker-compose.unified.yml -f docker-compose.unified.override.yml up -d
```

### Building the unified container
```bash
docker build -f Dockerfile.unified -t umihealth/unified:latest .
```

## Architecture

The unified container runs:
- **PostgreSQL** on port 5432 (internal)
- **.NET API** on port 8080 (internal)
- **Startup script** that initializes PostgreSQL, then starts the API
- **Health check** that monitors both services

## Port Mappings

- **5432** → PostgreSQL (external)
- **8080** → API (external)
- **8081** → API Debug (development only)
- **5050** → PgAdmin (development only)
- **8082** → Redis Commander (development only)

## Benefits

1. **Simplified deployment** - Single container for API + database
2. **Faster startup** - No network latency between API and database
3. **Easier local development** - One command to start everything
4. **Reduced resource usage** - Shared container resources

## Trade-offs

1. **Larger image size** - Includes both PostgreSQL and .NET runtime
2. **Less scalable** - Can't scale API and database independently
3. **Production considerations** - Not suitable for high-traffic production scenarios

## Environment Variables

Key environment variables for the unified container:

```yaml
# PostgreSQL (password should be provided via .env file)
POSTGRES_DB=UmiHealth
POSTGRES_USER=umihealth
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}

# API (connection string uses runtime password)
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=UmiHealth;Username=umihealth;Password=${POSTGRES_PASSWORD}
```

**Security Note:** Sensitive data like passwords and secrets are no longer hardcoded in the Dockerfile. They should be provided via environment variables or a `.env` file:

```bash
# .env file
POSTGRES_PASSWORD=your_secure_password_here
REDIS_PASSWORD=your_redis_password_here
JWT_SECRET=your_jwt_secret_here
```

## Health Checks

The container includes a comprehensive health check that verifies:
- PostgreSQL is responding on port 5432
- API is responding on port 8080

Both services must be healthy for the container to be considered healthy.

## Development Tools

When using the override file, you get:
- **PgAdmin** - PostgreSQL administration interface
- **Redis Commander** - Redis management interface
- **Debug ports** - Additional ports for API debugging

## Migration from Separate Containers

To migrate from the existing setup:
1. Backup existing PostgreSQL data
2. Update connection strings to use `localhost` instead of `postgres`
3. Update any service discovery to use the unified container name
4. Test all integrations thoroughly

## Troubleshooting

### Container fails to start
- Check the container logs: `docker logs umihealth-unified`
- Verify PostgreSQL initialization completed successfully
- Ensure API can connect to localhost:5432

### Health checks failing
- Verify both services are running: `docker exec umihealth-unified ps aux`
- Test PostgreSQL: `docker exec umihealth-unified pg_isready -h localhost -p 5432 -U umihealth`
- Test API: `docker exec umihealth-unified curl http://localhost:8080/health`

### Database connection issues
- Ensure PostgreSQL is fully started before API attempts to connect
- Check that the connection string uses `localhost` not `postgres`
- Verify database name and credentials match environment variables
