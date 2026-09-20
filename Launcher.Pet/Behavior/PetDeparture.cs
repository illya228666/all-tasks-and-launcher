using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;

internal sealed class PetDeparture
{
    private readonly Point _start;
    internal PetDeparture(Point start) => _start = start;
    private float _elapsed;
    private float? _landedAt;
    internal bool HasLanded => _landedAt is not null;
    internal float Scale
    {
        get { float t = Math.Clamp(_elapsed / 0.65f, 0, 1); return 1 - 0.5f * t * t * (3 - 2 * t); }
    }
    private const float Gravity = 1500f;
    private const float LaunchSpeed = 300f;

    internal bool Update(PetState state, PetEnvironment environment, float elapsed)
    {
        _elapsed += Math.Clamp(elapsed, 0, 0.05f);
        float startX = _start.X - environment.AreaScreenPosition.X;
        float startY = _start.Y - environment.AreaScreenPosition.Y;
        float min = PetLogicalGeometry.EdgePadding;
        float max = Math.Max(min, environment.AreaWidth - PetLogicalGeometry.Width - min);
        float targetX = Math.Clamp(startX + Math.Sign(environment.AreaWidth / 2f - startX - PetLogicalGeometry.Width / 2f) * PetLogicalGeometry.Width, min, max);
        float floor = environment.PetZoneTopY;
        float distance = Math.Max(0, floor - startY);
        float flightDuration = (LaunchSpeed + MathF.Sqrt(LaunchSpeed * LaunchSpeed + 2 * Gravity * distance)) / Gravity;
        float flight = Math.Max(0, _elapsed - 0.14f);
        state.X = startX + (targetX - startX) * Math.Clamp(flight / flightDuration, 0, 1) + PetLogicalGeometry.Width * (1 - Scale) / 2;
        float y = startY - LaunchSpeed * flight + Gravity * flight * flight / 2;
        if (_landedAt is null && flight >= flightDuration)
            _landedAt = _elapsed;
        if (_landedAt is float landed)
        {
            state.X = targetX + PetLogicalGeometry.Width * (1 - Scale) / 2;
            state.JumpLift = 0;
            state.Row = PetAnimationCatalog.FailedRow;
            float recovery = _elapsed - landed;
            state.Frame = recovery < 0.44f ? 4 : recovery < 0.58f ? 3 : recovery < 0.72f ? 5 : recovery < 0.96f ? 6 : 7;
            return recovery >= 1.4f;
        }
        state.JumpLift = floor - y;
        state.Row = PetAnimationCatalog.JumpRow;
        state.Frame = _elapsed < 0.14f ? 0 : flight < LaunchSpeed / Gravity ? 1 : 3;
        return false;
    }
}
