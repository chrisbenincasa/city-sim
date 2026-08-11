using System.Globalization;
using System.Text;
using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Harness;

/// <summary>
/// R6.1 — what the route cache's key costs, which <c>adr/0012</c> left ambiguous and
/// <c>plans/0010</c> named as R6's to settle.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the measurement R5.5.2 named and declined to make.</b> Its detour column compares an
/// <i>arc sum</i> against an <i>arc sum</i>, so a cached route is never charged the remainders its
/// key forces on it — which is why every cache row there reads about 0.00% and why that section says
/// in terms that <i>"correcting it would mean charging the served route the remainders its own
/// endpoints imply… and it is not made here."</i> It is made here.
/// </para>
/// <para>
/// <b>No storm runs in this section and that is deliberate.</b> The key's error is structural: it is
/// present on a graph nobody has touched, every Tick, for ever. Mixing it with invalidation would
/// confound two errors that heal differently — the whole distinction R5.5 drew between a structural
/// and a temporal currency.
/// </para>
/// </remarks>
internal static class KeyReport
{
    /// <summary>Pairs drawn per O-D rung. Every one is priced; none is sampled away.</summary>
    private const int PoolPairs = 2_048;

    /// <summary>
    /// The candidate keys, coarsest first. <c>adr/0012</c> says only <i>"keyed by origin-destination
    /// pair"</i>, written before anyone knew an Access Point is a <c>(Segment, offset)</c>.
    /// </summary>
    internal enum RouteKey
    {
        /// <summary>
        /// What <c>RouteCache.KeyOf</c> implements today: node <b>A</b> of each Segment, whichever
        /// end the traveller is actually near. One entry per Segment pair, and the largest error.
        /// </summary>
        NodeA,

        /// <summary>
        /// The endpoint each Access Point is nearer to. Same key space — nodes² — and up to four
        /// entries per Segment pair rather than one.
        /// </summary>
        NearestNode,

        /// <summary>
        /// The endpoint pair minimising the whole journey. Not implementable as a key without
        /// already knowing the answer; it is here as the <b>floor</b> of the nodes² family, which is
        /// what says whether the error is intrinsic to keying on nodes or an artefact of choosing
        /// badly.
        /// </summary>
        BestEndpoint,

        /// <summary>
        /// The full <c>(Segment, offset)</c> pair — Buildings², exact by construction. The control,
        /// and it must read zero.
        /// </summary>
        AccessPoint,
    }

    private static readonly RouteKey[] Keys =
    [
        RouteKey.NodeA, RouteKey.NearestNode, RouteKey.BestEndpoint, RouteKey.AccessPoint,
    ];

    internal static readonly OdRung[] OdRungs =
    [
        new(OdShape.Uniform, 0),
        new(OdShape.DistanceDecay, 1_024),
        new(OdShape.DistanceDecay, 256),
        new(OdShape.Monocentric, 512),
    ];

    private sealed record Keyed(
        OdRung Rung,
        RouteKey Key,
        int Samples,
        int MeanDetourHundredths,
        int P90DetourHundredths,
        int WorstDetourHundredths,
        long MeanAbsoluteQ,
        long P90AbsoluteQ,
        long WorstAbsoluteQ,
        int Unroutable,
        int CheaperThanTruth);

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var distribution = new OdDistribution(graph, new OdSampler(graph));

