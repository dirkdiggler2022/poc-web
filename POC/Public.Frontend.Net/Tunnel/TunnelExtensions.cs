using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Public.Frontend.Net.Utilities;
using Yarp.ReverseProxy.Forwarder;

namespace Public.Frontend.Net.Tunnel;

public static class TunnelExtensions
{
    public static IServiceCollection AddTunnelServices(this IServiceCollection services)
    {
        var tunnelFactory = new TunnelClientFactory();
        services.AddSingleton(tunnelFactory);
        services.AddSingleton<IForwarderHttpClientFactory>(tunnelFactory);
        return services;
    }

  
    public static IEndpointConventionBuilder MapHttp2Tunnel(this IEndpointRouteBuilder routes, string path)
    {
        return routes.MapPost(path, static async (HttpContext context,TunnelClientFactory tunnelFactory, IHostApplicationLifetime lifetime) =>
        {
            //var tenant = $"{context.Request?.RouteValues?["tenant"]}";
            // HTTP/2 duplex stream
            if (context.Request.Protocol != HttpProtocol.Http2)
            {
                return Results.BadRequest();
            }

            var connectionKey = context.GetConnectionKey();

            StaticLogger.Logger.LogInformation(StaticLogger.GetWrappedMessage($"{connectionKey} connected"));

            var (requests, responses) = tunnelFactory.GetConnectionChannel(connectionKey);

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

            return EmptyResult.Instance;
        });
    }

    //not using web sockets, but we might, so we will keep this
    public static IEndpointConventionBuilder MapWebSocketTunnel(this IEndpointRouteBuilder routes, string path)
    {
        var conventionBuilder = routes.MapGet(path, static async (HttpContext context, string host, TunnelClientFactory tunnelFactory, IHostApplicationLifetime lifetime) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return Results.BadRequest();
            }

            var (requests, responses) = tunnelFactory.GetConnectionChannel(host);

            await requests.Reader.ReadAsync(context.RequestAborted);

            var ws = await context.WebSockets.AcceptWebSocketAsync();

            var stream = new WebSocketStream(ws);

            // We should make this more graceful
            using var reg = lifetime.ApplicationStopping.Register(() => stream.Abort());

            // Keep reusing this connection while, it's still open on the backend
            while (ws.State == WebSocketState.Open)
            {
                // Make this connection available for requests
                await responses.Writer.WriteAsync(stream, context.RequestAborted);

                await stream.StreamCompleteTask;

                stream.Reset();
            }

            return EmptyResult.Instance;
        });

        // Make this endpoint do websockets automagically as middleware for this specific route
        conventionBuilder.Add(e =>
        {
            var sub = routes.CreateApplicationBuilder();
            sub.UseWebSockets().Run(e.RequestDelegate!);
            e.RequestDelegate = sub.Build();
        });

        return conventionBuilder;
    }


    //below are helpers to get the correct host, we do not know exactly how we will
    // end up doing this, but it will suffice for dev

    //gets key when connect is called to set up initial connection
    public static string GetConnectionKey(this HttpContext context)
    {
        //for testing
        var result = context.Request.Query["host"][0];
       // var result = context.Request.Host.ToString();
        return result;
    }

    //gets the connection key when calls to proxy are made 
    public static string? GetConnectionKey(this SocketsHttpConnectionContext context)
    {
       // var hostHeader = "X-Forwarded-Host";
        var hostHeader = "host-param";
        string? result = null;
        if (context.InitialRequestMessage.Headers.TryGetValues(hostHeader, out var tryValues))
            result = tryValues.SingleOrDefault() ?? string.Empty;
        return result;

    }
    // This is for .NET 6, .NET 7 has Results.Empty
    internal sealed class EmptyResult : IResult
    {
        internal static readonly EmptyResult Instance = new();

        public Task ExecuteAsync(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }
    }
}