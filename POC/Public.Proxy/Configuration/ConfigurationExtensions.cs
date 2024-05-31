using System.Net;

namespace Public.Proxy.Configuration
{
    public static class ConfigurationExtensions
    {
       
        public static IEndpointConventionBuilder MapAgentRoutes(this IEndpointRouteBuilder routes, string path)
        {
            return routes.Map(path, (HttpContext context) =>
            {

                return HttpStatusCode.Accepted;
            });
        }
    }
}
