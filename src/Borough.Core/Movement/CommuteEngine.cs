using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// The first Trip generator in the project: everybody with a Workplace walks to it once a Day.
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
/// <b>The occasion is a <em>phase</em>, not a schedule, and that is this task's one design
/// decision.</b> A commute recurs every Day; <see cref="EventWheel.Size"/> is exactly a Day. So a
/// Citizen armed on the Wheel re-arms at <c>+8192</c> for ever and never leaves the bucket it started
/// in, which makes the bucket a partition of the population by a constant — and a partition on a
/// constant is <b>derivable</b>. <see cref="CommuteRoster"/> is that partition,
/// <c>(derived AND rebuilt)</c>, costing one saved column fewer, one per-Tick re-arm fewer, and no
/// generalisation of the Wheel to a second table. <c>adr/0081</c> says generalising the Wheel is not
/// required for this milestone; the reciprocity above is why it would not have helped.
/// </para>
/// <para>
/// <b>Departures spread uniformly over a window, and the window is the peak seen from the other
/// side.</b> Under a uniform window of <c>W</c> Ticks the instantaneous departure rate is
/// <c>TICKS_PER_DAY ÷ W</c> times the daily average, so the peaking multiplier S2 R7 measured
/// (2–3× over the Day mean) <b>is</b> <c>TICKS_PER_DAY ÷ W</c> — one number seen from two sides. The
/// Ruleset therefore authors the multiplier, which is the side that has evidence, and
/// <see cref="JobRuleset.CommuteWindow"/> derives the window. That is <c>adr/0059</c> a fourth time:
/// <b>state the thing a designer has a reason for and derive the thing the engine needs.</b>
/// </para>
/// <para>
/// <b>The Day begins at the peak, and no offset key is opened.</b> Tick zero of the Day is the first
/// departure Tick. An offset would be a second hash-bearing number whose only consumer is a clock
/// nothing else reads: no Rule, no Layer and no Zone Rule asks what time of day it is, so <em>when</em>
/// the peak falls within the Day is unobservable and choosing it would be <c>adr/0052</c>'s
/// prohibition exactly — a number with no ratifier and no consequence.
/// </para>
/// <para>
/// <b>The evening leg is deliberately absent.</b> <c>plans/0023</c> scopes this to one Workplace and
/// one Trip a Day. A return journey makes a Citizen's day a <em>schedule</em>, and a schedule is what
/// arrives when <c>adr/0067</c>'s shopping or <c>adr/0032</c>'s school gives it a second entry — so
/// building half of it now is building a structure whose shape is decided by mechanisms that do not
/// exist (<c>adr/0070</c>).
/// </para>
/// <para>
/// <b>Nobody can be in flight when their next departure comes round, and it is the loader that
/// guarantees it.</b> A Citizen departs once per Day. A Trip that is not <see cref="TripFate.InFlight"/>
/// on creation never gets a Traveller at all, and one that is has passed the Commute Budget — which
/// <c>RulesetLoader</c> refuses to leave unstated wherever <c>[jobs]</c> is present. So an in-flight
/// commute is bounded by the Budget, the Budget is stated in minutes, and a Day is 24 in-world hours:
/// the overlap this class would otherwise have to guard against is arithmetically unreachable rather
/// than merely unlikely.
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
        JobRuleset jobs = _world.Rules.Jobs;

        if (!jobs.Runs || !_world.Rules.Trips.Runs)
        {
            return;
        }

        int phase = (int)(tick.Raw % Ticks.PerDay);

        if (phase >= jobs.CommuteWindow)
        {
            return;
        }

        CitizenTable citizens = _world.Citizens;

        foreach (int citizen in _world.Commutes.Departing(citizens, phase))
        {
            if (!_world.Buildings.Rows.TryResolve(citizens.Workplace[citizen], out int workplace))
            {
                continue;
            }

            int home = HomeOf(citizen);

            // A Citizen with a job and no home. Not a defect and not a Trip: the Unplaced Pool holds
            // Households that have nowhere to live, and adr/0069 makes housing them a mechanism of its
            // own that runs at its own pace. Walking from nowhere is not the honest degradation.
            if (home < 0)
            {
                continue;
            }

            _trips.Start(
                citizen,
                _world.PedestrianAccessPoint(home),
                _world.PedestrianAccessPoint(workplace),
                TripPurpose.Commute,
                tick);
        }
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
