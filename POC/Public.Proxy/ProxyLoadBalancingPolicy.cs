using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace Public.Proxy
{
    public class ProxyLoadBalancingPolicy : ILoadBalancingPolicy
    {
        public string Name => "ProxyLoadBalancingPolicy";

        private Dictionary<string, string> _serverLookup = new();

        public DestinationState? PickDestination(HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> availableDestinations)
        {
           
            var model = new DestinationModel(new DestinationConfig { Address = _serverLookup["Agent1"] });
            var ds = new DestinationState("SomeDestination", model);
            return ds;

            //return availableDestinations[^1];
        }

        public void AddServerRegistration(string connectionKey, string ipAddress)
        {
            if(_serverLookup.ContainsKey(connectionKey))
                _serverLookup[connectionKey] = ipAddress;
            else
            {
                _serverLookup.Add(connectionKey,ipAddress);
            }
        }
    }
}
