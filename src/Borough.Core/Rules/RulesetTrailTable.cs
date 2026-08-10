namespace Borough.Core.Rules;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// What every Ruleset transition this world survived destroyed, capped at the last
/// <see cref="Retained"/> of them with older ones aggregated to counts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Degradation stops being a logged warning and becomes state, and <c>05 §7</c> gives the reason
/// rather than the preference.</b> One class of bug survives every other diagnostic this project has:
/// a defect <em>caused</em> by a degradation three patches ago and surfacing now, whose cause is
/// upstream of every snapshot anybody holds. No replay can reach it — the run that would reproduce it
/// starts after the cause. So <em>"at Ruleset <c>a91f…</c>: 412 <c>coal</c> Bins dropped, 3 Buildings
/// derelicted"</em> has to be carried by the city itself, and <em>"why does this city have three
/// derelict Buildings nobody built"</em> becomes a line item instead of an archaeology.
/// </para>
/// <para>
/// <b>It is a collection, so <c>adr/0006</c> applies — and the sink was written before the
/// collection.</b> The trail grows with <em>patches survived</em> rather than with elapsed time, which
/// is already outside the ADR's clause, but *unbounded in principle* is not a defence and this project
/// has broken the rule once by building a collection first and looking for its sink afterwards
/// (slice 6's <c>map</c> emission). So the cap is here in the constructor: the table holds exactly
/// <see cref="Retained"/> + 1 rows for the life of the world, allocates them and never frees one.
/// </para>
/// <para>
/// <b>Row 0 is the aggregate, and it is an entry rather than a special case.</b> When the window is
/// full the oldest entry is folded into it — counts summed, <see cref="Transitions"/> climbing past 1
/// — and its <see cref="Ruleset"/> is <b>0</b>, which is this project's established spelling for
/// <em>no single Ruleset</em> (<see cref="Simulation.RulesetInForce"/> uses it for <em>before the
/// first Tick</em>). The alternative — a second one-row table for the aggregate — buys nothing:
/// summing the whole trail is then two loops with different shapes, and <c>BOR0901</c> would have made
/// it a second type either way.
/// </para>
/// <para>
/// <b>The counts climb and the collection does not, and only the second is what <c>adr/0006</c>
/// forbids.</b> A long run at steady state performs no reloads at all, so nothing here moves; a
/// designer balancing performs one per edit, and the aggregate is then a <em>counter</em> of exactly
/// the kind <c>05 §7</c> asked for. What must never grow is the row count, and it cannot.
/// </para>
/// <para>
/// <b>Ids and counts, never a string</b> (<c>adr/0002</c>). A transition names its Ruleset by content
/// hash and its casualties by number; the shell resolves <c>a91f…</c> to a filename and
/// <c>ResourceId 4</c> to <em>coal</em>. That constraint decides the layout rather than being
/// satisfied by it afterwards.
/// </para>
/// <para>
/// <b>Every column is <see cref="Touch.Cold"/>.</b> Nothing inside <c>step()</c> reads this table —
/// it is written on a reload, which is a designer's action, and read on inspection.
/// </para>
/// </remarks>
[Table]
public sealed class RulesetTrailTable
{
    /// <summary>
    /// <c>N</c>: how many transitions are kept in full before they are aggregated to counts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A world-creation constant, and a <c>const</c> for <see cref="EventWheel.Size"/>'s reason
    /// turned one notch sharper.</b> <c>adr/0015</c>'s no-<c>const</c> rule governs numbers a designer
    /// would want to change; this one a designer must <em>not</em> be able to change, because the thing
    /// it sizes is the record of their own edits. A reload that shrank the window would truncate the
    /// history that explains the city, on the authority of the same file whose adoption the history is
    /// about. Read through <c>adr/0015</c>'s membership test — <em>what live state points at it</em> —
    /// it is world-creation-fixed, alongside <c>WHEEL_SIZE</c>.
    /// </para>
    /// <para>
    /// <b>It is saved and hash-bearing, so <c>adr/0052</c> applies and it is <em>unratified</em>.</b>
    /// 16 is a guess: it is the smallest window that outlives a balance sitting's worth of
    /// remove-watch-restore cycles, which is an argument about a working habit and not a measurement.
    /// The refuting number is <em>how far back a real cross-patch diagnosis had to reach</em>, and
    /// nothing can produce it until patches exist — so the ratifier is <b>the first real cross-patch
    /// diagnosis</b> and the trigger is one that had to reach past the window. Recorded in
    /// <c>plans/0002</c> §D.
    /// </para>
    /// </remarks>
    public const int Retained = 16;

    private readonly Rows<RulesetTrailEntry> _rows;

