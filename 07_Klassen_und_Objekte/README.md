# Klassen und Objekte

Lösungen zu den drei Aufgaben aus `tasks.zip`, mit deutschen WPF-Oberflächen und einfachen C#-Klassen. Die Originalblätter liegen unverändert in `Aufgaben`.

| Projekt | Arbeitsblatt | Inhalt |
| --- | --- | --- |
| Person1_Konstruktoren | 01_Person1_Konstruktoren.docx | Vier Konstruktoren, Ausgabe aller Daten, vier Referenzen auf `null` setzen und `GC.Collect()` aufrufen |
| Person2_Properties | 02_Person2_V_properties.docx | Konstruktoren, Properties für Name und Alter, Willy (24), Fritz (22), Hugo (20), Eingabe von Max (23), Änderungen über Properties |
| Aquarium_Rechner | 03_Aquarium.docx | Abmessungen als Properties, nur lesbares Volumen, Preis für vier Seiten und Boden mit Aufrundung auf 5 Euro |
| Konstruktoren_Beispiele | Alle drei Aufgaben und IB_AB_Konstruktoren.docx | Konsolenprogramm mit `Main()`, den ursprünglichen Ausgaben und dem zusätzlichen Beispiel `CNatZahl` |

`IB_AB_Konstruktoren.docx` ist ein Infoblatt. Sein Beispiel zur Fakultät wird im vierten Menüpunkt des Konsolenprogramms gezeigt.

## Aufbau

Die Fachklassen stehen jeweils in einer eigenen Datei. Die WPF-Fenster verwenden normale Ereignismethoden im Code-behind. Es gibt keine zusätzliche MVVM-Bibliothek. `Gemeinsam/Stil.xaml` enthält die gemeinsamen Farben und Steuerelement-Stile.

Das Konsolenprojekt bindet dieselben Klassendateien ein. Damit bleiben Konsolenbeispiele und Fenster bei Änderungen an der Berechnung gleich. `CPerson.Ausgabe()` und die Ausgaben in den Konstruktoren von `CPerson2` sind in der Konsole sichtbar; die Fenster zeigen dieselben Daten und Meldungen als Text an.

## Aquarium

Die Maße werden in Zentimetern eingegeben, der Glaspreis in Euro pro Quadratdezimeter. Das Aquarium ist oben offen.

```text
Volumen in Litern = Länge × Breite × Höhe / 1000
Glasfläche in dm² = (2 × Länge × Höhe + 2 × Breite × Höhe + Länge × Breite) / 100
Preis = auf volle 5 Euro aufrunden (20 Euro + Glasfläche × Glaspreis)
```

Für das Beispiel `new Aquarium(80, 35, 35)` und `GetPreis(0.4)` ergeben sich **98,00 Liter**, **108,50 dm² Glas** und **65,00 Euro**. Ein Glaspreis von 0 ist zulässig; Maße müssen positiv sein. Das Fenster akzeptiert Komma und Punkt als Dezimalzeichen.

## Starten

Alle vier Programme sind in `zahlen.sln` im Ordner `07_Klassen_und_Objekte` enthalten. Nach dem Build des Launchers erscheinen sie gemeinsam in der Kategorie `07 Klassen und Objekte`.

Einzelne Projekte lassen sich ebenfalls aus dem Repository-Stamm starten:

```powershell
dotnet run --project 07_Klassen_und_Objekte/Person1_Konstruktoren
dotnet run --project 07_Klassen_und_Objekte/Person2_Properties
dotnet run --project 07_Klassen_und_Objekte/Aquarium_Rechner
dotnet run --project 07_Klassen_und_Objekte/Konstruktoren_Beispiele
```

Die gemeinsame Repository-Konfiguration wählt .NET 7 oder .NET 10. Die drei Fenster benötigen Windows und den passenden .NET Desktop Runtime. Die Fakultät ist für `int` auf 0 bis 12 begrenzt; `0!` ist 1.

Der manuelle Aufruf von `GC.Collect()` gehört ausdrücklich zur Person1-Aufgabe. In gewöhnlichen Programmen übernimmt .NET die Speicherbereinigung selbst. Die Oberfläche bestätigt den Aufruf und das Setzen der Referenzen auf `null`.
