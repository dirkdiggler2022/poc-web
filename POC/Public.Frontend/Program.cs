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
            var transformer = new CustomTransformer(); // or HttpTransformer.Default;
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


            app.MapForwarder("/{**catch-all}", "http://backend1.app", requestOptions, transformer, httpClient);

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

        internal class CustomTransformer : HttpTransformer
        {
            /// <summary>
            /// A callback that is invoked prior to sending the proxied request. All HttpRequestMessage
            /// fields are initialized except RequestUri, which will be initialized after the
            /// callback if no value is provided. The string parameter represents the destination
            /// URI prefix that should be used when constructing the RequestUri. The headers
            /// are copied by the base implementation, excluding some protocol headers like HTTP/2
            /// pseudo headers (":authority").
            /// </summary>
            /// <param name="httpContext">The incoming request.</param>
            /// <param name="proxyRequest">The outgoing proxy request.</param>
            /// <param name="destinationPrefix">The uri prefix for the selected destination server which can be used to create
            /// the RequestUri.</param>
            public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
            {
                // Copy all request headers
                await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

                //// Customize the query string:
                var queryContext = new QueryTransformContext(httpContext.Request);
                //queryContext.Collection.Remove("param1");
                //queryContext.Collection["area"] = "xx2";

                //// Assign the custom uri. Be careful about extra slashes when concatenating here. RequestUtilities.MakeDestinationAddress is a safe default.
                proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress("http://backend1.app", httpContext.Request.Path, queryContext.QueryString);

                //// Suppress the original request header, use the one from the destination Uri.
                //proxyRequest.Headers.Host = null;
            }
        }
    }
}
