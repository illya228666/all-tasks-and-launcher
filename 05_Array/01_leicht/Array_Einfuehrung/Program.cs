using System;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        ClearConsole();
        Console.WriteLine("Array - Einfuehrung\n");

        string[] artikel =
        {
            "Mountainbike TRACK",
            "Mountainbike EXTREME",
            "Tandem",
            "Puky",
            "Croozer",
            "Radset",
            "Sattel"
        };

        int[] rahmen = new int[10];
        rahmen[0] = 52;
        rahmen[1] = 60;

        int anzahl = artikel.Length;

        Console.WriteLine("Array artikel");
        for (int i = 0; i < artikel.Length; i++)
        {
            Console.WriteLine("artikel[{0}] = {1}", i, artikel[i]);
        }

        Console.WriteLine("\nArray rahmen");
        for (int i = 0; i < rahmen.Length; i++)
        {
            Console.WriteLine("rahmen[{0}] = {1}", i, rahmen[i]);
        }

        Console.WriteLine("\nAnzahl der gespeicherten Artikel: {0}", anzahl);

        Console.WriteLine("\nAusgabe mit foreach");
        foreach (string artikelname in artikel)
        {
            Console.WriteLine(artikelname);
        }

        Console.WriteLine("\nWeiter mit beliebiger Taste...");
        Console.ReadKey(true);
    }

    static void ClearConsole()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }
}
