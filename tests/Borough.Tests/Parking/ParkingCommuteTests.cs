using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Parking;

/// <summary>
/// <b>A car commute in a city that parks — the acceptance test for milestone 7 task 5.</b>
/// </summary>
/// <remarks>
/// <para>
/// Task 4 built the acquire and the release and nothing called them. This is the test that says the
/// wiring exists, and it runs a whole city rather than a fixture for that reason: the claim is not
/// <em>the method works</em> — <c>ParkingHoldTests</c> already holds that — it is <em>a Trip reaches
/// it</em>. ⚠ <b>A mechanism whose only caller is its test is a claim about the build that no run
/// holds</b>, and until this file existed that is what the parking pair was.
/// </para>
/// <para>
/// <b><c>rulesets/minimal.toml</c> plus <c>car_ownership_percent</c>, which is
/// <see cref="Movement.CarRouteLengthTests"/>'s fixture and is reused rather than restated.</b> The
/// shipped file already declares <c>parking = 8</c> on its dwelling and <c>[parking] radius_metres =
/// 400</c>, so supply and reach are both real; what it does not declare is car ownership, which is why
/// the golden session's hash does not move for any of this.
/// </para>
/// </remarks>
public sealed class ParkingCommuteTests
{
    private const int Ticks = 4_096;

    /// <summary>
    /// <b>Somebody drives, parks, and is recorded as holding the space they took.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The assertion is on the <em>pair</em> across a whole run, exactly as
    /// <c>ParkingHoldTests</c> asserts it at one write site: summed occupancy against the number of
    /// Citizens whose holding resolves. That equality is <c>adr/0084</c>'s conservation sum with the
    /// operand this milestone corrected — <b>Citizens</b>, not Travellers, so a car parked overnight
    /// with no journey in flight still counts on both sides.
    /// </para>
    /// <para>
    /// ⚠ <b>Task 5 wrote that sum out here by hand and task 6 made it
    /// <see cref="Invariant.ParkingOccupancyIsConserved"/>, so the loop is gone and the tier is
    /// asked instead.</b> A test holding its own copy of an invariant's arithmetic is
    /// <c>plans/0012</c> <i>Cause 1</i> with the drift pointed at the suite: the two would go on
    /// agreeing right up until one operand was corrected in one of them.
    /// </para>
    /// <para>
    /// <b>What stays here is the half the invariant must never make — that anybody parked at all.</b>
    /// The sum balances perfectly in a city where nothing ever happens, so an invariant asserting
    /// liveness would fire on every legitimately empty world, and a test that only ran the invariant
    /// would pass on one. ***A conservation check cannot also be a coverage check***, which is why
    /// the two claims sit on either side of this line.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_driving_city_parks_its_cars_and_the_two_sides_agree()
    {
        World world = Run(ownership: 100);

        Assert.True(Occupied(world) > 0, "nobody parked anywhere, so the wiring is not reached at all.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>Nobody parks in a city where nobody drives</b>, which is what says the count above is the
    /// parking and not the weather.
    /// </summary>
    /// <remarks>
    /// The control. <c>minimal.toml</c> declares the supply and the radius but no ownership, so the
    /// same world with <c>car_ownership_percent = 0</c> has every Car Park empty — and that is the
    /// reason the golden session's State Hash is untouched by this milestone so far.
    /// </remarks>
    [Fact]
    public void A_walking_city_leaves_every_car_park_empty()
    {
        World world = Run(ownership: 0);

        Assert.Equal(0, Occupied(world));
        Assert.Equal(0, Holders(world));
    }

    /// <summary>Summed occupancy over every live Car Park.</summary>
    private static int Occupied(World world)
    {
        int occupied = 0;

        for (int slot = 0; slot < world.CarParks.Rows.SlotCount; slot++)
        {
            if (world.CarParks.Rows.IsLive(slot))
            {
                occupied += world.CarParks.Occupied[slot];
            }
        }

        return occupied;
    }

    /// <summary>
    /// <b>The walk <em>to</em> the car costs something — <c>waypoints[0] → waypoints[1]</c>.</b>
    /// </summary>
    /// <remarks>
    /// A driver walks to wherever they last parked, which after their first journey is a real Car Park
    /// and is often not their own Building's. This is the origin half of the swap.
    /// </remarks>
    [Fact]
    public void The_walk_to_the_car_costs_something()
    {
        Assert.True(
            FlankingWalks(before: true) > 0,
            "no foot Leg leading into a drive ever spanned two Addresses, so a driver never walks to "
            + "their car and waypoints[1] is still the Building's own kerb.");
    }

    /// <summary>
    /// <b>The walk <em>from</em> the car costs something — <c>waypoints[2] → waypoints[3]</c>.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This is a separate test because one covering both passed with the destination swap
    /// reverted.</b> The first cut counted any non-trivial foot Leg, and every one it found was the
    /// walk <em>to</em> the car: a driver who parked in a neighbour's Car Park yesterday walks to it
    /// today whatever <c>waypoints[2]</c> says, because <see cref="TripEngine"/> takes the space at
    /// the Leg boundary either way. ***A test over both ends of a swap is passed by either end***, and
    /// reverting the destination endpoint is what showed it — the mutation survived.
    /// </para>
    /// <para>
    /// So this walks the Leg chain and looks only at a foot Leg that <em>follows</em> a drive.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_walk_from_the_car_costs_something()
    {
        Assert.True(
            FlankingWalks(before: false) > 0,
            "no foot Leg following a drive ever spanned two Addresses, so every driver arrives at "
            + "their destination's own door and waypoints[2] is not the Car Park they took.");
    }

    /// <summary>
    /// Non-trivial foot Legs adjacent to a drive, on the named side of it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It samples every Tick, because a Leg does not outlive its Trip.</b> The first cut of this
    /// walked <c>world.Legs</c> after the run and found <b>zero</b> Legs of either mode — not zero
    /// non-trivial walks, zero Legs — because <c>TripEngine.Release</c> frees them as Trips resolve. It
    /// would have passed had it asserted the absence it was actually looking at. ***A table emptied by
    /// the mechanism under test cannot be read after the mechanism has run.***
    /// </remarks>
    private static int FlankingWalks(bool before)
    {
        int found = 0;

        Run(ownership: 100, world =>
        {
            LegTable legs = world.Legs;

            for (int slot = 0; slot < legs.Rows.SlotCount; slot++)
            {
                if (!legs.Rows.IsLive(slot) || (TravelMode)legs.Mode[slot] != TravelMode.Car)
                {
                    continue;
                }

                int walk = before ? PrecedingLeg(legs, slot) : legs.Next[slot] - 1;

                if (walk < 0
                    || !legs.Rows.IsLive(walk)
                    || (TravelMode)legs.Mode[walk] != TravelMode.Foot)
                {
                    continue;
                }

                if (legs.From(walk, world.Roads.Segments) != legs.To(walk, world.Roads.Segments))
                {
                    found++;
                }
            }
        });

        return found;
    }

    /// <summary>The Leg whose <c>Next</c> is <paramref name="target"/>, or <c>-1</c>.</summary>
    /// <remarks>
    /// A Leg list is singly linked — <c>adr/0075</c>, and a back pointer would be a second copy of the
    /// order — so the Leg before one is found by looking rather than by reading.
    /// </remarks>
    private static int PrecedingLeg(LegTable legs, int target)
    {
        for (int slot = 0; slot < legs.Rows.SlotCount; slot++)
        {
            if (legs.Rows.IsLive(slot) && legs.Next[slot] - 1 == target)
            {
                return slot;
            }
        }

        return -1;
    }

    /// <summary>How many Citizens hold a Car Park that still resolves.</summary>
    private static int Holders(World world)
    {
        int held = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.CarParks.Rows.TryResolve(world.Citizens.ParkedIn[slot], out _))
            {
                held++;
            }
        }

        return held;
    }

