namespace Person2_Properties;

public class CPerson2
{
    private string m_Name;
    private int m_Alter;

    public string Name
    {
        get { return m_Name; }
        set { m_Name = value; }
    }

    public int Alter
    {
        get { return m_Alter; }
        set { m_Alter = value; }
    }

    // Die Meldung bleibt auch nach einer späteren Namensänderung erhalten.
    public string ErstellungsMeldung { get; private set; }

    public CPerson2()
    {
        m_Name = "nichts";
        m_Alter = 0;
        ErstellungsMeldung = "Das Objekt " + m_Name + " wurde erstellt.";
        Console.WriteLine(ErstellungsMeldung);
    }

    public CPerson2(string name)
    {
        m_Name = name;
        m_Alter = 0;
        ErstellungsMeldung = "Das Objekt " + m_Name + " wurde erstellt.";
        Console.WriteLine(ErstellungsMeldung);
    }

    public CPerson2(string name, int alter)
    {
        m_Name = name;
        m_Alter = alter;
        ErstellungsMeldung = "Das Objekt " + m_Name + " wurde erstellt.";
        Console.WriteLine(ErstellungsMeldung);
    }
}
