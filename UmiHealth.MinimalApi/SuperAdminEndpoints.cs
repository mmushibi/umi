using UmiHealth.MinimalApi.Services;

/// <summary>
/// Super-admin tenant management endpoints.
/// All endpoints in this section require "superadmin" or "operations" role in production.
/// TODO: Add authorization middleware to enforce role checks and audit all calls.
/// </summary>

public static class SuperAdminEndpoints
{
    public static void RegisterSuperAdminEndpoints(WebApplication app)
    {
        // Super-admin: List all tenants (cross-tenant)
        app.MapGet("/api/v1/superadmin/tenants", (Dictionary<string, object> tenantsDb, IAuditService auditService) =>
        {
            // TODO: Check user role is super-admin or operations
            auditService.LogSuperAdminAction("super-admin-user", null, "LIST_TENANTS", "tenants");
            var tenants = tenantsDb.Values.ToList();
            return Results.Ok(new {
                success = true,
                data = tenants,
                total = tenants.Count
            });
        });

// Super-admin: Get single tenant
        app.MapGet("/api/v1/superadmin/tenants/{tenantId}", (string tenantId, Dictionary<string, object> tenantsDb, IAuditService auditService) =>
        {
            // TODO: Check user role is super-admin or operations
            if (!tenantsDb.ContainsKey(tenantId))
            {
                auditService.LogSuperAdminAction("super-admin-user", tenantId, "GET_TENANT", "tenants", new Dictionary<string, object> { { "status", "NOT_FOUND" } });
                return Results.NotFound(new { success = false, message = "Tenant not found" });
            }
            auditService.LogSuperAdminAction("super-admin-user", tenantId, "GET_TENANT", "tenants");
            return Results.Ok(new { success = true, data = tenantsDb[tenantId] });
        });

        // Super-admin: Create tenant
        app.MapPost("/api/v1/superadmin/tenants", async (HttpRequest request, Dictionary<string, object> tenantsDb, IAuditService auditService) =>
        {
            try
            {
                // TODO: Check user role is super-admin or operations
                var tenantData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
                if (tenantData == null || !tenantData.ContainsKey("name"))
                {
                    return Results.BadRequest(new { success = false, message = "Tenant name is required" });
                }

                var tenantId = Guid.NewGuid().ToString();
                var tenant = new {
                    id = tenantId,
                    name = tenantData["name"],
                    email = tenantData.ContainsKey("email") ? tenantData["email"] : "",
                    status = tenantData.ContainsKey("status") ? tenantData["status"] : "active",
                    subscriptionPlan = tenantData.ContainsKey("subscriptionPlan") ? tenantData["subscriptionPlan"] : "Care",
                    createdAt = DateTime.UtcNow
                };
                tenantsDb[tenantId] = tenant;
                auditService.LogSuperAdminAction("super-admin-user", tenantId, "CREATE_TENANT", "tenants", new Dictionary<string, object> { { "name", tenantData["name"] } });
                return Results.Ok(new { success = true, data = tenant, message = "Tenant created" });
            }
            catch (Exception ex)
            {
                auditService.LogSuperAdminAction("super-admin-user", null, "CREATE_TENANT", "tenants", new Dictionary<string, object> { { "error", ex.Message } });
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        });

        // Super-admin: Update tenant (e.g., plan, status)
        app.MapPut("/api/v1/superadmin/tenants/{tenantId}", async (string tenantId, HttpRequest request, Dictionary<string, object> tenantsDb, IAuditService auditService) =>
        {
            try
            {
                // TODO: Check user role is super-admin or operations
                if (!tenantsDb.ContainsKey(tenantId))
                {
                    return Results.NotFound(new { success = false, message = "Tenant not found" });
                }

                var tenantData = await request.ReadFromJsonAsync<Dictionary<string, object>>();
                if (tenantData == null)
                {
                    return Results.BadRequest(new { success = false, message = "Invalid tenant data" });
                }

                var existing = (dynamic)tenantsDb[tenantId];
                var updated = new {
                    id = tenantId,
                    name = tenantData.ContainsKey("name") ? tenantData["name"] : existing.name,
                    email = tenantData.ContainsKey("email") ? tenantData["email"] : existing.email,
                    status = tenantData.ContainsKey("status") ? tenantData["status"] : existing.status,
                    subscriptionPlan = tenantData.ContainsKey("subscriptionPlan") ? tenantData["subscriptionPlan"] : existing.subscriptionPlan,
                    createdAt = existing.createdAt,
                    updatedAt = DateTime.UtcNow
                };
                tenantsDb[tenantId] = updated;
                auditService.LogSuperAdminAction("super-admin-user", tenantId, "UPDATE_TENANT", "tenants", tenantData);
                return Results.Ok(new { success = true, data = updated, message = "Tenant updated" });
            }
            catch (Exception ex)
            {
                auditService.LogSuperAdminAction("super-admin-user", tenantId, "UPDATE_TENANT", "tenants", new Dictionary<string, object> { { "error", ex.Message } });
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        });

        // Super-admin: Get all users across all tenants
        app.MapGet("/api/v1/superadmin/users", (Dictionary<string, object> usersDb, IAuditService auditService) =>
        {
            // TODO: Check user role is super-admin or operations
            auditService.LogSuperAdminAction("super-admin-user", null, "LIST_ALL_USERS", "users");
            var users = usersDb.Values.ToList();
            return Results.Ok(new { success = true, data = users, total = users.Count });
        });

        // Super-admin: Get users for a specific tenant
        app.MapGet("/api/v1/superadmin/tenants/{tenantId}/users", (string tenantId, Dictionary<string, object> usersDb, IAuditService auditService) =>
        {
            // TODO: Check user role is super-admin or operations
            var tenantUsers = usersDb.Values.Where(u => {
                var user = (dynamic)u;
                return user.tenantId?.ToString() == tenantId;
            }).ToList();
            auditService.LogSuperAdminAction("super-admin-user", tenantId, "LIST_TENANT_USERS", "users");
            return Results.Ok(new { success = true, data = tenantUsers, total = tenantUsers.Count });
        });

        // Super-admin: Force disable/enable user
        app.MapPut("/api/v1/superadmin/users/{userId}/status", async (string userId, HttpRequest request, Dictionary<string, object> usersDb, IAuditService auditService) =>
        {
            try
            {
                // TODO: Check user role is super-admin or operations
                if (!usersDb.ContainsKey(userId))
                {
                    return Results.NotFound(new { success = false, message = "User not found" });
                }

                var statusData = await request.ReadFromJsonAsync<Dictionary<string, string>>();
                var newStatus = statusData?.ContainsKey("status") == true ? statusData["status"] : "active";

                var existing = (dynamic)usersDb[userId];
                var updated = new {
                    id = userId,
                    username = existing.username,
                    email = existing.email,
                    password = existing.password,
                    role = existing.role,
                    status = newStatus,
                    tenantId = existing.tenantId,
                    createdAt = existing.createdAt,
                    updatedAt = DateTime.UtcNow
                };
                usersDb[userId] = updated;
                auditService.LogSuperAdminAction("super-admin-user", existing.tenantId?.ToString(), "UPDATE_USER_STATUS", "users", new Dictionary<string, object> { { "userId", userId }, { "newStatus", newStatus } });
                return Results.Ok(new { success = true, data = updated, message = "User status updated" });
            }
            catch (Exception ex)
            {
                auditService.LogSuperAdminAction("super-admin-user", null, "UPDATE_USER_STATUS", "users", new Dictionary<string, object> { { "error", ex.Message } });
                return Results.BadRequest(new { success = false, message = ex.Message });
            }
        });

        // Super-admin: Get audit logs
        app.MapGet("/api/v1/superadmin/audit", (IAuditService auditService) =>
        {
            // TODO: Check user role is super-admin or operations
            // In production, query audit_logs table from database
            var auditLog = ((AuditService)auditService).GetAuditLog();
            return Results.Ok(new { success = true, data = auditLog.ToList(), total = auditLog.Count });
        });
    }
}
