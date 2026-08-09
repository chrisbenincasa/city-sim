using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Instruments;

/// <summary>
/// A periodic reading of every collection's size and of what the Rule engine did, kept in a ring, and
/// the history behind <c>series(metric, window)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>It reads two kinds of number and the difference is not cosmetic.</b> A table counter is a
/// <em>level</em>: read it at any cadence and it means the same thing, because it is the size of
/// something that exists. A Rule counter is a <em>flow</em>: it has no value at an instant, only over
/// an interval, so it is accumulated between readings and a reading drains it. Sampling a flow the
/// way a level is sampled would report one Tick in sixty-four of a quantity <c>02 §4</c> makes
/// deliberately bursty — see <see cref="Aggregate"/> for why that matters and what replaces it.
/// </para>
/// <para>
/// <b>This is the instrument <c>adr/0006</c> needs and did not have.</b> <em>No collection in the
/// simulation may grow as a function of elapsed game time</em> is a claim about a run rather than
/// about a Tick: the failure is invisible at design time and takes hours of play to manifest, so
/// nothing that looks at one moment can see it. What can see it is a series.
/// </para>
/// <para>
/// <b>The ring is finite by construction, because the alternative would be the defect it detects.</b>
/// A census that appended a reading per cadence for the length of a run would be a collection growing
/// as a function of elapsed game time — <c>adr/0006</c> exactly, in the instrument written to catch
/// it. So the oldest reading is overwritten, and that overwriting is the sink. The cost is that a
/// window can outrun the history, which <see cref="Series.Complete"/> reports rather than hides.
/// </para>
/// <para>
/// <b>It is owned by whoever runs the session, never by the World.</b> The invariant registry sits on
/// the World because its claims are about that world and hold for one built by hand in a test. A
/// census is the opposite: it is a record of a <em>run</em>, and a world has no history until
/// something steps it. Putting it on the World would also have made it state — something the State
/// Hash and the save would each need an answer for — and an instrument that changes the hash is an
/// instrument that changes the city.
/// </para>
/// <para>
/// <b>Nothing here asserts.</b> Whether a series trends upward is a question for a caller with a
/// definition of steady state, and the corpus does not have one before the world has churn in it. The
/// census's job is to make the question answerable.
/// </para>
/// </remarks>
public sealed class Census
{
    /// <summary>The members of <see cref="CensusCounter"/>, which the layout below is a multiple of.</summary>
    private const int CountersPerTable = 3;

    /// <summary>The members of <see cref="RuleCounter"/>.</summary>
    private const int RuleCounters = 3;

    /// <summary>The members of <see cref="Aggregate"/>: a flow is read twice, as a sum and as a peak.</summary>
    private const int AggregatesPerRuleCounter = 2;

    /// <summary>
    /// The Rule engine's share of one reading. Fixed, because the engine's counters are declared in
    /// the core rather than derived from the world's shape.
    /// </summary>
    private const int RuleMetrics = RuleCounters * AggregatesPerRuleCounter;

    /// <summary>
    /// Readings held before the oldest is overwritten.
    /// </summary>
    /// <remarks>
    /// <b>Not a tuning number in <c>adr/0015</c>'s sense</b> — no designer would want to change it,
    /// because it cannot change the city. Its only job is to be finite, and the figure is chosen to
    /// cover a long run at a sane cadence: 1,024 readings is the whole of a 100,000-Tick run sampled
    /// every 64 Ticks.
    /// </remarks>
    public const int DefaultCapacity = 1_024;

    private readonly int _tables;

    /// <summary>Where the Rule engine's metrics begin, which is where the tables' end.</summary>
    private readonly int _ruleBase;

    private readonly int _metrics;
    private readonly int _capacity;
    private readonly ulong[] _ticks;

    /// <summary>Readings, sample-major: <c>_values[(sample * _metrics) + metric]</c>.</summary>
    /// <remarks>
    /// Sample-major because a reading is written all at once and a series is read once per run;
    /// writing is the operation on the cadence, so it is the one that gets the contiguous span.
    /// </remarks>
    private readonly long[] _values;

    private int _next;
    private int _count;
    private ulong _taken;

