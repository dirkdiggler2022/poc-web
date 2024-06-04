using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Public.Frontend.Net.Tunnel
{
    public static class ProxyExtensions
    {
        public static void RegisterProxy(this WebApplication app)
        {
            var configuration = app.Services.GetRequiredService<IConfiguration>();
            var proxyUrl = configuration["PublicProxyUrl"];
            var message = new HttpRequestMessage(HttpMethod.Post, $"{proxyUrl}/register-proxy");
            message.Content = new StringContent(proxyUrl);
            var client = new HttpClient();
            var response = client.Send(message);
        }
    }
}
