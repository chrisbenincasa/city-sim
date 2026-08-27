using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// The first Trip generator in the project: everybody with a Workplace travels to it once a Day.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what <c>adr/0080</c> was written against.</b> That decision built Phase 4 ahead of any
/// generator, on the ground that nothing in the cursor, the Fate or the Census depends on
/// <em>which</em> pairs travel — so the test of it is whether a generator can arrive without editing
/// any of them. It can: this class creates Trips through <see cref="TripEngine.Start"/> and touches
/// nothing else. <c>TripPurpose.Commanded</c> keeps its rule — <b>nothing downstream branches on the
/// purpose</b> — and the only new thing in Phase 4 is the loop below.
/// </para>
/// <para>
/// <b>The occasion is a <em>phase</em>, not a schedule, and <c>adr/0101</c> keeps that while
/// doubling it.</b> A Citizen leaves home at one Tick of the Day and leaves work at another, and both
/// are computed rather than armed: <see cref="CommuteRoster"/> is two partitions of the population,
/// <c>(derived AND rebuilt)</c>, costing no saved column and no per-Tick re-arm. What it is a
/// partition <em>on</em> has changed — it was the Citizen's id under a uniform window, and it is now
/// the Workplace's Shift start against the Citizen's own planned commute — but it is still a function
/// of saved state rather than an event that fires.
/// </para>
/// <para>
/// <b>The Day has a shape, and nothing here authors one.</b> Two peaks, a baseline through the middle
/// and a quiet night are what a spread of Shift start hours and an unequal spread of Shift lengths
/// look like from outside; there is no departure curve, no window and no peak dial. The morning is
/// sharp because a Workplace's staff share a start hour, and broad because they subtract their own
/// commutes from it; the evening is flatter because they do not share a Shift length. See
/// <c>adr/0101</c>, and <c>CONTEXT.md</c> → Shift.
/// </para>
/// <para>
/// <b>Tick 0 of the Day is midnight</b> (<see cref="Ticks.AtHour"/>). This class used to say the Day
/// began at the peak and that the choice was unobservable, which was true while nothing in the
/// simulation asked the time. A Day with a quiet night in it distinguishes its own ends, so the
/// freedom is spent — deliberately, once, in an ADR rather than in a Ruleset key.
/// </para>
/// <para>
/// <b>The evening leg is here, and the refusal it replaces was reasoning from an absence.</b> The
/// standing argument was that a return journey makes a Citizen's day a <em>schedule</em>, whose shape
/// is decided by <c>adr/0067</c>'s shopping and <c>adr/0032</c>'s school — both <em>unbuilt</em>, and
/// <c>adr/0070</c> says an unbuilt mechanism is not a design constraint, so that is the rule inverted.
/// A return commute is in any case the one journey a later generator cannot reshape: its endpoints are
/// fixed and already stored, and its occasion is the end of a Shift.
/// </para>
/// <para>
/// <b>Nobody can be in flight when their next departure comes round, and it is still the loader that
/// guarantees it — but the guarantee is now stated rather than accidental.</b> Under one journey a Day
/// the gap between departures was a whole Day and the Commute Budget bounded a journey in minutes, so
/// the overlap was arithmetically unreachable and nobody had to say so. With two journeys the gap is
/// the <b>Shift length</b>, so <c>RulesetLoader</c> refuses a minimum Shift at or below the Commute
/// Budget's ceiling. ⚠ <b>The old property was a happy accident of there being one journey and it read
/// as a design property</b>, which is the shape worth remembering: an invariant nothing enforces
/// survives exactly as long as the structure that made it free.
/// </para>
/// </remarks>
public sealed class CommuteEngine
{
    private readonly World _world;
    private readonly TripEngine _trips;

    /// <param name="world">The world whose Citizens commute.</param>
    /// <param name="trips">The one door a Trip is created through.</param>
    public CommuteEngine(World world, TripEngine trips)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(trips);

