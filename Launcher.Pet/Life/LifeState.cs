using System.Drawing;
using Launcher.Pet.Exploration;

namespace Launcher.Pet.Life;

public enum MossStage { Germinating, Growing, Mature, Exhausted }
public enum LifeItemKind { Spore, Remains }
public enum LifeHolder { World, Pet, User }
public enum LifeEventKind { Released, Established, Failed, Died, Decomposed }
public enum LifeTaskKind { Observe, Carry, Plant, Recycle }

// All quantities are integer milligrams. Every transition transfers matter.
public sealed class LifeProfile
{
    public int GerminationMin { get; set; } = 30;
    public int GerminationMax { get; set; } = 60;
    public int MaturityMin { get; set; } = 60;
    public int MaturityMax { get; set; } = 180;
    public int ReleaseMin { get; set; } = 60;
    public int ReleaseMax { get; set; } = 90;
    public int LifespanMin { get; set; } = 300;
    public int LifespanMax { get; set; } = 480;
    public int DecompositionMin { get; set; } = 60;
    public int DecompositionMax { get; set; } = 120;
    public int RenewalDelay { get; set; } = 120;
    public int GrowthPerSecond { get; set; } = 30;
    public int MaintenancePerSecond { get; set; } = 5;
    public int MaxBiomass { get; set; } = 6000;
    public int MaxItems { get; set; } = 96;
}

public sealed class MossColony
{
    public long Id { get; set; }
    public int Mass { get; set; }
    public int Age { get; set; }
    public int Germination { get; set; }
    public int Maturity { get; set; }
    public int Lifespan { get; set; }
    public int NextRelease { get; set; }
    public long Transfer { get; set; }
    public MossStage Stage { get; set; }
}

public sealed class LifeSite
{
    public string Id { get; set; } = "";
    public string Platform { get; set; } = "ruin:floor";
    public float Fraction { get; set; }
    public int Nutrients { get; set; }
    public MossColony? Colony { get; set; }
}

public sealed class LifeItem
{
    public long Id { get; set; }
    public string Site { get; set; } = "";
    public LifeItemKind Kind { get; set; }
    public LifeHolder Holder { get; set; }
    public int Mass { get; set; }
    public int Age { get; set; }
    public int Duration { get; set; }
    public int OriginalMass { get; set; }
    public long Transfer { get; set; }
    public long Death { get; set; }
    public bool Accelerated { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class LifeEvent
{
    public long Id { get; set; }
    public LifeEventKind Kind { get; set; }
    public string Site { get; set; } = "";
    public long AtSecond { get; set; }
    public long Related { get; set; }
    public bool Observed { get; set; }
}

public sealed class LifeLearning
{
    public int Releases { get; set; }
    public int TransferOutcomes { get; set; }
    public List<long> Deaths { get; set; } = new();
    public List<long> Decompositions { get; set; } = new();
    public bool Carry => Releases >= 2;
    public bool Plant => Carry && TransferOutcomes >= 3;
    public bool Recycle => Plant && Deaths.Intersect(Decompositions).Count() >= 2;
}

public sealed class LifeIntent
{
    public LifeTaskKind Kind { get; set; }
    public string Site { get; set; } = "";
    public string Destination { get; set; } = "";
    public long Item { get; set; }
    public long Event { get; set; }
    public bool PickedUp { get; set; }
    public float ActionSeconds { get; set; }
}

public sealed class LifeState
{
    public int Version { get; set; } = 1;
    public LifeProfile Profile { get; set; } = new();
    public ulong EcologyRandom { get; set; }
    public ulong BehaviorRandom { get; set; }
    public long Seconds { get; set; }
    public double Remainder { get; set; }
    public long NextId { get; set; } = 1;
    public int Reserve { get; set; } = 1000;
    public int QuietSeconds { get; set; }
    public int Generation { get; set; } = 1;
    public long TotalMatter { get; set; }
    public List<LifeSite> Sites { get; set; } = new();
    public List<LifeItem> Items { get; set; } = new();
    public List<LifeEvent> Events { get; set; } = new();
    public LifeLearning Learning { get; set; } = new();
    public LifeIntent? Intent { get; set; }
    public List<string> RecentSites { get; set; } = new();
    public long RestUntil { get; set; }
}

public sealed class PetLifeSave
{
    public int Version { get; set; } = 1;
    public RuinScene Geometry { get; set; } = null!;
    public LifeState Life { get; set; } = null!;
    public string? Monitor { get; set; }
    public float PetXFraction { get; set; }
    public string PetPlatform { get; set; } = "ruin:floor";
}

public sealed record LifeSiteView(string Id, PointF Position, int Nutrients, int Biomass, MossStage? Stage);
public sealed record LifeItemView(long Id, LifeItemKind Kind, PointF Position, LifeHolder Holder, int Mass);
public sealed record LifeSignalView(LifeEventKind Kind, PointF Position, long Age);
public sealed record LifeScene(IReadOnlyList<LifeSiteView> Sites, IReadOnlyList<LifeItemView> Items, long Seconds, int Generation,
    LifeTaskKind? Activity = null, string? Focus = null, float ActionSeconds = 0, IReadOnlyList<LifeSignalView>? Signals = null, double Fraction = 0);

// Explicit state allows exact continuation after saving; never consumes legacy Random.
internal static class LifeRandom
{
    internal static ulong Next(ref ulong state)
    {
        if (state == 0) state = 0x9e3779b97f4a7c15UL;
        state ^= state >> 12;
        state ^= state << 25;
        state ^= state >> 27;
        return unchecked(state * 2685821657736338717UL);
    }
    internal static int Range(ref ulong state, int min, int max) => min + (int)(Next(ref state) % (uint)(max - min + 1));
}
