# Проверка / Prüfung

## Выполнено статически / Statisch bearbeitet

- C# синтаксис разобран без компиляции / C#-Syntax ohne Kompilierung gelesen.
- Проверены ссылки проектов, направления зависимостей и отдельные Windows-targets / Projektverweise und Windows-Ziele geprüft.
- Проверены имена протокола клиента и прошивки / Protokollnamen auf beiden Seiten abgeglichen.
- Просмотрены владение состоянием, отмена, подписки и освобождение ресурсов / Besitz, Abbruch, Ereignisse und Freigabe durchgesehen.
- Проверены пути изображений, иконки и профиля публикации / Bild-, Symbol- und Veröffentlichungspfade geprüft.

Сборка, тесты, запуск приложения, публикация и прошивка не выполнялись по запросу пользователя.
Build, Tests, Programmstart, Veröffentlichung und Firmware-Upload wurden auf Benutzerwunsch nicht ausgeführt.
Синтаксический разбор не проверяет связывание типов, работу WinForms, COM, serial или фактическую публикацию.
Syntaxanalyse bestätigt keine Typbindung, WinForms-, COM-, Serial- oder Veröffentlichungsfunktion.

## Сценарии для будущего разрешённого прогона / Szenarien für einen später erlaubten Lauf

Все пункты ниже НЕ выполнены / Alle folgenden Punkte sind NICHT ausgeführt.

| Сценарий / Szenario | Ожидание / Erwartung |
| --- | --- |
| SDK 7 и SDK 10 / beide SDKs | Правильные целевые платформы; стартовый проект Launcher / passende Ziele, Launcher als Startprojekt |
| Windows-x64 publish | Учебные EXE и три PNG присутствуют; библиотеки не показаны как программы / Lernprogramme und Bilder vorhanden, keine Bibliothekskarten |
| Повреждённый/недоступный csproj / defekte Projektdatei | Остальной каталог доступен, ошибка видна / übrige Programme bleiben sichtbar |
| Вне репозитория / außerhalb Repository | EXE-поиск без Launcher и createdump / gefilterte EXE-Suche |
| Одинаковые заголовки / gleiche Titel | Разные пути дают разные карточки и статистику / getrennte Karten und Statistik |
| Файл удалён после обновления / EXE danach gelöscht | Запуск сообщает об отсутствии, статистика не растёт / kein Startzähler |
| UAC отменён / abgebrochen | Отдельный результат, статистика неизменна / eigener Status, kein Zähler |
| Фильтры, избранное, случайный старт / Filter, Favoriten, Zufall | Одна логика запуска; восстановление нового сохранения / gemeinsamer Startpfad |
| Повреждённый JSON, нет прав / JSON-Fehler, fehlende Rechte | Резервная копия либо блокировка записи; ошибка видна / Sicherung oder Schreibsperre mit Hinweis |
| Тема и отладка / Thema und Diagnose | Независимы; речь и прокрутка сохраняются / unabhängig, Sprache und Scrollposition bleiben |
| Размер окна, пустой список / Fenstergröße, leere Liste | Питомец остаётся доступным, области обновляются / Begleiterbereich bleibt gültig |
| Нет изображений / fehlende Bilder | Каталог работает, ошибка питомца видна / Programmliste bleibt nutzbar |
| USB отключён при POLL/LED / USB-Trennung | Нет параллельного обмена, есть переподключение / serieller Ablauf und Wiederverbindung |
| Несколько LED-команд / mehrere Befehle | Очередь сохраняет порядок / Reihenfolge bleibt erhalten |
| Закрытие во время поиска/ответа / Schließen beim Warten | Отмена и завершение до освобождения порта / Abbruch vor Portfreigabe |
| HELLO и короткие нажатия / kurze Tastendrücke | Подключение не имитирует нажатие; нажатия живут до POLL / Verbindung erzeugt keine Eingabe |
| Землетрясение во время прыжка/речи/подбора / Erdbeben während Aktion | Одно прерывание, повтор не продлевает / ein Übergang ohne Verlängerung |
| Речь и ожидающие действия / Sprache und wartende Aktionen | Прыжок раньше ходьбы; новая речь не мешает / Sprung vor Gehen, kein neuer Satz |
| Курсор и ожидающая шляпа / Maus und wartender Hut | Защищённый прыжок заканчивается, затем подбор / geschützter Sprung vor Aufheben |
| Утащить шляпу во время подбора / Hut beim Aufheben wegziehen | Отмена или смена цели, без телепортации / Abbruch oder neues Ziel |
| Опора перемещена/закрыта / Auflage bewegt/geschlossen | Шляпа следует за опорой либо падает / Hut folgt oder fällt |
| Explorer перезапущен / Explorer-Neustart | Старые идентификаторы не применяются / keine veralteten Identitäten |
| Несколько экранов, DPI, прокрутка / Monitore, DPI, Scrollen | Корректные Screen/Local координаты и обрезка / korrekte Koordinaten und Grenzen |
| Ручное перемещение при тряске / manuelle Verschiebung | Старая позиция не восстанавливается / alte Position wird nicht erzwungen |
| Закрытие из COM/оконного сообщения / Schließen während COM | Старый снимок не применяется, ресурсы освобождены / keine veraltete Aufnahme |
| Многократное обновление карточек / wiederholte Listenaktualisierung | Старые меню, контролы и шрифты освобождаются / alte Ressourcen werden freigegeben |
