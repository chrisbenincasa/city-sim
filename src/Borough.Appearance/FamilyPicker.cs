using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Appearance;

public enum FamilyChoice : byte
{
    /// <summary>A family's conditions admitted the Building.</summary>
    Eligible,

    /// <summary>No family admitted the Building, so its kind's fallback draws it.</summary>
    Fallback,

    /// <summary>No family admitted the Building and its kind has no fallback.</summary>
    Missing,
}

public readonly record struct FamilyPick(AppearanceFamily? Family, FamilyChoice Choice);

/// <summary>The run a detached house stands in: the coordinates its shared draws use, and what it shares.</summary>
public readonly record struct HouseRun(ulong Entity, Ticks Era, RunShare Share);

/// <summary>
/// The terrace an attached house stands in: the coordinates its shared draws use, the house's place
/// in it counted from the face's start, and how many Lots it spans.
/// </summary>
public readonly record struct Terrace(ulong Entity, Ticks Era, ulong Cell, int Index, int Houses);

/// <summary>
/// Chooses one Appearance Family per Building by a weighted draw over the families that admit it.
/// </summary>
/// <remarks>
/// The draw is a function of the Building's own facts and id, so no edit elsewhere in the city can
/// change it (adr/0173). Attached Buildings on one block face raised in the same era draw with a
/// shared seed, and a shared eligible set then gives them the same family. Detached houses on one
/// block face and era fall into runs of neighboring Lots by their position along the face, so a run
/// is a function of each Building's own facts too. Attached houses that only terrace families admit
/// fall into terraces the same way, and a terrace never shares its family with the terraces either
/// side of it.
/// </remarks>
public static class FamilyPicker
{
    private const ulong FaceSpace = 1UL << 63;
    private const ulong RunSpace = FaceSpace | (1UL << 62);
    private const ulong TerraceSpace = FaceSpace | (1UL << 61);
    private const int RunFaceShift = 20;

    public static FamilyPick Pick(StylePreset preset, WorldKey world, in BuildingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(preset);

        long total = 0;
        bool terraced = true;
        foreach (AppearanceFamily family in preset.Families)
        {
            if (family.Fallback || !family.Admits(facts)) continue;
            total += family.Weight;
            terraced &= family.Body?.Terrace is not null;
        }

        if (total > 0 && terraced && TerraceOf(preset, world, facts) is { } terrace)
        {
            return new FamilyPick(TerraceFamily(preset, world, facts, terrace), FamilyChoice.Eligible);
        }

        if (total > 0)
        {
            (ulong entity, Ticks era) = SeedOf(preset, world, facts);
            ulong draw = Randomness.Draw(world, entity, era, PurposeTag.AppearanceFamily);
            long at = (long)(draw % (ulong)total);
            foreach (AppearanceFamily family in preset.Families)
            {
                if (family.Fallback || !family.Admits(facts)) continue;
                at -= family.Weight;
                if (at < 0) return new FamilyPick(family, FamilyChoice.Eligible);
            }
        }

        string kind = facts.Kind;
        AppearanceFamily? fallback = Array.Find(preset.Families, f => f.Fallback && Array.IndexOf(f.Kinds, kind) >= 0);
        return fallback is null
            ? new FamilyPick(null, FamilyChoice.Missing)
            : new FamilyPick(fallback, FamilyChoice.Fallback);
    }

    /// <summary>Which of <see cref="FamilyBodyBuilder.HouseSeeds"/> draws a Building's house body is built from.</summary>
    public static int House(WorldKey world, ulong building) => House(world, building, Ticks.Zero);

    /// <summary>
    /// The draw a Building's house body is built from, and the draw whose roof pitch it takes. A matched
    /// run shares the pitch and a run of copies shares both.
    /// </summary>
    public static (int House, int Pitch) House(StylePreset preset, WorldKey world, in BuildingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(preset);
        HouseRun run = RunOf(preset, world, facts);
        int own = House(world, facts.Id);
        int shared = House(world, run.Entity, run.Era);
        return run.Share switch
        {
            RunShare.Copies => (shared, shared),
            RunShare.Matched => (own, shared),
            _ => (own, own),
        };
    }

