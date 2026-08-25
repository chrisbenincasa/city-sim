using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// The <c>w₅</c> shoreline source's two parameters: how far a fouled shore reaches, and how hard.
/// </summary>
/// <param name="Range">
/// How far from the water's edge the term reaches. <b>Zero disables the query</b>, which is the same
/// spelling as a world with no water.
/// </param>
/// <param name="IntensityPerFill">
/// Q16.16. What a <em>completely</em> fouled body puts at one Tile's distance from its edge, before
/// the falloff and before <c>w₅</c>. ⚠ <b>Per unit of FILL and not per unit of level</b> — see
/// <see cref="Shoreline"/> for why those are different quantities and why this one is the right one.
/// </param>
public readonly record struct ShorelineSource(Tiles Range, int IntensityPerFill);

/// <summary>
/// Desirability's <c>w₅</c> term: <b>how badly the water near a Tile is fouled.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>02 §2.4</c> composes <c>− w₂·pollution − w₃·noise + w₄·amenity − w₅·shoreline</c>, and this is
/// the last of those four to be built. <b>It SUBTRACTS</b>: the quantity is fouling, so clean water
/// contributes exactly zero and the waterfront premium a real city has is not in this model at all —
/// it would be a positive term and there is only one of those (<c>adr/0123</c>'s amenity, at
/// milestone 15).
/// </para>
/// <para>
/// 🔴 <b>THE INTENSITY IS THE BODY'S FILL FRACTION AND NOT ITS LEVEL, AND THAT SHARPENS TWO
/// DOCUMENTS.</b> <c>adr/0034</c> and <c>CONTEXT.md</c> → Water Body both say the intensity is
/// <em>"the Bin's level"</em>. They name the quantity and never its units, and taking the level
/// <em>absolutely</em> is wrong in a way that only shows up once capacity is derived from body size
/// (<c>adr/0160</c>): the measured sea here is <b>33,435 Cells</b> against a pond's tens, so the same
/// tonnage tipped into either produces the same absolute level and ***a teaspoon in the sea would
/// foul its whole coastline as hard as it fouls a pond.*** The fraction is the concentration, it is
/// bounded in <c>[0, 1]</c> so a weight means the same thing on every body, and it is the same
/// geometry-derived gradient <c>CONTEXT.md</c> already claims for debt-versus-rent.
/// </para>
/// <para>
/// <b>The source set is the body's PERIMETER, not its area</b> — <c>CONTEXT.md</c> → Water Body:
/// <em>"an area's influence on land is its perimeter, and a coastline and a pond are one geometry at
/// two lengths."</em> So a wet Cell contributes only where at least one orthogonal neighbour is dry.
/// ⚠ <b>Summing over the area instead would make a wide sea louder than a narrow river at the same
/// distance for no reason a player could see</b>, since the interior Cells are hidden behind the
/// shore ones.
/// </para>
/// <para>
/// <b>Intensities are summed and the logarithm is taken once</b>, exactly as
/// <see cref="LineSourceQueries"/> does and for its stated reason: two equal sources are half a bel
/// worse, not twice as bad. <see cref="Transcendental.Log1P"/> and not <c>Log</c>, so clean water
/// returns zero rather than a large negative number.
/// </para>
/// <para>
/// ⚠ <b>It reads the Bin and never the catchment.</b> Which Cells drain into a body is
/// <see cref="CatchmentCellTable"/> and belongs to runoff; what this asks is <em>what water is near
/// this Tile</em>, which is proximity. Wiring the catchment in here would make a Tile upstream of a
/// fouled lake suffer for it, which is the wrong direction.
/// </para>
/// </remarks>
public sealed class Shoreline
{
    private readonly WaterCellTable _cells;
    private readonly WaterResidency _residency;
    private readonly WaterBodyTable _bodies;
    private readonly BinTable _bins;
    private readonly int _capacityPerCell;

    /// <param name="cells">The wet Cells.</param>
    /// <param name="residency">The dense Cell-to-slot index over them.</param>
    /// <param name="bodies">The bodies, for their Bin and their size.</param>
    /// <param name="bins">Where the levels are.</param>
    /// <param name="capacityPerCell">
    /// <c>[water] capacity_per_cell</c>. The denominator under every fill fraction.
    /// </param>
    public Shoreline(
        WaterCellTable cells,
        WaterResidency residency,
        WaterBodyTable bodies,
        BinTable bins,
        int capacityPerCell)
    {
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(residency);
        ArgumentNullException.ThrowIfNull(bodies);
        ArgumentNullException.ThrowIfNull(bins);

        _cells = cells;
        _residency = residency;
        _bodies = bodies;
        _bins = bins;
        _capacityPerCell = capacityPerCell;
    }

