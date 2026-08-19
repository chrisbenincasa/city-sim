using System.Diagnostics;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// The route cache (5c task 4), and <b>the measurement that chooses its staleness rung</b> —
/// <c>adr/0012</c>'s caching bullet re-run on a real city instead of a synthetic lattice.
/// </summary>
/// <remarks>
/// <para>
/// <b>S2 R5 and R5.5.4 settled this on a graph with a uniform origin-destination draw, and R4
/// measured that a uniform draw is <em>a different city</em>.</b> Every figure in this corpus that
/// moved from a fixture to a real world moved the same way — the Rule unit by 2.8×, Trips per Tick by
/// 32%, 5c task 2's own error margin by half again. So the rungs are a switch and the numbers below
/// are taken here, on real home-to-work pairs produced by <c>EmploymentEngine</c>.
/// </para>
/// <para>
/// <b>The addition is measured by restoring a deletion, which is R5.5.4's technique exactly.</b>
/// Cache against the damaged graph, then put the Segments back — restoration <em>is</em> addition, and
/// it is the only way to guarantee every affected route was in the store before the road appeared.
/// </para>
/// </remarks>
public sealed class RouteCacheTests(ITestOutputHelper output)
{
    /// <summary>Entries in the measured store. Sets of four, so this is 256 sets.</summary>
    private const int Entries = 1024;

    /// <summary>
    /// Longest route an entry holds. 5c task 3 measured the golden fixture's longest at 19 Segments.
    /// </summary>
    private const int Stride = 64;

    /// <summary>Enough passes that a few hundred microseconds is a stable figure.</summary>
    private const int Repeats = 50;

    /// <summary>Ticks before the draw is taken. Half a Day, which is well past the job pass settling.</summary>
    private const int Settled = 1024;

    /// <summary>
    /// A hit returns exactly what a miss computes, which is what lets the exact rung stay invisible.
    /// </summary>
    /// <remarks>
    /// <b>The claim the whole cache rests on, and it is checkable without any consumer.</b> If a warm
    /// store ever answered differently from a cold one, the cache would be a change to the city rather
    /// than an optimisation — under <c>05 §4</c> that is the test, however the change was motivated.
    /// Every rung is checked, because a stale rung must still be exact when nothing has been edited.
    /// </remarks>
    [Theory]
    [InlineData(RouteStaleness.Exact)]
    [InlineData(RouteStaleness.Keep)]
    [InlineData(RouteStaleness.KeepAndRotate)]
    public void A_warm_store_answers_exactly_what_a_cold_one_computes(RouteStaleness policy)
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;
        int[] nodes = [.. WalkableNodes(graph).Take(24)];

        RouteCache warm = new(Entries, Stride, policy);
        RouteCache cold = new(Entries, Stride, policy);
        WalkScratch scratch = new();

        // Two passes over the warm store: the first fills it, the second must be served from it.
        for (int pass = 0; pass < 2; pass++)
        {
            foreach ((int from, int to) in Pairs(nodes))
            {
                _ = warm.Find(graph, from, to, TravelMode.Foot, scratch, out _);
            }
        }

        Assert.True(warm.Hits > 0, "the second pass hit nothing, so this proves nothing");