    /// <summary><c>minimal.toml</c> with ownership dialled, run to <see cref="Ticks"/>.</summary>
    /// <param name="ownership"><c>car_ownership_percent</c>.</param>
    /// <param name="each">
    /// Called after every Tick, for facts that do not survive to the end of the run.
    /// </param>
    private static World Run(int ownership, Action<World>? each = null)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        RulesetLoadResult parsed = RulesetLoader.Parse(
            $"{toml}\n[households]\ncar_ownership_percent = {ownership}\n", "test.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(4_000), GoldenFixtures.RulesetHash);

        builder.Append(new Core.Quantities.Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, parsed.Ruleset!);

        // O(world) twice per Tick against a phase meant to be O(woken) -- CarRouteLengthTests' reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

        if (each is null)
        {
            Replay.Trace(simulation, log, new Core.Quantities.Ticks(Ticks), 64, []);
        }
        else
        {
            // The same TickInput Replay.Trace builds. Stepping with `default` instead delivers no
            // commands at all, so Populate never fires and the sample walks an empty world -- which
            // reads exactly like "the mechanism produced nothing".
            for (int tick = 0; tick < Ticks; tick++)
            {
                var input = new TickInput(log.At(simulation.Tick), log.RulesetHashAt(simulation.Tick));

                simulation.Step(input);
                each(simulation.World);
            }
        }

        return simulation.World;
    }
}
