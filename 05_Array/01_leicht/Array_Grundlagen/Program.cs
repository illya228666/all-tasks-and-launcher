using System;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        ClearConsole();
        Console.WriteLine("Array - Grundlagen\n");

        string[] kundeName1 = new string[6];
        kundeName1[0] = "lena";
        kundeName1[1] = "jana";
        kundeName1[3] = "tobias";
        kundeName1[5] = "vincent";

        string[] kundeName2 = new string[6];
        kundeName2[0] = "lena";
        kundeName2[1] = "jana";
        kundeName2[1] = "alina";
        kundeName2[2] = "rene";

        Console.WriteLine("Quelltext 1");
        GibArrayAus(kundeName1);

        Console.WriteLine("\nQuelltext 2");
        GibArrayAus(kundeName2);

        Console.WriteLine("\nHinweis: Ein leerer Speicherplatz enthaelt keinen Text.");
        Console.WriteLine("\nWeiter mit beliebiger Taste...");
        Console.ReadKey(true);
    }

    static void GibArrayAus(string[] werte)
    {
        Console.WriteLine("Array-Feld | Wert");
        Console.WriteLine("-----------------");

        for (int i = 0; i < werte.Length; i++)
        {
            string ausgabe = string.IsNullOrWhiteSpace(werte[i]) ? "(leer)" : werte[i];
            Console.WriteLine("{0,10} | {1}", i, ausgabe);
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
