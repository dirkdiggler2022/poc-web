using System.Text;
using Microsoft.Extensions.Logging;

namespace Public.Frontend.Net.Utilities
{
    public static class StaticLogger
    {
        private static ILogger log = ApplicationLogging.CreateLogger("Static Logger");
        public static ILogger Logger => log;

        public static string GetWrappedMessage(string message)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-".PadLeft(100, '-'));
            sb.AppendLine(message);
            sb.AppendLine("-".PadLeft(100,'-'));

            return sb.ToString();
        }

    }
}
