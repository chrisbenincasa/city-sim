using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// The travel-time matrix, and <b>the entry-error measurement that ratifies
/// <see cref="RoutingPartition.DesignEdge"/></b> (5c task 2, <c>plans/0002</c> §D2).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the machine §D2 names, and it beats S2 R1's curve on four axes at once</b> — a real
/// world rather than a synthetic lattice, the origin-destination draw the city actually produces
/// rather than a uniform one, the current clock rather than 8192 Ticks a Day, and the mode being
/// asked about rather than car times read against foot rungs. Every one of those is a
/// re-denomination the spike curve needs and does not carry.
/// </para>
/// <para>
/// <b>The comparison is against a real search and nothing else.</b> <see cref="WalkRouting.Cost"/>
/// is what the job pass runs today, so the error reported here is exactly the error a consumer would
/// inherit by believing the matrix — not a distance, not a proxy, and not the matrix compared
/// against itself.
/// </para>
/// </remarks>
public sealed class TravelTimeMatrixTests(ITestOutputHelper output)
{
    private const int Pairs = 400;

    /// <summary>Enough passes that a few hundred microseconds of work is a stable millisecond figure.</summary>
    private const int Repeats = 200;

    /// <summary>
    /// A matrix row is a one-to-all search, and the diagonal is zero.
    /// </summary>
    /// <remarks>
    /// <b>The cheapest structural claim, and it catches the expensive mistake.</b> An entry read off
    /// the wrong access node, or a row filled from the wrong origin, still produces plausible times —
    /// the failure has no symptom except being wrong. A zero diagonal is the one entry whose correct
    /// value is known without computing anything.
    /// </remarks>
    [Fact]
    public void Every_partition_is_zero_from_itself_and_the_matrix_is_square()
    {
        (Simulation simulation, TravelTimeMatrix matrix) = Build(GoldenFixtures.Population);

        Assert.Equal(simulation.World.Roads.Partition.Count, matrix.Order);
        Assert.True(matrix.Order > 1);

        for (int p = 0; p < matrix.Order; p++)
        {
            Assert.Equal(TravelTime.Zero, matrix.From(p, p));
        }
    }

    /// <summary>
    /// The matrix rebuilds when the graph moves and does not when it has not.
    /// </summary>
    /// <remarks>
    /// <b>Both halves, because each failure is silent in the opposite direction.</b> A matrix that
    /// never refreshes answers with a city that no longer exists; one that refreshes on every call
    /// pays <c>n</c> full-graph searches per query and would read as a performance problem rather
    /// than as a correctness one.
    /// </remarks>
    [Fact]
    public void The_matrix_refreshes_on_a_road_edit_and_not_otherwise()
    {
        (Simulation simulation, TravelTimeMatrix matrix) = Build(GoldenFixtures.Population);
        WalkScratch scratch = new();
        RoadGraph graph = simulation.World.Roads;

        Assert.False(matrix.EnsureFresh(graph, TravelMode.Foot, scratch));

        graph.RebuildDerived();

        Assert.True(matrix.EnsureFresh(graph, TravelMode.Foot, scratch));
        Assert.False(matrix.EnsureFresh(graph, TravelMode.Foot, scratch));

        // A different mode is a different matrix, not a stale one.
        Assert.True(matrix.EnsureFresh(graph, TravelMode.Car, scratch));
    }

    /// <summary>
    /// One search fills a row, so a rebuild is <c>n</c> searches rather than <c>n²</c>.
    /// </summary>
    /// <remarks>
    /// <b>The claim the whole structure rests on, asserted rather than assumed.</b> If a future edit
    /// turned the fill into point-to-point queries the matrix would still be correct and would cost
    /// <see cref="TravelTimeMatrix.Order"/> times more to build — a regression with no wrong answer
    /// attached to it, which is the kind this corpus keeps finding a milestone late.
    /// </remarks>
    [Fact]
    public void A_rebuild_runs_one_search_per_partition()
    {
        (Simulation simulation, TravelTimeMatrix matrix) = Build(GoldenFixtures.Population);

        Assert.Equal(matrix.Order, matrix.Searches);

        output.WriteLine(
            $"order {matrix.Order}, searches {matrix.Searches}, nodes settled {matrix.Settled}");
    }

