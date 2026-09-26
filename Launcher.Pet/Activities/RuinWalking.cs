using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Activities;

internal static class RuinWalking
{
    internal static bool Step(PetActivityContext c, float target)
    {
        float delta = target - (c.Body.X + c.HalfWidth);
        c.Body.X += Math.Clamp(delta, -RuinMotion.WalkSpeed * c.Step, RuinMotion.WalkSpeed * c.Step);
        c.Actor.Present(PetMode.Walking);
        c.Body.Row = delta >= 0 ? PetAnimationCatalog.MoveRightRow : PetAnimationCatalog.MoveLeftRow;
        c.Body.Frame = (int)(c.Now / 120 % 8);
        return Math.Abs(delta) <= RuinMotion.WalkSpeed * c.Step;
    }
}
