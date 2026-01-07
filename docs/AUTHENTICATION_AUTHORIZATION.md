# Umi Health Authentication & Authorization System

This document describes the comprehensive authentication and authorization system implemented for Umi Health Pharmacy Management System using modern .NET technologies and JWT-based security.

## Overview

The system implements JWT-based authentication with secure token handling, role-based access control (RBAC), multi-tenant security, and branch-level access control for pharmacy management operations.

## 1. Technology Stack

### 1.1 Authentication Framework
- **.NET 8.0 Identity**: User management and authentication
- **JWT Bearer**: Token-based authentication
- **ASP.NET Core Authorization**: Policy-based authorization
- **Redis**: Token revocation and session management

### 1.2 Security Features
- **Multi-Factor Authentication**: Optional 2FA support
- **Rate Limiting**: API protection against brute force
- **Password Policies**: Strong password requirements
- **Audit Logging**: Complete authentication trail

## 2. JWT Token Strategy

### 2.1 Access Token

- **Expiry**: 15 minutes
- **Algorithm**: HS256 (symmetric encryption for microservices)
- **Claims**: User ID, Email, Name, Role, Tenant ID, Branch ID, Permissions
- **Usage**: API requests, session management

### 2.2 Refresh Token

- **Expiry**: 7 days
- **Algorithm**: HS256
- **Claims**: User ID, Tenant ID, Token Type (refresh)
- **Storage**: HTTP-only cookies
- **Usage**: Token renewal without re-authentication

### 2.3 Token Format

```json
{
  "alg": "HS256",
  "typ": "JWT"
}
{
  "nameid": "user-guid",
  "email": "user@example.com",
  "unique_name": "User Name",
  "role": "pharmacist",
  "tenant_id": "tenant-guid",
  "branch_id": "branch-guid",
  "username": "username",
  "permissions": "{}",
  "branch_access": "[]",
  "permission": ["inventory:read", "prescriptions:*"],
  "exp": 1640995200,
  "iss": "UmiHealthApi",
  "aud": "UmiHealthApi"
}
```

## 3. Implementation Details

### 3.1 JWT Service Configuration

```csharp
// UmiHealth.Identity/Services/JwtService.cs
public class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;

    public string GenerateToken(ApplicationUser user, string tenantId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim("role", user.Role),
            new Claim("tenant_id", tenantId),
            new Claim("branch_id", user.BranchId?.ToString() ?? ""),
            new Claim("username", user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_jwtOptions.AccessTokenExpiration),
            signingCredentials: new SigningCredentials(
                _jwtOptions.SigningKey, 
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 3.2 Authentication Middleware

```csharp
// UmiHealth.API/Middleware/TenantAuthenticationMiddleware.cs
public class TenantAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantProvider _tenantProvider;

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtOptions.Secret);
                
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var tenantId = jwtToken.Claims.FirstOrDefault(x => x.Type == "tenant_id")?.Value;
                
                if (tenantId != null)
                {
                    _tenantProvider.SetCurrentTenant(tenantId);
                }
            }
            catch
            {
                // Token validation failed
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }

        await _next(context);
    }
}
```

## 4. Role-Based Access Control

### 4.1 Permission Matrix

| Role | Permissions |
|------|-------------|
| **Super Admin** | `system:*`, `tenant:*`, `user:*`, `inventory:*`, `reports:*`, `pos:*`, `prescriptions:*`, `patients:*`, `subscriptions:*`, `branches:*`, `operations:*` |
| **Admin** | `tenant:manage`, `tenant:read`, `user:*`, `inventory:*`, `reports:*`, `pos:*`, `prescriptions:*`, `patients:*`, `branches:*` |
| **Pharmacist** | `patients:*`, `prescriptions:*`, `inventory:read`, `inventory:write`, `reports:read`, `pos:read`, `branches:read` |
| **Cashier** | `pos:*`, `patients:read`, `inventory:read`, `reports:sales`, `branches:read` |
| **Operations** | `tenant:create`, `tenant:read`, `subscriptions:*`, `system:monitor`, `reports:*`, `branches:read` |

### Permission Format

- **Resource**:Action** (e.g., `inventory:read`, `user:create`)
- **Wildcard**: `*` for all actions (e.g., `inventory:*`)
- **System-wide**: `system:*` for complete access

## 5.3 Branch-Level Access Control

### Access Rules

- **Super Admin**: Can access all branches system-wide
- **Admin**: Can access all branches within their tenant
- **Other Roles**: Can only access explicitly assigned branches

### Cross-Branch Access

- Requires explicit `branches:cross_access` permission
- Automatically granted to Super Admin and Admin roles
- Enables users to view/manage data across multiple branches

### Data Filtering

- All queries automatically filtered by accessible branches
- Middleware injects branch context into HTTP requests
- Controllers can access branch information via `HttpContext.Items`

## Implementation Details

### Services

#### JwtTokenService

- Generates and validates JWT tokens using RS256
- Manages token expiry and claims
- Handles asymmetric key generation/loading

#### AuthorizationService

- Role-based permission checking
- Permission matrix evaluation
- Cross-branch access validation

#### BranchAccessService

- Branch access control logic
- Data filtering by branch
- User-branch relationship management

### Attributes

#### `[RequirePermission("resource:action")]`

- Enforces specific permissions on controllers/actions
- Example: `[RequirePermission("inventory:write")]`

#### `[RequireBranchAccess]`

- Ensures user can access requested branch
- Automatically extracts branch ID from route/query parameters
- Example: `[RequireBranchAccess("branchId")]`

### Policies

#### Role-Based Policies

- `SuperAdmin`, `Admin`, `Pharmacist`, `Cashier`, `Operations`
- Applied using `[Authorize(Policy = "Admin")]`

#### Permission-Based Policies

- `SystemAccess`, `TenantManagement`, `UserManagement`, etc.
- Applied using `[Authorize(Policy = "InventoryManagement")]`

### Middleware

#### BranchFilterMiddleware

- Automatically injects branch context into requests
- Filters data queries by accessible branches
- Skips authentication endpoints

## Configuration

### JWT Settings

```json
{
  "Jwt": {
    "Issuer": "UmiHealthApi",
    "Audience": "UmiHealthApi",
    "PrivateKeyPem": "-----BEGIN RSA PRIVATE KEY-----\n...",
    "PublicKeyPem": "-----BEGIN PUBLIC KEY-----\n..."
  }
}
```

### Key Generation (Development)

```bash
# Generate RSA key pair
openssl genrsa -out private.pem 2048
openssl rsa -in private.pem -pubout -out public.pem
```

### Key Storage (Production)

- Store private keys in secure vault (Azure Key Vault, AWS KMS)
- Use environment variables for key paths
- Implement key rotation strategy

## Usage Examples

### Controller with Authorization

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    [HttpGet]
    [RequirePermission("inventory:read")]
    [RequireBranchAccess]
    public async Task<ActionResult> GetInventory()
    {
        // Automatically filtered by user's accessible branches
        var branchId = HttpContext.Items["CurrentBranchId"];
        // ...
    }

    [HttpPost]
    [RequirePermission("inventory:write")]
    [RequireBranchAccess]
    public async Task<ActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
        // User must have write permission and branch access
        // ...
    }
}
```

