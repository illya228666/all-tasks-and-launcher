
using System.Globalization;
using Helpers;
class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
        bool wiederholen = true;
        while (wiederholen)
        {
            Console.Clear();
            Console.WriteLine("Zahlenvergleich\n");
            int n = Read.Int("Wie viele Zahlen möchten Sie vergleichen? ", 2, int.MaxValue);
            Console.WriteLine();

            List<decimal> zahlen = new List<decimal>(capacity: n);
            for (int i = 1; i <= n; i++)
            {
                decimal value = Read.Decimal($"Eingabe der {i}. Zahl: ", decimal.MinValue, decimal.MaxValue, culture);
                zahlen.Add(value);
            }
            Console.Clear();
            Console.WriteLine("Zahlenvergleich\n");

            decimal kleinste = zahlen.Min();
            bool alleGleich = zahlen.Distinct().Count() == 1;

            Console.WriteLine("Eingegebene Werte:");
            for (int i = 0; i < zahlen.Count; i++)
                Console.WriteLine($"{i + 1}) {zahlen[i]}");

            Console.WriteLine();
            if (alleGleich)
            {
                Console.WriteLine("Die eingegebenen Zahlen sind gleich.");
            }
            else
            {
                int anzahlMin = zahlen.Count(x => x == kleinste);

                Console.WriteLine($"Die kleinste Zahl ist {kleinste}.");
                if (anzahlMin > 1)
                    Console.WriteLine($"Hinweis: Die kleinste Zahl kommt {anzahlMin}× vor.");
            }

            Console.Write("\nNoch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}