    /// <summary>
    /// The run a Building stands in. Only Buildings on a <see cref="BlockPattern.Detached"/> face that
    /// is not attached run together, and a preset without runs leaves every Building alone.
    /// </summary>
    /// <remarks>
    /// A face draws one run length in Lots for each era. A run is the stretch of the face that many
    /// parcels long holding the Lot's Address, so faces carved into equal parcels split into equal runs.
    /// </remarks>
    public static HouseRun RunOf(StylePreset preset, WorldKey world, in BuildingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.Runs is not { } runs || facts.Face == 0 || facts.ParcelFrontage <= 0
            || facts.Pattern != BlockPattern.Detached || Array.IndexOf(preset.Attached, facts.Pattern) >= 0)
        {
            return new HouseRun(facts.Id, Ticks.Zero, RunShare.Alone);
        }

        Ticks era = EraOf(preset, facts);
        ulong lengths = (ulong)(runs.Houses.High - runs.Houses.Low + 1);
        long houses = runs.Houses.Low + (long)(Randomness.Draw(world, FaceSpace | facts.Face, era, PurposeTag.AppearanceRun) % lengths);
        ulong cell = (ulong)(facts.Offset / (facts.ParcelFrontage * houses));
        ulong entity = RunSpace | (facts.Face << RunFaceShift) | cell;

