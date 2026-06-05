using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        string dateipfad = Path.Combine(AppContext.BaseDirectory, "test.txt");

        Console.WriteLine("Dateiverarbeitung - Grundlagen\n");
        Console.WriteLine($"Datei: {dateipfad}\n");

        using (StreamWriter writer = new(dateipfad, true))
        {
            writer.WriteLine("Hello World!");
        }

        Console.WriteLine("Dateiinhalt:\n");

        using (StreamReader reader = new(dateipfad))
        {
            while (!reader.EndOfStream)
            {
                string? zeile = reader.ReadLine();

                if (zeile is not null)
                {
                    Console.WriteLine(zeile);
                }
            }
        }

        Console.WriteLine("\nWeiter mit beliebiger Taste...");
        Console.ReadKey(true);
    }
}
