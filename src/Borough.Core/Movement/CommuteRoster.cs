using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// Which Citizens leave home on which Tick of the Day, and which leave work. The commute's two
/// daily occasions.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two partitions of the population, and the argument for a partition rather than a schedule
/// survives <c>adr/0101</c> — but it survives for a different reason than the one it was written
/// with, and the difference is the whole of this rewrite.</b> 5b-bis task 5 argued it from a
/// <em>constant</em>: a commute recurs every Day, <see cref="EventWheel.Size"/> is exactly a Day, so a
/// Citizen armed on the Wheel never leaves its bucket and a bucketing on a constant is derivable.
/// That argument turned on the phase being a draw on the Citizen's id, and the phase is not that any
/// more. What holds now is the weaker and sufficient claim: <b>both phases are pure functions of
/// saved state</b> — the Workplace's own id and kind, the Citizen's id, and
/// <see cref="CitizenTable.PlannedCommute"/> — so a rebuild reproduces them exactly and neither is
/// stored.
/// </para>
/// <para>
/// ⚠ <b>The old remark would have stayed true-looking and stopped being load-bearing</b>, which is
/// the failure this corpus keeps meeting: a justification that still reads well after the thing it
/// justified has moved. The Wheel is still not wanted, and it is now <em>closer</em> to being wanted
/// than it was — a Citizen who changed jobs changes both phases, so the buckets are no longer fixed
/// for life. They are still not <em>scheduled</em>, because nothing has to fire to move them; the
/// membership follows the Workplace handle, and re-deriving on a job change is one remove and one add.
/// </para>
/// <para>
/// <b>Two link columns, not one roster read twice.</b> A Citizen is in both partitions at once — out
/// at the Shift start less their planned commute, back at the Shift start plus their Shift length —
/// and one <c>next</c> cannot thread a row into two lists. See
/// <see cref="CitizenTable.CommuteReturnNext"/>.
/// </para>
/// <para>
/// <b>256 KB, flat, and not sized from the population.</b> Four <c>int</c> arrays over a Day of
/// Ticks, plus the two link columns on the Citizen.
/// </para>
/// <para>
/// <b>The phase is not read off the slot, and the reason is unchanged and still binding.</b>
/// <c>SyntheticCity</c> assigns Citizen <c>i</c> to Household <c>i mod H</c> and Household <c>j</c> to
/// Building <c>j mod B</c>, so slot order is a fixed stride through the Building table and anything
/// derived from it sends whole streets to work together for a reason with no cause in the city
/// (<c>02 §8</c> rule 5). The draws below key on <b>monotonic ids</b> throughout.
/// </para>
/// </remarks>
public sealed class CommuteRoster
{
    /// <summary>Slot plus one, so a zeroed array reads as <em>empty</em> rather than as slot 0.</summary>
    private readonly int[] _outHead = new int[Ticks.PerDay];

    private readonly int[] _outTail = new int[Ticks.PerDay];

    private readonly int[] _homeHead = new int[Ticks.PerDay];

    private readonly int[] _homeTail = new int[Ticks.PerDay];

    /// <summary>The outbound list. Composed at the call site, never stored.</summary>
    private IndexList Outbound(CitizenTable citizens) =>
        new(_outHead.AsSpan(), _outTail.AsSpan(), citizens.CommuteNext);

    /// <summary>The homeward list.</summary>
    private IndexList Homeward(CitizenTable citizens) =>
        new(_homeHead.AsSpan(), _homeTail.AsSpan(), citizens.CommuteReturnNext);

