namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// Every Building this world condemned and the condition that condemned it, capped at the last
/// <see cref="Retained"/> of them with older ones aggregated to counts.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>02 §9</c>'s hardest requirement, which is the one the build was one line from being able to
/// satisfy.</b> That section asks, for a Lot, <em>why it is vacant — not "vacant", but why</em>, and
/// calls it *"the hardest and the most valuable"*. The answer already exists at the moment of
/// condemnation: <see cref="RuleInstanceTable.Reported"/> holds the condition wherever an author wrote
/// an <c>on_fail</c> chain. <c>ZoneRuleEngine</c>'s own comment has recorded the gap for two
/// milestones — <em>"The condition behind the demolition is available and is not kept… nothing consumes
/// it: the row it would be copied off is freed on the next line."</em> This table is the somewhere to
/// put it.
/// </para>
/// <para>
/// <b>It stores what cannot be recomputed and nothing else, which is the test every column here had to
/// pass.</b> Most of <c>02 §9</c> needs no accumulator at all — a Building's occupants and Bin levels
/// are live state, a shortage is a scan, and a Lot's permissions are a Ruleset read — so the default is
/// a cold query and this table is the exception. <see cref="Lot"/> survives condemnation and is
/// therefore a handle rather than a copy; <see cref="Condition"/> and <see cref="Kind"/> do not survive
/// it, because <see cref="World.DestroyBuilding"/> frees the Rule Instances and the Building row on the
/// Tick this is written. ***A value measured once is not derivable***, which is <c>adr/0101</c>'s rule
/// for <c>planned_commute</c> reaching the decline model.
/// </para>
/// <para>
/// <b>It is a collection, so <c>adr/0006</c> applies, and the sink is the cap in the constructor.</b>
/// The table holds exactly <see cref="Retained"/> + 1 rows for the life of the world, allocates them
/// and never frees one. Row 0 is the aggregate and is an entry rather than a special case: when the
/// window fills, the oldest entry folds into it — <see cref="Condemnations"/> climbing past 1 — and its
/// <see cref="Kind"/> becomes <b>0</b>, this project's established spelling for <em>a kind the Ruleset
/// does not describe</em> and therefore for <em>no single kind</em>. <b>The counts climb and the
/// collection does not</b>, and only the second is what that ADR forbids.
/// </para>
/// <para>
/// <b>Entries are dense from slot 1 and in chronological order</b>, bought by <see cref="Record"/>
/// sliding them down rather than by anything <see cref="Rows{T}"/> promises — index order is hash
/// composition order (<c>02 §8</c>), so a ring buffer with a cursor would make two worlds that survived
/// the same condemnations hash differently depending on where the cursor happened to be. This is
/// <see cref="RulesetTrailTable"/>'s shape throughout, and deliberately so: it is the only precedent in
/// the project for a tally that outlives its subjects.
/// </para>
/// <para>
/// <b>Ids and numbers, never a string</b> (<c>adr/0002</c>). A condemnation names its place by Lot
/// handle and its cause by <see cref="ConditionId"/>; the shell resolves both.
/// </para>
/// <para>
/// <b>Every column is <see cref="Touch.Cold"/>.</b> Nothing inside <c>step()</c> reads this table. It
/// is written on a condemnation and read on inspection, which is <see cref="ColdPathAttribute"/>'s own
/// test for what Evidence is.
/// </para>
/// </remarks>
[Table]
public sealed class CondemnationTrailTable
{
    /// <summary>
    /// How many condemnations are kept in full before they are aggregated to counts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A world-creation constant, and a <c>const</c> for <see cref="RulesetTrailTable.Retained"/>'s
    /// reason on a second axis.</b> <c>adr/0015</c>'s no-<c>const</c> rule governs numbers a designer
    /// would want to change; this one nobody must be able to change under a live city, because the
    /// thing it sizes is the record that explains the city's own decline. A reload that shrank the
    /// window would truncate the history a player is in the middle of reading.
    /// </para>
    /// <para>
    /// <b>It is saved and hash-bearing, so <c>adr/0052</c> applies and it is <em>unratified</em>.</b>
    /// ⚠ <b>256 is a reasoned guess and explicitly not a measurement.</b> The measurement was attempted
    /// first: <c>--series</c> over ten Days on <c>rulesets/minimal.toml</c> shows the city fall from
    /// 1,201 Buildings to ~570 and then demolish 9–19 every 32 Ticks <em>without pause</em> — decline
    /// is a permanent uniform grind rather than an episode, because every shipped Ruleset inherits an
    /// <c>upkeep</c> Rule drawing on a Resource nothing produces. <b>A window sized to hold an episode
    /// cannot be sized in a city that has none</b>, and all four shipped files say in their own headers
    /// that they model no city. That is <c>adr/0052</c>'s amendment — <em>a ratifier names a machine, a
    /// world and a quantity</em> — with only the world missing.
    /// </para>
    /// <para>
    /// The argument the number rests on instead: <em>the smallest window that holds a whole decline
    /// episode plus the ordinary background of the period around it</em>, so an episode is legible
    /// against its own baseline — a diagnosis needs the contrast and not only the casualties. ⚠ <b>The
    /// cap is nearly free and that is what licenses erring large</b>: entries are
    /// <see cref="Touch.Cold"/>, so they cost nothing per Tick and the cost is bytes — 256 entries is
    /// ~5 KB against 86 MiB of tables at 1M. <c>adr/0006</c> requires a bound, not a tight one. ⚠ <b>Do
    /// not read 16 across from <see cref="RulesetTrailTable.Retained"/></b>: that number is sized by a
    /// <em>designer's</em> working habit and this one by a <em>player's</em> diagnosis, which is
    /// <c>plans/0012</c> Cause 5 — two quantities sharing a unit.
    /// </para>
    /// <para>
    /// <b>Ratifier</b>: the first real decline diagnosis on <b>a Ruleset that models a city</b>, which
    /// is <c>06</c>'s content row spanning milestones 12, 13 and 17. Refuting readings in both
    /// directions — a window never filled across a long run is too large; one that ran out of entries
    /// mid-episode is too small. Recorded in <c>plans/0028</c>.
    /// </para>
    /// </remarks>
    public const int Retained = 256;

