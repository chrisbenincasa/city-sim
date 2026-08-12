using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// Tick phase 4: Travellers advance along their Legs, Trips end, and every ended Trip is counted
/// before its rows go back.
/// </summary>
/// <remarks>
/// <para>
/// <b>Phase 4 has been an empty method since the Tick was written</b>, and <c>adr/0080</c> is why it
/// is filled now rather than when a generator arrives: nothing here depends on <em>which</em> pairs
/// travel. The cursor, the Fate, the counting and the release are the same machinery for a commanded
/// Trip and for a generated one, and building them against the commanded kind means the generator
/// arrives to a phase that already works rather than to a phase and a second code path.
/// </para>
/// <para>
/// <b>One sweep counts and frees, and Phase 0's own failures flow through it.</b> A Trip can end
/// before it ever moves — <c>adr/0079</c>'s Building with no front door, or an origin and destination
/// with no walkable route between them — and those are resolved where they are discovered, in
/// <c>Simulation.ApplyTrip</c>. They are <em>not</em> counted or freed there. Everything that ends
/// goes back through the sweep below, so there is exactly one place that reads a Fate and exactly one
/// place that releases a row, which is what makes <c>02</c>'s <i>no Trip without a Fate</i> checkable
/// and <c>adr/0006</c> satisfiable in one reading.
/// </para>
/// <para>
/// <b>The Fate reaches the Census before the row does back</b>, which is <c>plans/0021</c> task 2's
/// standing warning: <i>"a completed Trip's Fate must reach the Census before the row is freed, or
/// the only durable record of a failure is gone."</i> The counters are <b>flows</b> rather than
/// levels, on slice 7 task 9's precedent — read as a sum and a peak over the interval, and the
/// reading drains them.
/// </para>
/// <para>
/// <b>What this does not do is attribute volume.</b> <c>adr/0041</c> increments a Segment on entry
/// and decrements on exit, and only for <b>vehicular</b> Legs — <i>"walk Legs still contribute
/// nothing"</i>, because <c>CONTEXT.md</c> → Fidelity keeps pedestrians out of Stress entirely.
/// Milestone 5b resolves walk Legs and no others, so the attribution has nothing to attribute; it is
/// a separate change rather than a line here that would never execute.
/// </para>
/// </remarks>
public sealed class TripEngine
{
    private readonly World _world;

    private RuleFlow _completedFlow;
    private RuleFlow _noRouteFlow;
    private RuleFlow _overBudgetFlow;
    private RuleFlow _strandedFlow;

    /// <param name="world">The world whose Travellers this advances.</param>
    public TripEngine(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
    }

    /// <summary>
    /// Reads the interval's Trip Fates and resets them.
    /// </summary>
    /// <remarks>
    /// <b>The reading drains, and that is what makes these flows rather than levels.</b> A table
    /// counter is a <em>level</em> — how many rows there are — and asking twice gives the same answer.
    /// A Fate is an <em>event</em>: asking twice must not count it twice, so the interval belongs to
    /// whoever reads and the counter belongs to nobody in between.
    /// </remarks>
    public TripActivity Drain()
    {
        var activity = new TripActivity(
            _completedFlow, _noRouteFlow, _overBudgetFlow, _strandedFlow);

        _completedFlow = default;
        _noRouteFlow = default;
        _overBudgetFlow = default;
        _strandedFlow = default;

        return activity;
    }

    /// <summary>
    /// Advances every Traveller whose current Leg has completed, then counts and releases every
    /// Trip that has ended.
    /// </summary>
    public void Advance(Ticks tick)
    {
        AdvanceTravellers(tick);
        ReleaseEnded();
    }

    /// <summary>
    /// Moves each arrived Traveller onto its next Leg, or ends its Trip when there is none.
    /// </summary>
    /// <remarks>
    /// <b>The next Leg's arrival is computed here rather than inside <see cref="TravellerTable.Advance"/>,
    /// and the asymmetry is deliberate.</b> The table's job is the cursor — follow the Leg's own
    /// <c>next</c> and move — and a cost is a question about the Ruleset in force and the Tick, which
    /// a table has no business holding. So the caller peeks the same link the cursor is about to
    /// follow. It is one extra array read and it keeps <c>adr/0075</c>'s split intact: the Leg is the
    /// plan, the Traveller is the cursor, and neither prices anything.
    /// </remarks>
    private void AdvanceTravellers(Ticks tick)
    {
        TravellerTable travellers = _world.Travellers;
        LegTable legs = _world.Legs;
        TripTable trips = _world.Trips;

        for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
        {
            if (!travellers.Rows.IsLive(slot) || travellers.ArrivesAt[slot] > tick)
            {
                continue;
            }

            int current = travellers.CurrentLeg[slot];
            int encoded = legs.Next[current];

            if (encoded != 0)
            {
                travellers.Advance(slot, legs, tick + legs.Time[encoded - 1].ToTicksFloor());
                continue;
            }

            // No further Legs: the plan is exhausted, so the journey is over. Completed is the Fate
            // for *reaching the destination*, and reaching it is what running out of Legs means --
            // adr/0076's rule that a Fate names the journey rather than what happened at the far end.
            if (trips.Rows.TryResolve(travellers.Trip[slot], out int trip))
            {
                trips.Resolve(trip, TripFate.Completed);
            }

            travellers.Rows.Free(travellers.Rows.At(slot));
        }
    }

