using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Helpers;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
        string dateipfad = Path.Combine(AppContext.BaseDirectory, "daten.txt");
        bool beenden = false;

        while (!beenden)
        {
            Console.Clear();
            ZeigeMenue();

            int auswahl = Read.Int("Auswahl: ", 1, 3);
            Console.Clear();

            switch (auswahl)
            {
                case 1:
                    DatensatzErfassen(dateipfad, culture);
                    break;
                case 2:
                    DatensaetzeAuslesen(dateipfad, culture);
                    break;
                case 3:
                    beenden = true;
                    break;
            }
        }
    }

    static void ZeigeMenue()
    {
        Console.WriteLine("Hotelprotokoll\n");
        Console.WriteLine("Bitte waehlen Sie");
        Console.WriteLine("1 - Datensatz erfassen");
        Console.WriteLine("2 - Datensaetze auslesen");
        Console.WriteLine("3 - Programm beenden\n");
    }

    static void DatensatzErfassen(string dateipfad, CultureInfo culture)
    {
        Console.WriteLine("Datensatz erfassen\n");

        int zimmernummer = Read.Int("Zimmernummer: ", 1, int.MaxValue);
        decimal preis = Read.Decimal("Zimmerpreis: ", 0m, decimal.MaxValue, culture);
        string gastname = Read.String("Name des Gastes: ");

        string payload = $"{zimmernummer} - {preis.ToString("F2", culture)} - {gastname}";
        LogFile.AppendTimestampedLine(dateipfad, payload, culture);

        Console.WriteLine("\nDatensatz wurde gespeichert.");
        WarteAufRueckkehr();
    }

    static void DatensaetzeAuslesen(string dateipfad, CultureInfo culture)
    {
        Console.WriteLine("Datensaetze auslesen\n");

        List<string> alleZeilen = LogFile.ReadLines(dateipfad);

        if (alleZeilen.Count == 0)
        {
            Console.WriteLine("Keine Datensaetze vorhanden.");
            WarteAufRueckkehr();
            return;
        }

        DateTime datum = Read.Date("Datum (dd.MM.yyyy, leer = alle): ", "dd.MM.yyyy", culture, true, out bool wasEmpty);
        List<string> gefundeneZeilen = wasEmpty
            ? alleZeilen
            : LogFile.ReadLinesByDate(dateipfad, datum.ToString("dd.MM.yyyy", culture));

        Console.WriteLine();

        if (gefundeneZeilen.Count == 0)
        {
            Console.WriteLine("Keine Eintraege fuer dieses Datum gefunden.");
        }
        else
        {
            foreach (string zeile in gefundeneZeilen)
            {
                Console.WriteLine(zeile);
            }
        }

        WarteAufRueckkehr();
    }

    static void WarteAufRueckkehr()
    {
        Console.WriteLine("\nWeiter mit beliebiger Taste...");
        Console.ReadKey(true);
    }
}
