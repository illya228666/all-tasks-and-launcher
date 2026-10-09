using System.Windows;

namespace Person1_Konstruktoren;

public partial class MainWindow : Window
{
    private CPerson? erstePerson;
    private CPerson? zweitePerson;
    private CPerson? drittePerson;
    private CPerson? viertePerson;

    public MainWindow()
    {
        InitializeComponent();
        ErstellePersonen();
    }

    private void Erstellen_Click(object sender, RoutedEventArgs e)
    {
        ErstellePersonen();
    }

    private void ErstellePersonen()
    {
        string telefonName = TelefonName.Text.Trim();
        string telefon = TelefonEingabe.Text.Trim();
        string alterName = AlterName.Text.Trim();
        int alter;

        if (telefonName == "" || telefon == "" || alterName == "")
        {
            Status.Text = "Bitte beide Namen und eine Telefonnummer eingeben.";
            return;
        }
        if (!int.TryParse(AlterEingabe.Text, out alter) || alter < 0)
        {
            Status.Text = "Das Alter muss eine ganze Zahl ab 0 sein.";
            AlterEingabe.Focus();
            return;
        }

        erstePerson = new CPerson();
        zweitePerson = new CPerson("Maier");
        drittePerson = new CPerson(telefonName, telefon);
        viertePerson = new CPerson(alterName, alter);
        ErsteAusgabe.Text = erstePerson.GetDaten();
        ZweiteAusgabe.Text = zweitePerson.GetDaten();
        DritteAusgabe.Text = drittePerson.GetDaten();
        VierteAusgabe.Text = viertePerson.GetDaten();
        SammelnButton.IsEnabled = true;
        Status.Text = "Vier Objekte wurden mit vier verschiedenen Konstruktoren erstellt.";
    }

    private void Sammeln_Click(object sender, RoutedEventArgs e)
    {
        erstePerson = null;
        zweitePerson = null;
        drittePerson = null;
        viertePerson = null;
        // Nur für diese Übung: Normalerweise übernimmt .NET die Speicherbereinigung.
        GC.Collect();

        ErsteAusgabe.Text = "erstePerson = null";
        ZweiteAusgabe.Text = "zweitePerson = null";
        DritteAusgabe.Text = "drittePerson = null";
        VierteAusgabe.Text = "viertePerson = null";
        SammelnButton.IsEnabled = false;
        Status.Text = "Alle vier Referenzen stehen auf null. GC.Collect() wurde aufgerufen.";
    }
}
