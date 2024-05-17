namespace Public.Tests.ConsoleRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var runner = new LoadRunner();
            runner.Run();
            Console.Out.WriteLine("Completed");
            Console.In.ReadLine();
        }
    }
}
