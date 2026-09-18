using System.Drawing;
using Launcher.Pet.Hat;

namespace Launcher.Pet.Data;
public sealed record PetScene(PetMode Mode, int Row, int Frame, Rectangle LocalBounds, Point? HeadScreenPosition, Point WindowShake, bool HatAttached, HatScene Hat, string? Speech, int VisibleLetters);