    /// <summary>
    /// The Tick of the Day the jobs in <paramref name="employerId"/> start at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Drawn on the <em>Building</em>, which is what makes hours a property of the job</b>
    /// (<c>adr/0101</c>). A Citizen who changes employer changes their hours with nothing written on
    /// the Citizen and nothing to invalidate, because they never held the fact in the first place.
    /// </para>
    /// <para>
    /// <b>Uniform inside the kind's band, in whole in-world hours.</b> Hours rather than Ticks because
    /// that is what a designer means and what a workplace does — offices open on the hour, not at
    /// 08:17. <see cref="Ticks.AtHour"/> rounds, since an hour is 85.33 Ticks.
    /// </para>
    /// </remarks>
    /// <param name="key">The world seed, as the draw's first coordinate.</param>
    /// <param name="employerId">The employing Business's monotonic, never-reused id.</param>
    /// <param name="kind">The trade, as the Ruleset in force declares it.</param>
    public static int ShiftStartOf(WorldKey key, ulong employerId, BusinessKindDefinition kind)
    {
        int span = kind.ShiftStartLatestHour - kind.ShiftStartEarliestHour + 1;

        if (span < 1)
        {
            return 0;
        }

        // ⚠ TWO draws summed and halved, not one, and the measurement is what asked for it. A single
        // uniform draw over the band gave five near-equal clumps -- a PLATEAU with holes between them
        // -- while the return time next to it came out properly peaked. The two differ in exactly one
        // way: a return is `start + shift`, the SUM OF TWO uniform draws, and the sum of two uniforms
        // is triangular. So the peak in the evening was never authored, and the flatness in the
        // morning was the absence of the same trick rather than a missing curve.
        //
        // *** The shape is borrowed from inside the mechanism rather than invented at the write
        // site***, which is the whole reason it is allowable under adr/0043: it is not a distribution
        // somebody guessed, it is the one the other half of this same arithmetic already produces.
        //
        // Second coordinate mixed with the golden ratio, on EmploymentEngine's candidate-loop
        // precedent: one decision drawing twice, not two decisions sharing a stream.
        ulong first = Randomness.Draw(
            key, Randomness.Mix(employerId), Ticks.Zero, PurposeTag.ShiftStart);

        ulong second = Randomness.Draw(
            key, Randomness.Mix(employerId ^ 0x9E37_79B9_7F4A_7C15UL), Ticks.Zero,
            PurposeTag.ShiftStart);

        int hours = (int)(first % (ulong)(uint)span) + (int)(second % (ulong)(uint)span);

        // ⚠ AtClock AND NOT AtHour: a Shift START is a time of day, and a Day begins at 05:00. The
        // three other callers of AtHour are LENGTHS -- a Shift's hours, a punctuality margin, a whole
        // Day -- and must not be phased. This is the only site in the build where the two meanings
        // had to be told apart.
        return Ticks.AtClock(kind.ShiftStartEarliestHour + IntegerMath.RoundDiv(hours, 2));
    }

