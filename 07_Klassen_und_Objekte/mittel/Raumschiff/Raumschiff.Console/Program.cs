using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Raumschiff.Core;

namespace Raumschiff
{
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CRaumschiff.OnMessage += Console.WriteLine;
            try
            {
                ErstelleRaumschiffe();
            }
            catch (EndOfStreamException)
            {
                Console.WriteLine("Die Eingabe wurde beendet.");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            CRaumschiff.OnMessage -= Console.WriteLine;
        }

        // Eigener Stackrahmen, damit beim GC-Aufruf keine Raumschiff-Referenzen mehr leben.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ErstelleRaumschiffe()
        {
            Console.WriteLine("***** Raumschiff des Spielers erstellen *****");
            CRaumschiff spieler = new CRaumschiff();

            Console.WriteLine("***** Raumschiff des Gegners erstellen  *****");
            int x = LeseZahl("x-Position des Gegners:  ");
            int y = LeseZahl("y-Position des Gegners:  ");
            CRaumschiff gegner = new CRaumschiff(x, y);

            GC.KeepAlive(spieler);
            GC.KeepAlive(gegner);
        }

        private static int LeseZahl(string frage)
        {
            while (true)
            {
                Console.Write(frage);
                string eingabe = Console.ReadLine();
                if (eingabe == null) throw new EndOfStreamException();
                if (int.TryParse(eingabe, out int zahl)) return zahl;
                Console.WriteLine("Bitte eine ganze Zahl eingeben.");
            }
        }
    }
}
