using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// Which Citizens leave for work on which Tick of the Day. The commute's daily occasion.
/// </summary>
/// <remarks>
/// <para>
/// <b>A partition of the population by departure phase, and the argument for it being a partition
/// rather than a schedule is the whole of 5b-bis task 5's design.</b> A commute recurs <em>every
/// Day</em>, and <see cref="EventWheel.Size"/> is <em>exactly</em> a Day
/// (<see cref="Ticks.PerDay"/>). So a Citizen armed on the Wheel would re-arm at
/// <c>+<see cref="Ticks.PerDay"/></c> for ever and never leave the bucket it started in — which makes the bucket a function of a constant,
/// and <b>a bucketing on a constant is derivable rather than scheduled</b>. Putting it on the Wheel
/// would have paid a saved column, a per-Tick re-arm and a generalisation of the Wheel to a second
/// table for a structure that never changes. <c>adr/0081</c> says generalising the Wheel is not
/// required; this is why it is not even useful.
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c>, and it passes <c>05 §3</c>'s test rather than merely wanting
/// to.</b> A Citizen's phase is a pure function of its <em>monotonic id</em>, the world seed and the
/// Ruleset in force, and <see cref="IndexList.InsertOrdered"/> puts each bucket in ascending slot
/// order regardless of insertion order — so a rebuild reproduces this <b>exactly rather than
/// plausibly</b>, which is the property <see cref="Rebuild"/>'s test asserts. That is
/// <c>BuildingResidency</c>'s argument reused, and it is reused because it is the same argument.
/// </para>
/// <para>
/// <b>It reads the Ruleset, so it rebuilds when the Ruleset changes.</b> The departure window is
/// derived from <c>[jobs] commute_peak_factor</c>, so a hot reload that retunes the peak moves every
/// Citizen's departure — which is <c>adr/0064</c>'s disposition, the same one a Bin's capacity has:
/// <em>derived from the Ruleset in force</em>, so retuning reaches the standing city rather than
/// only the next Citizen born. ⚠ It is also 5a-bis's trap — <b>a derived structure that caches a
/// Ruleset value reads as <em>absent</em> rather than as <em>stale</em> before its first
/// rebuild</b> — so <c>World.Adopt</c> rebuilds this explicitly rather than leaving it to the next
/// write.
/// </para>
/// <para>
/// <b>The phase is drawn from the id and not from the slot, and the difference is a spatial
/// wave.</b> A slot-derived phase would be free — bucket <c>b</c> is the arithmetic progression
/// <c>b, b + window, …</c>, no index at all — and it would be wrong here, because
/// <c>SyntheticCity</c> assigns Citizen <c>i</c> to Household <c>i mod H</c> and Household <c>j</c>
/// to Building <c>j mod B</c>. Slot order is therefore a fixed stride through the Building table,
/// and a phase read off it would send whole streets to work together for a reason with no cause in
/// the city. That is <c>02 §8</c> rule 5, and it is the same failure the Unplaced Pool's draw exists
/// to avoid.
/// </para>
/// <para>
/// <b>128 KB, flat, and not sized from the population.</b> Two <c>int</c> arrays over a Day of
/// Ticks, plus <see cref="CitizenTable.CommuteNext"/>, which was already allocated and read by
/// nothing.
/// </para>
/// </remarks>
public sealed class CommuteRoster
{
    /// <summary>Slot plus one, so a zeroed array reads as <em>empty</em> rather than as slot 0.</summary>
    private readonly int[] _head = new int[Ticks.PerDay];

    private readonly int[] _tail = new int[Ticks.PerDay];

    /// <summary>The list threaded through this roster. Composed at the call site, never stored.</summary>
    private IndexList List(CitizenTable citizens) =>
        new(_head.AsSpan(), _tail.AsSpan(), citizens.CommuteNext);

