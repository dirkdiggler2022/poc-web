using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace Public.Frontend.Net.Tunnel
{
    public class TunnelLoadBalancingPolicy:ILoadBalancingPolicy
    {
        public string Name => "TunnelLoadBalancingPolicy";

        public DestinationState? PickDestination(HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> availableDestinations)
        {
            return availableDestinations[^1];
        }
    }
}
