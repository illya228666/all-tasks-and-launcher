namespace Person1_Konstruktoren;

public class CPerson
{
    private string _Name;
    private string _Telefon;
    private int _Alter;

    public CPerson()
    {
        _Name = "NoName";
        _Telefon = "NoTelefon";
        _Alter = 0;
    }

    public CPerson(string nameParam)
    {
        _Name = nameParam;
        _Telefon = "NoTelefon";
        _Alter = 0;
    }

    public CPerson(string nameParam, string telefonParam)
    {
        _Name = nameParam;
        _Telefon = telefonParam;
        _Alter = 0;
    }

    public CPerson(string nameParam, int alterParam)
    {
        _Name = nameParam;
        _Telefon = "NoTelefon";
        _Alter = alterParam;
    }

    // Beide Oberflächen benutzen dieselben Personendaten.
    public string GetDaten()
    {
        return "Name: " + _Name + "\nTelefon: " + _Telefon + "\nAlter: " + _Alter;
    }

    public void Ausgabe()
    {
        Console.WriteLine(GetDaten());
        Console.WriteLine();
    }
}
