using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetJump
{
    internal const int ObstacleProbeOffsetPixels = 2;
    internal const int MaxDelayMs = 17000;
    internal const int MinDelayMs = 10000;
    internal static void Prepare(PetState state, PetEnvironment environment)
    {
        var obstacles = environment.ObstaclesLocal;
        int bottom = obstacles.Count == 0 ? 0 : obstacles.Max(rect => rect.Top);
        var row = obstacles.Where(rect => rect.Top == bottom).ToArray();
        float probe = state.X + ObstacleProbeOffsetPixels;
        state.FailedJump = row.Length > 0 && probe >= row.Min(rect => rect.Left) && probe <= row.Max(rect => rect.Right);
    }

    internal static bool Update(PetState state, long nowMs)
    {
        long elapsed = nowMs - state.StartedAtMs;
        var frames = state.FailedJump ? PetAnimationCatalog.FailedJumpFrames : PetAnimationCatalog.SuccessfulJumpFrames;
        foreach (var frame in frames)
        {
            int duration = PetAnimationCatalog.FrameDurationsByRow[frame.Row][frame.Frame];
            if (elapsed < duration)
            {
                state.Row = frame.Row;
                state.Frame = frame.Frame;
                state.JumpLift = frame.Lift * PetAnimationCatalog.FrameHeight / (state.FailedJump ? 4f : 3f);
                return false;
            }

            elapsed -= duration;
        }

        return true;
    }
}
