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
/// Milestone 5c task 6's owed measurement: <b>how loaded a road in this simulation actually gets</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The volume-delay function is a curve, and a curve is only worth what the range it is evaluated
/// over is worth.</b> BPR's α and β are textbook and S2 ran them; what neither of those establishes is
/// whether <em>this</em> city ever reaches a volume/capacity ratio where the curve differs from a flat
/// line. The first cut of <see cref="TripEngine"/> did not, and only this measurement said so.
/// </para>
/// <para>
/// ⚠ <b>The headline is that peak load does not move with population at all</b> — <c>v/c</c> <b>0.44</b>
/// at 4,000 Citizens, at 16,000 and at 64,000 alike, a 16× population against an identical load. The paved extent scales with the square root
/// of the population (<c>SyntheticCity</c>, <c>plans/0003</c> queue item 6), so the network grows with
/// the traffic and <b>the city gets bigger rather than busier</b>. ***A generated city cannot congest
/// itself, because the same number sizes both the demand and the supply.*** Congestion in this design
/// is something a <em>player</em> makes by laying too little road for what they zoned, which is
/// <c>adr/0090</c>'s whole point and is unreachable from <c>CommandKind.Populate</c>.
/// </para>
/// <para>
/// <b>So this suite asserts a floor and prints the table.</b> The floor is that the function is not
/// inert; the number that matters is the one printed, because a threshold asserted against a figure
/// nobody has ratified is a test of the fixture. The named ratifier for α, β and the clamp is 5c task
/// 8's long run.
/// </para>
/// </remarks>
public sealed class VolumeDelayReachTests(ITestOutputHelper output)
{
    private const string Traffic = "\n[traffic]\nalpha_percent = 15\nbeta = 4\nclamp_percent = 400\n";

    private readonly ITestOutputHelper _output = output;

