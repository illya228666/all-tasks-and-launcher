using System.Drawing;
using Launcher.Pet.Data;
using Launcher.Pet.Sprites;

namespace Launcher.Pet.Behavior;
internal static class PetPlacement
{
    internal static Rectangle SpriteBounds(PetBody body, PetEnvironment environment, Point shake) =>
        PetSpriteLayout.GetBounds(LogicalPosition(body, environment), body.Appearance.GetFrameGeometry(body.Row, body.Frame), shake, environment.Scale, body.Appearance);

    internal static Point? VisibleHead(PetBody body, PetEnvironment environment, Rectangle bounds) =>
        PetSpriteLayout.VisibleHead(bounds, body.Appearance.GetFrameGeometry(body.Row, body.Frame), environment.AreaScreenPosition, environment.VisibleScreenBounds, body.Appearance);

    internal static void Fit(PetBody state, PetEnvironment environment)
    {
        float minimum = PetLogicalGeometry.EdgePadding;
        float maximum = Math.Max(minimum, environment.AreaWidth - PetLogicalGeometry.Width * environment.Scale - minimum);
        float x = float.IsNaN(state.X) ? (environment.AreaWidth - PetLogicalGeometry.Width * environment.Scale) / 2f : state.X;
        state.X = Math.Clamp(x, minimum, maximum);
    }

    internal static Point LogicalPosition(PetBody state, PetEnvironment environment)
    {
        return new((int)Math.Round(float.IsNaN(state.X) ? 0 : state.X), environment.PetZoneTopY + (int)Math.Round(PetLogicalGeometry.Height * (1 - environment.Scale) - state.JumpLift));
    }
}
