using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>
/// Milestone 5b's artefact: <b>what it costs to walk across this city, and how much further than the
/// grid it makes you go.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the second honest Severance instrument and it measures the half the first one could
/// not.</b> <c>--roads</c> reports <em>disconnection</em> — how many walkable nodes have no
/// pedestrian route to the rest of the city at all — and says in its own output that the larger half
/// is <b>detour</b>: <i>"a crossing four hundred metres away severs a neighbourhood in every sense a
/// player would recognise and is fully connected here."</i> Measuring detour needs a shortest path,
/// which is what 5b built. So this prints a <b>cost distribution</b> rather than a count, and the two
/// instruments must agree about disconnection or one of them is wrong.
/// </para>
/// <para>
/// <b>The baseline is the grid, not the crow.</b> Detour here is the real walk against
/// <c>|Δeast| + |Δnorth|</c> at walking pace — the shortest route that could exist on a perfect
/// lattice with a crossing everywhere. Straight-line distance would fold in the fact that streets are
/// not diagonals, which is true of every grid city and is not Severance; measuring against the grid
/// ideal isolates <em>what the Arterials cost you</em>, which is the question. It also needs no square
/// root.
/// </para>
/// <para>
/// <b>⚠ This is not a Trip model and must not be read as one.</b> Nothing generates Trips
/// (<c>plans/0002</c> §A: no shop, no Provider List, <c>Scope.Pool</c> throws, nothing schedules an
/// occasion), so these are Building pairs walked exhaustively or by stride — <b>a census of what the
/// city costs, not a sample of what anybody does</b>. The distinction is load-bearing because S2 R4
/// found a uniform origin-destination draw is the <b>longest-trip distribution available</b>, so the
/// aggregate below is an upper bound on a real population and the per-band rows are the part that
/// transfers. <c>plans/0013</c>'s walk-search row is a curve in distance for the same reason: the
/// bands are the answer, the total is a property of a draw nobody has justified.
/// </para>
/// <para>
/// <b>It refuses without a Ruleset rather than degrading</b>, which is <c>--zones</c>' and
/// <c>--roads</c>' precedent: a road network is content, and a city with no Streets would print an
/// empty table that reads as a broken instrument rather than as a file declaring no <c>[roads]</c>.
/// </para>
/// </remarks>
internal static class TripDump
{
    /// <summary>
    /// The most pairs walked before the census becomes a stride sample.
    /// </summary>
    /// <remarks>
    /// <b>A cap that is printed rather than silent.</b> Pairs go as the square of the Building count,
    /// so a default city is exhaustive and a large one is not; a bound that quietly truncated would
    /// make a partial census read as a complete one. The output says which happened and how many
    /// pairs it walked.
    /// </remarks>
    private const int PairCap = 200_000;

    /// <summary>The bands, in blocks of grid-ideal walking. The rungs `plans/0013` prices.</summary>
    private static readonly int[] Bands = [1, 2, 4, 8, 16, 32];

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        RoadGraph graph = world.Roads;

        if (!graph.Exists)
        {
            output.WriteLine(
                "This Ruleset declares no [roads], so this world has no Road Graph and nobody can "
                + "walk anywhere. That is a legitimate Ruleset and an empty picture, which is why "
                + "the runner asks for a Ruleset rather than inventing a network.");

            return 3;
        }

        TripRuleset trips = rules.Trips;

        if (!trips.Runs)
        {
            output.WriteLine(
                "This Ruleset declares no [trips], so what a crossing costs is unauthored and this "
                + "instrument may not choose it (adr/0052). It refuses rather than walking at zero: "
                + "zero is a legitimate crossing cost -- adr/0074's rung 1, the city where the shop "
                + "opposite is the shop next door -- so a zero standing in for an unauthored number "
                + "would be indistinguishable from a decision in every figure below.");

            return 3;
        }

        Address[] doors = Doors(world, out int homeless);

        if (doors.Length < 2)
        {
            output.WriteLine(
                $"Only {doors.Length} Building(s) in this world have an Access Point, so there is no "
                + "pair to walk between. A Building's front door is derived from its Lot's frontage, "
                + "so this means the subdivider carved nothing — check [lots] and [roads].");

            return 3;
        }

