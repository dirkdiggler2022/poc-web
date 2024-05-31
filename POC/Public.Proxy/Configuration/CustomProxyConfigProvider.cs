using Yarp.ReverseProxy.Configuration;

namespace Public.Proxy.Configuration
{

    //this will be something that is integrated with cosmo db or 
    //azure configs.
    public class CustomProxyConfigProvider : IProxyConfigProvider
    {
        // Marked as volatile so that updates are atomic
        private volatile CustomProxyConfig _config;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public CustomProxyConfigProvider(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
            : this(routes, clusters, Guid.NewGuid().ToString())
        { }

        /// <summary>
        /// Creates a new instance, specifying a revision id of the configuration.
        /// </summary>
        public CustomProxyConfigProvider(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, string revisionId)
        {
            _config = new CustomProxyConfig(routes, clusters, revisionId);
        }

        /// <summary>
        /// Implementation of the IProxyConfigProvider.GetConfig method to supply the current snapshot of configuration
        /// </summary>
        /// <returns>An immutable snapshot of the current configuration state</returns>
        public IProxyConfig GetConfig() => _config;

        /// <summary>
        /// Swaps the config state with a new snapshot of the configuration, then signals that the old one is outdated.
        /// </summary>
        public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            var newConfig = new CustomProxyConfig(routes, clusters);
            UpdateInternal(newConfig);
        }

        /// <summary>
        /// Swaps the config state with a new snapshot of the configuration, then signals that the old one is outdated.
        /// </summary>
        public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, string revisionId)
        {
            var newConfig = new CustomProxyConfig(routes, clusters, revisionId);
            UpdateInternal(newConfig);
        }

        private void UpdateInternal(CustomProxyConfig newConfig)
        {
            var oldConfig = Interlocked.Exchange(ref _config, newConfig);
            oldConfig.SignalChange();
        }

    }
}
