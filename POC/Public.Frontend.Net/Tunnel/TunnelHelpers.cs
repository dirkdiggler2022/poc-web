using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Public.Frontend.Net.Tunnel
{
    public static class TunnelHelpers
    {
        //below are helpers to get the correct host, we do not know exactly how we will
        // end up doing this, but it will suffice for dev

        //gets key when connect is called to set up initial connection
        public static string GetConnectionKey(this HttpContext context)
        {
            //for testing
            //var result = context.Request.Query["host"][0];
            var result = context.Request.RouteValues["agent"].ToString();
            // var result = context.Request.Host.ToString();
            return result;
        }

        //gets the connection key when calls to proxy are made 
        public static string? GetConnectionKey(this SocketsHttpConnectionContext context)
        {
            // var hostHeader = "X-Forwarded-Host";
            var hostHeader = "host-param";
            string? result = null;
            if (context.InitialRequestMessage.Headers.TryGetValues(hostHeader, out var tryValues))
                result = tryValues.SingleOrDefault() ?? string.Empty;
            return result;

        }
    }
}
