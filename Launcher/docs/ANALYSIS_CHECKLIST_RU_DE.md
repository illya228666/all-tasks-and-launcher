# Проверка сборки / Buildprüfung

По окончательному указанию пользователя в результирующей серии нет тестовых проектов,
тестов, тестовых зависимостей и снимков проверки. Финальная проверка ограничена сборкой Launcher.
Gemäß abschließender Nutzeranweisung enthält die Ergebnisreihe keine Testprojekte,
Tests, Testabhängigkeiten oder Prüfaufnahmen. Die Abschlussprüfung beschränkt sich auf den Launcher-Build.

## Команды / Befehle

```powershell
dotnet build Launcher/Launcher.csproj -c Debug
dotnet build Launcher/Launcher.csproj -c Release
```

Среда / Umgebung: Windows, .NET SDK 10.0.401.
Результат финальной сборки: Debug и Release успешны, 0 ошибок; в каждой сборке прежнее предупреждение CS8602 в учебном Wohngeld/Program.cs:19.
Abschlussergebnis: Debug und Release erfolgreich, 0 Fehler; jeweils die bestehende CS8602-Warnung in Wohngeld/Program.cs:19.
Сборка не подтверждает работу оборудования, взаимодействие с Explorer/COM или визуальное поведение.
Ein erfolgreicher Build bestätigt weder Hardware, Explorer/COM noch visuelles Laufzeitverhalten.
Совместимость с SDK 7 отдельной сборкой не проверялась / SDK 7 wurde nicht separat gebaut.

До уточнения требований выполнялись программные проверки, снимки и пробная публикация.
Добавленные для этого исходники, ссылки и артефакты удалены; после уточнения выполняется только сборка.
Vor der Präzisierung liefen Softwareprüfungen, Aufnahmen und eine Probeveröffentlichung.
Dafür ergänzte Quellen, Referenzen und Artefakte wurden entfernt; danach wird nur gebaut.
