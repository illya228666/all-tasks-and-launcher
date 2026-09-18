using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetPlacement
{
    internal static void Fit(PetState state, PetEnvironment environment)
    {
        float minimum = PetAnimationCatalog.EdgePadding;
        float maximum = Math.Max(minimum, environment.AreaWidth - PetAnimationCatalog.FrameWidth - minimum);
        float x = float.IsNaN(state.X) ? (environment.AreaWidth - PetAnimationCatalog.FrameWidth) / 2f : state.X;
        state.X = Math.Clamp(x, minimum, maximum);
    }

    internal static Rectangle LocalBounds(PetState state, PetEnvironment environment, Point shake)
    {
        Point offset = PetAnimationCatalog.GetFrameOffset(state.Row, state.Frame);
        return new((int)Math.Round(float.IsNaN(state.X) ? 0 : state.X) + offset.X - shake.X / 2, environment.GroundLocalY + offset.Y - (int)Math.Round(state.JumpLift) + Math.Min(0, shake.Y / 2), PetAnimationCatalog.CellWidth, PetAnimationCatalog.CellHeight);
    }

    internal static Point? VisibleHead(PetState state, PetEnvironment environment, Point shake)
    {
        Rectangle bounds = LocalBounds(state, environment, shake);
        var head = new Point(environment.AreaScreenPosition.X + bounds.X + PetAnimationCatalog.BodyAnchorXByRow[state.Row][state.Frame], environment.AreaScreenPosition.Y + bounds.Y + PetAnimationCatalog.HeadHitHeight / 2);
        return environment.VisibleScreenBounds.Contains(head) ? head : null;
    }
}
