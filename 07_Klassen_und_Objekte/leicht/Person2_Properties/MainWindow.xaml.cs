using System.Windows;
using System.Windows.Controls;

namespace Person2_Properties;

public partial class MainWindow : Window
{
    private List<CPerson2> personen = new List<CPerson2>();

    public MainWindow()
    {
        InitializeComponent();
        ErstelleBeispiele();
    }

    private void ErstelleBeispiele()
    {
        CPerson2 personA = new CPerson2();
        personA.Name = "Willy";
        personA.Alter = 24;
        CPerson2 personB = new CPerson2();
        personB.Name = "Fritz";
        personB.Alter = 22;
        CPerson2 personC = new CPerson2("Hugo");
        personC.Alter = 20;

        personen = new List<CPerson2> { personA, personB, personC };
        PersonenTabelle.ItemsSource = personen;
        PersonenTabelle.SelectedIndex = 0;
        Meldungen.Text = personA.ErstellungsMeldung + "\n" + personB.ErstellungsMeldung + "\n" + personC.ErstellungsMeldung;
        NeuerName.Text = "Max";
        NeuesAlter.Text = "23";
        AktualisiereAusgabe();
        Status.Text = "Willy, Fritz und Hugo sind bereit. Ergänze Max mit dem dritten Konstruktor.";
    }

    private void Erstellen_Click(object sender, RoutedEventArgs e)
    {
        string name = NeuerName.Text.Trim();
        int alter;
        if (!PruefeEingabe(name, NeuesAlter.Text, out alter)) return;

        CPerson2 personD = new CPerson2(name, alter);
        if (personen.Count == 4)
        {
            personen[3] = personD;
        }
        else
        {
            personen.Add(personD);
        }
        PersonenTabelle.Items.Refresh();
        PersonenTabelle.SelectedItem = personD;
        Meldungen.Text = Meldungen.Text + "\n" + personD.ErstellungsMeldung;
        AktualisiereAusgabe();
        Status.Text = "Die vierte Person wurde mit Name und Alter erstellt.";
    }

    private void Aendern_Click(object sender, RoutedEventArgs e)
    {
        CPerson2? person = PersonenTabelle.SelectedItem as CPerson2;
        if (person == null)
        {
            Status.Text = "Bitte zuerst eine Person auswählen.";
            return;
        }
        string name = BearbeitenName.Text.Trim();
        int alter;
        if (!PruefeEingabe(name, BearbeitenAlter.Text, out alter)) return;

        person.Name = name;
        person.Alter = alter;
        PersonenTabelle.Items.Refresh();
        AktualisiereAusgabe();
        Status.Text = "Name und Alter wurden über die Properties geändert.";
    }

    private bool PruefeEingabe(string name, string alterText, out int alter)
    {
        alter = 0;
        if (name == "")
        {
            Status.Text = "Bitte einen Namen eingeben.";
            return false;
        }
        if (!int.TryParse(alterText, out alter) || alter < 0)
        {
            Status.Text = "Das Alter muss eine ganze Zahl ab 0 sein.";
            return false;
        }
        return true;
    }

    private void Auswahl_Geaendert(object sender, SelectionChangedEventArgs e)
    {
        CPerson2? person = PersonenTabelle.SelectedItem as CPerson2;
        if (person != null)
        {
            BearbeitenName.Text = person.Name;
            BearbeitenAlter.Text = person.Alter.ToString();
        }
    }

    private void AktualisiereAusgabe()
    {
        string[] reihenfolge = { "Der Erste", "Der Zweite", "Der Dritte", "Der Vierte" };
        Ausgabe.Text = "";
        for (int i = 0; i < personen.Count; i++)
        {
            if (i > 0) Ausgabe.Text = Ausgabe.Text + "\n";
            Ausgabe.Text = Ausgabe.Text + reihenfolge[i] + " heißt " + personen[i].Name
                + " und ist " + personen[i].Alter + " Jahre alt.";
        }
    }

    private void Zuruecksetzen_Click(object sender, RoutedEventArgs e)
    {
        ErstelleBeispiele();
    }
}