    /// <summary>
    /// The two Ticks of the Day <paramref name="citizen"/> travels on, if they hold a job at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>outbound = start − what the journey cost when the job was taken</c></b>, which is the one
    /// line of this decision that produces a broad peak rather than a spike: somebody who lives
    /// further out leaves earlier, for ever, and nobody authored a spread. <b><c>homeward = start +
    /// their own Shift length</c></b>, which is why the evening is flatter — a Workplace's staff
    /// arrive together and leave apart.
    /// </para>
    /// <para>
    /// <b>Both wrap, and the outbound one is why.</b> A job starting at 00:30 with a forty-minute
    /// commute is left for at 23:50 the previous Day, which is an ordinary night shift and not an
    /// error — so the arithmetic is a floor-mod over the Day rather than a clamp at zero, which would
    /// have quietly piled every such Citizen onto Tick 0.
    /// </para>
    /// <para>
    /// <b>False for a Citizen with no job, a Workplace that no longer resolves, or a kind the Ruleset
    /// has stopped declaring.</b> The third is dereliction: <c>World.TryDeclaredJobs</c> keeps the
    /// workers of an undeclared kind rather than sacking them, and this keeps them out of the roster,
    /// which is the same choice — they hold a job whose hours the Ruleset no longer states.
    /// </para>
    /// </remarks>
    public static bool TryPhasesOf(
        CitizenTable citizens,
        BuildingTable buildings,
        BusinessTable businesses,
        Ruleset rules,
        WorldKey key,
        int citizen,
        out int outbound,
        out int homeward)
    {
        ArgumentNullException.ThrowIfNull(citizens);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(rules);

        outbound = 0;
        homeward = 0;

        if (!rules.Jobs.Runs
            || !businesses.Rows.TryResolve(citizens.Workplace[citizen], out int workplace))
        {
            return false;
        }

        // ⚠ THE SECOND HOP, and it is where an unpremised employer stops. A Business carries its own
        // jobs and shift hours now, but a COMMUTE needs somewhere to go -- so a Citizen employed by a
        // Business with no premises is employed, counted, and rosters no Trip. adr/0146 makes that a
        // real case rather than a corner: a founder is their own Business's first worker, and a
        // founded Business is unpremised until placement tenants it (adr/0147). The reverse
        // transition is new -- a workplace that GAINS a location -- and World.Premise re-rosters.
        if (!buildings.Rows.TryResolve(businesses.Building[workplace], out int premises))
        {
            return false;
        }

        byte kind = businesses.Kind[workplace];

        if (!rules.DeclaresBusiness(kind))
        {
            return false;
        }

        BusinessKindDefinition definition = rules.BusinessKind(kind);

        if (definition.Jobs <= 0)
        {
            return false;
        }

        ulong id = citizens.Rows.IdAt(citizen);

        // 🔴 HASH-BEARING, and this is the line that moves the whole city. adr/0101 says the Shift
        // start hour belongs to the WORKPLACE, and the Workplace stopped being a Building -- so the
        // draw is keyed on the Business's monotonic id and every Citizen re-rolls. All four golden
        // artefacts re-record. adr/0100: that costs nothing while nobody is carrying a save.
        long start = ShiftStartOf(key, businesses.Rows.IdAt(workplace), definition);
        long planned = (long)citizens.PlannedCommute[citizen].Raw;
        long shift = (long)rules.Jobs.ShiftLengthOf(key, id).Raw;
        long early = (long)rules.Jobs.PunctualityOf(key, id).Raw;

        outbound = WrapIntoDay(start - planned - early);
        homeward = WrapIntoDay(start + shift);
        return true;
    }

    /// <summary>Rebuilds both partitions from saved state and the Ruleset in force.</summary>
    /// <remarks>
    /// <b>Walked in slot order, which is what makes the result exact</b> — a rebuild has no other
    /// order available, and <see cref="IndexList.InsertOrdered"/> puts each bucket in ascending slot
    /// order however it was filled, so a rebuilt roster is byte-identical to a maintained one rather
    /// than merely equivalent.
    /// </remarks>
    public void Rebuild(
        CitizenTable citizens,
        BuildingTable buildings,
        BusinessTable businesses,
        Ruleset rules,
        WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        Array.Clear(_outHead);
        Array.Clear(_outTail);
        Array.Clear(_homeHead);
        Array.Clear(_homeTail);
        citizens.CommuteNext.Span.Clear();
        citizens.CommuteReturnNext.Span.Clear();
        citizens.CommuteBucket.Span.Clear();

        IndexList outbound = Outbound(citizens);
        IndexList homeward = Homeward(citizens);

        for (int slot = 0; slot < citizens.Rows.SlotCount; slot++)
        {
            if (citizens.Rows.IsLive(slot)
                && TryPhasesOf(
                    citizens, buildings, businesses, rules, key, slot,
                    out int outAt, out int homeAt))
            {
                outbound.InsertOrdered(outAt, slot);
                homeward.InsertOrdered(homeAt, slot);
                citizens.CommuteBucket[slot] = Pack(outAt, homeAt);
            }
        }
    }

