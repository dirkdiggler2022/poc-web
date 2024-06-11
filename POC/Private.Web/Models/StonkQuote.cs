namespace Private.Web.Models
{
    public class StonkQuote
    {
        public string Symbol { get; set; }
        public double Current { get; set; }
        public double High { get; set; }
        public double Low { get; set; }


        public static List<StonkQuote> GetQuotes(int count)
        {
            List<StonkQuote> result = new List<StonkQuote>();
            List<string> randomStrings = GenerateRandomStrings(count, 3);
            var rand = new Random();
            foreach (var symbol in randomStrings)
            {
                var quote = new StonkQuote();
                quote.Symbol = symbol;
                quote.Current = rand.NextDouble() * rand.Next(1, 3);
                quote.High = rand.NextDouble() * rand.Next(1, 3);
                quote.Low = rand.NextDouble() * rand.Next(1, 3);
                result.Add(quote);

            }

            return result;
        }

        static List<string> GenerateRandomStrings(int count, int stringLength)
        {
            // List to store the generated strings
            List<string> randomStrings = new List<string>();

            // Characters to choose from (a to z)
            char[] chars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

            // Create an instance of Random
            Random random = new Random();

            for (int i = 0; i < count; i++)
            {
                char[] stringChars = new char[stringLength];

                for (int j = 0; j < stringLength; j++)
                {
                    stringChars[j] = chars[random.Next(chars.Length)];
                }

                randomStrings.Add(new string(stringChars));
            }

            return randomStrings;
        }
    }
}
