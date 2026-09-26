namespace Launcher.Pet.Data;
internal sealed class PetBody
{
    // Published pose, not a behavior selector. Only PetActor.Activity controls
    // execution; retaining the label keeps scene consumers source-compatible.
    internal PetMode Mode;
    internal float X = float.NaN;
    internal int Row, Frame, LookIndex;
    internal float JumpLift;
}
