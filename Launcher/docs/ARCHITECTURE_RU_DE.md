# Архитектура / Architektur

## 1. Направление зависимостей / Abhängigkeitsrichtung

```text
Launcher
  -> Launcher.Apps.Windows -> Launcher.Apps
  -> Launcher.Pet.Windows  -> Launcher.Pet
  -> Launcher.Device
  -> Launcher.Apps
  -> Launcher.Pet
```

RU: Каталог, питомец и устройство не знают друг о друге. Между ними стоят конкретные классы в `Launcher/Connections`. `Startup` создаёт зависимости явно; контейнера и общей шины событий нет.
DE: Programmliste, Begleiter und Gerät kennen einander nicht. Konkrete Klassen in `Connections` verbinden sie. `Startup` erstellt Abhängigkeiten ausdrücklich, ohne Container oder allgemeinen Ereignisbus.

RU: Учебные ProjectReference в Launcher — список поставляемых программ. Они отделены от ссылок на библиотеки Launcher. Источник проверяет OutputType, поэтому библиотеки не становятся карточками.
DE: Lernprojekt-Verweise beschreiben mitgelieferte Programme. Sie stehen getrennt von Launcher-Bibliotheken. Nur ausführbare Projekte werden zu Karten.

## 2. Кто владеет состоянием / Wer besitzt den Zustand

| Владелец / Besitzer | Данные и ресурсы / Daten und Ressourcen |
| --- | --- |
| LauncherSession | Настройки приложения, связи, время жизни устройства / Einstellungen, Verbindungen, Gerätelebensdauer |
| AppList | Программы, фильтр, избранное, статистика / Programme, Filter, Favoriten, Statistik |
| MainWindow и панели / und Bereiche | Элементы управления и меню / Steuerelemente und Menüs |
| PetWorld | Закрытые состояния питомца, речи и шляпы / interne Zustände von Begleiter, Sprache und Hut |
| PetWindowsSession | Единственный таймер, изображения, дополнительные окна / einziger Timer, Bilder, Zusatzfenster |
| DeviceConnection | Последовательный порт, цикл опроса, очередь LED-команд / serieller Port, Abfrageschleife, LED-Warteschlange |

RU: Форма не меняет коллекции AppList. Для сохранения получается копия `AppListSettings`; результаты показа используют неизменяемые записи. Отрисовка читает `PetScene` и не меняет PetWorld.
DE: Das Fenster verändert keine AppList-Sammlungen. Einstellungen werden als Kopie exportiert. Die Zeichnung liest `PetScene`, ohne PetWorld zu verändern.

## 3. Каталог / Programmliste

```text
SearchPanel -> AppListConnection -> AppList -> AppSearch + AppSort
AppCard -> AppListConnection -> AppLaunch -> IAppStart -> AppProcess
ProjectAppSource -> проектные ссылки / Projektverweise
                 -> FolderAppSource при пустом результате / bei leerem Ergebnis
```

RU: `IAppSource` и `IAppStart` определены потребителем в Launcher.Apps. Windows-реализации зависят от этих границ. Фильтр не проверяет файлы: доступность приходит из источника и повторно проверяется непосредственно при запуске.
DE: `IAppSource` und `IAppStart` gehören dem Verbraucher Launcher.Apps. Windows-Implementierungen erfüllen diese Grenzen. Dateiverfügbarkeit wird beim Einlesen und unmittelbar vor dem Start geprüft.

RU: ID — нормализованный относительный путь EXE от каталога запуска, с `/` и единым регистром. Название и категория не являются ключами. Переименование или перемещение самого EXE изменяет ID. Программы с одинаковыми заголовками сохраняются, если пути различаются.
DE: Die ID ist der normalisierte relative EXE-Pfad mit `/` und einheitlicher Großschreibung. Titel und Kategorie sind keine Schlüssel. Ein anderer EXE-Pfad bedeutet eine andere ID; gleiche Titel bleiben erlaubt.

RU: «Все категории» — `null`. Отмена UAC имеет отдельный результат. Статистика увеличивается только после успешного старта процесса. Случайный старт использует тот же AppLaunch.
DE: „Alle Kategorien“ entspricht `null`. UAC-Abbruch ist ein eigenes Ergebnis. Nur ein erfolgreicher Prozessstart zählt; Zufallsstart verwendet denselben AppLaunch.

RU: XML читается напрямую, без вычисления MSBuild-выражений. Это соответствует текущим простым учебным csproj. При переходе на условные/вычисляемые ссылки потребуется источник на основе вычисленного проекта. Поиск EXE не проходит через вложенные ссылки каталогов и не скрывает ошибки чтения.
DE: XML wird direkt gelesen, ohne MSBuild-Auswertung. Für berechnete Projektverweise wäre später eine ausgewertete Quelle nötig. EXE-Suche folgt keinen untergeordneten Verzeichnisverknüpfungen und meldet Lesefehler.

