using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Public.Frontend.Net.Tunnel;
using Public.Frontend.Net.Utilities;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Public.Frontend.Net;
using Public.Frontend.Net.Configuration;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

public static class TunnelExensions
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
        return routes.MapPost(path, static async (HttpContext context, TunnelClientFactory tunnelFactory, IHostApplicationLifetime lifetime) =>
        {
            // HTTP/2 duplex stream
            if (context.Request.Protocol != HttpProtocol.Http2)
            {
                return Results.BadRequest();
            }

            var connectionKey = context.GetConnectionKey();
            var (requests, responses) = tunnelFactory.GetConnectionChannel(connectionKey);

            StaticLogger.Logger.LogInformation(StaticLogger.GetWrappedMessage($"{connectionKey} connected via http2"));

            await requests.Reader.ReadAsync(context.RequestAborted);

            var stream = new DuplexHttpStream(context);

            using var reg = lifetime.ApplicationStopping.Register(() => stream.Abort());

            // Keep reusing this connection while, it's still open on the backend
            // JC - Can't safely re-use them
            // while (!context.RequestAborted.IsCancellationRequested)
            // {
                // Make this connection available for requests
                await responses.Writer.WriteAsync(stream, context.RequestAborted);

                await stream.StreamCompleteTask;

                stream.Reset();
            // }

            return EmptyResult.Instance;
        });
    }

    public static IEndpointConventionBuilder MapWebSocketTunnel(this IEndpointRouteBuilder routes, string path)
    {
        var conventionBuilder = routes.MapGet(path, static async (HttpContext context,TunnelClientFactory tunnelFactory, InMemoryConfigProvider proxyConfigProvider, IHostApplicationLifetime lifetime) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return Results.BadRequest();
            }


            var connectionKey = context.GetConnectionKey();
            var (requests, responses) = tunnelFactory.GetConnectionChannel(connectionKey);
            CreateDynamicRoute(connectionKey,proxyConfigProvider);
            StaticLogger.Logger.LogInformation(StaticLogger.GetWrappedMessage($"{connectionKey} connected via websockets"));

            await requests.Reader.ReadAsync(context.RequestAborted);

            var ws = await context.WebSockets.AcceptWebSocketAsync();

            var stream = new WebSocketStream(ws);

            // We should make this more graceful
            using var reg = lifetime.ApplicationStopping.Register(() => stream.Abort());

            // Keep reusing this connection while, it's still open on the backend
            // JC - Don't reuse streams until able to safely reuse them 
            // while (ws.State == WebSocketState.Open)
            // {
                // Make this connection available for requests
                await responses.Writer.WriteAsync(stream, context.RequestAborted);

                await stream.StreamCompleteTask;

                stream.Reset();
            // }

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

    public static IEndpointConventionBuilder MapAgentInfoEndpoint(this IEndpointRouteBuilder routes, string path)
    {
        return routes.Map(path, static async (HttpContext context, TunnelClientFactory tunnelClientFactory) =>
        {

            var connectionKey = context.Request.RouteValues["agent"].ToString();
            return connectionKey;

        });
    }

    public static IEndpointConventionBuilder MapAgentsInfoEndpoint(this IEndpointRouteBuilder routes, string path)
    {
        return routes.Map(path, static async (HttpContext context, TunnelClientFactory tunnelClientFactory) =>
        {
            var hostInfo = $"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}";
            var agents = string.Join(",",tunnelClientFactory.GetConnectectClients());

            return $"{hostInfo} - {agents}";
            //var connectionKey = context.Request.RouteValues["agent"].ToString();
            //return connectionKey;

        });
    }

    static void CreateDynamicRoute(string connectionKey, InMemoryConfigProvider proxyConfigProvider)
    {
        var (routeConfig,clusterConfig) = GetRouteConfig(connectionKey);
        var routes = proxyConfigProvider.GetConfig().Routes.ToList();
        if (routes.Any(n => n.RouteId.Equals($"{connectionKey}-route")))
            return;

        var clusters = proxyConfigProvider.GetConfig().Clusters.ToList();
        routes.Add(routeConfig);
        clusters.Add(clusterConfig);
        proxyConfigProvider.Update(routes,clusters);

        //var jsonString = JsonSerializer.Serialize(proxyConfigProvider.GetConfig());
        //File.WriteAllText("c:/Temp/routes.txt",jsonString);

    }
    static (RouteConfig RouteConfig, ClusterConfig ClusterConfig) GetRouteConfig(string connectionKey)
    {
        var cluster = new ClusterConfig
        {
            ClusterId = $"{connectionKey}-cluster",
            Destinations = new ConcurrentDictionary<string, DestinationConfig>(new List<KeyValuePair<string, DestinationConfig>> { new("default", GetDestinationConfig(connectionKey)) })
        };

        var route = new RouteConfig
        {
            RouteId = $"{connectionKey}-route",
            ClusterId = cluster.ClusterId,
            Match = new RouteMatch
            {
                Path = "{**catch-all}",
                Headers = new List<RouteHeader>
                {
                    new()
                    {
                        Name = GlobalConstants.CONNECTION_KEY_HEADER_NAME,
                        Values = new List<string>{connectionKey}
                    }
                }
            },

        };

        (RouteConfig RouteConfig, ClusterConfig ClusterConfig) result = (route, cluster);
        return result;
    }
    static DestinationConfig GetDestinationConfig(string connectionKey)
    {
        var result = new DestinationConfig
        {
            // Address = $"http://{connectionKey}.proxy.{Guid.NewGuid()}.app"
            Address = $"http://backend1.app"
        };
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