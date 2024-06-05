using System.Text.Json;
using Public.Proxy.Configuration;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;

namespace Public.Proxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddConsole();


            //loads our routes, probably will be cosmo or azure configs
            var routeLoader = new CustomConfigurationLoader().GetProvider().GetConfig();

            var proxyLoadBalancingPolicy = new ProxyLoadBalancingPolicy();
            builder.Services.AddSingleton<ILoadBalancingPolicy>(proxyLoadBalancingPolicy);


            builder.Services.AddSingleton<ProxyInfoService>();
            builder.Services.AddSingleton<ServiceDiscoveryManager>();


            builder.Services.AddReverseProxy()
                .LoadFromMemory(new List<RouteConfig>(), new List<ClusterConfig>())
                //.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var app = builder.Build();

            app.MapReverseProxy();

            // Uncomment to support websocket connections

            app.Map("/routes", (HttpContext context, InMemoryConfigProvider proxyConfigProvider) =>
            {

                var proxyConfig = proxyConfigProvider.GetConfig();
                var response = JsonSerializer.Serialize(proxyConfig);
                return response;

            });


            app.Map("/health", (HttpContext context, ProxyInfoService proxyInfoService) =>
            {
                //var hostInfo = $"Local = {context.Connection.LocalIpAddress}:{context.Connection.LocalPort};  Remote = {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}; ";
                proxyInfoService.UpdateProxyConnectionStatus(context);
                
            });

            app.MapPost("/RegisterEndpoint", (HttpContext context,ServiceDiscoveryManager serviceDiscoveryManager) =>
            {


                serviceDiscoveryManager.UpdateRegistrations(context);
                return "Ok";
            });

            app.Run();
        }
    }
}
