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
            
            builder.Services.AddReverseProxy()
            //    .LoadFromMemory(routeLoader.Routes, routeLoader.Clusters);
                //.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var app = builder.Build();

            app.MapReverseProxy();

            // Uncomment to support websocket connections

            app.Map("/routes", (HttpContext context, IProxyConfigProvider proxyConfigProvider) =>
            {

                var proxyConfig = proxyConfigProvider.GetConfig();
                var response = JsonSerializer.Serialize(proxyConfig);
                return response;

            });


            app.Map("/health", (HttpContext context) =>
            {
                var hostInfo = $"Local = {context.Connection.LocalIpAddress}:{context.Connection.LocalPort};  Remote = {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}; ";
                return $"{hostInfo} is Healthy";
            });
            app.MapGet("/register/{**catch-all}", (HttpContext context) =>
            {
                //var proxyPolicy = policy as ProxyLoadBalancingPolicy;
                proxyLoadBalancingPolicy.AddServerRegistration("Agent1","https://localhost:7243");
               //proxyLoadBalancingPolicy.AddServerRegistration("Agent1","https://localhost:7243");
                return "Ok";
            });
            app.Run();
        }
    }
}
