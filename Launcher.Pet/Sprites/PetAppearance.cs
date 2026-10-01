using System.Drawing;
using Launcher.Pet.Animation;

namespace Launcher.Pet.Sprites;

/// <summary>
/// Sprite dimensions and timing for one selectable pet. Behavior rows keep their
/// existing meanings; an unauthored pose resolves to idle in this one place.
/// Add future pets to Available without adding branches to drawing or activities.
/// </summary>
public sealed class PetAppearance
{
    public static PetAppearance Original { get; } = new("sumrak", "Sumrak", new(149, 200), 1f, 8);
    public static PetAppearance Chibi { get; } = new("sumrak-chibi-v2", "Sumrak · Chibi", new(362, 362), 200f / 248f, 16,
        new[] { Repeat(12, 140), Repeat(16, 80), Repeat(16, 80), Repeat(12, 100), Repeat(16, 80),
            PetAnimationCatalog.FrameDurationsByRow[5], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(),
            Repeat(8, 140), Repeat(8, 140), Repeat(4, 180), Repeat(8, 180) });
    public static IReadOnlyList<PetAppearance> Available { get; } = Array.AsReadOnly(new[] { Original, Chibi });
    public string Id { get; }
    public string Name { get; }
    public Size CellSize { get; }
    public float RenderScale { get; }
    public int Columns { get; }
    private readonly int[][]? _durations;
    private readonly PetFrameGeometry[][]? _geometry;
    private IReadOnlyDictionary<string, PetClip> _clips = new Dictionary<string, PetClip>();
    public IReadOnlyDictionary<string, PetClip> Clips => _clips;
    public bool UsesClipFiles => _clips.Count > 0;
    public PetClip? Clip(string name) => _clips.GetValueOrDefault(name);
    public PetClip? Clip(int row) => _clips.Values.FirstOrDefault(clip => clip.Row == row);
    public static PetAppearance FromDefinition(PetAppearanceDefinition definition)
    {
        if (definition is null || definition.Actions is null || definition.Actions.Any(a => a is null) || definition.Version != 1 || definition.Id != "sumrak" || definition.CellSize.Width is < 64 or > 2048
            || definition.CellSize.Height is < 64 or > 2048 || !float.IsFinite(definition.StandingHeight)
            || definition.StandingHeight <= 0 || definition.StandingHeight > definition.CellSize.Height
            || definition.Actions.Length is < 1 or > 64 || definition.Actions.Select(a => a.Name).Distinct().Count() != definition.Actions.Length
            || definition.Actions.Select(a => a.Row).Distinct().Count() != definition.Actions.Length
            || definition.Actions.Any(a => string.IsNullOrEmpty(a.Name) || a.Durations is null || a.HatFrames is null || a.NoHatFrames is null || a.Geometry is null || a.NoHatGeometry is null
                || a.Row is < 0 or > 63 || a.Durations.Length is < 1 or > 64
                || a.Durations.Any(d => d is < 1 or > 10000) || a.HatFrames.Length != a.Durations.Length
                || a.NoHatFrames.Length != a.Durations.Length || a.Geometry.Length != a.Durations.Length
                || a.NoHatGeometry.Length != 0 && a.NoHatGeometry.Length != a.Durations.Length
                || a.Phases is null || a.Phases.Values.Any(p => p < 0 || p >= a.Durations.Length)
                || a.Geometry.Concat(a.NoHatGeometry).Any(g => g.BodyAnchorX < 0 || g.BodyAnchorX >= definition.CellSize.Width
                    || g.GroundAnchorY < 0 || g.GroundAnchorY >= definition.CellSize.Height || g.HeadBounds.Width <= 0 || g.HeadBounds.Height <= 0))
            || !definition.Actions.Any(a => a.Name == "idle" && a.Row == 0))
            throw new InvalidDataException("Invalid character animation bundle.");
        string[] required = { "idle", "idle-curious", "wave", "look-upper", "look-lower", "walk-right", "walk-left", "jump", "failed",
            "grab", "climb", "pull-up", "drag", "fall", "recovery", "hat-pickup", "observe", "pickup", "carry-right", "carry-left", "carry-climb", "place", "plant", "recycle" };
        if (required.Any(name => !definition.Actions.Any(a => a.Name == name))
            || definition.Actions.Any(a => !a.Phases.ContainsKey("commit") || a.Geometry.Concat(a.NoHatGeometry).Any(g => g.ItemAnchor is null || g.HatAnchor is null)))
            throw new InvalidDataException("Character bundle lacks a required clip, phase or landmark.");
        int count = definition.Actions.Max(a => a.Row) + 1;
        var durations = Enumerable.Range(0, count).Select(_ => Array.Empty<int>()).ToArray();
        var geometry = Enumerable.Range(0, count).Select(_ => new[] { definition.Actions.First(a => a.Row == 0).Geometry[0] }).ToArray();
        foreach (var action in definition.Actions) { durations[action.Row] = action.Durations; geometry[action.Row] = action.Geometry; }
        return new(definition.Id, "Sumrak", definition.CellSize, 200 / definition.StandingHeight, definition.Actions.Max(a => a.Durations.Length), durations, geometry)
        { _clips = definition.Actions.ToDictionary(a => a.Name) };
    }
    private static readonly float[] JumpLift = { 0, 0, 0, .25f, .5f, .75f, 1, 1, .75f, .5f, .25f, 0, 0, 0, 0, 0 };
    private static readonly PetFrameGeometry ChibiDefault = new(181, 335, new(181, 160), new(95, 82, 172, 140));