## 4. Питомец / Begleiter

```text
PetWindowsSession
  читает время, мышь, область, рабочий стол / liest Zeit, Maus, Bereich, Desktop
    -> PetEnvironment -> PetWorld.Update
      -> HatWorld: физика и опоры / Physik und Auflagen
      -> PetBehavior: выбор действия / Aktionsauswahl
      -> PetIdle / PetWave / PetWalk / PetJump / PetLook / PetHatPickup / PetEarthquake
      -> PetSpeech: фраза и число символов / Satz und Zeichenanzahl
    <- PetScene
  PetDrawing + HatWindow + SpeechBubbleWindow + WindowShake
```

RU: В логике нет Form, Control, Bitmap, COM, дескрипторов, файлов или системных часов. Point/Rectangle из System.Drawing.Primitives используются как обычные числа геометрии. Время передаётся аргументом, Random — через конструктор.
DE: Die Logik enthält keine Fenster, Bitmaps, COM-Aufrufe, Handles, Dateien oder Systemuhr. Point/Rectangle sind reine Geometriedaten. Zeit und Zufall werden von außen übergeben.

RU: `PetPlacement` отвечает за границы и преобразование геометрии сцены. Local — координаты внутри PetArea, Screen — экранные. UI передаёт прямоугольники препятствий; питомец не знает типов карточек.
DE: `PetPlacement` berechnet Grenzen und Szenengeometrie. Local liegt in PetArea, Screen auf dem Bildschirm. Das UI liefert Hindernisrechtecke; Kartentypen sind unbekannt.

### Приоритеты / Prioritäten

| Ситуация / Situation | Решение / Entscheidung |
| --- | --- |
| Землетрясение / Erdbeben | Прерывает остальные действия; повтор не перезапускает / unterbricht andere Aktionen; kein Neustart bei Wiederholung |
| Подбор начат / Aufheben läuft | Защищён от взгляда и обычных действий / vor Blick und normalen Aktionen geschützt |
| Шляпа ждёт во время прыжка / Hut wartet beim Sprung | Завершить прыжок, затем подобрать / Sprung beenden, dann aufheben |
| Курсор внутри окна / Maus im Fenster | Взгляд прерывает обычное поведение и речь / Blick unterbricht Normalverhalten und Sprache |
| Прыжок и ходьба ждут речь / Sprung und Gehen warten | После речи сначала прыжок, затем ходьба / nach dem Sprechen zuerst springen, dann gehen |
| Ожидает действие / Aktion wartet | Новая речь не начинается / kein neuer Satz |
| Шляпу утащили / Hut weggezogen | Подбор отменяется или цель обновляется / Aufheben wird beendet oder Ziel erneuert |

RU: Переходы выполняет PetBehavior. Завершение махания сохраняет речь и ожидающие действия. Начало и конец прочих действий очищают временные данные. Расчёт кадра использует прошедшее время, а не число Tick.
DE: PetBehavior führt Übergänge aus. Winken erhält Sprache und wartende Aktionen. Frames richten sich nach vergangener Zeit statt nach Tick-Anzahl.

RU: Таймер Windows имеет интервал 10 мс; курсор считывается раз в 50 мс, снимок значков кэшируется на 500 мс, отладочное окно обновляется раз в 100 мс. Это не обещание точности Windows-таймера. Физика ограничивает шаг 50 мс. Сканирование рабочего стола включается только для снятой шляпы или отладочного показа.
DE: Windows-Timer: 10 ms, Maus: 50 ms, Symbolaufnahme: 500 ms, Diagnoseanzeige: 100 ms. Die Timerpräzision wird nicht garantiert. Der Physikschritt ist auf 50 ms begrenzt; Desktop-Abfragen laufen nur bei abgenommenem Hut oder Diagnoseanzeige.

RU: COM может обработать новые сообщения во время чтения. Снимок публикуется целиком; номер изменения проверяется до применения. Низкоуровневые объявления находятся в DesktopWindowApi и DesktopIconApi. Поверхности в логике имеют непрозрачный строковый ID, без Windows handles.
DE: COM kann während des Lesens Nachrichten verarbeiten. Nur vollständige Aufnahmen werden übernommen, sofern die Änderungsversion passt. Native Deklarationen liegen in den benannten API-Dateien; die Logik sieht nur undurchsichtige IDs.

RU: WindowShake двигает только обычное главное окно. Развёрнутое окно не перемещается. После ручного перемещения не восстанавливается устаревшая позиция. Смена темы не пересоздаёт карточки и не сбрасывает питомца.
DE: WindowShake bewegt nur das normale Hauptfenster. Maximierte Fenster bleiben stehen; manuelle Verschiebungen werden nicht rückgängig gemacht. Ein Farbwechsel erzeugt weder neue Karten noch einen neuen Begleiterzustand.

