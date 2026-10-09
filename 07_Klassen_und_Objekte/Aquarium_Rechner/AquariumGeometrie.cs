using System.Windows;

namespace Aquarium_Rechner;

public class AquariumGeometrie
{
    public double Laenge { get; }
    public double Hoehe { get; }
    public double TiefeX { get; }
    public double TiefeY { get; }
    private double links;
    private double unten;

    public AquariumGeometrie(double laenge, double breite, double hoehe, Size bereich)
    {
        // Erst normalisieren, damit auch sehr große Eingaben darstellbar sind.
        double groesstesMass = Math.Max(laenge, Math.Max(breite, hoehe));
        double l = laenge / groesstesMass;
        double b = breite / groesstesMass;
        double h = hoehe / groesstesMass;
        double platzX = Math.Max(1, bereich.Width - 108);
        double platzY = Math.Max(1, bereich.Height - 65);
        double faktor = Math.Min(platzX / (l + 0.62 * b), platzY / (h + 0.36 * b));

        Laenge = l * faktor;
        Hoehe = h * faktor;
        TiefeX = b * faktor * 0.62;
        TiefeY = b * faktor * 0.36;
        links = 58 + (platzX - Laenge - TiefeX) / 2;
        unten = 24 + (platzY + Hoehe + TiefeY) / 2;
    }

    // x: links -> rechts, y: Boden -> Oberkante, z: vorne -> hinten.
    public Point Punkt(double x, double y, double z)
    {
        return new Point(links + x * Laenge + z * TiefeX,
            unten - y * Hoehe - z * TiefeY);
    }
}
