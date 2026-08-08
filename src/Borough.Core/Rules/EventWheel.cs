namespace Borough.Core.Rules;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// The Event Wheel's buckets: one row per Tick in the wheel's period.
/// </summary>
/// <remarks>
/// <b>A table rather than a bare array, because a bare array beside the columns is exactly what
/// <c>BOR0901</c> is an error for.</b> The buckets are storage the simulation reads every Tick, so
/// they are declared like any other storage and folded like any other storage. At
/// <see cref="EventWheel.Size"/> the whole thing is 64 KiB.
/// </remarks>
[Table]
public sealed class WheelBucketTable
{
    private readonly Rows<WheelBucket> _rows;

    /// <param name="size">The wheel's period, in Ticks. Every row is allocated up front.</param>
    public WheelBucketTable(int size)
    {
        _rows = new Rows<WheelBucket>("wheel_bucket", size, Buffering.OneCopy);

        Head = _rows.Saved<int>("head", Touch.PerTick);
        Tail = _rows.Saved<int>("tail", Touch.PerTick);

        _rows.Seal();

        // The buckets are the wheel's shape rather than its contents: there are exactly `size` of
        // them for the life of the world, none is ever freed, and slot i is Tick i mod size. The
        // allocator is used rather than bypassed so that the slot count and the columns agree.
        for (int i = 0; i < size; i++)
        {
            _rows.Allocate();
        }
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<WheelBucket> Rows => _rows;

    /// <summary>Head of the Rule Instances armed for this bucket's Tick.</summary>
    public Column<int> Head { get; }

    /// <summary>Tail, so an arming appends rather than push-fronts.</summary>
    public Column<int> Tail { get; }
}

/// <summary>
/// The minimal Event Wheel: a bucket per Tick, an arming, and a drain.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is slice 7's half of a slice 9 mechanism, and the split is deliberate</b>
/// (<c>plans/0011</c>, decision owed 1, branch c). Almost all of the Rule engine reaches without a
/// wheel at all, because a subscription is woken by <em>the mutator that writes the Bin</em> and not
/// by a timer. What does need one is scheduled dispatch — the <c>rate</c> re-arm after a success, and
/// the first firing of any Rule at all — and <c>02 §4.1</c> is emphatic that the alternative is
/// forbidden: <em>nothing ever walks the Building list looking for work.</em>
/// </para>
/// <para>
/// <b>What is here is a bucket array and a drain, and nothing that decides what session C owns.</b>
/// That session's subject is the Wheel's <em>semantics</em> — sinks, <c>adr/0006</c>, what <c>02 §7</c>
/// promises — none of which is settled by an array existing. Sleeps longer than the period, the
/// overflow list they need, and everything else the Wheel eventually carries are slice 9's.
/// </para>
/// <para>
/// <b>The buckets are <see cref="Disposition.Saved"/>, which is the conservative call.</b> A bucket's
/// membership is recoverable from every armed row's <see cref="RuleInstanceTable.NextTick"/>, but the
/// <em>order within</em> a bucket is arrival order and is not. Phase 3's shuffle probably makes that
/// order unobservable — but Phase 3 does not exist yet to check the claim against, and 64 KiB is a
/// cheap price for not having to make it.
/// </para>
/// </remarks>
public sealed class EventWheel
{
    /// <summary>
    /// <c>WHEEL_SIZE</c>: the wheel's period, in Ticks. A world-creation constant.
    /// </summary>
    /// <remarks>
    /// <b>Set by the longest routine sleep, which is what makes it a constant rather than tuning.</b>
    /// It is baked into the save, it is not hot-reloadable, and a <c>const</c> is therefore the
    /// correct spelling — <c>adr/0015</c>'s no-<c>const</c> rule governs numbers a designer would want
    /// to change, and changing this one is a different world rather than a different balance.
    /// </remarks>
    public const int Size = 8192;

    private readonly WheelBucketTable _buckets;
    private readonly RuleInstanceTable _instances;

    /// <param name="instances">The rows this wheel schedules.</param>
    public EventWheel(RuleInstanceTable instances)
    {
        ArgumentNullException.ThrowIfNull(instances);

        _instances = instances;
        _buckets = new WheelBucketTable(Size);
    }

    /// <summary>The bucket rows, for the hash composition and the footprint report.</summary>
    public WheelBucketTable Buckets => _buckets;

    /// <summary>The armed Rule Instances, bucket by bucket.</summary>
    /// <remarks>
    /// Bound freshly on each use, for <see cref="Entities.World.Occupants"/>'s reason: the spans run
    /// over the live prefix of their columns, and that prefix moves when a row is allocated.
    /// </remarks>
    public IndexList Armed => new(_buckets.Head, _buckets.Tail, _instances.QueueNext);

    /// <summary>Which bucket a Tick lands in.</summary>
    public static int BucketOf(Ticks tick) => (int)(tick.Raw % Size);

    /// <summary>
    /// Arms a Rule Instance to fire <paramref name="delay"/> Ticks after <paramref name="now"/>.
    /// </summary>
    /// <remarks>
    /// <b>A delay of zero or of a whole period is refused rather than clamped.</b> Both land the row
    /// back in the bucket currently being drained, which is an unbounded loop inside one Tick — a
    /// Rule that fires, re-arms onto the bucket it just came off, and fires again. Refusing is the
    /// minimal wheel being honest about its period; carrying a longer sleep is slice 9's overflow
    /// list, and clamping here would decide that question quietly.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The delay is zero, or is a whole period or more.</exception>
    public void Arm(int instanceSlot, Ticks now, uint delay)
    {
        if (delay == 0 || delay >= Size)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delay),
                delay,
                $"an Event Wheel arming must be at least 1 Tick and less than WHEEL_SIZE ({Size}). "
                + "A longer sleep needs slice 9's overflow list, not a wrap.");
        }

        Ticks at = now + new Ticks(delay);

        _instances.NextTick[instanceSlot] = at;
        _instances.WaitingOn[instanceSlot] = default;
        Armed.Append(BucketOf(at), instanceSlot);
    }

    /// <summary>
    /// Takes the next Rule Instance due on <paramref name="tick"/>, or
    /// <see cref="Rows.NoSlot"/> when the bucket is spent.
    /// </summary>
    /// <remarks>
    /// <b>One at a time rather than a returned collection</b>, because a collection would be an
    /// allocation per Tick on the phase that runs every Tick. The caller loops; a row it re-arms
    /// cannot land back in this bucket, which is what <see cref="Arm"/>'s refusal buys.
    /// </remarks>
    public int PopDue(Ticks tick) => Armed.PopFront(BucketOf(tick));
}
