namespace Public.Tests.ConsoleRunner
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var requests = 100; //default
            bool useProxy = true;

            if (args.Length > 0 && int.TryParse(args[0], out int requestCount))
                requests = requestCount;
            if(args.Length > 1 && bool.TryParse(args[1],out bool shouldUseProxy))
                useProxy = shouldUseProxy;

            var runner = new LoadRunner();
            var response = await runner.Run(requests,useProxy);
            Console.Out.WriteLine("Completed");
            Console.Out.WriteLine($"{response.Succeeded.Count + response.Failed.Count} messages to {response.RequestUrl}");
            Console.Out.WriteLine($"{response.Failed.Count} failures");
            Console.Out.WriteLine($"{response.Succeeded.Count} succeeded");
            Console.Out.WriteLine($"Elapsed Time = {response.RunTime.TotalMilliseconds} ms");
            foreach (var item in response.Failed)
            {
                Console.Out.WriteLine(item.Message);
            }
            Console.In.ReadLine();
        }
    }
}
