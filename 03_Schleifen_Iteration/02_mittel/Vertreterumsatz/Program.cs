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
            Console.WriteLine("Berechnung des Tagesumsatzes aller Vertreter");
            Console.WriteLine("============================================\n");

            decimal gesamtumsatzsumme = 0m;

            while (true)
            {
                int vertreterNr = Read.Int("Bitte Vertreter-Nr. eingeben (Ende = 0): ", 0, int.MaxValue);

                if (vertreterNr == 0)
                    break;

                decimal summeVertreter = 0m;

                while (true)
                {
                    decimal umsatz = Read.Decimal($"Bitte Umsatz fuer Vertreter {vertreterNr} eingeben (Ende = 0): ", 0m, decimal.MaxValue, culture);

                    if (umsatz == 0m)
                        break;

                    summeVertreter = summeVertreter + umsatz;
                }

                Console.WriteLine($"Summe fuer Vertreter {vertreterNr}: {summeVertreter:F2} Euro\n");
                gesamtumsatzsumme = gesamtumsatzsumme + summeVertreter;
            }

            Console.WriteLine($"\nGesamtumsatzsumme: {gesamtumsatzsumme:F2} Euro\n");

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}
