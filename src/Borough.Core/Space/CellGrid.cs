using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The Cell grid: 32×32 Tiles, frozen, and the storage unit of every Map Layer.
/// </summary>
/// <remarks>
/// <para>
/// <b>A world-creation constant, baked into the save, and never tuned</b> (<c>adr/0034</c>,
/// <c>CONTEXT.md</c> → Cell). Its size <em>is</em> the resolution of pollution, which feeds Fertility
/// and Desirability and therefore the choice model — so changing it changes the State Hash. It is a
/// <c>const</c> here and not a Ruleset value for exactly that reason: the Ruleset is the set of
/// numbers a designer may change without changing the city, and this is not one of them.
/// </para>
/// <para>
/// <b>Every conversion is a shift, because 32 is a power of two.</b> That is not a micro-optimisation
/// — it is what makes the Cell/Chunk split cost nothing. A strict power-of-two divisor means no
/// boundary can disagree with another about which side of a line something is on, which is the
/// property <c>adr/0034</c> bought the split with.
/// </para>
/// </remarks>
public static class CellGrid
{
    /// <summary>The Cell's edge, in Tiles. <b>Design constant. Never tuned.</b></summary>
    public const int TilesPerCell = 32;

    /// <summary>The shift <see cref="TilesPerCell"/> is a power of two by.</summary>
    public const int TilesPerCellShift = 5;

    /// <summary>Tiles in one Cell. Sealing's denominator (<c>adr/0022</c>, as amended by <c>adr/0034</c>).</summary>
    public const int TilesInCell = TilesPerCell * TilesPerCell;

    /// <summary>
    /// The Cell's edge in metres, for stating a kernel range in domain units.
    /// </summary>
    /// <remarks>
    /// <b>≈128 m, from a Tile of roughly 4 m</b> (<c>CONTEXT.md</c> → Cell, <c>Quantities.Tiles</c>).
    /// It exists so that a kernel radius can be <em>authored in metres and derived into Cells</em>
    /// rather than the other way round — <c>02 §2.5</c> question 2 is <em>what is its actionable range
    /// in metres, and can you defend the figure from reality</em>, and a radius that was only ever a
    /// Cell count has no answer to it.
    /// </remarks>
    public const int MetresPerCell = 128;

    /// <summary>
    /// The map's edge, in Tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠ SETTLED 2026-08-12 at 512 Cells and DELIBERATELY NOT YET CHANGED HERE.</b> Session J closed
    /// this in <c>adr/0089</c> — <em>the map is sized by how many commutes fit across it</em> — at
    /// <b>512 Cells, a 16384² Tile map, 65.5 km a side</b>. A map is sized by the number of Commute
    /// Budgets that fit across it, which is <b>0.9</b> at the value below and 3.7–5.2 at 512; at 0.9 no
    /// Trip on the map can exceed the Budget, so the decline mechanism the Budget exists to drive is
    /// inert everywhere. The old 2048² fallback is struck.
    /// </para>
    /// <para>
    /// <b>The flip is gated on a defect, and the gate is why this still reads 128.</b>
    /// <see cref="RoadGenerator"/> lays a complete Street lattice over the whole map at world creation —
    /// <c>(WorldTiles ÷ block_tiles + 1)²</c> nodes — so 512 would generate <b>525,312 Street
    /// Segments</b> and, at <c>lots_per_segment = 5</c>, <b>2,626,560 Lots</b> against the 225,000
    /// <see cref="Entities.World"/> allocates for a 1M city. That is <c>adr/0021</c>'s <em>memory scales
    /// with developed area, not with map area</em> being false in exactly one place, and it is invisible
    /// at 128 because a 16 km map is one a city genuinely does pave. Its repair is
    /// <c>plans/0002</c> ledger #2 — <em>open map, or progressive land unlock</em> — which is a design
    /// question and must not be answered by capping the generator here.
    /// </para>
    /// <para>
    /// Changing this moves every State Hash and re-records all three golden baselines, so it is one
    /// commit of its own. Nothing else in the grid arithmetic cares — it is a shift either way — which
    /// is why this is a named constant with one reader rather than a number scattered through the
    /// diffusion.
    /// </para>
    /// <para>
    /// <b>Derived from the Cell count rather than the other way round, and the direction is
    /// load-bearing.</b> Stating the map in Tiles and dividing would admit a map that is not a whole
    /// number of Cells — 4,097 Tiles gives a fractional Cell at the north and east edges, which is a
    /// Map Layer value covering a different amount of ground than every other one. Multiplying cannot
    /// express that map. (It is also the spelling <c>BOR0203</c> accepts: the division would have to
    /// state its rounding, and the honest rounding for a map size is <em>do not allow the case</em>.)
    /// </para>
    /// </remarks>
    public const int WorldTiles = WorldCells * TilesPerCell;

