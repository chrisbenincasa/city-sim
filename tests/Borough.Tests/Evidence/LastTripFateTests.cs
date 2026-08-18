using Borough.Core.Determinism;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Core.Tables;
using Borough.Tests.Golden;

namespace Borough.Tests.Evidence;

using Evidence = Borough.Core.Evidence.Evidence;
using CitizenEvidence = Borough.Core.Evidence.CitizenEvidence;

/// <summary>
/// Milestone 6 task 7: a Trip's Fate outlives its Trip, because the Citizen who made the journey
/// outlives it too.
/// </summary>
/// <remarks>
/// <para>
/// <b>What was wrong.</b> <c>02 §9</c>'s Citizen row asks for <i>"current or last Trip with its
/// Fate"</i>. <c>TripEngine.Release</c> asserts the Trip carries a Fate and frees the row on the next
/// line, and <c>AdvanceTravellers</c> frees the <b>Traveller</b> — which holds the only
/// Citizen-to-Trip link there is — earlier in the same pass. So the Fate was computed, checked, and
/// destroyed together with any way of saying whose journey it had been.
/// </para>
/// <para>
/// ⚠ <b>The milestone scoped this as <i>"task 2's situation verbatim"</i> and it is not.</b> Task 2
/// copies a condemnation into a trail because its subject — the Building — is destroyed, so there is
/// no entity left to hang the fact on; that is the milestone's own D3 argument. Here the subject is
/// the <b>Citizen</b>, who outlives the journey by design, and <c>02 §9</c> asks the question
/// <em>of a Citizen</em>. ***What is freed is not always the subject, and it is the subject that
/// decides the shape.*** A trail would also not have scaled: a commute is two journeys a Day, so a
/// million Citizens end roughly a thousand Trips <em>per Tick</em> and a 256-entry window would cover
/// a quarter of one.
/// </para>
/// <para>
/// ⚠ <b><c>TripFate.Stranded</c> is produced by no production site in the build</b>, which is why
/// <see cref="Every_fate_the_engine_produces_reaches_the_citizen"/> covers three Fates and
/// <see cref="A_fate_the_engine_cannot_produce_still_reaches_the_citizen"/> covers the fourth through
/// the door. 5b-bis task 8 read <c>trips stranded</c> as 0 over 100,000 Ticks and filed it as
/// unexercised; it is stronger than that — <c>TripEngine.cs:563</c> says in its own comment that the
/// Fate <i>"is a question for whoever builds <c>TripFate.Stranded</c>"</i>. <c>adr/0070</c>'s
/// <b>unbuilt</b> class, and the door is tested rather than the absent producer.
/// </para>
/// </remarks>
public sealed class LastTripFateTests
{
    /// <summary>
    /// <b>Every Fate the engine can produce lands on the Citizen who made the journey.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The load-bearing test, and it is a <c>[Theory]</c> because <b>one Fate proves nothing about the
    /// others</b>: the three are written at three structurally different places, two inside
    /// <c>TripEngine.Start</c> before a Traveller exists and one in <c>AdvanceTravellers</c> after it
    /// has been freed. A mechanism wired at one of the three passes a single-Fate test.
    /// </para>
    /// <para>
    /// <b>Each row drives the engine rather than the door</b> — a real <c>TripEngine.Start</c> on a
    /// world configured to produce that Fate — so what is under test is that every path <em>reaches</em>
    /// the door, which is the thing a missed call site breaks and the thing a door-level test cannot
    /// see.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(TripFate.Completed)]
    [InlineData(TripFate.NoRouteFound)]
    [InlineData(TripFate.ExceededCommuteBudget)]
    public void Every_fate_the_engine_produces_reaches_the_citizen(TripFate expected)
    {
        (Simulation simulation, int citizen) = CityProducing(expected);

        Assert.Equal(expected, LastFateOf(simulation.World, citizen));
    }

    /// <summary>
    /// <b>The door records a Fate the engine cannot yet produce</b>, so the column is complete over
    /// the Fate set rather than over today's producers.
    /// </summary>
    /// <remarks>
    /// <c>TripFate.Stranded</c> has a doc-comment saying exactly when it applies, a Census counter and
    /// no writer — <c>adr/0070</c>'s <b>unbuilt</b>, not <b>refused</b>. Asserting the door handles it
    /// is what stops the day it is built from also being the day somebody discovers this column drops
    /// it. ***A closed set with an unbuilt member is still a closed set, and the store has to cover
    /// the set rather than the producers.***
    /// </remarks>
    [Fact]
    public void A_fate_the_engine_cannot_produce_still_reaches_the_citizen()
    {
        (Simulation simulation, int citizen) = CityProducing(TripFate.Completed);

        simulation.World.RecordTripFate(citizen, TripFate.Stranded);

        Assert.Equal(TripFate.Stranded, LastFateOf(simulation.World, citizen));
    }

