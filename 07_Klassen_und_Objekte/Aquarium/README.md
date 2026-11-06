# Aquarium

Selbstständig lauffähige Lösung der Schulaufgabe **Aquarium / Zoogeschäft Bremer**.

- `Aquarium.Core` – gemeinsame Fachklasse: Maße, Volumen, Glasfläche und Preis.
- `Aquarium.Console` – eigenständige Konsolenanwendung mit Eingaben und Beispielwerten.
- `Aquarium.Wpf` – WPF-Anwendung mit animierter, maßstabsgetreuer Vorschau.
- `docs/03_Aquarium.docx` – ursprüngliches Arbeitsblatt.

Beide Programme verwenden **dieselbe** Berechnung aus `Aquarium.Core`, ohne Codekopien oder Abhängigkeiten von anderen Aufgaben.

## Starten

Im Projektverzeichnis:

```powershell
dotnet run --project Aquarium.Console
dotnet run --project Aquarium.Wpf
```

Für WPF wird Windows mit der passenden .NET Desktop Runtime benötigt. Die Lösung `Aquarium.sln` kann unabhängig von `all-tasks-and-launcher` geöffnet werden.

Der Ziel-Framework-Wert ist wie im Ursprungsprojekt vom MSBuild/SDK abhängig (net7.0 oder net10.0), konfiguriert durch die lokale `Directory.Build.props`.

## Berechnung

- Maße in Zentimetern; alle müssen größer als 0 sein.
- Volumen (Liter): Länge × Breite × Höhe / 1000
- Glasfläche (dm²): (2 × Länge × Höhe + 2 × Breite × Höhe + Länge × Breite) / 100
- Endpreis: 20 € Grundbetrag + Glasfläche × Glaspreis, auf 5 € aufgerundet.

Beispiel: 80 × 35 × 35 cm, 0,40 €/dm² → **98 Liter**, **108,50 dm²**, **65 €**.

Das Verzeichnis ist im übergeordneten Repository zunächst eine eigenständige **Projektstruktur**, noch kein eigener Git-Remote oder Submodule. Es kann separat ausgecheckt und in ein eigenständiges Repository überführt werden.
