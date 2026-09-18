# Launcher — карта проекта / Projektkarte

Launcher запускает учебные программы, показывает питомца и принимает события ESP8266.
Launcher startet Lernprogramme, zeigt einen Begleiter und verarbeitet ESP8266-Eingaben.

## Проекты / Projekte

| Проект / Projekt | Назначение / Aufgabe |
| --- | --- |
| Launcher | Запуск, окно, настройки, связи / Start, Fenster, Einstellungen, Verbindungen |
| Launcher.Apps | Каталог и правила запуска / Programmliste und Startregeln |
| Launcher.Apps.Windows | Проектные файлы, EXE, процессы / Projektdateien, EXE, Prozesse |
| Launcher.Pet | Поведение, речь, шляпа / Verhalten, Sprache, Hut |
| Launcher.Pet.Windows | Рисование и рабочий стол / Zeichnen und Desktop |
| Launcher.Device | Порт, команды, прошивка / Port, Befehle, Firmware |

Все шесть проектов находятся в папке решения `Launcher`. Библиотеки лежат рядом с запускаемым проектом в репозитории.
Alle sechs Projekte stehen im Lösungsordner `Launcher`. Bibliotheken liegen neben dem Startprojekt im Repository.

## Порядок чтения / Lesereihenfolge

1. `Program` → `LauncherStartup` → `LauncherSession`: создание и время жизни / Aufbau und Lebensdauer.
2. `AppListConnection` → `AppList`, `AppSearch`, `AppSort`: действия каталога / Listenaktionen.
3. `ProjectAppSource` → `FolderAppSource`: источники программ / Programmquellen.
4. `AppLaunch` → `AppProcess`: запуск и статистика / Start und Statistik.
5. `PetWindowsSession` → `PetWorld` → `PetBehavior`: входные данные, решения, отображение / Eingaben, Entscheidungen, Anzeige.
6. `DeviceConnection` → `DeviceProtocol` + `SerialExchange`: последовательный обмен / serieller Austausch.
7. `DevicePetConnection`: кнопка платы вызывает землетрясение / Gerätetaste löst Erdbeben aus.

## Настройки и ресурсы / Einstellungen und Ressourcen

- Новый файл: `%LOCALAPPDATA%/zahlen-launcher/launcher-settings.json`. Старый `launcher-state.json` не читается и не изменяется. / Neue Datei; alte Zustandsdatei bleibt unverändert.
- Повреждённый JSON копируется в `.broken-*` до сброса. Если копирование или чтение невозможно, запись блокируется. / Beschädigte Daten werden gesichert; bei fehlendem sicheren Zugriff wird nicht überschrieben.
- Тема и показ столкновений — независимые настройки. / Farbthema und Kollisionsanzeige sind unabhängig.
- PNG принадлежат `Launcher.Pet.Windows`, копируются в `Resources` при сборке и публикации. / Bilder gehören der Windows-Begleiterbibliothek und werden mitgeliefert.
- Прошивка: `Launcher.Device/Firmware/LauncherIoEsp8266`. После изменения протокола требуется соответствующая прошивка. / Die Firmware muss zum Protokoll passen.

## Платформа / Plattform

Корневой `Directory.Build.props` выбирает .NET 7 либо .NET 10 по версии MSBuild. Windows-проекты явно добавляют `-windows`. WinForms используется только в `Launcher` и `Launcher.Pet.Windows`. `System.IO.Ports` принадлежит `Launcher.Device`.
Die zentrale Datei wählt .NET 7 oder .NET 10 anhand der MSBuild-Version. Windows-Projekte ergänzen `-windows`; WinForms und SerialPort bleiben in ihren zuständigen Projekten.

Профиль `Windows-x64` остаётся у Launcher и публикует автономное приложение в корневую папку `publish`.
Das Profil `Windows-x64` bleibt im Startprojekt und erzeugt die eigenständige Ausgabe im Ordner `publish`.

## Документация / Dokumentation

- [Архитектура / Architektur](docs/ARCHITECTURE_RU_DE.md)
- [Проверка и сценарии / Prüfung und Szenarien](docs/ANALYSIS_CHECKLIST_RU_DE.md)
- [Термины / Begriffe](docs/GLOSSARY_RU_DE.md)

Во время этого рефакторинга сборка, тесты, запуск и прошивка не выполнялись. Статический разбор не подтверждает работу интерфейса или оборудования.
Während dieses Refactorings wurden Build, Tests, Programmstart und Firmware-Upload nicht ausgeführt. Statische Prüfung bestätigt weder Oberfläche noch Hardwarebetrieb.
