# Понятные имена / Verständliche Namen

| Имя / Name | RU | DE |
| --- | --- | --- |
| Startup | Создаёт части приложения | Erstellt Anwendungsteile |
| Session | Владеет временем жизни ресурсов | Besitzt die Lebensdauer von Ressourcen |
| Connection | Связывает два самостоятельных участника | Verbindet zwei selbständige Teile |
| Source | Читает исходный список | Liest eine Ausgangsliste |
| AppList | Хранит программы и пользовательский выбор | Hält Programme und Benutzerauswahl |
| AppLaunch | Правила запуска и учёт результата | Startregeln und Ergebniszählung |
| AppProcess | Вызов процесса Windows | Windows-Prozessstart |
| SettingsFile | Чтение и безопасная запись настроек | Lesen und sicheres Schreiben |
| PetWorld | Полное логическое состояние питомца | Gesamter logischer Begleiterzustand |
| PetBehavior | Выбор следующего действия | Auswahl der nächsten Aktion |
| PetEnvironment | Снимок окружения на один шаг | Umgebungsaufnahme für einen Schritt |
| PetScene | Готовые данные для отображения | Fertige Anzeigedaten |
| Drawing | Рисует, но не решает поведение | Zeichnet, entscheidet aber kein Verhalten |
| Local | Координаты внутри области питомца | Koordinaten im Begleiterbereich |
| Screen | Координаты рабочего стола | Bildschirmkoordinaten |
| Surface | Геометрия опоры шляпы | Geometrie einer Hutauflage |
| Protocol | Смысл команд и ответов платы | Bedeutung von Befehlen und Antworten |
| Exchange | Ограниченный по времени обмен строками | Zeitlich begrenzter Zeilenaustausch |

RU: Миллисекунды обозначаются Ms, секунды — Seconds, пиксели — Pixels. Простой предметный смысл важнее шаблонных терминов. Один класс не обязан иметь интерфейс; интерфейс нужен для реальной границы зависимости.
DE: Ms bezeichnet Millisekunden, Seconds Sekunden und Pixels Bildpunkte. Konkrete Bedeutung geht vor Mustervokabular. Ein Interface wird für eine tatsächliche Abhängigkeitsgrenze verwendet, nicht automatisch für jede Klasse.
