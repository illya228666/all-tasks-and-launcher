//using Helpers;
//using System.Globalization;

//class Programm
//{
//    static void Main()
//    {
//        Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
//        bool wiederholen = true;

//        while (wiederholen)
//        {
//            Console.Clear();
//            Console.WriteLine("Zahlensysteme\n");
//            Console.WriteLine("1) DEZIMALZAHL -> BINÄRZAHL");
//            Console.WriteLine("2) BINÄRZAHL -> DEZIMALZAHL");
//            Console.WriteLine("3) DEZIMALZAHL -> HEXADEZIMALZAHL");
//            Console.WriteLine();

//            int auswahl = Read.Int("Auswahl: ", 1, 3);

//            Console.Clear();

//            switch (auswahl)
//            {
//                case 1, 3:

//                    int dezimalzahl = Read.Int("Bitte eine natuerliche Zahl eingeben: ", 0, int.MaxValue);

//                    case 1:

//                        string dual = BerechneDualzahl(dezimalzahl);

//                        Console.WriteLine($"\nDezimal: {dezimalzahl}");
//                        Console.WriteLine($"Dual:    {dual}");

//                        break;

//                    case 3:

//                        string hex = BerechneHexzahl(dezimalzahl);

//                        Console.WriteLine($"\nDezimal: {dezimalzahl}");
//                        Console.WriteLine($"Hexadezimalzahl: {hex}");

//                        break;

//                case 2:

//                    break;

//            }


//            Console.Write("\nNoch einmal? (j/n): ");
//            var key = Console.ReadKey(true).KeyChar;
//            if (key == 'n' || key == 'N')
//                wiederholen = false;
//        }
//    }

//    static string BerechneDualzahl(int dezimalzahl)
//    {
//        return BerechneInBeliebigeBasis(dezimalzahl, 2);
//    }

//    static string BerechneInBeliebigeBasis(int dezimalzahl, int basisWert)
//    {
//        const string symbole = "0123456789ABCDEF";

//        if (dezimalzahl == 0)
//            return "0";

//        int restzahl = dezimalzahl;
//        string ergebnis = string.Empty;

//        while (restzahl > 0)
//        {
//            int rest = restzahl % basisWert;
//            ergebnis = symbole[rest] + ergebnis;
//            restzahl = restzahl / basisWert;
//        }

//        return ergebnis;
//    }

//    static string BerechneHexzahl(int dezimalzahl)
//    {
//        return BerechneInBeliebigeBasis(dezimalzahl, 16);
//    }

//}