namespace Launcher.Pet.Hat;

internal sealed class HatPhysics
{
    private const float Gravity = 1050f;
    private const float MaxSpeed = 1000f;

    internal void Advance(HatState state, float elapsedSeconds)
    {
        // Ограничение не даёт паузе UI-потока превратиться в огромный скачок.
        float dt = Math.Clamp(elapsedSeconds, 0f, 0.05f);
        state.VelocityY = Math.Min(MaxSpeed, state.VelocityY + Gravity * dt);
        state.Position = new(state.Position.X, state.Position.Y + state.VelocityY * dt);
        state.FallTimeSeconds += dt;
        state.Angle = HatRotationProfile.MaxAngleDegrees
            * MathF.Sin(state.FallTimeSeconds * HatRotationProfile.SwingRadiansPerSecond);
    }
}
