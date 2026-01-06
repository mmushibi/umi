using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace UmiHealth.Api.Hubs
{
    /// <summary>
    /// Simple test hub for connection testing
    /// No authorization required for basic connectivity testing
    /// </summary>
    public class TestHub : Hub
    {
        private readonly ILogger<TestHub> _logger;

        public TestHub(ILogger<TestHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected to TestHub: {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Connected", $"Welcome! Your connection ID is {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            _logger.LogInformation("Client disconnected from TestHub: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Simple ping method for testing
        /// </summary>
        public async Task<string> Ping(string message = "ping")
        {
            _logger.LogInformation("Ping received from {ConnectionId}: {Message}", Context.ConnectionId, message);
            return $"pong: {message}";
        }

        /// <summary>
        /// Echo method for testing bidirectional communication
        /// </summary>
        public async Task Echo(string message)
        {
            await Clients.Caller.SendAsync("EchoResponse", message);
        }

        /// <summary>
        /// Get server time
        /// </summary>
        public async Task<DateTime> GetServerTime()
        {
            return DateTime.UtcNow;
        }
    }
}
