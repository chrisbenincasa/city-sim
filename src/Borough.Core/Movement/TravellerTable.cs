using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// One Traveller. Empty for the reason <c>Entities.Citizen</c> is empty.
/// </summary>
public readonly struct Traveller;

/// <summary>
/// The Citizens currently on a journey — <b>the cursor executing a Trip's plan</b>: which Leg it is
/// on, and when it arrives.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Traveller is a view, not an owner</b> (<c>CONTEXT.md</c> → Promotion/Demotion), and this
/// table is where that sentence becomes structural. Everything durable about a journey is on the
/// <see cref="TripTable">Trip</see> and the <see cref="LegTable">Leg</see>, which outlive it;
/// everything transient is here, on a row created on demand and released on arrival. <b>No conserved
/// quantity may be added to this table</b> — money, a Need, a Habit — because the row is designed to
/// be destroyed. Session M has already applied the rule once, moving the Habit Route from the
/// Traveller to the Citizen on exactly this ground.
/// </para>
/// <para>
/// <b>The plan/cursor split is what makes <c>03 §4</c> invariant 3 writable</b>, which it has been
/// owed since it was written. Demotion discards the Traveller's <em>cursor</em> state — queue
/// position, headway, a Switch Lane traversal in progress — and never the plan, because the plan is
/// on the Leg and the Leg is not the embodiment. Milestone 6 writes that enumeration against this
/// structure rather than inventing one.
/// </para>
/// <para>
/// <b>The current Leg is a slot rather than an ordinal, and the arrival Tick is whole Ticks rather
/// than <see cref="TravelTime"/>.</b> The first is because the cursor advances by following the
/// Leg's <see cref="LegTable.Next"/> and never by counting. The second is because an arrival is an
/// <em>instant on the clock</em> and <see cref="Ticks"/> is what the clock and the Event Wheel are
/// denominated in — <c>adr/0071</c> keeps the two types unconvertible precisely so that a traversal
/// cost can never be armed into the Wheel by accident.
/// </para>
/// </remarks>
[Table]
public sealed class TravellerTable
{
    private readonly Rows<Traveller> _rows;