    /// <summary>The slot the aggregate lives at, for the life of the world.</summary>
    public const int AggregateSlot = 0;

    private readonly Rows<CondemnationTrailEntry> _rows;

    /// <summary>Builds an empty trail: the aggregate row, and nothing condemned yet.</summary>
    public CondemnationTrailTable(Rows<Lot> lots)
    {
        ArgumentNullException.ThrowIfNull(lots);

        _rows = new Rows<CondemnationTrailEntry>("condemnation_trail", Retained + 1, Buffering.OneCopy);

        Tick = _rows.Saved<Ticks>("tick", Touch.Cold);

        // Severable, because a Lot outlives the Building on it but is not immortal: adr/0079 deletes a
        // vacant Lot that loses its frontage, which is precisely the state a condemned Lot is in.
        Lot = _rows.SavedHandle("lot", lots, Touch.Cold, Reference.Severable);

        Condition = _rows.Saved<ConditionId>("condition", Touch.Cold);
        Kind = _rows.Saved<byte>("kind", Touch.Cold);
        Condemnations = _rows.Saved<long>("condemnations", Touch.Cold);

        _rows.Seal();

        // The aggregate exists from world creation, holding zero condemnations. Allocating it here is
        // what makes slot 0 mean the same thing in a world that has never declined and in one that has
        // been grinding for a million Ticks, so nothing that reads this table needs a liveness branch.
        _rows.Allocate();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<CondemnationTrailEntry> Rows => _rows;

    /// <summary>The Tick the condemnation happened on. On the aggregate, the newest one folded in.</summary>
    public Column<Ticks> Tick { get; }

    /// <summary>Where it stood. Severed on the aggregate, which names no single place.</summary>
    public HandleColumn<Lot> Lot { get; }

    /// <summary>
    /// The condition the Building's Rule was reporting when it was condemned, from
    /// <see cref="RuleInstanceTable.Reported"/>. <see cref="ConditionId.None"/> where the author wrote
    /// no <c>on_fail</c> chain, and on the aggregate.
    /// </summary>
    public Column<ConditionId> Condition { get; }

    /// <summary>
    /// The Building's kind. <b>0 on the aggregate</b>, which names none — the same spelling a Building
    /// the Ruleset cannot describe carries (<c>adr/0057</c>).
    /// </summary>
    public Column<byte> Kind { get; }

    /// <summary>
    /// How many condemnations this row accounts for: 1 on an entry, and the count on the aggregate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one figure that survives the cap</b>, and the reason this is a column on every row rather
    /// than only on the aggregate: a total is a sum over the table with no branch for which rows were
    /// kept in full.
    /// </remarks>
    /// <para>
    /// ⚠ <b><c>long</c> rather than <c>int</c>, because this is the one quantity in the table with no
    /// sink at all.</b> Every other column is overwritten as entries slide down; the aggregate's count
    /// only ever grows, for the life of the world, and that is what <em>attribution decays to
    /// magnitude</em> means. Measured 2026-08-17: a 1,000-Citizen city condemns <b>0.031 Buildings a
    /// Tick</b> over 100,000 Ticks, which scaled by <c>World</c>'s own Lot allocation is <b>~57 a
    /// Tick</b> at a million — so a 32-bit counter overflows after roughly <b>162 hours</b> of play at
    /// the 4× target. Reachable, and the failure is silent: the count wraps negative and the trail
    /// starts reporting that the city has un-condemned Buildings.
    /// </para>
    /// <para>
    /// <b><c>adr/0065</c> arriving on a third axis, and the lesson is the one that file already
    /// coined.</b> A Bin's <c>level</c> was an <c>int</c> while <c>Money</c> was a <c>long</c>, so the
    /// corpus held <em>two widths for one quantity</em>; here there is one quantity and the question is
    /// whether its width matches its <b>lifetime</b>. ***A counter with no sink is denominated in the
    /// life of the world, not in the size of the city*** — so the bound that sizes it is a campaign
    /// length rather than a population, and every other count in this project is sized by the latter.
    /// </para>
    /// </remarks>
    public Column<long> Condemnations { get; }

    /// <summary>How many condemnations are retained in full. The aggregate is not one of them.</summary>
    public int Count => _rows.LiveCount - 1;

    /// <summary>The slot of the <paramref name="index"/>-th retained entry, oldest first.</summary>
    public int EntrySlot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return index + 1;
    }

