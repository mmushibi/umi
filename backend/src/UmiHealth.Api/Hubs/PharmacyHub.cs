using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace UmiHealth.Api.Hubs
{
    [Authorize]
    public class PharmacyHub : Hub
    {
        private readonly ILogger<PharmacyHub> _logger;

        public PharmacyHub(ILogger<PharmacyHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;
            var branchId = Context.GetHttpContext()?.Request.Query["branchId"];
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(branchId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}_branch_{branchId}");
                _logger.LogInformation("User {UserId} connected to tenant {TenantId}, branch {BranchId}", userId, tenantId, branchId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;
            var branchId = Context.GetHttpContext()?.Request.Query["branchId"];
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(branchId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}_branch_{branchId}");
                _logger.LogInformation("User {UserId} disconnected from tenant {TenantId}, branch {BranchId}", userId, tenantId, branchId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinBranchGroup(string tenantId, string branchId)
        {
            var userTenantId = Context.User?.FindFirst("TenantId")?.Value;
            
            if (string.IsNullOrEmpty(userTenantId) || userTenantId != tenantId)
            {
                throw new UnauthorizedAccessException("Access denied to tenant");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}_branch_{branchId}");
            _logger.LogInformation("User {UserId} joined branch group for tenant {TenantId}, branch {BranchId}", 
                Context.UserIdentifier, tenantId, branchId);
        }

        public async Task LeaveBranchGroup(string tenantId, string branchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}_branch_{branchId}");
            _logger.LogInformation("User {UserId} left branch group for tenant {TenantId}, branch {BranchId}", 
                Context.UserIdentifier, tenantId, branchId);
        }
    }
}