        long total = runs.Alone + runs.Matched + runs.Copies;
        long at = (long)(Randomness.Draw(world, entity, era, PurposeTag.AppearanceRunShare) % (ulong)total);
        RunShare share = at < runs.Alone ? RunShare.Alone
            : at < runs.Alone + runs.Matched ? RunShare.Matched
            : RunShare.Copies;
        return new HouseRun(entity, era, share);
    }

    /// <summary>
    /// The terrace an attached Building stands in, or <c>null</c> where the preset has no terraces or
    /// the Building stands on no attached face.
    /// </summary>
    /// <remarks>
    /// A face draws one terrace length in Lots for each era, and a terrace is that many parcels of the
    /// face, as a run of detached houses is.
    /// </remarks>
    public static Terrace? TerraceOf(StylePreset preset, WorldKey world, in BuildingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.Terraces is not { } span || facts.Face == 0 || facts.ParcelFrontage <= 0
            || Array.IndexOf(preset.Attached, facts.Pattern) < 0)
        {
            return null;
        }

        Ticks era = EraOf(preset, facts);
        ulong lengths = (ulong)(span.High - span.Low + 1);
        int houses = (int)(span.Low + (long)(Randomness.Draw(world, FaceSpace | facts.Face, era, PurposeTag.AppearanceTerrace) % lengths));
        int parcel = facts.Offset / facts.ParcelFrontage;
        ulong cell = (ulong)(parcel / houses);
        return new Terrace(TerraceSpace | (facts.Face << RunFaceShift) | cell, era, cell, parcel % houses, houses);
    }

    /// <summary>The roof pitch in degrees a terrace's houses share, or 0 for a form without one.</summary>
    public static int TerracePitch(AppearanceFamily family, WorldKey world, Terrace terrace)
    {
        ArgumentNullException.ThrowIfNull(family);
        if (family.Body?.Terrace?.PitchDegrees is not { } pitch) return 0;
        ulong draw = Randomness.Draw(world, terrace.Entity, terrace.Era, PurposeTag.AppearanceTerracePitch);
        return (int)(pitch.Low + (long)(draw % (ulong)(pitch.High - pitch.Low + 1)));
    }

    /// <summary>
    /// A terrace's family. An even terrace takes its first-ranked family; an odd one passes over the
    /// families its two neighbors took, so no two neighboring terraces share a family.
    /// </summary>
    /// <remarks>
    /// Each terrace ranks every family once, so two Buildings that admit different families still
    /// agree on a terrace's pick unless the pick admits only one of them. A neighbor whose Lot admits
    /// a family this Building does not can therefore still match it.
    /// </remarks>
    private static AppearanceFamily TerraceFamily(StylePreset preset, WorldKey world, in BuildingFacts facts, Terrace terrace)
    {
        ulong Seed(ulong cell) => Randomness.Draw(world, (terrace.Entity & ~CellMask) | cell, terrace.Era, PurposeTag.AppearanceFamily);

        if (terrace.Cell % 2 == 0) return Ranked(preset, facts, Seed(terrace.Cell));
        AppearanceFamily before = Ranked(preset, facts, Seed(terrace.Cell - 1));
        AppearanceFamily after = Ranked(preset, facts, Seed(terrace.Cell + 1));
        return Ranked(preset, facts, Seed(terrace.Cell), before, after);
    }

    private const ulong CellMask = (1UL << RunFaceShift) - 1;

    /// <summary>
    /// The lowest-scoring family that admits the Building, leaving out up to two of them while any
    /// other remains. A family's score is its lowest of one hashed ticket per unit of weight, so each
    /// family ranks first in proportion to its weight.
    /// </summary>
    private static AppearanceFamily Ranked(StylePreset preset, in BuildingFacts facts, ulong seed,
        AppearanceFamily? skip = null, AppearanceFamily? also = null)
    {
        AppearanceFamily? best = null, first = null;
        ulong bestScore = ulong.MaxValue, firstScore = ulong.MaxValue;
        for (int i = 0; i < preset.Families.Length; i++)
        {
            AppearanceFamily family = preset.Families[i];
            if (family.Fallback || !family.Admits(facts)) continue;
            ulong score = ulong.MaxValue;
            for (int ticket = 0; ticket < family.Weight; ticket++)
            {
                score = Math.Min(score, Mix(seed ^ ((ulong)i << 32) ^ (ulong)ticket));
            }

            if (first is null || score < firstScore) (first, firstScore) = (family, score);
            if (ReferenceEquals(family, skip) || ReferenceEquals(family, also)) continue;
            if (best is null || score < bestScore) (best, bestScore) = (family, score);
        }

        return best ?? first!;
    }

    private static ulong Mix(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
        z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
        return z ^ (z >> 31);
    }

    private static int House(WorldKey world, ulong entity, Ticks era) =>
        (int)(Randomness.Draw(world, entity, era, PurposeTag.AppearanceHouse) % FamilyBodyBuilder.HouseSeeds);

    /// <summary>
    /// Chooses a body's paint scheme by a weighted draw on the Building's own id, so each house in an
    /// attached run is painted on its own.
    /// </summary>
    /// <returns>The scheme's index in <see cref="AppearanceFamily.Paints"/>, or -1 where the family has none.</returns>
    public static int Paint(AppearanceFamily family, WorldKey world, ulong building) => Paint(family, world, building, Ticks.Zero);

    /// <summary>Chooses a body's paint scheme, shared by a run of copies.</summary>
    public static int Paint(AppearanceFamily family, StylePreset preset, WorldKey world, in BuildingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(preset);
        HouseRun run = RunOf(preset, world, facts);
        return run.Share == RunShare.Copies ? Paint(family, world, run.Entity, run.Era) : Paint(family, world, facts.Id);
    }

    private static int Paint(AppearanceFamily family, WorldKey world, ulong entity, Ticks era)
    {
        ArgumentNullException.ThrowIfNull(family);
        if (family.Paints is not { Length: > 0 } schemes) return -1;

        long total = 0;
        foreach (PaintScheme scheme in schemes) total += scheme.Weight;
        long at = (long)(Randomness.Draw(world, entity, era, PurposeTag.AppearancePaint) % (ulong)total);
        for (int i = 0; i < schemes.Length; i++)
        {
            at -= schemes[i].Weight;
            if (at < 0) return i;
        }

        return schemes.Length - 1;
    }

    /// <summary>
    /// The entity and Tick coordinates of a Building's draw. An attached Building draws on its block
    /// face and era, a house in a matched run or a run of copies on its run, and any other on its own id.
    /// </summary>
    /// <remarks>Face and run keys carry the top bit, so they never meet a Building id.</remarks>
    private static (ulong Entity, Ticks Era) SeedOf(StylePreset preset, WorldKey world, in BuildingFacts facts)
    {
        if (facts.Face != 0 && Array.IndexOf(preset.Attached, facts.Pattern) >= 0)
        {
            return (FaceSpace | facts.Face, EraOf(preset, facts));
        }

        HouseRun run = RunOf(preset, world, facts);
        return run.Share == RunShare.Alone ? (facts.Id, Ticks.Zero) : (run.Entity, run.Era);
    }

    private static Ticks EraOf(StylePreset preset, in BuildingFacts facts)
    {
        ulong era = (ulong)(facts.RaisedDay / preset.EraDays);
        return new Ticks((era * 4) + (ulong)facts.Side);
    }
}
