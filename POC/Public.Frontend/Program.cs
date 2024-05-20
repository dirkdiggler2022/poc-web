using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
using Microsoft.AspNetCore.Http;
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

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                .AddTransforms(builderContext =>
                {

                    builderContext.AddRequestTransform(async (transformContext) =>
                    {
                        var queryContext = new QueryTransformContext(transformContext.HttpContext.Request);
 

                        if (transformContext.HttpContext.Request.RouteValues.ContainsKey("agent"))
                        {
                            var headerValue = transformContext.HttpContext.Request.RouteValues["agent"].ToString();
                            transformContext.ProxyRequest.Headers.Add("host-param", headerValue);

                            var path = transformContext.HttpContext.Request.Path.ToString().Replace($"/{headerValue}", "");
                            transformContext.ProxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress("http://backend1.app", path, queryContext.QueryString);
                            //  proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress("http://backend1.app", httpContext.Request.Path, queryContext.QueryString);
                        }
                        
                    });
                });


            builder.Services.AddTunnelServices();

            var app = builder.Build();

            app.MapReverseProxy();

            // Uncomment to support websocket connections
            app.MapWebSocketTunnel("/connect-ws");

            // Auth can be added to this endpoint and we can restrict it to certain points
            // to avoid exteranl traffic hitting it
            app.MapHttp2Tunnel("/connect-h2/{agent}");

            app.Run();

        }


    }


    /// <summary>
    /// Custom request transformation
    /// </summary>
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
            //var tenant = $"{httpContext.Request?.RouteValues?["tenant"]}";
            // Customize the query string:
            var queryContext = new QueryTransformContext(httpContext.Request);

            if (queryContext.Collection.TryGetValue("host", out var host))
            {
                proxyRequest.Headers.Add("host-param", new List<string?> { host});
                queryContext.Collection.Remove("host");
            }
            
            //proxyRequest.Headers.Add("tenant", new List<string?>{ tenant });
            //queryContext.Collection.Remove("param1");
            //queryContext.Collection["area"] = "xx2";
            //var path = httpContext.Request.Path.ToString().Replace($"/{tenant}", "");
            
            // Assign the custom uri. Be careful about extra slashes when concatenating here. RequestUtilities.MakeDestinationAddress is a safe default.
            proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress("http://backend1.app", httpContext.Request.Path, queryContext.QueryString);

            // Suppress the original request header, use the one from the destination Uri.
            proxyRequest.Headers.Host = null;
        }
    }
}
