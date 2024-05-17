using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Cryptography.Xml;
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
            //allow alternate schemes so we can use ngrok
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AllowAlternateSchemes = true;
                
            });
            builder.Services.AddHttpForwarder();

            var tx = new CustomTransformer();
            builder.Services.AddTunnelServices();

            var app = builder.Build();

            ApplicationLogging.LoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

            app.MapForwarder("/{**catch-all}", "http://backend1.app", (transform) =>
            {
                transform.RequestTransforms.Add(new RequestFuncTransform(async (ctx) =>
                {
                   await tx.TransformRequestAsync(ctx.HttpContext, ctx.ProxyRequest, ctx.DestinationPrefix,
                        ctx.CancellationToken);
                }));
            });
            //var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100),  };

           // app.MapForwarder("/{**catch-all}", "http://backend1.app");
            app.MapHttp2Tunnel("/connect-h2/Agent1");
            app.MapHttp2Tunnel("/connect-h2/Agent2");
            app.MapHttp2Tunnel("/connect-h2/Agent3");

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
