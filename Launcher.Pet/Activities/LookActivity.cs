using Launcher.Pet.Animation;
using Launcher.Pet.Behavior;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class LookActivity : PetActivity
{
    internal override PetMode Mode => PetMode.Looking;
    internal override bool UsesRoutine => true;
    internal override void Update(PetActivityContext c) => PetLook.Update(c.Body, c.Environment);
}
