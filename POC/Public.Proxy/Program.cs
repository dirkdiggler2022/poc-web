using System.Text.Json;
using Public.Proxy.Configuration;
using Yarp.ReverseProxy.Configuration;

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

            app.Run();
        }
    }
}
