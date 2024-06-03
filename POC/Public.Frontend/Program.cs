using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
using Public.Frontend.Net.Configuration;
using Public.Frontend.Net.Tunnel;
using Public.Frontend.Net.Utilities;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Transforms;

namespace Public.Frontend
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("proxy-config/proxy.config.json", true,true);
            builder.Configuration.AddJsonFile("proxy-config-old/proxy.config.json", true, true);

            builder.Logging.AddConsole();
            
            builder.WebHost.ConfigureKestrel(options =>
            {
                //need for ngrok, not sure if needed in platform
                options.AllowAlternateSchemes = true;
            });
            //loads our routes, probably will be cosmo or azure configs
            //var routeLoader = new CustomConfigurationLoader();
            //var configProvider =     routeLoader.GetProvider().GetConfig();
            //builder.Services.AddSingleton<IProxyConfig>(configProvider);
            //builder.Services.AddSingleton<CustomConfigurationLoader>(routeLoader);
            //builder.Services.AddSingleton<ILoadBalancingPolicy, TunnelLoadBalancingPolicy>();
            builder.Services.AddReverseProxy()
                //.LoadFromMemory(configProvider.Routes, configProvider.Clusters)
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            //var forwarder = new DirectForwardingService();
            //builder.Services.AddSingleton(forwarder);

            ////.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
            //builder.Services.AddHttpForwarder();

            builder.Services.AddTunnelServices();

            var app = builder.Build();

            ApplicationLogging.LoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();


            app.MapReverseProxy();

            // Uncomment to support websocket connections
            app.MapWebSocketTunnel("/connect-ws/{connectionKey}");

            // Auth can be added to this endpoint and we can restrict it to certain points
            // to avoid exteranl traffic hitting it
            app.MapHttp2Tunnel("/connect-h2/{connectionKey}");


            app.MapAgentInfoEndpoint("/Agent/{agent}");

            app.MapAgentsInfoEndpoint("/agents");

            app.MapRouteInfo("/routes");

            app.Map("/health", (HttpContext context) =>
            {
                var hostInfo = $"Local = {context.Connection.LocalIpAddress}:{context.Connection.LocalPort};  Remote = {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}; ";
                return $"{hostInfo} is Healthy";
            });

            app.Map("/Forward/{**catch-all}", async (HttpContext context, IHttpForwarder forwarder, DirectForwardingService forwardingService) =>
            {
                await forwardingService.Forward(context, forwarder);
            });

            app.Run();
        }

    }
}