    /// <summary>
    /// <b>The entry-error measurement.</b> How wrong the matrix is against a real walk, in clock
    /// minutes, over the city's own pairs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Both directions are reported and they are not interchangeable.</b> An entry that
    /// <em>understates</em> costs a consumer a walk it was going to run anyway. An entry that
    /// <em>overstates</em> is the dangerous one: a reject keyed on it discards a job somebody could
    /// have taken, and nothing downstream can tell that it happened. So the overstatement tail is the
    /// number a reject margin has to be built from.
    /// </para>
    /// <para>
    /// <b>⚠ There is no geometric bound to compare it against, and finding that out is half of what
    /// this measurement is for.</b> An entry runs access node to access node and a real journey runs
    /// Address to Address, so the difference is two within-partition walks — and a walk inside a
    /// partition is <em>road</em> distance, which nothing bounds by the partition's size. A spiral
    /// street, a cul-de-sac or a severed corner makes it arbitrarily long. This is
    /// <c>BuildingResidency</c>'s rule one level up: <i>a catchment is a time rather than a distance,
    /// and no geometry on the Cell grid can express it.</i> The bound is therefore empirical, and a
    /// reject margin built on it is a measurement with a safety factor rather than a proof.
    /// </para>
    /// </remarks>
    [Trait(Tier.Key, Tier.Instrument)]
    [Fact]
    public void The_entry_error_against_a_real_walk()
    {
        output.WriteLine(
            " population  order  pairs  mean    p50     p90     max over  max under  impassable");

        foreach (int population in (int[])[GoldenFixtures.Population, 40_000])
        {
            (Simulation simulation, TravelTimeMatrix matrix) = Build(population);

            World world = simulation.World;
            RoutingPartition partition = world.Roads.Partition;
            TripRuleset trips = world.Rules.Trips;
            WalkScratch scratch = new();

            List<int> errors = [];
            int impassable = 0;
            int sampled = 0;

            foreach ((int from, int to) in Sample(world))
            {
                Address origin = world.PedestrianAccessPoint(from);
                Address destination = world.PedestrianAccessPoint(to);

                TravelTime truth =
                    WalkRouting.Cost(world.Roads, TravelMode.Foot, origin, destination, trips.CrossingCost, scratch);

                int a = PartitionOf(world, partition, from);
                int b = PartitionOf(world, partition, to);

                if (a == RoutingPartition.None || b == RoutingPartition.None)
                {
                    continue;
                }

                TravelTime estimate = matrix.From(a, b);

                if (truth.IsImpassable || estimate.IsImpassable)
                {
                    impassable++;
                    continue;
                }

                sampled++;
                errors.Add(estimate.Raw - truth.Raw);
            }

            errors.Sort();

            output.WriteLine(
                $" {population,-11} {matrix.Order,-6} {sampled,-6}"
                + $" {Minutes(Mean(errors)),-7:0.00} {Minutes(At(errors, 50)),-7:0.00}"
                + $" {Minutes(At(errors, 90)),-7:0.00} {Minutes(errors[^1]),-9:0.00}"
                + $" {Minutes(errors[0]),-10:0.00} {impassable}");

            Assert.True(sampled > Pairs / 4, "Too few comparable pairs to say anything.");
        }
    }

    /// <summary>
    /// <b>What a reject would actually refuse</b>, over the same pairs — before anything is wired to
    /// it.
    /// </summary>
    /// <remarks>
    /// <b>Taken first on 5b-bis's precedent, which cost a milestone to learn.</b> The job-search box
    /// was derived correctly, tested correctly, and filtered nothing in any world this project
    /// builds, because nobody asked what it <em>did</em> to a real city until a milestone later. A
    /// reject rule is the same shape — provably safe, cheap, and possibly inert — so the yield is
    /// measured before the wiring rather than after it. The columns are the two certainties
    /// separately, because they fire for unrelated reasons: <b>severed</b> is topology and needs no
    /// margin, <b>far</b> is an estimate and needs the whole overstatement tail subtracted from it.
    /// </remarks>
    [Trait(Tier.Key, Tier.Instrument)]
    [Fact]
    public void What_a_reject_would_refuse()
    {
        output.WriteLine(
            " population  pairs  same-partition  severed  far (safe)  far (raw)  ceiling  margin");

        foreach (int population in (int[])[GoldenFixtures.Population, 40_000])
        {
            (Simulation simulation, TravelTimeMatrix matrix) = Build(population);

            World world = simulation.World;
            RoutingPartition partition = world.Roads.Partition;
            TripRuleset trips = world.Rules.Trips;

            TravelTime margin = TravelTimeMatrix.EntryError(partition, world.Rules.Roads.WalkSpeed);

            int pairs = 0;
            int diagonal = 0;
            int severed = 0;
            int farSafe = 0;
            int farRaw = 0;

            foreach ((int from, int to) in Sample(world))
            {
                int a = PartitionOf(world, partition, from);
                int b = PartitionOf(world, partition, to);

                if (a == RoutingPartition.None || b == RoutingPartition.None)
                {
                    continue;
                }

                pairs++;

                if (a == b)
                {
                    diagonal++;
                }

                TravelTime estimate = matrix.From(a, b);

                if (estimate.IsImpassable)
                {
                    severed++;
                    continue;
                }

                if (estimate > trips.CommuteBudget)
                {
                    farRaw++;
                }

                if (estimate.Raw - margin.Raw > trips.CommuteBudget.Raw)
                {
                    farSafe++;
                }
            }

            output.WriteLine(
                $" {population,-11} {pairs,-6} {diagonal,-15} {severed,-8} {farSafe,-11} {farRaw,-10}"
                + $" {Minutes(trips.CommuteBudget.Raw):0.0}m    {Minutes(margin.Raw):0.0}m");
        }
    }

