# Klassen und Objekte

Lösungen der Aufgaben aus `tasks.zip` mit WPF-Oberflächen und Konsolenbeispielen. Die Originalblätter liegen unter `Aufgaben`.

| Projekt | Inhalt |
| --- | --- |
| Person1_Konstruktoren | Vier Konstruktoren und Referenzen/GC |
| Person2_Properties | Konstruktoren und Properties für Name/Alter |
| [Aquarium](Aquarium/) | Eigenständige Lösung mit `Aquarium.Core`, `Aquarium.Wpf` und `Aquarium.Console` |
| Konstruktoren_Beispiele | Konsolenprogramm für Person1, Person2 und `CNatZahl` (Fakultät) |

Die WPF-Anwendungen für Person1 und Person2 verwenden weiterhin `Gemeinsam/Stil.xaml`. Aquarium besitzt seine eigene Kopie der benötigten Stilressourcen und kann unabhängig von den übrigen Aufgaben gebaut und gestartet werden.

## Starten

```powershell
dotnet run --project 07_Klassen_und_Objekte/Person1_Konstruktoren
dotnet run --project 07_Klassen_und_Objekte/Person2_Properties
dotnet run --project 07_Klassen_und_Objekte/Konstruktoren_Beispiele
dotnet run --project 07_Klassen_und_Objekte/Aquarium/Aquarium.Console
dotnet run --project 07_Klassen_und_Objekte/Aquarium/Aquarium.Wpf
```

Alle Anwendungen sind in `zahlen.sln` eingebunden und erscheinen im Launcher. Die Details und Berechnungsformeln für Aquarium stehen in [Aquarium/README.md](Aquarium/README.md).
