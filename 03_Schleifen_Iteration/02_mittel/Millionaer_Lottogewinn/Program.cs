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
            Console.WriteLine("Millionaer / Lottogewinn\n");
            Console.WriteLine("1) Wie lange bis zum Millionaer?");
            Console.WriteLine("2) Wie lange reicht der Lottogewinn?");
            Console.WriteLine();

            int auswahl = Read.Int("Auswahl: ", 1, 2);

            Console.Clear();
            if (auswahl == 1)
            {
                BerechneMillionaer(culture);
            }
            else
            {
                BerechneLottogewinn(culture);
            }

            Console.Write("\nNoch einmal? (j/n): ");
            var key = Console.ReadKey(true).KeyChar;
            if (key == 'n' || key == 'N')
                wiederholen = false;
        }
    }

    static void BerechneMillionaer(CultureInfo culture)
    {
        Console.WriteLine("Wie lange dauert es, um Millionaer zu werden?\n");

        decimal einlage = Read.Decimal("Einlage in Euro: ", 0.01m, decimal.MaxValue, culture);
        decimal zinssatz = Read.Decimal("Zinssatz (p.a.) in %: ", 0m, 100m, culture);
        int anfangsjahr = Read.Int("Anfangsjahr: ", 1, 9999);

        if (einlage < 1000000m && zinssatz == 0m)
        {
            Console.WriteLine("\nMit 0 % Zinsen wird aus der Einlage kein Millionaersbetrag.");
            return;
        }

        decimal kontostand = einlage;
        int jahr = anfangsjahr;
        int jahre = 0;

        Console.WriteLine();
        Console.WriteLine("Jahr   Wert am Jahresanfang   Zinsen pro Jahr   Wert am Jahresende");
        Console.WriteLine("--------------------------------------------------------------------");

        while (kontostand < 1000000m)
        {
            decimal startwert = kontostand;
            decimal zinsen = startwert * zinssatz / 100m;
            decimal endwert = startwert + zinsen;

            Console.WriteLine($"{jahr,4}   {startwert,20:F2}   {zinsen,15:F2}   {endwert,18:F2}");

            kontostand = endwert;
            jahr = jahr + 1;
            jahre = jahre + 1;
        }

        Console.WriteLine();
        Console.WriteLine($"Nach {jahre} Jahren ist das Vermoegen mindestens 1.000.000,00 Euro.");
    }

    static void BerechneLottogewinn(CultureInfo culture)
    {
        Console.WriteLine("Wie lange reicht ein Lottogewinn, um davon zu leben?\n");

        decimal kapital = Read.Decimal("Lottogewinn in Euro: ", 0.01m, decimal.MaxValue, culture);
        decimal verzinsung = Read.Decimal("Verzinsung (p.a.) in %: ", 0m, 100m, culture);
        decimal monatsrente = Read.Decimal("Monatliche Rente in Euro: ", 0.01m, decimal.MaxValue, culture);

        decimal zinsProMonatSatz = verzinsung / 100m / 12m;

        if (zinsProMonatSatz > 0m && monatsrente <= kapital * zinsProMonatSatz)
        {
            Console.WriteLine("\nDie monatliche Rente ist nicht groesser als die Monatszinsen.");
            Console.WriteLine("Das Kapital wird dadurch nicht aufgebraucht.");
            return;
        }

        int monat = 1;
        int gesamtMonate = 0;

        Console.WriteLine();
        Console.WriteLine("Monat  Kapital am Anfang   Zinsen    monatliche Rente   Kapital am Ende");
        Console.WriteLine("--------------------------------------------------------------------------");

        while (kapital > 0m)
        {
            decimal startkapital = kapital;
            decimal zinsen = startkapital * zinsProMonatSatz;

            if (startkapital + zinsen < monatsrente)
                break;

            decimal endkapital = startkapital + zinsen - monatsrente;

            Console.WriteLine($"{monat,5}  {startkapital,17:F2}  {zinsen,8:F2}  {monatsrente,17:F2}  {endkapital,15:F2}");

            kapital = endkapital;
            monat = monat + 1;
            gesamtMonate = gesamtMonate + 1;
        }

        int jahre = gesamtMonate / 12;
        int restMonate = gesamtMonate % 12;

        Console.WriteLine();
        Console.WriteLine($"Der Lottogewinn reicht {jahre} Jahre und {restMonate} Monate, um davon zu leben.");
    }
}
