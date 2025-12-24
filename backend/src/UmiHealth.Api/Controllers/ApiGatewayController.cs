using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using UmiHealth.Api.Services;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ApiGatewayController : ControllerBase
    {
        private readonly IApiGatewayService _gatewayService;
        private readonly ILogger<ApiGatewayController> _logger;

        public ApiGatewayController(
            IApiGatewayService gatewayService,
            ILogger<ApiGatewayController> logger)
        {
            _gatewayService = gatewayService;
            _logger = logger;
        }

        [HttpGet("services")]
        [Authorize(Roles = "SuperAdmin,Admin,Operations")]
        public async Task<ActionResult> GetServiceStatuses()
        {
            try
            {
                var services = _gatewayService.GetServiceStatuses();
                return Ok(new { success = true, data = services });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to get service statuses");
                return StatusCode(500, new { success = false, message = "Failed to retrieve service statuses" });
            }
        }

        [HttpGet("services/{serviceName}/health")]
        [Authorize(Roles = "SuperAdmin,Admin,Operations")]
        public async Task<ActionResult> CheckServiceHealth(string serviceName)
        {
            try
            {
                var healthResult = await _gatewayService.CheckServiceHealthAsync(serviceName);
                return Ok(new { success = true, data = healthResult });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to check health for service: {ServiceName}", serviceName);
                return StatusCode(500, new { success = false, message = $"Failed to check health for service: {serviceName}" });
            }
        }

        [HttpPost("services/{serviceName}/register")]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult RegisterService(string serviceName, [FromBody] ServiceRegistrationRequest request)
        {
            try
            {
                var endpoint = new ServiceEndpoint
                {
                    Id = System.Guid.NewGuid().ToString(),
                    Url = request.Url,
                    IsHealthy = true,
                    Weight = request.Weight ?? 1,
                    RequestCount = 0,
                    LastUsed = System.DateTime.UtcNow
                };

                _gatewayService.RegisterService(serviceName, endpoint);
                
                return Ok(new { success = true, message = $"Service endpoint registered successfully" });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to register service endpoint for: {ServiceName}", serviceName);
                return StatusCode(500, new { success = false, message = "Failed to register service endpoint" });
            }
        }

        [HttpDelete("services/{serviceName}/endpoints/{endpointId}")]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult UnregisterService(string serviceName, string endpointId)
        {
            try
            {
                _gatewayService.UnregisterService(serviceName, endpointId);
                return Ok(new { success = true, message = "Service endpoint unregistered successfully" });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister service endpoint: {EndpointId}", endpointId);
                return StatusCode(500, new { success = false, message = "Failed to unregister service endpoint" });
            }
        }
    }

    public class ServiceRegistrationRequest
    {
        public string Url { get; set; } = string.Empty;
        public int? Weight { get; set; }
    }
}