        foreach ((int from, int to) in Pairs(nodes))
        {
            bool warmFound = warm.Find(graph, from, to, TravelMode.Foot, scratch, out ReadOnlySpan<int> warmRoute);
            int[] kept = warmRoute.ToArray();

            cold.Clear();

            bool coldFound = cold.Find(graph, from, to, TravelMode.Foot, scratch, out ReadOnlySpan<int> coldRoute);

            Assert.Equal(coldFound, warmFound);
            Assert.Equal(coldRoute.ToArray(), kept);
        }
    }

    /// <summary>
    /// A bulldozed Segment is never on a served route, at every rung.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0012</c>'s removal half, which is not a policy and must hold everywhere.</b> The test
    /// is containment — <i>does this route contain that Segment</i> — run at the moment of use against
    /// the per-Segment Epoch, so there is no interval during which a Traveller drives through a road
    /// that is not there. The stale rungs are allowed to be wrong about an <em>addition</em> and about
    /// nothing else.
    /// </remarks>
    [Theory]
    [InlineData(RouteStaleness.Exact)]
    [InlineData(RouteStaleness.Keep)]
    [InlineData(RouteStaleness.KeepAndRotate)]
    public void No_rung_ever_serves_a_route_through_a_bulldozed_Segment(RouteStaleness policy)
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;
        int[] nodes = [.. WalkableNodes(graph).Take(24)];

        RouteCache cache = new(Entries, Stride, policy);
        WalkScratch scratch = new();

        foreach ((int from, int to) in Pairs(nodes))
        {
            _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
        }

        int demolished = Demolish(graph, out _);

        Assert.True(demolished > 0, "the gesture removed nothing, so this proves nothing");

        int served = 0;

        foreach ((int from, int to) in Pairs(nodes))
        {
            if (!cache.Find(graph, from, to, TravelMode.Foot, scratch, out ReadOnlySpan<int> route))
            {
                continue;
            }

            served++;

            foreach (int segment in route)
            {
                Assert.True(graph.Segments.Rows.IsLive(segment));
            }
        }

        Assert.True(served > 0);
    }

    /// <summary>
    /// ⚠ <b>The measurement that chooses the rung.</b> Hit rate, edit cost, staleness detour and the
    /// price of a lookup against a search — all on real home-to-work pairs.
    /// </summary>
    /// <remarks>
    /// <b>Not an assertion about a threshold; the numbers are the output.</b> It asserts only what
    /// would make the reading meaningless — that there were pairs to measure and that the gesture
    /// actually changed the graph — and prints the rest for <c>plans/0026</c> to carry. A tripwire
    /// here would be a number chosen to pass, which is the failure `adr/0043` is about.
    /// </remarks>
    [Trait(Tier.Key, Tier.Instrument)]
    [Fact]
    public void What_each_staleness_rung_costs_on_a_real_commute_draw()
    {
        // Long enough that EmploymentEngine has assigned jobs, so the pairs are the city's own rather
        // than a draw somebody invented. A uniform draw is what S2 had and what R4 measured as a
        // different city.
        Simulation simulation = Populated(GoldenFixtures.Population, Settled);

        (int From, int To)[] commutes = [.. CommutePairs(simulation)];

        Assert.True(commutes.Length > 64, $"only {commutes.Length} commute pairs — nothing to measure");

        output.WriteLine($"{commutes.Length} home-to-work node pairs, "
            + $"{Distinct(commutes)} distinct, {simulation.World.Roads.Segments.Rows.LiveCount} Segments live");
        output.WriteLine("");

        foreach (RouteStaleness policy in
            (RouteStaleness[])[RouteStaleness.Exact, RouteStaleness.Keep, RouteStaleness.KeepAndRotate])
        {
            Report(policy, commutes);
        }
    }

    private void Report(RouteStaleness policy, (int From, int To)[] commutes)
    {
        Simulation simulation = Populated(GoldenFixtures.Population, Settled);
        RoadGraph graph = simulation.World.Roads;
        RouteCache cache = new(Entries, Stride, policy);
        WalkScratch scratch = new();

        // --- warm, then measure the steady-state hit rate on a second pass -------------------------
        foreach ((int from, int to) in commutes)
        {
            _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
        }

        int filled = cache.Resident;
        int tooLong = cache.TooLong;

        cache.ResetCounters();

        foreach ((int from, int to) in commutes)
        {
            _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
        }

        int warmHits = cache.Hits;
        int warmMisses = cache.Misses;

        // --- the gesture: delete, then restore. Restoration is the addition. ------------------------
        cache.ResetCounters();

        int removed = Demolish(graph, out (int Column, int Row, StreetAxis Axis)[] gesture);

        // Traffic across the damaged graph, so the store is correct about the deletion before the
        // addition arrives — R5.5.4's ordering, and the reason the reading is about addition alone.
        foreach ((int from, int to) in commutes)
        {
            _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
        }

        int editFlushes = cache.Flushes;
        int editInvalidations = cache.Invalidations;
        int editSearches = cache.Misses;

        foreach ((int column, int row, StreetAxis axis) in gesture)
        {
            graph.LayStreet(column, row, axis);
        }

        graph.RebuildDerived();

        // --- what the store now serves, against what the graph now holds ---------------------------
        cache.ResetCounters();

        (int Stale, long Excess, long Fresh, int Worst) detour = Detour(graph, cache, scratch, commutes);

        // --- the rotation is given a window with traffic in it, because that is what teaches it -----
        int taught = 0;

        if (policy == RouteStaleness.KeepAndRotate)
        {
            // 0.40 forced refreshes a Tick over 1,024 Ticks — R5.5.4's winning row, scaled to this
            // store rather than to its 412.
            for (int tick = 0; tick < 1024; tick++)
            {
                cache.Rotate(RotationSlice(tick, Entries, period: 1024));

                foreach ((int from, int to) in Sample(commutes, tick))
                {
                    _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
                }
            }

            taught = Detour(graph, cache, scratch, commutes).Stale;
        }

        // --- price -----------------------------------------------------------------------------
        double hitMicros = Micros(() =>
        {
            foreach ((int from, int to) in commutes)
            {
                _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
            }
        }, commutes.Length);

        RouteCache cold = new(Entries, Stride, policy);

        double missMicros = Micros(() =>
        {
            cold.Clear();

            foreach ((int from, int to) in commutes)
            {
                _ = cold.Find(graph, from, to, TravelMode.Foot, scratch, out _);
            }
        }, commutes.Length);

        output.WriteLine($"--- {policy} ---");
        output.WriteLine($"  resident after warm       {filled} of {Entries}, {tooLong} routes too long for the stride");
        output.WriteLine($"  warm hit rate             {Percent(warmHits, warmHits + warmMisses)} "
            + $"({warmHits} hits, {warmMisses} misses)");
        output.WriteLine($"  the edit gesture          {removed} Segments removed and restored");
        output.WriteLine($"    flushes                 {editFlushes}");
        output.WriteLine($"    entries invalidated     {editInvalidations}");
        output.WriteLine($"    searches it caused      {editSearches} of {commutes.Length}");
        output.WriteLine($"  after the addition        {detour.Stale} of {commutes.Length} routes are stale");
        output.WriteLine($"    mean detour             {Percent(detour.Excess, detour.Fresh)} over the whole draw");
        output.WriteLine($"    worst single detour     {detour.Worst}%");

        if (policy == RouteStaleness.KeepAndRotate)
        {
            output.WriteLine($"    after 1024 Ticks        {taught} still stale");
        }

        output.WriteLine($"  lookup, warm              {hitMicros:F3} us");
        output.WriteLine($"  lookup, cold (a search)   {missMicros:F3} us");
        output.WriteLine("");
    }

    /// <summary>
    /// ⚠ <b>Is the hit rate a property of the city or of the store's size?</b> The sweep that decides
    /// whether <c>adr/0012</c>'s kill trigger has fired.
    /// </summary>
    /// <remarks>
    /// <b>That ADR's own revisit trigger says a low hit rate retires the cache</b> — *"if the hit rate
    /// comes back low, the amendment stands and the cache does not"* — so a number near the floor has
    /// to be attributed before it is quoted. Two causes want opposite responses: a draw with no
    /// repetition means the cache cannot pay at any size, and a store smaller than the working set means
    /// it pays at a larger one. The distinct-pair count separates them and this sweep confirms which.
    /// </remarks>
    [Trait(Tier.Key, Tier.Instrument)]
    [Fact]
    public void Whether_the_hit_rate_is_the_citys_or_the_stores()
    {
        Simulation simulation = Populated(GoldenFixtures.Population, Settled);
        RoadGraph graph = simulation.World.Roads;
        (int From, int To)[] commutes = [.. CommutePairs(simulation)];
        int distinct = Distinct(commutes);
        WalkScratch scratch = new();

        output.WriteLine($"{commutes.Length} lookups over {distinct} distinct pairs — "
            + $"{Percent(commutes.Length - distinct, commutes.Length)} of a single pass is a repeat");
        output.WriteLine("");
        output.WriteLine("  entries   resident   warm hit rate   pure-hit us   search us");

        foreach (int entries in (int[])[256, 512, 1024, 2048, 4096, 8192])
        {
            RouteCache cache = new(entries, Stride, RouteStaleness.Exact);

            foreach ((int from, int to) in commutes)
            {
                _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
            }

            int resident = cache.Resident;

            cache.ResetCounters();

            foreach ((int from, int to) in commutes)
            {
                _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
            }

            int hits = cache.Hits;
            int misses = cache.Misses;

            double blended = Micros(() =>
            {
                foreach ((int from, int to) in commutes)
                {
                    _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
                }
            }, commutes.Length);

            RouteCache cold = new(entries, Stride, RouteStaleness.Exact);

            double search = Micros(() =>
            {
                cold.Clear();

                foreach ((int from, int to) in commutes)
                {
                    _ = cold.Find(graph, from, to, TravelMode.Foot, scratch, out _);
                }
            }, commutes.Length);

            output.WriteLine($"  {entries,7}   {resident,8}   {Percent(hits, hits + misses),13}   "
                + $"{blended,11:F3}   {search,9:F3}");
        }

        Assert.True(distinct > 0);
    }

    /// <summary>
    /// ⚠ <b>Does the working set scale with the city?</b> The measurement that decides whether a store
    /// whose size is a design choice can serve a population that is not.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0012</c> calls this store <i>"a store whose size is a design choice and therefore
    /// constant in the city"</i>, and the sweep above shows the hit rate is a function of store ÷
    /// working set.</b> If the working set is the employed population, those two sentences cannot both
    /// hold at 1M. This is the shape S2 R1 found for the travel-time matrix — routes beside times at
    /// 4.06 GiB — which <c>adr/0047</c> fixed by moving route storage *here*. A cost that was moved is
    /// not a cost that was removed, so the arrival has to be checked at the destination.
    /// </remarks>
    [Fact]
    public void Whether_the_working_set_scales_with_the_city()
    {
        WalkScratch scratch = new();

        output.WriteLine("  citizens   employed   distinct pairs   Segments   longest route   "
            + "hit @1024   hit @4096");

        int smallest = 0;
        int largest = 0;

        foreach (int population in (int[])[1000, 2000, 4000, 8000, 16000])
        {
            Simulation simulation = Populated(population, Settled);
            RoadGraph graph = simulation.World.Roads;
            (int From, int To)[] commutes = [.. CommutePairs(simulation)];

            if (commutes.Length == 0)
            {
                output.WriteLine($"  {population,8}   nothing employed yet");

                continue;
            }

            int distinct = Distinct(commutes);
            int longest = Longest(graph, commutes, scratch);

            output.WriteLine($"  {population,8}   {commutes.Length,8}   {distinct,14}   "
                + $"{graph.Segments.Rows.LiveCount,8}   {longest,13}   "
                + $"{HitRate(graph, commutes, scratch, 1024),9}   {HitRate(graph, commutes, scratch, 4096),9}");

            smallest = smallest == 0 ? distinct : smallest;
            largest = distinct;
        }

        // The one claim this table makes, asserted so it cannot rot into a printout nobody reads: the
        // working set grows with the city. A store whose size is a design choice therefore covers a
        // shrinking fraction of it, which is the finding — not the hit rate at any one rung.
        Assert.True(
            largest > smallest * 8,
            $"distinct pairs went {smallest} -> {largest} over a 16x population change, "
            + "so the working set is not population-scaled and the scaling finding does not hold");
    }

    /// <summary>The warm hit rate a store of a given size gives on a draw.</summary>
    private static string HitRate(
        RoadGraph graph, (int From, int To)[] commutes, WalkScratch scratch, int entries)
    {
        RouteCache cache = new(entries, Stride, RouteStaleness.Exact);

        for (int pass = 0; pass < 2; pass++)
        {
            if (pass == 1)
            {
                cache.ResetCounters();
            }

            foreach ((int from, int to) in commutes)
            {
                _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
            }
        }

        return Percent(cache.Hits, cache.Hits + cache.Misses);
    }

    /// <summary>The longest route in the draw. What the store's stride would have to be.</summary>
    private static int Longest(
        RoadGraph graph, (int From, int To)[] commutes, WalkScratch scratch)
    {
        RouteCache cache = new(RouteCache.Ways, 4096, RouteStaleness.Exact);
        int longest = 0;

        foreach ((int from, int to) in commutes)
        {
            if (cache.Find(graph, from, to, TravelMode.Foot, scratch, out ReadOnlySpan<int> route))
            {
                longest = Math.Max(longest, route.Length);
            }
        }

        return longest;
    }

    /// <summary>
    /// ⚠ <b>The replacement policy against the access pattern a commute actually is</b>, plus the two
    /// numbers that decide whether a shared pair-keyed store can work at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A commute is a once-per-Day cyclic scan and LRU is provably worst on one.</b> Every employed
    /// Citizen departs once a Day and <c>CommuteRoster</c> puts each in a fixed bucket, so the order is
    /// stable across Days. Over a working set larger than the store, LRU evicts precisely the entry
    /// needed next. The rungs here test whether anything recovers it — and the answer bounds itself,
    /// because on a *uniform* scan no policy can retain a better subset than an arbitrary one.
    /// </para>
    /// <para>
    /// ⚠ <b>The sharing column is the number <c>adr/0012</c> says could not be taken.</b> That ADR:
    /// *"the price of the key is settled exactly and the benefit cannot be settled at all until Trip
    /// generation exists (`06` 5b)"*. It exists. This is the benefit, and it is the fraction of
    /// commutes that share a node pair with another commute — the only thing a store keyed by pair
    /// rather than by traveller can ever buy.
    /// </para>
    /// <para>
    /// <b>The median column exists because 5c task 4's first filing extrapolated a maximum.</b> Memory
    /// scales on the middle of a distribution and the earlier reading was its tail, which is the same
    /// error <c>plans/0012</c> **Cause 5** governs, committed on a number this session produced itself.
    /// </para>
    /// </remarks>
    [Trait(Tier.Key, Tier.Instrument)]
    [Fact]
    public void Which_replacement_policy_survives_a_commute_scan()
    {
        WalkScratch scratch = new();

        foreach (int population in (int[])[4000, 16000])
        {
            Simulation simulation = Populated(population, Settled);
            RoadGraph graph = simulation.World.Roads;
            (int From, int To)[] commutes = [.. CommutePairs(simulation)];

            if (commutes.Length == 0)
            {
                continue;
            }

            int distinct = Distinct(commutes);
            int[] lengths = [.. RouteLengths(graph, commutes, scratch)];

            Array.Sort(lengths);

            output.WriteLine($"=== {population} Citizens: {commutes.Length} commutes, "
                + $"{distinct} distinct pairs ===");
            output.WriteLine($"  shared: {Percent(commutes.Length - distinct, commutes.Length)} of "
                + "commutes share a node pair with another — all a pair key can ever buy");
            output.WriteLine($"  route length: median {lengths[lengths.Length / 2]}, "
                + $"p90 {lengths[lengths.Length * 9 / 10]}, max {lengths[^1]} Segments");
            output.WriteLine("");
            output.WriteLine("  store    ceiling      Lru      Mru   Random     None");

            foreach (int entries in (int[])[256, 1024, 4096])
            {
                string ceiling = Percent(Math.Min(entries, distinct), distinct);

                output.WriteLine($"  {entries,5}   {ceiling,8}   "
                    + $"{Scan(graph, commutes, scratch, entries, RouteEviction.Lru),6}   "
                    + $"{Scan(graph, commutes, scratch, entries, RouteEviction.Mru),6}   "
                    + $"{Scan(graph, commutes, scratch, entries, RouteEviction.Random),6}   "
                    + $"{Scan(graph, commutes, scratch, entries, RouteEviction.None),6}");
            }

            output.WriteLine("");
        }
    }

    /// <summary>
    /// The steady-state hit rate over repeated Days — <b>the scan, not a warm-up</b>.
    /// </summary>
    /// <remarks>
    /// <b>Four passes and the reading is the fourth</b>, because one pass measures a store filling up
    /// and two measures the transient after it filled. A cyclic access pattern's cost only appears once
    /// the store has been round the cycle at least twice.
    /// </remarks>
    private static string Scan(
        RoadGraph graph,
        (int From, int To)[] commutes,
        WalkScratch scratch,
        int entries,
        RouteEviction eviction)
    {
        RouteCache cache = new(entries, Stride, RouteStaleness.Exact, eviction);

        for (int day = 0; day < 4; day++)
        {
            if (day == 3)
            {
                cache.ResetCounters();
            }

            foreach ((int from, int to) in commutes)
            {
                _ = cache.Find(graph, from, to, TravelMode.Foot, scratch, out _);
            }
        }

        return Percent(cache.Hits, cache.Hits + cache.Misses);
    }

    /// <summary>Every commute's route length, so a median can be taken rather than a maximum.</summary>
    private static IEnumerable<int> RouteLengths(
        RoadGraph graph, (int From, int To)[] commutes, WalkScratch scratch)
    {
        RouteCache cache = new(RouteCache.Ways, 4096, RouteStaleness.Exact);

        foreach ((int from, int to) in commutes)
        {
            if (cache.Find(graph, from, to, TravelMode.Foot, scratch, out ReadOnlySpan<int> route))
            {
                yield return route.Length;
            }
        }
    }

    /// <summary>
    /// How many routes the store serves that are worse than a fresh search, and by how much.
    /// </summary>
    /// <remarks>
    /// <b>Cost against cost, never route against route.</b> Two different Segment lists of identical
    /// cost are the same answer — a grid of identical Streets produces them constantly — so comparing
    /// the lists would report a detour of zero as a difference and make every rung look equally bad.
    /// </remarks>
    private static (int Stale, long Excess, long Fresh, int Worst) Detour(
        RoadGraph graph, RouteCache cache, WalkScratch scratch, (int From, int To)[] commutes)
    {
        int stale = 0;
        long excess = 0;
        long fresh = 0;
        int worst = 0;

        foreach ((int from, int to) in commutes)
        {
            if (!cache.Find(graph, from, to, TravelMode.Foot, scratch, out ReadOnlySpan<int> route))
            {
                continue;
            }

            long served = CostOf(graph, from, route);

            scratch.Begin(graph.Nodes.Rows.SlotCount);
            scratch.Seed(from, TravelTime.Zero);
            scratch.SettleAll(graph, TravelMode.Foot);

            TravelTime best = scratch.CostTo(to);

            if (best.IsImpassable || served <= best.Raw)
            {
                fresh += best.IsImpassable ? 0 : best.Raw;

                continue;
            }

            stale++;
            fresh += best.Raw;
            excess += served - best.Raw;

            if (best.Raw > 0)
            {
                worst = Math.Max(worst, (int)(100 * (served - best.Raw) / best.Raw));
            }
        }

        return (stale, excess, fresh, worst);
    }

    /// <summary>What a Segment list actually costs on the graph as it now stands.</summary>
    private static long CostOf(RoadGraph graph, int origin, ReadOnlySpan<int> route)
    {
        long total = 0;
        int cursor = origin;

        foreach (int segment in route)
        {
            int start = graph.Nodes.ArcStart[cursor];
            int taken = -1;

            for (int i = start; i < start + graph.Nodes.ArcCount[cursor]; i++)
            {
                if (graph.Arcs.Segment[i] != segment || !graph.Arcs.Admits(i, TravelMode.Foot))
                {
                    continue;
                }

                if (taken < 0 || graph.Arcs.FootTime[i] < graph.Arcs.FootTime[taken])
                {
                    taken = i;
                }
            }

            if (taken < 0)
            {
                return long.MaxValue;
            }

            total += graph.Arcs.FootTime[taken].Raw;
            cursor = graph.Arcs.Target[taken];
        }

        return total;
    }

    /// <summary>
    /// Bulldozes a run of Streets — <b>a gesture rather than an edit</b>, which is S2 R5's whole point.
    /// </summary>
    /// <remarks>
    /// <b>A player does not delete a Segment; a player drags.</b> R3 and R4 both priced one Segment and
    /// both said in their own words that the case they could not reach was many Segments in a single
    /// gesture. Four Segments is the smallest run worth drawing at these block sizes.
    /// </remarks>
    private static int Demolish(RoadGraph graph, out (int Column, int Row, StreetAxis Axis)[] gesture)
    {
        List<(int, int, StreetAxis)> removed = [];

        for (int column = 2; column < 6 && removed.Count < 4; column++)
        {
            if (graph.BulldozeStreet(column, 3, StreetAxis.East))
            {
                removed.Add((column, 3, StreetAxis.East));
            }
        }

        graph.RebuildDerived();
        gesture = [.. removed];

        return removed.Count;
    }

    /// <summary>
    /// How many entries a rotation of the given period drops this Tick, without accumulating error.
    /// </summary>
    /// <remarks>
    /// <b>Derived from the period, because a period is what the design authors and a rate is what it
    /// implies</b> — the same relationship <c>[jobs] commute_peak_factor</c> has with its departure
    /// window (`adr/0059`, a fifth time). Computed as a difference of two integer divisions so that a
    /// store that is not a whole multiple of the period still sweeps exactly once per period rather
    /// than drifting.
    /// </remarks>
    private static int RotationSlice(int tick, int entries, int period)
    {
        int within = tick % period;

        return (int)(((long)entries * (within + 1) / period) - ((long)entries * within / period));
    }

    /// <summary>Real home-to-work node pairs, taken from Citizens the job pass has employed.</summary>
    /// <remarks>
    /// <b>The nearest endpoint of each Address's Segment, which is R6.1a's one comparison at insert.</b>
    /// Always taking the <c>a</c> end costs exactly 2× the nearest end on every rung, geometrically, and
    /// it is the caller's job because only the caller holds the Address.
    /// </remarks>
    private static IEnumerable<(int From, int To)> CommutePairs(Simulation simulation)
    {
        World world = simulation.World;

        for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
        {
            if (!world.Citizens.Rows.IsLive(citizen)
                || !world.Buildings.Rows.TryResolve(world.Citizens.Workplace[citizen], out int workplace)
                || !world.Households.Rows.TryResolve(world.Citizens.HouseholdOf[citizen], out int household)
                || !world.Buildings.Rows.TryResolve(world.Households.Dwelling[household], out int home))
            {
                continue;
            }

            if (!Nearest(world, world.PedestrianAccessPoint(home), out int from)
                || !Nearest(world, world.PedestrianAccessPoint(workplace), out int to)
                || from == to)
            {
                continue;
            }

            yield return (from, to);
        }
    }

    /// <summary>The endpoint of an Address's Segment that the Address is nearer to.</summary>
    private static bool Nearest(World world, Address address, out int node)
    {
        node = Rows.NoSlot;

        RoadGraph graph = world.Roads;

        if (!address.Exists || !graph.Segments.Rows.IsLive(address.Segment))
        {
            return false;
        }

        int segment = address.Segment;

        if (!graph.Nodes.Rows.TryResolve(graph.Segments.NodeA[segment], out int a)
            || !graph.Nodes.Rows.TryResolve(graph.Segments.NodeB[segment], out int b))
        {
            return false;
        }

        Tiles length = graph.Segments.LengthTiles[segment];

        node = address.Offset.Raw * 2 <= length.Raw ? a : b;

        return true;
    }

    /// <summary>Distinct node pairs in the draw — the ceiling on what any cache of any size could hold.</summary>
    private static int Distinct((int From, int To)[] commutes)
    {
        HashSet<(int, int)> seen = [];

        foreach ((int from, int to) in commutes)
        {
            seen.Add((from, to));
        }

        // Enumerated nowhere — Count is a read, and the hash-map ban is on walking one (05 §4 lint 3).
        return seen.Count;
    }

    private static IEnumerable<(int From, int To)> Pairs(int[] nodes)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            yield return (nodes[i], nodes[(i + 7) % nodes.Length]);
        }
    }

    /// <summary>A slice of the draw, so a rotation window has traffic without re-running the whole set.</summary>
    private static IEnumerable<(int From, int To)> Sample((int From, int To)[] commutes, int tick)
    {
        for (int i = 0; i < 8; i++)
        {
            yield return commutes[((tick * 8) + i) % commutes.Length];
        }
    }

    private static IEnumerable<int> WalkableNodes(RoadGraph graph)
    {
        for (int node = 0; node < graph.Nodes.Rows.SlotCount; node++)
        {
            if (!graph.Nodes.Rows.IsLive(node))
            {
                continue;
            }

            int start = graph.Nodes.ArcStart[node];

            for (int i = start; i < start + graph.Nodes.ArcCount[node]; i++)
            {
                if (graph.Arcs.Admits(i, TravelMode.Foot))
                {
                    yield return node;

                    break;
                }
            }
        }
    }

    private static string Percent(long part, long whole) =>
        whole == 0 ? "n/a" : $"{100.0 * part / whole:F2}%";

    private static double Micros(Action pass, int operations)
    {
        pass();

        Stopwatch clock = Stopwatch.StartNew();

        for (int i = 0; i < Repeats; i++)
        {
            pass();
        }

        clock.Stop();

        return clock.Elapsed.TotalMicroseconds / (Repeats * (double)operations);
    }

    /// <summary>
    /// A populated city, advanced <paramref name="ticks"/> Ticks through the replay door.
    /// </summary>
    /// <remarks>
    /// <b>Through <c>Replay.Trace</c> rather than <c>Simulation.Step</c>, because a bare
    /// <c>TickInput</c> names Ruleset <c>0x0</c> and the reload path refuses it.</b> The log is the
    /// door every command goes through (<c>adr/0080</c>), and stepping around it is how a test ends up
    /// exercising a world no session could produce.
    /// </remarks>
    private static Simulation Populated(int population, int ticks = 1)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, GoldenFixtures.Rules());
        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

        Replay.Trace(simulation, log, new Ticks((ulong)ticks), ticks, []);

        return simulation;
    }
}
