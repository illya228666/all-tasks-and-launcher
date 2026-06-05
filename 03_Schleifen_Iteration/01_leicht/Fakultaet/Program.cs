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
            Console.WriteLine("Fakultät berechnen\n");

            int zahl = Read.Int("Bitte geben Sie eine Zahl ein: ", 0, 20);

            long fakultaet = 1;
            int faktor = zahl;

            while (faktor > 1)
            {
                fakultaet = fakultaet * faktor;
                faktor = faktor - 1;
            }

            string berechnung;
            if (zahl == 0)
            {
                berechnung = "1";
            }
            else
            {
                berechnung = string.Empty;
                int i = zahl;
                while (i >= 1)
                {
                    berechnung = berechnung + i;
                    if (i > 1)
                        berechnung = berechnung + " * ";
                    i = i - 1;
                }
            }

            Console.WriteLine($"\nBerechnung: {berechnung}");
            Console.WriteLine($"Die Fakultät von {zahl} lautet {fakultaet}.\n");

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}
