# Session Management & Auto-Logout Implementation Guide

**Date**: January 7, 2026  
**Feature**: 30-minute inactivity auto-logout with server-side token blacklist

## Overview

This implementation provides comprehensive session management for the UmiHealth platform with:
- ✅ 30-minute inactivity auto-logout (client-side detection)
- ✅ Server-side token blacklist mechanism
- ✅ Cross-tab/window logout synchronization
- ✅ Secure token validation on every API request
- ✅ Refresh token revocation on logout

---

## Architecture

### Client-Side (Frontend)

1. **Activity Monitoring**: Tracks user interactions (click, mousemove, keydown, scroll, touchstart)
2. **Local Storage Sync**: Uses `umi_last_activity` timestamp to sync across browser tabs
3. **Inactivity Timer**: Triggers auto-logout after 30 minutes of inactivity
4. **Server Notification**: Calls `/auth/logout` endpoint to revoke tokens on the server

### Server-Side (Backend)

1. **Auth Controller**: New `/api/v1/auth/*` endpoints for login, logout, token refresh, validation
2. **Token Blacklist**: Database table (`BlacklistedTokens`) stores revoked tokens
3. **TokenBlacklistService**: Manages blacklist operations and cleanup
4. **Validation**: Every protected endpoint checks token validity and blacklist status

---

## Implementation Details

### 1. Client-Side Session Tracking

**Files Modified**:
- `js/auth-manager.js`
- `js/api-client.js`
- `portals/pharmacist/js/auth-manager.js`
- `portals/pharmacist/js/auth-service.js`

**Mechanism**:
```javascript
// Activity monitoring (automatic on all interactions)
['click','mousemove','keydown','scroll','touchstart'].forEach(evt => {
    window.addEventListener(evt, () => {
        localStorage.setItem('umi_last_activity', Date.now());
    }, {passive:true});
});

// Inactivity timeout check (default 30 minutes)
const timeoutMs = 30 * 60 * 1000; // 30 minutes
setTimeout(() => {
    if (Date.now() - lastActivityTime >= timeoutMs) {
        // Call logout endpoint and redirect to signin
    }
}, timeoutMs);

// Cross-tab sync via storage events
window.addEventListener('storage', (e) => {
    if (e.key === 'umi_logged_out') {
        // Another tab logged out, logout here too
        window.location.href = '/auth/signin.html';
    }
});
```

**Configuration**:
```javascript
// Customize timeout (in minutes)
localStorage.setItem('sessionTimeoutMinutes', 30);
```

### 2. Server-Side Auth Endpoints

**New Controller**: `backend/src/UmiHealth.Api/Controllers/AuthController.cs`

**Endpoints**:

#### POST `/api/v1/auth/login`
Request:
```json
{
  "email": "user@example.com",
  "password": "password123",
  "tenantSubdomain": "optional-tenant"
}
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ0eXAiOiJKV1QiLCJhbGc...",
    "refreshToken": "uuid-uuid-uuid",
    "user": { "id": "...", "email": "...", "roles": [...] }
  }
}
```

#### POST `/api/v1/auth/logout`
Request:
```json
{
  "refreshToken": "uuid-uuid-uuid"
}
```

Response:
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

**Actions**:
- ✅ Revokes refresh token in database (sets `IsRevoked=true`)
- ✅ Adds access token to blacklist
- ✅ Logs the action

#### POST `/api/v1/auth/refresh`
Request:
```json
{
  "accessToken": "expired.token.here",
  "refreshToken": "uuid-uuid-uuid"
}
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "new.access.token",
    "refreshToken": "new.refresh.token"
  }
}
```

**Validation**:
- ✅ Checks if refresh token is blacklisted
- ✅ Validates token signature and expiry
- ✅ Issues new token pair

#### POST `/api/v1/auth/validate`
Request:
```json
{
  "token": "token.to.validate"
}
```

Response:
```json
{
  "success": true,
  "isValid": true,
  "message": "Token is valid"
}
```

#### POST `/api/v1/auth/force-logout/{userId}` (Admin Only)
Blacklists all active tokens for a user.

### 3. Token Blacklist Service

**Interface**: `backend/src/UmiHealth.Identity/ITokenBlacklistService.cs`

**Implementation**: `backend/src/UmiHealth.Identity/Services/TokenBlacklistService.cs`

