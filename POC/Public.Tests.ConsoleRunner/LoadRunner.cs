using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Public.Tests.ConsoleRunner
{
    internal class LoadRunner
    {
        public const string HOST_BASE = "https://localhost:7243";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public async Task<LoadReport> Run(int amount, bool useProxy)
        {
            Logger.Info($"starting benchmark with {amount} messages: UseProxy = {useProxy}");
            ConcurrentQueue<ResponseDetails> responses = new ConcurrentQueue<ResponseDetails>();
 
            var taskList = new List<Task>();
            var apiUrl = "https://localhost:7067/api/stonks";
            var proxyUrl = "https://localhost:7243/api/stonks";

            var requestUrl = useProxy ? proxyUrl : apiUrl;

            for (var i = 0; i < amount; i++)
            {
                taskList.Add(GetPage(requestUrl).ContinueWith((r) => responses.Enqueue(r.Result)));
            }
            // Create an instance of Stopwatch
            Stopwatch stopwatch = new Stopwatch();

            // Start the stopwatch before the method execution
            stopwatch.Start();
            await Task.WhenAll(taskList);
            stopwatch.Stop();
            var result = new LoadReport();
            result.RequestUrl = requestUrl;
            result.Failed = responses.Where(n => !n.Success).ToList();
            result.Succeeded = responses.Where(n => n.Success).ToList();

            TimeSpan elapsedTime = stopwatch.Elapsed;
            result.RunTime = elapsedTime;

            Logger.Info($"completed benchmark with {result.Failed.Count} failures, {result.Succeeded.Count} succeeded in {result.RunTime.TotalMilliseconds} ms");
            return result;
        }


        public class ResponseDetails
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
        public async Task<ResponseDetails> GetPage(string requestUrl)
        {
            var result = new ResponseDetails();
            try
            {
                var client = new HttpClient();


                var message = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                var response = await client.SendAsync(message, CancellationToken.None);

                if (!response.IsSuccessStatusCode)
                {
                    result.Message = response.ReasonPhrase;
                    Logger.Warn(result.Message);
                }
                else
                {
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                Logger.Error(result.Message);
            }

            return result;

        }

        internal class LoadReport
        {
            public TimeSpan RunTime { get; set; }
            public string RequestUrl { get; set; }
            public List<ResponseDetails> Failed { get; set; }
            public List<ResponseDetails> Succeeded { get; set; }

        }
    }
}
