using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Public.Tests
{
    public static class AppBuilder
    {
        // pass string[] args received from Main()
        public static WebApplication Create(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json", false, true);
            builder.Services.AddReverseProxy()
                //.LoadFromMemory(configProvider.Routes, configProvider.Clusters)
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
            // configure services & other stuff

            return builder.Build();
        }
    }
}
