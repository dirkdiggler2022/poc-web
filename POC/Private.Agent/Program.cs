using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.AspNetCore.Connections.Features;
using Private.Agent.Net.Monitoring;

namespace Private.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            builder.Services.AddSingleton<ConnectionMonitor>();

            
            var url = builder.Configuration["Tunnel:Url"]!;

            //overload command line arg for url
            if (args.Length > 0 && Uri.TryCreate(args[0].ToString(), UriKind.Absolute, out var test))
                url = test.ToString();


            builder.WebHost.UseTunnelTransport(url, options =>
            {
                options.Transport = url.Contains("connect-h2") ? TransportType.HTTP2 : TransportType.WebSockets;
            });

            var app = builder.Build();

            app.Use(async (context, next) =>
            {
                var feature = context.Features.Get<IConnectionSocketFeature>();
                if (feature != null)
                {
                    var dirk = feature.Socket.Poll(10000, SelectMode.SelectError);
                }
                //if (tlsFeature != null && tlsFeature.CipherAlgorithm == CipherAlgorithmType.Null)
                //{
                //    throw new NotSupportedException(
                //        $"Prohibited cipher: {tlsFeature.CipherAlgorithm}");
                //}

                await next();
            });

            app.MapReverseProxy();


            app.MapGet("/api/health", async (HttpContext context) =>
            {
                return HttpStatusCode.Accepted;
            });

            var cm = app.Services.GetRequiredService<ConnectionMonitor>();
            cm.Start();

            app.Run();


           
        }
    }
}
