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
            Console.WriteLine("Notenauswertung\n");
            int gesamt = Read.Int("Gesamtpunktzahl der Klassenarbeit : ", 1, int.MaxValue);
            double erreicht = Read.Double("erreichte Punkte : ", 0, gesamt);

            int prozent = (int)Math.Round(erreicht * 100.0 / gesamt);
            string notentext = ErmittleNote(prozent);

            Console.WriteLine("\nAuswertung:");
            Console.WriteLine($"{erreicht} Punkte bedeuten {prozent} %.");
            Console.WriteLine($"Die Note {notentext} wurde erreicht.");

            Console.Write("\nNoch einmal? (j/n): ");
            char key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
    static string ErmittleNote(int prozent)
    {
        if (prozent >= 92) return "sehr gut";
        if (prozent >= 81) return "gut";
        if (prozent >= 67) return "befriedigend";
        if (prozent >= 50) return "ausreichend";
        if (prozent >= 30) return "mangelhaft";
        return "ungenügend";
    }
}
