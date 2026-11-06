# all-tasks-and-launcher

C#-Übungsprojekte mit einem Windows-Launcher für Konsolen- und WPF-Anwendungen. Die gemeinsame Lösung ist `zahlen.sln`. Aquarium hat ein eigenes Repository und ist unter `07_Klassen_und_Objekte/Aquarium` als Git-Submodul eingebunden.

## Klonen und starten

Die Befehle für Build, Start und Veröffentlichung benötigen Windows und ein .NET SDK. Die Hauptprojekte verwenden mit dem .NET 10 SDK das Ziel .NET 10 und mit älteren unterstützten SDKs das Ziel .NET 7.

```powershell
git clone --recurse-submodules https://github.com/illya228666/all-tasks-and-launcher.git
cd all-tasks-and-launcher
dotnet build zahlen.sln
dotnet run --project Launcher
```

In einem vorhandenen Checkout nach einem Pull das Submodul auf die festgelegte Revision bringen:

```powershell
git submodule update --init --recursive
```

Falls Git bei einem Checkout von vor der Umstellung meldet, dass das Aquarium-Verzeichnis bereits Dateien enthält, prüfe den Inhalt. Wenn dort nur alte Build-Artefakte liegen, verschiebe das gesamte Verzeichnis `07_Klassen_und_Objekte/Aquarium` aus dem Checkout und führe den Submodulbefehl erneut aus.

## Aquarium separat verwenden

```powershell
git clone https://github.com/illya228666/Aquarium.git
cd Aquarium
dotnet build Aquarium.sln
```

Im Hauptrepository erscheinen die Konsolen- und WPF-Version von Aquarium im Launcher. In `zahlen.sln` stehen ihre Projekte mit der gemeinsamen Bibliothek im Lösungsordner `07_Klassen_und_Objekte/Aquarium`.

## Launcher veröffentlichen

```powershell
dotnet publish Launcher/Launcher.csproj -c Release -r win-x64 --self-contained true -p:PublishProfile=Windows-x64
```

Die Laufzeitoptionen auf der Kommandozeile gelten auch für die Aquarium-Projekte. Das Windows-x64-Paket wird lokal unter `publish/` erzeugt. Weitere Informationen stehen in der [Launcher-Dokumentation](Launcher/README.md) und in der [Übersicht zu Klassen und Objekten](07_Klassen_und_Objekte/README.md).
