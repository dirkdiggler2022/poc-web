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

            builder.Services.AddTunnelServices();

            var app = builder.Build();

            app.MapForwarder("/{**catch-all}", "http://backend1.app");

            app.MapHttp2Tunnel("/connect-h2");

            app.Run();
        }

    }
}
