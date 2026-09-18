using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetWave
{
    internal const int LoopCount = 3;
    internal const int DelayMs = 9000;
    internal static bool Update(PetState state, long nowMs)
    {
        long elapsed = nowMs - state.StartedAtMs;
        PetFrames.Loop(state, PetAnimationCatalog.WaveRow, elapsed);
        return elapsed >= PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.WaveRow].Sum() * PetWave.LoopCount;
    }
}