    /// <param name="capacity">Initial slot count. In-flight Travellers, not population.</param>
    /// <param name="citizens">The table this one's Citizen handles address.</param>
    /// <param name="trips">The table this one's Trip handles address.</param>
    public TravellerTable(int capacity, CitizenTable citizens, TripTable trips)
    {
        ArgumentNullException.ThrowIfNull(citizens);
        ArgumentNullException.ThrowIfNull(trips);

        _rows = new Rows<Traveller>("traveller", capacity, Buffering.OneCopy);

        // Required on both: a Traveller whose Citizen or Trip has been freed is not a state the
        // design models, it is a leak — the row should have been released when the journey ended.
        // This is the opposite call from the Leg's Addresses, and deliberately so.
        Citizen = _rows.SavedHandle("citizen", citizens.Rows);
        Trip = _rows.SavedHandle("trip", trips.Rows);

        CurrentLeg = _rows.Saved<int>("current_leg", Touch.PerTick);
        CurrentHop = _rows.Saved<int>("current_hop", Touch.PerTick);
        Carry = _rows.Saved<TravelTime>("carry", Touch.PerTick);
        ArrivesAt = _rows.Saved<Ticks>("arrives_at", Touch.PerTick);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Traveller> Rows => _rows;

    /// <summary>Who is travelling. <b>Where every conserved quantity lives.</b></summary>
    public HandleColumn<Entities.Citizen> Citizen { get; }

    /// <summary>The Trip being executed.</summary>
    public HandleColumn<Trip> Trip { get; }

    /// <summary>
    /// The slot of the Leg being travelled, or <see cref="Rows.NoSlot"/> before the first.
    /// </summary>
    public Column<int> CurrentLeg { get; }

    /// <summary>
    /// The slot of the <see cref="RouteHopTable">route hop</see> being crossed, or
    /// <see cref="Rows.NoSlot"/> — <b>which Segment this Traveller is on right now</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This narrows <c>adr/0075</c> and <c>CONTEXT.md</c> → Traveller, both of which say a
    /// Statistical Traveller is <em>nowhere in particular</em> between its endpoints.</b> That was true
    /// while nothing asked; <c>adr/0041</c> asks, because it attributes a Segment's volume <b>on
    /// entry</b> and decrements it on exit, so somebody has to know which Segment. The alternative
    /// considered was smearing a Leg's whole cost across its Segments at planning time, which is cheaper
    /// and cannot answer <i>how many vehicles are on this Segment now</i> — the quantity Stress,
    /// Fidelity and the volume-delay function are all defined over.
    /// </para>
    /// <para>
    /// <b>What survives of <em>nowhere in particular</em> is the <em>position within</em> the Segment.</b>
    /// A Statistical Traveller has no offset along its Segment, no lane and no headway; it is on the
    /// Segment for a dwell time and then on the next one. That is still the whole distinction from
    /// Microscopic, and it is the one <c>adr/0007</c>'s promotion is defined against.
    /// </para>
    /// <para>
    /// <see cref="Rows.NoSlot"/> on a walk Leg, and on a drive Leg whose route has no hops at all —
    /// which is a real journey, not a failure. ***Absent is not zero.***
    /// </para>
    /// </remarks>
    public Column<int> CurrentHop { get; }

    /// <summary>
    /// The sub-Tick remainder owed from everything crossed so far, always less than one Tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Without this a drive is instantaneous, and that is arithmetic rather than taste.</b> An
    /// arrival is an instant on the clock, so each hop's cost has to be floored to whole Ticks — and a
    /// 32-Tile Street at 50 km/h costs <b>0.22 Ticks</b> under the 2048-Tick Day (<c>adr/0071</c>'s own
    /// illustration, restated by <c>adr/0094</c>). Flooring each hop independently would make every
    /// Segment free and a twenty-Segment commute would take no time at all.
    /// </para>
    /// <para>
    /// <b>Carried across Legs as well as hops</b>, so a Trip's realised duration is its parts summed and
    /// floored once rather than floored piecewise. That is what keeps the three Legs of
    /// <c>adr/0008</c>'s <c>walk → drive → walk</c> from being cheaper than the one journey they are.
    /// </para>
    /// </remarks>
    public Column<TravelTime> Carry { get; }

    /// <summary>The Tick at which the current Leg — or the current Segment — completes.</summary>
    public Column<Ticks> ArrivesAt { get; }

    /// <summary>
    /// Puts a Citizen on the road at the head of a Trip's Legs.
    /// </summary>
    public Handle<Traveller> Create(
        Handle<Entities.Citizen> citizen, Handle<Trip> trip, int firstLeg, Ticks arrivesAt)
    {
        Handle<Traveller> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Citizen[slot] = citizen;
        Trip[slot] = trip;
        CurrentLeg[slot] = firstLeg;
        CurrentHop[slot] = Tables.Rows.NoSlot;
        Carry[slot] = TravelTime.Zero;
        ArrivesAt[slot] = arrivesAt;

        return handle;
    }

    /// <summary>
    /// Moves the cursor onto the next Leg of its Trip.
    /// </summary>
    /// <remarks>
    /// <b>Following the Leg's own <c>next</c> rather than re-walking the Trip</b>, which is what
    /// makes advancing O(1) and is the reason the link lives on the element.
    /// </remarks>
    /// <returns><see langword="false"/> when the Trip has no further Legs — the Traveller has arrived.</returns>
    public bool Advance(int slot, LegTable legs, Ticks arrivesAt)
    {
        ArgumentNullException.ThrowIfNull(legs);

        int encoded = legs.Next[CurrentLeg[slot]];

        if (encoded == 0)
        {
            return false;
        }

        CurrentLeg[slot] = encoded - 1;
        CurrentHop[slot] = Tables.Rows.NoSlot;
        ArrivesAt[slot] = arrivesAt;

        return true;
    }
}
