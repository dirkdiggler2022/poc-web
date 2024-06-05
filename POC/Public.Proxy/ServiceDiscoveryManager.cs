using System.Collections.ObjectModel;
using Yarp.ReverseProxy.Configuration;

namespace Public.Proxy
{
    public class ServiceDiscoveryManager(ILoggerFactory loggerFactory, InMemoryConfigProvider inMemoryConfigProvider)
    {
        private readonly ILogger<ServiceDiscoveryManager> _logger = loggerFactory.CreateLogger<ServiceDiscoveryManager>();

        private readonly Dictionary<string, string> _tunnelProxyRegistrations = new();
        private readonly InMemoryConfigProvider _inMemoryConfigProvider = inMemoryConfigProvider;
        public void UpdateRegistrations(HttpContext context)
        {
            var connectionKey = context.Request.Headers["X-Connection-Key"].ToString();
            var tunnelHost = context.Request.Headers["X-Tunnel-Host"].ToString();
            if (_tunnelProxyRegistrations.TryGetValue(connectionKey, out string? value))
            {
                if (value != tunnelHost)
                    UpdateTunnelProxyRoute(connectionKey, tunnelHost);
            }
            else
            {
                UpdateTunnelProxyRoute(connectionKey, tunnelHost);
                _tunnelProxyRegistrations[connectionKey] = tunnelHost;
            }


        }

        private void UpdateTunnelProxyRoute(string connectionKey, string tunnelHost)
        {
            var config = inMemoryConfigProvider.GetConfig();
            var existingClusters = config.Clusters.ToList();
            var existingRoutes = config.Routes.ToList();
            var existingCluster = existingClusters.SingleOrDefault(n => n.ClusterId.Equals($"{connectionKey}-cluster"));
            var existingRoute = existingRoutes.SingleOrDefault(n => n.RouteId.Equals($"{connectionKey}-route"));
            if (existingCluster != null)
                existingClusters.Remove(existingCluster);
            if (existingRoute != null)
                existingRoutes.Remove(existingRoute);

            var newClusterDestination = new DestinationConfig { Address = $"https://{tunnelHost}" };
            var newClusterDestinations = new Dictionary<string, DestinationConfig>
            {
                { "default", newClusterDestination }
            };
            var newClusterConfig = new ClusterConfig
                { ClusterId = $"{connectionKey}-route", Destinations = newClusterDestinations };

            var requiredHeaders = new RouteHeader
            {
                Mode = HeaderMatchMode.Exists, Name = "X-Connection-Key", Values = new List<string> { connectionKey }
            };
            var newRouteMatch = new RouteMatch { Path = "{**catch-all}", Headers = new List<RouteHeader>{requiredHeaders}};

            var newRouteConfig = new RouteConfig
                { ClusterId = newClusterConfig.ClusterId, RouteId = $"{connectionKey}-route", Match = newRouteMatch};

            existingClusters.Add(newClusterConfig);
            existingRoutes.Add(newRouteConfig);

            _inMemoryConfigProvider.Update(existingRoutes,existingClusters);

        }
    }
}
