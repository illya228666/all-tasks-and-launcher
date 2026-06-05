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
            Console.WriteLine("Global Trade Item Number (GTIN)");
            Console.WriteLine("bis 2009: European Article Number (EAN)\n");
            Console.WriteLine("Berechnung der Pruefziffer\n");

            string eingabe = LiesZiffernfolge("Geben Sie die 12-stellige Artikelnummer ein : ", 12);
            int[] ziffer = new int[13];

            for (int i = 0; i < eingabe.Length; i++)
            {
                ziffer[i] = eingabe[i] - '0';
            }

            int pruefziffer = BerechnePruefziffer(ziffer);
            ziffer[12] = pruefziffer;

            Console.WriteLine("Die Pruefziffer lautet                       : {0}", pruefziffer);
            Console.WriteLine("Die komplette GTIN bzw. EAN lautet          : {0}{1}", eingabe, pruefziffer);

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
        int summe = 0;

        for (int i = 0; i < 12; i++)
        {
            int faktor = (i + 1) % 2 == 0 ? 3 : 1;
            summe = summe + ziffer[i] * faktor;
        }

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