    private PetAppearance(string id, string name, Size cell, float renderScale, int columns,
        int[][]? durations = null, PetFrameGeometry[][]? geometry = null)
        => (Id, Name, CellSize, RenderScale, Columns, _durations, _geometry) = (id, name, cell, renderScale, columns, durations, geometry);

    public static PetAppearance Find(string? id) => Available.FirstOrDefault(pet => pet.Id == id) ?? Original;
    public bool UsesSeparateFrames => _durations is not null;
    public int AuthoredRows => _durations?.Length ?? PetSpriteCatalog.AtlasRows;
    public int RequiredRenderAreaHeight => UsesSeparateFrames
        ? PetLogicalGeometry.Height + (int)Math.Ceiling((CellSize.Height - GetFrameGeometry(0, 0).GroundAnchorY) * RenderScale)
        : PetSpriteCatalog.RequiredRenderAreaHeight;
    public int GetFrameCount(int row) => UsesSeparateFrames
        ? (IsAuthored(row) ? _durations![row].Length : 1) : PetSpriteCatalog.GetFrameCount(row);
    public Rectangle GetSourceRectangle(int row, int frame)
    {
        if (UsesSeparateFrames && !IsAuthored(row)) (row, frame) = (0, 0);
        frame = Math.Clamp(frame, 0, GetFrameCount(row) - 1);
        return new(frame * CellSize.Width, row * CellSize.Height, CellSize.Width, CellSize.Height);
    }
    public PetFrameGeometry GetFrameGeometry(int row, int frame, bool hat = true)
    {
        if (!hat && Clip(row) is { NoHatGeometry.Length: > 0 } clip)
            return clip.NoHatGeometry[Math.Clamp(frame, 0, clip.NoHatGeometry.Length - 1)];
        if (!UsesSeparateFrames) return PetSpriteCatalog.GetFrameGeometry(row, Math.Clamp(frame, 0, GetFrameCount(row) - 1));
        if (!IsAuthored(row)) (row, frame) = (0, 0);
        return _geometry is null ? ChibiDefault : _geometry[row][Math.Clamp(frame, 0, GetFrameCount(row) - 1)];
    }
    /// <summary>Attach pixel-derived head bounds without changing timing or the shared root.</summary>
    public PetAppearance WithGeometry(PetFrameGeometry[][] geometry)
    {
        if (!UsesSeparateFrames || geometry.Length != AuthoredRows || geometry.Where((frames, row) => frames.Length != GetFrameCount(row)).Any())
            throw new ArgumentException("Sprite geometry does not match the selected pet.", nameof(geometry));
        return new(Id, Name, CellSize, RenderScale, Columns, _durations, geometry) { _clips = _clips };
    }
    internal int[] GetFrameDurations(int row) => UsesSeparateFrames && IsAuthored(row)
        ? _durations![row] : PetAnimationCatalog.FrameDurationsByRow[row];
    internal IEnumerable<PetJumpFrame> GetJumpFrames(bool failed)
    {
        if (!UsesSeparateFrames) return failed ? PetAnimationCatalog.FailedJumpFrames : PetAnimationCatalog.SuccessfulJumpFrames;
        if (UsesClipFiles)
        {
            var jumping = Clip("jump")!;
            var jumpingFrames = Enumerable.Range(0,jumping.Durations.Length).Select(i => new PetJumpFrame(jumping.Row,i,JumpLift[Math.Min(JumpLift.Length-1,i)]));
            var falling = Clip("failed")!;
            return failed ? jumpingFrames.Concat(Enumerable.Range(0,falling.Durations.Length).Select(i => new PetJumpFrame(falling.Row,i,0))) : jumpingFrames;
        }
        var jump = JumpLift.Select((lift, frame) => new PetJumpFrame(PetAnimationCatalog.JumpRow, frame, lift));
        return failed ? jump.Concat(PetAnimationCatalog.FailedJumpFrames.Where(frame => frame.Row != PetAnimationCatalog.JumpRow)) : jump;
    }
    private bool IsAuthored(int row) => row >= 0 && row < AuthoredRows && _durations![row].Length > 0;
    private static int[] Repeat(int count, int duration) => Enumerable.Repeat(duration, count).ToArray();
}
