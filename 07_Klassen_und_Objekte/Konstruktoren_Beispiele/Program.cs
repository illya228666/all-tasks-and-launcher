using System.Globalization;
using Aquarium_Rechner;
using Person1_Konstruktoren;
using Person2_Properties;

namespace Konstruktoren_Beispiele;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        bool weiter = true;
        while (weiter)
        {
            Console.WriteLine("\nKonstruktoren und Properties");
            Console.WriteLine("1 · CPerson: vier Konstruktoren");
            Console.WriteLine("2 · CPerson2: Properties");
            Console.WriteLine("3 · Aquarium: Beispiel aus dem Arbeitsblatt");
            Console.WriteLine("4 · CNatZahl: Fakultät aus dem Infoblatt");
            Console.WriteLine("0 · Beenden");
            Console.Write("Auswahl: ");
            string? auswahl = Console.ReadLine();
            try
            {
                if (auswahl == "1") Person1Beispiel();
                else if (auswahl == "2") Person2Beispiel();
                else if (auswahl == "3") AquariumBeispiel();
                else if (auswahl == "4") FakultaetBeispiel();
                else if (auswahl == "0" || auswahl == null) weiter = false;
                else Console.WriteLine("Bitte 0, 1, 2, 3 oder 4 eingeben.");
            }
            catch (EndOfStreamException)
            {
                weiter = false;
            }
        }
    }

    static void Person1Beispiel()
    {
        CPerson? erstePerson = new CPerson();
        erstePerson.Ausgabe();
        CPerson? zweitePerson = new CPerson("Maier");
        zweitePerson.Ausgabe();

        string name = LeseText("Name der dritten Person: ");
        string telefon = LeseText("Telefonnummer: ");
        CPerson? drittePerson = new CPerson(name, telefon);
        drittePerson.Ausgabe();

        name = LeseText("Name der vierten Person: ");
        int alter = LeseZahl("Alter: ", 0, int.MaxValue);
        CPerson? viertePerson = new CPerson(name, alter);
        viertePerson.Ausgabe();

        erstePerson = null;
        zweitePerson = null;
        drittePerson = null;
        viertePerson = null;
        GC.Collect();
        Console.WriteLine("Vier Referenzen auf null gesetzt; GC.Collect() aufgerufen.");
    }

    static void Person2Beispiel()
    {
        CPerson2 personA = new CPerson2();
        personA.Name = "Willy";
        CPerson2 personB = new CPerson2();
        personB.Name = "Fritz";
        CPerson2 personC = new CPerson2("Hugo");
        personA.Alter = 24;
        personB.Alter = 22;
        personC.Alter = 20;
        Console.WriteLine("Der Erste heißt " + personA.Name + " und ist " + personA.Alter + " Jahre alt.");
        Console.WriteLine("Der Zweite heißt " + personB.Name + " und ist " + personB.Alter + " Jahre alt.");
        Console.WriteLine("Der Dritte heißt " + personC.Name + " und ist " + personC.Alter + " Jahre alt.");

        string name = LeseText("Name der vierten Person (Beispiel: Max): ");
        int alter = LeseZahl("Alter (Beispiel: 23): ", 0, int.MaxValue);
        CPerson2 personD = new CPerson2(name, alter);
        Console.WriteLine("Der Vierte heißt " + personD.Name + " und ist " + personD.Alter + " Jahre alt.");
    }

    static void AquariumBeispiel()
    {
        Aquarium meinAquarium = new Aquarium(80, 35, 35);
        CultureInfo kultur = CultureInfo.GetCultureInfo("de-DE");
        Console.WriteLine("Volumen: " + meinAquarium.Volumen.ToString("F2", kultur) + " Liter");
        Console.WriteLine("Preis: " + meinAquarium.GetPreis(0.4).ToString("F2", kultur) + " Euro");
    }

    static void FakultaetBeispiel()
    {
        int wert = LeseZahl("Zahl von 0 bis 12 (Beispiel: 5): ", 0, 12);
        CNatZahl zahl = new CNatZahl(wert);
        int fakultaet = zahl.BerechneFakultaet();
        Console.WriteLine("Fakultät: " + fakultaet);
    }

    static string LeseText(string frage)
    {
        while (true)
        {
            Console.Write(frage);
            string? eingabe = Console.ReadLine();
            if (eingabe == null) throw new EndOfStreamException("Die Eingabe wurde beendet.");
            if (eingabe.Trim() != "") return eingabe.Trim();
            Console.WriteLine("Bitte einen Text eingeben.");
        }
    }

    static int LeseZahl(string frage, int minimum, int maximum)
    {
        while (true)
        {
            string eingabe = LeseText(frage);
            int zahl;
            if (int.TryParse(eingabe, out zahl) && zahl >= minimum && zahl <= maximum) return zahl;
            Console.WriteLine("Bitte eine ganze Zahl von " + minimum + " bis " + maximum + " eingeben.");
        }
    }
}