        _world = world;
        _trips = trips;
    }

    /// <summary>Starts the commute of everybody whose departure phase is this Tick of the Day.</summary>
    /// <remarks>
    /// <para>
    /// <b>Two refusals, and both are silence rather than an exception.</b> A Ruleset with no
    /// <c>[jobs]</c> has no employment and therefore no commute; one with no <c>[trips]</c> has no
    /// crossing cost, and <see cref="TripEngine.Start"/> would price every junction at zero. Neither
    /// is a defect in a Ruleset — <c>rulesets/minimal.toml</c> was both for six slices — so a
    /// generator that threw would make an ordinary file unloadable. The refusal that <em>is</em> loud
    /// lives at the loader: <c>[jobs]</c> without a Commute Budget is rejected outright.
    /// </para>
    /// <para>
    /// <b>Walked in bucket order, which is slot order, which is why replay reproduces it.</b>
    /// <see cref="IndexList.InsertOrdered"/> keeps each bucket ascending by slot however it was
    /// filled, so the Trips of one Tick are created in the same sequence on a rebuild as on the run
    /// that saved it — and Trip ids, which the State Hash folds, come out the same.
    /// </para>
    /// </remarks>
    public void Generate(Ticks tick)
    {
        if (!_world.Rules.Jobs.Runs || !_world.Rules.Trips.Runs)
        {
            return;
        }

        int phase = (int)(tick.Raw % Ticks.PerDay);
        CitizenTable citizens = _world.Citizens;

        // Outbound before homeward, and the order is arbitrary but must be fixed: both walk in slot
        // order and both create Trips, so swapping them renumbers every Trip id the State Hash folds
        // on any Tick that carries both. It carries both often -- an early shift's return and a late
        // shift's departure share the middle of the Day, which is the all-day baseline this design
        // exists to produce.
        foreach (int citizen in _world.Commutes.Departing(citizens, phase))
        {
            Travel(citizen, homeward: false, tick);
        }

        foreach (int citizen in _world.Commutes.Returning(citizens, phase))
        {
            Travel(citizen, homeward: true, tick);
        }
    }

    /// <summary>Starts one leg of one Citizen's commute, in whichever direction.</summary>
    /// <remarks>
    /// <b>One method for both directions rather than two loops that drifted.</b> The endpoints are the
    /// same pair read in the opposite order, and every other term -- the mode, the purpose, the
    /// refusals -- is identical, so a second copy would be two places for the Commute Budget to be
    /// applied differently.
    /// </remarks>
    private void Travel(int citizen, bool homeward, Ticks tick)
    {
        CitizenTable citizens = _world.Citizens;

        // Two hops as of milestone 27 task 7: a Workplace is a Business, and a Business sits in
        // premises. An employer with no premises has nowhere to travel to -- a founder before
        // placement (adr/0146, adr/0147) -- and that is a Citizen with a job and no journey rather
        // than a defect.
        if (!_world.Businesses.Rows.TryResolve(citizens.Workplace[citizen], out int employer)
            || !_world.Buildings.Rows.TryResolve(
                _world.Businesses.Building[employer], out int workplace))
        {
            return;
        }

        int home = HomeOf(citizen);

        // A Citizen with a job and no home. Not a defect and not a Trip: the Unplaced Pool holds
        // Households that have nowhere to live, and adr/0069 makes housing them a mechanism of its
        // own that runs at its own pace. Walking from nowhere is not the honest degradation.
        if (home < 0)
        {
            return;
        }

        TravelMode mode = _world.ModeOf(citizen);
        (int origin, int destination) = homeward ? (workplace, home) : (home, workplace);

        // Set BEFORE Start rather than after, and it is not a style choice: every refusal inside
        // Start resolves through World.ResolveTrip on the spot, so a journey that is never made
        // has already been read back and reverted by the time Start returns. Writing it afterwards
        // would stamp "travelling" onto somebody standing still.
        citizens.Activity[citizen] = (byte)(homeward
            ? CitizenActivity.TravellingHome
            : CitizenActivity.TravellingToWork);

        _trips.Start(citizen, origin, destination, mode, TripPurpose.Commute, tick);
    }

    /// <summary>The Building a Citizen lives in, or <c>-1</c> if their Household is unplaced.</summary>
    private int HomeOf(int citizen)
    {
        if (!_world.Households.Rows.TryResolve(
                _world.Citizens.HouseholdOf[citizen], out int household))
        {
            return -1;
        }

        return _world.Buildings.Rows.TryResolve(
            _world.Households.Dwelling[household], out int building)
            ? building
            : -1;
    }
}
