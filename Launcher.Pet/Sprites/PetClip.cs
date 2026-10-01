using System.Drawing;

namespace Launcher.Pet.Sprites;

public sealed record PetClip
{
    public string Name { get; init; } = "";
    public int Row { get; init; }
    public bool Loop { get; init; }
    public int[] Durations { get; init; } = Array.Empty<int>();
    public string[] HatFrames { get; init; } = Array.Empty<string>();
    public string[] NoHatFrames { get; init; } = Array.Empty<string>();
    public PetFrameGeometry[] Geometry { get; init; } = Array.Empty<PetFrameGeometry>();
    public PetFrameGeometry[] NoHatGeometry { get; init; } = Array.Empty<PetFrameGeometry>();
    public Dictionary<string, int> Phases { get; init; } = new();
    public int FrameAt(long elapsedMs)
    {
        long total = Durations.Sum();
        long time = Loop ? Math.Max(0, elapsedMs) % total : Math.Min(Math.Max(0, elapsedMs), total - 1);
        for (int i = 0; i < Durations.Length; i++) { if (time < Durations[i]) return i; time -= Durations[i]; }
        return Durations.Length - 1;
    }
    public int FrameAtProgress(float progress) => Math.Clamp((int)(Math.Clamp(progress, 0, 1) * Durations.Length), 0, Durations.Length - 1);
}

public sealed record PetAppearanceDefinition
{
    public int Version { get; init; } = 1;
    public string Id { get; init; } = "sumrak";
    public Size CellSize { get; init; }
    public float StandingHeight { get; init; }
    public PetClip[] Actions { get; init; } = Array.Empty<PetClip>();
}

internal readonly record struct PetVisualPose(int Row, int Frame);
