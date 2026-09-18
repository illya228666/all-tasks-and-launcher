using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetWalk
{
    internal const float PixelsPerCycle = 120f;
    internal const int MaxDelayMs = 20000;
    internal const int MinDelayMs = 10000;
    internal static bool Prepare(PetState state, PetEnvironment environment, Random random)
    {
        float min = PetLogicalGeometry.EdgePadding;
        float max = Math.Max(min, environment.AreaWidth - PetLogicalGeometry.Width - min);
        float distance = environment.WindowWidth / 8f;
        bool left = state.X - min >= distance, right = max - state.X >= distance;
        if (!left && !right)
            return false;
        bool moveRight = right && (!left || random.Next(2) == 0);
        float available = moveRight ? max - state.X : state.X - min;
        distance += (float)random.NextDouble() * (available - distance);
        state.WalkStartX = state.X;
        state.WalkTargetX = state.X + (moveRight ? distance : -distance);
        int cycleMs = PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.MoveRightRow].Sum();
        state.WalkDurationMs = Math.Max(1, (int)Math.Round(distance / PetWalk.PixelsPerCycle)) * cycleMs;
        return true;
    }

    internal static bool Update(PetState state, long nowMs)
    {
        long elapsed = nowMs - state.StartedAtMs;
        state.X = state.WalkStartX + (state.WalkTargetX - state.WalkStartX) * Math.Min(1f, (float)elapsed / state.WalkDurationMs);
        PetFrames.Loop(state, state.WalkTargetX >= state.WalkStartX ? PetAnimationCatalog.MoveRightRow : PetAnimationCatalog.MoveLeftRow, elapsed);
        return elapsed >= state.WalkDurationMs;
    }
}
