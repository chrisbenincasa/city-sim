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
/// <param name="Paints">The paint schemes a body draws one of, or none where it keeps its model's colours.</param>
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
    FamilyBody? Body = null,
    PaintScheme[]? Paints = null)
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

/// <summary>An sRGB colour, as authored.</summary>
public readonly record struct Paint(byte R, byte G, byte B);

/// <summary>
/// Colours for some of a body's parts, drawn together. A part the scheme does not name keeps its
/// model's colour.
/// </summary>
/// <param name="Weight">The scheme's share of a draw among its family's schemes.</param>
public sealed record PaintScheme(IReadOnlyDictionary<string, Paint> Parts, int Weight);

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

    /// <summary>
    /// A residential entrance. Neighbouring hall bays on the ground storey share one door centred on
    /// them, under a canopy on the street wall. Above the ground a hall bay is a window.
    /// </summary>
    Hall,

    /// <summary>
    /// A door onto a balcony that spans it. On the ground storey it is a garden door with no balcony.
    /// </summary>
    Balcony,
}

/// <summary>
/// An opening's size, and the height of its foot above its storey's floor. A <c>null</c> sill keeps
/// the builder's own.
/// </summary>
public readonly record struct OpeningSize(float Width, float Height, float? Sill);

/// <summary>
/// One storey's bays along a wall. A filling token repeats to take up the bays the fixed tokens
/// leave, shared evenly among the filling tokens. Bays that do not share evenly go in pairs to the
/// outermost fillers, and a last odd one to the middle filler, so a symmetric row stays symmetric.
/// A token of several kinds lays them in turn, so a filling <c>balcony+window</c> alternates.
/// </summary>
public sealed record BayRow(BayKind[][] Tokens, bool[] Fills)
{
    public static readonly BayRow Blank = new([[BayKind.Blank]], [true]);

    /// <summary>The row laid out over <paramref name="bays"/> bays.</summary>
    public BayKind[] Over(int bays)
    {
        int fixedCount = Fills.Count(f => !f);
        int fillers = Tokens.Length - fixedCount;
        int spare = Math.Max(0, bays - fixedCount);
        int[] share = new int[fillers];
        if (fillers > 0)
        {
            Array.Fill(share, spare / fillers);
            int left = spare % fillers;
            for (int outer = 0; left >= 2; outer++, left -= 2)
            {
                share[outer]++;
                share[fillers - 1 - outer]++;
            }

            if (left == 1) share[(fillers - 1) / 2]++;
        }

        var laid = new List<BayKind>(bays);
        int filler = 0;
        for (int i = 0; i < Tokens.Length && laid.Count < bays; i++)
        {
            int count = Fills[i] ? share[filler++] : 1;
            for (int n = 0; n < count && laid.Count < bays; n++) laid.Add(Tokens[i][n % Tokens[i].Length]);
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
/// <param name="Vents">Rooftop vents, spaced evenly along the frontage.</param>
/// <param name="Openings">
/// Opening sizes that replace the builder's own for window, door, stair, shop, roller and balcony
/// bays, on every storey.
/// </param>
/// <param name="GableDegrees">
/// The pitch of a gable roof whose ridge runs along the street, or 0 for a flat roof.
/// </param>
/// <param name="Chimney">A chimney stands behind the ridge near the right-hand end.</param>
/// <param name="Steps">A step stands before each street door.</param>
/// <param name="PartyLine">
/// The body reads as two halves: a party line runs up the middle of the street and back walls and an
/// upstand crosses the roof.
/// </param>
/// <param name="Shopfront">
/// A fascia runs over each wall's ground-storey shops, and an awning shades each run of street shops.
/// </param>
/// <param name="Panels">Panel joints stand at every bay line of the street and back walls, with a floor band at each storey.</param>
/// <param name="Rooflights">Two rooflights and a vent stand over each street bay.</param>
/// <param name="ReceivingCanopy">A canopy shelters each roller door on the back wall.</param>
public sealed record FamilyBody(
    string Library,
    IReadOnlyDictionary<string, (float Along, float Up)> TileMetres,
    float BayMetres,
    float ParapetMetres,
    bool Pilasters,
    int Plant,
    WallRule Street,
    WallRule Back,
    WallRule Side,
    bool RoofHatch,
    int Vents,
    IReadOnlyDictionary<BayKind, OpeningSize> Openings,
    float GableDegrees,
    bool Chimney,
    bool Steps,
    bool PartyLine = false,
    bool Shopfront = false,
    bool Panels = false,
    bool Rooflights = false,
    bool ReceivingCanopy = false);

/// <summary>The side walls a body shares with a neighbour, left and right as seen from the street.</summary>
[Flags]
public enum AttachedSides : byte
{
    None = 0,
    Left = 1,
    Right = 2,

    /// <summary>The left neighbour runs crosswise, so the roof hips down to it.</summary>
    LeftCrosswise = 4,

    /// <summary>The right neighbour runs crosswise, so the roof hips down to it.</summary>
    RightCrosswise = 8,
}
