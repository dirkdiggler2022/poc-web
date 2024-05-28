using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Public.Tests
{
    public class LoadTests
    {
        public const string HOST_BASE = "https://localhost:7243";
        [Fact]
        public async Task Connect()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(HOST_BASE));
            var respose = await client.SendAsync(request);
            Assert.True(respose.IsSuccessStatusCode);

        }

       // public async

        [Fact]
        public async Task RunLoad()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 4000; i++)
            {
                tasks.Add(Connect());
            }
            tasks.Add(Connect());
   
            var responses = Task.WaitAll(tasks.ToArray(), TimeSpan.FromMinutes(1));

        }
    }
}
