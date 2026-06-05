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
            Console.WriteLine("Prozentsatz-Rechner\n");
            decimal kapital = Read.Decimal("Kapital: ", 0m, decimal.MaxValue, culture);
            decimal zinsen = Read.Decimal("Zinsen: ", decimal.MinValue, decimal.MaxValue, culture);
            Console.Clear();
            if (kapital == 0)
            {
                Console.WriteLine("Fehler bei der Eingabe");
            }
            else
            {
                decimal prozentsatz = (zinsen * 100m) / kapital;
                Console.WriteLine("Ergebnis der Berechnung:\n");
                Console.WriteLine($"Kapital: {kapital:F2} €");
                Console.WriteLine($"Zinsen: {zinsen:F2} €");
                Console.WriteLine($"Prozentsatz: {prozentsatz:F2} %");
            }
            Console.Write("\nNoch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}