## 5. Устройство / Gerät

RU: DeviceConnection выполняет один фоновый цикл, который единолично использует порт. LED-команды ожидают в Channel; опрос и команды не пересекаются. Ошибка подключения даёт отключённое состояние; повторный поиск ограничен паузой. Закрытие отменяет ожидания и дожидается выхода цикла.
DE: Eine Hintergrundschleife besitzt den Port. LED-Befehle warten im Channel; Befehle und Abfragen überlappen nicht. Verbindungsfehler führen zum getrennten Zustand und einer Pause vor erneuter Suche. Beim Schließen wird der Ablauf abgebrochen und abgewartet.

RU: DevicePanelConnection передаёт результаты в UI через BeginInvoke. DevicePetConnection связывает только смысловое событие кнопки и действие питомца. Устройство не знает GPIO-поведения питомца или элементов формы.
DE: DevicePanelConnection stellt UI-Ergebnisse über BeginInvoke zu. DevicePetConnection verbindet ausschließlich die Bedeutung einer Taste mit einer Begleiteraktion.

Протокол / Protokoll: `HELLO` → `LAUNCHER_IO 1`; `POLL` → `INPUT NONE` / `INPUT BUTTON_PRESSED`; `INDICATOR ON/OFF` → `OK INDICATOR`.
`HELLO` очищает старые нажатия / verwirft alte Tastendrücke. Электрические параметры сохранены / elektrische Parameter unverändert.

## 6. Запуск, закрытие, ошибки / Start, Ende, Fehler

RU: Program → LauncherStartup → LauncherSession. Сначала читаются настройки и создаются связи. Shown загружает каталог, запускает питомца и устройство. Отсутствие изображений отключает питомца с сообщением, оставляя каталог доступным.
DE: Einstellungen und Verbindungen werden vor Shown aufgebaut. Shown lädt Programme und startet Begleiter und Gerät. Fehlende Bilder deaktivieren nur den Begleiter und erzeugen einen Hinweis.

RU: Закрытие блокирует новые действия; ждёт завершения устройства; останавливает питомца и тряску; сохраняет настройки; закрывает форму. Dispose снимает связи и освобождает ресурсы. Повторный Close не запускает второе закрытие.
DE: Schließen sperrt Aktionen, wartet auf das Gerät, stoppt Begleiter und Bewegung, speichert und schließt das Fenster. Dispose löst Verbindungen und Ressourcen; wiederholtes Close startet keinen zweiten Ablauf.

RU: Ошибки ожидаемых внешних операций превращаются в результат или видимый текст. Пустые catch допускаются только для ожидаемой отмены/исчезнувшего окна. Повреждённые настройки копируются до замены; временный файл удаляется после неудачной записи.
DE: Erwartbare externe Fehler werden Ergebnisse oder sichtbare Hinweise. Stilles Abfangen bleibt erwarteter Abbruch-/Fensterschließlogik vorbehalten. Beschädigte Einstellungen werden gesichert; temporäre Dateien werden aufgeräumt.

## 7. Как расширять / Erweiterungswege

1. **Действие каталога / Listenaktion:** добавить смысл в AppCardAction, обработать в AppListConnection; правило — в Launcher.Apps, системный вызов — в Launcher.Apps.Windows. / Bedeutung verbinden, Regel und Systemaufruf getrennt halten.
2. **Поведение питомца / Begleiteraktion:** добавить режим, класс расчёта и переходы в PetBehavior; кадры — в каталоге анимаций. Windows не решает приоритеты. / Modus, Berechnung und Übergänge hinzufügen; Windows wählt keine Priorität.
3. **Команда платы / Gerätebefehl:** согласованно изменить DeviceProtocol, очередь/обмен и прошивку, обновить таблицу протокола. / Protokoll, Austausch und Firmware gemeinsam ändern.
4. **Связь возможностей / Verbindung:** добавить конкретную подписку в Connections и её снятие в Dispose. Например, событие устройства вызывает метод PetWindowsSession; прямой ссылки Device → Pet нет. / Konkrete Verbindung anlegen und wieder lösen; keine direkte Geräteabhängigkeit zum Begleiter.

RU: Новый самостоятельный тип — новый файл; namespace повторяет папку. Значения настройки находятся рядом со своим поведением, единицы видны в именах. Дополнительный проект нужен для самостоятельной границы зависимости, а не для каждого класса.
DE: Jeder eigenständige Typ hat eine Datei; Namespaces folgen Ordnern. Einstellwerte stehen bei ihrem Verhalten und nennen Einheiten. Neue Projekte bilden Abhängigkeitsgrenzen, keine Einzelklassen.
