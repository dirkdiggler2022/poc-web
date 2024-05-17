using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Public.Tests.ConsoleRunner
{
    internal class LoadRunner
    {
        public const string HOST_BASE = "https://localhost:7243";
    
        public void Run()
        {
            Task.Run(() => ConnectHostByKey("myhost").Start());
            Task.Run(() => ConnectHostByKey("myhost2").Start());
            Task.Run(() => ConnectHostByKey("myhost3").Start());
            //ConnectHostByKey("myhost2");
            //ConnectHostByKey("myhost3");
        }

        HttpClient GetHttp2Client()
        {
            HttpClient result = new HttpClient
            {
                DefaultRequestVersion = HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
            return result;
        }

        async Task ConnectHostByKey(string key)
        {
            var client = GetHttp2Client();
            var message = new HttpRequestMessage(HttpMethod.Post, new Uri($"{ConnectUrl}?host={key}"));
            message.Version = HttpVersion.Version20;
            await client.SendAsync(message);
        }

        string ConnectUrl
        {
            get { return $"{HOST_BASE}/connect-h2"; }
        }
    }
}
