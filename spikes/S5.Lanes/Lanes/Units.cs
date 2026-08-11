using Borough.Core.Arithmetic;

namespace S5.Lanes.Lanes;

/// <summary>
/// The unit block, and every constant in it is derived from a document rather than chosen here.
/// </summary>
/// <remarks>
/// <para>
/// <b>S5 must not choose a hash-bearing number</b> (<c>adr/0052</c>), and the IDM's three tuning
/// parameters are already filed as unset in <c>plans/0002</c> §D2. So this file states a
/// <em>fixture</em> parameter set with its provenance beside each value, and the spike reports a
/// sensitivity row so that a reader can see whether the answer is a property of the structure or of
/// the tuning. If the cost moves with the parameters, the number S5 publishes is worth much less
/// than it looks.
/// </para>
/// <para>
/// <b>The Tick is not a free parameter and this is the part everybody gets wrong.</b>
/// <c>adr/0019</c> derives <c>TICKS_PER_DAY = 8192</c> <em>from</em> car-following resolution and
/// prints the row this file uses: at roughly 65 km/h a vehicle covers <b>4.2 m per Tick</b>, which
/// is 12% of the ~36 m safe following distance, and the ADR names Treiber's Δt ≤ 0.5 s as the
/// constraint being satisfied. So one IDM integration step per Tick is the design, and there is no
/// substepping to price. A reader arriving at 8192 Ticks against a 86,400-second day and concluding
/// that a Tick is 10.5 simulated seconds has read the wrong ratio: a Day here is a simulation
/// object of 8192 Ticks, not a converted quantity, and the seconds come from the speed ladder.
/// </para>
/// <para>
/// <b>Space is Tiles and a Tile is 4 m</b>, from the Cell being 32×32 Tiles at ≈128 m
/// (<c>CLAUDE.md</c> → Constants). Everything below is Q16.16 Tiles and Tiles per Tick, because
/// <c>adr/0019</c> is explicit that the simulation contains no metres and no seconds.
/// </para>
/// </remarks>
internal static class Units
{
    /// <summary>Metres in a Tile. Cell = 32×32 Tiles ≈ 128 m.</summary>
    public const int MetresPerTile = 4;

    /// <summary>
    /// A Segment is 32 Tiles — 128 m — which S2 R0 measured and <c>CONTEXT.md</c> → Segment calls
    /// <i>"roughly a block-length link"</i>.
    /// </summary>
    public const int SegmentLengthTiles = 32;

    /// <summary><c>CONTEXT.md</c> → Segment: <i>"~30,000 Segments … about four Lanes each"</i>.</summary>
    public const int LanesPerSegment = 4;

    /// <summary>
    /// A Q16.16 value from an exact rational. <c>Fixed.FromInt</c> cannot be used to build these:
    /// its argument is bounded by ±32,768, and every conversion below has a denominator larger
    /// than that. Scaling first and dividing once is also the only spelling that keeps the
    /// rounding stated — <c>RoundDiv</c> rather than C#'s truncation toward zero.
    /// </summary>
    private static int Q(long numerator, long denominator) =>
        (int)IntegerMath.RoundDiv(numerator << 16, denominator);

    /// <summary>
    /// Free-flow speed, Q16.16 Tiles per Tick. <c>adr/0019</c>'s own row: 4.2 m per Tick at
    /// 8192 Ticks per Day, which is 1.05 Tiles.
    /// </summary>
    public static readonly int FreeFlowSpeed = Q(105, 100);

    /// <summary>
    /// Vehicle length, Q16.16 Tiles. 5 m, the standard IDM car. Enters the gap and therefore the
    /// jam spacing, which is what sizes a queue.
    /// </summary>
    public static readonly int VehicleLength = Q(5, MetresPerTile);

    /// <summary>
    /// IDM <c>s0</c>, the minimum bumper-to-bumper gap. 2 m — Treiber's highway value.
    /// </summary>
    public static readonly int MinimumGap = Q(2, MetresPerTile);