        Header(output, world, graph, trips, doors.Length, homeless);

        var census = new Census(graph, doors, trips.CrossingCost);

        census.Walk(out bool exhaustive);

        output.WriteLine();
        output.WriteLine("## What it costs to walk, by grid-ideal distance");
        census.WriteBands(output, graph);

        output.WriteLine();
        output.WriteLine("## Severance — the detour half, measured");
        census.WriteVerdict(output, graph, exhaustive);

        return 0;
    }

    private static void Header(
        TextWriter output, World world, RoadGraph graph, TripRuleset trips, int doors, int homeless)
    {
        RoadRuleset roads = graph.Ruleset;

        output.WriteLine("# Borough Trip cost dump");
        output.WriteLine(
            $"# {graph.Nodes.Rows.LiveCount} nodes, {graph.Segments.Rows.LiveCount} Segments, "
            + $"block {roads.BlockTiles} Tiles, {roads.ArterialCount} Arterials, a foot crossing "
            + $"every {roads.FootCrossingEvery} severed Street, "
            + $"{roads.FootPathsPerThousandBlocks} cut-throughs per thousand blocks.");
        output.WriteLine(
            $"# {world.Buildings.Rows.LiveCount} Buildings: {doors} with an Access Point, "
            + $"{homeless} with none.");

        if (homeless > 0)
        {
            output.WriteLine(
                "# A Building with no Access Point is adr/0079's named absence, not a defect: it "
                + "outlived its frontage, and every Trip to it ends *no route found*. It is excluded "
                + "from the pairs below, because a walk that cannot start measures no distance.");
        }

        output.WriteLine(
            $"# The crossing cost is {Minutes(trips.CrossingCost.Raw)} min, from [trips] "
            + "crossing_seconds -- hash-bearing and UNRATIFIED (adr/0052, plans/0002 §D1), and this "
            + "run is half of what ratifies it: the distribution below at a candidate value against "
            + "the same distribution at zero. It applies only to two Addresses on one Segment and "
            + "opposite sides, so it moves the first band and nothing beyond it.");

        output.WriteLine(
            trips.HasCommuteBudget
                ? $"# The Commute Budget is {Minutes(trips.CommuteBudget.Raw)} min. This census does "
                    + "not apply it -- every pair is walked -- so what is below is geometry rather "
                    + "than behaviour, and it is the distribution the Budget should have been read "
                    + "off rather than a check on the value."
                : "# This city has no Commute Budget: [trips] states no commute_budget_minutes, "
                    + "which is a city where a Trip's length refuses nothing. That is the state the "
                    + "number is measured FROM -- it is a percentile of the distribution below.");
    }

    /// <summary>Every Building's pedestrian Access Point, and how many have none.</summary>
    private static Address[] Doors(World world, out int homeless)
    {
        var doors = new List<Address>(world.Buildings.Rows.LiveCount);

        homeless = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            Address door = world.PedestrianAccessPoint(slot);

            if (door.Exists)
            {
                doors.Add(door);
            }
            else
            {
                homeless++;
            }
        }

        return [.. doors];
    }

    /// <summary>One band's accumulated walks.</summary>
    private sealed class Band
    {
        internal List<long> Costs { get; } = [];

        internal List<long> Detours { get; } = [];

        internal int Unreachable { get; set; }
    }

    /// <summary>Walks the pairs and holds what they cost.</summary>
    private sealed class Census(RoadGraph graph, Address[] doors, TravelTime crossing)
    {
        private readonly Band[] _bands = [.. Bands.Select(_ => new Band())];
        private readonly WalkScratch _scratch = new();
        private int _pairs;

        /// <summary>
        /// Walks every pair, or a strided sample of <see cref="PairCap"/> of them.
        /// </summary>
        /// <remarks>
        /// <b>Strided rather than drawn, and it needs no <c>PurposeTag</c> because it decides
        /// nothing.</b> The counter-based RNG exists so that two simulation decisions never correlate
        /// invisibly (<c>02 §8</c>); an instrument that reads a city and writes a table makes no
        /// decision, reaches no State Hash, and adding a tag for it would put a diagnostic in the
        /// enum <c>BOR0801</c>–<c>BOR0803</c> police. Striding by successive offsets is reproducible
        /// by construction, and bias in <em>which</em> pairs are drawn moves how many land in each
        /// band rather than what a walk in that band costs — which is why the bands are the output
        /// and the total is not.
        /// </remarks>
        internal void Walk(out bool exhaustive)
        {
            int n = doors.Length;
            long all = (long)n * (n - 1) / 2;

            exhaustive = all <= PairCap;

            for (int offset = 1; offset < n && _pairs < PairCap; offset++)
            {
                for (int i = 0; i < n && _pairs < PairCap; i++)
                {
                    int j = i + offset;

                    if (j >= n)
                    {
                        break;
                    }

                    Pair(doors[i], doors[j]);
                }
            }
        }

        internal int Pairs => _pairs;

        private void Pair(Address from, Address to)
        {
            _pairs++;

            Tiles ideal = GridIdeal(graph, from, to);
            int band = BandOf(graph, ideal);

            if (band < 0)
            {
                return;
            }

            TravelTime cost = WalkRouting.Cost(graph, TravelMode.Foot, from, to, crossing, _scratch);

            if (cost.IsImpassable)
            {
                _bands[band].Unreachable++;
                return;
            }

            TravelTime floor = TravelTime.Over(ideal, WalkSpeed(graph));

            _bands[band].Costs.Add(cost.Raw);

            if (floor.Raw > 0)
            {
                _bands[band].Detours.Add(IntegerMath.RoundDiv((long)cost.Raw * 100, floor.Raw));
            }
        }

        internal void WriteBands(TextWriter output, RoadGraph roadGraph)
        {
            output.WriteLine(
                "A band holds every pair whose GRID IDEAL is at most that far — |Δeast| + |Δnorth|, "
                + "the shortest route that could exist on a perfect lattice with a crossing "
                + "everywhere. Bands are upper bounds and each holds everything above the one "
                + "before, so the 32-block row is 17-to-32 blocks and its walks are shorter than "
                + "its label.");
            output.WriteLine(
                "Walks are IN-WORLD minutes (a Tick is 42.19 in-world seconds, not the 1/16 s of "
                + "wall clock plans/0013 is denominated in). Detour is the real walk against the "
                + "grid ideal: 100% is the ideal, 160% is half again as far.");
            output.WriteLine();
            output.WriteLine(
                "  up to        metres     pairs   p50 walk   p90 walk    detour p50   p90   "
                + "no route");

            for (int i = 0; i < Bands.Length; i++)
            {
                Band band = _bands[i];
                int metres = Bands[i] * roadGraph.Ruleset.BlockTiles * MetresPerTile;
                int walked = band.Costs.Count;

                if (walked == 0)
                {
                    // Two different nothings, and printing them the same way is how a severed band
                    // reads as a free one: percentiles of an empty list are zero, so this row said
                    // "0.0 min, 0% detour" over a band where EVERY pair was unreachable.
                    output.WriteLine(
                        band.Unreachable == 0
                            ? $"  {Bands[i],2} blocks  {metres,10}         —  no pair landed in this band"
                            : $"  {Bands[i],2} blocks  {metres,10}         —  NOT ONE of this band's "
                              + $"{band.Unreachable} pairs had a route");

                    continue;
                }

                band.Costs.Sort();
                band.Detours.Sort();

                output.WriteLine(
                    $"  {Bands[i],2} blocks  {metres,10}  {walked,8}  "
                    + $"{Minutes(Percentile(band.Costs, 50)),6} min  {Minutes(Percentile(band.Costs, 90)),6} min  "
                    + $"{Percentile(band.Detours, 50),10}%  {Percentile(band.Detours, 90),4}%  "
                    + $"{band.Unreachable,8}");
            }
        }

        internal void WriteVerdict(TextWriter output, RoadGraph roadGraph, bool exhaustive)
        {
            int unreachable = _bands.Sum(band => band.Unreachable);
            int reachable = _bands.Sum(band => band.Costs.Count);
            int stranded = roadGraph.Connectivity.StrandedOnFoot;

            output.WriteLine(
                exhaustive
                    ? $"Walked every one of {_pairs} Building pairs."
                    : $"Walked {_pairs} Building pairs of {(long)doors.Length * (doors.Length - 1) / 2}"
                      + " — the cap, so this is a stride sample and not a census. The per-band rows "
                      + "still hold; the pair COUNTS in them do not describe the whole city.");
            output.WriteLine(
                $"{unreachable} pair(s) had no pedestrian route at all, against {reachable} that did.");
            output.WriteLine(
                $"--roads reports {stranded} walkable node(s) cut off from the largest pedestrian "
                + "piece. The two instruments measure the same disconnection from opposite ends and "
                + (Agrees(stranded, unreachable)
                    ? "they agree."
                    : "THEY DISAGREE, which is a defect in one of them rather than a fact about the "
                      + "city."));

            output.WriteLine();

            List<long> spread = [.. _bands.SelectMany(band => band.Detours)];

            if (spread.Count == 0)
            {
                output.WriteLine("No reachable pair landed in a band, so there is no detour to report.");
                return;
            }

            spread.Sort();

            output.WriteLine(
                $"DETOUR, over every reachable pair: p50 {Percentile(spread, 50)}%, "
                + $"p90 {Percentile(spread, 90)}%, p99 {Percentile(spread, 99)}%, "
                + $"worst {spread[^1]}% of the grid ideal.");
            output.WriteLine(
                "This is the number --roads says it cannot see. A city can be fully connected on "
                + "foot and still make people walk half again as far, and THAT is what a resident "
                + "experiences as a severed neighbourhood. Compare two Rulesets to read it: "
                + "rulesets/minimal.toml against rulesets/severance.toml is the pair this exists for.");
        }

        /// <summary>
        /// Whether the pair census and the component count tell the same story.
        /// </summary>
        /// <remarks>
        /// <b>Agreement is one-directional and that is the honest test.</b> Stranded nodes with no
        /// unreachable pair is ordinary — the pocket may hold no Building — but an unreachable pair
        /// with nothing stranded cannot happen, because a pair with no route means its two Addresses
        /// are in different foot components and that is exactly what the count measures.
        /// </remarks>
        private static bool Agrees(int stranded, int unreachable) => unreachable == 0 || stranded > 0;
    }

    /// <summary>Metres a Tile — `05 §26`, and the figure `Speed.PerKilometrePerHour` is derived from.</summary>
    private const int MetresPerTile = Core.Quantities.Tiles.Metres;

    /// <summary>Minutes an in-world Day, against `Ticks.PerDay` Ticks of it.</summary>
    private const int MinutesPerDay = Core.Quantities.Ticks.MinutesPerDay;

    private static Speed WalkSpeed(RoadGraph graph) => graph.Ruleset.WalkSpeed;

    /// <summary>Which band a grid-ideal distance falls in, or -1 if it is past the last one.</summary>
    private static int BandOf(RoadGraph graph, Tiles ideal)
    {
        int blocks = IntegerMath.RoundDiv(ideal.Raw, graph.Ruleset.BlockTiles);

        for (int i = 0; i < Bands.Length; i++)
        {
            if (blocks <= Bands[i])
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// The shortest walk that could exist between two Addresses on a perfect lattice.
    /// </summary>
    /// <remarks>
    /// Manhattan rather than straight-line, because Streets are not diagonals in any grid city and
    /// folding that in would charge the lattice for being a lattice. What is left is what the
    /// Arterials and the missing crossings cost, which is Severance.
    /// </remarks>
    private static Tiles GridIdeal(RoadGraph graph, Address from, Address to)
    {
        (Tiles fromEast, Tiles fromNorth) = Position(graph, from);
        (Tiles toEast, Tiles toNorth) = Position(graph, to);

        return new Tiles(
            IntegerMath.Abs(fromEast.Raw - toEast.Raw) + IntegerMath.Abs(fromNorth.Raw - toNorth.Raw));
    }

    /// <summary>Where an Address is on the map: along its Segment by its offset.</summary>
    private static (Tiles East, Tiles North) Position(RoadGraph graph, Address address)
    {
        RoadSegmentTable segments = graph.Segments;
        RoadNodeTable nodes = graph.Nodes;

        if (!nodes.Rows.TryResolve(segments.NodeA[address.Segment], out int a)
            || !nodes.Rows.TryResolve(segments.NodeB[address.Segment], out int b))
        {
            return (Tiles.Zero, Tiles.Zero);
        }

        int length = segments.LengthTiles[address.Segment].Raw;

        if (length <= 0)
        {
            return (nodes.East[a], nodes.North[a]);
        }

        long along = address.Offset.Raw;

        return (new Tiles(Along(nodes.East[a], nodes.East[b], along, length)),
                new Tiles(Along(nodes.North[a], nodes.North[b], along, length)));
    }

    /// <summary>One axis of the interpolation, kept whole so neither axis rounds differently.</summary>
    private static int Along(Tiles from, Tiles to, long along, int length) =>
        from.Raw + (int)IntegerMath.RoundDiv((to.Raw - from.Raw) * along, length);

    /// <summary>The <paramref name="which"/>th percentile of a sorted list.</summary>
    private static long Percentile(List<long> sorted, int which)
    {
        ArgumentNullException.ThrowIfNull(sorted);

        if (sorted.Count == 0)
        {
            return 0;
        }

        int index = (int)IntegerMath.RoundDiv((long)(sorted.Count - 1) * which, 100);

        return sorted[index];
    }

    /// <summary>A Q16.16 TravelTime in Ticks, as <b>in-world</b> clock minutes.</summary>
    /// <remarks>
    /// <para>
    /// <b>Minutes because a Commute Budget is authored in them</b> (<c>adr/0008</c>, session F: clock
    /// minutes, one currency across modes, and it is a percentile of exactly the distribution this
    /// instrument prints). Printed to one decimal by integer arithmetic — the shell may format, and
    /// it is <c>Core</c> that may not hold a <c>double</c>.
    /// </para>
    /// <para>
    /// <b>⚠ The conversion is the IN-WORLD one and this got it wrong first time, which is worth a
    /// sentence because the two rates are both real and both correct about different things.</b> A
    /// Tick is <b>1/16 s of wall clock</b> at the reference rate — that is the 15.6 ms budget's
    /// world, and it is what <c>plans/0013</c> is denominated in — and <b>42.1875 s of in-world
    /// time</b>, because a Day is 86,400 s over <see cref="Core.Quantities.Ticks.PerDay"/> Ticks
    /// (<c>Speed.PerKilometrePerHour</c>'s derivation). A walk is a thing a <em>resident</em> does,
    /// so it is the second. The first reads a 3 km walk as 13 seconds, which is wrong by 169× and
    /// looks merely small rather than absurd — a placeholder inside the range of legitimate answers,
    /// which is session F's finding arriving in an instrument.
    /// </para>
    /// </remarks>
    internal static string Minutes(long rawTicks)
    {
        // Scaled BEFORE the fraction is dropped, and the order is the whole of the correction. This
        // read `rawTicks >> 16` first, which floors to a whole Tick -- 42.1875 s of in-world time --
        // and only then converted, so every figure this instrument printed was short by up to that
        // much. It showed up as the shipped 20-minute Commute Budget printing as 19.9, which is a
        // rounding artefact nobody would look twice at; on a 2.5-minute band it is 7%. The
        // denominator is Ticks.PerDay in Q16.16, and the numerator peaks near 3.1e13 for a
        // Fixed.MaxValue cost, comfortably inside a long.
        long tenths = IntegerMath.RoundDiv(
            rawTicks * MinutesPerDay * 10, (long)Core.Quantities.Ticks.PerDay << 16);

        return $"{tenths / 10}.{tenths % 10}";
    }
}
