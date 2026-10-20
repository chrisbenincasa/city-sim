using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>One District's network for one delivered Utility. Row type of the table below.</summary>
public readonly struct UtilityFlow;

/// <summary>
/// What each District's network for a delivered Utility made, was asked for, and can deliver.
/// </summary>
/// <remarks>
/// <para>
/// One row per delivered-Utility Pool row, opened by <c>World.FitDistrictPools</c> and freed with the
/// Pool row in <c>World.RetirePool</c>. <b>No stock stands in these rows and none stands in the
/// Pool's Bin</b> — <c>adr/0139</c> makes a Pool a market rather than a store. A Utility declaring
/// <c>[[resource]] storage = "reserve"</c> keeps its carry-over in the Bins of the Buildings that
/// hold it, and <see cref="FromReserve"/> and <see cref="ToReserve"/> are what the network records of
/// the two directions.
/// </para>
/// <para>
/// Saved rather than derived because a Day's production is history. Nothing records which Ticks a
/// plant's Rule fired on, so a reload could not recompute what the network carried.
/// </para>
/// <para>
/// <see cref="Produced"/> and <see cref="Demanded"/> accumulate through the Day;
/// <c>World.SettleUtilityNetworks</c> reads them at the next Day boundary, writes
/// <see cref="Delivered"/> for the Day that follows, copies the pair into
/// <see cref="Supplied"/>/<see cref="Asked"/> beside what was actually served, and zeroes them.
/// Supply a reserve cannot absorb is lost rather than carried, and a Utility with no reserve loses
/// all of it.
/// </para>
/// </remarks>
[Table]
public sealed class UtilityFlowTable
{
    private readonly Rows<UtilityFlow> _rows;

