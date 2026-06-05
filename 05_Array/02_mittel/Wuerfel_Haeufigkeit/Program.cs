using System;
using Helpers;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        bool wiederholen = true;
        Random wuerfel = new();

        while (wiederholen)
        {
            ClearConsole();
            Console.WriteLine("Absolute Haeufigkeit der Zahlen 1 bis 6 eines Wuerfels\n");

            int anzahlWuerfe = Read.Int("Wie haeufig soll gewuerfelt werden? ", 1, 10000);
            int[] summe = new int[6];

            for (int i = 1; i <= anzahlWuerfe; i++)
            {
                int index = wuerfel.Next(1, 7);
                summe[index - 1] = summe[index - 1] + 1;
            }

            Console.WriteLine("\nDie folgende Grafik zeigt, wie haeufig eine Zahl gewuerfelt wurde.\n");
            for (int i = 0; i < summe.Length; i++)
            {
                Console.WriteLine("{0}: {1}", i + 1, new string('*', summe[i]));
            }

            int maxIndex = 0;
            for (int i = 1; i < summe.Length; i++)
            {
                if (summe[i] > summe[maxIndex])
                {
                    maxIndex = i;
                }
            }

            Console.WriteLine("\nDie am meisten gewuerfelte Zahl ist die {0} mit {1} Wuerfen.", maxIndex + 1, summe[maxIndex]);

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
