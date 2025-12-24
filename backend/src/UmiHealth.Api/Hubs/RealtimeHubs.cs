using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Core.Entities;

namespace UmiHealth.Api.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time inventory updates
    /// Notifies clients of stock changes, low stock alerts, and expiry warnings
    /// </summary>
    public class InventoryHub : Hub
    {
        private static readonly Dictionary<string, List<string>> TenantConnections = new();

        public override async Task OnConnectedAsync()
        {
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value ?? "unknown";
            
            // Add connection to tenant group
            if (!TenantConnections.ContainsKey(tenantId))
            {
                TenantConnections[tenantId] = new List<string>();
            }
            TenantConnections[tenantId].Add(Context.ConnectionId);

            // Join tenant group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value ?? "unknown";

            if (TenantConnections.TryGetValue(tenantId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    TenantConnections.Remove(tenantId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to specific branch inventory updates
        /// </summary>
        public async Task SubscribeToBranchInventory(string branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"inventory_branch_{branchId}");
        }

        /// <summary>
        /// Unsubscribe from branch inventory updates
        /// </summary>
        public async Task UnsubscribeFromBranchInventory(string branchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"inventory_branch_{branchId}");
        }

        /// <summary>
        /// Subscribe to product-specific updates
        /// </summary>
        public async Task SubscribeToProduct(string productId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"product_{productId}");
        }

        /// <summary>
        /// Unsubscribe from product updates
        /// </summary>
        public async Task UnsubscribeFromProduct(string productId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"product_{productId}");
        }
    }

    /// <summary>
    /// SignalR Hub for real-time sales updates
    /// Notifies clients of completed sales, returns, and payment confirmations
    /// </summary>
    public class SalesHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value ?? "unknown";
            var branchId = Context.User?.FindFirst("branch_id")?.Value ?? "all";

            // Join tenant and branch groups
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sales_branch_{branchId}");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Subscribe to real-time sales dashboard
        /// </summary>
        public async Task SubscribeToDashboard(string branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard_{branchId}");
        }

        /// <summary>
        /// Unsubscribe from dashboard
        /// </summary>
        public async Task UnsubscribeFromDashboard(string branchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dashboard_{branchId}");
        }

        /// <summary>
        /// Request live sales data
        /// </summary>
        public async Task RequestLiveSalesData(string branchId)
        {
            // Client calls this to request current sales data
            await Clients.Group($"dashboard_{branchId}").SendAsync("RefreshSalesData");
        }
    }

    /// <summary>
    /// SignalR Hub for notifications and alerts
    /// Sends urgent notifications like low stock, expiry warnings, prescription ready
    /// </summary>
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? "unknown";
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value ?? "unknown";

            // Join user and tenant groups
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        public async Task GetUnreadCount(string userId)
        {
            // Send unread count to client
            await Clients.Caller.SendAsync("UnreadCountUpdated", 0);
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public async Task MarkAsRead(string notificationId)
        {
            // Server marks notification as read
            // Notify user about update
            await Clients.Caller.SendAsync("NotificationMarked", notificationId);
        }
    }

    /// <summary>
    /// Service for broadcasting real-time updates
    /// </summary>
    public interface IRealtimeNotificationService
    {
        /// <summary>
        /// Notify about stock update
        /// </summary>
        Task NotifyInventoryUpdateAsync(
            string tenantId,
            string branchId,
            string productId,
            string productName,
            int newQuantity,
            int previousQuantity);

        /// <summary>
        /// Notify about low stock
        /// </summary>
        Task NotifyLowStockAsync(
            string tenantId,
            string branchId,
            string productName,
            int remainingStock);

        /// <summary>
        /// Notify about expiring products
        /// </summary>
        Task NotifyExpiringProductAsync(
            string tenantId,
            string branchId,
            string productName,
            DateTime expiryDate);

        /// <summary>
        /// Notify about completed sale
        /// </summary>
        Task NotifySaleCompletedAsync(
            string tenantId,
            string branchId,
            string saleNumber,
            decimal totalAmount);

        /// <summary>
        /// Notify about payment received
        /// </summary>
        Task NotifyPaymentReceivedAsync(
            string tenantId,
            string userId,
            string saleNumber,
            decimal amount);

        /// <summary>
        /// Notify about user event
        /// </summary>
        Task NotifyUserAsync(
            string userId,
            string title,
            string message,
            object data = null);

        /// <summary>
        /// Update dashboard metrics
        /// </summary>
        Task UpdateDashboardAsync(
            string branchId,
            object metrics);
    }

    /// <summary>
    /// Implementation of real-time notification service
    /// </summary>
    public class RealtimeNotificationService : IRealtimeNotificationService
    {
        private readonly IHubContext<InventoryHub> _inventoryHub;
        private readonly IHubContext<SalesHub> _salesHub;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public RealtimeNotificationService(
            IHubContext<InventoryHub> inventoryHub,
            IHubContext<SalesHub> salesHub,
            IHubContext<NotificationHub> notificationHub)
        {
            _inventoryHub = inventoryHub;
            _salesHub = salesHub;
            _notificationHub = notificationHub;
        }

        public async Task NotifyInventoryUpdateAsync(
            string tenantId,
            string branchId,
            string productId,
            string productName,
            int newQuantity,
            int previousQuantity)
        {
            var update = new
            {
                productId,
                productName,
                newQuantity,
                previousQuantity,
                change = newQuantity - previousQuantity,
                timestamp = DateTime.UtcNow
            };

            // Notify branch-specific subscribers
            await _inventoryHub.Clients.Group($"inventory_branch_{branchId}")
                .SendAsync("InventoryUpdated", update);

            // Notify product-specific subscribers
            await _inventoryHub.Clients.Group($"product_{productId}")
                .SendAsync("ProductInventoryUpdated", update);
        }

        public async Task NotifyLowStockAsync(
            string tenantId,
            string branchId,
            string productName,
            int remainingStock)
        {
            var alert = new
            {
                type = "LowStock",
                productName,
                remainingStock,
                severity = remainingStock <= 5 ? "Critical" : "Warning",
                timestamp = DateTime.UtcNow
            };

            // Notify tenant and branch
            await _notificationHub.Clients.Group($"tenant_{tenantId}")
                .SendAsync("LowStockAlert", alert);

            await _inventoryHub.Clients.Group($"inventory_branch_{branchId}")
                .SendAsync("LowStockAlert", alert);
        }

        public async Task NotifyExpiringProductAsync(
            string tenantId,
            string branchId,
            string productName,
            DateTime expiryDate)
        {
            var alert = new
            {
                type = "ExpiringProduct",
                productName,
                expiryDate,
                daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days,
                timestamp = DateTime.UtcNow
            };

            // Notify tenant and branch
            await _notificationHub.Clients.Group($"tenant_{tenantId}")
                .SendAsync("ExpiryAlert", alert);

            await _inventoryHub.Clients.Group($"inventory_branch_{branchId}")
                .SendAsync("ExpiryAlert", alert);
        }

        public async Task NotifySaleCompletedAsync(
            string tenantId,
            string branchId,
            string saleNumber,
            decimal totalAmount)
        {
            var saleEvent = new
            {
                saleNumber,
                totalAmount,
                timestamp = DateTime.UtcNow
            };

            // Notify sales dashboard
            await _salesHub.Clients.Group($"dashboard_{branchId}")
                .SendAsync("SaleCompleted", saleEvent);

            // Notify tenant
            await _salesHub.Clients.Group($"tenant_{tenantId}")
                .SendAsync("SaleCompleted", saleEvent);
        }

        public async Task NotifyPaymentReceivedAsync(
            string tenantId,
            string userId,
            string saleNumber,
            decimal amount)
        {
            var payment = new
            {
                saleNumber,
                amount,
                timestamp = DateTime.UtcNow
            };

            // Notify specific user
            await _notificationHub.Clients.Group($"user_{userId}")
                .SendAsync("PaymentReceived", payment);

            // Notify tenant
            await _notificationHub.Clients.Group($"tenant_{tenantId}")
                .SendAsync("PaymentReceived", payment);
        }

        public async Task NotifyUserAsync(
            string userId,
            string title,
            string message,
            object data = null)
        {
            var notification = new
            {
                title,
                message,
                data,
                timestamp = DateTime.UtcNow
            };

            await _notificationHub.Clients.Group($"user_{userId}")
                .SendAsync("Notification", notification);
        }

        public async Task UpdateDashboardAsync(
            string branchId,
            object metrics)
        {
            await _salesHub.Clients.Group($"dashboard_{branchId}")
                .SendAsync("DashboardMetricsUpdated", metrics);
        }
    }
}
