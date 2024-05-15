using System.Net.Http;
using System.Security.Cryptography.Xml;

namespace Public.Frontend
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHttpForwarder();
            var app = builder.Build();

            app.MapForwarder("/{**catch-all}", "https://example.com");

            app.Run();
        }

        //public static void Main(string[] args)
        //{
        //    var builder = WebApplication.CreateBuilder(args);

        //    builder.Services.AddReverseProxy()
        //        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        //    //builder.WebHost.ConfigureKestrel(options =>
        //    //{

        //    //    options.AllowAlternateSchemes = true;

        //    //});
        //    builder.Services.AddTunnelServices();

        //    var app = builder.Build();

        //    app.MapReverseProxy();

        //    // Uncomment to support websocket connections
        //    //app.MapWebSocketTunnel("/connect-ws");

        //    // Auth can be added to this endpoint and we can restrict it to certain points
        //    // to avoid exteranl traffic hitting it
        //    app.MapHttp2Tunnel("/connect-h2");

        //    app.Run();
        //}
    }
}
