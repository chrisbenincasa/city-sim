using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// Milestone 5c task 5's owed measurement: <b>how long a car commute's route is, in Segments</b>.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>This exists because task 4 published a route-memory figure that was withdrawn in full</b>
/// (<c>plans/0012</c> <b>Cause 5</b>, seventh sighting, and the first that is a <em>projection</em>
/// rather than a quotation). That figure fitted <c>√population</c> through five points, took a route
/// length <em>maximum</em> where memory takes a median, drew an employment ratio from a Ruleset whose
/// own header says it models no city, and used a cache <em>working set</em> as a live-route count.
/// ***An extrapolation is a claim about a mechanism, not about a curve*** — and the mechanism that
/// caps a foot route is the Commute Budget, which at 50 minutes and 5 km/h is 4.17 km, about 32
/// blocks. The fit ran straight through it.
/// </para>
/// <para>
/// <b>So this measures and does not project.</b> Every row printed below is a count taken off a
/// running city at a stated population; there is no fit, no per-Citizen figure and no total. What it
/// is <em>for</em> is the other half of <c>plans/0002</c> §C's <i>where routes live at 1M</i>: a car
/// route is the one a route cache would have to store, because <c>adr/0075</c> gives a Leg a cost and
/// no path and only a vehicular Leg needs Segments at all (<c>adr/0041</c>).
/// </para>
/// <para>
/// ⚠ <b>The car cap is a different mechanism from the foot cap and it is much weaker.</b> The same
/// 50-minute ceiling at a Street's 50 km/h reaches 41.7 km, which is wider than the paved extent of
/// any city this project can currently build — so on the car side the Budget is not what bounds the
/// route, the <em>map</em> is. Naming that is the point: a bound that comes from the fixture rather
/// than from a rule will move when the fixture does.
/// </para>
/// </remarks>
public sealed class CarRouteLengthTests(ITestOutputHelper output)
{
    private const int Ticks = 2_048;
    private const int HashEvery = 64;

    private readonly ITestOutputHelper _output = output;

    private static Ruleset WithOwnership(int percent)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        RulesetLoadResult result = RulesetLoader.Parse(
            $"{toml}\n[households]\ncar_ownership_percent = {percent}\n", "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static Simulation Run(Ruleset rules, int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(population), GoldenFixtures.RulesetHash);

        builder.Append(new Core.Quantities.Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, rules);

        Replay.Trace(simulation, log, new Core.Quantities.Ticks(Ticks), HashEvery, []);

        return simulation;
    }

    /// <summary>Route lengths in Segments over every commute in the city, in a given mode.</summary>
    /// <remarks>
    /// ⚠ <b>A journey along one Segment records no path and is counted as length 1, not skipped.</b>
    /// <see cref="WalkRouting.Cost"/> answers the same-Segment case in closed form and never reaches
    /// the search, so the recorder has nothing to hand back — <b>absent is not zero</b>. Dropping
    /// those would censor the distribution at its shortest end, which is exactly the failure 5b-bis
    /// task 6 found in the cost histogram.
    /// </remarks>
    private static List<int> RouteLengths(World world, TravelMode mode, out int impassable)
    {
        var scratch = new WalkScratch();
        Span<int> route = stackalloc int[256];
        List<int> lengths = [];

        TravelTime crossing = world.Rules.Trips.CrossingCost;
        int missed = 0;

        foreach ((Address from, Address to) in CarOwnershipTests.Commutes(world, mode))
        {
            TravelTime cost = WalkRouting.Cost(
                world.Roads, mode, from, to, crossing, scratch, recordPath: true);

            if (cost.IsImpassable)
            {
                missed++;
                continue;
            }

            int length = scratch.Arrived == WalkScratch.NoNode
                ? WalkScratch.NoPath
                : scratch.PathTo(world.Roads.Arcs, scratch.Arrived, route);

            lengths.Add(length == WalkScratch.NoPath ? 1 : Math.Max(length, 1));
        }

        impassable = missed;

        return lengths;
    }

