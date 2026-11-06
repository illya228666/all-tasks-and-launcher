# Klassen und Objekte

Lösungen der Aufgaben aus `tasks.zip` mit WPF-Oberflächen und Konsolenbeispielen. Die Originalblätter liegen unter `Aufgaben`. Das [Aquarium-Arbeitsblatt](https://github.com/illya228666/Aquarium/blob/d36d90a3be62c48526ffb6e873366f27f7312a80/docs/03_Aquarium.docx) liegt im Submodul.

| Projekt | Inhalt |
| --- | --- |
| Person1_Konstruktoren | Vier Konstruktoren und Referenzen/GC |
| Person2_Properties | Konstruktoren und Properties für Name/Alter |
| [Aquarium](https://github.com/illya228666/Aquarium) | Git-Submodul mit eigener Lösung für `Aquarium.Core`, `Aquarium.Wpf` und `Aquarium.Console` |
| Konstruktoren_Beispiele | Konsolenprogramm für Person1, Person2 und `CNatZahl` (Fakultät) |

Die WPF-Anwendungen für Person1 und Person2 verwenden weiterhin `Gemeinsam/Stil.xaml`. Aquarium besitzt seine eigene Kopie der benötigten Stilressourcen und kann unabhängig von den übrigen Aufgaben gebaut und gestartet werden.

## Starten

Die folgenden Befehle werden im Stammordner des Hauptrepositories ausgeführt.

```powershell
git submodule update --init --recursive
dotnet run --project 07_Klassen_und_Objekte/Person1_Konstruktoren
dotnet run --project 07_Klassen_und_Objekte/Person2_Properties
dotnet run --project 07_Klassen_und_Objekte/Konstruktoren_Beispiele
dotnet run --project 07_Klassen_und_Objekte/Aquarium/Aquarium.Console
dotnet run --project 07_Klassen_und_Objekte/Aquarium/Aquarium.Wpf
```

Alle Anwendungen sind in `zahlen.sln` eingebunden und erscheinen im Launcher. Für die eigenständige Arbeit mit Aquarium öffne `Aquarium/Aquarium.sln`. Die Details und Berechnungsformeln stehen in der README des [Aquarium-Repositories](https://github.com/illya228666/Aquarium).