    /// <summary>
    /// The fouling level at a Tile. <b>Zero where the water in reach is clean, and zero is exact.</b>
    /// </summary>
    /// <remarks>
    /// Zero is <em>provable</em> rather than approximate: every body's fill is zero until something
    /// puts something in, each term is then zero, and <c>Log1P(0)</c> is zero. That is what lets the
    /// term ship on a world whose water is untouched without becoming <c>adr/0123</c>'s
    /// present-and-permanently-zero — the zero is a property of <em>that world</em>, and
    /// <c>rulesets/coastal.toml</c> is a world where it is not zero.
    /// </remarks>
    public int Fouling(ShorelineSource source, Tiles east, Tiles north)
    {
        int range = source.Range.Raw;

        if (range <= 0 || _capacityPerCell <= 0)
        {
            return 0;
        }

        Cells cellEast = CellGrid.ToCells(east);
        Cells cellNorth = CellGrid.ToCells(north);

        // Plus one because the range is measured from a Tile to the nearest point of a Cell's box, so
        // a Cell whose near edge is in range may have its index a further Cell out.
        int window = IntegerMath.CeilDiv(range, CellGrid.TilesPerCell) + 1;

        long total = 0;

        for (int across = -window; across <= window; across++)
        {
            for (int up = -window; up <= window; up++)
            {
                total += Contribution(
                    source,
                    new Cells(cellEast.Raw + across),
                    new Cells(cellNorth.Raw + up),
                    east,
                    north);
            }
        }

        // Saturated rather than checked, on LineSourceQueries.Level's reasoning: a read-only query
        // must not throw on a world somebody is allowed to build.
        return Transcendental.Log1P(total > int.MaxValue ? int.MaxValue : (int)total);
    }

    /// <summary>One shore Cell's contribution, or zero if it is not one, is clean, or is out of range.</summary>
    private long Contribution(
        ShorelineSource source, Cells cellEast, Cells cellNorth, Tiles east, Tiles north)
    {
        int slot = _residency.Slot(cellEast, cellNorth);

        if (slot == WaterResidency.NotResident || !IsShore(cellEast, cellNorth))
        {
            return 0;
        }

        int fill = Fill(_cells.Body[slot]);

        if (fill <= 0)
        {
            return 0;
        }

        int distance = DistanceTiles(cellEast, cellNorth, east, north);

        if (distance > source.Range.Raw)
        {
            return 0;
        }

        long scaled = ((long)fill * source.IntensityPerFill) >> Fixed.FractionalBits;

        // One Tile is the floor, for LineSourceQueries.Contribution's reason: a Tile standing in the
        // water is at distance zero and 1/0 is not a fouled beach.
        return IntegerMath.RoundDiv(scaled, distance < 1 ? 1 : distance);
    }

    /// <summary>
    /// Whether a wet Cell is on the water's edge. <b>Four neighbours, not eight.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>An off-map neighbour reads as dry</b> (<see cref="WaterResidency.Slot"/>), so a body
    /// reaching the map's boundary has its boundary Cells counted as shore. That costs nothing: there
    /// is no land off the map for them to foul, and every Tile this query is ever asked about is on
    /// it.
    /// </remarks>
    private bool IsShore(Cells east, Cells north) =>
        !_residency.IsWet(new Cells(east.Raw - 1), north)
        || !_residency.IsWet(new Cells(east.Raw + 1), north)
        || !_residency.IsWet(east, new Cells(north.Raw - 1))
        || !_residency.IsWet(east, new Cells(north.Raw + 1));

    /// <summary>How full a body's Bin is, Q16.16 in <c>[0, 1]</c>. See this class's remarks.</summary>
    private int Fill(Handle<WaterBody> body)
    {
        if (!_bodies.Rows.TryResolve(body, out int at)
            || !_bins.Rows.TryResolve(_bodies.Bin[at], out int bin))
        {
            return 0;
        }

        long capacity = (long)_bodies.CellCount[at] * _capacityPerCell;

        if (capacity <= 0)
        {
            return 0;
        }

        long level = _bins.LevelAt(bin);
        long fill = IntegerMath.RoundDiv(level * Fixed.One, capacity);

        return fill > Fixed.One ? Fixed.One : (int)fill;
    }

    /// <summary>Tiles from a Tile to the nearest point of a Cell's box, floored.</summary>
    private static int DistanceTiles(Cells cellEast, Cells cellNorth, Tiles east, Tiles north) =>
        (int)IntegerMath.SqrtFloor(
            (Gap(cellEast, east) * Gap(cellEast, east)) + (Gap(cellNorth, north) * Gap(cellNorth, north)));

    /// <summary>The gap on one axis, zero inside the Cell.</summary>
    private static long Gap(Cells cell, Tiles at)
    {
        long low = CellGrid.ToTiles(cell).Raw;
        long high = low + CellGrid.TilesPerCell - 1;

        return at.Raw < low ? low - at.Raw : at.Raw > high ? at.Raw - high : 0;
    }
}
