using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Public.Frontend.Net.Utilities
{
    public static class HelperExtensions
    {
        //below are helpers to get the correct host, we do not know exactly how we will
        // end up doing this, but it will suffice for dev

        //gets key when connect is called to set up initial connection, this is a post
        public static string GetConnectionKey(this HttpContext context)
        {
            //if you want to use default, make sure you are not passing header
            string result = GlobalConstants.DEFAULT_TEST_CONNECTION_KEY;
            if (context.Request.RouteValues.TryGetValue("connectionKey", out var testValue))
                result = testValue?.ToString();
            return result;
   
        }

        //gets the connection key when calls to proxy are made 
        public static string GetConnectionKey(this SocketsHttpConnectionContext context)
        {
            //if you want to use default, make sure you are not passing header
            string result = GlobalConstants.DEFAULT_TEST_CONNECTION_KEY;
            if (context.InitialRequestMessage.Headers.TryGetValues(GlobalConstants.CONNECTION_KEY_HEADER_NAME, out var tryValues))
                result = tryValues.SingleOrDefault() ?? string.Empty;
           return result;
          
        }
    }
}
