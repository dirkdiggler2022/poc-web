using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace Public.Frontend.Net.Configuration
{
    //this will be the thing that loads from DB or file
    //creates clusters and routes
    public class CustomConfigurationLoader
    {
        //will be connectionKey id or something to identify customer
        private List<string> _connectionKeys;
        private List<RouteConfig> _routeConfigs = new();
        private List<ClusterConfig> _clusterConfigs = new();
        private void InitFakeConnections()
        {
            _connectionKeys = Enumerable.Range(0, 20).Select(n => $"Agent{n}").ToList();
        }

        void LoadRoutesAndClusters()
        {


            foreach (var connectionKey in _connectionKeys)
            {
                var config = GetRouteConfig(connectionKey);
                _routeConfigs.Add(config.RouteConfig);
                _clusterConfigs.Add(config.ClusterConfig);
            }
        }

        (RouteConfig RouteConfig, ClusterConfig ClusterConfig) GetRouteConfig(string connectionKey)
        {
            var cluster = new ClusterConfig
            {
                ClusterId = $"{connectionKey}-cluster",
                Destinations = new ConcurrentDictionary<string, DestinationConfig>(new List<KeyValuePair<string, DestinationConfig>>{new("default",GetDestinationConfig(connectionKey))})
            };

            var route = new RouteConfig
            {
                RouteId = $"{connectionKey}-route",
                ClusterId = cluster.ClusterId,
                Match = new RouteMatch
                {
                    Path = "{**catch-all}",
                    Headers = new List<RouteHeader>
                    {
                        new()
                        {
                            Name = GlobalConstants.CONNECTION_KEY_HEADER_NAME,
                            Values = new List<string>{connectionKey}
                        }
                    }
                },
                
            };

            (RouteConfig RouteConfig, ClusterConfig ClusterConfig) result = (route,cluster);
            return result;
        }

        static DestinationConfig GetDestinationConfig(string connectionKey)
        {
            var result = new DestinationConfig
            {
                // Address = $"http://{connectionKey}.proxy.{Guid.NewGuid()}.app"
                Address = $"http://backend1.app"
            };
            return result;

        }

        public CustomProxyConfigProvider GetProvider()
        {
            InitFakeConnections();
            LoadRoutesAndClusters();
            var result = new CustomProxyConfigProvider(_routeConfigs, _clusterConfigs);
            return result;
        } 

    }
}
