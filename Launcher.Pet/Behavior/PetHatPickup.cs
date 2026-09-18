using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;

namespace Launcher.Pet.Behavior;
internal static class PetHatPickup
{
    internal const float RunSpeedPixelsPerSecond = 220f;
    internal const int RunFrameMs = 80;
    internal static float TargetX(Point screenPoint, PetEnvironment environment) => Math.Clamp(screenPoint.X - environment.AreaScreenPosition.X - PetLogicalGeometry.Width / 2f, PetLogicalGeometry.EdgePadding, Math.Max(PetLogicalGeometry.EdgePadding, environment.AreaWidth - PetLogicalGeometry.Width - PetLogicalGeometry.EdgePadding));
    internal static bool Walk(PetState state, PetEnvironment environment, Point target, long nowMs, float elapsedSeconds)
    {
        float distance = TargetX(target, environment) - state.X;
        float step = PetHatPickup.RunSpeedPixelsPerSecond * Math.Clamp(elapsedSeconds, 0, 0.05f);
        state.X += Math.Clamp(distance, -step, step);
        state.Row = distance >= 0 ? PetAnimationCatalog.MoveRightRow : PetAnimationCatalog.MoveLeftRow;
        state.Frame = (int)((nowMs - state.StartedAtMs) / PetHatPickup.RunFrameMs % PetAnimationCatalog.FrameDurationsByRow[state.Row].Length);
        return Math.Abs(distance) <= step;
    }

    internal static bool PutOn(PetState state, HatWorld hat, long nowMs)
    {
        long elapsed = nowMs - state.StartedAtMs;
        foreach (var frame in PetAnimationCatalog.HatPickupFrames)
        {
            if (frame.PutOn)
                hat.Attach();
            if (elapsed < frame.DurationMs)
            {
                state.Row = frame.Row;
                state.Frame = frame.Frame;
                return false;
            }

            elapsed -= frame.DurationMs;
        }

        return true;
    }
}
