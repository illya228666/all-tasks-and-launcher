namespace Launcher.Pet.Hat;

internal static class HatTiming
{
    // Единый runtime tick для состояния шляпы: drag, fall и resting.
    internal const int RuntimeTickIntervalMs = 16;

    // Explorer icon geometry читается через COM/cross-process API и поэтому
    // имеет собственную freshness policy, независимую от runtime tick.
    internal const int DesktopIconSnapshotLifetimeMs = 500;
}
