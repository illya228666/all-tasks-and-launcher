namespace Launcher.Pet.Hat;

internal static class HatRotationProfile
{
    internal const float MaxAngleDegrees = 7f;
    internal const int FrameCount = 5;
    internal const float SwingRadiansPerSecond = 5f;

    internal static float GetFrameAngle(int frameIndex)
    {
        if ((uint)frameIndex >= FrameCount)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));

        float step = (MaxAngleDegrees * 2f) / (FrameCount - 1);
        return -MaxAngleDegrees + (frameIndex * step);
    }

    internal static int GetNearestFrameIndex(float angle)
    {
        int nearestFrame = 0;
        float smallestDistance = Math.Abs(GetFrameAngle(0) - angle);

        for (int frame = 1; frame < FrameCount; frame++)
        {
            float distance = Math.Abs(GetFrameAngle(frame) - angle);
            // Строгое сравнение сохраняет прежнее поведение MinBy при равенстве:
            // выбирается первый (меньший) индекс.
            if (distance >= smallestDistance)
                continue;

            smallestDistance = distance;
            nearestFrame = frame;
        }

        return nearestFrame;
    }
}
