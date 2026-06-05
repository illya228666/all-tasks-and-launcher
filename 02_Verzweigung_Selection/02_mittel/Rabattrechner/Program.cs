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
            Console.WriteLine("Rabattrechner\n");

            Console.Write("Produkt: ");
            string produkt = Console.ReadLine() ?? string.Empty;

            decimal ePreis = Read.Decimal("Einzelpreis: ", 0m, decimal.MaxValue, culture);

            int menge = Read.Int("Menge: ", 1, int.MaxValue);

            decimal rabatt = Read.Decimal("Rabatt [%]: ", 0m, 100m, culture);

            Console.Clear();

            decimal gPreis = ePreis * menge;
            decimal gRabatt = gPreis * rabatt / 100m;
            decimal nettoPreis = gPreis - gRabatt;

            Console.WriteLine($"Für das Produkt \"{produkt}\" ergibt sich folgende Berechnung mit einem Rabattsatz von {rabatt:F2} %:\n");
            Console.WriteLine($"Der Kunde kauft: {menge} Stück {produkt} zu einem Einzelpreis von {ePreis:F2} €\n");
            Console.WriteLine($"Gesamtpreis ohne Rabatt: {gPreis:F2} €");
            Console.WriteLine($"Rabattbetrag: {gRabatt:F2} €");
            Console.WriteLine($"Gesamtpreis nach Rabattabzug: {nettoPreis:F2} €\n");

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}
