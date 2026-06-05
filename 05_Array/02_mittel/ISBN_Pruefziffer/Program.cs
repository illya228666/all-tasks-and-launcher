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
            Console.WriteLine("ISBN: Internationale Standard Buch Nummer\n");

            string eingabe = LiesIsbnOhnePruefziffer();
            int[] ziffer = new int[9];

            for (int i = 0; i < ziffer.Length; i++)
            {
                ziffer[i] = eingabe[i] - '0';
            }

            int pruefwert = BerechnePruefwert(ziffer);
            string pruefziffer = pruefwert == 10 ? "X" : pruefwert.ToString();

            Console.WriteLine("Die Pruefziffer lautet:        {0}", pruefwert);
            Console.WriteLine("Die vollstaendige ISBN lautet: {0}{1}", eingabe, pruefziffer);

            Console.Write("\nNoch einmal? (j/n): ");
            string antwort = Console.ReadLine() ?? string.Empty;
            if (antwort.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                wiederholen = false;
            }
        }
    }

    static int BerechnePruefwert(int[] ziffer)
    {
        int summe = 0;

        for (int i = 0; i < ziffer.Length; i++)
        {
            summe = summe + ziffer[i] * (i + 1);
        }

        return summe % 11;
    }

    static string LiesIsbnOhnePruefziffer()
    {
        while (true)
        {
            Console.Write("Eingabe der 9-stelligen ISBN: ");
            string input = (Console.ReadLine() ?? string.Empty).Trim();

            if (input.Length == 9 && IstZiffernfolge(input))
            {
                return input;
            }

            Console.WriteLine("Du kannst nicht richtig zaehlen! Versuche es noch einmal.");
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
