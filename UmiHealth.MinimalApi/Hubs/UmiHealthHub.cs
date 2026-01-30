using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace UmiHealth.MinimalApi.Hubs
{
    public class UmiHealthHub : Hub
    {
        private readonly ILogger<UmiHealthHub> _logger;

        public UmiHealthHub(ILogger<UmiHealthHub> logger)
        {
            _logger = logger;
        }

        // Connect user to their tenant-specific group
        public async Task JoinTenantGroup()
        {
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
                _logger.LogInformation($"User {Context.UserIdentifier} joined tenant group: {tenantId}");
            }
        }

        // Connect user to their role-specific group
        public async Task JoinRoleGroup()
        {
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
                _logger.LogInformation($"User {Context.UserIdentifier} joined role group: {role}");
            }
        }

        // Send real-time notification to specific tenant
        public async Task SendToTenant(string tenantId, string message, string type = "info")
        {
            await Clients.Group($"tenant_{tenantId}").SendAsync("ReceiveNotification", new
            {
                message = message,
                type = type,
                timestamp = DateTime.UtcNow,
                sender = "System"
            });
        }

        // Send real-time notification to specific role
        public async Task SendToRole(string role, string message, string type = "info")
        {
            await Clients.Group($"role_{role}").SendAsync("ReceiveNotification", new
            {
                message = message,
                type = type,
                timestamp = DateTime.UtcNow,
                sender = "System"
            });
        }

        // Broadcast to all connected users
        public async Task Broadcast(string message, string type = "info")
        {
            await Clients.All.SendAsync("ReceiveNotification", new
            {
                message = message,
                type = type,
                timestamp = DateTime.UtcNow,
                sender = "System"
            });
        }

        // Real-time inventory updates
        public async Task InventoryUpdated(string tenantId, object inventoryData)
        {
            await Clients.Group($"tenant_{tenantId}").SendAsync("InventoryUpdated", inventoryData);
        }

        // Real-time sales updates
        public async Task SaleCreated(string tenantId, object saleData)
        {
            await Clients.Group($"tenant_{tenantId}").SendAsync("SaleCreated", saleData);
        }

        // Real-time prescription updates
        public async Task PrescriptionCreated(string tenantId, object prescriptionData)
        {
            await Clients.Group($"tenant_{tenantId}").SendAsync("PrescriptionCreated", prescriptionData);
        }

        // Real-time user updates (for admin panels)
        public async Task UserUpdated(string tenantId, object userData)
        {
            await Clients.Group($"tenant_{tenantId}").SendAsync("UserUpdated", userData);
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User?.Identity?.Name ?? "Anonymous";
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
            _logger.LogInformation($"User {user} connected to SignalR hub. Tenant: {tenantId}");
            
            // Auto-join tenant and role groups
            await JoinTenantGroup();
            await JoinRoleGroup();
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.User?.Identity?.Name ?? "Anonymous";
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
            _logger.LogInformation($"User {user} disconnected from SignalR hub. Tenant: {tenantId}");
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
