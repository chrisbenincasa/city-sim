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
    public const int MetresPerCell = TilesPerCell * Tiles.Metres;

    /// <summary>
    /// The map's edge, in Tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>✅ 512 Cells — a 16384² Tile map, 65.5 km a side. Settled 2026-08-12, flipped
    /// 2026-08-13.</b> Session J closed this in <c>adr/0089</c> — <em>the map is sized by how many
    /// commutes fit across it</em>. A map is sized by the number of Commute Budgets that fit across it,
    /// which was <b>0.9</b> at the old 128 and is <b>3.7–5.2</b> here; at 0.9 no Trip on the map could
    /// exceed the Budget, so the decline mechanism the Budget exists to drive was inert everywhere. The
    /// old 2048² fallback is struck.
    /// </para>
    /// <para>
    /// <b>⚠ The flip moved no State Hash, and that is worth stopping on.</b> All three golden baselines
    /// reproduced unchanged. <c>05 §4</c> says <em>a change is an optimisation if the State Hash is
    /// unchanged, and a design change otherwise, however it was motivated</em> — and by that test this
    /// reads as an optimisation, which it plainly is not: it decides whether the player can build
    /// separate towns or only one blob. <b>The rule tests whether a change moves <em>this</em> city; a
    /// map size is a bound on the cities that are <em>reachable</em>, and a fixture in one corner never
    /// approaches it.</b> It is hash-neutral <em>because</em> queue item 6 landed first — before that,
    /// the generator paved the map and this would have moved every hash in the project.
    /// </para>
    /// <para>
    /// <b>⚠ It cost 11.6 MB at 1M, not the 4 MB <c>adr/0089</c> accounted for</b> — 192,780 KB resident
    /// against 204,412 KB, both measured. That ADR counted four derived <c>int[]</c> residency arrays
    /// and missed a fifth map-scaled structure: <see cref="StreetGrid"/>'s node and edge index, sized
    /// from <see cref="WorldTiles"/> at ~3.2 MB. <b>It is sized that way correctly and must stay so</b>,
    /// because a player may lay a Street anywhere on the map under <c>CommandKind.Connect</c> and the
    /// index needs a slot for it. Trivial against 200 MB, and recorded because the inventory was stated
    /// as complete.
    /// </para>
    /// <para>
    /// <b>⚠ Three fixtures were laying at map extent and had to pin their own, all for one reason.</b>
    /// <c>rulesets/severance.toml</c> stranded <b>0%</b> of pedestrians on the worst of eight seeds
    /// after the flip — its sixteen Arterials spread over sixteen times the ground — so it had stopped
    /// demonstrating the mechanism it exists for, without a number in it moving; the walk-search
    /// benchmark's graph went from 16,641 nodes to 263,169, sixteen times the fixture for a benchmark
    /// claiming to time <em>the shipped city</em>; and the <c>[roads]</c> loader's two spatial maxima
    /// were <c>[InlineData]</c> literals of 4,096 and 4,097, of which the refused one failed loudly and
    /// <b>the accepted one stayed green while ceasing to test a boundary at all</b>. Each now states the
    /// extent it was characterised at. ***A paved extent is not a map size***, which is queue item 6's
    /// own finding arriving three files further on.
    /// </para>
    /// <para>
    /// <b>✅ THE GATE IS DISCHARGED — <c>plans/0003</c> queue item 6, 2026-08-13 — and the flip is now
    /// a one-line commit of its own that nothing stands in front of.</b> The gate was that
    /// <see cref="RoadGenerator"/> paved the whole map, <c>(WorldTiles ÷ block_tiles + 1)²</c> nodes, so
    /// 512 would have generated <b>525,312 Street Segments</b> and, at <c>lots_per_segment = 5</c>,
    /// <b>2,626,560 Lots</b> against the 225,000 <see cref="Entities.World"/> allocates for a 1M city.
    /// <see cref="RoadGenerator.LayInto"/> now takes an extent and
    /// <see cref="Entities.SyntheticCity"/> derives it from the Lot count the population asks for, so a
    /// 1M city paves a 4,800-Tile lattice — <b>8.6% of a 512-Cell map</b>, which is the buildable
    /// fraction <c>adr/0089</c> reasoned about — and the map's own size stops being an input to it.
    /// </para>
    /// <para>
    /// <b>⚠ The sentence this comment used to carry is the reason the gate existed, and it was never
    /// true.</b> It said the lattice was laid <em>"at world creation"</em>. It is laid by
    /// <see cref="Entities.SyntheticCity"/>, which is reached only by <c>CommandKind.Populate</c> — a
    /// verb no player has — so a player's world has had <b>no roads at all</b> since 5a and
    /// <c>adr/0021</c> was never violated where this paragraph said it was. Both this item and
    /// <c>adr/0089</c> reasoned from that sentence.
    /// <c>adr/0093</c>: <em>a description of the build is where to look, and never what you found</em>,
    /// and its writing half is the fix — <b>name a symbol, never a time</b>. <em>"At world creation"</em>
    /// cannot be checked without already knowing the answer; <em>"when <c>SyntheticCity</c> runs"</em> is
    /// one grep.
    /// </para>
    /// <para>
    /// ~~Changing this moves every State Hash and re-records all three golden baselines, so it is one
    /// commit of its own.~~ <b>Measured false on the day it was acted on:</b> it moved none of them, for
    /// the reason two paragraphs up. It was still one commit of its own, which was the useful half of
    /// the advice. Nothing else in the grid arithmetic cares — it is a shift either way — which is why
    /// this is a named constant with one reader rather than a number scattered through the diffusion.
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
    /// The map's edge, in Cells. <b>512, which is a 16384² Tile map, 65.5 km a side.</b> See
    /// <see cref="WorldTiles"/>.
    /// </summary>
    public const int WorldCells = 512;

    /// <summary>Every Cell on the map. The residency index's length, and nothing else's.</summary>
    public const int WorldCellCount = WorldCells * WorldCells;

    /// <summary>The Cell a Tile coordinate falls in.</summary>
    /// <remarks>
    /// An arithmetic shift, so it floors for negatives exactly as <see cref="IntegerMath.FloorDiv"/>
    /// does. Tile −1 is in Cell −1 rather than in Cell 0, which is the answer a boundary test wants.
    /// </remarks>
    public static Cells ToCells(Tiles tiles) =>
        new(IntegerMath.ShiftRight(tiles.Raw, TilesPerCellShift));

    /// <summary>The Cell a Tile coordinate falls in, clamped onto the map.</summary>
    /// <remarks>
    /// <para>
    /// <b>For a boundary coordinate, which is a fencepost rather than a position.</b> A map of N
    /// Tiles has N+1 grid lines, so the far edge of the world is Tile <see cref="WorldTiles"/> — one
    /// past the last Tile, and in a Cell that does not exist. A Road Node may legitimately stand
    /// there and a Lot on a map edge may sit against it.
    /// </para>
    /// <para>
    /// ⚠ <b>Use this only where the caller means <em>the ground under this</em> and a boundary
    /// coordinate is expected.</b> <see cref="ToCells(Tiles)"/> stays the default and stays strict,
    /// because silently folding an off-map Cell onto the edge is how a bug in a coordinate becomes a
    /// plausible-looking field.
    /// </para>
    /// </remarks>
    public static Cells ToCellsClamped(Tiles tiles)
    {
        int cell = ToCells(tiles).Raw;

        if (cell < 0)
        {
            cell = 0;
        }

        if (cell >= WorldCells)
        {
            cell = WorldCells - 1;
        }

        return new Cells(cell);
    }

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