    private static Simulation Start(int population, bool congestion, int capacityPerHour = 3_600)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath)
                .Replace(
                    "street_capacity_per_hour       = 3600",
                    $"street_capacity_per_hour       = {capacityPerHour}",
                    StringComparison.Ordinal)
            + "\n[households]\ncar_ownership_percent = 100\n"
            + (congestion ? Traffic : string.Empty);

        RulesetLoadResult rules = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(rules.Ok, rules.Describe());

        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(population), GoldenFixtures.RulesetHash);

        Simulation simulation = Replay.Start(builder.Build(), rules.Ruleset!);

        simulation.Step(new TickInput(
            [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

        return simulation;
    }

    /// <summary>
    /// <b>The busiest a single Segment direction ever gets, over a Day's worth of Ticks.</b>
    /// </summary>
    private static (int PeakOne, long PeakTotal, int Segments) Busiest(Simulation simulation, int ticks)
    {
        RoadSegmentTable segments = simulation.World.Roads.Segments;

        int peakOne = 0;
        long peakTotal = 0;

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));

            long total = 0;

            for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
            {
                if (!segments.Rows.IsLive(slot))
                {
                    continue;
                }

                peakOne = Math.Max(
                    peakOne, Math.Max(segments.VolumeForward[slot], segments.VolumeBackward[slot]));
                total += segments.VolumeForward[slot] + segments.VolumeBackward[slot];
            }

            peakTotal = Math.Max(peakTotal, total);
        }

        int live = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (segments.Rows.IsLive(slot))
            {
                live++;
            }
        }

        return (peakOne, peakTotal, live);
    }

    /// <summary>
    /// <b>How busy a generated city gets, at four populations, measured.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The denominator is Little's Law and not the per-Tick flow, and that distinction is the
    /// whole finding.</b> A Segment running at capacity <em>holds</em> <c>capacity per Tick × its
    /// free-flow crossing time</c> Vehicles, which at a Street is 42 × 0.218 = <b>9.2</b> — a 14 m
    /// spacing on a 128 m block. Dividing by 42 instead reports a load 4.5× too low, and the first cut
    /// of this mechanism did exactly that on the strength of <c>adr/0041</c>'s <i>a vehicle crosses
    /// about one Segment per Tick</i> — a sentence that was true under the 8192-Tick Day and that
    /// <c>adr/0094</c> retired without touching it.
    /// </remarks>
    [Fact]
    public void How_loaded_a_generated_city_gets()
    {
        _output.WriteLine("population | Segments | peak one way | peak total | v/c | delay");
        _output.WriteLine("-----------+----------+--------------+------------+------+-------");

        double first = 0;
        double last = 0;

        foreach (int population in (int[])[4_000, 16_000, 64_000])
        {
            Simulation simulation = Start(population, congestion: true);
            RoadSegmentTable segments = simulation.World.Roads.Segments;

            (int peakOne, long peakTotal, int live) = Busiest(simulation, 512);

            int perTick = segments.CapacityPerDay[0] / Ticks.PerDay;
            double dwell = TravelTime.Over(segments.LengthTiles[0], segments.FreeFlow[0]).Raw / 65_536.0;
            double load = peakOne / (perTick * dwell);
            double delay = 1 + (0.15 * Math.Pow(Math.Min(load, 4), 4));

            _output.WriteLine(
                $"{population,10} | {live,8} | {peakOne,12} | {peakTotal,10} | {load,4:F2} | x{delay:F4}");

            first = first == 0 ? load : first;
            last = load;
        }

        // Not inert: the curve is evaluated somewhere it is not flat. This is the assertion the first
        // cut of the mechanism failed, and it failed it silently -- every other test in this milestone
        // passed while the function multiplied every cost by 1.0000.
        Assert.True(first > 0.1, $"the busiest Segment at 4,000 Citizens reached v/c {first:F2}.");

        // And it does not run away either: a 16x population must not produce a 16x load, or the
        // measurement is of the fixture rather than of the city.
        Assert.True(last < 4, $"the busiest Segment at 64,000 Citizens reached v/c {last:F2}.");
    }

    /// <summary>
    /// ⚠ <b>Load is nearly flat in population, so a bigger generated city is not a busier one.</b>
    /// </summary>
    /// <remarks>
    /// <b>Asserted rather than merely printed, because the property is load-bearing for what task 8
    /// can conclude.</b> A long run at a higher population is not a congestion test; the only thing
    /// that raises v/c here is a player laying less road than they zoned, and no test can do that
    /// through <c>CommandKind.Populate</c>. Somebody reading a flat congestion figure off a 100,000-Tick
    /// run needs to find this sentence rather than conclude the function does not work.
    /// </remarks>
    [Fact]
    public void A_much_bigger_city_is_not_a_much_busier_one()
    {
        (int small, _, int smallSegments) = Busiest(Start(4_000, congestion: true), 512);
        (int large, _, int largeSegments) = Busiest(Start(64_000, congestion: true), 512);

        _output.WriteLine($"4,000 Citizens: {smallSegments} Segments, busiest holds {small}");
        _output.WriteLine($"64,000 Citizens: {largeSegments} Segments, busiest holds {large}");

        Assert.True(largeSegments > smallSegments * 5, "the network did not grow with the population.");
        Assert.True(
            large < small * 4,
            $"the busiest Segment grew from {small} to {large}, which is not the flat behaviour the "
            + "square-root paved extent predicts.");
    }

    /// <summary>
    /// ⚠ <b>Where the curve starts to bite, swept on the one dial that reaches it: road capacity.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Demand cannot be raised against a generated network</b> — the test above is why — so the
    /// sweep moves the supply instead. <c>street_capacity_per_hour</c> is authored
    /// <c>[roads]</c> Ruleset data and every other number is held fixed, which makes this the
    /// volume-delay function's response curve and nothing else.
    /// </para>
    /// <para>
    /// ⚠ <b>The low end of this sweep is where a defect was found and it would not have been found any
    /// other way.</b> The first cut floored capacity to whole Vehicles per Tick before dividing, and a
    /// Day is 2,048 Ticks — so every capacity under 2,048 a Day floored to zero, hit the guard written
    /// for an <em>absent</em> capacity, and reported no delay at all. The sweep showed ×2.48 at 200
    /// Vehicles an hour and ×1.0000 at 60, which is not a curve any function has.
    /// ***A guard written for an absent quantity will fire on a small one.***
    /// </para>
    /// </remarks>
    [Fact]
    public void Where_the_curve_starts_to_bite()
    {
        long Occupancy(int capacityPerHour, bool congestion)
        {
            Simulation simulation = Start(4_000, congestion, capacityPerHour);
            long total = 0;

            for (int tick = 0; tick < 512; tick++)
            {
                simulation.Step(new TickInput([], rulesetHash: 0));

                RoadSegmentTable segments = simulation.World.Roads.Segments;

                for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
                {
                    if (segments.Rows.IsLive(slot))
                    {
                        total += segments.VolumeForward[slot] + segments.VolumeBackward[slot];
                    }
                }
            }

            return total;
        }

        _output.WriteLine("Vehicles/hour | free-flow | loaded | ratio");
        _output.WriteLine("--------------+-----------+--------+------");

        double previous = 0;

        foreach (int capacity in (int[])[3_600, 400, 200, 60])
        {
            long free = Occupancy(capacity, congestion: false);
            long loaded = Occupancy(capacity, congestion: true);
            double ratio = (double)loaded / free;

            _output.WriteLine($"{capacity,13} | {free,9} | {loaded,6} | x{ratio:F4}");

            // Monotone in the narrowing direction: a narrower road is never faster. This is what the
            // floored-capacity defect broke, and it broke it non-monotonically -- x2.48 at 200 and
            // x1.0000 at 60 -- so monotonicity is the property worth asserting rather than any level.
            Assert.True(
                ratio >= previous - 0.001,
                $"a capacity of {capacity} an hour was faster than the wider road above it.");

            previous = ratio;
        }

        // And it saturates rather than running away, because the clamp is what bounds it.
        Assert.True(previous > 1, "no capacity in the sweep produced any delay at all.");
    }
}
