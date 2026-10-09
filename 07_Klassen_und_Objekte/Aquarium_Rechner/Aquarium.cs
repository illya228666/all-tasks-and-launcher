namespace Aquarium_Rechner;

public class Aquarium
{
    private double m_Laenge;
    private double m_Breite;
    private double m_Hoehe;

    // Alle Abmessungen werden in Zentimetern gespeichert.
    public double Laenge
    {
        get { return m_Laenge; }
        set
        {
            PruefeAbmessung(value);
            m_Laenge = value;
        }
    }

    public double Breite
    {
        get { return m_Breite; }
        set
        {
            PruefeAbmessung(value);
            m_Breite = value;
        }
    }

    public double Hoehe
    {
        get { return m_Hoehe; }
        set
        {
            PruefeAbmessung(value);
            m_Hoehe = value;
        }
    }

    public double Volumen
    {
        get { return Laenge * Breite * Hoehe / 1000; }
    }

    public double Glasflaeche
    {
        get
        {
            // Zwei lange Seiten, zwei kurze Seiten und ein Boden, ohne Deckel.
            return (2 * Laenge * Hoehe + 2 * Breite * Hoehe + Laenge * Breite) / 100;
        }
    }

    public Aquarium() : this(80, 35, 35)
    {
    }

    public Aquarium(double laenge, double breite, double hoehe)
    {
        Laenge = laenge;
        Breite = breite;
        Hoehe = hoehe;
    }

    public double GetPreis(double glaspreis)
    {
        if (!double.IsFinite(glaspreis) || glaspreis < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(glaspreis), "Der Glaspreis muss mindestens 0 sein.");
        }

        double preis = 20 + Glasflaeche * glaspreis;
        return Math.Ceiling(preis / 5) * 5;
    }

    private void PruefeAbmessung(double wert)
    {
        if (!double.IsFinite(wert) || wert <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wert), "Abmessungen müssen größer als 0 sein.");
        }
    }
}
