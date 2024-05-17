using System.Net;
using System.Net.Http;

namespace Public.Tests
{
    public class MessageTests
    {
        public const string HOST_BASE = "https://localhost:7243";
        [Fact]
        public async Task Test1()
        {
            Thread thread1 = new Thread(new ParameterizedThreadStart(ConnectHostByKey));
            thread1.Start("myhost");

            Thread thread2 = new Thread(new ParameterizedThreadStart(ConnectHostByKey));
            thread2.Start("myhost");

            Thread thread3 = new Thread(new ParameterizedThreadStart(ConnectHostByKey));
            thread3.Start("myhost");
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

        void ConnectHostByKey(object state)
        {
            var key = state as string;
            var client = GetHttp2Client();
            var message = new HttpRequestMessage(HttpMethod.Post, new Uri($"{ConnectUrl}?host={key}"));
            message.Version = HttpVersion.Version20;
            var result = client.SendAsync(message).Result;
           
        }

        string ConnectUrl
        {
            get { return $"{HOST_BASE}/connect-h2"; }
        }
    }
}