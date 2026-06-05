using System;
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
            Console.WriteLine("Bevoelkerungsentwicklung\n");
            Console.WriteLine("Wann ist Land B groesser als Land A?\n");

            decimal landA = Read.Decimal("Bevoelkerung in Land A: ", 1m, decimal.MaxValue, culture);
            decimal aAenderung = Read.Decimal("Veraenderung in % fuer Land A: ", -100m, 100m, culture);
            Console.WriteLine();
            decimal landB = Read.Decimal("Bevoelkerung in Land B: ", 1m, decimal.MaxValue, culture);
            decimal bAenderung = Read.Decimal("Veraenderung in % fuer Land B: ", -100m, 100m, culture);
            Console.WriteLine();
            int jahr = Read.Int("Aktuelles Jahr: ", 1, 9999);

            if (landB > landA)
            {
                Console.WriteLine("\nFehler: Land A muss groesser als Land B sein.");
            }
            else
            {
                int jahre = 0;

                Console.WriteLine();
                Console.WriteLine("Jahr   Land A            Land B");
                Console.WriteLine("---------------------------------------");
                Console.WriteLine($"{jahr,4}   {landA,12:F0}      {landB,12:F0}");

                while (landA > landB && jahre < 5000)
                {
                    landA = landA + (landA * aAenderung / 100m);
                    landB = landB + (landB * bAenderung / 100m);
                    jahr = jahr + 1;
                    jahre = jahre + 1;

                    Console.WriteLine($"{jahr,4}   {landA,12:F0}      {landB,12:F0}");
                }

                Console.WriteLine();
                if (landB > landA)
                {
                    Console.WriteLine($"Nach {jahre} Jahren ist Land B groesser als Land A.");
                }
                else
                {
                    Console.WriteLine("Land B wird im berechneten Zeitraum nicht groesser als Land A.");
                }
            }

            Console.Write("\nNoch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}
