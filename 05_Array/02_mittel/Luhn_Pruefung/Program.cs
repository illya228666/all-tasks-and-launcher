using System;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        bool wiederholen = true;

        while (wiederholen)
        {
            ClearConsole();
            Console.WriteLine("Ermittlung der Pruefziffer einer 16-stelligen Kreditkartennummer");
            Console.WriteLine("===============================================================\n");

            string eingabe = LiesZiffernfolge("Geben Sie Ihre Kreditkartennummer ein: ", 16);
            int[] ziffer = new int[16];

            for (int i = 0; i < eingabe.Length; i++)
            {
                ziffer[i] = eingabe[i] - '0';
            }

            int pruefziffer = BerechnePruefziffer(ziffer);
            bool gueltig = pruefziffer == ziffer[15];

            Console.WriteLine("Die Pruefziffer lautet {0}.", pruefziffer);
            Console.WriteLine("Die Kreditkartennummer {0} ist {1}.", eingabe, gueltig ? "gueltig" : "ungueltig");

            Console.Write("\nNoch einmal? (j/n): ");
            string antwort = Console.ReadLine() ?? string.Empty;
            if (antwort.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                wiederholen = false;
            }
        }
    }

    static int BerechnePruefziffer(int[] ziffer)
    {
        int summeUngerade = 0;
        int summeGerade = 0;

        for (int i = 0; i < 15; i++)
        {
            int stelle = i + 1;

            if (stelle % 2 == 1)
            {
                int wert = ziffer[i] * 2;
                summeUngerade = summeUngerade + wert / 10 + wert % 10;
            }
            else
            {
                summeGerade = summeGerade + ziffer[i];
            }
        }

        int summe = summeUngerade + summeGerade;
        return (10 - (summe % 10)) % 10;
    }

    static string LiesZiffernfolge(string prompt, int laenge)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = (Console.ReadLine() ?? string.Empty).Trim();

            if (input.Length == laenge && IstZiffernfolge(input))
            {
                return input;
            }

            Console.WriteLine("Falsche Eingabe. Bitte genau {0} Ziffern eingeben.\n", laenge);
        }
    }

    static bool IstZiffernfolge(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    static void ClearConsole()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }
}
