using System.Drawing;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetPlacement
{
    internal static void Fit(PetState state, PetEnvironment environment)
    {
        float minimum = PetLogicalGeometry.EdgePadding;
        float maximum = Math.Max(minimum, environment.AreaWidth - PetLogicalGeometry.Width - minimum);
        float x = float.IsNaN(state.X) ? (environment.AreaWidth - PetLogicalGeometry.Width) / 2f : state.X;
        state.X = Math.Clamp(x, minimum, maximum);
    }

    internal static Point LogicalPosition(PetState state, PetEnvironment environment)
    {
        return new((int)Math.Round(float.IsNaN(state.X) ? 0 : state.X), environment.PetZoneTopY - (int)Math.Round(state.JumpLift));
    }
}