    /// <summary>How many Buildings this world has condemned, aggregated ones included.</summary>
    /// <remarks>
    /// <c>long</c> for <see cref="Condemnations"/>' reason, and it would be wrong in <c>int</c> even if
    /// the column were not: this sums 257 rows, so a narrow return would overflow before the column it
    /// is summing did.
    /// </remarks>
    public long CondemnationsRecorded()
    {
        long total = 0;

        for (int slot = 0; slot <= Count; slot++)
        {
            total += Condemnations[slot];
        }

        return total;
    }

    /// <summary>Records one condemnation, aggregating the oldest entry if the window is full.</summary>
    /// <remarks>
    /// <b>There is no filter at the write door, and that is a difference from
    /// <see cref="RulesetTrailTable.Record"/> rather than an omission.</b> That trail refuses a
    /// transition that cost the city nothing, because an all-zero entry would push a real one out of a
    /// window sized for diagnosis. A condemnation is never nothing: the Building is gone either way,
    /// and a condemnation with <see cref="ConditionId.None"/> — an author who wrote no <c>on_fail</c>
    /// chain — is exactly the case a player most needs to see, since the alternative is a Building that
    /// vanishes with no entry anywhere.
    /// </remarks>
    /// <param name="now">The Tick the condemnation happened on.</param>
    /// <param name="lot">The Lot it stood on, which outlives it.</param>
    /// <param name="kind">The Building's kind, which does not.</param>
    /// <param name="condition">What its Rule was reporting, which does not either.</param>
    internal void Record(Ticks now, Handle<Lot> lot, byte kind, ConditionId condition)
    {
        int slot;

        if (Count < Retained)
        {
            // Never freed, so the allocator hands slots out in ascending order and the window stays
            // dense. The resolve is not a formality: density is this table's own invariant rather than
            // a property of Rows, and reading it back is where a change to the free list would be
            // caught instead of silently reordering the trail.
            slot = _rows.Resolve(_rows.Allocate());

            if (slot != Count)
            {
                throw new InvalidOperationException(
                    $"condemnation_trail allocated slot {slot} where {Count} was due; the trail depends "
                    + "on its rows being dense and in chronological order.");
            }
        }
        else
        {
            Aggregate(EntrySlot(0));

            for (int i = 1; i < Retained; i++)
            {
                Copy(i + 1, i);
            }

            slot = Retained;
        }

        Tick[slot] = now;
        Lot[slot] = lot;
        Kind[slot] = kind;
        Condition[slot] = condition;
        Condemnations[slot] = 1L;
    }

    private void Aggregate(int slot)
    {
        Condemnations[AggregateSlot] += Condemnations[slot];

        // The aggregate's Tick is the newest condemnation folded into it, which is what makes the trail
        // readable as a timeline: everything before this Tick is a count, everything after it is named.
        Tick[AggregateSlot] = Tick[slot];
    }

    private void Copy(int from, int to)
    {
        Tick[to] = Tick[from];
        Lot[to] = Lot[from];
        Kind[to] = Kind[from];
        Condition[to] = Condition[from];
        Condemnations[to] = Condemnations[from];
    }
}
