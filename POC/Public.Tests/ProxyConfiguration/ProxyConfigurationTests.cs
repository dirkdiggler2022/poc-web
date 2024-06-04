using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace Public.Tests.ProxyConfiguration
{
    public class ProxyConfigurationTests
    {

        IProxyConfigProvider _proxyConfigProvider;
        private WebApplication _app;
        public ProxyConfigurationTests()
        {
           _app = AppBuilder.Create(new string[]{});
        }

        [Fact]
        public async Task DoStuff()
        {
            var proxyConfigProvider = _app.Services.GetRequiredService<IProxyConfigProvider>();
            //proxyConfigProvider.GetConfig().Routes;
            Assert.NotNull(proxyConfigProvider);
        }
    }
}
