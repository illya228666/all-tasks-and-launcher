# Переработка MVP / MVP-Überarbeitung

База / Basis: `4725c4f8`. Исходный конец / alter Stand: `ff862f0`.
Резервная ветка / Sicherung: `codex/mvp-original-ff862f0`.
Новая серия / neue Reihe: `codex/architecture-rewrite`.

## Замысел и результат / Absicht und Ergebnis

| Старый коммит | Замысел / Absicht | Результат / Ergebnis |
| --- | --- | --- |
| 677e379f | Передача геометрии / Geometrieübergabe | Единое преобразование сцены и кликов / gemeinsame Transformation |
| 55c2de1c | Общие пресеты кадров / Frame-Presets | Вместо компенсаций нормализации — исходный масштаб и якоря / Quellmaßstab und Anker |
| cb66e41 | Ракурс вместе с наклоном / Ansicht mit Neigung | HatVisualPose и Drawing-кэш / Szenenpose und Cache |
| f3aeadb | Пропорции первых строк / Proportionen | Исходные размеры всех 11 строк / alle 11 Quellzeilen |
| 9254235 | Мягкое приземление / sanfte Landung | Settling и корректное RGBA-смешивание / korrekte Mischung |
| d8c33b4 | Независимость ракурса / unabhängige Ansicht | Отдельная временная фаза / eigene Zeitphase |
| 3b1c93b | Непрерывное падение / fortlaufender Fall | Цикл ракурса до столкновения / Ansichtszyklus bis Kontakt |
| 7717b8f | Равномерный масштаб / gleichmäßiger Maßstab | Один масштаб без растяжения поз / keine Verzerrung |
| 10b7f2f | v2 и chaos / v2 und Chaos | Версии/возможности сохранены; назначения v2 и chaos убраны по согласованию / Versionen behalten, Aktionen entfernt |
| f910a73 | Новая распиновка / Pinwechsel | Конфигурация платы / BoardConfig |
| c77f4a6 | Видимость и диагностика / Sichtbarkeit und Diagnose | Отдельная строка UI, классификация ошибок / eigene UI-Zeile, Fehlerklassen |
| e9912fb | Согласование проводов / Verdrahtungsabgleich | Окончательная конфигурация / endgültige Konfiguration |
| ff862f0 | Разная полярность / unterschiedliche Polarität | Параметры двух входов / zwei Eingangsparameter |

## Решения / Entscheidungen

Сохранены шесть производственных проектов и явные Connections. По окончательному указанию пользователя тестовые проекты и тестовые зависимости отсутствуют; финальная проверка — только сборка.
Sechs Produktionsprojekte und konkrete Connections bleiben. Auf abschließenden Nutzerwunsch gibt es keine Testprojekte oder Testabhängigkeiten; Abschlussprüfung nur per Build.

Геометрия 4725c4f8 также скорректирована: покадровая нормализация изображений противоречила естественным пропорциям. Масштаб задаётся в коде; редактор кадров и runtime-настройки не добавлялись.
Auch die Geometrie von 4725c4f8 wurde angepasst: Einzelbildnormalisierung widersprach natürlichen Proportionen. Maßstab bleibt Codekonfiguration, ohne Editor oder Laufzeiteinstellungen.

API устройства изменён намеренно: вместо bool принятия команды используется Task с итогом. Indicator не эмулируется через зелёный LED. Ожидающие состояния объединяются; входные события не объединяются.
Die Geräte-API liefert absichtlich Task-Ergebnisse statt Annahme-bool. Indicator wird nicht durch Grün emuliert. Wartende Ausgabezustände werden ersetzt, Eingabeereignisse nicht.

Шляпа сохраняет прежний профиль столкновения и физический шаг до 50 мс. Это стилизованное движение, не точная 3D-симуляция. Цикл ракурса: 1,8 с в одну сторону; успокоение: 0,35 с.
Der Hut behält Kollisionsprofil und maximal 50-ms-Physikschritt. Es bleibt stilisierte Bewegung, keine exakte 3D-Simulation. Ansicht: 1,8 s pro Richtung; Beruhigung: 0,35 s.

Проверки и ограничения: [ANALYSIS_CHECKLIST_RU_DE.md](ANALYSIS_CHECKLIST_RU_DE.md).
