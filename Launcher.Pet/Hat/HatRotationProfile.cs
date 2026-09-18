namespace Launcher.Pet.Hat;
public static class HatRotationProfile
{
    public const float MaxAngleDegrees = 7f;
    // 17 заранее отрисованных поз вместо 5: шаг 0,875°, включая нейтральный кадр.
    public const int FrameCount = 17;
    // Медленное боковое скольжение и более быстрое левое/правое покачивание разделены:
    // шляпа успевает заметно наклониться в обе стороны даже за сравнительно короткое падение.
    public const float SwingRadiansPerSecond = 2.5f;
    public const float TiltRadiansPerSecond = 5f;
    public static float GetFrameAngle(int frameIndex)
    {
        if ((uint)frameIndex >= FrameCount)
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        float step = (MaxAngleDegrees * 2f) / (FrameCount - 1);
        return -MaxAngleDegrees + (frameIndex * step);
    }

    public static int GetNearestFrameIndex(float angle)
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
