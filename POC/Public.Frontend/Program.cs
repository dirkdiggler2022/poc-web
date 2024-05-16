using System.Net.Http;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.Xml;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Public.Frontend.Net.Tunnel;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace Public.Frontend
{
    public class Program
    {

        //public static void Main(string[] args)
        //{
        //    var builder = WebApplication.CreateBuilder(args);

        //    builder.WebHost.ConfigureKestrel(options =>
        //    {
        //        options.AllowAlternateSchemes = true;
        //    });
        //    builder.Services.AddReverseProxy()
        //        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        //    builder.Services.AddTunnelServices();

        //    var app = builder.Build();

        //    app.MapReverseProxy();

        //    // Uncomment to support websocket connections
        //    app.MapWebSocketTunnel("/connect-ws");

        //    // Auth can be added to this endpoint and we can restrict it to certain points
        //    // to avoid exteranl traffic hitting it
        //    app.MapHttp2Tunnel("/connect-h2");

        //    app.Run();
        //}

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //allow alternate schemes so we can use ngrok
            builder.WebHost.ConfigureKestrel(options =>
            {
                 options.AllowAlternateSchemes = true;
                //options.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
            });


            builder.Services.AddHttpForwarder();

            var tx = new CustomTransformer();
            builder.Services.AddTunnelServices();

            var app = builder.Build();


            //may be useful to transform requests
            //app.MapForwarder("/{**catch-all}", "http://backend1.app", (transform) =>
            //{
            //    transform.RequestTransforms.Add(new RequestFuncTransform((ctx) =>
            //    {
            //        return tx.TransformRequestAsync(ctx.HttpContext, ctx.ProxyRequest, ctx.DestinationPrefix,
            //            ctx.CancellationToken);
            //    }));
            //});
            //var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100),  };

            app.MapForwarder("/{**catch-all}", "http://backend1.app");
            app.MapHttp2Tunnel("/connect-h2");

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
