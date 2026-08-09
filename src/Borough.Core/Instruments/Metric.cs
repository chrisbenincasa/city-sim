namespace Borough.Core.Instruments;

/// <summary>
/// The counters a table exposes to the <see cref="Census"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three counters rather than one, because they fail differently.</b> A table that is leaking and
/// a table that is merely busy both show a rising <see cref="Live"/>, and nothing about that number
/// alone separates them. The pair that separates them is <see cref="Live"/> against
/// <see cref="Slots"/>: slots are only ever allocated when the free list is empty, so
/// <em>Slots rising while Live is flat</em> is a free list that is not being returned to — which is
/// <c>adr/0006</c>'s failure with the population held constant, and is invisible in a row count.
/// </para>
/// <para>
/// <b><see cref="Capacity"/> is the third because it is the one that costs memory.</b> Slots are a
/// high-water mark and capacity is what was actually allocated to hold them; they diverge because
/// growth is geometric. A run whose capacity climbs while its slots do not has an array being grown
/// by something other than demand.
/// </para>
/// </remarks>
public enum CensusCounter : byte
{
    /// <summary>Rows in use. The city's size, and on its own not evidence of anything.</summary>
    Live,

    /// <summary>
    /// Slots ever allocated — the high-water mark of simultaneous demand.
    /// </summary>
    /// <remarks>
    /// Rises only when a row is created and the free list is empty, so it is flat across any
    /// create-and-destroy cycle that recycles. This is the counter <c>adr/0006</c> is about.
    /// </remarks>
    Slots,

    /// <summary>The length of the backing arrays. What the table costs, as opposed to what it holds.</summary>
    Capacity,
}

/// <summary>
/// The counters the Rule engine exposes to the <see cref="Census"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>02 §4</c> names two of these and then argues that the interesting quantity is a third.</b>
/// The section calls for <em>Rule evaluations per Tick</em> and <em>walked chain depth</em>, and in
/// the same paragraph says the cost driver is <em>how often a Bin crosses the supplied/short
/// boundary</em> — which neither of them measures. <see cref="Due"/> is not that third quantity, but
/// it is what makes the first two readable at all: without it a rising evaluation count could equally
/// be a bigger city or a more unstable one, and those mean opposite things.
/// </para>
/// <para>
/// <b>The pair that separates them is <see cref="Due"/> against <see cref="Evaluations"/>.</b> Due
/// rows are the scheduled load — what the Event Wheel handed over — and evaluations are what was
/// actually spent, so <em>evaluations − due</em> is the whole cost of chain walking and Phase 3's
/// re-check. This is the same discrimination <see cref="CensusCounter"/> makes with
/// <see cref="CensusCounter.Live"/> against <see cref="CensusCounter.Slots"/>, and it is here for the
/// same reason: one number rising is not evidence of which thing rose.
/// </para>
/// <para>
/// <b>All three are flows, which is what separates them from every table counter.</b> A table counter
/// is a level read at an instant and is the same number whether it is read every Tick or every
/// thousand. These accumulate, so a reading is <em>what happened since the last one</em> and the
/// reading drains it — see <see cref="Aggregate"/>.
/// </para>
/// </remarks>
public enum RuleCounter : byte
{
    /// <summary>
    /// Rule Instances taken off the Event Wheel — the scheduled load, before anything is spent on it.
    /// </summary>
    /// <remarks>
    /// A failed Rule does not re-arm (<c>02 §4.1</c>), so this falls as a District starves and rises
    /// as it recovers. It is the denominator the other two are read against.
    /// </remarks>
    Due,

    /// <summary>
    /// Rules evaluated — <c>02 §4</c>'s first counter, counting every evaluation rather than every
    /// due row.
    /// </summary>
    /// <remarks>
    /// <b>One per <c>Check</c>, which is what the section's own note asked for</b>: a head, every
    /// non-terminal link of a chain walked below it, and Phase 3's re-check are each an evaluation,
    /// because each costs one. Counting due rows instead — which is what this counter did before task
    /// 9 — <em>"does not see a chain link at all"</em>, so the quantity the tripwire is stated over
    /// was the one quantity chain walking could not move.
    /// </remarks>
    Evaluations,

    /// <summary>
    /// Chain rungs descended — <c>02 §4</c>'s second counter, and a depth rather than a cost.
    /// </summary>
    /// <remarks>
    /// <b>Every rung reached, the terminal included.</b> A terminal is never evaluated (it has no term
    /// that could be short, so ordinary Rule semantics would fire it and re-arm the head on
    /// <c>rate</c>), so it adds a rung here and no evaluation there. That divergence is the point:
    /// this counter is how deep the ladder went and <see cref="Evaluations"/> is what the descent
    /// cost, and a chain that ends in a report costs less than its depth suggests.
    /// </remarks>
    ChainRungs,
}

