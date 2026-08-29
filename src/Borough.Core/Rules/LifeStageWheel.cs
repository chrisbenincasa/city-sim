namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// A bucket per Day, carrying Households towards their next Life Stage.
/// </summary>
/// <remarks>
/// <para>
/// <b>A second wheel rather than a second tier on <see cref="EventWheel"/>, and the reason is a
/// type.</b> That class walks <c>QueueNext</c>, <c>NextTick</c> and <c>Blocked</c>, all three of
/// which live on <see cref="RuleInstanceTable"/>; a Household has none of them. Generalising the
/// wheel over its columns is what slice 9's <em>"generalises it to everything else that sleeps"</em>
/// always meant, and it is deliberately not done here: <c>adr/0056</c>'s own rule is that <b>a wheel
/// is added when its consumer exists</b>, and applying that to the abstraction says the same thing —
/// one consumer is not a pattern. Needs decay and the housed re-evaluation are the second and third,
/// and the day either arrives this class and <see cref="EventWheel"/> collapse into one.
/// </para>
/// <para>
/// <b>Coarse only, with no fine tier and no cascade.</b> A Life Stage countdown is denominated in
/// Days by <c>adr/0011</c> and there is no calendar to make it finer against, so a transition fires
/// at the start of its due Day and needs no phase. That removes the whole of stage 0's load-bearing
/// ordering property: there is no fine bucket to cascade into, so there is nothing to get the order
/// of. ⚠ <b>The cost is a spike</b> — every Household due on a Day transitions on that Day's first
/// Tick, which is order 3,000 at a million Citizens — and <c>adr/0011</c>'s <c>W</c> smears the
/// cohort across Days rather than within one. ***Accepted deliberately at scoping rather than
/// overlooked***: the per-transition work is a table lookup, a draw and a re-arm.
/// </para>
/// <para>
/// <b>It shares <see cref="EventWheel.CoarseDays"/> and does not restate it.</b> The two wheels have
/// no reason to disagree about how far ahead a Day bucket can reach, and a second constant would be
/// a number that drifts from the first one.
/// </para>
/// </remarks>
public sealed class LifeStageWheel
{
    private readonly WheelBucketTable _buckets;
    private readonly HouseholdTable _households;

    /// <param name="households">The rows this wheel schedules.</param>
    public LifeStageWheel(HouseholdTable households)
    {
        ArgumentNullException.ThrowIfNull(households);

        _households = households;
        _buckets = new WheelBucketTable(EventWheel.CoarseDays, "life_stage_bucket");
    }

    /// <summary>The bucket rows, for the hash composition and the footprint report.</summary>
    public WheelBucketTable Buckets => _buckets;

    /// <summary>The Households waiting on a stage change, bucket by Day.</summary>
    /// <remarks>
    /// Bound freshly on each use, for <see cref="EventWheel.Armed"/>'s reason: the spans run over the
    /// live prefix of their columns, and that prefix moves when a row is allocated.
    /// </remarks>
    public IndexList Waiting => new(_buckets.Head, _buckets.Tail, _households.StageNext);

    /// <summary>Which bucket a Day lands in.</summary>
    /// <remarks>
    /// <b>A plain remainder, and it is safe because a Day index is never negative.</b> Tick 0 is Day
    /// 0 and the clock only moves forward, so the sign case <c>IntegerMath.FloorDiv</c> exists for
    /// cannot arise here — unlike in <see cref="DayOf"/>, where the division is stated rather than
    /// written raw because <c>BOR0203</c> is an error for it.
    /// </remarks>
    public static int BucketOf(long day) => (int)(day % EventWheel.CoarseDays);

    /// <summary>Which Day a Tick belongs to.</summary>
    public static long DayOf(Ticks tick) => IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);

    /// <summary>
    /// Schedules a Household to leave its current stage on <paramref name="dueDay"/>.
    /// </summary>
    /// <remarks>
    /// <b>The bound is <see cref="EventWheel.CoarseDays"/> and it is exclusive at BOTH ends.</b> A
    /// transition due today would land in the bucket this Day is draining, which is an unbounded loop
    /// inside one Tick — a Household that transitions, re-arms onto the bucket it just came off, and
    /// transitions again. One a whole period out lands in the same bucket as one due now, so the
    /// wheel could not tell them apart and would wake it 128 Days early. ***Both are the same wrap
    /// <see cref="EventWheel.Arm"/> refuses one tier down.***
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The due Day is not strictly inside the next <see cref="EventWheel.CoarseDays"/>.
    /// </exception>
    public void Arm(int householdSlot, long today, long dueDay)
    {
        long ahead = dueDay - today;

        if (ahead < 1 || ahead >= EventWheel.CoarseDays)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dueDay),
                dueDay,
                $"a Life Stage arming must fall between 1 and {EventWheel.CoarseDays - 1} Days "
                + $"ahead; this one is {ahead} Days from Day {today}. A stage due today lands in the "
                + "bucket being drained and one a whole period out is indistinguishable from it.");
        }

        _households.NextStageDay[householdSlot] = (int)dueDay;
        Waiting.Append(BucketOf(dueDay), householdSlot);
    }

    /// <summary>
    /// Takes the next Household due on <paramref name="day"/>, or <see cref="Rows.NoSlot"/>.
    /// </summary>
    /// <remarks>
    /// <b>One at a time rather than a returned collection</b>, for <see cref="EventWheel.PopDue"/>'s
    /// reason: a collection is an allocation on a path that runs every Day. A Household the caller
    /// re-arms cannot land back in this bucket, which is what <see cref="Arm"/>'s refusal buys.
    /// </remarks>
    public int PopDue(long day) => Waiting.PopFront(BucketOf(day));

    /// <summary>Unlinks a Household from its bucket — for a row about to be freed.</summary>
    /// <remarks>
    /// <b>Returns whether it was there</b>, because discarding that is the defect
    /// <c>BinTests</c> records for the Rule wheel: a row that says it is armed and is not in its
    /// bucket would be silently skipped, and the caller would free a row this wheel still holds.
    /// ⚠ <b>Nothing calls this in stage 1</b> — no Household dissolves until stage 2 — and it exists
    /// now because <c>World.DestroyHousehold</c> is the one site that will need it and finding out
    /// then is finding out late.
    /// </remarks>
    public bool Disarm(int householdSlot) =>
        Waiting.Remove(BucketOf(_households.NextStageDay[householdSlot]), householdSlot);
}
