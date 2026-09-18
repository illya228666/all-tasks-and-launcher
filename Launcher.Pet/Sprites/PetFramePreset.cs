namespace Launcher.Pet.Sprites;

// Preset описывает визуальную позу, а не конкретную анимацию.
// Например idle, blink и wave могут использовать один Standing.
internal enum PetFramePreset
{
    Standing,
    Running,
    Crouched,
    Airborne,
    Leaning,
    Staggering,
    Fallen,
    Action,
    Looking,
    LookingUp,
    LookingDown
}