    /// <summary>Puts a Citizen in both of its buckets.</summary>
    /// <remarks>
    /// <b>Called when a job is taken rather than when a Citizen is born</b>, which is the change
    /// <c>adr/0101</c> makes to this class's contract. A Citizen with no Workplace has no hours, so
    /// the roster holds only the employed — and a roster that held everybody would be a departure
    /// list most of whose entries had nowhere to go.
    /// </remarks>
    public void Add(
        CitizenTable citizens,
        BuildingTable buildings,
        BusinessTable businesses,
        Ruleset rules,
        WorldKey key,
        int citizen)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        if (TryPhasesOf(citizens, buildings, businesses, rules, key, citizen, out int outAt, out int homeAt))
        {
            Outbound(citizens).InsertOrdered(outAt, citizen);
            Homeward(citizens).InsertOrdered(homeAt, citizen);
            citizens.CommuteBucket[citizen] = Pack(outAt, homeAt);
        }
    }

    /// <summary>Takes a Citizen out of both buckets, before its row is freed or its job changes.</summary>
    /// <remarks>
    /// <para>
    /// <b>Before the row goes back, because the id is read off it.</b> A Citizen removed after its row
    /// was freed would leave a dangling entry that the next allocation of that slot would find itself
    /// already in — the recycled-slot defect <c>BuildingResidency.Remove</c> carries the same warning
    /// about.
    /// </para>
    /// <para>
    /// ⚠ <b>And before the <em>Workplace handle</em> changes, which is new and is the sharper of the
    /// two.</b> Both phases are computed from the Workplace, so a Citizen whose job is rewritten first
    /// and unrostered second is removed from the buckets its <em>new</em> job names and left for ever
    /// in the ones its old job put it in. The order is remove, rewrite, add — and
    /// <c>EmploymentEngine</c> is the only caller that can get it wrong.
    /// </para>
    /// </remarks>
    public void Remove(CitizenTable citizens, int citizen)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        // ⚠ Read from where the row was PUT, never recomputed from the Workplace. See
        // CitizenTable.CommuteBucket: the handle is severable, so demolition makes a recomputed
        // bucket say `not rostered` and turns this into a silent no-op.
        int packed = citizens.CommuteBucket[citizen];

        if (packed == 0)
        {
            return;
        }

        Outbound(citizens).Remove((packed & 0xFFFF) - 1, citizen);
        Homeward(citizens).Remove(IntegerMath.ShiftRight(packed, 16) - 1, citizen);
        citizens.CommuteBucket[citizen] = 0;
    }

    /// <summary>Two buckets in one <c>int</c>, offset by one so that zero means <em>neither</em>.</summary>
    private static int Pack(int outbound, int homeward) =>
        (outbound + 1) | IntegerMath.ShiftLeft(homeward + 1, 16);

    /// <summary>The Citizens leaving home for work on one Tick of the Day.</summary>
    /// <param name="citizens">The table whose <c>CommuteNext</c> threads the bucket.</param>
    /// <param name="phase">A Tick of the Day, below <see cref="Ticks.PerDay"/>.</param>
    public IndexListWalk Departing(CitizenTable citizens, int phase)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        return Outbound(citizens).Walk(phase);
    }

    /// <summary>The Citizens leaving work for home on one Tick of the Day.</summary>
    public IndexListWalk Returning(CitizenTable citizens, int phase)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        return Homeward(citizens).Walk(phase);
    }

    /// <summary>How many Citizens leave home on one Tick of the Day.</summary>
    public int CountAt(CitizenTable citizens, int phase)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        return Outbound(citizens).Length(phase);
    }

    /// <summary>How many Citizens leave work on one Tick of the Day.</summary>
    public int ReturningCountAt(CitizenTable citizens, int phase)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        return Homeward(citizens).Length(phase);
    }

    /// <summary>
    /// A Tick of the Day, from an offset that may be negative or past midnight.
    /// </summary>
    /// <remarks>
    /// <b>A floor-mod rather than <c>%</c>, because the outbound phase is genuinely negative for an
    /// early shift</b> — C#'s remainder keeps the sign of the numerator, so a Citizen leaving at
    /// 23:50 for a 00:30 start would land on bucket <c>−42</c> and throw. Written through
    /// <see cref="IntegerMath.FloorDiv"/> rather than as a mask, even though a Day is a power of two,
    /// because the mask is correct by a coincidence this file must not depend on.
    /// </remarks>
    private static int WrapIntoDay(long tickOfDay) =>
        (int)(tickOfDay - ((long)Ticks.PerDay * IntegerMath.FloorDiv(tickOfDay, Ticks.PerDay)));
}
