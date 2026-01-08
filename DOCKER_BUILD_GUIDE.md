# Docker Build Process Guide

## Overview

This document explains the Docker build process for UmiHealth services and the context requirements for successful builds.

## Architecture

### Project Structure
```
backend/src/
├── UmiHealth.API/           # Main API service
├── UmiHealth.Identity/      # Identity/Auth service  
├── UmiHealth.Core/          # Core domain logic
├── UmiHealth.Infrastructure/# Infrastructure layer
├── UmiHealth.Application/  # Application services
├── UmiHealth.Domain/        # Domain entities
├── UmiHealth.Shared/        # Shared utilities
└── UmiHealth.Persistence/   # Data access layer
```

### Build Context Requirements

**Critical**: The Docker build context must be `backend/src` because:

1. **Dockerfile COPY Commands**: All Dockerfiles reference sibling directories:
   ```dockerfile
   COPY ["UmiHealth.Shared/UmiHealth.Shared.csproj", "UmiHealth.Shared/"]
   COPY ["UmiHealth.Core/UmiHealth.Core.csproj", "UmiHealth.Core/"]
   ```

2. **Dependency Resolution**: Each service needs access to multiple project directories during the build process.

3. **Context Visibility**: Docker can only access files within the specified build context.

## CI/CD Build Process

### GitHub Actions Workflow

The build process uses explicit `docker buildx` commands instead of the Docker build action to avoid path resolution issues:

```yaml
- name: Build and push ${{ matrix.service }} image
  run: |
    if [ "${{ matrix.service }}" = "API" ]; then
      docker buildx build \
        --file backend/src/UmiHealth.API/Dockerfile \
        --tag ghcr.io/mmushibi/umihealth-api:latest \
        --platform linux/amd64 \
        --push \
        backend/src
    elif [ "${{ matrix.service }}" = "Identity" ]; then
      docker buildx build \
        --file backend/src/UmiHealth.Identity/Dockerfile \
        --tag ghcr.io/mmushibi/umihealth-identity:latest \
        --platform linux/amd64 \
        --push \
        backend/src
    fi
```

### Why This Approach Works

1. **Explicit Paths**: No matrix variable interpolation issues
2. **Correct Context**: `backend/src` provides access to all project directories
3. **Proper Syntax**: Valid `docker buildx build` command structure
4. **Full Control**: No abstraction layers causing confusion

## Local Development

### Building Services Locally

To build services locally, use the same pattern as the CI/CD:

```bash
# Build API service
docker buildx build \
  --file backend/src/UmiHealth.API/Dockerfile \
  --tag umihealth-api:latest \
  backend/src

# Build Identity service  
docker buildx build \
  --file backend/src/UmiHealth.Identity/Dockerfile \
  --tag umihealth-identity:latest \
  backend/src
```

### Testing Containers

```bash
# Run API service
docker run -p 8080:8080 umihealth-api:latest

# Run Identity service
docker run -p 8081:8080 umihealth-identity:latest
```

## Docker Build Optimization

### .dockerignore Files

Each service has a `.dockerignore` file to:

1. **Exclude Build Artifacts**: `bin/`, `obj/`, `*.dll`, `*.pdb`
2. **Exclude Development Files**: `.vscode/`, `.idea/`, `*.user`
3. **Exclude Unnecessary Services**: Each service only includes its dependencies
4. **Reduce Context Size**: Faster build times and smaller images

### Multi-Stage Builds

All Dockerfiles use multi-stage builds:

1. **Build Stage**: Compiles the .NET application
2. **Publish Stage**: Creates optimized runtime image
3. **Final Stage**: Minimal runtime image with only necessary files

## Common Issues and Solutions

### Issue: "file not found" errors during Docker build
**Cause**: Build context doesn't include all required project directories
**Solution**: Ensure build context is `backend/src`

### Issue: Path resolution failures in CI/CD
**Cause**: Matrix variable interpolation or Docker build action issues
**Solution**: Use explicit `docker buildx build` commands

### Issue: Slow build times
**Cause**: Large build context including unnecessary files
**Solution**: Use `.dockerignore` files to exclude unused files

## Future Considerations

### If Adding More Services

1. **Maintain Current Structure**: Keep all services in `backend/src/`
2. **Update CI/CD Matrix**: Add new service to the matrix strategy
3. **Create Service-Specific .dockerignore**: Exclude unrelated services
4. **Document Dependencies**: Clearly state which projects each service depends on

### Potential Restructuring Options

If the number of services grows significantly, consider:

1. **Microservice Pattern**: Each service in its own repository
2. **Monorepo with Subfolders**: Services in separate subdirectories with local dependencies
3. **Solution-Level Dockerfile**: Single Dockerfile building entire solution

## Environment Variables

### Required for Production

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `JwtSettings__Secret`, `JwtSettings__Issuer`, `JwtSettings__Audience`
- `Redis__ConnectionString`

### Service-Specific Variables

**API Service**:
- `IdentityService__Url`

**Identity Service**:
- `EmailSettings__SmtpServer`, `EmailSettings__SmtpPort`, `EmailSettings__Username`, `EmailSettings__Password`

## Troubleshooting

### Debug Build Issues

1. **Check File Structure**: Verify all required directories exist in `backend/src/`
2. **Validate Dockerfile Paths**: Ensure COPY commands match actual directory structure
3. **Test Locally**: Use same commands as CI/CD to reproduce issues
4. **Check .dockerignore**: Ensure required files aren't being excluded

### Performance Optimization

1. **Monitor Build Context Size**: Use `docker buildx du` to check context size
2. **Optimize Layer Caching**: Order COPY commands from least to most frequently changed
3. **Use .dockerignore**: Exclude unnecessary files and directories

## Support

For Docker build issues:
1. Check this guide first
2. Verify the build context is correct
3. Ensure all required project directories exist
4. Check .dockerignore files aren't excluding needed files