    /// <summary>
    /// The map's edge, in Cells. 128, which is a 4096² Tile map — and <b>512 is the decided value,
    /// gated</b>. See <see cref="WorldTiles"/>.
    /// </summary>
    public const int WorldCells = 128;

    /// <summary>Every Cell on the map. The residency index's length, and nothing else's.</summary>
    public const int WorldCellCount = WorldCells * WorldCells;

    /// <summary>The Cell a Tile coordinate falls in.</summary>
    /// <remarks>
    /// An arithmetic shift, so it floors for negatives exactly as <see cref="IntegerMath.FloorDiv"/>
    /// does. Tile −1 is in Cell −1 rather than in Cell 0, which is the answer a boundary test wants.
    /// </remarks>
    public static Cells ToCells(Tiles tiles) =>
        new(IntegerMath.ShiftRight(tiles.Raw, TilesPerCellShift));

    /// <summary>The Tile at a Cell's low corner.</summary>
    public static Tiles ToTiles(Cells cells) =>
        new(IntegerMath.ShiftLeft(cells.Raw, TilesPerCellShift));

    /// <summary>A Cell extent, in metres. For reporting a range in the units it was authored in.</summary>
    public static int ToMetres(Cells cells) => cells.Raw * MetresPerCell;

    /// <summary>
    /// The Cell extent that covers a range stated in metres, rounded up.
    /// </summary>
    /// <remarks>
    /// Rounded up rather than to nearest, because a kernel radius is a <em>reach</em>: rounding down
    /// would silently shorten a plume whose range was defended from reality, and the truncation would
    /// be invisible in the field it produced.
    /// </remarks>
    public static Cells FromMetres(int metres) =>
        new(IntegerMath.CeilDiv(metres, MetresPerCell));

    /// <summary>Whether a Cell coordinate is on the map.</summary>
    public static bool Contains(Cells east, Cells north) =>
        (uint)east.Raw < WorldCells && (uint)north.Raw < WorldCells;

    /// <summary>
    /// The row-major index of a Cell on the map. North-major, so a west-east scan is contiguous.
    /// </summary>
    /// <remarks>
    /// <b>Not a slot and never a sort key.</b> It addresses the residency index, which is a lookup
    /// from a coordinate to whatever slot happens to hold that Cell. <c>05 §3</c>'s rule about handle
    /// indices applies to the slot on the other side, not to this.
    /// </remarks>
    public static int Index(Cells east, Cells north) => (north.Raw * WorldCells) + east.Raw;
}

/// <summary>
/// The Chunk grid: the technical partition, a strict multiple of the Cell, and <b>provisionally 1:1</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>1:1 is recorded as provisional and belongs to a profiler, not to this slice</b> (<c>adr/0034</c>,
/// <c>adr/0040</c>). S2 owns the real value because pathfinding has the strongest claim on it, and S2
/// R3 has already narrowed the pathfinding cluster to 8 or 16 Chunks a side without closing it. What
/// this slice owes is that moving the number is a <em>shift</em> and touches nothing that hashes.
/// </para>
/// <para>
/// <b>Strict multiple is load-bearing.</b> It is what makes every Cell↔Chunk conversion a shift, and
/// what makes it impossible for the two grids to disagree about a boundary. A Chunk that were merely
/// <em>about</em> a multiple of the Cell would put a Layer value on two sides of a save record.
/// </para>
/// </remarks>
public static class ChunkGrid
{
    /// <summary>The Chunk's edge, in Cells. <b>Provisional: 1:1 with the Cell.</b></summary>
    public const int CellsPerChunk = 1;

    /// <summary>The shift <see cref="CellsPerChunk"/> is a power of two by.</summary>
    public const int CellsPerChunkShift = 0;

    /// <summary>The Chunk's edge, in Tiles. ≥32×32 by <c>02 §2.1</c>.</summary>
    public const int TilesPerChunk = CellsPerChunk * CellGrid.TilesPerCell;

    /// <summary>The Chunk a Cell coordinate falls in.</summary>
    public static Chunks ToChunks(Cells cells) =>
        new(IntegerMath.ShiftRight(cells.Raw, CellsPerChunkShift));

    /// <summary>The Cell at a Chunk's low corner.</summary>
    public static Cells ToCells(Chunks chunks) =>
        new(IntegerMath.ShiftLeft(chunks.Raw, CellsPerChunkShift));
}
