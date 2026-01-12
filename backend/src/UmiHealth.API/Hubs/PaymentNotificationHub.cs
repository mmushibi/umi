using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using UmiHealth.Application.Models;

namespace UmiHealth.API.Hubs
{
    [Authorize]
    public class PaymentNotificationHub : Hub
    {
        private readonly ILogger<PaymentNotificationHub> _logger;

        public PaymentNotificationHub(ILogger<PaymentNotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Notify operations and super admin of new payment approval request
        /// </summary>
        public async Task NotifyPaymentApprovalRequest(PaymentApprovalRequest request)
        {
            await Clients.All.SendAsync("PaymentApprovalRequest", new
            {
                paymentId = request.PaymentId,
                tenantId = request.TenantId,
                tenantName = request.TenantName,
                planType = request.PlanType,
                amount = request.Amount,
                paymentMethod = request.PaymentMethod,
                requestDate = request.RequestDate,
                requestedBy = request.RequestedBy,
                additionalNotes = request.AdditionalNotes
            });

            _logger.LogInformation("Payment approval request sent to all clients: {PaymentId} for tenant {TenantId}", 
                request.PaymentId, request.TenantId);
        }

        /// <summary>
        /// Notify tenant of payment status update
        /// </summary>
        public async Task NotifyPaymentStatusUpdate(string paymentId, string status, string? approvedBy = null, string? transactionId = null, string? confirmationNumber = null)
        {
            await Clients.All.SendAsync("PaymentStatusUpdate", new
            {
                paymentId = paymentId,
                status = status,
                approvedBy = approvedBy,
                transactionId = transactionId,
                confirmationNumber = confirmationNumber,
                updatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Payment status update sent: {PaymentId} - {Status}", paymentId, status);
        }

        /// <summary>
        /// Notify tenant of user limit request status
        /// </summary>
        public async Task NotifyUserLimitRequest(string requestId, string status, string? approvedBy = null)
        {
            await Clients.All.SendAsync("UserLimitRequestUpdate", new
            {
                requestId = requestId,
                status = status,
                approvedBy = approvedBy,
                updatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("User limit request update sent: {RequestId} - {Status}", requestId, status);
        }

        /// <summary>
        /// Join specific tenant group for targeted notifications
        /// </summary>
        public async Task JoinTenantGroup(string tenantId)
        {
            await Groups.AddToGroupAsync($"tenant_{tenantId}");
            _logger.LogInformation("Client joined tenant group: {TenantId}", tenantId);
        }

        /// <summary>
        /// Leave tenant group
        /// </summary>
        public async Task LeaveTenantGroup(string tenantId)
        {
            await Groups.RemoveFromGroupAsync($"tenant_{tenantId}");
            _logger.LogInformation("Client left tenant group: {TenantId}", tenantId);
        }
    }
}
