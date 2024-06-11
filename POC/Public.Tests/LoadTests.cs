using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Public.Tests
{
    public class LoadTests
    {

        public class ResponseDetails
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
        public async Task<ResponseDetails> GetPage()
        {
            var result = new ResponseDetails();
            var client = new HttpClient();
            var apiUrl = "https://localhost:7067/api/stonks";
            var proxyUrl = "https://localhost:7243/api/stonks";
            var message = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            var response = await client.SendAsync(message, CancellationToken.None);
            var failureCount = 0;
            if (!response.IsSuccessStatusCode)
            {
                result.Message = response.ReasonPhrase;
            }
            else
            {
                result.Success = true;
            }

            return result;


            //Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task LoadServer()
        {
            ConcurrentQueue<ResponseDetails> result = new ConcurrentQueue<ResponseDetails>();
            var requestCount = 1000;
            var taskList = new List<Task>();
            for (var i = 0; i < requestCount; i++)
            {
                taskList.Add(GetPage().ContinueWith((r)=>result.Enqueue(r.Result)));
            }

            await Task.WhenAll(taskList);
            var failures = result.Where(n => !n.Success);
            var failureCount = failures.Count();
            var successCount = result.Count(n=>n.Success);
            var totalCount = failureCount + successCount;
            Assert.Equal(totalCount,requestCount);
        }
    }
}