    /// <param name="world">The world whose shape fixes the metrics. Not retained.</param>
    /// <param name="capacity">Readings held. See <see cref="DefaultCapacity"/>.</param>
    /// <remarks>
    /// <b>The world is read for its table count and then let go.</b> A census that held the world
    /// would keep it alive for as long as anything held a series, and it needs nothing from it
    /// between readings.
    /// </remarks>
    public Census(World world, int capacity = DefaultCapacity)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _tables = world.Tables.Length;
        _ruleBase = _tables * CountersPerTable;
        _metrics = _ruleBase + RuleMetrics;
        _capacity = capacity;
        _ticks = new ulong[capacity];
        _values = new long[capacity * _metrics];
    }

    /// <summary>Readings held before the oldest is overwritten.</summary>
    public int Capacity => _capacity;

    /// <summary>Readings currently held, which saturates at <see cref="Capacity"/>.</summary>
    public int Count => _count;

    /// <summary>
    /// Readings ever taken, including those since overwritten.
    /// </summary>
    /// <remarks>
    /// A counter rather than a collection, so it is not what <c>adr/0006</c> is about. It is here
    /// because it is what distinguishes a short run from a long one whose history has been eaten,
    /// which is the whole of <see cref="Series.Complete"/>.
    /// </remarks>
    public ulong Taken => _taken;

    /// <summary>The number of tables, and so the range of <see cref="Metric.Table"/>.</summary>
    public int Tables => _tables;

    /// <summary>
    /// Takes one reading of every metric: every table's size, and the Rule engine's interval since
    /// the previous reading.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The caller owns the cadence, for the reason the State Hash's caller does.</b> <em>Due</em>
    /// is a property of the run rather than of the simulation, and sharing the hash's cadence is what
    /// makes a reading and a hash from the same Tick two views of one moment.
    /// </para>
    /// <para>
    /// <b>It takes a Simulation rather than a World because half of a reading is a run's and not a
    /// world's.</b> A table's size can be read off a world nobody ever stepped; what the Rule engine
    /// did between two readings cannot. The Tick comes from the same place for the same reason — a
    /// reading stamped with a Tick the simulation disagreed with would be a series about nothing.
    /// </para>
    /// <para>
    /// <b>The reading <em>drains</em> the engine's counters</b>, so two readings of one Tick are not
    /// two readings: the second sees an empty interval. That is the flow half behaving as a flow, and
    /// it is what stops the counters becoming the unbounded accumulation they exist to watch for.
    /// </para>
    /// </remarks>
    /// <param name="simulation">The run to read: its world, its Tick, and its Rule engine.</param>
    public void Observe(Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        Observe(simulation.World, simulation.Tick, simulation.Rules.Drain());
    }

    /// <summary>
    /// Takes one reading of every metric, against a world and a Rule engine's interval stated
    /// separately.
    /// </summary>
    /// <remarks>
    /// <b>The explicit form, and it exists because a world is not always a run.</b> A world built by
    /// hand in a test has a shape to read and no history to have run, and passing
    /// <c>default</c> here says so: no Rule was due, none was evaluated, no chain was walked. That is
    /// a true reading rather than a placeholder — which is the distinction that decides whether a zero
    /// may be written at all.
    /// </remarks>
    /// <param name="world">The world to read. Must have the shape the census was built against.</param>
    /// <param name="tick">The Tick to stamp the reading with.</param>
    /// <param name="activity">The Rule engine's interval since the previous reading, already drained.</param>
    public void Observe(World world, Ticks tick, RuleActivity activity)
    {
        ArgumentNullException.ThrowIfNull(world);

        ReadOnlySpan<Rows> tables = world.Tables;

        if (tables.Length != _tables)
        {
            throw new ArgumentException(
                $"census was built for {_tables} tables and this world has {tables.Length}; "
                + "a series across two worlds is not a series.",
                nameof(world));
        }

        int at = _next * _metrics;

        for (int i = 0; i < tables.Length; i++)
        {
            Rows table = tables[i];
            int slot = at + (i * CountersPerTable);

            _values[slot + (int)CensusCounter.Live] = table.LiveCount;
            _values[slot + (int)CensusCounter.Slots] = table.SlotCount;
            _values[slot + (int)CensusCounter.Capacity] = table.Capacity;
        }

        Write(_values, at + _ruleBase, RuleCounter.Due, activity.Due);
        Write(_values, at + _ruleBase, RuleCounter.Evaluations, activity.Evaluations);
        Write(_values, at + _ruleBase, RuleCounter.ChainRungs, activity.ChainRungs);

        _ticks[_next] = tick.Raw;
        _next = (_next + 1) % _capacity;
        _taken++;

        if (_count < _capacity)
        {
            _count++;
        }

        static void Write(long[] values, int at, RuleCounter counter, RuleFlow flow)
        {
            int slot = at + ((int)counter * AggregatesPerRuleCounter);

            values[slot + (int)Aggregate.Sum] = flow.Sum;
            values[slot + (int)Aggregate.Peak] = flow.Peak;
        }
    }

    /// <summary>
    /// <c>05 §2</c>'s <c>series(metric, window)</c>: one metric's readings over the last
    /// <paramref name="window"/> Ticks.
    /// </summary>
    /// <remarks>
    /// <b>The window is measured back from the newest reading, not from the current Tick.</b> The
    /// census does not know what Tick it is — it knows what Tick it last looked — and answering
    /// against a Tick nobody sampled would silently shorten every window by the cadence.
    /// </remarks>
    /// <param name="metric">Which table's which counter.</param>
    /// <param name="window">How far back to reach, in Ticks.</param>
    /// <returns>The readings, oldest first, and whether the window was covered in full.</returns>
    public Series Series(Metric metric, Ticks window)
    {
        int offset = Offset(metric);

        if (_count == 0)
        {
            return new Series(metric, [], complete: true);
        }

        int oldest = _count < _capacity ? 0 : _next;
        ulong newest = _ticks[(_next + _capacity - 1) % _capacity];
        ulong floor = newest >= window.Raw ? newest - window.Raw : 0;

        int first = 0;

        while (first < _count && _ticks[(oldest + first) % _capacity] < floor)
        {
            first++;
        }

        int length = _count - first;
        var samples = new CensusSample[length];

        for (int k = 0; k < length; k++)
        {
            int slot = (oldest + first + k) % _capacity;
            samples[k] = new CensusSample(new Ticks(_ticks[slot]), _values[(slot * _metrics) + offset]);
        }

        // Covered in full either because a reading older than the window survives — so the window's
        // start is inside the history — or because nothing has been overwritten yet.
        bool complete = first > 0 || _taken <= (ulong)_capacity;

        return new Series(metric, samples, complete);
    }

    /// <summary>
    /// Where one metric sits in a reading: the tables in declaration order, then the Rule engine.
    /// </summary>
    /// <remarks>
    /// <b>The Rule engine's block is last and fixed in size</b>, so adding a table moves nothing about
    /// how a Rule metric is addressed and adding a Rule counter moves no table's. The two families
    /// grow on different schedules — one with the world, one with the core — and interleaving them
    /// would have coupled those.
    /// </remarks>
    private int Offset(Metric metric)
    {
        if (metric.Source is MetricSource.Table)
        {
            if ((uint)metric.Table >= (uint)_tables)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(metric), metric.Table, $"this census has {_tables} tables.");
            }

            if (metric.Counter is not (CensusCounter.Live or CensusCounter.Slots or CensusCounter.Capacity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(metric), metric.Counter, "not a counter this census reads.");
            }

            return (metric.Table * CountersPerTable) + (int)metric.Counter;
        }

        if (metric.RuleCounter is not (RuleCounter.Due or RuleCounter.Evaluations or RuleCounter.ChainRungs))
        {
            throw new ArgumentOutOfRangeException(
                nameof(metric), metric.RuleCounter, "not a Rule counter this census reads.");
        }

        if (metric.Aggregate is not (Aggregate.Sum or Aggregate.Peak))
        {
            throw new ArgumentOutOfRangeException(
                nameof(metric), metric.Aggregate, "not a reduction this census takes.");
        }

        return _ruleBase + ((int)metric.RuleCounter * AggregatesPerRuleCounter) + (int)metric.Aggregate;
    }
}
