namespace Konstruktoren_Beispiele;

public class CNatZahl
{
    private int m_Zahl;

    public CNatZahl(int wert)
    {
        // 13! passt nicht mehr in eine int-Variable.
        if (wert < 0 || wert > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(wert), "Bitte eine Zahl von 0 bis 12 wählen.");
        }
        m_Zahl = wert;
    }

    public int BerechneFakultaet()
    {
        int fakultaet = 1;
        int faktor = 1;
        while (faktor <= m_Zahl)
        {
            fakultaet = fakultaet * faktor;
            faktor = faktor + 1;
        }
        return fakultaet;
    }
}
