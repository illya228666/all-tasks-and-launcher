using System;
using System.Globalization;
using Helpers;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");

        bool wiederholen = true;

        while (wiederholen)
        {
            ClearConsole();
            Console.WriteLine("Eingabe von Zahlen\n");

            int[] zahlen = new int[10];
            int summe = 0;

            for (int i = 0; i < zahlen.Length; i++)
            {
                zahlen[i] = Read.Int($"Wert {i + 1,2}: ", int.MinValue, int.MaxValue);
                summe = summe + zahlen[i];
            }

            Console.WriteLine("\nSumme  : {0}", summe);
            Console.WriteLine("\nEingegeben wurden in umgekehrter Reihenfolge:\n");

            for (int i = zahlen.Length - 1; i >= 0; i--)
            {
                double anteil = summe == 0 ? 0 : (double)zahlen[i] / summe * 100;
                Console.WriteLine("{0,-4} (Anteil: {1})", zahlen[i], anteil.ToString("F2", culture));
            }

            Console.Write("\nNoch einmal? (j/n): ");
            string antwort = Console.ReadLine() ?? string.Empty;
            if (antwort.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                wiederholen = false;
            }
        }
    }

    static void ClearConsole()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }
}
