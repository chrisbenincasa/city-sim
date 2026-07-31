using BenchmarkDotNet.Attributes;
using S4.Kernels.Baseline;

namespace S4.Kernels.Kernels;

/// <summary>
/// K5 — wheel bucket drain and reschedule, across 8,192 buckets. plans/0004 task 8.
///
/// *Decides:* random writes across a large structure — **the Wheel's own cost, which nothing in the
/// corpus has ever sized.** The Event Wheel is described as the single largest performance lever in the
/// project and its overhead has never had a number put on it.
///
/// **What this kernel is, and what K2 is.** The Wheel's own state is three things: the 8,192 bucket
/// heads, the intrusive <c>wheel_next</c> link on every scheduled entity, and that entity's
/// <c>next_event_tick</c> — all three are WorldSchema's per-Tick tier. Draining a bucket chases the link
/// list, and rescheduling writes a new tick and splices the entity into a bucket somewhere else in the
/// ring. What it does *not* include is the woken entity's wake-tier columns; that gather is K2, by
/// design, and the Tick's real cost is the sum of the two. Folding them together here would produce one
/// number that could not be attributed to either.
///
/// **The population is derived, not chosen.** Every table in WorldSchema carrying a <c>wheel_next</c>
/// column is scheduled: Citizens, Households, Buildings, Businesses. At the target that is 1,560,000
/// entities. The link column is 6.2 MB and the tick column 12.5 MB, so the Wheel's own working set is
/// ~18.7 MB against a 12 MB L3 — it does not fit, and it would not fit at half the population either.
///
/// **The wake interval is a parameter because the corpus has never fixed it, and the answer moves with
/// it.** It is tempting to reason that 1,560,000 entities over 8,192 buckets is ~190 wakes per Tick. That
/// is wrong, and the error is worth stating: bucket occupancy is only uniform if the reschedule delay is
/// uniform over the whole ring. An entity that wakes every M Ticks is drained 1/M of the time, so the
/// drain rate is N/M per Tick and the bucket being drained holds N/M — not N/8192. At M = 4096 that is
/// 381 wakes per Tick; at M = 256, a Citizen waking 32 times a Day, it is 6,094. The parameters bracket
/// the range the design might land in, and the per-wake cost is what carries across them.
///
/// **Two variants, and the second one is the ideal.** There is no closed form for what a pointer chase
/// "should" cost, so the floor is measured rather than asserted: the same reschedule arithmetic, the same
/// scattered write into the bucket heads, the same two writes to the entity — with the entities visited
/// in index order instead of by following <c>wheel_next</c>. Identical work, identical traffic, no
/// address dependency. The ratio is the price of the chase and nothing else.
///
/// **Both variants are reported per wake.** <c>OperationsPerInvoke</c> is the drain target, so
/// BenchmarkDotNet's mean is nanoseconds per woken entity directly, and the figures stay comparable
/// across parameters that drain at wildly different rates per Tick.
///
/// **The measured cost includes the hash.** A reschedule needs a delay and the simulation will compute it
/// the same way, so the counter hash sits inside the timed loop deliberately. It is roughly 15 integer
/// operations; against a DRAM miss it is small, and against the sequential floor it is not, which
/// compresses the ratio toward 1 and makes the reported chase penalty a floor on the real one.
/// </summary>
public unsafe class K5WheelDrain
{
    /// <summary>WHEEL_SIZE. A world-creation constant, set by the longest routine sleep.</summary>
    private const int WheelSize = 8192;

    private const int Empty = -1;

    /// <summary>Wakes per invocation. Fixed so that every parameter reports the same unit.</summary>
    private const int TargetWakes = 1_000_000;

    /// <summary>Mean Ticks between an entity's wakes — 2, 8 and 32 wakes per Day at TICKS_PER_DAY = 8192.</summary>
    [Params(4096, 1024, 256)]
    public int MeanWakeIntervalTicks { get; set; }

    private int _entities;
    private int _delaySpan;
    private long _tick;
    private int _cursor;

    private int* _head;
    private int* _next;
    private long* _nextEventTick;

