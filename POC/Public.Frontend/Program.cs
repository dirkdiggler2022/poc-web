using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Yarp.ReverseProxy.Forwarder;

namespace Public.Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHttpForwarder();
            var socketHandler = new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                EnableMultipleHttp2Connections = true,
                ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
                ConnectTimeout = TimeSpan.FromSeconds(15),
            };
            var httpClient = new HttpMessageInvoker(socketHandler);

            static async ValueTask<Stream> DefaultConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }


            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            //builder.WebHost.ConfigureKestrel(options =>
            //{

            //    options.AllowAlternateSchemes = true;

            //});
            builder.Services.AddTunnelServices();

            var app = builder.Build();

            app.Map("/Test/{**catch-all}", async (HttpContext httpContext, IHttpForwarder forwarder, TunnelClientFactory tunnelClientFactory, CancellationToken cancellationToken) =>
            {
                var previous = DefaultConnectCallback;
                var (requests, responses) = tunnelClientFactory.GetConnectionChannel("backend1.app");

               // await requests.Reader.ReadAsync(httpContext.RequestAborted);
                // Ask for a connection
                await requests.Writer.WriteAsync(0, httpContext.RequestAborted);

                while (true)
                {
                   var stream = await responses.Reader.ReadAsync(httpContext.RequestAborted);

                    if (stream is ICloseable c && c.IsClosed)
                    {
                        // Ask for another connection
                        await requests.Writer.WriteAsync(0, cancellationToken);

                        continue;
                    }

                    return stream;
                }

               // return await previous(httpContext, cancellationToken);
                //await forwarder.SendAsync(httpContext, "http://example.com", httpClient);
            });
            app.MapReverseProxy();
            //  app.AddProxyServices();
            // Uncomment to support websocket connections
            //app.MapWebSocketTunnel("/connect-ws");

            // Auth can be added to this endpoint and we can restrict it to certain points
            // to avoid exteranl traffic hitting it
            app.MapHttp2Tunnel("/connect-h2");
                //app.MapCatchAll();
            app.Run();
        }
    }
}