    private static int Percentile(List<int> sorted, int percent) =>
        sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, sorted.Count * percent / 100)];

    /// <summary>
    /// <b>What a car commute's route costs to store, measured, at four populations.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The populations are the ones the rest of this milestone was measured at, so the rows line up
    /// with task 4's coverage sweep and can be read beside it. <b>Nothing here is extrapolated to 1M</b>
    /// — that is the error being repaid, and the honest statement is the shape of the curve plus the
    /// mechanism that would bend it, which is <c>plans/0002</c> §C's job rather than a test's.
    /// </para>
    /// <para>
    /// <b>The assertion is deliberately weak and the output is the deliverable.</b> A route that
    /// crosses at least one Segment and no more than the graph holds is all a machine can check here;
    /// the number that matters is printed, because ***a threshold asserted against a figure nobody has
    /// ratified is a test of the fixture***.
    /// </para>
    /// </remarks>
    [Fact]
    public void What_a_car_commute_costs_to_store()
    {
        _output.WriteLine(
            "population | commutes | median | p90 | max | mean | no route");
        _output.WriteLine(
            "-----------+----------+--------+-----+-----+------+---------");

        foreach (int population in (int[])[1_000, 4_000, 8_000, 16_000])
        {
            World world = Run(WithOwnership(100), population).World;
            List<int> lengths = RouteLengths(world, TravelMode.Car, out int impassable);

            if (lengths.Count == 0)
            {
                _output.WriteLine($"{population,10} | {0,8} | no commutes");
                continue;
            }

            lengths.Sort();

            double mean = lengths.Average();

            _output.WriteLine(
                $"{population,10} | {lengths.Count,8} | {Percentile(lengths, 50),6} | "
                + $"{Percentile(lengths, 90),3} | {lengths[^1],3} | {mean,4:F1} | {impassable,8}");

            Assert.True(lengths[0] >= 1, "a commute crossed no Segment at all.");
            Assert.True(
                lengths[^1] < world.Roads.Segments.Rows.SlotCount,
                "a route crossed more Segments than the graph holds.");
        }
    }

    /// <summary>
    /// ⚠ <b>A car route and the walk it replaces are different lengths, and which is longer is not
    /// obvious.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A driver is faster per Segment and takes the cheapest route <em>in time</em>, so on a graph
    /// with uniform Streets the two are the same path. They diverge where the mode masks differ —
    /// an Arterial carries cars and not pedestrians — and where free-flow varies between Segments.
    /// <b>Both shipped Rulesets set <c>arterial_count = 0</c></b>, so this fixture has no Arterials at
    /// all, and the honest reading is that <b>this measurement cannot separate the two modes on the
    /// city it is run on</b>.
    /// </para>
    /// <para>
    /// ***A comparison run on a fixture that lacks the mechanism under comparison measures the
    /// fixture.*** It is recorded because the number will change the moment a player lays an Arterial
    /// — which <c>adr/0090</c> says is the only way one can now exist — and somebody will otherwise
    /// read today's agreement as evidence the two are the same.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_drive_and_a_walk_take_the_same_streets_on_a_city_with_no_arterials()
    {
        World world = Run(WithOwnership(100), 4_000).World;

        Assert.Equal(0, world.Roads.Ruleset.ArterialCount);

        List<int> driving = RouteLengths(world, TravelMode.Car, out int driveMissed);
        List<int> walking = RouteLengths(world, TravelMode.Foot, out int walkMissed);

        _output.WriteLine($"drive: {driving.Count} routes, mean {driving.Average():F2} Segments, "
            + $"{driveMissed} unreachable");
        _output.WriteLine($"walk:  {walking.Count} routes, mean {walking.Average():F2} Segments, "
            + $"{walkMissed} unreachable");

        Assert.Equal(walking.Count, driving.Count);
        Assert.Equal(walkMissed, driveMissed);

        // Not Assert.Equal on the lists: two routes of equal cost are the same answer, and the search
        // is free to settle either. The claim is about the *distribution*, which is what a route store
        // would be sized against.
        Assert.Equal(walking.Sum(), driving.Sum());
    }
}
