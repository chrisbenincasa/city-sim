using Borough.Core.Determinism;
using Borough.Core.Quantities;

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

/// <summary>
/// Chooses one Appearance Family per Building by a weighted draw over the families that admit it.
/// </summary>
/// <remarks>
/// The draw is a function of the Building's own facts and id, so no edit elsewhere in the city can
/// change it (adr/0173). Attached Buildings on one block face raised in the same era draw with a
/// shared seed, and a shared eligible set then gives them the same family.
/// </remarks>
public static class FamilyPicker
{
    private const ulong FaceSpace = 1UL << 63;

    public static FamilyPick Pick(StylePreset preset, WorldKey world, in BuildingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(preset);

        long total = 0;
        foreach (AppearanceFamily family in preset.Families)
        {
            if (!family.Fallback && family.Admits(facts)) total += family.Weight;
        }

        if (total > 0)
        {
            (ulong entity, Ticks era) = SeedOf(preset, facts);
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

    /// <summary>
    /// Chooses a body's paint scheme by a weighted draw on the Building's own id, so each house in an
    /// attached run is painted on its own.
    /// </summary>
    /// <returns>The scheme's index in <see cref="AppearanceFamily.Paints"/>, or -1 where the family has none.</returns>
    public static int Paint(AppearanceFamily family, WorldKey world, ulong building)
    {
        ArgumentNullException.ThrowIfNull(family);
        if (family.Paints is not { Length: > 0 } schemes) return -1;

        long total = 0;
        foreach (PaintScheme scheme in schemes) total += scheme.Weight;
        long at = (long)(Randomness.Draw(world, building, Ticks.Zero, PurposeTag.AppearancePaint) % (ulong)total);
        for (int i = 0; i < schemes.Length; i++)
        {
            at -= schemes[i].Weight;
            if (at < 0) return i;
        }

        return schemes.Length - 1;
    }

    /// <summary>
    /// The entity and Tick coordinates of a Building's draw. An attached Building draws on its block
    /// face and era; any other draws on its own id.
    /// </summary>
    /// <remarks>Face keys carry the top bit, so they never meet a Building id.</remarks>
    private static (ulong Entity, Ticks Era) SeedOf(StylePreset preset, in BuildingFacts facts)
    {
        if (facts.Face == 0 || Array.IndexOf(preset.Attached, facts.Pattern) < 0)
        {
            return (facts.Id, Ticks.Zero);
        }

        ulong era = (ulong)(facts.RaisedDay / preset.EraDays);
        return (FaceSpace | facts.Face, new Ticks((era * 4) + (ulong)facts.Side));
    }
}
