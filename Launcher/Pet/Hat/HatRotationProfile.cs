namespace Launcher.Pet.Hat;

internal static class HatRotationProfile
{
    internal const float MaxAngleDegrees = 7f;
    // 17 заранее отрисованных поз вместо 5: шаг 0,875°, включая нейтральный кадр.
    internal const int FrameCount = 17;
    // Полный цикл скольжения занимает примерно 2,5 секунды.
    internal const float SwingRadiansPerSecond = 2.5f;

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
