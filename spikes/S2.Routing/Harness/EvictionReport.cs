using System.Globalization;
using System.Text;
using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Harness;

/// <summary>
/// R6.2 — the eviction policy, which <c>adr/0012</c> never states and <c>adr/0017</c> has a pattern
/// for that nobody has written down for routes.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the one part of R6 that does not depend on the number nobody has measured.</b> The
/// cache's <i>hit rate</i> rests entirely on Trip repetition, which needs Trip generation — R5.3 said
/// so of its own pool and R6.1b closed the one other candidate source. But a lookup that <b>should</b>
/// have hit and did not is a pure loss whatever the repetition rate turns out to be, so this section
/// is robust where the rest of R6 is conditional.
/// </para>
/// <para>
/// <b>It therefore reports blame rather than a rate.</b> A hit percentage cannot distinguish a cache
/// that is too small from one that is throwing away entries it still holds, and those want opposite
/// repairs. The classification is the standard three: <i>cold</i> (never seen), <i>capacity</i> (a
/// perfect cache of the same size would also have missed) and <i>conflict</i> (a perfect cache of the
/// same size would have hit, and this scheme missed anyway). <b>Conflict is the only column that is a
/// defect</b>, and it is the one R5.3's 28–31% floor and R6.1b's 71.9% → 15.9% collapse both live in.
/// </para>
/// </remarks>
internal static class EvictionReport
{
    private const int Capacity = 1_024;
    private const int Trips = 16_384;

    /// <summary>
    /// Pool sizes, as a load against <see cref="Capacity"/>. R5.3 ran one rung — 512 pairs into 1,024
    /// entries, which it described as 2× over-provisioning — and read a miss floor that did not move
    /// with edit rate. A floor measured at one load is a point, not a floor.
    /// </summary>
    private static readonly int[] PoolSizes = [256, 512, 1_024, 2_048];

    private enum Scheme
    {
        /// <summary>What <c>RouteCache</c> implements: one slot, <c>mix(key) % capacity</c>.</summary>
        DirectModulo,

        /// <summary>The same single slot, indexed by the <b>top</b> bits of the same multiply.</summary>
        DirectHighBits,

        /// <summary>Two ways per set, least-recently-used within the set.</summary>
        TwoWay,

        /// <summary>Four ways per set — <c>adr/0017</c>'s fixed-capacity least-used, sized.</summary>
        FourWay,

        /// <summary>Eight ways per set.</summary>
        EightWay,

        /// <summary>
        /// Fully associative LRU. Not shippable at this size and not meant to be: it is the
        /// <b>bound</b>, and the gap between it and a rung is exactly that rung's conflict misses.
        /// </summary>
        FullyAssociative,
    }

    private static readonly Scheme[] Schemes =
    [
        Scheme.DirectModulo, Scheme.DirectHighBits, Scheme.TwoWay, Scheme.FourWay,
        Scheme.EightWay, Scheme.FullyAssociative,
    ];

    private sealed record Blamed(
        int Pool,
        bool Concentrated,
        Scheme Scheme,
        int HitPermille,
        int ColdPermille,
        int CapacityPermille,
        int ConflictPermille,
        int MeanProbeHundredths);

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var distribution = new OdDistribution(graph, new OdSampler(graph));