/// <summary>
/// How a flow counter's Ticks are reduced into one reading.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two readings rather than one, because the tripwire and the bill are different questions.</b>
/// <c>02 §4</c>'s tripwire is stated per Tick — <em>chain walking fits while fewer than N evaluations
/// occur per Tick</em> — and a mean over a census interval cannot state it: burstiness is
/// <em>authored</em> under this design, so the interval containing the burst and the interval either
/// side of it are the same mean. <see cref="Sum"/> is what a run cost; <see cref="Peak"/> is what the
/// worst Tick in it cost, and only the second can be held against a budget.
/// </para>
/// <para>
/// <b>Neither loses the Ticks between readings, which is why there is one census cadence and not
/// two.</b> The accumulator behind these sees every Tick even though it is read every sixty-fourth,
/// so a slow reading discards only the <em>shape</em> of the interval — and <c>sum ÷ cadence</c>
/// against <see cref="Peak"/> recovers the part of that shape anybody reads a census for, which is
/// whether the load is a plateau or a spike. A second cadence would buy a finer view of something
/// already fully observed, at the cost of the property that a reading and a State Hash from the same
/// Tick are two views of one moment.
/// </para>
/// </remarks>
public enum Aggregate : byte
{
    /// <summary>The total over the interval since the previous reading. Divided by the cadence, a mean rate.</summary>
    Sum,

    /// <summary>The largest single Tick in that interval. The figure a per-Tick tripwire is read against.</summary>
    Peak,
}

/// <summary>Which family of thing a <see cref="Metric"/> names.</summary>
/// <remarks>
/// <b>Two families rather than one, because a level and a flow are not the same kind of number.</b>
/// A table counter is read at an instant and means the same thing at any cadence; a Rule counter is
/// accumulated between readings and drained by one. Filing the second as a synthetic table would have
/// let <c>live</c>, <c>slots</c> and <c>capacity</c> name three things that are none of those — the
/// drift <see cref="Metric"/> exists to prevent.
/// </remarks>
public enum MetricSource : byte
{
    /// <summary>One counter of one table: a collection's size, sampled.</summary>
    Table,

    /// <summary>One counter of the Rule engine: a flow, accumulated and drained.</summary>
    Rules,
}

/// <summary>
/// What a <see cref="Census"/> series is a series <em>of</em>: one counter of one table, or one
/// counter of the Rule engine.
/// </summary>
/// <remarks>
/// <para>
/// <b>An id and a number, never a name.</b> <c>adr/0002</c> gives the core ids and gives the shell
/// every string a human reads, and a metric identified by a string would have put the vocabulary of
/// the panel inside the simulation. <see cref="Table"/> is the index into <c>World.Tables</c>, which
/// is declaration order — the same order the State Hash folds tables in, so a metric means the same
/// thing to a trace as it does to a hash.
/// </para>
/// <para>
/// <b>A table metric names a table rather than a collection because the intrusive-list pattern makes
/// those the same thing.</b> Every variable-length structure in the core is an intrusive index list
/// whose nodes are rows, so the total length of every list over a table is that table's live row
/// count and there is no separate collection to sample. A list whose nodes are live rows missing from
/// it is not a magnitude problem but a correctness one, and belongs to the invariant tiers rather
/// than here.
/// </para>
/// <para>
/// <b>The accessors for the family a metric is not are errors rather than defaults.</b> A
/// <see cref="Table"/> of zero on a Rule metric would be a valid table index and would read as the
/// first table, so the wrong question gets an answer that looks right. Constructed only through the
/// two factories, for the same reason.
/// </para>
/// </remarks>
public readonly record struct Metric
{
    private readonly int _table;
    private readonly byte _counter;
    private readonly byte _aggregate;

    private Metric(MetricSource source, int table, byte counter, byte aggregate)
    {
        Source = source;
        _table = table;
        _counter = counter;
        _aggregate = aggregate;
    }

    /// <summary>Which family this names.</summary>
    public MetricSource Source { get; }

    /// <summary>Index into <c>World.Tables</c>, in declaration order.</summary>
    public int Table => Source is MetricSource.Table
        ? _table
        : throw new InvalidOperationException($"a {Source} metric does not name a table.");

    /// <summary>Which of the table's counters.</summary>
    public CensusCounter Counter => Source is MetricSource.Table
        ? (CensusCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a table counter.");

    /// <summary>Which of the Rule engine's counters.</summary>
    public RuleCounter RuleCounter => Source is MetricSource.Rules
        ? (RuleCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a Rule counter.");

    /// <summary>How the counter's Ticks are reduced into one reading.</summary>
    /// <remarks>
    /// Meaningful only for a flow. A table counter is read at an instant, so there is nothing over
    /// which to take a sum or a peak and asking is a mistake rather than a default.
    /// </remarks>
    public Aggregate Aggregate => Source is MetricSource.Rules
        ? (Aggregate)_aggregate
        : throw new InvalidOperationException($"a {Source} metric is a level and is not aggregated.");

    /// <summary>One counter of one table.</summary>
    /// <param name="table">Index into <c>World.Tables</c>, in declaration order.</param>
    /// <param name="counter">Which of the table's counters.</param>
    public static Metric Of(int table, CensusCounter counter) =>
        new(MetricSource.Table, table, (byte)counter, 0);

    /// <summary>One counter of the Rule engine, under one reduction.</summary>
    /// <param name="counter">Which of the engine's counters.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(RuleCounter counter, Aggregate aggregate) =>
        new(MetricSource.Rules, 0, (byte)counter, (byte)aggregate);
}
