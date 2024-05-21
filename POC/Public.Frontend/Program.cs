using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
using Public.Frontend.Net.Configuration;
using Public.Frontend.Net.Tunnel;
using Public.Frontend.Net.Utilities;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace Public.Frontend
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddConsole();

            builder.WebHost.ConfigureKestrel(options =>
            {
                //need for ngrok, not sure if needed in platform
                options.AllowAlternateSchemes = true;
            });
            //loads our routes, probably will be cosmo or azure configs
            var routeLoader = new CustomConfigurationLoader().GetProvider().GetConfig();
            builder.Services.AddReverseProxy()
                .LoadFromMemory(routeLoader.Routes, routeLoader.Clusters);

                //.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


            builder.Services.AddTunnelServices();

            var app = builder.Build();

            ApplicationLogging.LoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();


            app.MapReverseProxy();

            // Uncomment to support websocket connections
            //app.MapWebSocketTunnel("/connect-ws");

            // Auth can be added to this endpoint and we can restrict it to certain points
            // to avoid exteranl traffic hitting it
            app.MapHttp2Tunnel("/connect-h2/{connectionKey}");

            app.Run();
        }

    }
}
