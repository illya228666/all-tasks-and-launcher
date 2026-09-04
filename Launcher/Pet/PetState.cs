namespace Launcher.Pet;

internal enum PetMode
{
    Idle,
    Waving,
    Moving,
    Jumping,
    TrackingCursor
}

internal sealed class PetState
{
    internal PetMode Mode { get; set; } = PetMode.Idle;
    internal int Row { get; set; } = Animation.PetAnimationCatalog.IdleRow;
    internal int Frame { get; set; }
    internal int IdleElapsedMs { get; set; }
    internal int WaveLoopsRemaining { get; set; }
    internal float X { get; set; } = float.NaN;
    internal float MoveStartX { get; set; }
    internal float MoveTargetX { get; set; }
    internal int MoveElapsedMs { get; set; }
    internal int MoveDurationMs { get; set; }
    internal Animation.PetJumpFrame[]? JumpSequence { get; set; }
    internal int JumpIndex { get; set; }
    internal float JumpPeak { get; set; }
    internal bool JumpPending { get; set; }
    internal bool MovementPending { get; set; }
    internal int LookIndex { get; set; }
}
