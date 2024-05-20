using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace Private.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
            //.AddTransforms(builderContext =>
            //{

            //    builderContext.AddRequestTransform(async (transformContext) =>
            //    {
            //        var queryContext = new QueryTransformContext(transformContext.HttpContext.Request);


            //        //if (transformContext.HttpContext.Request.RouteValues.ContainsKey("agent"))
            //        //{
            //        //    var headerValue = transformContext.HttpContext.Request.RouteValues["agent"].ToString();
            //        //    transformContext.ProxyRequest.Headers.Add("host-param", headerValue);

            //        //    var path = transformContext.HttpContext.Request.Path.ToString()
            //        //        .Replace($"/{headerValue}", "").Replace("/Proxy1", "").Replace("/Proxy2", "");
            //        //    transformContext.ProxyRequest.RequestUri =
            //        //        RequestUtilities.MakeDestinationAddress("http://backend1.app", path,
            //        //            queryContext.QueryString);
            //        transformContext.ProxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(transformContext.DestinationPrefix, transformContext.HttpContext.Request.Path.ToString().Replace("/Proxy1","").Replace("/Agent1",""), queryContext.QueryString);
            //        //}

            //    });
       // });

            var url = builder.Configuration["Tunnel:Url"]!;

            //overload listen 
            if (args.Length > 0 && Uri.TryCreate(args[0].ToString(), UriKind.Absolute, out var test))
                url = test.ToString();


            builder.WebHost.UseTunnelTransport(url);

            var app = builder.Build();

            app.MapReverseProxy();

            app.Run();
        }
    }
}
