using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Instruments;

/// <summary>
/// A periodic reading of every collection's size, kept in a ring, and the history behind
/// <c>series(metric, window)</c>.
/// </summary>
/// <remarks>
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
        _metrics = _tables * CountersPerTable;
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
    /// Takes one reading of every metric.
    /// </summary>
    /// <remarks>
    /// <b>The caller owns the cadence, for the reason the State Hash's caller does.</b> <em>Due</em>
    /// is a property of the run rather than of the simulation, and sharing the hash's cadence is what
    /// makes a reading and a hash from the same Tick two views of one moment.
    /// </remarks>
    /// <param name="world">The world to read. Must have the shape the census was built against.</param>
    /// <param name="tick">The Tick to stamp the reading with.</param>
    public void Observe(World world, Ticks tick)
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

        _ticks[_next] = tick.Raw;
        _next = (_next + 1) % _capacity;
        _taken++;

        if (_count < _capacity)
        {
            _count++;
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
        int offset = (metric.Table * CountersPerTable) + (int)metric.Counter;

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
}
