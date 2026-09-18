using System.Drawing;
using Launcher.Pet.Hat;

namespace Launcher.Pet.Data;
public sealed record PetEnvironment(Point AreaScreenPosition, int AreaWidth, int PetZoneTopY, int WindowWidth, Rectangle VisibleScreenBounds, bool CursorInsideWindow, Point CursorScreenPosition, IReadOnlyList<Rectangle> ObstaclesLocal, IReadOnlyList<HatSurface> Surfaces);