### Service with Branch Filtering

```csharp
public class InventoryService
{
    public async Task<List<InventoryItem>> GetItemsAsync(ClaimsPrincipal user)
    {
        var query = _context.InventoryItems.AsQueryable();
        
        // Automatically filter by accessible branches
        var filteredQuery = await _branchAccessService
            .FilterByBranchAsync(user, query);
            
        return await filteredQuery.ToListAsync();
    }
}
```

### Client-Side Token Management

```javascript
// Store tokens securely
localStorage.setItem('umi_access_token', accessToken);
// Store refresh token in HTTP-only cookie (recommended)

// Auto-refresh before expiry
setInterval(async () => {
    const response = await fetch('/api/v1/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: getRefreshToken() })
    });
    
    if (response.ok) {
        const { accessToken } = await response.json();
        localStorage.setItem('umi_access_token', accessToken);
    }
}, 14 * 60 * 1000); // 14 minutes
```

## Security Considerations

### Token Security

- RS256 provides asymmetric encryption security
- Short access token expiry (15 minutes) limits exposure
- Refresh tokens stored securely (HTTP-only cookies)
- Token validation includes signature, expiry, and issuer

### Branch Access Control

- Automatic data filtering prevents accidental data exposure
- Explicit permission required for cross-branch access
- Role-based access limits available actions

### Key Management

- RSA keys generated with 2048-bit strength
- Private keys never exposed in client code
- Support for key rotation without service interruption

## Testing

### Unit Tests

- Token generation and validation
- Permission matrix evaluation
- Branch access control logic
- Role-based authorization

### Integration Tests

- End-to-end authentication flow
- Authorization attribute behavior
- Middleware functionality
- Cross-branch access scenarios

## Migration Guide

### From Existing Authentication

1. Update `Program.cs` to register new services
2. Replace `[Authorize]` with specific policies/permissions
3. Add branch filtering to data queries
4. Update client-side token handling
5. Generate and configure RSA keys

### Configuration Updates

```csharp
// Program.cs
builder.Services.AddUmiAuthentication(builder.Configuration);
builder.Services.AddUmiAuthorizationPolicies();

var app = builder.Build();
app.UseUmiAuthentication();
```

## Troubleshooting

### Common Issues

- **401 Unauthorized**: Check token expiry and format
- **403 Forbidden**: Verify user permissions and branch access
- **Key Loading**: Ensure RSA keys are properly formatted
- **Branch Filtering**: Check user's assigned branches

### Debug Information

- Enable detailed JWT logging in development
- Check `HttpContext.Items` for branch context
- Validate token claims using JWT debugger tools
- Review permission matrix for role assignments

## Future Enhancements

### Planned Features

- Multi-factor authentication (MFA)
- Biometric authentication support
- Advanced audit logging
- Real-time permission updates
- API key authentication for integrations

### Scalability

- Distributed token validation
- Caching for permission checks
- Optimized branch filtering queries
- Load balancer-friendly session management
