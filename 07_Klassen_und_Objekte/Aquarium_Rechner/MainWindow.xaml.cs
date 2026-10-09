using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Aquarium_Rechner;

public partial class MainWindow : Window
{
    private CultureInfo kultur = CultureInfo.GetCultureInfo("de-DE");

    public MainWindow()
    {
        InitializeComponent();
        BerechneAquarium();
    }

    private void Berechnen_Click(object sender, RoutedEventArgs e)
    {
        BerechneAquarium();
    }

    private void BerechneAquarium()
    {
        double laenge, breite, hoehe, glaspreis;
        if (!LeseZahl(LaengeEingabe.Text, out laenge) || laenge <= 0
            || !LeseZahl(BreiteEingabe.Text, out breite) || breite <= 0
            || !LeseZahl(HoeheEingabe.Text, out hoehe) || hoehe <= 0)
        {
            ZeigeFehler("Bitte für Länge, Breite und Höhe positive Zahlen eingeben.");
            return;
        }
        if (!LeseZahl(GlaspreisEingabe.Text, out glaspreis) || glaspreis < 0)
        {
            ZeigeFehler("Der Glaspreis muss eine Zahl ab 0 sein.");
            return;
        }

        Aquarium meinAquarium = new Aquarium(laenge, breite, hoehe);
        double materialpreis = meinAquarium.Glasflaeche * glaspreis;
        double preis = meinAquarium.GetPreis(glaspreis);
        if (!double.IsFinite(meinAquarium.Volumen) || !double.IsFinite(preis))
        {
            ZeigeFehler("Die Zahlen sind zu groß. Bitte kleinere Maße oder einen kleineren Glaspreis eingeben.");
            return;
        }

        VolumenAusgabe.Text = meinAquarium.Volumen.ToString("N2", kultur);
        PreisAusgabe.Text = preis.ToString("N2", kultur) + " €";
        Rechenweg.Text = "Glasfläche (4 Seiten + Boden): " + meinAquarium.Glasflaeche.ToString("N2", kultur) + " dm²\n"
            + "Material: " + materialpreis.ToString("N2", kultur) + " €\n"
            + "Sockelbetrag: 20,00 €\n"
            + "Vor dem Aufrunden: " + (20 + materialpreis).ToString("N2", kultur) + " €";
        AquariumVorschau.SetAbmessungen(laenge, breite, hoehe);
        VorschauHinweis.Text = "Proportionale Live-Vorschau · Maße in Zentimetern";
        Status.Text = "Berechnung aktualisiert.";
    }

    private bool LeseZahl(string text, out double zahl)
    {
        string eingabe = text.Trim().Replace(',', '.');
        return double.TryParse(eingabe, NumberStyles.Float, CultureInfo.InvariantCulture, out zahl)
            && double.IsFinite(zahl);
    }

    private void ZeigeFehler(string meldung)
    {
        VolumenAusgabe.Text = "—";
        PreisAusgabe.Text = "—";
        Rechenweg.Text = "Für diese Eingabe liegt noch keine gültige Berechnung vor.";
        Status.Text = meldung;
    }

    private void Abmessung_Geaendert(object sender, TextChangedEventArgs e)
    {
        // TextChanged wird auch beim Aufbau des Fensters ausgelöst.
        if (AquariumVorschau == null) return;
        double laenge, breite, hoehe;
        bool gueltig = LeseZahl(LaengeEingabe.Text, out laenge) && laenge > 0;
        gueltig = LeseZahl(BreiteEingabe.Text, out breite) && breite > 0 && gueltig;
        gueltig = LeseZahl(HoeheEingabe.Text, out hoehe) && hoehe > 0 && gueltig;
        if (gueltig)
        {
            AquariumVorschau.SetAbmessungen(laenge, breite, hoehe);
            VorschauHinweis.Text = "Proportionale Live-Vorschau · Preis mit „Berechnen“ aktualisieren";
        }
        else
        {
            AquariumVorschau.SetGueltig(false);
            VorschauHinweis.Text = "Bitte drei positive Abmessungen eingeben.";
        }
    }

    private void Animation_Click(object sender, RoutedEventArgs e)
    {
        AquariumVorschau.AnimationLaeuft = !AquariumVorschau.AnimationLaeuft;
        AnimationButton.Content = AquariumVorschau.AnimationLaeuft ? "Ⅱ Pause" : "▶ Weiter";
    }

    private void Beispiel_Click(object sender, RoutedEventArgs e)
    {
        LaengeEingabe.Text = "80";
        BreiteEingabe.Text = "35";
        HoeheEingabe.Text = "35";
        GlaspreisEingabe.Text = "0,40";
        BerechneAquarium();
    }
}
