using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetLook
{
    internal const int DeadzoneRadiusPixels = 12;
    internal static void Update(PetState state, PetEnvironment environment)
    {
        int x = environment.CursorScreenPosition.X - environment.AreaScreenPosition.X - (int)Math.Round(state.X) - PetAnimationCatalog.FrameWidth / 2;
        int y = environment.CursorScreenPosition.Y - environment.AreaScreenPosition.Y - environment.GroundLocalY - PetAnimationCatalog.FrameHeight / 2;
        if ((long)x * x + (long)y * y > PetLook.DeadzoneRadiusPixels * PetLook.DeadzoneRadiusPixels)
            state.LookIndex = GetLookIndex(x, y);
        state.Row = PetAnimationCatalog.LookFirstRow + state.LookIndex / 8;
        state.Frame = state.LookIndex % 8;
    }

    internal static int GetLookIndex(float deltaX, float deltaY)
    {
        double degrees = Math.Atan2(deltaX, -deltaY) * 180d / Math.PI;
        if (degrees < 0)
            degrees += 360d;
        return (int)Math.Round(degrees / 22.5d, MidpointRounding.AwayFromZero) % 16;
    }
}
