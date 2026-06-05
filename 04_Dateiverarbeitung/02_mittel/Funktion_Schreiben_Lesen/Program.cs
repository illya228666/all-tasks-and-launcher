using System.Globalization;
using Helpers;

class Program
{
    static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("de-DE");
    static readonly string Protokolldatei = Path.Combine(AppContext.BaseDirectory, "protokoll.txt");

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        bool beenden = false;

        while (!beenden)
        {
            TryClearConsole();
            ZeigeMenue();

            int auswahl = Read.Int("Auswahl: ", 1, 4);
            TryClearConsole();

            switch (auswahl)
            {
                case 1:
                    SchreibeEintrag();
                    break;
                case 2:
                    ZeigeEintraege();
                    break;
                case 3:
                    SichereDatei();
                    break;
                case 4:
                    beenden = true;
                    break;
            }
        }
    }

    static void ZeigeMenue()
    {
        Console.WriteLine("Funktion Schreiben / Lesen\n");
        Console.WriteLine("Bitte waehlen Sie");
        Console.WriteLine("1 - Protokolleintrag schreiben");
        Console.WriteLine("2 - Protokolleintraege anzeigen");
        Console.WriteLine("3 - Protokolldatei sichern");
        Console.WriteLine("4 - Programm beenden\n");
    }

    static void SchreibeEintrag()
    {
        Console.WriteLine("Protokolleintrag schreiben\n");

        string text = Read.String("Text: ");
        Schreiben(text);

        Console.WriteLine("\nEintrag wurde gespeichert.");
        Console.WriteLine($"Datei: {Protokolldatei}");
        WarteAufRueckkehr();
    }

    static void ZeigeEintraege()
    {
        Console.WriteLine("Protokolleintraege anzeigen\n");

        DateTime datum = Read.Date("Datum (dd.MM.yyyy): ", "dd.MM.yyyy", Culture, false, out _);
        int anzahl = Anzeigen(datum.ToString("dd.MM.yyyy", Culture));

        if (anzahl > 0)
        {
            Console.WriteLine($"\nGefundene Eintraege: {anzahl}");
        }

        WarteAufRueckkehr();
    }

    static void SichereDatei()
    {
        Console.WriteLine("Protokolldatei sichern\n");

        string eingabe = Read.String("Sicherungsdatei (Pfad oder Dateiname): ");
        string sicherungsdatei = ErmitteleZielpfad(eingabe);

        Sichern(sicherungsdatei);
        WarteAufRueckkehr();
    }

    static string ErmitteleZielpfad(string eingabe)
    {
        if (Path.IsPathRooted(eingabe))
        {
            return eingabe;
        }

        return Path.Combine(AppContext.BaseDirectory, eingabe);
    }

    static void Schreiben(string text)
    {
        LogFile.AppendTimestampedLine(Protokolldatei, text, Culture);
    }

    static int Anzeigen(string datum)
    {
        List<string> eintraege = LogFile.ReadLinesByDate(Protokolldatei, datum);

        Console.WriteLine($"Protokolleintraege fuer den {datum}\n");

        if (eintraege.Count == 0)
        {
            Console.WriteLine($"Keine Protokolleintraege fuer den {datum} vorhanden.");
            return 0;
        }

        foreach (string eintrag in eintraege)
        {
            Console.WriteLine(eintrag);
        }

        return eintraege.Count;
    }

    static void Sichern(string sicherungsdatei)
    {
        List<string> eintraege = LogFile.ReadLines(Protokolldatei);

        if (eintraege.Count == 0)
        {
            Console.WriteLine("Keine Protokolleintraege zum Sichern vorhanden.");
            return;
        }

        string? verzeichnis = Path.GetDirectoryName(sicherungsdatei);
        if (!string.IsNullOrWhiteSpace(verzeichnis))
        {
            Directory.CreateDirectory(verzeichnis);
        }

        using StreamWriter writer = new(sicherungsdatei, true);

        foreach (string eintrag in eintraege)
        {
            writer.WriteLine(eintrag);
        }

        Console.WriteLine("Sicherungsdatei wurde aktualisiert.");
        Console.WriteLine($"Eintraege gesichert: {eintraege.Count}");
        Console.WriteLine($"Datei: {sicherungsdatei}");
    }

    static void WarteAufRueckkehr()
    {
        Console.WriteLine("\nWeiter mit beliebiger Taste...");

        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(true);
        }
    }

    static void TryClearConsole()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
        }
    }
}
