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
            Console.WriteLine("Tankrechnung\n");

            string benzinart = string.Empty;
            decimal literPreisBrutto = 0m;
            bool benzinOk = false;

            // --- Benzinart einlesen ---
            while (!benzinOk)
            {
                Console.Write("Benzinart (Normalbenzin / Superbenzin / Diesel): ");
                string input = (Console.ReadLine() ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

                switch (input)
                {
                    case "normalbenzin":
                    case "normal":
                    case "n":
                        benzinart = "Normalbenzin";
                        literPreisBrutto = 1.612m;
                        benzinOk = true;
                        break;

                    case "superbenzin":
                    case "super":
                    case "s":
                        benzinart = "Superbenzin";
                        literPreisBrutto = 1.674m;
                        benzinOk = true;
                        break;

                    case "diesel":
                    case "d":
                        benzinart = "Diesel";
                        literPreisBrutto = 1.465m;
                        benzinOk = true;
                        break;

                    default:
                        Console.WriteLine("Diese Benzinart kenne ich nicht. Versuch’s nochmal.\n");
                        break;
                }
            }

            decimal liter = Read.Decimal(
            "Getankte Liter: ",
            0.1m,
            decimal.MaxValue,
            culture
            );

            Console.Clear();

            decimal brutto = liter * literPreisBrutto;
            decimal netto = brutto / 1.19m;
            decimal mwst = brutto - netto;

            Console.WriteLine();
            Console.WriteLine("┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓");
            Console.WriteLine("┃          ⛽ T A N K R E C H N U N G          ┃");
            Console.WriteLine("┃                (Kassenbon) 🧾                ┃");
            Console.WriteLine("┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛");
            Console.WriteLine();
            Console.WriteLine($"🛢️ Kraftstoff: {benzinart}");
            Console.WriteLine($"⛽ Getankt: {liter.ToString("F1", culture)} L");
            Console.WriteLine($"💶 Preis/Liter: {literPreisBrutto.ToString("F3", culture)} €");
            Console.WriteLine();
            Console.WriteLine("════════════════════ 💳 B E T R Ä G E ══════════════════════");
            Console.WriteLine($"🧾 Nettobetrag:\t\t\t {netto.ToString("F2", culture),12} € (ohne MwSt)");
            Console.WriteLine($"🧮 Mehrwertsteuer (19 %):\t {mwst.ToString("F2", culture),12} € (+ Steuer 🙃)");
            Console.WriteLine($"🏁 Bruttobetrag:\t\t {brutto.ToString("F2", culture),12} € ✅");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("~-~-~-~-~-~-~-~~-~-~-~-~-~");
            Console.WriteLine("  🚗💨 Danke fürs Tanken!");
            Console.WriteLine("~-~-~-~-~-~-~-~~-~-~-~-~-~");
            Console.WriteLine();

            Console.Write("Noch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;

            if (key == 'n' || key == 'N')
                wiederholen = false;
        }

    }
}