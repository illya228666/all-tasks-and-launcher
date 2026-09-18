using Launcher.Pet.Data;

namespace Launcher.Pet.Animation;
internal static class PetFrames
{
    internal static void Loop(PetState state, int row, long elapsedMs)
    {
        int[] durations = PetAnimationCatalog.FrameDurationsByRow[row];
        long phase = Math.Max(0, elapsedMs) % durations.Sum();
        int frame = 0;
        while (phase >= durations[frame])
            phase -= durations[frame++];
        state.Row = row;
        state.Frame = frame;
    }
}
