using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UmiHealth.Api.Services
{
    public interface IApiGatewayService
    {
        Task<GatewayResponse> RouteRequestAsync(GatewayRequest request);
        Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName);
        void RegisterService(string serviceName, ServiceEndpoint endpoint);
        void UnregisterService(string serviceName, string endpointId);
        IEnumerable<ServiceStatus> GetServiceStatuses();
    }

    public class ApiGatewayService : IApiGatewayService
    {
        private readonly ILogger<ApiGatewayService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, ServiceRegistry> _serviceRegistry;
        private readonly ConcurrentDictionary<string, ServiceHealth> _serviceHealth;
        private readonly Timer _healthCheckTimer;

        public ApiGatewayService(
            ILogger<ApiGatewayService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _serviceRegistry = new ConcurrentDictionary<string, ServiceRegistry>();
            _serviceHealth = new ConcurrentDictionary<string, ServiceHealth>();
            
            // Initialize services from configuration
            InitializeServicesFromConfiguration();
            
            // Start health check timer
            _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        public async Task<GatewayResponse> RouteRequestAsync(GatewayRequest request)
        {
            var serviceName = ResolveServiceName(request.Path, request.Method);
            
            if (string.IsNullOrEmpty(serviceName))
            {
                return new GatewayResponse
                {
                    StatusCode = 404,
                    Content = "Service not found for the requested path",
                    Headers = new Dictionary<string, string>()
                };
            }

            if (!_serviceRegistry.TryGetValue(serviceName, out var serviceRegistry))
            {
                return new GatewayResponse
                {
                    StatusCode = 503,
                    Content = $"Service '{serviceName}' is not available",
                    Headers = new Dictionary<string, string>()
                };
            }

            var endpoint = SelectEndpoint(serviceName, serviceRegistry);
            if (endpoint == null)
            {
                return new GatewayResponse
                {
                    StatusCode = 503,
                    Content = $"No healthy endpoints available for service '{serviceName}'",
                    Headers = new Dictionary<string, string>()
                };
            }

            try
            {
                var targetUrl = BuildTargetUrl(endpoint, request);
                var response = await ForwardRequest(targetUrl, request);
                
                // Update endpoint statistics
                endpoint.RequestCount++;
                endpoint.LastUsed = DateTime.UtcNow;
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward request to {EndpointUrl}", endpoint.Url);
                
                // Mark endpoint as unhealthy
                MarkEndpointUnhealthy(serviceName, endpoint.Id);
                
                return new GatewayResponse
                {
                    StatusCode = 502,
                    Content = "Failed to forward request to service",
                    Headers = new Dictionary<string, string>()
                };
            }
        }

        public async Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName)
        {
            if (!_serviceRegistry.TryGetValue(serviceName, out var serviceRegistry))
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = "Service not found"
                };
            }

            var healthCheckTasks = serviceRegistry.Endpoints.Select(async endpoint =>
            {
                try
                {
                    var healthUrl = $"{endpoint.Url}/health";
                    var response = await _httpClient.GetAsync(healthUrl);
                    
                    return new EndpointHealth
                    {
                        EndpointId = endpoint.Id,
                        Url = endpoint.Url,
                        IsHealthy = response.IsSuccessStatusCode,
                        ResponseTime = response.Headers.Date?.Subtract(DateTime.UtcNow).TotalMilliseconds ?? 0
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for endpoint {EndpointUrl}", endpoint.Url);
                    return new EndpointHealth
                    {
                        EndpointId = endpoint.Id,
                        Url = endpoint.Url,
                        IsHealthy = false,
                        ResponseTime = -1
                    };
                }
            });

            var endpointHealthResults = await Task.WhenAll(healthCheckTasks);
            var healthyEndpoints = endpointHealthResults.Count(h => h.IsHealthy);
            var totalEndpoints = endpointHealthResults.Length;

            return new HealthCheckResult
            {
                IsHealthy = healthyEndpoints > 0,
                Message = $"{healthyEndpoints}/{totalEndpoints} endpoints are healthy",
                EndpointHealth = endpointHealthResults.ToList()
            };
        }

        public void RegisterService(string serviceName, ServiceEndpoint endpoint)
        {
            var serviceRegistry = _serviceRegistry.GetOrAdd(serviceName, _ => new ServiceRegistry
            {
                ServiceName = serviceName,
                Endpoints = new List<ServiceEndpoint>(),
                LoadBalancingStrategy = GetLoadBalancingStrategy(serviceName)
            });

            lock (serviceRegistry.Endpoints)
            {
                // Remove existing endpoint with same URL if exists
                serviceRegistry.Endpoints.RemoveAll(e => e.Url == endpoint.Url);
                serviceRegistry.Endpoints.Add(endpoint);
            }

            _logger.LogInformation("Registered endpoint {EndpointUrl} for service {ServiceName}", endpoint.Url, serviceName);
        }

        public void UnregisterService(string serviceName, string endpointId)
        {
            if (_serviceRegistry.TryGetValue(serviceName, out var serviceRegistry))
            {
                lock (serviceRegistry.Endpoints)
                {
                    var removed = serviceRegistry.Endpoints.RemoveAll(e => e.Id == endpointId);
                    if (removed > 0)
                    {
                        _logger.LogInformation("Unregistered endpoint {EndpointId} for service {ServiceName}", endpointId, serviceName);
                    }
                }
            }
        }

        public IEnumerable<ServiceStatus> GetServiceStatuses()
        {
            return _serviceRegistry.Select(kvp =>
            {
                var registry = kvp.Value;
                var healthyEndpoints = registry.Endpoints.Count(e => e.IsHealthy);
                var totalEndpoints = registry.Endpoints.Count;

                return new ServiceStatus
                {
                    ServiceName = registry.ServiceName,
                    HealthyEndpoints = healthyEndpoints,
                    TotalEndpoints = totalEndpoints,
                    IsHealthy = healthyEndpoints > 0,
                    LoadBalancingStrategy = registry.LoadBalancingStrategy,
                    Endpoints = registry.Endpoints.Select(e => new EndpointStatus
                    {
                        Id = e.Id,
                        Url = e.Url,
                        IsHealthy = e.IsHealthy,
                        RequestCount = e.RequestCount,
                        LastUsed = e.LastUsed
                    }).ToList()
                };
            });
        }

        private void InitializeServicesFromConfiguration()
        {
            var servicesConfig = _configuration.GetSection("ApiGateway:Services");
            
            foreach (var serviceSection in servicesConfig.GetChildren())
            {
                var serviceName = serviceSection.Key;
                var endpoints = serviceSection.GetSection("Endpoints").Get<List<string>>();
                
                if (endpoints != null)
                {
                    foreach (var endpointUrl in endpoints)
                    {
                        var endpoint = new ServiceEndpoint
                        {
                            Id = Guid.NewGuid().ToString(),
                            Url = endpointUrl.TrimEnd('/'),
                            IsHealthy = true,
                            Weight = 1,
                            RequestCount = 0,
                            LastUsed = DateTime.UtcNow
                        };
                        
                        RegisterService(serviceName, endpoint);
                    }
                }
            }
        }

        private string? ResolveServiceName(string path, string method)
        {
            // Define routing rules based on path patterns
            var routingRules = new Dictionary<string, (string ServiceName, string[] Methods)>
            {
                ["/api/v1/auth"] = ("auth-service", new[] { "GET", "POST", "PUT", "DELETE" }),
                ["/api/v1/users"] = ("user-service", new[] { "GET", "POST", "PUT", "DELETE" }),
                ["/api/v1/patients"] = ("patient-service", new[] { "GET", "POST", "PUT", "DELETE" }),
                ["/api/v1/inventory"] = ("inventory-service", new[] { "GET", "POST", "PUT", "DELETE" }),
                ["/api/v1/sales"] = ("sales-service", new[] { "GET", "POST", "PUT" }),
                ["/api/v1/payments"] = ("payment-service", new[] { "GET", "POST", "PUT" }),
                ["/api/v1/prescriptions"] = ("prescription-service", new[] { "GET", "POST", "PUT", "DELETE" }),
                ["/api/v1/subscriptions"] = ("subscription-service", new[] { "GET", "POST", "PUT" }),
                ["/api/v1/tenants"] = ("tenant-service", new[] { "GET", "POST", "PUT" }),
                ["/api/v1/reports"] = ("report-service", new[] { "GET" })
            };

            foreach (var rule in routingRules)
            {
                if (path.StartsWith(rule.Key, StringComparison.OrdinalIgnoreCase) && 
                    rule.Value.Methods.Contains(method))
                {
                    return rule.Value.ServiceName;
                }
            }

            return null;
        }

        private ServiceEndpoint? SelectEndpoint(string serviceName, ServiceRegistry serviceRegistry)
        {
            var healthyEndpoints = serviceRegistry.Endpoints.Where(e => e.IsHealthy).ToList();
            
            if (!healthyEndpoints.Any())
            {
                return null;
            }

            return serviceRegistry.LoadBalancingStrategy.ToLower() switch
            {
                "roundrobin" => SelectRoundRobin(healthyEndpoints),
                "weighted" => SelectWeighted(healthyEndpoints),
                "leastconnections" => SelectLeastConnections(healthyEndpoints),
                _ => SelectRoundRobin(healthyEndpoints)
            };
        }

        private ServiceEndpoint SelectRoundRobin(List<ServiceEndpoint> endpoints)
        {
            var endpoint = endpoints.OrderBy(e => e.RequestCount).First();
            return endpoint;
        }

        private ServiceEndpoint SelectWeighted(List<ServiceEndpoint> endpoints)
        {
            var totalWeight = endpoints.Sum(e => e.Weight);
            var random = new Random().NextDouble() * totalWeight;
            
            var currentWeight = 0.0;
            foreach (var endpoint in endpoints)
            {
                currentWeight += endpoint.Weight;
                if (random <= currentWeight)
                {
                    return endpoint;
                }
            }

            return endpoints.First();
        }

        private ServiceEndpoint SelectLeastConnections(List<ServiceEndpoint> endpoints)
        {
            return endpoints.OrderBy(e => e.RequestCount).First();
        }

        private string BuildTargetUrl(ServiceEndpoint endpoint, GatewayRequest request)
        {
            var targetUrl = $"{endpoint.Url}{request.Path}";
            
            if (!string.IsNullOrEmpty(request.QueryString))
            {
                targetUrl += request.QueryString;
            }

            return targetUrl;
        }

        private async Task<GatewayResponse> ForwardRequest(string targetUrl, GatewayRequest request)
        {
            var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUrl);
            
            // Copy headers
            foreach (var header in request.Headers)
            {
                if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add gateway-specific headers
            httpRequest.Headers.Add("X-Gateway-Request-ID", request.RequestId);
            httpRequest.Headers.Add("X-Gateway-Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            // Add body if present
            if (!string.IsNullOrEmpty(request.Body) && 
                (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
            {
                httpRequest.Content = new StringContent(request.Body, System.Text.Encoding.UTF8, request.ContentType);
            }

            var response = await _httpClient.SendAsync(httpRequest);
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            var headers = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            return new GatewayResponse
            {
                StatusCode = (int)response.StatusCode,
                Content = responseBody,
                Headers = headers
            };
        }

        private void MarkEndpointUnhealthy(string serviceName, string endpointId)
        {
            if (_serviceRegistry.TryGetValue(serviceName, out var serviceRegistry))
            {
                var endpoint = serviceRegistry.Endpoints.FirstOrDefault(e => e.Id == endpointId);
                if (endpoint != null)
                {
                    endpoint.IsHealthy = false;
                    _logger.LogWarning("Marked endpoint {EndpointId} as unhealthy", endpointId);
                }
            }
        }

        private string GetLoadBalancingStrategy(string serviceName)
        {
            return _configuration[$"ApiGateway:Services:{serviceName}:LoadBalancingStrategy"] ?? "RoundRobin";
        }

        private async void PerformHealthChecks(object? state)
        {
            foreach (var serviceName in _serviceRegistry.Keys)
            {
                try
                {
                    await CheckServiceHealthAsync(serviceName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check failed for service {ServiceName}", serviceName);
                }
            }
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
            _httpClient?.Dispose();
        }
    }

    // Supporting classes
    public class GatewayRequest
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string QueryString { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/json";
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }

    public class GatewayResponse
    {
        public int StatusCode { get; set; }
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class ServiceRegistry
    {
        public string ServiceName { get; set; } = string.Empty;
        public List<ServiceEndpoint> Endpoints { get; set; } = new();
        public string LoadBalancingStrategy { get; set; } = "RoundRobin";
    }

    public class ServiceEndpoint
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsHealthy { get; set; } = true;
        public int Weight { get; set; } = 1;
        public long RequestCount { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }

    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<EndpointHealth> EndpointHealth { get; set; } = new();
    }

    public class EndpointHealth
    {
        public string EndpointId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public double ResponseTime { get; set; }
    }

    public class ServiceStatus
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public int HealthyEndpoints { get; set; }
        public int TotalEndpoints { get; set; }
        public string LoadBalancingStrategy { get; set; } = string.Empty;
        public List<EndpointStatus> Endpoints { get; set; } = new();
    }

    public class EndpointStatus
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public long RequestCount { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class ServiceHealth
    {
        public DateTime LastCheck { get; set; } = DateTime.UtcNow;
        public bool IsHealthy { get; set; } = true;
        public Dictionary<string, bool> EndpointHealth { get; set; } = new();
    }
}
