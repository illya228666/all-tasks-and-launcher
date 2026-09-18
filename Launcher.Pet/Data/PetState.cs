namespace Launcher.Pet.Data;
internal sealed class PetState
{
    internal PetMode Mode;
    internal float X = float.NaN;
    internal int Row, Frame, LookIndex;
    internal long StartedAtMs;
    internal float JumpLift, WalkStartX, WalkTargetX;
    internal int WalkDurationMs;
    internal bool FailedJump;
}
