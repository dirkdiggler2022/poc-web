namespace Private.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddLog4Net("log4net.config");

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var url = builder.Configuration["Tunnel:Url"]!;

            //overload command line arg for url
            if (args.Length > 0 && Uri.TryCreate(args[0].ToString(), UriKind.Absolute, out var test))
                url = test.ToString();


            builder.WebHost.UseTunnelTransport(url, options =>
            {
                options.Transport = url.Contains("connect-h2") ? TransportType.HTTP2 : TransportType.WebSockets;
            });

            var app = builder.Build();

            app.MapReverseProxy();

            app.Run();
        }
    }
}