        report.AppendLine("## S2 R6.2 — the eviction policy, and who is to blame for a miss");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**No eviction policy is stated anywhere in the corpus.** `adr/0012` permits caching and "
            + $"says nothing about what leaves; `adr/0017` shows the pattern — fixed capacity, "
            + $"least-used eviction — and nobody has written it down for routes. `RouteCache` "
            + $"implements neither: it is **direct-mapped with one slot**, and an insert whose slot is "
            + $"taken simply overwrites."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**This section reports blame rather than a rate**, because a hit percentage cannot tell "
            + $"a cache that is too small from one throwing away entries it still holds, and those "
            + $"want opposite repairs. *Cold* is a first reference and is unavoidable. *Capacity* is a "
            + $"miss a perfect cache of the same size would also have taken. **Conflict is a miss a "
            + $"perfect cache of the same size would have avoided, and it is the only column that is "
            + $"a defect.** {Capacity:N0} entries, {Trips:N0} Trips drawn with repetition."));
        report.AppendLine();

        var rows = new List<Blamed>();

        foreach (int pool in PoolSizes)
        {
            OdPair[] drawn = distribution.Draw(
                CounterHash.Seed, pool, Modes.Car, KeyReport.OdRungs[0], out _, out _);
            OdPair[] snapped = KeyReport.Snap(graph, drawn, 5);

            foreach (Scheme scheme in Schemes)
            {
                rows.Add(Measure(graph, snapped, pool, concentrated: false, scheme));
            }
        }

        // R6.1b's structured-key case, where the shipped scheme lost two lookups in three while the
        // cache was not full. If the conflict column does not explain that, this instrument is wrong.
        OdPair[] structuredDraw = distribution.Draw(
            CounterHash.Seed, 512, Modes.Car, KeyReport.OdRungs[0], out _, out _);
        OdPair[] structured = KeyReport.Concentrate(graph, KeyReport.Snap(graph, structuredDraw, 5), 8);

        foreach (Scheme scheme in Schemes)
        {
            rows.Add(Measure(graph, structured, 512, concentrated: true, scheme));
        }

        report.AppendLine(
            "| Pool | Load | Scheme | Hit | Cold | Capacity | **Conflict** | Mean probes |");
        report.AppendLine("|---|---|---|---:|---:|---:|---:|---:|");

        foreach (Blamed row in rows)
        {
            string pool = row.Concentrated
                ? "512, 8 sites"
                : row.Pool.ToString("N0", CultureInfo.InvariantCulture);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {pool} | {Load(row.Pool)} | {Label(row.Scheme)} "
                + $"| {Permille(row.HitPermille)} | {Permille(row.ColdPermille)} "
                + $"| {Permille(row.CapacityPermille)} | **{Permille(row.ConflictPermille)}** "
                + $"| {row.MeanProbeHundredths / 100}.{row.MeanProbeHundredths % 100:D2} |"));
        }

        report.AppendLine();
        AppendProse(report, rows);

        return report.ToString();
    }

    private static Blamed Measure(
        RoadGraph graph, OdPair[] pool, int poolSize, bool concentrated, Scheme scheme)
    {
        int ways = scheme switch
        {
            Scheme.TwoWay => 2,
            Scheme.FourWay => 4,
            Scheme.EightWay => 8,
            Scheme.FullyAssociative => Capacity,
            _ => 1,
        };

        int sets = Capacity / ways;

        var key = new long[Capacity];
        var used = new long[Capacity];
        var occupied = new bool[Capacity];

        // The reference: a fully-associative LRU of the same capacity, run in lock step. It is what
        // separates a capacity miss from a conflict miss, and it is the whole instrument.
        var idealKey = new long[Capacity];
        var idealUsed = new long[Capacity];
        var idealOccupied = new bool[Capacity];

        var seen = new HashSet<long>();

        int hits = 0;
        int cold = 0;
        int capacity = 0;
        int conflict = 0;
        long probes = 0;

        for (int trip = 0; trip < Trips; trip++)
        {
            OdPair pair = pool[KeyReport.Draw(trip, pool.Length)];
            long value = KeyReport.KeyOf(graph, pair, KeyReport.RouteKey.NearestNode);

            bool everSeen = !seen.Add(value);
            bool idealHit = Touch(idealKey, idealUsed, idealOccupied, 0, Capacity, value, trip);

            int set = IndexOf(value, sets, scheme);
            int from = set * ways;
            bool hit = Touch(key, used, occupied, from, ways, value, trip);

            probes += ways;

            if (hit)
            {
                hits++;
                continue;
            }

            if (!everSeen)
            {
                cold++;
            }
            else if (!idealHit)
            {
                capacity++;
            }
            else
            {
                conflict++;
            }
        }

        return new Blamed(
            poolSize,
            concentrated,
            scheme,
            // 1_000L rather than 1_000: these are counts and cannot overflow at this scale, but
            // BOR0207 is deliberately strict for the reason BOR0203 records — a lint that is
            // sometimes right is one that gets suppressed at the site where it was right.
            (int)((hits * 1_000L) / Trips),
            (int)((cold * 1_000L) / Trips),
            (int)((capacity * 1_000L) / Trips),
            (int)((conflict * 1_000L) / Trips),
            (int)((probes * 100) / Trips));
    }

    /// <summary>
    /// Looks a key up in one set and records the access. Returns whether it was already resident;
    /// either way the key ends up resident, evicting the least recently used slot in the set.
    /// </summary>
    private static bool Touch(
        long[] key, long[] used, bool[] occupied, int from, int ways, long value, int now)
    {
        int free = -1;
        int oldest = from;

        for (int i = from; i < from + ways; i++)
        {
            if (occupied[i] && key[i] == value)
            {
                used[i] = now;
                return true;
            }

            if (!occupied[i] && free < 0)
            {
                free = i;
            }

            if (used[i] < used[oldest])
            {
                oldest = i;
            }
        }

        int slot = free >= 0 ? free : oldest;
        occupied[slot] = true;
        key[slot] = value;
        used[slot] = now;
        return false;
    }

    private static int IndexOf(long value, int sets, Scheme scheme)
    {
        ulong mixed = (ulong)value * 0x9E37_79B9_7F4A_7C15UL;

        if (scheme == Scheme.FullyAssociative)
        {
            return 0;
        }

        if (scheme == Scheme.DirectModulo)
        {
            // RouteCache's own indexing, reproduced exactly. One xor-shift, then a modulo — which
            // reads the LOW bits, and a multiply concentrates entropy in the HIGH ones.
            mixed ^= mixed >> 29;
            return (int)(mixed % (ulong)sets);
        }

        // CounterHash.Below is multiply-high: it consumes the TOP bits of the word, which is exactly
        // the half the multiply above concentrated the entropy into. Reused rather than re-derived —
        // its own comment already explains why the low bits are the wrong ones to read.
        return CounterHash.Below(mixed, sets);
    }

    private static void AppendProse(StringBuilder report, List<Blamed> rows)
    {
        Blamed At(int pool, Scheme scheme, bool concentrated = false) =>
            rows.First(r => r.Pool == pool && r.Scheme == scheme && r.Concentrated == concentrated);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**At R5.3's own rung — 512 pairs into 1,024 entries — the shipped scheme's misses are "
            + $"{Permille(At(512, Scheme.DirectModulo).ConflictPermille)} conflict and "
            + $"{Permille(At(512, Scheme.DirectModulo).CapacityPermille)} capacity.** Every one of "
            + $"them is a lookup a perfect cache **of the same size** would have served. **R5.3's "
            + $"28–31% floor is not a property of cache size and never was**, and reading it as one is "
            + $"what made it look like a fact of life rather than a bug with a fix."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Associativity is the lever, and four ways is where it stops paying.** At the same rung "
            + $"conflict falls {Permille(At(512, Scheme.DirectModulo).ConflictPermille)} → "
            + $"{Permille(At(512, Scheme.TwoWay).ConflictPermille)} → "
            + $"{Permille(At(512, Scheme.FourWay).ConflictPermille)} → "
            + $"{Permille(At(512, Scheme.EightWay).ConflictPermille)} across 1, 2, 4 and 8 ways, "
            + $"against a fully-associative bound of "
            + $"{Permille(At(512, Scheme.FullyAssociative).ConflictPermille)}. **Four ways recovers "
            + $"most of the gap at four probes**, and the probes are contiguous — on the cache line an "
            + $"entry already occupies, close to free. This is `adr/0017`'s fixed-capacity least-used "
            + $"pattern, sized, and it is the first number the corpus has for it."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The index function is not the lever, and this section predicted that it would be.** "
            + $"The hypothesis on file was that `RouteCache.Slot` multiplies by the golden-ratio "
            + $"constant — driving entropy upward — and then takes `% capacity`, which reads the low "
            + $"bits, so the modulo discards exactly what the multiply created. **Measured, that is "
            + $"wrong on random keys**: high-bit indexing reads "
            + $"{Permille(At(512, Scheme.DirectHighBits).ConflictPermille)} against modulo's "
            + $"{Permille(At(512, Scheme.DirectModulo).ConflictPermille)} at 0.50× load and "
            + $"{Permille(At(1_024, Scheme.DirectHighBits).ConflictPermille)} against "
            + $"{Permille(At(1_024, Scheme.DirectModulo).ConflictPermille)} at 1.00× — **level or "
            + $"slightly worse.** A route key is already a pair of well-spread node ids, so there is "
            + $"no structure in the low bits for the modulo to expose."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Where it does help is exactly where R6.1b found the damage: structured keys.** On the "
            + $"eight-destination pool, high-bit indexing takes conflict "
            + $"{Permille(At(512, Scheme.DirectModulo, true).ConflictPermille)} → "
            + $"{Permille(At(512, Scheme.DirectHighBits, true).ConflictPermille)}, and four ways takes "
            + $"it to {Permille(At(512, Scheme.FourWay, true).ConflictPermille)}. **So the index "
            + $"function is a robustness fix rather than a throughput one** — it costs nothing and it "
            + $"is what stops a concentrated city falling off a cliff the uniform draw never shows. "
            + $"**Both changes are worth making and only one of them shows up in the average case**, "
            + $"which is the argument for measuring the concentrated rung at all."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**One honest limit on that row.** R6.1b's worst case — 15.9% hit — was the "
            + $"`access-point` key, and this table keys on `nearest-node` throughout, where the same "
            + $"pool reads {Permille(At(512, Scheme.DirectModulo, true).HitPermille)}. The conflict "
            + $"column is clearly elevated against the unconcentrated rung "
            + $"({Permille(At(512, Scheme.DirectModulo, true).ConflictPermille)} against "
            + $"{Permille(At(512, Scheme.DirectModulo).ConflictPermille)} at identical load), so the "
            + $"mechanism is confirmed. **The magnitude is not**, and this table does not reproduce "
            + $"R6.1b's extreme."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Load is the axis R5.3 never swept, and it dominates everything above.** Conflict at "
            + $"four ways runs {Permille(At(256, Scheme.FourWay).ConflictPermille)} → "
            + $"{Permille(At(512, Scheme.FourWay).ConflictPermille)} → "
            + $"{Permille(At(1_024, Scheme.FourWay).ConflictPermille)} across 0.25×, 0.50× and 1.00×, "
            + $"and **capacity misses appear only at 2.00×**, where they reach "
            + $"{Permille(At(2_048, Scheme.DirectModulo).CapacityPermille)}. R5.3 measured one load "
            + $"and called the result a floor; **it is a point on a curve that triples.**"));
        report.AppendLine();

        report.AppendLine(
            "**What this section does not depend on.** Every figure is a *conditional* claim — given a "
            + "lookup that repetition would have made a hit, whose fault is it that it was not? — so "
            + "it survives the cache's absolute hit rate being unmeasurable until Trip generation "
            + "exists. **The cold column is the part hostage to the invented pool** and it is reported "
            + "separately rather than folded in, so a reader can see which half of the table moves "
            + "with the invention. The conflict column does not.");
        report.AppendLine();
    }

    private static string Load(int pool) => string.Create(
        CultureInfo.InvariantCulture, $"{(pool * 100) / Capacity / 100}.{(pool * 100) / Capacity % 100:D2}×");

    private static string Permille(int value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value / 10}.{value % 10}%");

    private static string Label(Scheme scheme) => scheme switch
    {
        Scheme.DirectModulo => "`direct`, modulo — **shipped**",
        Scheme.DirectHighBits => "`direct`, high bits",
        Scheme.TwoWay => "2-way LRU",
        Scheme.FourWay => "4-way LRU",
        Scheme.EightWay => "8-way LRU",
        _ => "fully associative — *bound*",
    };
}
