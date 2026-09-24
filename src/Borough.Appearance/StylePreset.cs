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
    ushort Zones)
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
