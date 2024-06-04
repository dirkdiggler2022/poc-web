using Yarp.ReverseProxy.Configuration;

namespace Public.Proxy.Configuration
{
    public class ProxyInfo
    {
        private readonly List<string> _proxyDestinationAddresses = new();

        public ProxyInfo()
        {
       
        }
        public void RegisterProxyHost(HttpContext context)
        {
            var host = context.Request.Host.ToString();
            if (_proxyDestinationAddresses.Contains(host))
                return;
            
            _proxyDestinationAddresses.Add(host);

            StaticLogger.Logger.LogInformation($"Registering Proxy Host {host}");
        }



    }
}
