using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class WaveActivity : PetActivity
{
    internal const int LoopCount = 3;
    internal const int DelayMs = 9000;
    internal override PetMode Mode => PetMode.Waving;
    internal override bool UsesRoutine => true;
    internal override bool Calm => true;
    internal override bool PreserveSpeech => true;
    internal override bool PreserveJumpSchedule => true;
    internal override bool PreserveWalkSchedule => true;
    internal override void Update(PetActivityContext c)
    {
        long elapsed = c.Now - StartedAtMs;
        PetFrames.Loop(c.Body, PetAnimationCatalog.WaveRow, elapsed);
        if (elapsed >= PetAnimationCatalog.FrameDurationsByRow[PetAnimationCatalog.WaveRow].Sum() * WaveActivity.LoopCount)
            c.Actor.Change(new IdleActivity(), c.Now);
    }
}
