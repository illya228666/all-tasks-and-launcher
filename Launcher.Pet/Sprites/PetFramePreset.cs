namespace Launcher.Pet.Sprites;

// Общая визуальная форма кадра. Preset задаёт базовые render-настройки,
// а конкретный кадр может переопределить только отличающиеся значения.
internal enum PetFramePreset
{
    Standing,
    Running,
    Waving,
    Jumping,
    Failed,
    Action,
    Looking,
    LookingUp,
    LookingDown
}
