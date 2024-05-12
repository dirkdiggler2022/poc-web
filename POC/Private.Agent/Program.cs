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

            //app.Use(async (context, next) =>
            //{
            //    context.Request.Headers.Add("x-my-custom-header", "middleware response");
            //    // context.Response.Headers.Add("x-my-custom-header", "middleware response");

            //    await next();
            //});

            app.MapReverseProxy();



            app.Run();
        }
    }
}
