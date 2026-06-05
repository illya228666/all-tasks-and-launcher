using System;
using System.Globalization;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
        bool wiederholen = true;

        while (wiederholen)
        {
            Console.Clear();
            Console.WriteLine("Schreibtischtest zu Schleifen\n");

            int a = 3;
            int b = 8;
            int c = 0;

            while (a <= b)
            {
                c = c + 1;
                a = a + 1;
            }

            Console.WriteLine("Übung 1");
            Console.WriteLine($"Ausgabe c: {c}\n");

            int d = 0;
            int e = 5;
            int f = 1;
            int schritte = 0;
            string verlauf = $"{d}";

            while (d != e && schritte < 8)
            {
                d = d + f;
                f = f + 1;
                schritte = schritte + 1;
                verlauf = verlauf + $" -> {d}";
            }

            Console.WriteLine("Übung 2");
            Console.WriteLine($"Zwischenwerte d: {verlauf}");
            if (d == e)
            {
                Console.WriteLine($"Ausgabe d: {d}\n");
            }
            else
            {
                Console.WriteLine("Ergebnis: Endlosschleife (d trifft den Wert 5 nicht).\n");
            }

            int g = 1;
            int h = 5;
            int i = h;

            while (i > 0)
            {
                g = g * i;
                i = i - 1;
            }

            Console.WriteLine("Übung 3");
            Console.WriteLine($"Ausgabe g: {g}\n");

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}
