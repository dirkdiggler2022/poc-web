namespace Private.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var url = builder.Configuration["Tunnel:Url"]!;

            builder.WebHost.UseTunnelTransport(url);
       
            var app = builder.Build();
        
            app.MapReverseProxy();

            app.Run();
        }
    }
}
