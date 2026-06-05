# Architecture (RU + DE)

## Поток данных / Datenfluss
1. UI (`Launcher.UI.Main`) получает пользовательские действия.
2. UI вызывает `LauncherFacade`.
3. `LauncherFacade` использует:
   - `IAppDiscoveryService` (поиск приложений),
   - `AppFilterService` (фильтрация),
   - `AppSortService` (сортировка),
   - `IStateStorageService` (состояние).
4. Результат возвращается в UI.
5. UI рендерит карточки (`AppCardControl`).

## Ответственности / Verantwortlichkeiten
- `Domain`: RU: только данные; DE: nur Daten.
- `Application`: RU: правила работы; DE: Arbeitsregeln.
- `Infrastructure`: RU: доступ к файлам/JSON; DE: Datei-/JSON-Zugriff.
- `UI`: RU: отображение и события; DE: Darstellung und Events.

## Почему это удобно учить / Warum das lernfreundlich ist
- RU: Легко смотреть один слой за раз.
- DE: Man kann jede Schicht getrennt verstehen.
- RU: Меньше "магии" в форме.
- DE: Weniger "Magie" im Formular.
- RU: Проще находить ошибки и тестировать.
- DE: Fehler finden und testen wird einfacher.
