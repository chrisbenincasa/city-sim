namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
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

    /// <param name="size">The wheel's period, in buckets. Every row is allocated up front.</param>
    /// <param name="name">
    /// The table's name. <b>Two instances of this table exist as of the coarse tier</b> — one bucket
    /// per Tick and one per Day — and a name is how a save, a footprint report and a hash-composition
    /// failure tell them apart. ***Two tables sharing a name is a diagnostic that names the wrong
    /// one***, which costs nothing until the day somebody is reading it.
    /// </param>
    public WheelBucketTable(int size, string name = "wheel_bucket")
    {
        _rows = new Rows<WheelBucket>(name, size, Buffering.OneCopy);

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
/// The Event Wheel: a bucket per Tick, a bucket per Day above it, an arming, a cascade and a drain.
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
    public const int Size = 2048;

    /// <summary>
    /// The coarse wheel's period, in <b>Days</b>. A world-creation constant, like <see cref="Size"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>128 Days, and the floor under it is derived rather than chosen.</b> <c>plans/0036</c>
    /// decision 3 asked for a bucket count and could not answer it, because the answer is <i>the
    /// longest sleep any consumer takes</i> and there was no consumer. <c>plans/0046</c> supplies one:
    /// a Life Stage of <c>N</c> Days drawn over a window of <c>W</c> sleeps for <c>N + W − 1</c> Days
    /// at most, and the longest of its four stages is 48 over a window of 16 — <b>63 Days</b>. 64
    /// buckets is therefore the derived floor; this is one doubling above it, because the stage table
    /// in <c>plans/0046</c> is stamped <c>PROVISIONAL</c> and a number chosen by taste will move.
    /// </para>
    /// <para>
    /// ⚠ <b>The cost of the headroom is 1 KiB and that is the whole argument for taking it.</b> Two
    /// <c>int</c> columns over 128 rows. The fine wheel is 64 KiB and nobody has ever noticed it.
    /// </para>
    /// </remarks>
    public const int CoarseDays = 128;

    /// <summary>
    /// The longest arming this wheel accepts, in Ticks: <b>one Day short of the coarse period</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>One Day short, and the missing Day is not slack.</b> An arming of <c>d</c> Ticks made at
    /// phase <c>p</c> of Day <c>D</c> falls due on Day <c>D + ⌊(p + d) / 2048⌋</c>, which is up to one
    /// Day beyond <c>D + ⌊d / 2048⌋</c> — so a delay of a whole <see cref="CoarseDays"/> worth of Ticks
    /// can land on Day <c>D + 128</c>, whose bucket is <c>D</c>'s own. That is the same wrap
    /// <see cref="Size"/> refuses one tier down, arriving through the phase instead of through the
    /// delay, and it is why this bound is stated separately rather than written <c>CoarseDays *
    /// Ticks.PerDay</c> at the call site.
    /// </remarks>
    public const long CoarseCeilingTicks = (CoarseDays - 1) * (long)Ticks.PerDay;

    private readonly WheelBucketTable _buckets;
    private readonly WheelBucketTable _coarse;
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
        _coarse = new WheelBucketTable(CoarseDays, "coarse_wheel_bucket");
    }

    /// <summary>The bucket rows, for the hash composition and the footprint report.</summary>
    public WheelBucketTable Buckets => _buckets;

    /// <summary>The coarse bucket rows, one per Day of <see cref="CoarseDays"/>.</summary>
    public WheelBucketTable CoarseBuckets => _coarse;

    /// <summary>The armed Rule Instances, bucket by bucket.</summary>
    /// <remarks>
    /// Bound freshly on each use, for <see cref="Entities.World.Occupants"/>'s reason: the spans run
    /// over the live prefix of their columns, and that prefix moves when a row is allocated.
    /// </remarks>
    public IndexList Armed => new(_buckets.Head, _buckets.Tail, _instances.QueueNext);

    /// <summary>The Rule Instances sleeping past a Day, bucket by Day.</summary>
    /// <remarks>
    /// ⚠ <b>The same link column as <see cref="Armed"/>, and that is safe because the two tiers are
    /// exclusive rather than layered.</b> A row is on one wheel or the other, never both:
    /// <see cref="Arm"/> chooses by delay and <see cref="Cascade"/> moves it across by unlinking it
    /// from one before appending it to the other. A second <c>queue_next</c> column would be a saved,
    /// hashed column whose every value is a duplicate of the first one's.
    /// </remarks>
    public IndexList CoarseArmed => new(_coarse.Head, _coarse.Tail, _instances.QueueNext);

    /// <summary>Which bucket a Tick lands in.</summary>
    public static int BucketOf(Ticks tick) => (int)(tick.Raw % Size);

    /// <summary>Which coarse bucket a Tick's <b>Day</b> lands in.</summary>
    public static int CoarseBucketOf(Ticks tick) =>
        (int)(IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay) % CoarseDays);

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
    /// ✅ <b>A longer sleep goes to the coarse wheel, which exists as of <c>plans/0046</c> stage 0.</b>
    /// This paragraph used to be a refusal, and the refusal's own text named the reason it could not
    /// be lifted: <i>it has no consumer until Life Stages arrive</i>. ⚠ <b>Life Stages were in turn
    /// deferred because the coarse wheel did not exist</b> — two milestones each cited as the other's
    /// reason not to exist, which nothing in the corpus was shaped to notice, because each half was
    /// locally true. <c>plans/0046</c> broke it by building the wheel first and against a stage table
    /// that fixes the longest sleep, which is also what made <see cref="CoarseDays"/> derivable.
    /// </para>
    /// <para>
    /// <b><c>adr/0056</c>'s refusal of the flat overflow list stands and is what the second tier
    /// is.</b> At 1,000,000 Households essentially every Household is mid-stage, so an overflow list
    /// holds ~1,000,000 entries permanently and staggering the rescan is the whole content of the
    /// repair — which makes a staggered overflow list <em>a coarse wheel with its buckets hidden</em>.
    /// Buckets, visible, is the same structure with the stagger stated.
    /// </para>
    /// <para>
    /// ⚠ <b>Past <see cref="CoarseCeilingTicks"/> the refusal is unchanged, and it is still a wrap
    /// rather than a capacity.</b> A wrap puts the row's next event in the past and nothing says so,
    /// which is the <c>HONEST DEGRADATION</c> case: a third tier is the answer if a consumer for a
    /// sleep past 127 Days ever appears, and no consumer in <c>plans/0046</c> comes within half of it.
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// The delay is zero, or is <see cref="CoarseCeilingTicks"/> or more.
    /// </exception>
    public void Arm(int instanceSlot, Ticks now, uint delay)
    {
        if (delay == 0 || delay >= CoarseCeilingTicks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delay),
                delay,
                $"an Event Wheel arming must be at least 1 Tick and less than {CoarseCeilingTicks} "
                + $"({CoarseDays - 1} Days). Below WHEEL_SIZE ({Size}) it goes on the fine wheel and "
                + "above it on the coarse one; past the ceiling there is no tier, and adr/0056 "
                + "refuses a wrap because a wrap puts the next event in the past and nothing says so.");
        }

        _invariants.Require(
            !IsArmed(instanceSlot, now), Invariant.RuleInstanceIsArmedOrWaiting, instanceSlot);

        Ticks at = now + new Ticks(delay);

        _instances.NextTick[instanceSlot] = at;
        _instances.WaitingOn[instanceSlot] = default;

        // The tier is chosen by the DELAY and not by the due Tick's distance, and the two differ: a
        // delay of 2047 made at phase 1 falls due on tomorrow's Day yet belongs on the fine wheel,
        // where BucketOf resolves it correctly because the fine wheel's period is exactly a Day and it
        // does not care which Day. Choosing by delay is also what makes the bound above checkable
        // against one number rather than against `now`.
        if (delay < Size)
        {
            Armed.Append(BucketOf(at), instanceSlot);
        }
        else
        {
            CoarseArmed.Append(CoarseBucketOf(at), instanceSlot);
        }
    }

    /// <summary>
    /// Moves everything the coarse wheel holds for <b>today</b> onto the fine wheel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Runs on a Day boundary and does nothing on any other Tick</b>, so its cost is one modulo
    /// 2,047 Ticks out of 2,048 and one bucket drain on the 2,048th. What it drains is one Day's worth
    /// of long sleepers rather than all of them: that is the stagger, and it is the entire reason
    /// <c>adr/0056</c> refused the flat overflow list.
    /// </para>
    /// <para>
    /// ⚠ <b>It must run BEFORE the fine wheel is drained, and a row landing in the bucket about to be
    /// popped is the case that says why.</b> A sleep ending at exactly midnight cascades into fine
    /// bucket 0 on the same Tick that drains fine bucket 0 — so cascading first fires it today, on the
    /// Tick it was armed for, and cascading second would leave it sitting for a whole further period.
    /// ***The ordering is load-bearing and it is not visible from either method alone***, which is why
    /// it is stated here and asserted by <c>CoarseWheelTests</c> rather than left to the call site.
    /// </para>
    /// <para>
    /// <b>Every row drained here is due today, and that is arithmetic rather than a hope.</b>
    /// <see cref="Arm"/> only reaches this wheel with a delay of at least <see cref="Size"/>, so the
    /// due Day is strictly after the arming Day; the ceiling puts it strictly inside the next
    /// <see cref="CoarseDays"/>; and the first Day boundary congruent to it modulo the period is
    /// therefore the due Day itself. <c>WorldInvariants.RuleInstancesAreQueuedExactlyOnce</c> checks
    /// it rather than assuming it.
    /// </para>
    /// </remarks>
    public void Cascade(Ticks tick)
    {
        if (tick.Raw % (ulong)Ticks.PerDay != 0UL)
        {
            return;
        }

        int bucket = CoarseBucketOf(tick);
        IndexList coarse = CoarseArmed;

        // Popped rather than walked, because the append below writes the same queue_next column the
        // walk would be reading. Draining to exhaustion is safe for the reason a fine drain is not:
        // nothing this loop touches can re-arm, since firing happens later in the phase.
        for (int slot = coarse.PopFront(bucket); slot != Rows.NoSlot; slot = coarse.PopFront(bucket))
        {
            Armed.Append(BucketOf(_instances.NextTick[slot]), slot);
        }
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