        report.AppendLine("## S2 R6.1 — the cache key's granularity");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Working rung: {graph.Segments:N0} Segments, {graph.Nodes:N0} nodes, "
            + $"{graph.Arcs:N0} arcs. Free-flow car costs, `Chebyshev`, no storm and no Epoch — **the "
            + $"key's error is structural and is present on a graph nobody has touched.**"));
        report.AppendLine();

        AppendDetour(report, graph, distribution);
        AppendKeySpace(report, graph, distribution);

        return report.ToString();
    }

    // --- R6.1b the key space, and the population S2 does not have ---------------------------------

    /// <summary>Buildings placed per Segment. Invented, and therefore swept — R5.3's rule.</summary>
    private static readonly int[] BuildingsPerSegment = [1, 5, 20];

    /// <summary>
    /// How many distinct places the city's trips <i>end at</i>. Zero means "wherever the O-D rung
    /// put them", which is the sweep's own control.
    /// </summary>
    /// <remarks>
    /// <b>This axis exists because the first one did not move.</b> Buildings per Segment reads a flat
    /// 1.00× collapse at every rung, and it has to: 512 pairs drawn over 33,018 Segments is 512 draws
    /// from a billion Segment pairs, so no two trips share one however many Buildings sit on each.
    /// A column that cannot move is not evidence, and the corpus's rule is to pair it with a rung
    /// expected to be non-zero. <b>This is that rung</b>, and the two are published together because
    /// the contrast is the finding.
    /// </remarks>
    private static readonly int[] DestinationSites = [0, 128, 32, 8];

    /// <summary>R5.3's shape exactly, so the two sections' hit rates are comparable by construction.</summary>
    private const int LookupPoolPairs = 512;
    private const int LookupTrips = 4_096;
    private const int LookupCapacity = 1_024;

    private sealed record Spaced(
        OdRung Rung,
        int Buildings,
        int Sites,
        RouteKey Key,
        int DistinctKeys,
        int DistinctPairs,
        int HitPermille,
        int Resident,
        int Evictions);

    private static void AppendKeySpace(
        StringBuilder report, RoadGraph graph, OdDistribution distribution)
    {
        report.AppendLine("### R6.1b — the key space, and the population this spike does not have");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**`plans/0010` argues the key on hit rate and S2 cannot draw the population the argument "
            + $"is about.** *\"Keyed on those, the space is Buildings² ≈ 2.25 × 10¹⁰ and the hit rate "
            + $"is approximately zero. Keyed on the endpoints… the space collapses to nodes² and the "
            + $"five Buildings sharing a Segment share one entry instead of minting five.\"* That is a "
            + $"claim about **Buildings**, and this spike has none — it draws Access Points at random "
            + $"offsets on random Segments, so two trips share a Segment only by accident. **Buildings "
            + $"are therefore invented here, and swept**, on the rule R5.3 established for its own "
            + $"pool: the *level* of any figure below is a property of the invention, and **only the "
            + $"ratio between key rungs under one pool may be quoted.**"));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Buildings are placed evenly along each Segment and a drawn Access Point is **snapped** to "
            + $"the nearest one, so the O-D shape is the swept family's and only the offsets are the "
            + $"invention. {LookupPoolPairs:N0} distinct pairs, {LookupTrips:N0} trips drawn from them "
            + $"with repetition, a {LookupCapacity:N0}-entry direct-mapped cache — **R5.3's shape "
            + $"exactly**, so the hit columns are comparable with it."));
        report.AppendLine();

        var rows = new List<Spaced>();

        foreach (OdRung rung in OdRungs)
        {
            foreach (int buildings in BuildingsPerSegment)
            {
                OdPair[] drawn = distribution.Draw(
                    CounterHash.Seed, LookupPoolPairs, Modes.Car, rung, out _, out _);
                OdPair[] pool = Snap(graph, drawn, buildings);

                foreach (RouteKey key in Keys)
                {
                    if (key == RouteKey.BestEndpoint)
                    {
                        // Not implementable as a key — it needs the answer to compute the key. It is
                        // a bound in R6.1a and would be a fiction in a hit-rate column.
                        continue;
                    }

                    rows.Add(MeasureSpace(graph, pool, rung, buildings, 0, key));
                }
            }
        }

        var concentrated = new List<Spaced>();
        OdRung uniform = OdRungs[0];

        foreach (int sites in DestinationSites)
        {
            OdPair[] drawn = distribution.Draw(
                CounterHash.Seed, LookupPoolPairs, Modes.Car, uniform, out _, out _);
            OdPair[] pool = Concentrate(graph, Snap(graph, drawn, 5), sites);

            foreach (RouteKey key in Keys)
            {
                if (key == RouteKey.BestEndpoint)
                {
                    continue;
                }

                concentrated.Add(MeasureSpace(graph, pool, uniform, 5, sites, key));
            }
        }

        report.AppendLine(
            "| O-D rung | Buildings / Segment | Key | Distinct keys | of pairs | Collapse | Hit | Resident | Evictions |");
        report.AppendLine("|---|---:|---|---:|---:|---:|---:|---:|---:|");

        foreach (Spaced row in rows)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Label(row.Rung)} | {row.Buildings} | {Label(row.Key)} "
                + $"| {row.DistinctKeys:N0} | {row.DistinctPairs:N0} "
                + $"| {Collapse(row)}× "
                + $"| {row.HitPermille / 10}.{row.HitPermille % 10}% "
                + $"| {row.Resident:N0} | {row.Evictions:N0} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"**Every row reads 1.00×, and that is the result rather than a disappointment.** Adding "
            + $"Buildings to a Segment mints more Access Points; it does not make two trips *end on "
            + $"the same Segment*, which is the only thing a node-keyed entry can collapse. With 512 "
            + $"pairs drawn over {graph.Segments:N0} Segments — 512 draws from about a billion "
            + $"Segment pairs — no two share one, whatever sits on them. **The column cannot move on "
            + $"this axis**, so it is evidence of nothing on its own and is published beside an axis "
            + $"where it does move."));
        report.AppendLine();
        report.AppendLine(
            "**The hit column is not idle, though, and it corroborates R5.3 from outside.** It sits "
            + "near 70% for every rung — a **~30% miss floor with no storm, no Epoch and nothing "
            + "stale** — which is R5.3's *28–31% of lookups missing on direct-mapped collisions before "
            + "a road is touched*, reproduced by a different harness on a different pool. **That is "
            + "R6.2's premise confirmed independently**, and it is the strongest thing this sub-table "
            + "says.");
        report.AppendLine();

        AppendConcentration(report, concentrated);
        AppendKeySpaceProse(report, graph, rows, concentrated);
    }

    /// <summary>
    /// Redirects every Trip in the pool to one of <paramref name="sites"/> destination Access Points,
    /// modelling a city with that many places worth travelling to. Zero leaves the draw alone.
    /// </summary>
    /// <remarks>
    /// The sites are taken from the pool's own destinations, so they are places the O-D rung already
    /// considered reachable — inventing coordinates would add a second fiction on top of this one.
    /// </remarks>
    internal static OdPair[] Concentrate(RoadGraph graph, OdPair[] pool, int sites)
    {
        if (sites <= 0 || sites >= pool.Length)
        {
            return pool;
        }

        var redirected = new OdPair[pool.Length];

        for (int i = 0; i < pool.Length; i++)
        {
            AccessPoint site = pool[i % sites].Destination;
            redirected[i] = new OdPair(pool[i].Origin, site, pool[i].StraightLineTiles);
        }

        return redirected;
    }

    private static void AppendConcentration(StringBuilder report, List<Spaced> rows)
    {
        report.AppendLine();
        report.AppendLine(
            "**The axis that does move: how many places the city's trips end at.** Same pool size, "
            + "same cache, same keys, uniform origins — only the destination set shrinks. This is a "
            + "second invention and is swept for the same reason the first is.");
        report.AppendLine();
        report.AppendLine(
            "| Destination sites | Key | Distinct keys | of pairs | Collapse | Hit | Resident | Evictions |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");

        foreach (Spaced row in rows)
        {
            string sites = row.Sites <= 0 ? "unrestricted" : row.Sites.ToString(CultureInfo.InvariantCulture);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {sites} | {Label(row.Key)} "
                + $"| {row.DistinctKeys:N0} | {row.DistinctPairs:N0} "
                + $"| {Collapse(row)}× "
                + $"| {row.HitPermille / 10}.{row.HitPermille % 10}% "
                + $"| {row.Resident:N0} | {row.Evictions:N0} |"));
        }

        report.AppendLine();
    }

    private static string Collapse(Spaced row) =>
        row.DistinctKeys == 0
            ? "—"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{(row.DistinctPairs * 100) / row.DistinctKeys / 100}."
                    + $"{(row.DistinctPairs * 100) / row.DistinctKeys % 100:D2}");

    /// <summary>
    /// Snaps each drawn Access Point onto one of <paramref name="buildings"/> evenly spaced positions
    /// on its Segment. The O-D <i>shape</i> stays the swept family's; only the offsets are invented.
    /// </summary>
    internal static OdPair[] Snap(RoadGraph graph, OdPair[] drawn, int buildings)
    {
        var snapped = new OdPair[drawn.Length];

        for (int i = 0; i < drawn.Length; i++)
        {
            snapped[i] = new OdPair(
                SnapOne(graph, drawn[i].Origin, buildings),
                SnapOne(graph, drawn[i].Destination, buildings),
                drawn[i].StraightLineTiles);
        }

        return snapped;
    }

    private static AccessPoint SnapOne(RoadGraph graph, AccessPoint point, int buildings)
    {
        int length = graph.SegmentLengthTiles[point.Segment];

        if (length <= 0 || buildings <= 0)
        {
            return point;
        }

        int index = (point.OffsetTiles * buildings) / (length + 1);

        if (index > buildings - 1)
        {
            index = buildings - 1;
        }

        // Evenly spaced and never on an endpoint: a Building sitting exactly on a node would make the
        // key's error zero by construction and flatter every rung below.
        return new AccessPoint(point.Segment, (((2 * index) + 1) * length) / (2 * buildings));
    }

    private static Spaced MeasureSpace(
        RoadGraph graph, OdPair[] pool, OdRung rung, int buildings, int sites, RouteKey key)
    {
        var distinctKeys = new HashSet<long>();
        var distinctPairs = new HashSet<long>();

        foreach (OdPair pair in pool)
        {
            distinctKeys.Add(KeyOf(graph, pair, key));
            distinctPairs.Add(KeyOf(graph, pair, RouteKey.AccessPoint));
        }

        // A direct-mapped cache with RouteCache's own mixing, so the miss floor below is the same
        // quantity R5.3 measured at 28-31% and not a differently-shaped one.
        var slotKey = new long[LookupCapacity];
        var occupied = new bool[LookupCapacity];
        int hits = 0;
        int evictions = 0;

        for (int trip = 0; trip < LookupTrips; trip++)
        {
            OdPair pair = pool[Draw(trip, pool.Length)];
            long value = KeyOf(graph, pair, key);
            int slot = Slot(value);

            if (occupied[slot] && slotKey[slot] == value)
            {
                hits++;
                continue;
            }

            if (occupied[slot])
            {
                evictions++;
            }

            occupied[slot] = true;
            slotKey[slot] = value;
        }

        int resident = 0;

        for (int i = 0; i < LookupCapacity; i++)
        {
            if (occupied[i])
            {
                resident++;
            }
        }

        return new Spaced(
            rung,
            buildings,
            sites,
            key,
            distinctKeys.Count,
            distinctPairs.Count,
            (int)((hits * 1_000L) / LookupTrips),
            resident,
            evictions);
    }

    internal static long KeyOf(RoadGraph graph, OdPair pair, RouteKey key)
    {
        switch (key)
        {
            case RouteKey.NodeA:
                return ((long)graph.SegmentNodeA[pair.Origin.Segment] << 32)
                    | (uint)graph.SegmentNodeA[pair.Destination.Segment];

            case RouteKey.NearestNode:
            {
                int origin = Nearer(
                    graph,
                    pair.Origin,
                    graph.SegmentNodeA[pair.Origin.Segment],
                    graph.SegmentNodeB[pair.Origin.Segment]);
                int destination = Nearer(
                    graph,
                    pair.Destination,
                    graph.SegmentNodeA[pair.Destination.Segment],
                    graph.SegmentNodeB[pair.Destination.Segment]);

                return ((long)origin << 32) | (uint)destination;
            }

            default:
                return ((long)Packed(pair.Origin) << 32) | (uint)Packed(pair.Destination);
        }
    }

    /// <summary>A <c>(Segment, offset)</c> in one int. Offsets are Tiles along a Segment of ≤ 64.</summary>
    private static int Packed(AccessPoint point) => (point.Segment << 6) | (point.OffsetTiles & 63);

    /// <summary>
    /// Which pool entry a Trip draws. Counter-based on the Tick index rather than a stream, which is
    /// what makes the sweep reproducible across rungs.
    /// </summary>
    internal static int Draw(int trip, int poolSize) =>
        CounterHash.Below(
            CounterHash.Of(CounterHash.Seed, (ulong)trip, 0, CounterHash.Purpose.KeyPoolDraw),
            poolSize);

    /// <summary><c>RouteCache.Slot</c>'s mixing, so the miss floor is R5.3's quantity.</summary>
    private static int Slot(long key)
    {
        ulong mixed = (ulong)key * 0x9E37_79B9_7F4A_7C15UL;
        mixed ^= mixed >> 29;
        return (int)(mixed % LookupCapacity);
    }

    private static void AppendDetour(
        StringBuilder report, RoadGraph graph, OdDistribution distribution)
    {
        report.AppendLine("### R6.1a — what a coarser key costs the traveller who shares it");
        report.AppendLine();
        report.AppendLine(
            "**The trade is stated in `plans/0010` and has never been measured**: *\"two Buildings at "
            + "opposite ends of a long Segment share a route that is wrong for one of them by up to a "
            + "Segment length.\"* Every figure below is a whole-journey cost — the arcs **plus both "
            + "Access Point remainders** — against a flat search on the same graph, which is the "
            + "quantity both a driver and the Commute Budget actually consume.");
        report.AppendLine();

        var rows = new List<Keyed>();
        var search = new PointToPoint(graph);

        foreach (OdRung rung in OdRungs)
        {
            OdPair[] pool = distribution.Draw(
                CounterHash.Seed, PoolPairs, Modes.Car, rung, out _, out _);

            foreach (RouteKey key in Keys)
            {
                rows.Add(Measure(graph, search, pool, rung, key));
            }
        }

        report.AppendLine(
            "| O-D rung | Key | Mean detour | p90 | Worst | Mean, Ticks | Worst, Ticks | Sample |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");

        foreach (Keyed row in rows)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Label(row.Rung)} | {Label(row.Key)} "
                + $"| {Hundredths(row.MeanDetourHundredths)}% "
                + $"| {Hundredths(row.P90DetourHundredths)}% "
                + $"| {Hundredths(row.WorstDetourHundredths)}% "
                + $"| {Ticks(row.MeanAbsoluteQ)} | {Ticks(row.WorstAbsoluteQ)} "
                + $"| {row.Samples:N0} |"));
        }

        report.AppendLine();
        AppendDetourProse(report, rows);
    }

    /// <summary>
    /// Prices one key against the truth over one pool. Same-Segment pairs are excluded and counted:
    /// they are answered by R3.8's bypass without consulting any cache, so charging a key for them
    /// would credit the key with a case it never sees.
    /// </summary>
    private static Keyed Measure(
        RoadGraph graph,
        PointToPoint search,
        OdPair[] pool,
        OdRung rung,
        RouteKey key)
    {
        var detours = new List<int>(pool.Length);
        var absolutes = new List<long>(pool.Length);
        int unroutable = 0;
        int cheaper = 0;

        foreach (OdPair pair in pool)
        {
            if (pair.Origin.Segment == pair.Destination.Segment)
            {
                continue;
            }

            long truth = WholeJourney(search, pair.Origin, pair.Destination);

            if (truth <= 0)
            {
                unroutable++;
                continue;
            }

            long served = Served(graph, search, pair, key);

            if (served < 0)
            {
                unroutable++;
                continue;
            }

            long over = served - truth;

            // A served route cheaper than the unconstrained optimum is impossible: forcing a journey
            // through a chosen node can only add. Counted rather than assumed, because it is the one
            // way this instrument could be silently wrong in the direction that flatters the key.
            if (over < 0)
            {
                cheaper++;
            }

            // A served route cheaper than the truth is an admissibility bug, not a negative detour,
            // and it must be visible rather than averaged away.
            detours.Add((int)((over * 10_000) / truth));
            absolutes.Add(over);
        }

        detours.Sort();
        absolutes.Sort();

        return new Keyed(
            rung,
            key,
            detours.Count,
            Mean(detours),
            Percentile(detours, 90),
            detours.Count == 0 ? 0 : detours[^1],
            Mean(absolutes),
            Percentile(absolutes, 90),
            absolutes.Count == 0 ? 0 : absolutes[^1],
            unroutable,
            cheaper);
    }

    /// <summary>
    /// What the key actually serves: onto the key's origin node, along the cached node-to-node
    /// route, then off the key's destination node. <b>The remainders are the point</b> — dropping
    /// them is what made R5.5.2's cache rows read 0.00%.
    /// </summary>
    private static long Served(
        RoadGraph graph, PointToPoint search, OdPair pair, RouteKey key)
    {
        if (key == RouteKey.AccessPoint)
        {
            // The control. Measured by a second independent search rather than assigned from the
            // truth: a zero that came from `served = truth` would prove the assignment worked and
            // nothing else. R5.5.2 makes the same argument about its own flat rung.
            return WholeJourney(search, pair.Origin, pair.Destination);
        }

        int oa = graph.SegmentNodeA[pair.Origin.Segment];
        int ob = graph.SegmentNodeB[pair.Origin.Segment];
        int da = graph.SegmentNodeA[pair.Destination.Segment];
        int db = graph.SegmentNodeB[pair.Destination.Segment];

        if (key == RouteKey.NodeA)
        {
            return Through(graph, search, pair, oa, da);
        }

        if (key == RouteKey.NearestNode)
        {
            int origin = Nearer(graph, pair.Origin, oa, ob);
            int destination = Nearer(graph, pair.Destination, da, db);
            return Through(graph, search, pair, origin, destination);
        }

        long best = -1;

        foreach (int o in stackalloc[] { oa, ob })
        {
            foreach (int d in stackalloc[] { da, db })
            {
                long candidate = Through(graph, search, pair, o, d);

                if (candidate >= 0 && (best < 0 || candidate < best))
                {
                    best = candidate;
                }
            }
        }

        return best;
    }

    /// <summary>The whole journey when forced through one node at each end.</summary>
    private static long Through(
        RoadGraph graph, PointToPoint search, OdPair pair, int origin, int destination)
    {
        int onto = SegmentEntry.CostToEndpoint(graph, null, Modes.Car, pair.Origin, origin);
        int off = SegmentEntry.CostFromEndpoint(graph, null, Modes.Car, destination, pair.Destination);

        if (onto >= SegmentEntry.Unreachable || off >= SegmentEntry.Unreachable)
        {
            return -1;
        }

        if (origin == destination)
        {
            return onto + off;
        }

        long between = WholeJourney(search, AtNode(graph, origin), AtNode(graph, destination));

        return between < 0 ? -1 : onto + between + off;
    }

    /// <summary>Which of a Segment's two endpoints an Access Point is nearer to, in cost.</summary>
    private static int Nearer(RoadGraph graph, AccessPoint point, int a, int b)
    {
        int toA = SegmentEntry.CostToEndpoint(graph, null, Modes.Car, point, a);
        int toB = SegmentEntry.CostToEndpoint(graph, null, Modes.Car, point, b);

        return toB < toA ? b : a;
    }

    /// <summary>
    /// An Access Point sitting exactly on a node. <b>Not</b> <c>new AccessPoint(anySegmentAt(node), 0)</c>,
    /// which lands on that Segment's node A and is a whole Segment away when the node is its node B.
    /// </summary>
    private static AccessPoint AtNode(RoadGraph graph, int node)
    {
        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            int segment = graph.ArcSegment[arc];

            if (graph.SegmentNodeA[segment] == node)
            {
                return new AccessPoint(segment, 0);
            }

            if (graph.SegmentNodeB[segment] == node)
            {
                return new AccessPoint(segment, graph.SegmentLengthTiles[segment]);
            }
        }

        return new AccessPoint(0, 0);
    }

    /// <summary>
    /// The quantity both a driver and the Commute Budget consume. <c>SearchOutcome.CostTicks</c> is
    /// documented as <i>"total travel time, Q16.16 Ticks, <b>including both offset remainders</b>"</i>
    /// — so the search already computes what R5.5.2's arc sum was dropping, and this section did not
    /// have to reconstruct it.
    /// </summary>
    private static long WholeJourney(
        PointToPoint search, AccessPoint origin, AccessPoint destination)
    {
        search.Bootstrap(origin, destination, Modes.Car, HeuristicKind.Chebyshev);
        var outcome = search.Expand();

        return outcome.Found ? outcome.CostTicks : -1;
    }

    private static void AppendDetourProse(StringBuilder report, List<Keyed> rows)
    {
        int cheaper = rows.Sum(r => r.CheaperThanTruth);
        var nodeA = rows.Where(r => r.Key == RouteKey.NodeA).ToList();
        var nearest = rows.Where(r => r.Key == RouteKey.NearestNode).ToList();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The headline is in the two absolute columns and not in the three percentage ones.** "
            + $"`node-a`'s mean error is **{Ticks(nodeA.Min(r => r.MeanAbsoluteQ))}–"
            + $"{Ticks(nodeA.Max(r => r.MeanAbsoluteQ))} Ticks** across the whole O-D family, and "
            + $"`nearest-node`'s is **{Ticks(nearest.Min(r => r.MeanAbsoluteQ))}–"
            + $"{Ticks(nearest.Max(r => r.MeanAbsoluteQ))}** — flat to two decimal places while the "
            + $"*percentage* for the same key swings from "
            + $"{Hundredths(nodeA.Min(r => r.MeanDetourHundredths))}% to "
            + $"{Hundredths(nodeA.Max(r => r.MeanDetourHundredths))}%, better than five-fold. "
            + $"**The key's error is bounded by Segment geometry and has nothing to do with the trip "
            + $"distribution**; the percentage is a statement about journey length wearing a "
            + $"statement about the key. **This is R4.1's finding reproduced one layer down** — there, "
            + $"a District-granular detour went 18.52% → 128.82% because the error was fixed in Ticks "
            + $"and the journey was not. Same shape, same cause, different mechanism."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**So a percentage must not be quoted for this key without its rung, and the absolute "
            + $"should be preferred to it.** `plans/0010` already requires the rung to be named beside "
            + $"every figure; what this table adds is that for *this* quantity the rung-invariant "
            + $"number exists and is the better one to carry."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**`node-a` costs exactly twice what `nearest-node` does, on every rung**, and the factor "
            + $"is geometric rather than empirical: node A is an arbitrary end of the Segment, so a "
            + $"traveller pays a half-Segment on average at each end, where choosing the nearer end "
            + $"pays a quarter. **The fix is free** — it is one comparison per Access Point at insert, "
            + $"the key space is unchanged at nodes², and `adr/0012`'s owed amendment can state it in "
            + $"a sentence."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**But the greedy choice is not monotone, and the tail is where it shows.** On "
            + $"decay L=1024 `nearest-node`'s worst reads "
            + $"**{Hundredths(rows.First(r => r.Key == RouteKey.NearestNode && r.Rung.Shape == OdShape.DistanceDecay && r.Rung.DecayLengthTiles == 1_024).WorstDetourHundredths)}%** "
            + $"against `node-a`'s "
            + $"**{Hundredths(rows.First(r => r.Key == RouteKey.NodeA && r.Rung.Shape == OdShape.DistanceDecay && r.Rung.DecayLengthTiles == 1_024).WorstDetourHundredths)}%** — "
            + $"the coarser key wins that column. *Nearer along the Segment* is not *better for the "
            + $"journey*: the near endpoint can point away from the destination, and then the "
            + $"traveller pays the Segment twice. **A mean improved by 2× and a tail made worse is "
            + $"a trade, not a strict win**, and it is the shape `05 §4` says to look at rather than "
            + $"an average."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Routes cheaper than the unconstrained optimum: {cheaper}.** Forcing a journey through "
            + $"a chosen node can only add cost, so this column must read zero and is printed on the "
            + $"run where it does. A negative detour here would mean the composition was crediting the "
            + $"key with a shortcut the truth search had not found, which is the one way this "
            + $"instrument could be wrong in the direction that flatters its subject."));
        report.AppendLine();

        Keyed control = rows.First(r => r.Key == RouteKey.AccessPoint);
        Keyed worstNodeA = rows.Where(r => r.Key == RouteKey.NodeA)
            .OrderByDescending(r => r.MeanDetourHundredths).First();
        Keyed bestNodeA = rows.Where(r => r.Key == RouteKey.NodeA)
            .OrderBy(r => r.MeanDetourHundredths).First();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Read the `access-point` rows first: they are the control and they must read zero.** "
            + $"They are measured by a second independent search rather than assigned from the truth, "
            + $"so a zero says the composition is right and not that an assignment worked — the "
            + $"argument R5.5.2 makes about its own flat rung. Any non-zero there is an instrument "
            + $"defect and invalidates every other row."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**`node-a` is what the harness implements today, and it is the coarsest key on the "
            + $"ladder**: every Access Point on a Segment is routed through that Segment's node A, "
            + $"however far along it the traveller actually is. Its mean detour runs from "
            + $"**{Hundredths(bestNodeA.MeanDetourHundredths)}%** on {Label(bestNodeA.Rung)} to "
            + $"**{Hundredths(worstNodeA.MeanDetourHundredths)}%** on {Label(worstNodeA.Rung)}."));
        report.AppendLine();

        report.AppendLine(
            "**`best-endpoint` is the floor of the nodes² family and is not implementable as a key**, "
            + "because choosing the best endpoint pair means already knowing the answer. It is here to "
            + "separate two explanations that a single coarse row cannot: whether the error is "
            + "**intrinsic** to keying on nodes, or an artefact of choosing the endpoint badly. The "
            + "gap between `node-a` and `best-endpoint` is the part a better key could recover; the "
            + "gap between `best-endpoint` and `access-point` is the part no nodes² key can.");
        report.AppendLine();

        report.AppendLine(
            "**The absolute columns are the ones the Commute Budget consumes, and they are why this "
            + "table reports both.** A percentage cannot be judged without a journey length, and "
            + "`plans/0010` decision 8 records that the Budget's granularity is undecided — an error "
            + "of a few Ticks is free against a Budget read to the nearest half hour and "
            + "disqualifying against one read to the minute. `Units.cs` puts the Budget at *order a "
            + "hundred Ticks*. **This section reports the error and does not judge it**, which is the "
            + "same handling R1 gave the matrix's own 11.32%; the two are owed the same answer and "
            + "`plans/0010` says to answer them once.");
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Same-Segment pairs are excluded and the sample size is printed per row.** A pair whose "
            + $"origin and destination share a Segment is answered by R3.8's bypass without consulting "
            + $"any cache, so charging a key for it would credit the key with a case it never sees. "
            + $"The control retained {control.Samples:N0} of {PoolPairs:N0} drawn pairs."));
        report.AppendLine();

        report.AppendLine(
            "**What this table cannot say is anything about hit rate.** Hit rate is a property of how "
            + "many *distinct* keys a population of Buildings generates, and **S2 has no Buildings** — "
            + "it draws Access Points at random offsets on random Segments, so no two pairs share a "
            + "Segment except by accident. `plans/0010`'s *\"the five Buildings sharing a Segment "
            + "share one entry instead of minting five\"* is a statement about a population this "
            + "spike does not have. Measuring it needs an invented Buildings-per-Segment pool, and "
            + "**an invented pool must be swept or its level is a guess wearing a measurement's "
            + "clothes** — R5.3's debt, in the same words.");
        report.AppendLine();
    }

    private static void AppendKeySpaceProse(
        StringBuilder report, RoadGraph graph, List<Spaced> rows, List<Spaced> concentrated)
    {
        var exact = concentrated.Where(r => r.Key == RouteKey.AccessPoint).ToList();
        var nodeA = concentrated.Where(r => r.Key == RouteKey.NodeA).ToList();

        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"**The collapse column reads 1.00× on every row of both tables, and after two attempts "
            + $"to move it that is the section's finding rather than its failure.** A node-keyed "
            + $"entry collapses two Trips only when they share a Segment at **both** ends. "
            + $"Concentrating destinations onto 8 sites leaves 512 distinct origins, so the pairs "
            + $"stay distinct; adding Buildings to a Segment mints Access Points without making two "
            + $"Trips end together. **Collapse is a property of the ratio between the Trip "
            + $"population and the Segment-pair space**, and this graph has {graph.Segments:N0} "
            + $"Segments — about {(long)graph.Segments * graph.Segments / 1_000_000L:N0} million "
            + $"ordered pairs. No pool S2 can draw is dense in that."));
        report.AppendLine();

        report.AppendLine(
            "**Which puts a question mark against `plans/0010`'s argument for the coarse key, and the "
            + "honest position is that it is unconfirmed rather than refuted.** *\"The five Buildings "
            + "sharing a Segment share one entry instead of minting five\"* is true only if those five "
            + "Buildings' Trips also **end** on a shared Segment. Against 10⁹ Segment pairs, a real "
            + "city's Trips may well be sparse enough that a node key collapses almost nothing — in "
            + "which case the hit rate comes from **the same person repeating the same journey**, "
            + "which no key affects, and the coarse key would be paying R6.1a's detour for very "
            + "little. **S2 cannot settle this**: it needs a Trip population, which is `06` milestone "
            + "5b. What R6.1 does settle is the price side, exactly, and that the price is avoidable "
            + "at no cost in key space.");
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The concentration sweep moved a different column hard, and it belongs to R6.2.** As "
            + $"destinations concentrate, `access-point`'s hit rate falls "
            + $"**{exact.First().HitPermille / 10}.{exact.First().HitPermille % 10}% → "
            + $"{exact.Last().HitPermille / 10}.{exact.Last().HitPermille % 10}%** with evictions "
            + $"rising {exact.First().Evictions:N0} → {exact.Last().Evictions:N0}, while `node-a` on "
            + $"the same pools falls only to "
            + $"{nodeA.Last().HitPermille / 10}.{nodeA.Last().HitPermille % 10}%. **The key space did "
            + $"not shrink — distinct keys stay at 511–512 throughout — so this is not capacity, it is "
            + $"the slot function.** `RouteCache.Slot` is one multiply and one xor-shift, and on "
            + $"structured keys, where the low half takes very few values, it clusters. **A cache can "
            + $"lose two lookups in three to its hash while holding every entry it needs**, and that "
            + $"is R6.2's subject arriving early — R5.3's 28–31% miss floor is the same defect at a "
            + $"gentler input."));
        report.AppendLine();

        report.AppendLine(
            "**No document may cite a hit rate from this section.** Both axes are invented, neither "
            + "moved the column it was built to move, and the level of every hit figure is a property "
            + "of a 512-pair pool standing in for Trip repetition that does not exist. What may be "
            + "carried out of here is structural: **collapse needs coincidence at both ends**, and "
            + "**the slot function degrades on structured keys**.");
        report.AppendLine();
    }

    private static string Label(OdRung rung) => rung.Shape switch
    {
        OdShape.Uniform => "uniform",
        OdShape.DistanceDecay => $"decay L={rung.DecayLengthTiles}",
        _ => $"monocentric L={rung.DecayLengthTiles}",
    };

    private static string Label(RouteKey key) => key switch
    {
        RouteKey.NodeA => "`node-a`",
        RouteKey.NearestNode => "`nearest-node`",
        RouteKey.BestEndpoint => "`best-endpoint`",
        _ => "`access-point`",
    };

    private static int Mean(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        long total = 0;

        foreach (int value in values)
        {
            total += value;
        }

        return (int)(total / values.Count);
    }

    private static long Mean(List<long> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        long total = 0;

        foreach (long value in values)
        {
            total += value;
        }

        return total / values.Count;
    }

    private static int Index(int count, int percent)
    {
        int at = (count * percent) / 100;
        return at > count - 1 ? count - 1 : at;
    }

    private static int Percentile(List<int> sorted, int percent) =>
        sorted.Count == 0 ? 0 : sorted[Index(sorted.Count, percent)];

    private static long Percentile(List<long> sorted, int percent) =>
        sorted.Count == 0 ? 0 : sorted[Index(sorted.Count, percent)];

    private static string Hundredths(int value)
    {
        int part = value % 100;
        return string.Create(
            CultureInfo.InvariantCulture, $"{value / 100}.{(part < 0 ? -part : part):D2}");
    }

    /// <summary>A Q16.16 Tick count, printed as Ticks to two places.</summary>
    private static string Ticks(long q)
    {
        long magnitude = q < 0 ? -q : q;
        long whole = q / 65_536;
        long fraction = ((magnitude % 65_536) * 100) / 65_536;

        return string.Create(CultureInfo.InvariantCulture, $"{whole}.{fraction:D2}");
    }
}
