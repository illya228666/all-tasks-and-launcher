using System.Drawing;
using Launcher.Pet.Hat;

namespace Launcher.Pet.Data;
public sealed record PetEnvironment(Point AreaScreenPosition, int AreaWidth, int PetZoneTopY, int WindowWidth, Rectangle VisibleScreenBounds, bool CanTrackCursor, Point CursorScreenPosition, IReadOnlyList<Rectangle> ObstaclesLocal, IReadOnlyList<HatSurface> Surfaces, string? PickupSurfaceIdentity = null, float Scale = 1f, Launcher.Pet.Exploration.RuinScene? Ruins = null, float AwakeningSeconds = 0);