    /// <summary>
    /// <b>A Citizen who has never finished a journey reports no last Trip at all</b>, rather than a
    /// Fate that reads as an outcome.
    /// </summary>
    /// <remarks>
    /// <c>TripFate.InFlight</c> is the stored sentinel, and it is free rather than chosen: that enum
    /// reserves zero for <i>the Trip has not ended</i> precisely so a row nothing has written cannot
    /// read back as an outcome, and a freshly allocated Citizen is zero-filled. ***A sentinel outside
    /// the range of legitimate answers is the one kind that can announce itself*** — the rule
    /// <c>adr/0074</c>'s crossing cost and <c>adr/0098</c>'s ownership rate both had to reach for.
    /// <b>Asserted at Tick 0 before any Trip</b>, because after one the column is legitimately set and
    /// the sentinel would be untestable.
    /// </remarks>
    [Fact]
    public void A_citizen_who_has_never_travelled_has_no_last_trip()
    {
        World world = Populated(GoldenFixtures.Rules()).World;

        int citizen = FirstLiveCitizen(world);

        Assert.Equal(TripFate.InFlight, (TripFate)world.Citizens.LastTripFate[citizen]);
        Assert.Null(Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).LastTrip);
    }

    /// <summary>
    /// <b>A second journey overwrites the first</b>, which is what <em>last</em> means.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The column is not a history and the test says so in the direction that could go wrong. A
    /// mechanism that wrote only the <em>first</em> Fate — an <c>if (fate == InFlight)</c> guard put in
    /// to protect the sentinel — would pass every other test in this file, because every other test
    /// makes one journey.
    /// </para>
    /// <para>
    /// <b>The two Fates differ</b>, so the assertion cannot be satisfied by a write that did not
    /// happen. A pair of <c>Completed</c>s would read identically whether the second write landed or
    /// not.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_last_fate_is_the_last_one_and_not_the_first()
    {
        (Simulation simulation, int citizen) = CityProducing(TripFate.NoRouteFound);

        Assert.Equal(TripFate.NoRouteFound, LastFateOf(simulation.World, citizen));

        simulation.World.RecordTripFate(citizen, TripFate.Completed);

        Assert.Equal(TripFate.Completed, LastFateOf(simulation.World, citizen));
    }

    /// <summary>
    /// <b>The Day is the Day it ended on</b>, and it is Days rather than Ticks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CitizenTable.LastTripEndedDay</c> carries why: <c>Ticks</c> is a <c>ulong</c>, so a
    /// Tick-denominated column is 8 MB at a million Citizens against 2 MB here, for a field no code
    /// path from <c>step()</c> reads. What it costs is within-Day resolution.
    /// </para>
    /// <para>
    /// <b>Read at two different Days rather than one</b>, because a mechanism that stored a constant —
    /// zero, or the Day the world was created — agrees with a single reading taken on Day 0 and with
    /// half of any single reading taken later. The floor is asserted rather than the exact Tick, which
    /// is the whole of what the denomination buys and the whole of what it gives up.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_day_is_the_day_the_journey_ended()
    {
        World world = Populated(GoldenFixtures.Rules()).World;

        int citizen = FirstLiveCitizen(world);

        world.RecordTripFate(citizen, TripFate.Completed);

        Assert.Equal(0, world.Citizens.LastTripEndedDay[citizen]);

        Advance(world, Ticks.PerDay * 3);
        world.RecordTripFate(citizen, TripFate.Completed);

        Assert.Equal(3, world.Citizens.LastTripEndedDay[citizen]);
        Assert.Equal(
            3, Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).LastTrip!.Value.EndedDay);
    }

    /// <summary>
    /// <b>The count stops at its width instead of wrapping</b> (<c>adr/0003</c>).
    /// </summary>
    /// <remarks>
    /// <c>ReachFailures</c>' argument and its unreachability: 65,535 Days against a campaign of 562,
    /// so no run can reach this and it is asserted directly because nothing else can assert it at all.
    /// A wrapped Day would date the most recent journey in the city to its founding, which is the one
    /// failure that reads as the mechanism working.
    /// </remarks>
    [Fact]
    public void The_day_saturates_rather_than_wrapping()
    {
        World world = Populated(GoldenFixtures.Rules()).World;

        int citizen = FirstLiveCitizen(world);

        Advance(world, (long)ushort.MaxValue * Ticks.PerDay);
        world.RecordTripFate(citizen, TripFate.Completed);

        Assert.Equal(ushort.MaxValue, world.Citizens.LastTripEndedDay[citizen]);

        Advance(world, Ticks.PerDay * 2);
        world.RecordTripFate(citizen, TripFate.Completed);

        Assert.Equal(ushort.MaxValue, world.Citizens.LastTripEndedDay[citizen]);
    }

    /// <summary>
    /// <b>The current Trip and the last Trip are different members and neither stands in for the
    /// other.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §9</c> asks for <i>"current <b>or</b> last"</i> and the build answers both, which is more
    /// than was asked and is what stops a panel having to guess which one it got. The discriminating
    /// case is somebody who is travelling <em>and</em> has travelled before: a single field would have
    /// to choose, and choosing wrongly is invisible.
    /// </remarks>
    [Fact]
    public void Somebody_mid_journey_who_has_travelled_before_has_both()
    {
        (Simulation simulation, int citizen) = CityProducing(TripFate.Completed);

        // A journey long enough to still be running when the Tick ends, which a same-Cell walk is
        // not: adr/0071 makes a short walk sub-Tick, so it completes on the Tick it departed and
        // there would be nothing in flight to find.
        Assert.True(
            StartLongTrip(simulation, citizen),
            "no Trip in this world outlasts the Tick it began on.");

        World world = simulation.World;
        CitizenEvidence evidence = Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen));

        Assert.NotNull(evidence.Trip);
        Assert.Equal(TripFate.InFlight, evidence.Trip!.Value.Fate);

        Assert.NotNull(evidence.LastTrip);
        Assert.Equal(TripFate.Completed, evidence.LastTrip!.Value.Fate);
    }

    private static TripFate LastFateOf(World world, int citizen) =>
        (TripFate)world.Citizens.LastTripFate[citizen];

    /// <summary>
    /// A world whose engine has just produced <paramref name="fate"/> for the returned Citizen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three explicit builders rather than one parameterised path</b>, because the three Fates are
    /// reached by three genuinely different worlds and a helper that papered over that would be
    /// asserting the door three times and the wiring never.
    /// </para>
    /// <para>
    /// ⚠ <b><c>NoRouteFound</c> needs a world with no Road Graph, which the generated city cannot
    /// supply.</b> Milestone 6 task 4 measured it: <b>0 of 150</b> vacant Lots in a generated city
    /// lack frontage, because <c>RoadGenerator</c> lays the lattice the Lots are carved from — so
    /// every Building there has a real Access Point and <c>adr/0079</c>'s hole is unreachable. The
    /// hand-built world has Lots and Buildings and no Segments at all, which is that hole exactly:
    /// <c>World.PedestrianAccessPoint</c> returns <c>Address.None</c> and <c>Start</c> refuses on its
    /// first branch. ***A branch unreachable in a generated city is reachable in a hand-built one, and
    /// which of the two you use is a statement about what you are testing.***
    /// </para>
    /// </remarks>
    private static (Simulation Simulation, int Citizen) CityProducing(TripFate fate) => fate switch
    {
        TripFate.Completed => Completed(),
        TripFate.NoRouteFound => NoRoute(),
        TripFate.ExceededCommuteBudget => OverBudget(),
        _ => throw new ArgumentOutOfRangeException(
            nameof(fate),
            fate,
            "no production site in the build writes this Fate. See the file's remarks."),
    };

    /// <summary>A journey that runs to its end, in the generated city.</summary>
    private static (Simulation Simulation, int Citizen) Completed()
    {
        Simulation simulation = Populated(GoldenFixtures.Rules());
        World world = simulation.World;

        int citizen = FirstLiveCitizen(world);
        int home = HomeOf(world, citizen);

        Assert.Equal(
            TripFate.InFlight,
            simulation.Trips.Start(
                citizen, home, FarBuilding(world, home), TravelMode.Foot, TripPurpose.Commanded,
                world.Tick));

        // Stepped until the journey ends rather than once, because how many Ticks a walk across this
        // city takes is a property of the fixture's geometry and not of what is under test -- and a
        // single step passed on the first draft only because the walk happened to be short.
        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(TickInput.Empty);

            if ((TripFate)world.Citizens.LastTripFate[citizen] != TripFate.InFlight)
            {
                return (simulation, citizen);
            }
        }

        Assert.Fail($"no journey of Citizen {citizen}'s ended inside a whole Day.");

        return (simulation, citizen);
    }

    /// <summary>A journey with no road under it, in a world that has none.</summary>
    private static (Simulation Simulation, int Citizen) NoRoute()
    {
        Ruleset ruleset = new(
            resources: [],
            rules: [],
            kinds: [new KindDefinition(0, 0, 0, 0) { Occupants = 1 }],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],
            zoneRules: []);

        var world = new World(64, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xE71D_E0CE_0000_0007UL));

        Handle<Building> home = world.CreateBuilding(
            world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1), kind: 0, Ticks.Zero,
            simulation.Key);

        Handle<Building> away = world.CreateBuilding(
            world.Lots.Create(new Tiles(4), new Tiles(0), zone: 1), kind: 0, Ticks.Zero,
            simulation.Key);

        int citizen = world.Citizens.Rows.Resolve(
            world.CreateCitizen(world.CreateHousehold(home, lifeStage: 0), Ticks.Zero));

        Assert.Equal(
            TripFate.NoRouteFound,
            simulation.Trips.Start(
                citizen,
                world.Buildings.Rows.Resolve(home),
                world.Buildings.Rows.Resolve(away),
                TravelMode.Foot,
                TripPurpose.Commanded,
                world.Tick));

        return (simulation, citizen);
    }

    /// <summary>A journey the Commute Budget refuses before anybody sets off.</summary>
    private static (Simulation Simulation, int Citizen) OverBudget()
    {
        // Three, which is the tightest ceiling the loader will accept: three strictly increasing
        // rungs of at least a minute put the floor there. adr/0095, and ReachFailureTests turns the
        // same lever for the same reason.
        Simulation simulation = Populated(WithCeiling(3));
        World world = simulation.World;

        int citizen = FirstLiveCitizen(world);
        int home = HomeOf(world, citizen);

        Assert.Equal(
            TripFate.ExceededCommuteBudget,
            simulation.Trips.Start(
                citizen, home, FarBuilding(world, home), TravelMode.Foot, TripPurpose.Commanded,
                world.Tick));

        return (simulation, citizen);
    }

    private static bool StartLongTrip(Simulation simulation, int citizen)
    {
        World world = simulation.World;
        int home = HomeOf(world, citizen);

        return simulation.Trips.Start(
            citizen, home, FarBuilding(world, home), TravelMode.Foot, TripPurpose.Commanded,
            world.Tick) == TripFate.InFlight;
    }

    private static int FirstLiveCitizen(World world)
    {
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Households.Rows.TryResolve(world.Citizens.HouseholdOf[slot], out int household)
                && world.Buildings.Rows.IsValid(world.Households.Dwelling[household]))
            {
                return slot;
            }
        }

        Assert.Fail("no Citizen in this world lives anywhere, so no journey has an origin.");

        return Rows.NoSlot;
    }

    private static int HomeOf(World world, int citizen)
    {
        int household = world.Households.Rows.Resolve(world.Citizens.HouseholdOf[citizen]);

        return world.Buildings.Rows.Resolve(world.Households.Dwelling[household]);
    }

    /// <summary>The live Building furthest from <paramref name="from"/> by slot, which is far enough.</summary>
    private static int FarBuilding(World world, int from)
    {
        for (int slot = world.Buildings.Rows.SlotCount - 1; slot >= 0; slot--)
        {
            if (slot != from && world.Buildings.Rows.IsLive(slot))
            {
                return slot;
            }
        }

        Assert.Fail("this world holds one Building, so no journey has a destination.");

        return Rows.NoSlot;
    }

    private static void Advance(World world, long ticks)
    {
        world.Clock.Tick[0] = new Ticks(world.Tick.Raw + (ulong)ticks);
    }

    private static Simulation Populated(Ruleset rules)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        Simulation simulation = Replay.Start(builder.Build(), rules);

        simulation.VerifyDecideWritesNothing = false;

        simulation.Step(new TickInput(
            [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

        return simulation;
    }

    /// <summary>The shipped Ruleset with the Commute Budget's ceiling brought down.</summary>
    /// <remarks><c>ReachFailureTests.WithCeiling</c>'s shape and its reasoning.</remarks>
    private static Ruleset WithCeiling(int ceiling)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath)
            .Replace("commute_fast_minutes = 20", "commute_fast_minutes = 1", StringComparison.Ordinal)
            .Replace(
                "commute_moderate_minutes = 40", "commute_moderate_minutes = 2", StringComparison.Ordinal)
            .Replace(
                "commute_budget_minutes = 50",
                $"commute_budget_minutes = {ceiling}",
                StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
