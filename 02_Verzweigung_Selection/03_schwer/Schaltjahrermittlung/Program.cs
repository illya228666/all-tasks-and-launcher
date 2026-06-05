using System;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using Helpers;

Console.OutputEncoding = System.Text.Encoding.UTF8; Console.InputEncoding = System.Text.Encoding.UTF8; CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");

while (true)
{

    Console.Clear();
    int year ;
    bool isSchaltjahrermittlung;

    year = Read.Int("Geben Sie das gewünschte Jahr ein: ", 1, 9999);

    isSchaltjahrermittlung = 
        year % 400 == 0 ? true : 
        (year % 100 == 0 ? false : 
        (year % 4 == 0 ? true: false) );

    Console.Write($"\nDas Jahr {year} ist {(!isSchaltjahrermittlung ? $"kein" : "")} Schaltjahr!  {(!isSchaltjahrermittlung ? $"(-)" : "(+)")}\r\n\n");

    Console.Write("Noch einmal? (j/n): ");
    var key = Console.ReadKey(true).KeyChar;
    if (key == 'n' || key == 'N')
        break;

}





