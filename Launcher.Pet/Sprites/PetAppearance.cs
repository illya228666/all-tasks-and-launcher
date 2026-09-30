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
    private static readonly float[] JumpLift = { 0, 0, 0, .25f, .5f, .75f, 1, 1, .75f, .5f, .25f, 0, 0, 0, 0, 0 };
    private static readonly PetFrameGeometry ChibiDefault = new(181, 335, new(181, 160), new(95, 82, 172, 140));

    private PetAppearance(string id, string name, Size cell, float renderScale, int columns,
        int[][]? durations = null, PetFrameGeometry[][]? geometry = null)
        => (Id, Name, CellSize, RenderScale, Columns, _durations, _geometry) = (id, name, cell, renderScale, columns, durations, geometry);

    public static PetAppearance Find(string? id) => Available.FirstOrDefault(pet => pet.Id == id) ?? Original;
    public bool UsesSeparateFrames => _durations is not null;
    public int AuthoredRows => _durations?.Length ?? PetSpriteCatalog.AtlasRows;
    public int RequiredRenderAreaHeight => UsesSeparateFrames
        ? PetLogicalGeometry.Height + (int)Math.Ceiling((CellSize.Height - ChibiDefault.GroundAnchorY) * RenderScale)
        : PetSpriteCatalog.RequiredRenderAreaHeight;
    public int GetFrameCount(int row) => UsesSeparateFrames
        ? (IsAuthored(row) ? _durations![row].Length : 1) : PetSpriteCatalog.GetFrameCount(row);
    public Rectangle GetSourceRectangle(int row, int frame)
    {
        if (UsesSeparateFrames && !IsAuthored(row)) (row, frame) = (0, 0);
        frame = Math.Clamp(frame, 0, GetFrameCount(row) - 1);
        return new(frame * CellSize.Width, row * CellSize.Height, CellSize.Width, CellSize.Height);
    }
    public PetFrameGeometry GetFrameGeometry(int row, int frame)
    {
        if (!UsesSeparateFrames) return PetSpriteCatalog.GetFrameGeometry(row, Math.Clamp(frame, 0, GetFrameCount(row) - 1));
        if (!IsAuthored(row)) (row, frame) = (0, 0);
        return _geometry is null ? ChibiDefault : _geometry[row][Math.Clamp(frame, 0, GetFrameCount(row) - 1)];
    }
    /// <summary>Attach pixel-derived head bounds without changing timing or the shared root.</summary>
    public PetAppearance WithGeometry(PetFrameGeometry[][] geometry)
    {
        if (!UsesSeparateFrames || geometry.Length != AuthoredRows || geometry.Where((frames, row) => frames.Length != GetFrameCount(row)).Any())
            throw new ArgumentException("Sprite geometry does not match the selected pet.", nameof(geometry));
        return new(Id, Name, CellSize, RenderScale, Columns, _durations, geometry);
    }
    internal int[] GetFrameDurations(int row) => UsesSeparateFrames && IsAuthored(row)
        ? _durations![row] : PetAnimationCatalog.FrameDurationsByRow[row];
    internal IEnumerable<PetJumpFrame> GetJumpFrames(bool failed)
    {
        if (!UsesSeparateFrames) return failed ? PetAnimationCatalog.FailedJumpFrames : PetAnimationCatalog.SuccessfulJumpFrames;
        var jump = JumpLift.Select((lift, frame) => new PetJumpFrame(PetAnimationCatalog.JumpRow, frame, lift));
        return failed ? jump.Concat(PetAnimationCatalog.FailedJumpFrames.Where(frame => frame.Row != PetAnimationCatalog.JumpRow)) : jump;
    }
    private bool IsAuthored(int row) => row >= 0 && row < AuthoredRows && _durations![row].Length > 0;
    private static int[] Repeat(int count, int duration) => Enumerable.Repeat(duration, count).ToArray();
}
