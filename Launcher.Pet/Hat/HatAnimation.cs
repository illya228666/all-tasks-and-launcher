namespace Launcher.Pet.Hat;
public static class HatAnimation
{
    public const int FallingFrameCount = 7;
    private const float PoseLegSeconds = 1.8f;
    public static HatVisualPose Evaluate(HatMode mode, float angle, float fallSeconds, float settlementProgress)
    {
        if (mode is not (HatMode.Falling or HatMode.Settling)) return new(0, angle, 0);
        float phase = Math.Max(0, fallSeconds) % (2 * PoseLegSeconds) / PoseLegSeconds;
        float progress = phase <= 1 ? phase : 2 - phase;
        int frame = (int)MathF.Round(Smooth(progress) * FallingFrameCount);
        return new(frame, angle, mode == HatMode.Settling ? Smooth(Math.Clamp(settlementProgress, 0, 1)) : 0);
    }
    private static float Smooth(float value) => value * value * (3 - 2 * value);
}