    /// <summary>
    /// <b>The control: on the Ruleset that severs, the matrix says so.</b>
    /// </summary>
    /// <remarks>
    /// <b>Without this the zero above is unreadable</b>, exactly as <c>--roads</c>' Severance verdict
    /// was until it acquired one — a mechanism reporting nothing everywhere passes for a mechanism
    /// reporting nothing correctly. <c>rulesets/severance.toml</c> exists because
    /// <c>rulesets/minimal.toml</c> demonstrably cannot sever at any dial value, so it is the file
    /// with something to report. What this asserts is that an <see cref="TravelTime.Impassable"/>
    /// entry is produced by real topology and is therefore the one thing in the matrix a consumer may
    /// act on without a margin.
    /// </remarks>
    [Fact]
    public void A_severed_city_produces_impassable_entries()
    {
        Ruleset severing = Ruleset(Path.Combine(AppContext.BaseDirectory, "Rulesets", "severance.toml"));
        (Simulation simulation, TravelTimeMatrix matrix) = Build(GoldenFixtures.Population, severing);

        int impassable = 0;
        int reachable = 0;

        for (int from = 0; from < matrix.Order; from++)
        {
            for (int to = 0; to < matrix.Order; to++)
            {
                if (matrix.From(from, to).IsImpassable)
                {
                    impassable++;
                }
                else
                {
                    reachable++;
                }
            }
        }

        output.WriteLine(
            $"severance.toml: order {matrix.Order}, {impassable} impassable of "
            + $"{impassable + reachable} entries");

        Assert.True(impassable > 0, "a Ruleset chosen to sever produced a fully connected matrix");
        Assert.True(reachable > matrix.Order, "every pair severed is a broken graph, not Severance");
    }

