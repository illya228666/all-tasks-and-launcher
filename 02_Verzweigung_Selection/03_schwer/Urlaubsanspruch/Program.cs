using System;
using System.Globalization;
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
            Console.WriteLine("Urlaubsanspruch-Rechner\n");
            Console.Write("Name (optional): ");
            string name = Console.ReadLine() ?? string.Empty;

            int alter = Read.Int("Alter: ", 0, 120);

            int behinderung = Read.Int("Behinderung [%]: ", 0, 100);

            Console.WriteLine();
            Console.WriteLine("Beschäftigungszeitraum im Kalenderjahr (volle Monate).");

            int von = Read.Int("Beschäftigt von: ", 1, 12);
            int bis = Read.Int("Beschäftigt bis: ", 1, 12);

            while (von > bis)
            {
                Console.WriteLine("\nFehler: „Beschäftigt von“ muss kleiner oder gleich „Beschäftigt bis“ sein.\n");
                von = Read.Int("Beschäftigt von (Monat 1-12): ", 1, 12);
                bis = Read.Int("Beschäftigt bis (Monat 1-12): ", 1, 12);
            }

            int volleMonate = bis - von + 1;

            int grundurlaub = (alter < 18) ? 30 : 24;

            int zuschlagBehinderung = (behinderung >= 50) ? 5 : 0;
            int zuschlagAlter = (alter >= 55) ? 2 : 0;
            int jahresAnspruch = grundurlaub + zuschlagBehinderung + zuschlagAlter;

            decimal anteiligerAnspruch = jahresAnspruch * volleMonate / 12m;
            decimal gerundetAuf = Math.Ceiling(anteiligerAnspruch);

            Console.Clear();
            Console.WriteLine("Urlaubsanspruch-Rechner\n");
            Console.WriteLine($"Beschäftigter: {name}");
            Console.WriteLine($"Alter: {alter} Jahre");
            Console.WriteLine($"Behinderung: {behinderung} %");
            Console.WriteLine($"Beschäftigt: Monat {von} bis {bis} (volle Monate: {volleMonate})\n");
            Console.WriteLine("Berechnung:");
            Console.WriteLine($"Grundurlaub (6-Tage-Woche): {grundurlaub} Werktage");
            Console.WriteLine($"Zuschlag Behinderung (>= 50%): {zuschlagBehinderung} Werktage");
            Console.WriteLine($"Zuschlag ab 55 Jahren: {zuschlagAlter} Werktage");
            Console.WriteLine($"Jahresanspruch (gesamt): {jahresAnspruch} Werktage\n");
            //Console.WriteLine($"Anteiliger Anspruch: {anteiligerAnspruch:F2} Werktage");

            Console.WriteLine($"Auf volle Tage aufgerundet: {gerundetAuf} Werktage\n");

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N') wiederholen = false;
        }
    }
}