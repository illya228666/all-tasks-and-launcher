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
        Random zufallszahl = new();

        while (wiederholen)
        {
            ClearConsole();
            Console.WriteLine("Minimum, Maximum, Mittelwert\n");

            int anzahl = Read.Int("Wie viele Zufallszahlen sollen erzeugt werden? ", 1, 1000);
            int obergrenze = Read.Int("Obergrenze (einschliesslich): ", 1, int.MaxValue - 1);
            int[] zahl = new int[anzahl];

            for (int i = 0; i < zahl.Length; i++)
            {
                zahl[i] = zufallszahl.Next(0, obergrenze + 1);
                Console.WriteLine("Die {0}. Zahl lautet: {1}", i + 1, zahl[i]);
            }

            int minimum = zahl[0];
            int maximum = zahl[0];
            int minimumIndex = 0;
            int maximumIndex = 0;
            int summe = 0;

            for (int i = 0; i < zahl.Length; i++)
            {
                summe = summe + zahl[i];

                if (zahl[i] < minimum)
                {
                    minimum = zahl[i];
                    minimumIndex = i;
                }

                if (zahl[i] > maximum)
                {
                    maximum = zahl[i];
                    maximumIndex = i;
                }
            }

            double mittelwert = (double)summe / zahl.Length;

            Console.WriteLine("\nDas Minimum der Zahlen lautet    : {0}", minimum);
            Console.WriteLine("Das Maximum der Zahlen lautet    : {0}", maximum);
            Console.WriteLine("Die Summe der Zahlen lautet      : {0}", summe);
            Console.WriteLine("Der Mittelwert der Zahlen lautet : {0}", mittelwert.ToString("F2", culture));
            Console.WriteLine("\nDas Minimum der Zahlen lautet {0} und steht an {1}. Stelle.", minimum, minimumIndex + 1);
            Console.WriteLine("Das Maximum der Zahlen lautet {0} und steht an {1}. Stelle.", maximum, maximumIndex + 1);

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
