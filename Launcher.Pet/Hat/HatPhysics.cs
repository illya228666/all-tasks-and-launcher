namespace Launcher.Pet.Hat;
internal sealed class HatPhysics
{
    // Скорость снижения в пикселях/с и сопротивление воздуха: лёгкая шляпа парит.
    private const float MaxSpeed = 115f;
    private const float AirResponse = 3f;
    private const float GlideLift = 0.35f;
    // Полуразмах бокового скольжения в пикселях; полный проход — 64 пикселя.
    private const float SwayDistance = 32f;
    private const float FlutterStrength = 0.12f;
    internal void Advance(HatState state, float elapsedSeconds)
    {
        // Ограничение не даёт паузе UI-потока превратиться в огромный скачок.
        float dt = Math.Clamp(elapsedSeconds, 0f, 0.05f);
        float previousPhase = state.FallTimeSeconds * HatRotationProfile.SwingRadiansPerSecond;
        state.FallTimeSeconds += dt;
        float phase = state.FallTimeSeconds * HatRotationProfile.SwingRadiansPerSecond;
        float swing = MathF.Sin(phase);
        // Разность косинусов даёт плавный старт и разворот без накопления дрейфа.
        // Третья гармоника добавляет мелкое трепетание к широкому скольжению.
        float dx = SwayDistance * (MathF.Cos(previousPhase) - MathF.Cos(phase) + FlutterStrength / 3f * (MathF.Cos(3f * previousPhase) - MathF.Cos(3f * phase)));

        // Наклон колеблется быстрее бокового скольжения, поэтому даже короткое падение
        // не выглядит как постоянный завал в одну сторону.
        float tiltPhase = state.FallTimeSeconds * HatRotationProfile.TiltRadiansPerSecond;
        float tilt = (MathF.Sin(tiltPhase) + FlutterStrength * MathF.Sin(3f * tiltPhase)) / (1f + FlutterStrength);
        state.Angle = HatRotationProfile.MaxAngleDegrees * tilt;

        // При боковом скольжении воздух поддерживает шляпу; на развороте она проседает.
        // Экспоненциальное сопротивление плавно выводит скорость на предел без рывка.
        float targetSpeed = MaxSpeed * (1f - GlideLift * swing * swing);
        float previousSpeed = state.VelocityY;
        float response = 1f - MathF.Exp(-AirResponse * dt);
        state.VelocityY += (targetSpeed - state.VelocityY) * response;
        float dy = targetSpeed * dt + (previousSpeed - targetSpeed) * response / AirResponse;
        state.Position = new(state.Position.X + dx, state.Position.Y + dy);
    }
}