    /// <summary>
    /// The Tick of the Day <paramref name="id"/> leaves for work on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Uniform inside the window, which is the only shape the corpus can support.</b> A tapered
    /// peak would be a distribution nobody has measured; what <em>has</em> been measured is the
    /// peak's <b>height</b> — S2 R7's 2–3× — and a uniform window reproduces a stated height exactly
    /// (<c>JobRuleset.CommuteWindow</c>). Shape is a second question with its own evidence, and
    /// there is none.
    /// </para>
    /// <para>
    /// <b>Its own <see cref="PurposeTag"/>, and the Tick coordinate is zero.</b> A departure phase is
    /// a property of a person rather than of a moment, so the draw must not move with the clock — a
    /// Citizen whose commute time changed every Day would be re-rolling a decision the design says is
    /// made once (<c>CONTEXT.md</c> → Provider List: <i>"how I get to work is decided when the job is
    /// taken, not every morning"</i>).
    /// </para>
    /// </remarks>
    /// <param name="key">The world seed, as the draw's first coordinate.</param>
    /// <param name="id">The Citizen's monotonic, never-reused id.</param>
    /// <param name="window">How many Ticks of the Day departures spread over.</param>
    public static int PhaseOf(WorldKey key, ulong id, int window)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(window, 1);

        ulong value = Randomness.Draw(
            key, Randomness.Mix(id), Ticks.Zero, PurposeTag.CommuteDeparture);

        return (int)(value % (ulong)(uint)window);
    }

    /// <summary>Rebuilds the whole roster from the Citizens' ids and the Ruleset in force.</summary>
    /// <remarks>
    /// <b>Walked in slot order, which is what makes the result exact</b> — a rebuild has no other
    /// order available, so a derived list claiming to be a pure function of saved state has to be one
    /// a slot-order walk produces.
    /// </remarks>
    public void Rebuild(CitizenTable citizens, WorldKey key, JobRuleset jobs)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        Array.Clear(_head);
        Array.Clear(_tail);
        citizens.CommuteNext.Span.Clear();

        if (!jobs.Runs)
        {
            return;
        }

        IndexList list = List(citizens);
        int window = jobs.CommuteWindow;

        for (int slot = 0; slot < citizens.Rows.SlotCount; slot++)
        {
            if (citizens.Rows.IsLive(slot))
            {
                list.InsertOrdered(PhaseOf(key, citizens.Rows.IdAt(slot), window), slot);
            }
        }
    }

    /// <summary>Puts a Citizen in its departure bucket.</summary>
    public void Add(CitizenTable citizens, WorldKey key, JobRuleset jobs, int citizen)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        if (!jobs.Runs)
        {
            return;
        }

        List(citizens).InsertOrdered(
            PhaseOf(key, citizens.Rows.IdAt(citizen), jobs.CommuteWindow), citizen);
    }

    /// <summary>Takes a Citizen out of its departure bucket, before its row is freed.</summary>
    /// <remarks>
    /// <b>Before the row goes back, because the id is read off it.</b> A Citizen removed after its
    /// row was freed would leave a dangling entry that the next allocation of that slot would find
    /// itself already in — the recycled-slot defect <c>BuildingResidency.Remove</c> carries the same
    /// warning about.
    /// </remarks>
    public void Remove(CitizenTable citizens, WorldKey key, JobRuleset jobs, int citizen)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        if (!jobs.Runs)
        {
            return;
        }

        List(citizens).Remove(
            PhaseOf(key, citizens.Rows.IdAt(citizen), jobs.CommuteWindow), citizen);
    }

    /// <summary>The Citizens leaving for work on one Tick of the Day.</summary>
    /// <param name="citizens">The table whose <c>CommuteNext</c> threads the bucket.</param>
    /// <param name="phase">A Tick of the Day, below <see cref="Ticks.PerDay"/>.</param>
    public IndexListWalk Departing(CitizenTable citizens, int phase)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        return List(citizens).Walk(phase);
    }

    /// <summary>How many Citizens leave on one Tick of the Day.</summary>
    public int CountAt(CitizenTable citizens, int phase)
    {
        ArgumentNullException.ThrowIfNull(citizens);

        return List(citizens).Length(phase);
    }
}
