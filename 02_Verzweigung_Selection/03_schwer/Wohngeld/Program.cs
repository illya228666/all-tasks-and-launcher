using System;
using System.Globalization;
using Helpers;

Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");

while (true)
{
    double verdienst;
    bool verheiratet;
    double grenze;
    int kinder;

    Console.Clear();
    verdienst = Read.Double("Geben Sie Ihren Verdienst ein.\n", 0, double.MaxValue);
    if (verdienst == 0) Console.Write("\nSchade");

    Console.WriteLine("\nSind Sie verheiratet? (J/N)");
    string tempVerh = Console.ReadLine().ToLower();
    verheiratet = new string[] { "j", "ja", "yes", "y", "1" }.Contains(tempVerh) ? true : false;

    kinder = Read.Int("\nWie viele Kinder haben Sie?\n", 0, int.MaxValue);

    grenze = verheiratet ? 800 : 500;

    switch (kinder)
    {
        case 1:
            grenze = grenze + 200;
            break;

        case 2:
            grenze = grenze + 400;
            break;

        case 3:
            grenze = grenze + 600;
            break;

        case > 3:
            grenze = grenze + 800;
            break;
    }

    Console.WriteLine($"\nGrenze: {grenze}");

    if (verdienst < grenze)
    {
        
        Console.WriteLine($"\nSie bekommen Wohngeld bewilligt in Höhe von {(grenze - verdienst)}€");
    }
    else
    {
        Console.WriteLine("\nSie sind nicht wohngeldberechtigt!");
    }

    Console.Write("\nNoch einmal? (j/n): ");
    var key = Console.ReadKey(true).KeyChar;
    if (key == 'n' || key == 'N')
        break;
}
