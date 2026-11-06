using System.Globalization;
using System.Text;
using Aquarium.Core;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Aquarium-Rechner · Konsolenversion");
Console.WriteLine("Eingabetaste übernimmt den jeweiligen Beispielwert.");
Console.WriteLine();

double laenge = LeseZahl("Länge (cm)", 80, true);
double breite = LeseZahl("Breite (cm)", 35, true);
double hoehe = LeseZahl("Höhe (cm)", 35, true);
double glaspreis = LeseZahl("Glaspreis (€/dm²)", 0.40, false);

Aquarium aquarium = new(laenge, breite, hoehe);
double preis = aquarium.GetPreis(glaspreis);
if (!double.IsFinite(aquarium.Volumen) || !double.IsFinite(aquarium.Glasflaeche) || !double.IsFinite(preis))
{
    Console.Error.WriteLine("Die Werte sind zu groß. Bitte kleinere Abmessungen eingeben.");
    return;
}

CultureInfo kultur = CultureInfo.GetCultureInfo("de-DE");
Console.WriteLine();
Console.WriteLine("Volumen:     " + aquarium.Volumen.ToString("N2", kultur) + " Liter");
Console.WriteLine("Glasfläche: " + aquarium.Glasflaeche.ToString("N2", kultur) + " dm² (ohne Deckel)");
Console.WriteLine("Gesamtpreis: " + preis.ToString("N2", kultur) + " € (auf 5 € aufgerundet)");

static double LeseZahl(string titel, double standard, bool mussPositivSein)
{
    while (true)
    {
        Console.Write($"{titel} [{standard.ToString("G", CultureInfo.GetCultureInfo("de-DE"))}]: ");
        string? eingabe = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(eingabe))
            return standard;
        eingabe = eingabe.Trim().Replace(',', '.');
        if (double.TryParse(eingabe, NumberStyles.Float, CultureInfo.InvariantCulture, out double wert)
            && double.IsFinite(wert) && (mussPositivSein ? wert > 0 : wert >= 0))
            return wert;
        Console.WriteLine(mussPositivSein
            ? "Bitte eine positive Zahl eingeben."
            : "Bitte eine Zahl ab 0 eingeben.");
    }
}
