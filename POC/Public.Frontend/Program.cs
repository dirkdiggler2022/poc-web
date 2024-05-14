using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Channels;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace Public.Frontend
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var tunnelClientFactory = new TunnelClientFactory();
            builder.Services.AddSingleton(tunnelClientFactory);
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
            var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };


            var app = builder.Build();
            app.MapPost("/connect-h2",
                static async (HttpContext context, TunnelClientFactory tunnelFactory, string host,
                    IHostApplicationLifetime lifetime) =>
                {


                    // HTTP/2 duplex stream
                    if (context.Request.Protocol != HttpProtocol.Http2)
                    {
                        return Results.BadRequest();
                    }

                    var (requests, responses) = tunnelFactory.GetConnectionChannel(host);

                    await requests.Reader.ReadAsync(context.RequestAborted);

                    var stream = new DuplexHttpStream(context);

                    using var reg = lifetime.ApplicationStopping.Register(() => stream.Abort());

                    // Keep reusing this connection while, it's still open on the backend
                    while (!context.RequestAborted.IsCancellationRequested)
                    {
                        // Make this connection available for requests
                        await responses.Writer.WriteAsync(stream, context.RequestAborted);

                        await stream.StreamCompleteTask;

                        stream.Reset();
                    }
                    return TunnelExensions.EmptyResult.Instance;
                });





            socketHandler.ConnectCallback = async (context, cancellationToken) =>
            {

                var (requests, responses) = tunnelClientFactory.GetConnectionChannel("backend1.app");
                // Ask for a connection
                await requests.Writer.WriteAsync(0, cancellationToken);

                while (true)
                {

                    var stream = await responses.Reader.ReadAsync(cancellationToken);

                    if (stream is ICloseable c && c.IsClosed)
                    {
                        // Ask for another connection
                        await requests.Writer.WriteAsync(0, cancellationToken);

                        continue;
                    }
                    return stream;
                }


            };


           // app.MapForwarder("/{**catch-all}", "http://backend1.app", requestOptions, transformer, httpClient);
            app.MapForwarder("/{**catch-all}", "http://backend1.app", requestOptions, HttpTransformer.Default, httpClient);
            app.Run();

        }


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

    }
}