    /// <summary>
    /// <b>An Impassable matrix entry does not prove an impassable journey, and the component labels
    /// do.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The soundness check the reject rule turns on, and it fails in the direction that would have
    /// cost jobs.</b> A matrix entry runs access node to access node. If a partition holds two pieces
    /// that do not connect to each other, its access node sits in one of them, and a journey starting
    /// in the <em>other</em> piece may succeed where the entry says nothing can. So a reject keyed on
    /// Impassable discards reachable work, silently, in exactly the severed city the mechanism was
    /// supposed to make legible.
    /// </para>
    /// <para>
    /// <b><see cref="RoadNodeTable.FootComponent"/> has no such hole and has been in the tree since
    /// milestone 5a.</b> Union-find unions both endpoints of every Segment admitting the mode, so two
    /// Addresses in different components genuinely have no route and two in the same component
    /// genuinely have one — a property of the graph rather than of any tiling laid over it. This
    /// asserts the agreement in both directions against a real search, which is what makes it usable
    /// as a certainty.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_component_labels_agree_with_the_walk_and_the_matrix_does_not()
    {
        Ruleset severing = Ruleset(Path.Combine(AppContext.BaseDirectory, "Rulesets", "severance.toml"));
        (Simulation simulation, TravelTimeMatrix matrix) = Build(GoldenFixtures.Population, severing);

        World world = simulation.World;
        RoutingPartition partition = world.Roads.Partition;
        WalkScratch scratch = new();

        int componentDisagreed = 0;
        int matrixOverstated = 0;
        int matrixUnderstated = 0;
        int walkImpassable = 0;
        int walked = 0;

        foreach ((int from, int to) in Sample(world))
        {
            Address origin = world.PedestrianAccessPoint(from);
            Address destination = world.PedestrianAccessPoint(to);

            if (!origin.Exists || !destination.Exists)
            {
                continue;
            }

            TravelTime truth = WalkRouting.Cost(
                world.Roads, TravelMode.Foot, origin, destination, world.Rules.Trips.CrossingCost, scratch);

            walked++;

            bool sameComponent = Component(world, origin) == Component(world, destination)
                && Component(world, origin) != RoadConnectivity.Unlabelled;

            if (sameComponent == truth.IsImpassable)
            {
                componentDisagreed++;
            }

            int a = PartitionOf(world, partition, from);
            int b = PartitionOf(world, partition, to);

            if (truth.IsImpassable)
            {
                walkImpassable++;
            }

            if (a == RoutingPartition.None || b == RoutingPartition.None)
            {
                continue;
            }

            bool entrySevered = matrix.From(a, b).IsImpassable;

            if (entrySevered && !truth.IsImpassable)
            {
                matrixOverstated++;
            }

            if (!entrySevered && truth.IsImpassable)
            {
                matrixUnderstated++;
            }
        }

        output.WriteLine(
            $"severance.toml: {walked} walks, {walkImpassable} with no route. Component check "
            + $"disagreed {componentDisagreed} times. Matrix called a walkable pair severed "
            + $"{matrixOverstated} times and a severed pair walkable {matrixUnderstated} times.");

        // The component labels are exact by construction and this asserts it against real searches.
        Assert.Equal(0, componentDisagreed);

        // ⚠ The matrix's own agreement is NOT asserted, and the absence is the finding. An entry runs
        // access node to access node, so a partition holding two disconnected pieces can produce an
        // entry that contradicts a real journey in either direction. That did not happen here — both
        // counts came back 0 on 399 pairs — which makes it an untriggered hazard rather than a
        // refuted one, and a test asserting 0 would be asserting a property of this fixture's
        // geometry while reading as a property of the matrix.
        Assert.True(walkImpassable > 0, "a Ruleset chosen to sever produced no unwalkable pair");
    }

    /// <summary>
    /// <b>What the reachability reject is worth</b>, in walk searches and in wall time, on the city
    /// that severs.
    /// </summary>
    /// <remarks>
    /// <b>The acceptance number 5c task 2 owes, and it is not the matrix's.</b> The task was scoped
    /// around the job pass paying ~32.5 µs a walk search per candidate; what removes most of that
    /// bill on a severed city is <see cref="RoadNodeTable.FootComponent"/>, because a search that is
    /// going to fail settles the origin's entire component first. This reports the cost per walk with
    /// the reject in place; the figure without it is in <c>plans/0026</c>'s record, taken by removing
    /// the reject and re-running this.
    /// </remarks>
    [Trait(Tier.Key, Tier.Instrument)]
    [Fact]
    public void What_the_reachability_reject_is_worth()
    {
        Ruleset severing = Ruleset(Path.Combine(AppContext.BaseDirectory, "Rulesets", "severance.toml"));
        (Simulation simulation, _) = Build(GoldenFixtures.Population, severing);

        World world = simulation.World;
        WalkScratch scratch = new();
        List<(Address From, Address To)> pairs = [];

        foreach ((int from, int to) in Sample(world))
        {
            pairs.Add((world.PedestrianAccessPoint(from), world.PedestrianAccessPoint(to)));
        }

        int differing = 0;
        int missing = 0;

        foreach ((Address from, Address to) in pairs)
        {
            if (!from.Exists || !to.Exists)
            {
                missing++;
            }
            else if (Component(world, from) != Component(world, to))
            {
                differing++;
            }
        }

        output.WriteLine(
            $"of {pairs.Count} pairs: {missing} have no Address, {differing} cross a component");

        // One untimed pass, so the measurement is not paying for first-touch page faults on the
        // scratch arrays.
        Pass(world, pairs, scratch);

        var clock = System.Diagnostics.Stopwatch.StartNew();

        for (int repeat = 0; repeat < Repeats; repeat++)
        {
            Pass(world, pairs, scratch);
        }

        clock.Stop();

        output.WriteLine(
            $"severance.toml: {pairs.Count} walks x{Repeats} in {clock.Elapsed.TotalMilliseconds:0.0}"
            + $" ms — {clock.Elapsed.TotalMilliseconds * 1000 / (pairs.Count * Repeats):0.00} µs a"
            + " walk, with the reachability reject in place");

        Assert.True(differing > pairs.Count / 2, "the fixture stopped severing");
    }

