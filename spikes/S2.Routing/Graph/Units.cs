using Borough.Core.Arithmetic;

namespace S2.Routing.Graph;

/// <summary>
/// The units this spike routes in, stated rather than left silent.
/// </summary>
/// <remarks>
/// <para>
/// <b>The cost function is time.</b> <c>02 §5.9</c>: <i>"the cost function used for routing must be
/// the same quantity used to judge trip failure, and the same quantity shown to the player. SC4
/// routed on distance while the player was scored on time, and the traffic system became
/// unlearnable as a result."</i> The Commute Budget is drawn as a wedge on the sun arc
/// (<c>01 §7</c>), so the quantity is time and a denominator that routes on distance is measuring
/// the SC4 failure with a stopwatch.
/// </para>
/// <para>
/// <b>Which leaves the question the plan did not anticipate: time in what unit?</b> <c>02 §2</c> is
/// categorical — <i>"the core holds exactly two quantities relating to time and space: an integer
/// Tick counter and integer Tile coordinates. There are no seconds in the library and no
/// metres."</i> But a Tick is <c>1/8192</c> of a Day, and R2a's own arithmetic in
/// <c>plans/0010</c> puts a vehicle at <b>about one Segment per Tick</b>. So a cost accumulated in
/// whole Ticks gives almost every Segment a cost of 1, and A* on that graph minimises
/// <b>hop count</b>, not time. It would return a different path from the one the design intends
/// and would do it while appearing to route on time.
/// </para>
/// <para>
/// <b>So the cost is sub-Tick, and this spike uses Q16.16 Ticks.</b> Addition is exact, so a path
/// cost is the exact sum of its Segments' costs with no accumulation error; the resolution is
/// <c>1/65536</c> of a Tick, four orders finer than the difference between two candidate routes;
/// and the range, ±32,768 Ticks, is four Days against a Commute Budget of order a hundred Ticks.
/// </para>
/// <para>
/// <b>This is a spike-local extension of the format and R7 owes a verdict on it.</b> <c>05 §121</c>
/// says <i>"Q16.16 is for sub-Tile positions and nothing else"</i>, and sub-Tick time is not a
/// sub-Tile position. The alternative — an integer count of some fixed fraction of a Tick — is the
/// same representation wearing a different name, so nothing about the measurement changes either
/// way. What changes is whether the core acquires a second Q16.16 meaning, and that is a decision
/// for the corpus rather than for a benchmark.
/// </para>
/// <para>
/// <b>Metres and seconds appear in this file's comments only.</b> They are the two exchange rates
/// <c>02 §2</c> names as living outside the simulation, and they are written down here so the
/// speeds below can be checked against reality — which <c>02 §10</c> requires of any authored
/// number. Nothing reads them.
/// </para>
/// </remarks>
internal static class Units
{
    /// <summary>Tiles across the map. <c>CLAUDE.md</c>'s constant table, 4096² Tiles.</summary>
    public const int MapTiles = 4096;

    /// <summary>Tiles on a side of a Cell. A frozen design constant (<c>CONTEXT.md</c> → Cell).</summary>
    public const int CellTiles = 32;

    /// <summary>
    /// Tiles on a side of a Chunk. <b>Provisional, and R3 is the task that must not forget it is.</b>
    /// </summary>
    /// <remarks>
    /// <c>plans/0002</c> records <i>"Phase 1 proceeds at Chunk = Cell, provisional"</i>, and
    /// <c>adr/0040</c> is what makes that pin harmless here: the pathfinding cluster is a whole
    /// number of Chunks and is sized independently, so R3 sweeps <b>Chunks per cluster</b> and the
    /// Chunk's own value only sets the granularity of the rungs, never the answer. If the Chunk
    /// later doubles, every rung of this sweep still exists — it is reached at half the multiple.
    /// </remarks>
    public const int ChunkTiles = CellTiles;

    /// <summary>Ticks in a Day. <c>CLAUDE.md</c>'s constant table.</summary>
    public const int TicksPerDay = 8192;

    // The two exchange rates, for checking the speeds below against reality and for nothing else.
    // A Tile is ~4 m (05 §26: "268 km² (4096² Tiles @ ~4 m)"), so the map is 16.4 km across.
    // A Day is 86,400 s over 8,192 Ticks, so a Tick is ~10.55 s of in-world time.
    //
    // The conversion is exact and pleasingly round, which is worth stating because it means these
    // constants carry no rounding of their own:
    //
    //   Tiles/Tick  = (km/h) × (1000/3600) m/s × 10.546875 s/Tick ÷ 4 m/Tile
    //               = (km/h) × 0.732421875
    //   Q16.16      = (km/h) × 0.732421875 × 65536
    //               = (km/h) × 48000        exactly
    private const int PerKilometrePerHour = 48_000;

    /// <summary>
    /// A Street's free-flow speed, Q16.16 Tiles/Tick. 50 km/h → 36.62 Tiles/Tick.
    /// </summary>
    public const int StreetFreeFlow = 50 * PerKilometrePerHour;

    /// <summary>
    /// An Arterial's free-flow speed, Q16.16 Tiles/Tick. 90 km/h → 65.92 Tiles/Tick.
    /// </summary>
    public const int ArterialFreeFlow = 90 * PerKilometrePerHour;

    /// <summary>
    /// A foot-only Segment's free-flow speed, Q16.16 Tiles/Tick. Walking pace, 5 km/h →
    /// 3.66 Tiles/Tick, which is also the ceiling on the foot traversal of any Segment.
    /// </summary>
    public const int WalkFreeFlow = 5 * PerKilometrePerHour;

    /// <summary>
    /// Q16.16 vehicles per Tick, per vehicle per hour. The same exact conversion as the speeds:
    /// <c>(veh/h) × 10.546875/3600 × 65536 = (veh/h) × 192</c>, exactly.
    /// </summary>
    public const int PerVehiclePerHour = 192;

    /// <summary>
    /// The traversal time of a run of road, Q16.16 Ticks, at a given free-flow speed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A Segment has one free-flow speed and two modes traverse it at different rates</b>, which
    /// one column cannot express. <c>CONTEXT.md</c> → Segment lists free-flow speed among the
    /// attributes a Segment carries, singular, and <c>CONTEXT.md</c> → Road Graph insists on
    /// <i>one graph with mode masks</i> rather than two networks — so the resolution cannot be a
    /// second speed column without contradicting one or the other.
    /// </para>
    /// <para>
    /// It is instead <c>min(the mode's own top speed, the road's free-flow speed)</c>: a pedestrian
    /// walks at walking pace on a boulevard and on a lane alike, and a car is held to the road. The
    /// mode's ceiling is applied by the caller, which is why this takes a speed rather than a mode.
    /// </para>
    /// </remarks>
    public static int TraversalTicks(int lengthTiles, int speedTilesPerTick) =>
        Fixed.Div(Fixed.FromInt(lengthTiles), speedTilesPerTick);

    /// <summary>The smaller of two Q16.16 speeds. Named because the choice above is load-bearing.</summary>
    public static int SlowerOf(int a, int b) => a < b ? a : b;
}
