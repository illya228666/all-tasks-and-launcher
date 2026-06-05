using System;
using System.Globalization;
using System.Threading;
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
            Console.WriteLine("Zahlensysteme\n");
            Console.WriteLine("1) Zahl umwandeln (Dual/Oktal/Hex/Beliebig)");
            Console.WriteLine("2) Sekunden im Dualsystem zaehlen");
            Console.WriteLine();

            int auswahl = Read.Int("Auswahl: ", 1, 2);

            Console.Clear();
            if (auswahl == 1)
            {
                int dezimalzahl = Read.Int("Bitte eine natuerliche Zahl eingeben: ", 0, int.MaxValue);

                string dual = BerechneDualzahl(dezimalzahl);
                string oktal = BerechneOktalzahl(dezimalzahl);
                string hexa = BerechneHexzahl(dezimalzahl);

                Console.WriteLine($"\nDezimal: {dezimalzahl}");
                Console.WriteLine($"Dual:    {dual}");
                Console.WriteLine($"Oktal:   {oktal}");
                Console.WriteLine($"Hex:     {hexa}");

                int basis = Read.Int("\nBeliebige Basis (2 bis 16): ", 2, 16);
                string beliebig = BerechneInBeliebigeBasis(dezimalzahl, basis);
                Console.WriteLine($"Basis {basis}: {beliebig}");
            }
            else
            {
                ZaehleSekundenImDualsystem();
            }

            Console.Write("\nNoch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }

    static string BerechneDualzahl(int dezimalzahl)
    {
        return BerechneInBeliebigeBasis(dezimalzahl, 2);
    }

    static string BerechneOktalzahl(int dezimalzahl)
    {
        return BerechneInBeliebigeBasis(dezimalzahl, 8);
    }

    static string BerechneHexzahl(int dezimalzahl)
    {
        return BerechneInBeliebigeBasis(dezimalzahl, 16);
    }

    static string BerechneInBeliebigeBasis(int dezimalzahl, int basisWert)
    {
        const string symbole = "0123456789ABCDEF";

        if (dezimalzahl == 0)
            return "0";

        int restzahl = dezimalzahl;
        string ergebnis = string.Empty;

        while (restzahl > 0)
        {
            int rest = restzahl % basisWert;
            ergebnis = symbole[rest] + ergebnis;
            restzahl = restzahl / basisWert;
        }

        return ergebnis;
    }

    static void ZaehleSekundenImDualsystem()
    {
        Console.WriteLine("Sekundenzaehler im Dualsystem");
        Console.WriteLine("Beenden mit beliebiger Taste.\n");

        int sekunden = 0;

        while (!Console.KeyAvailable)
        {
            string dual = BerechneDualzahl(sekunden);
            Console.Write($"\rSekunde: {sekunden,6}  Dual: {dual,-32}");
            Thread.Sleep(1000);
            sekunden = sekunden + 1;
        }

        Console.ReadKey(true);
        Console.WriteLine();
    }
}

