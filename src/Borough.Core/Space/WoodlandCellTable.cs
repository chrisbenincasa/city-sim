using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell's worth of Woodland. Empty for the reason <see cref="LayerCell"/> is empty.
/// </summary>
public readonly struct WoodlandCell;

/// <summary>
/// How many of each Cell's Tiles are wooded — <b>dense, one row per Cell, and Sealing is its
/// ceiling.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0158</c>, milestone 24 task 8a. <see cref="WoodlandGenerator.LayInto"/> fills it at world
/// creation and <see cref="MapLayers.Seal"/> takes from it as ground is built on. <b>Nothing puts
/// Woodland back yet</b> — regrowth is task 8b, and it carries the rate this table deliberately does
/// not know about.
/// </para>
/// <para>
/// <b>A table of its own rather than a column on <see cref="LayerCellTable"/>, and it is the same
/// decision <see cref="TerrainCellTable"/> took for the same reason.</b> A Layer row means
/// <em>something happened here</em>, so that table is sparse by design and every Layer pass is
/// <c>O(live rows)</c>. <b>Woodland is the one quantity that exists where the city is not</b>: putting
/// it there would create a row for every wooded Cell on the Tick the world is made, which is not a
/// storage change but a <em>cost</em> change to passes that never asked about trees. The measurement
/// is <see cref="TerrainCellTable"/>'s and is not re-taken here.
/// </para>
/// <para>
/// <b>Sealing is the ceiling, and that is arithmetic rather than a rule.</b> A Cell has
/// <see cref="CellGrid.TilesInCell"/> Tiles; <see cref="LayerCellTable.Sealing"/> counts those ever
/// built on and this counts those wooded, and the two sets cannot overlap because building over forest
/// clears it (<c>CONTEXT.md</c> → Zone). So <c>Woodland + Sealing ≤ TilesInCell</c> is what the two
/// counts <em>mean</em>. ⚠ <b>The bound spans two tables and neither can enforce it alone</b> — it is
/// checked where both are visible, in <see cref="MapLayers"/>, which is also the only place that may
/// write this column once the world is running.
/// </para>
/// <para>
/// <b>The slot IS <see cref="CellGrid.Index"/>.</b> Every row is allocated in the constructor, in
/// index order, and none is ever freed — so there is no residency index, no <c>Ensure</c>, and no
/// east/north columns to store a coordinate the position already carries. Identical in shape to
/// <see cref="TerrainCellTable"/>, and deliberately so.
/// </para>
/// <para>
/// <b><c>(saved AND hashed)</c>, four bytes a Cell.</b> ⚠ <b>An <c>int</c> rather than the
/// <c>ushort</c> the range would fit</b>: the count is bounded by
/// <see cref="CellGrid.TilesInCell"/> = 1,024, which a <c>ushort</c> holds with room to spare. It is an
/// <c>int</c> because <see cref="LayerCellTable.Sealing"/> is one and the two are added, compared and
/// subtracted on every write — <em>a narrower column here would buy 512 KB and pay for it with a
/// conversion at every site the bound is checked</em>, which is the wrong trade at this size.
/// </para>
/// </remarks>
[Table]
public sealed class WoodlandCellTable
{
    private readonly Rows<WoodlandCell> _rows;

    /// <summary>Allocates one row per Cell, in <see cref="CellGrid.Index"/> order.</summary>
    /// <remarks>
    /// <b>Every row, here, once</b> — <see cref="TerrainCellTable"/>'s constructor and its reasoning,
    /// which is that a Cell that could be missing would need a residency index and a residency index
    /// is the thing this table exists to not have.
    /// </remarks>
    public WoodlandCellTable()
    {
        _rows = new Rows<WoodlandCell>("woodland_cell", CellGrid.WorldCellCount);

        Tiles = _rows.Saved<int>("tiles");

        _rows.Seal();

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            _rows.Allocate();
        }
    }

    /// <summary>The allocator, for the State Hash and the save.</summary>
    public Rows<WoodlandCell> Rows => _rows;

    /// <summary>
    /// How many of this Cell's Tiles are wooded. <b>0 to <see cref="CellGrid.TilesInCell"/>.</b>
    /// </summary>
    /// <remarks>
    /// <b>A count and not a fraction</b>, so it is denominated the same way
    /// <see cref="LayerCellTable.Sealing"/> is and the two can be added without a scale between them.
    /// That shared denomination is the whole of why the ceiling is checkable.
    /// </remarks>
    public Column<int> Tiles { get; }

    /// <summary>How many of one Cell's Tiles are wooded.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The Cell is off the map.</exception>
    public int At(Cells east, Cells north) => Tiles[Slot(east, north)];

    /// <summary>
    /// Sets how many of one Cell's Tiles are wooded. <b>Does not check the Sealing ceiling.</b>
    /// </summary>
    /// <remarks>
    /// The ceiling spans two tables, so it is <see cref="MapLayers"/>'s to enforce and not this
    /// table's to know about. Callers outside world creation go through <see cref="MapLayers"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The Cell is off the map, or <paramref name="tiles"/> is negative or above
    /// <see cref="CellGrid.TilesInCell"/>.
    /// </exception>
    public void Set(Cells east, Cells north, int tiles)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tiles);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tiles, CellGrid.TilesInCell);

        Tiles[Slot(east, north)] = tiles;
    }

    /// <summary>
    /// A fingerprint of everything this table holds.
    /// </summary>
    /// <remarks>
    /// <b>The fold lives here and any remembered value lives on the owner</b>, which is
    /// <see cref="TerrainCellTable.Fingerprint"/>'s rule and <c>BOR0901</c>'s reason for it: every field
    /// of a <c>[Table]</c> type is a declared column, so a remembered fold would be exactly the
    /// undeclared field <c>adr/0003</c> refuses.
    /// </remarks>
    public ulong Fingerprint()
    {
        ulong hash = 0;
        _rows.FoldAll(ref hash);

        return hash;
    }

    /// <summary>The row for a Cell, which is its index and never a lookup.</summary>
    private static int Slot(Cells east, Cells north)
    {
        if (!CellGrid.Contains(east, north))
        {
            throw new ArgumentOutOfRangeException(
                nameof(east),
                $"Cell ({east.Raw}, {north.Raw}) is off a {CellGrid.WorldCells}-Cell map.");
        }

        return CellGrid.Index(east, north);
    }
}
