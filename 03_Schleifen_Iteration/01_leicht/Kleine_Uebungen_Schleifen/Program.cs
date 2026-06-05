using System;
using System.Globalization;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
        bool wiederholen = true;

        while (wiederholen)
        {
            Console.Clear();
            Console.WriteLine("Kleine Uebungsaufgaben zu Schleifen\n");

            int zahl = 13;
            while (zahl <= 29)
            {
                Console.Write($"{zahl} ");
                zahl = zahl + 4;
            }
            Console.WriteLine("\n");

            zahl = 2;
            while (zahl >= -4)
            {
                Console.Write($"{zahl} ");
                zahl = zahl - 1;
            }
            Console.WriteLine("\n");

            zahl = 2000;
            while (zahl <= 6000)
            {
                Console.Write($"{zahl} ");
                zahl = zahl + 1000;
            }
            Console.WriteLine("\n");

            zahl = 5;
            while (zahl <= 13)
            {
                Console.Write($"Z{zahl} ");
                zahl = zahl + 2;
            }
            Console.WriteLine("\n");

            zahl = 1;
            while (zahl <= 3)
            {
                Console.Write($"ab{zahl} ");
                zahl = zahl + 1;
            }
            Console.WriteLine("\n");

            int zehner = 0;
            while (zehner <= 20)
            {
                int add = 2;
                while (add <= 3)
                {
                    Console.Write($"c{zehner + add} ");
                    add = add + 1;
                }

                zehner = zehner + 10;
            }
            Console.WriteLine("\n");

            zahl = 13;
            while (zahl <= 45)
            {
                if (zahl != 25 && zahl != 29)
                {
                    Console.Write($"{zahl} ");
                }

                zahl = zahl + 4;
            }
            Console.WriteLine("\n");

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }
}