    /// <summary>
    /// ⚠ <b>Node-settle counts are deliberately not reported here, and the reason is a defect this
    /// test had.</b> <see cref="WalkScratch.Relaxed"/> is reset by <c>Begin</c>, which the reject
    /// path never reaches — so a rejected walk leaves the <em>previous</em> walk's count standing and
    /// a sum over it double-counts. The first version of this measurement reported bit-identical
    /// totals with and without the reject and read as <i>the reject never fires</i>, when it fires
    /// 321 times in 399. <b>An instrument that is only valid on the path it is measuring away is not
    /// an instrument.</b>
    /// </summary>
    private static void Pass(World world, List<(Address From, Address To)> pairs, WalkScratch scratch)
    {
        foreach ((Address from, Address to) in pairs)
        {
            WalkRouting.Cost(world.Roads, TravelMode.Foot, from, to, world.Rules.Trips.CrossingCost, scratch);
        }
    }

    /// <summary>The foot component an Address sits in, through the Segment it hangs on.</summary>
    /// <remarks>
    /// <b>Either endpoint answers, because union-find unions both.</b> A Segment admitting
    /// <see cref="TravelMode.Foot"/> puts its two nodes in one component by construction, so reading
    /// node A is not a choice between two answers.
    /// </remarks>
    private static int Component(World world, Address address)
    {
        RoadGraph graph = world.Roads;

        if (!graph.Segments.Rows.IsLive(address.Segment)
            || !graph.Nodes.Rows.TryResolve(graph.Segments.NodeA[address.Segment], out int node))
        {
            return RoadConnectivity.Unlabelled;
        }

        return graph.Nodes.FootComponent[node];
    }

    /// <summary>Deterministic pairs of Buildings, walked by a fixed stride over the live slots.</summary>
    /// <remarks>
    /// <b>A stride rather than a draw, because a test may not reach for <c>System.Random</c> and does
    /// not need to.</b> What the sample has to be is spread and reproducible; two coprime strides
    /// over the slot space give both, and the pairs it produces are the city's own — a Building at
    /// one end and a Building at the other, which is the origin-destination distribution S2 R4 found
    /// a uniform draw is not.
    /// </remarks>
    private static IEnumerable<(int From, int To)> Sample(World world)
    {
        List<int> live = [];

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot))
            {
                live.Add(slot);
            }
        }

        if (live.Count < 2)
        {
            yield break;
        }

        for (int i = 0; i < Pairs; i++)
        {
            int from = live[(i * 7) % live.Count];
            int to = live[((i * 13) + 1) % live.Count];

            if (from != to)
            {
                yield return (from, to);
            }
        }
    }

    private static int PartitionOf(World world, RoutingPartition partition, int building)
    {
        if (!world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lot))
        {
            return RoutingPartition.None;
        }

        return partition.At(world.Lots.East[lot], world.Lots.North[lot]);
    }

    private static double Minutes(double raw) =>
        raw * Ticks.MinutesPerDay / ((double)Fixed.One * Ticks.PerDay);

    private static double Mean(List<int> values)
    {
        long total = 0;

        foreach (int value in values)
        {
            total += value;
        }

        return values.Count == 0 ? 0 : (double)total / values.Count;
    }

    private static double At(List<int> sorted, int percentile) =>
        sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, sorted.Count * percentile / 100)];

    private static Ruleset Ruleset(string path)
    {
        RulesetLoadResult result = RulesetLoader.Parse(File.ReadAllText(path), path);

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static (Simulation Simulation, TravelTimeMatrix Matrix) Build(
        int population, Ruleset? rules = null)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, rules ?? GoldenFixtures.Rules());

        Replay.Trace(simulation, log, new Ticks(1), 1, []);

        TravelTimeMatrix matrix = new();

        matrix.EnsureFresh(simulation.World.Roads, TravelMode.Foot, new WalkScratch());

        return (simulation, matrix);
    }
}