    /// <param name="capacity">Initial row count — one per delivered Utility per District.</param>
    /// <param name="pools">The table <see cref="DistrictPool"/> handles are resolved against.</param>
    public UtilityFlowTable(int capacity, DistrictPoolTable pools)
    {
        ArgumentNullException.ThrowIfNull(pools);

        _rows = new Rows<UtilityFlow>("utility_flow", capacity, Buffering.OneCopy);

        // Touch.Cold throughout: the two counters are written by a firing and the rest once a Day,
        // and nothing reads a row on a Tick that neither fires nor settles.
        Pool = _rows.SavedHandle("pool", pools.Rows, Touch.Cold);
        Produced = _rows.Saved<long>("produced", Touch.Cold);
        Demanded = _rows.Saved<long>("demanded", Touch.Cold);
        Delivered = _rows.Saved<Ratio>("delivered", Touch.Cold);
        Supplied = _rows.Saved<long>("supplied", Touch.Cold);
        Asked = _rows.Saved<long>("asked", Touch.Cold);
        Received = _rows.Saved<long>("received", Touch.Cold);
        FromReserve = _rows.Saved<long>("from_reserve", Touch.Cold);
        ToReserve = _rows.Saved<long>("to_reserve", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<UtilityFlow> Rows => _rows;

    /// <summary>
    /// The largest amount a Day's counter holds. Every total the settle derives from one saturates
    /// here as well.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A stated ceiling rather than a wrap.</b> A Ruleset may legally declare an output of
    /// <c>int.MaxValue</c> at an apply count of <c>int.MaxValue</c>, which posts 4.6 × 10¹⁸ in one
    /// firing — so a third firing used to turn <see cref="Produced"/> negative, and a negative
    /// production is a city that generates power by asking for it.
    /// </para>
    /// <para>
    /// <b>A quintillion, which leaves the max-flow its headroom.</b> An undirected link holds its
    /// capacity in both residual directions and the pair sums to twice the capacity, so every
    /// quantity the settle puts in that matrix needs a factor of two inside <c>long</c> — and 2 ×
    /// 10¹⁸ is a fifth of <see cref="long.MaxValue"/>. A network at this bound is a Ruleset defect
    /// rather than a city, and saturating is what makes it read as one.
    /// </para>
    /// </remarks>
    public const long Bound = 1_000_000_000_000_000_000;

    /// <summary>
    /// The Pool row whose District and Utility this network is. <b>The Resource is read off the
    /// Pool's Bin rather than copied here</b>, so the two cannot disagree.
    /// </summary>
    public HandleColumn<DistrictPool> Pool { get; }

    /// <summary>What plants in this District have fed the network today.</summary>
    public Column<long> Produced { get; }

    /// <summary>
    /// What consumers in this District have asked of it today, uncapped.
    /// </summary>
    /// <remarks>
    /// Posted at the count a Rule would have applied at with the Utility whole, so a brownout does
    /// not hide the demand that caused it.
    /// </remarks>
    public Column<long> Demanded { get; }

    /// <summary>
    /// The fraction of each consumer's draw this District's network delivers for the current Day, in
    /// <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// A new row opens at zero: nothing was produced yesterday, so a District gets its first Day's
    /// delivery from the settle that follows its first Day of production.
    /// </remarks>
    public Column<Ratio> Delivered { get; }

    /// <summary>Yesterday's <see cref="Produced"/>.</summary>
    public Column<long> Supplied { get; }

    /// <summary>Yesterday's <see cref="Demanded"/>.</summary>
    public Column<long> Asked { get; }

    /// <summary>
    /// What yesterday's network served its consumers — the local cover and the inflow together, less
    /// the road loss they pay first. The numerator <see cref="Delivered"/> was computed from.
    /// </summary>
    public Column<long> Received { get; }

    /// <summary>
    /// What yesterday's network in this District drew out of a reserve — its own towers first, then
    /// whatever a neighbor's exported over the links.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Not a part of <see cref="Received"/>.</b> The two overlap and neither contains the other,
    /// because road loss comes off what the network gathered before any consumer is served and nothing
    /// records whether a lost unit came out of a tower or off a plant. So a District whose towers
    /// cover only its leak reports this above zero with <see cref="Received"/> at zero, and no column
    /// says how much of what the consumers got had been stored.
    /// Zero for a Utility declaring <c>storage = "none"</c>, which keeps nothing between Days.
    /// </remarks>
    public Column<long> FromReserve { get; }

    /// <summary>
    /// What yesterday's refill added to this District's own towers — its own leftover production
    /// first, then whatever a neighbor's surplus reached over the links.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is not the level and says nothing about it.</b> The level stands in the Bins; this is
    /// one Day's inflow, and a tower full since last week reports zero here.
    /// </remarks>
    public Column<long> ToReserve { get; }

    /// <summary>
    /// <paramref name="standing"/> + <paramref name="amount"/> × <paramref name="times"/>, saturated
    /// at <see cref="Bound"/>. Every operand is a count and none may be negative.
    /// </summary>
    /// <remarks>
    /// An <c>Int128</c> intermediate, on <see cref="Arithmetic.IntegerMath.MulDivFloor"/>'s
    /// precedent. The product of two <c>long</c>s always fits one, so the saturation is decided on
    /// the true total rather than on whatever survived the multiply.
    /// </remarks>
    public static long Total(long standing, long amount, long times)
    {
        Int128 total = standing + ((Int128)amount * times);

        return total > Bound ? Bound : (long)total;
    }

    /// <summary>Adds <c>amount × count</c> to today's production, saturating at <see cref="Bound"/>.</summary>
    public void Produce(int slot, long amount, long count) =>
        Produced[slot] = Total(Produced[slot], amount, count);

    /// <summary>Adds <c>amount × count</c> to today's demand, saturating at <see cref="Bound"/>.</summary>
    public void Demand(int slot, long amount, long count) =>
        Demanded[slot] = Total(Demanded[slot], amount, count);

    /// <summary>Opens a network for one Pool row, delivering nothing until the first settle.</summary>
    public Handle<UtilityFlow> Create(Handle<DistrictPool> pool)
    {
        Handle<UtilityFlow> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Pool[slot] = pool;
        Produced[slot] = 0;
        Demanded[slot] = 0;
        Delivered[slot] = Ratio.Zero;
        Supplied[slot] = 0;
        Asked[slot] = 0;
        Received[slot] = 0;
        FromReserve[slot] = 0;
        ToReserve[slot] = 0;

        return handle;
    }
}
