using System;
using System.Globalization;
using Helpers;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");

        bool wiederholen = true;

        while (wiederholen)
        {
            ClearConsole();
            Console.WriteLine("Berechnen eines Angebots abhaengig vom Saisonpreis\n");

            double[] saisonpreise = new double[] { 59.50, 69.50, 79.50, 64.50 };
            double aufenthaltsdauer = 7;
            double mwstSatz = 7;
            double anzahlPersonen = 2;

            Console.WriteLine("Preisliste fuer ein Doppelzimmer");
            for (int i = 0; i < saisonpreise.Length; i++)
            {
                Console.WriteLine("Saisonpreis {0}: {1:F2} Euro", i + 1, saisonpreise[i]);
            }

            int saison = Read.Int("\nEingabe der Saison (1-4): ", 1, 4);

            double nettopreis = saisonpreise[saison - 1] * aufenthaltsdauer * anzahlPersonen;
            double mwst = nettopreis * mwstSatz / 100;
            double gesamtpreis = nettopreis + mwst;

            Console.WriteLine();
            Console.WriteLine("Aufenthaltsdauer: {0} Tage", aufenthaltsdauer);
            Console.WriteLine("Personen:         {0}", anzahlPersonen);
            Console.WriteLine("Nettopreis:       {0:F2} Euro", nettopreis);
            Console.WriteLine("MwSt ({0:F2} %):    {1:F2} Euro", mwstSatz, mwst);
            Console.WriteLine("Gesamtpreis:      {0:F2} Euro", gesamtpreis);

            Console.Write("\nNoch einmal? (j/n): ");
            string antwort = Console.ReadLine() ?? string.Empty;
            if (antwort.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                wiederholen = false;
            }
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
