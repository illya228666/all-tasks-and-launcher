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

## Землетрясение Launcher / Launcher-Erdbeben
- RU: `PetController.TryStartEarthquake()` — подготовленная точка входа без подключённого вызова. Режим `Earthquake` на 5000 мс прерывает обычные анимации, включая прыжок, речь и подбор шляпы. Использует существующий animation timer и абсолютное время.
- DE: Vorbereiteter Einstieg ohne Ausloeser; unterbricht normale Animationen fuer 5000 ms, mit bestehendem Timer und absoluter Zeit.
- RU: Кадры и длительность находятся в `PetAnimationCatalog`; `HatController` отвечает за слёт и обычную физику шляпы; `PetRenderer` — за дрожь питомца и перемещение только главной формы Launcher в оконном режиме. Развёрнутое окно не перемещается, окна других проектов не затрагиваются.
- DE: Der Katalog liefert die Frames, der Hut-Controller die Hutphysik, der Renderer bewegt nur das normale Launcher-Fenster. Andere Projektfenster bleiben unveraendert.
- RU: По завершении восстанавливаются положение формы и обычное поведение питомца, включая подбор упавшей шляпы. Stop/dispose или перекомпоновка завершают эффект досрочно; ручное перемещение формы не отменяется восстановлением старой позиции.
- DE: Abschluss stellt Position und Normalbetrieb wieder her; Stop/dispose oder Layoutwechsel beenden den Effekt vorzeitig. Manuelles Verschieben wird nicht rueckgaengig gemacht.