    /// <summary>Builds an empty trail: the aggregate row, and nothing survived yet.</summary>
    public RulesetTrailTable()
    {
        _rows = new Rows<RulesetTrailEntry>("ruleset_trail", Retained + 1, Buffering.OneCopy);

        Tick = _rows.Saved<Ticks>("tick", Touch.Cold);
        Ruleset = _rows.Saved<ulong>("ruleset", Touch.Cold);
        Transitions = _rows.Saved<int>("transitions", Touch.Cold);
        BuildingsDerelicted = _rows.Saved<int>("buildings_derelicted", Touch.Cold);
        BinsDropped = _rows.Saved<int>("bins_dropped", Touch.Cold);
        RuleInstancesRearmed = _rows.Saved<int>("rule_instances_rearmed", Touch.Cold);

        _rows.Seal();

        // The aggregate exists from world creation, holding zero transitions. Allocating it here is
        // what makes slot 0 mean the same thing in a world that has never reloaded and in one that has
        // reloaded a thousand times, so nothing that reads this table needs a liveness branch.
        _rows.Allocate();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<RulesetTrailEntry> Rows => _rows;

    /// <summary>The Tick the transition happened on. On the aggregate, the newest one folded in.</summary>
    public Column<Ticks> Tick { get; }

    /// <summary>
    /// The content hash of the Ruleset put in force. <b>0 on the aggregate</b>, which names none.
    /// </summary>
    public Column<ulong> Ruleset { get; }

    /// <summary>How many transitions this row accounts for: 1 on an entry, and the count on the aggregate.</summary>
    public Column<int> Transitions { get; }

    /// <summary>Buildings whose kind the new Ruleset could not describe.</summary>
    public Column<int> BuildingsDerelicted { get; }

    /// <summary>Bins whose Resource the new Ruleset could not describe, and their stock with them.</summary>
    public Column<int> BinsDropped { get; }

    /// <summary>Rule Instances re-armed, which is every wait list in the world dropped.</summary>
    public Column<int> RuleInstancesRearmed { get; }

    /// <summary>The slot the aggregate lives at, for the life of the world.</summary>
    public const int AggregateSlot = 0;

    /// <summary>
    /// How many transitions are retained in full. The aggregate is not one of them.
    /// </summary>
    public int Count => _rows.LiveCount - 1;

    /// <summary>The slot of the <paramref name="index"/>-th retained entry, oldest first.</summary>
    /// <remarks>
    /// <b>Entries are dense from slot 1 and in chronological order</b>, which is bought by
    /// <see cref="Record"/> sliding them down rather than by anything <see cref="Rows{T}"/> promises.
    /// It is worth the copy: index order is hash composition order (<c>02 §8</c>), so a ring buffer
    /// with a cursor would make two worlds that survived the same transitions hash differently
    /// depending on where the cursor happened to be.
    /// </remarks>
    public int EntrySlot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return index + 1;
    }

    /// <summary>Records one transition, aggregating the oldest entry if the window is full.</summary>
    /// <remarks>
    /// <b>A transition that cost the city nothing is not recorded, and the filter is the point of the
    /// collection rather than a saving.</b> This trail answers <em>what did a reload destroy</em>;
    /// a tuning reload destroys nothing, and an all-zero entry would push a real one out of a window
    /// sized for diagnosis. <em>How many Rulesets this session was played against</em> is a different
    /// question with a different answer already built — <see cref="Simulation.Reloads"/> — and keeping
    /// them apart is what stops this from becoming a reload log.
    /// </remarks>
    /// <param name="now">The Tick the transition happened on.</param>
    /// <param name="ruleset">The content hash of the Ruleset put in force.</param>
    /// <param name="cost">What the transition cost, from <see cref="Entities.World.Adopt"/>.</param>
    internal void Record(Ticks now, ulong ruleset, RulesetDegradation cost)
    {
        if (cost.IsNothing)
        {
            return;
        }

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
                    $"ruleset_trail allocated slot {slot} where {Count} was due; the trail depends on "
                    + "its rows being dense and in chronological order.");
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
        Ruleset[slot] = ruleset;
        Transitions[slot] = 1;
        BuildingsDerelicted[slot] = cost.BuildingsDerelicted;
        BinsDropped[slot] = cost.BinsDropped;
        RuleInstancesRearmed[slot] = cost.RuleInstancesRearmed;
    }

    /// <summary>Everything the trail knows, entries and aggregate together.</summary>
    /// <remarks>
    /// <b>The one figure that survives the cap</b>, and the reason <see cref="Transitions"/> is a
    /// column on every row rather than only on the aggregate: a total is a sum over the table, with no
    /// branch for which rows were kept in full.
    /// </remarks>
    public RulesetDegradation Total()
    {
        int derelicted = 0;
        int dropped = 0;
        int rearmed = 0;

        for (int slot = 0; slot <= Count; slot++)
        {
            derelicted += BuildingsDerelicted[slot];
            dropped += BinsDropped[slot];
            rearmed += RuleInstancesRearmed[slot];
        }

        return new RulesetDegradation(derelicted, dropped, rearmed);
    }

    /// <summary>How many transitions this world has survived, aggregated ones included.</summary>
    public int TransitionsRecorded()
    {
        int total = 0;

        for (int slot = 0; slot <= Count; slot++)
        {
            total += Transitions[slot];
        }

        return total;
    }

    private void Aggregate(int slot)
    {
        Transitions[AggregateSlot] += Transitions[slot];
        BuildingsDerelicted[AggregateSlot] += BuildingsDerelicted[slot];
        BinsDropped[AggregateSlot] += BinsDropped[slot];
        RuleInstancesRearmed[AggregateSlot] += RuleInstancesRearmed[slot];

        // The aggregate's Tick is the newest transition folded into it, which is what makes the trail
        // readable as a timeline: everything before this Tick is a count, everything after it is named.
        Tick[AggregateSlot] = Tick[slot];
    }

    private void Copy(int from, int to)
    {
        Tick[to] = Tick[from];
        Ruleset[to] = Ruleset[from];
        Transitions[to] = Transitions[from];
        BuildingsDerelicted[to] = BuildingsDerelicted[from];
        BinsDropped[to] = BinsDropped[from];
        RuleInstancesRearmed[to] = RuleInstancesRearmed[from];
    }
}