**Methods**:
```csharp
// Check if token is blacklisted
Task<bool> IsTokenBlacklistedAsync(string token);

// Add token to blacklist
Task BlacklistTokenAsync(string token, string reason);

// Blacklist all tokens for a user (force logout)
Task BlacklistUserTokensAsync(Guid userId, string reason);

// Clean up expired entries
Task CleanupExpiredTokensAsync();
```

**Security Features**:
- ✅ Tokens are hashed (SHA256) before storage to avoid plaintext secrets in DB
- ✅ Expired tokens are auto-cleaned by `CleanupExpiredTokensAsync`
- ✅ Blacklist check is performed on token validation

### 4. Database Schema

**Existing Entity**: `BlacklistedToken`
```csharp
public class BlacklistedToken : BaseEntity
{
    public string Token { get; set; }           // SHA256 hash of token
    public DateTime ExpiresAt { get; set; }    // When token naturally expires
    public string? Reason { get; set; }        // Why it was blacklisted
}
```

**Existing Entity**: `RefreshToken`
```csharp
public class RefreshToken : TenantEntity
{
    public string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }        // ← Used by logout
    public DateTime? IssuedAt { get; set; }
    public string? JwtTokenId { get; set; }
}
```

---

## Configuration

### Development Setup

**Environment Variables** (set before running backend):

```bash
# Development JWT secret (provided)
Jwt__Key="_-e%(@}wO.D%o*%q.#1@J;?$Lu5=r{?)"
Jwt__ExpiryMinutes=60
Jwt__Issuer=UmiHealth
Jwt__Audience=UmiHealthUsers

# Database connection
ConnectionStrings__DefaultConnection="Host=localhost;Database=umihealth;Username=postgres;Password=root"

# Refresh token expiry (hours)
Jwt__RefreshTokenExpirationHours=24
```

**PowerShell**:
```powershell
$env:Jwt__Key = "_-e%(@}wO.D%o*%q.#1@J;?$Lu5=r{?)"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=umihealth;Username=postgres;Password=root"
```

### Production Setup

**Environment Variables**:
```bash
Jwt__Key="RYS^7$dc^$x:d3RNnSLN|%Y9KRrVXS+|.kEATH_z#M_z7p=;^XHy#a1xu]J_VWS["
Jwt__ExpiryMinutes=30
ConnectionStrings__DefaultConnection="<production-postgres-connection>"
```

**appsettings.Production.json**:
```json
{
  "Jwt": {
    "Key": "RYS^7$dc^$x:d3RNnSLN|%Y9KRrVXS+|.kEATH_z#M_z7p=;^XHy#a1xu]J_VWS[",
    "ExpiryMinutes": 30,
    "Issuer": "UmiHealth",
    "Audience": "UmiHealthUsers",
    "RefreshTokenExpirationHours": 24
  }
}
```

---

## Client Usage

### Login
```javascript
const response = await authManager.login(email, password, tenantSubdomain);
if (response.success) {
  console.log('Logged in as:', response.user);
}
```

### Logout
```javascript
const result = await authManager.logout();
// Tokens cleared, user redirected to signin
```

### Session Timeout Customization
```javascript
// Change timeout to 60 minutes (for dev/testing)
localStorage.setItem('sessionTimeoutMinutes', 60);
localStorage.setItem('umi_last_activity', Date.now()); // Reset timer
```

### Token Validation
```javascript
const isValid = await apiClient.request('/auth/validate', {
  method: 'POST',
  body: JSON.stringify({ token: accessToken })
});
```

---

## Testing

### Manual Testing Checklist

1. **Login / Logout**
   - [ ] User can login with valid credentials
   - [ ] Access token is stored in localStorage
   - [ ] Logout endpoint called successfully
   - [ ] Tokens cleared from localStorage

2. **Session Timeout (30 min)**
   - [ ] No activity for 30 minutes → auto-logout
   - [ ] Activity resets the timer
   - [ ] Toast/notification shows before logout
   - [ ] Redirect to signin page

3. **Cross-Tab Synchronization**
   - [ ] Login in Tab A
   - [ ] Open Tab B → both show logged in
   - [ ] Logout in Tab A
   - [ ] Tab B auto-logs out

4. **Token Blacklist**
   - [ ] Logout revokes refresh token
   - [ ] Attempt to refresh with revoked token → fails
   - [ ] Old access token becomes invalid

