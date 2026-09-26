namespace Launcher.Pet.Data;
// Compatibility labels for the published pose. Activities never dispatch on this
// enum; a new mechanic can reuse a label without extending a global state machine.
public enum PetMode
{
    Idle,
    Waving,
    Walking,
    Jumping,
    Looking,
    RetrievingHat,
    PuttingOnHat,
    Climbing,
    Grabbing,
    PullingUp,
    Falling,
    Traversing,
    Earthquake,
    Dragging,
    Recovering
}
