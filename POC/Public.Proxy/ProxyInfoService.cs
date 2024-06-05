namespace Public.Proxy
{
    public class ProxyInfoService
    {
        private Dictionary<string, DateTime> _proxyConnectionStatus = new();

        public Dictionary<string, DateTime> ProxyConnectionStatus=>_proxyConnectionStatus;

        private readonly ILogger<ProxyInfoService> _logger;


        public ProxyInfoService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProxyInfoService>();
        }

        public void UpdateProxyConnectionStatus(HttpContext context)
        {
            var host = context.Request.Host.ToString();
            if (_proxyConnectionStatus.ContainsKey(host))
            {
                _logger.LogInformation($"Updating Proxy Registration for {context.Request.Host}");
                _proxyConnectionStatus[host] = DateTime.UtcNow;
            }
            else
            {
                _logger.LogInformation($"Adding Proxy Registration for {context.Request.Host}");
                _proxyConnectionStatus.Add(host,DateTime.UtcNow);
            }
        }
    }
}
