using Borough.Core.Space;

namespace Borough.Appearance;

/// <summary>An inclusive range of whole numbers.</summary>
public readonly record struct Bounds(long Low, long High)
{
    public bool Holds(long value) => value >= Low && value <= High;
}

/// <summary>
/// An authored architectural style a Building can be drawn in, and the facts that make a Building
/// eligible for it. An absent condition admits every Building.
/// </summary>
/// <param name="Kinds">The Ruleset kind names this family can draw.</param>
/// <param name="Weight">The family's share of a draw among the eligible families.</param>
/// <param name="Fallback">
/// Drawn only when no other family admits the Building. A fallback carries no conditions.
/// </param>
/// <param name="Model">The asset the shell resolves, or <c>null</c> where none is authored yet.</param>
/// <param name="Zones">Admits a Lot carrying any of these permission bits.</param>
/// <param name="Body">How the family builds its body, or <c>null</c> where the massing draws it.</param>
public sealed record AppearanceFamily(
    string Id,
    string File,
    int Line,
    string[] Kinds,
    int Weight,
    bool Fallback,
    string? Model,
    Bounds? Storeys,
    Bounds? FrontageMetres,
    Bounds? DepthMetres,
    Bounds? RaisedDay,
    BlockPattern[]? Patterns,
    ushort Zones,
    FamilyBody? Body = null)
{
    public bool Admits(in BuildingFacts facts)
    {
        string kind = facts.Kind;
        return Array.Exists(Kinds, k => k == kind)
            && (Storeys is not { } storeys || storeys.Holds(facts.Storeys))
            && (FrontageMetres is not { } frontage || frontage.Holds(facts.FrontageMetres))
            && (DepthMetres is not { } depth || depth.Holds(facts.DepthMetres))
            && (RaisedDay is not { } raised || raised.Holds(facts.RaisedDay))
            && (Patterns is not { } patterns || Array.IndexOf(patterns, facts.Pattern) >= 0)
            && (Zones == 0 || (Zones & facts.Zone) != 0);
    }
}

/// <summary>
/// A Style Preset: the Appearance Families a city is drawn from, chosen by the player at map start.
/// </summary>
/// <param name="EraDays">
/// The length of an era in game Days. Attached Buildings on one block face raised in the same era
/// share a draw, and so share a family.
/// </param>
/// <param name="Attached">The block patterns whose Buildings stand in attached runs.</param>
public sealed record StylePreset(string Name, int EraDays, BlockPattern[] Attached, AppearanceFamily[] Families);

/// <summary>What one bay of one storey shows on a wall.</summary>
public enum BayKind : byte
{
    Blank,
    Window,
    Shop,
    Entry,
    Door,
    Roller,
    Stair,
}

/// <summary>
/// One storey's bays along a wall. A filling token repeats to take up the bays the fixed tokens
/// leave, shared among the filling tokens from the left.
/// </summary>
public sealed record BayRow(BayKind[] Kinds, bool[] Fills)
{
    public static readonly BayRow Blank = new([BayKind.Blank], [true]);

    /// <summary>The row laid out over <paramref name="bays"/> bays.</summary>
    public BayKind[] Over(int bays)
    {
        int fixedCount = Fills.Count(f => !f);
        int fillers = Kinds.Length - fixedCount;
        int spare = Math.Max(0, bays - fixedCount);
        var laid = new List<BayKind>(bays);
        int filler = 0;
        for (int i = 0; i < Kinds.Length && laid.Count < bays; i++)
        {
            int count = !Fills[i] ? 1 : (spare / fillers) + (filler++ < spare % fillers ? 1 : 0);
            for (int n = 0; n < count && laid.Count < bays; n++) laid.Add(Kinds[i]);
        }

        while (laid.Count < bays) laid.Add(BayKind.Blank);
        return [.. laid];
    }
}

/// <summary>A wall's ground storey and every storey above it.</summary>
public sealed record WallRule(BayRow Ground, BayRow Upper);

/// <summary>
/// How a family builds its body at any size it admits. Metres throughout.
/// </summary>
/// <param name="Library">The authored model whose materials dress the body's parts.</param>
/// <param name="TileMetres">The size one texture tile covers, by part. Unlisted parts tile per metre.</param>
/// <param name="Plant">Rooftop plant units, spaced evenly along the frontage.</param>
public sealed record FamilyBody(
    string Library,
    IReadOnlyDictionary<string, (float Along, float Up)> TileMetres,
    float BayMetres,
    float ParapetMetres,
    bool Pilasters,
    int Plant,
    WallRule Street,
    WallRule Back,
    WallRule Side);