    /// <summary>
    /// Counts every ended Trip and gives its rows back — the Legs first, then the Trip.
    /// </summary>
    /// <remarks>
    /// <b>Popped rather than walked, because the walk follows links in the rows being released.</b>
    /// <see cref="IndexList.PopFront"/> unlinks before the row is freed, so the list is never read
    /// through a slot that has gone back to the allocator.
    /// </remarks>
    private void ReleaseEnded()
    {
        TripTable trips = _world.Trips;
        LegTable legs = _world.Legs;
        IndexList legList = trips.LegList(legs);

        int completed = 0;
        int noRoute = 0;
        int overBudget = 0;
        int stranded = 0;

        for (int slot = 0; slot < trips.Rows.SlotCount; slot++)
        {
            if (!trips.Rows.IsLive(slot))
            {
                continue;
            }

            var fate = (TripFate)trips.Fate[slot];

            if (fate == TripFate.InFlight)
            {
                continue;
            }

            switch (fate)
            {
                case TripFate.Completed: completed++; break;
                case TripFate.NoRouteFound: noRoute++; break;
                case TripFate.ExceededCommuteBudget: overBudget++; break;
                case TripFate.Stranded: stranded++; break;
                default: break;
            }

            for (int leg = legList.PopFront(slot); leg != Rows.NoSlot; leg = legList.PopFront(slot))
            {
                legs.Rows.Free(legs.Rows.At(leg));
            }

            Release(slot);
        }

        _completedFlow = _completedFlow.Fold(completed);
        _noRouteFlow = _noRouteFlow.Fold(noRoute);
        _overBudgetFlow = _overBudgetFlow.Fold(overBudget);
        _strandedFlow = _strandedFlow.Fold(stranded);
    }

    /// <summary>
    /// Gives a Trip row back, having checked it carries a Fate.
    /// </summary>
    /// <remarks>
    /// <b>The only place a Trip row is freed, and the check is here rather than at the caller so that
    /// it stays that way.</b> <c>02 §10</c>'s <i>no Trip without a Fate</i> — see
    /// <see cref="Invariant.TripHasAFate"/>. The loop above cannot violate it, which is the point: the
    /// guard exists for the second release site, not for the first.
    /// </remarks>
    /// <remarks>
    /// <b>Internal rather than private so the violation can be written.</b> No path through the public
    /// API reaches this with an unresolved Trip — that is what the guard is for — so a test confined to
    /// <see cref="Advance"/> could only assert that the guard never fires, which is a different claim
    /// and is equally true of a guard that is not there.
    /// </remarks>
    internal void Release(int slot)
    {
        _world.Invariants.Require(
            (TripFate)_world.Trips.Fate[slot] != TripFate.InFlight,
            Invariant.TripHasAFate,
            slot);

        _world.Trips.Rows.Free(_world.Trips.Rows.At(slot));
    }
}

/// <summary>
/// What Tick phase 4 did over a Census interval: one flow per <see cref="TripFate"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>All four, including the two milestone 5b cannot produce, and the zeroes are informative.</b>
/// <c>adr/0076</c> closes the Fate set at four, so a counter each is the shape that needs no edit when
/// the missing conditions arrive. <see cref="TripFate.ExceededCommuteBudget"/> is structurally
/// unreachable until the Commute Budget exists as <c>[trips]</c> Ruleset data — milestone 5b-bis,
/// <c>adr/0081</c> — and <see cref="TripFate.Stranded"/> needs a Segment to vanish under a Trip in
/// flight, which <c>CommandKind.Connect</c>'s bulldoze can do today.
/// </para>
/// <para>
/// <b>A reading that shows four zeroes and a reading taken over a Tick with no Trips in it are the
/// same value</b>, which is worth knowing before drawing a conclusion from one: these count Trips that
/// <em>ended</em> in the interval, not Trips that existed.
/// </para>
/// </remarks>
public readonly record struct TripActivity(
    RuleFlow Completed, RuleFlow NoRouteFound, RuleFlow ExceededCommuteBudget, RuleFlow Stranded);
