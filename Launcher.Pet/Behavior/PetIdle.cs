using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Behavior;
internal static class PetIdle
{
    internal static void Update(PetState state, long nowMs) => PetFrames.Loop(state, PetAnimationCatalog.IdleRow, nowMs - state.StartedAtMs);
}
