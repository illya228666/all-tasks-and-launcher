using System.Globalization;

namespace Helpers
{
    public class Read
    {
        static public decimal Decimal(string prompt, decimal min, decimal max, CultureInfo culture)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (decimal.TryParse(input, NumberStyles.Number, culture, out decimal value)
                    && value >= min && value <= max)
                {
                    return value;
                }

                Console.WriteLine($"Falsche Eingabe\n");
            }
        }

        static public double Double(string prompt, double min, double max)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (double.TryParse(input, out double value)
                    && value >= min && value <= max)
                {
                    return value;
                }

                Console.WriteLine($"Falsche Eingabe\n");
            }
        }

        static public int Int(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int value) && value >= min && value <= max)
                {
                    return value;
                }

                Console.WriteLine($"Falsche Eingabe.\n");
            }
        }

        static public string String(string prompt, bool allowEmpty = false)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = (Console.ReadLine() ?? string.Empty).Trim();

                if (allowEmpty || !string.IsNullOrEmpty(input))
                {
                    return input;
                }

                Console.WriteLine("Falsche Eingabe.\n");
            }
        }

        static public DateTime Date(string prompt, string format, CultureInfo culture, bool allowEmpty, out bool wasEmpty)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = (Console.ReadLine() ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(input) && allowEmpty)
                {
                    wasEmpty = true;
                    return default;
                }

                if (DateTime.TryParseExact(input, format, culture, DateTimeStyles.None, out DateTime value))
                {
                    wasEmpty = false;
                    return value;
                }

                Console.WriteLine("Falsche Eingabe.\n");
            }
        }
    }
}
