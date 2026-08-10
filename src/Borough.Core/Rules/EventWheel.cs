namespace Borough.Core.Rules;

using Borough.Core.Invariants;
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
/// <b>What is here is a bucket array and a drain, and nothing that decides what session C owned.</b>
/// That session's subject was the Wheel's <em>semantics</em> — sinks, <c>adr/0006</c>, what
/// <c>02 §7</c> promises — none of which is settled by an array existing. It has since run and
/// produced <c>adr/0056</c>: the Wheel is <b>two levels</b>, a fine wheel of one bucket per Tick and a
/// coarse wheel of one bucket per Day, with <b>one wheel per scheduled table</b> rather than one wheel
/// over tagged entities. <b>A sleep longer than the fine period is the coarse wheel's, and the flat
/// overflow list an earlier version of this remark promised was refused rather than deferred</b> — a
/// staggered rescan of a million permanently-resident entries is a coarse wheel with its buckets
/// hidden.
/// </para>
/// <para>
/// <b>This is still the fine wheel only, and slice 9 did not generalise it.</b> The coarse wheel has no
/// consumer until Life Stages arrive in Phase 2, and <c>adr/0056</c>'s rule is that a wheel is added
/// when its consumer exists — so this type stays bound to <see cref="RuleInstanceTable"/> by name.
/// Generalising it against the only consumer there is would produce that consumer's shape wearing a
/// type parameter. See <c>plans/0016</c>, decision owed 2.
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
    private readonly InvariantRegistry _invariants;

    /// <param name="instances">The rows this wheel schedules.</param>
    /// <param name="invariants">
    /// The channel <see cref="Arm"/>'s state refusal reports through. <b>A registry rather than a
    /// throw, because a double arming is a claim about the world and not about an argument</b> — the
    /// delay refusal above it is the other way round. It is the same choice
    /// <c>World.Subscribe</c> makes at the other half of the partition, and it means a crash artifact
    /// carries the invariant's number rather than an exception's text.
    /// </param>
    public EventWheel(RuleInstanceTable instances, InvariantRegistry invariants)
    {
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentNullException.ThrowIfNull(invariants);

        _instances = instances;
        _invariants = invariants;
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
    /// <summary>
    /// Arms a Rule Instance to fire <paramref name="delay"/> Ticks after <paramref name="now"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A delay of zero or of a whole period is refused rather than clamped.</b> Both land the row
    /// back in the bucket currently being drained, which is an unbounded loop inside one Tick — a
    /// Rule that fires, re-arms onto the bucket it just came off, and fires again.
    /// </para>
    /// <para>
    /// <b>A longer sleep belongs to the coarse wheel, and the refusal is what says so honestly.</b>
    /// <c>adr/0056</c> refused the flat overflow list an earlier draft of this message offered: at
    /// 1,000,000 Households essentially every Household is mid-stage, so the list holds ~1,000,000
    /// entries permanently, and staggering the rescan is the whole content of the repair — which makes
    /// a staggered overflow list <em>a coarse wheel with its buckets hidden</em>. Until a consumer for
    /// a long sleep exists the arming is refused rather than represented, because a wrap would put a
    /// Household's next event in the past and nothing would say so (<c>HONEST DEGRADATION</c>).
    /// </para>
    /// <para>
    /// <b>A row already armed is refused too, and the two refusals hold each other up.</b> Arming
    /// twice appends the row to a second bucket, so it fires twice and one of the two entries outlives
    /// every subsequent unlink. It is detectable here in <c>O(1)</c> only because a delay of zero is
    /// refused: that is what makes <see cref="RuleInstanceTable.NextTick"/> strictly greater than
    /// <paramref name="now"/> for an armed row and exactly equal for one in flight, so the same column
    /// separates the two states without a new one. See <see cref="IsArmed"/>.
    /// </para>
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
                + "A longer sleep is the coarse wheel's, and it has no consumer until Life Stages "
                + "arrive; adr/0056 refused both a wrap and a flat overflow list, because a wrap puts "
                + "the next event in the past and a staggered list is a coarse wheel with its buckets "
                + "hidden.");
        }

        _invariants.Require(
            !IsArmed(instanceSlot, now), Invariant.RuleInstanceIsArmedOrWaiting, instanceSlot);

        Ticks at = now + new Ticks(delay);

        _instances.NextTick[instanceSlot] = at;
        _instances.WaitingOn[instanceSlot] = default;
        Armed.Append(BucketOf(at), instanceSlot);
    }

    /// <summary>
    /// Whether this row is on the wheel right now, as against in flight this Tick.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0056</c>'s <c>{armed, waiting}</c> partition has a third state, and this is the
    /// predicate that sees it.</b> <see cref="RuleInstanceTable.Blocked"/> reads
    /// <see cref="Blocking.Nothing"/> for an armed row <em>and</em> for one Phase 1 has already
    /// popped, so it cannot separate them alone. <see cref="RuleInstanceTable.NextTick"/> can: a
    /// popped row was due <em>now</em>, and an armed one is due strictly later, because
    /// <see cref="Arm"/> refuses a delay of zero. A row due <em>before</em> now is unreachable while
    /// the drain visits every bucket in order — and
    /// <c>WorldInvariants.RuleInstancesAreQueuedExactlyOnce</c> is where that is checked rather than
    /// assumed, because the check for it cannot be written modulo the period.
    /// </remarks>
    public bool IsArmed(int instanceSlot, Ticks now) =>
        _instances.Blocked[instanceSlot] == Blocking.Nothing
        && _instances.NextTick[instanceSlot] > now;

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
