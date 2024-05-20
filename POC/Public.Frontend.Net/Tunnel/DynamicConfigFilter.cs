using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace Public.Frontend.Net.Tunnel
{
    public class DynamicConfigFilter : IProxyConfigFilter
    {
        private DynamicRouteService _dynamicRouteService;
        public DynamicConfigFilter(DynamicRouteService dynamicRouteService)
        {
            _dynamicRouteService = dynamicRouteService;
        }
        public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
        {
            return new ValueTask<ClusterConfig>(cluster);

        }

        public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
        {
            return new ValueTask<RouteConfig>(route);
        }
    }
}