    [GlobalSetup]
    public void Setup()
    {
        _entities = (int)ScheduledEntities(1_000_000);
        _delaySpan = Math.Min(WheelSize - 1, (2 * MeanWakeIntervalTicks) - 1);

        _head = (int*)Streams.Allocate((nuint)(WheelSize * sizeof(int)));
        _next = (int*)Streams.Allocate((nuint)_entities * sizeof(int));
        _nextEventTick = (long*)Streams.Allocate((nuint)_entities * sizeof(long));

        for (var b = 0; b < WheelSize; b++)
        {
            _head[b] = Empty;
        }

        for (var e = 0; e < _entities; e++)
        {
            var bucket = (int)(CounterHash.Of((ulong)e, 0, CounterHash.Purpose.K5InitialBucket) & (WheelSize - 1));
            _next[e] = _head[bucket];
            _head[bucket] = e;
            _nextEventTick[e] = bucket;
        }

        // Setup inserts in index order and spreads entities uniformly across buckets. Neither is the
        // steady state: real occupancy is triangular, not uniform, and a list built in index order is
        // the floor variant's access pattern wearing the baseline's name. Four wakes per entity puts
        // both right, and costs a fifth of a second once per benchmark process.
        _tick = 0;
        _cursor = 0;
        Drain(4 * _entities);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Streams.Free((byte*)_head);
        Streams.Free((byte*)_next);
        Streams.Free((byte*)_nextEventTick);
        _head = null;
        _next = null;
        _nextEventTick = null;
    }

    /// <summary>The Wheel as designed: drain the bucket at the cursor by chasing the intrusive list.</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = TargetWakes)]
    public int Revolution() => Drain(TargetWakes);

    /// <summary>
    /// The floor. Same hash, same scattered head write, same two writes to the entity, same number of
    /// loads — visited in index order, so the address of the next entity is known before the current
    /// one's line arrives.
    /// </summary>
    [Benchmark(OperationsPerInvoke = TargetWakes)]
    public int SequentialFloor()
    {
        var head = _head;
        var next = _next;
        var nextEventTick = _nextEventTick;
        var entities = _entities;
        var span = (ulong)_delaySpan;
        var tick = _tick;
        var cursor = _cursor;
        var sink = 0;

        for (var drained = 0; drained < TargetWakes; drained++)
        {
            if (cursor >= entities)
            {
                cursor = 0;
                tick++;
            }

            sink += next[cursor];

            var delay = 1 + (int)(CounterHash.Of((ulong)cursor, (ulong)tick, CounterHash.Purpose.K5RescheduleDelay) % span);
            var target = (int)((tick + delay) & (WheelSize - 1));

            nextEventTick[cursor] = tick + delay;
            next[cursor] = head[target];
            head[target] = cursor;
            cursor++;
        }

        _tick = tick;
        _cursor = cursor;
        return sink;
    }

    private int Drain(int targetWakes)
    {
        var head = _head;
        var next = _next;
        var nextEventTick = _nextEventTick;
        var span = (ulong)_delaySpan;
        var tick = _tick;
        var drained = 0;

        while (drained < targetWakes)
        {
            var bucket = (int)(tick & (WheelSize - 1));
            var entity = head[bucket];
            head[bucket] = Empty;

            while (entity >= 0)
            {
                var following = next[entity];

                var delay = 1 + (int)(CounterHash.Of((ulong)entity, (ulong)tick, CounterHash.Purpose.K5RescheduleDelay) % span);
                var target = (int)((tick + delay) & (WheelSize - 1));

                nextEventTick[entity] = tick + delay;
                next[entity] = head[target];
                head[target] = entity;

                drained++;
                entity = following;
            }

            tick++;
        }

        _tick = tick;
        return drained;
    }

    /// <summary>
    /// Every table carrying a <c>wheel_next</c> column is on the Wheel. Derived from the schema so that
    /// a column added in slice 4 changes this figure rather than silently disagreeing with it.
    /// </summary>
    private static long ScheduledEntities(long citizens) =>
        WorldSchema.All
            .Where(table => table.Columns.Any(column => column.Name == "wheel_next"))
            .Sum(table => table.Rows(citizens));
}