5. **Admin Force Logout**
   - [ ] Admin calls force-logout on user
   - [ ] User's all sessions immediately invalidated
   - [ ] User is redirected to signin

### Postman Test Collection

```bash
# Login
POST /api/v1/auth/login
Body: { "email": "test@example.com", "password": "test123" }

# Validate Token
POST /api/v1/auth/validate
Body: { "token": "<access-token>" }

# Logout
POST /api/v1/auth/logout
Body: { "refreshToken": "<refresh-token>" }

# Refresh Token
POST /api/v1/auth/refresh
Body: { "accessToken": "<expired-token>", "refreshToken": "<refresh-token>" }

# Get Profile
GET /api/v1/auth/me
Header: Authorization: Bearer <access-token>
```

---

## Security Considerations

### ✅ Implemented

1. **Token Hashing**: Tokens are SHA256 hashed before storage
2. **Blacklist Cleanup**: Expired entries automatically removed
3. **Revocation Check**: Every request validates token is not blacklisted
4. **HTTPS Enforced**: Backend redirects HTTP to HTTPS
5. **CORS Managed**: Origin policies enforced
6. **JWT Validation**: Signature, issuer, audience verified

### ⚠️ Additional Recommendations

1. **HTTP-Only Cookies** (future enhancement)
   - Store refresh tokens in HTTP-only cookies
   - Prevents XSS token theft

2. **Refresh Token Rotation**
   - Issue new refresh token on every refresh
   - Invalidate old refresh token

3. **Rate Limiting**
   - Limit login attempts to prevent brute force
   - Implement account lockout after N failures

4. **Audit Logging**
   - Log all login/logout events
   - Track token blacklist additions

---

## Troubleshooting

### Issue: Session not timing out
**Solution**:
```javascript
// Check if activity is being tracked
console.log(localStorage.getItem('umi_last_activity'));

// Verify timeout setting
console.log(localStorage.getItem('sessionTimeoutMinutes'));

// Manually trigger logout
localStorage.setItem('umi_logged_out', Date.now());
```

### Issue: Token still valid after logout
**Solution**:
- Verify `/auth/logout` endpoint called successfully
- Check `BlacklistedTokens` table contains the token hash
- Ensure token validation checks blacklist

### Issue: Cross-tab logout not working
**Solution**:
```javascript
// Check storage event listener is registered
window.addEventListener('storage', (e) => {
  console.log('Storage event:', e.key, e.newValue);
});

// Test manually
localStorage.setItem('umi_logged_out', Date.now());
```

---

## Files Modified/Created

### Backend
- ✅ Created: `Controllers/AuthController.cs` - Auth endpoints
- ✅ Created: `Identity/ITokenBlacklistService.cs` - Interface
- ✅ Created: `Identity/Services/TokenBlacklistService.cs` - Implementation
- ✅ Modified: `AuthenticationService.cs` - Added GetProfileAsync method
- ✅ Modified: `Infrastructure/DependencyInjection.cs` - Registered service

### Frontend
- ✅ Modified: `js/auth-manager.js` - Added inactivity logout
- ✅ Modified: `js/api-client.js` - Updated logout to call server
- ✅ Modified: `portals/pharmacist/js/auth-manager.js` - Added inactivity logout
- ✅ Modified: `portals/pharmacist/js/auth-service.js` - Added inactivity logout

### Database
- ✅ Existing: `BlacklistedToken` entity (already in schema)
- ✅ Existing: `RefreshToken` entity (already in schema)

---

## Deployment

### Step 1: Backend Deployment
```bash
cd backend
dotnet build
dotnet publish -c Release
# Set environment variables before running
export Jwt__Key="<production-secret>"
export ConnectionStrings__DefaultConnection="<prod-connection>"
dotnet UmiHealth.API.dll
```

### Step 2: Frontend Deployment
- No build required (JavaScript changes are ready)
- Ensure API_BASE points to correct backend URL
- Session timeout defaults to 30 minutes

### Step 3: Database
- Existing migrations support `BlacklistedToken` table
- Run migrations if deploying fresh: `dotnet ef database update`

---

## Performance Impact

- **Minimal**: Blacklist checks use indexed queries
- **Storage**: ~1KB per blacklisted token
- **Cleanup**: Runs at-demand, can be scheduled nightly

---

## Support & Questions

For issues or questions about this implementation:
1. Check the **Troubleshooting** section
2. Review the **Architecture** section for overview
3. Test with the provided **Postman Collection**

