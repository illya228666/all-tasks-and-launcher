# Launcher (Учебный проект / Lernprojekt)

## RU: Что это
`Launcher` — учебный WinForms-проект для школьников (8-11 класс), который показывает:
- как разделять UI, доменные модели, сервисы и инфраструктуру;
- как делать читаемый и анализируемый код;
- как сопровождать код двуязычной документацией (RU + DE).

## DE: Was ist das
`Launcher` ist ein WinForms-Lernprojekt fuer Schuelerinnen und Schueler (Klasse 8-11), das zeigt:
- wie man UI, Domänenmodelle, Services und Infrastruktur trennt;
- wie man lesbaren und analysierbaren Code schreibt;
- wie man Code mit zweisprachiger Doku (RU + DE) begleitet.

## RU: Быстрый старт
1. Открыть `zahlen.sln` в Visual Studio.
2. Выбрать проект запуска: `Launcher`.
3. Выполнить Build (`Ctrl+Shift+B`) и Run (`F5`).

## DE: Schnellstart
1. `zahlen.sln` in Visual Studio oeffnen.
2. `Launcher` als Startprojekt waehlen.
3. Build (`Ctrl+Shift+B`) und Run (`F5`) ausfuehren.

## Структура проекта / Projektstruktur
- `Domain/` — RU: модели данных; DE: Datenmodelle.
- `Application/` — RU: фасад и прикладная логика; DE: Fassade und Anwendungslogik.
- `Infrastructure/` — RU: файловая/JSON-инфраструктура; DE: Datei-/JSON-Infrastruktur.
- `UI/` — RU: форма и визуальная координация; DE: Formular und visuelle Koordination.
- `UI/Controls/` — RU: пользовательские контролы; DE: User Controls.
- `docs/` — RU+DE учебные материалы.

## RU: Маршрут анализа для школьника
1. Начни с `Program.cs` (точка входа).
2. Открой `UI/Main.cs` и найди поля/конструктор.
3. Посмотри `BindEvents` в `UI/Main.Events.cs`.
4. Проследи цепочку `LoadAppCatalog -> LauncherFacade.LoadApps`.
5. Изучи `Infrastructure/ProjectReferenceDiscoveryService.cs`.
6. Найди, как формируется state-файл (`LauncherConstants`).
7. Проверь чтение/запись в `JsonStateStorageService`.
8. Посмотри фильтрацию в `AppFilterService`.
9. Посмотри сортировку в `AppSortService`.
10. Вернись в `UI/Main.Rendering.cs` и свяжи данные с карточкой.
11. Открой `UI/Controls/AppCardControl.cs`.
12. Проследи, как событие кнопки карточки доходит до запуска EXE.
13. Проверь, как обновляется статистика запусков.
14. Переключи тему и найди код в `UI/Main.Theme.cs`.
15. Повтори анализ, рисуя собственную схему потока данных.

## DE: Analyse-Route fuer Schueler
1. Starte mit `Program.cs` (Einstiegspunkt).
2. Oeffne `UI/Main.cs` und finde Felder/Konstruktor.
3. Sieh dir `BindEvents` in `UI/Main.Events.cs` an.
4. Verfolge `LoadAppCatalog -> LauncherFacade.LoadApps`.
5. Analysiere `Infrastructure/ProjectReferenceDiscoveryService.cs`.
6. Finde, wie der State-Pfad gebaut wird (`LauncherConstants`).
7. Pruefe Laden/Speichern in `JsonStateStorageService`.
8. Verstehe die Filterlogik in `AppFilterService`.
9. Verstehe die Sortierlogik in `AppSortService`.
10. Gehe zu `UI/Main.Rendering.cs` und verbinde Daten mit Karte.
11. Oeffne `UI/Controls/AppCardControl.cs`.
12. Verfolge, wie ein Button-Event bis zum EXE-Start laeuft.
13. Pruefe, wie die Start-Statistik aktualisiert wird.
14. Wechsle das Theme und finde den Code in `UI/Main.Theme.cs`.
15. Wiederhole die Analyse mit eigenem Datenfluss-Diagramm.

## Полезные документы / Weitere Dokumente
- `docs/ARCHITECTURE_RU_DE.md`
- `docs/GLOSSARY_RU_DE.md`
- `docs/ANALYSIS_CHECKLIST_RU_DE.md`
