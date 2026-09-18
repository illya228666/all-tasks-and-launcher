using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetEarthquake
{
    internal const float ShakePixels = 8f;
    internal const int DurationMs = 5000;
    internal static Point Update(PetState state, long nowMs)
    {
        long elapsed = Math.Clamp(nowMs - state.StartedAtMs, 0, PetEarthquake.DurationMs - 1);
        var frame = PetAnimationCatalog.EarthquakeFrames.First(item => elapsed < item.UntilMs);
        state.Row = frame.Row;
        state.Frame = frame.Frame;
        float seconds = elapsed / 1000f;
        float envelope = Math.Min(1f, Math.Min(elapsed / 200f, (PetEarthquake.DurationMs - elapsed) / 900f));
        float amplitude = PetEarthquake.ShakePixels * envelope;
        return new((int)Math.Round(amplitude * (MathF.Sin(seconds * 47f) + 0.35f * MathF.Sin(seconds * 83f))), (int)Math.Round(amplitude * 0.55f * MathF.Sin(seconds * 61f)));
    }
}
