using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace Public.Frontend.Net.Tunnel
{
    public class DynamicRouteService
    {

        private readonly List<RouteConfig> _Routes = new List<RouteConfig>();
        private readonly List<ClusterConfig> _Clusters = new List<ClusterConfig>();
        
        public List<RouteConfig> Routes { get { return _Routes; } }
        public List<ClusterConfig> Clusters { get { return _Clusters; } }


        public void AddRoute(string connectionKey)
        {
            var rc = new RouteConfig
            {
                RouteId = connectionKey,
                Match = new RouteMatch { Path = $"/{connectionKey}/{{**catch-all}}" },
                
                ClusterId = "alpha"

            };

            _Routes.Add(rc);

        }
    }
}
