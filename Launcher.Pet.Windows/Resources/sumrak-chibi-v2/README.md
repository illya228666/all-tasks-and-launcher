# Sumrak chibi v2 — 11 действий, 148 кадров

Все 11 действий созданы по утверждённому мастеру. Код приложения и старые спрайты не изменены. Художественные листы созданы imagegen, обработка повторно использует generate2dsprite. Откройте `preview.html` непосредственно с диска для воспроизведения и покадрового просмотра на светлом, тёмном и шахматном фоне при высоте персонажа 160/200/220 px.

## Outputs

- `master/raw-master.png`: selected HD chibi master, original generation resolution.
- `master/single-1.png`: transparent master.
- `idle/raw-sheet.png`: original 3-row, 4-column sheet (1448 x 1086).
- `idle/sheet-transparent.png`: processed transparent sheet; cells 362 x 362.
- `idle/idle-1.png` through `idle/idle-12.png`: chronological frames, left to right across rows.
- `idle/animation.gif`: loop preview, 140 ms per frame, 1680 ms per cycle.
- `character-scale-profile.json`: common processing scale and foot-contact origin for later compatible grounded actions.
- Per-action `pipeline-meta.json`: frame geometry and QC metrics. Per-action `prompt-used.txt`: final generation instruction. Master prompt refers to the first generated adult-proportion version; the user-supplied concept is the original identity reference.

## Approval and verification

The idle has a single blink at frames 5–7 and subtle breathing/cloth motion. Strict processor QC passes for all 12 frames with no empty frames, source/output edge contact or paste clamping. Processing uses one uniform scale, never individual frame enlargement. GIF is provided for human motion and loop approval; temporal artistic quality is not established by numeric QC alone.

Transparent output uses magenta chroma cleanup (threshold 100, edge threshold 180). Original generation images are retained for further cleanup if necessary. Review at 160–220 px character height as well as native resolution before accepting the appearance.

## Комплект и тайминг

| Каталог | Кадры | мс/кадр | Повтор |
|---|---:|---:|---|
| idle | 12 | 140 | цикл |
| walk | 16 | 80 | цикл |
| run | 16 | 60 | цикл |
| jump | 16 | 80 | однократно |
| sit-down | 12 | 100 | однократно |
| seated-idle | 12 | 140 | цикл |
| sleep | 12 | 180 | цикл |
| wake-up | 12 | 110 | однократно |
| wave | 12 | 100 | однократно |
| surprise | 12 | 100 | однократно |
| hat-trick | 16 | 90 | однократно |

В каждом каталоге сохранены исходный `raw-sheet.png`, очищенный `raw-sheet-clean.png`, `sheet-transparent.png`, отдельные нумерованные PNG, GIF, промпты и метаданные. `references/concept.png` сохраняет исходный концепт. Кадры идут слева направо и сверху вниз: 12 кадров — 3 строки × 4 столбца; 16 — 4 × 4. `bundle.json` содержит порядок кадров, тайминг, опорные точки и итоговую проверку. GIF повторяют и однократные действия для просмотра; HTML дополнительно держит последний кадр 500 мс. Эти повторы не являются игровым поведением.

## Геометрия и окончательная проверка

Ячейка 362 × 362 px, общая опора `(181, 335)` от верхнего левого угла. Наземные действия используют общий профиль и линию стоп без индивидуального растягивания. Прыжок и трюк со шляпой используют фиксированную исходную опору первого кадра, сохраняющую траекторию и смещение шляпы. Отдельная шляпа не отфильтрована как шум.

Для wake-up исходные ячейки расширены вертикально на 24 px: полные шляпы заходили над номинальной границей строки. Это исправление разреза, не дорисовка. Первоначальная неуспешная диагностика фиксированной сетки сохранена в `pipeline-meta.json`; окончательная проверка исправленных кадров — в `export-meta.json`.

Все 148 кадров прошли проверки наличия фигуры, отсутствия касания выходных границ и обрезания при размещении. Остаточный пурпурный цвет подавлен на двухпиксельной кромке без изменения геометрии альфа-канала. Ходьба, бег, прыжок, пробуждение и трюк со шляпой корректировались адресными генерациями. Число кадров само по себе не доказывает плавности.

Листы и воспроизведение проверены через HTML-просмотр, в том числе на светлом/тёмном фоне и при 160/220 px. В финальных ходьбе и беге выделены проходящие и широкие фазы; ракурс ближе к боковому, чем у ожидания. Покадровая генерация оставляет небольшую нестабильность деталей и не заменяет ручной анимационный риг. Эти циклы не следует считать эталоном биомеханически точного шага только на основании технической проверки.

Ожидаемые переходы: `sit-down → seated-idle`, `sleep → wake-up → idle`. Точное пиксельное совпадение между отдельными генерациями не гарантировано; небольшие различия масштаба и позы остаются возможны. Общий профиль обработки не является точным ригом. Проверить стыки в будущем движке, при необходимости применить короткий переход.

`export-assets.py` — детерминированный вспомогательный экспорт с проверками, не код питомца. Повторный экспорт: сначала обработать исходники через `generate2dsprite.py process`, затем запустить `export-assets.py --processor <путь к generate2dsprite.py>`. Не применять подавление кромки многократно к уже обработанным PNG. Промпты корректирующих генераций сохранены отдельно в `prompt-correction*.txt`.

Сборка, тесты приложения и интеграция не выполнялись — вне задачи.

The project already copies Resource PNGs recursively during build/publish. These files are not selected by the runtime, but may be copied with other resources. Project/build configuration was not changed.