    /// <summary>
    /// IDM <c>T</c>, the desired time headway, in <b>Ticks</b>. 1.5 s at 0.2326 simulated seconds
    /// per Tick — that quotient being 4.2 m per Tick divided by 18.06 m/s — is 6.45 Ticks.
    /// </summary>
    public static readonly int DesiredHeadwayTicks = Q(645, 100);

    /// <summary>
    /// IDM <c>a</c>, maximum acceleration, Q16.16 Tiles per Tick squared. 1.4 m/s² × 0.2326² s²
    /// per Tick² ÷ 4 m per Tile = 0.01893.
    /// </summary>
    public static readonly int MaxAcceleration = Q(1893, 100_000);

    /// <summary>
    /// IDM <c>b</c>, comfortable deceleration. 2.0 m/s² by the same conversion = 0.02705.
    /// </summary>
    public static readonly int ComfortableBraking = Q(2705, 100_000);

    /// <summary>
    /// <c>2√(ab)</c>, the IDM's interaction denominator. <b>It is a constant of the parameter set,
    /// so no square root is ever evaluated per Vehicle</b> — which is the one place the integer
    /// transplant could have been expensive and is not. 2 × √(0.01893 × 0.02705) = 0.04526.
    /// </summary>
    public static readonly int TwoRootAb = Q(4526, 100_000);

    /// <summary>
    /// <c>1 / 2√(ab)</c>, Q16.16, so that the interaction term can be a multiply rather than a
    /// divide. Measured against the dividing form in L1 rather than assumed to be better.
    /// </summary>
    public static readonly int InverseTwoRootAb = Fixed.Div(Fixed.One, TwoRootAb);

    /// <summary>
    /// The gap floor, Q16.16 Tiles. Below this the interaction term is evaluated at this value
    /// instead, which bounds <c>s*/s</c> and therefore the squaring.
    /// </summary>
    /// <remarks>
    /// A real implementation needs this for the same reason: <c>s</c> appears in a denominator and
    /// a queue that has just been materialised can hold two Vehicles at an overlapping position.
    /// It is stated as a constant rather than hidden in a clamp because it is a modelling choice
    /// and a reader is entitled to see it. 0.05 Tiles is 20 cm.
    /// </remarks>
    public static readonly int GapFloor = Q(5, 100);

    /// <summary>
    /// The cap on <c>s*/s</c> before it is squared, Q16.16.
    /// </summary>
    /// <remarks>
    /// <b>Q16.16 forces this and a float engine does not, which is a real difference between our
    /// transplant and Citybound's original.</b> <c>Fixed.Mul</c> narrows to ±32,768 under
    /// <c>checked</c>, so squaring a ratio above 181 throws rather than saturating. The physical
    /// content is the same either way — the IDM's braking term is unbounded and every
    /// implementation clamps the resulting acceleration — but here the clamp is a correctness
    /// requirement rather than a nicety, and it is stated where it can be argued with. 64 gives a
    /// braking term of 4,096·a, four orders of magnitude beyond comfortable braking.
    /// </remarks>
    public static readonly int MaxGapRatio = Fixed.FromInt(64);

    /// <summary>The Tick budget at 4× speed, in nanoseconds. <c>CLAUDE.md</c> → Constants.</summary>
    public const long TickBudgetNanoseconds = 15_600_000;

    /// <summary>
    /// Jam spacing, Q16.16 Tiles: a stopped Vehicle occupies its own length plus <c>s0</c>.
    /// 5 m + 2 m = 7 m = 1.75 Tiles, so a 32-Tile Lane holds 18 Vehicles at a standstill.
    /// </summary>
    public static int JamSpacing => VehicleLength + MinimumGap;

    /// <summary>
    /// Vehicles a single Lane of a Segment holds at a standstill: 18.
    /// </summary>
    public static int VehiclesPerLaneAtJam =>
        Fixed.ToIntFloor(Fixed.Div(Fixed.FromInt(SegmentLengthTiles), JamSpacing));
}
