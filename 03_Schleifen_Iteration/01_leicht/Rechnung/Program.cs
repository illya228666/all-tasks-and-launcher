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
            Console.WriteLine("Rechnung\n");

            decimal gesamtpreisNetto = 0m;

            while (true)
            {
                int menge = Read.Int("Bitte Artikelmenge eingeben (0 = Ende): ", 0, int.MaxValue);

                if (menge == 0)
                    break;

                decimal einzelpreis = Read.Decimal("Bitte Einzelpreis eingeben: ", 0m, decimal.MaxValue, culture);
                gesamtpreisNetto = gesamtpreisNetto + (menge * einzelpreis);
            }

            decimal mehrwertsteuer = gesamtpreisNetto * 0.19m;
            decimal gesamtpreisBrutto = gesamtpreisNetto + mehrwertsteuer;

            Console.WriteLine();
            Console.WriteLine($"Gesamtpreis netto         {gesamtpreisNetto,10:F2} Euro");
            Console.WriteLine($"+ 19 % Mehrwertsteuer     {mehrwertsteuer,10:F2} Euro");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"Gesamtpreis brutto        {gesamtpreisBrutto,10:F2} Euro\n");

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}
