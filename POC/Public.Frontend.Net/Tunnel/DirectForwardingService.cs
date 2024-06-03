using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace Public.Frontend.Net.Tunnel
{
    
    public class DirectForwardingService
    {
        private readonly SocketsHttpHandler _httpHandler;
        private readonly HttpMessageInvoker _messageInvoker;
        private readonly ForwarderRequestConfig _forwarderRequestConfig;
        private readonly CustomTransformer _customTransformer;
        public DirectForwardingService()
        {
            _httpHandler = new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                EnableMultipleHttp2Connections = true,
                ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
                ConnectTimeout = TimeSpan.FromSeconds(15),
            };
            _messageInvoker = new HttpMessageInvoker(_httpHandler);
            _forwarderRequestConfig = new ForwarderRequestConfig{ ActivityTimeout = TimeSpan.FromSeconds(100) };
            _customTransformer = new CustomTransformer();
        }

        public async Task Forward(HttpContext httpContext, IHttpForwarder forwarder)
        {
            var destinationPrefix = "https://localhost:7243";
            var error = await forwarder.SendAsync(httpContext, destinationPrefix,
                _messageInvoker, _forwarderRequestConfig, _customTransformer);
            // Check if the operation was successful
            if (error != ForwarderError.None)
            {
                var errorFeature = httpContext.GetForwarderErrorFeature();
                var exception = errorFeature.Exception;
            }
            
        }
    }
    public class CustomTransformer : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(HttpContext httpContext,
            HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
        {
            // Copy all request headers
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

            // Customize the query string:
            var queryContext = new QueryTransformContext(httpContext.Request);
            //queryContext.Collection.Remove("param1");
            //queryContext.Collection["area"] = "xx2";

            // Assign the custom uri. Be careful about extra slashes when concatenating here. RequestUtilities.MakeDestinationAddress is a safe default.
            proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(proxyRequest.RequestUri.ToString(), httpContext.Request.Path, queryContext.QueryString);

            // Suppress the original request header, use the one from the destination Uri.
           // proxyRequest.Headers.Host = null;
        }
    }

}
