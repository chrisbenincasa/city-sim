using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Review;

// Review probe B: DistrictMarkets and StandingHousing are lazy caches kept valid by hand-placed
// Invalidate calls (and, for Largest, by incremental Moved updates). A load starts them stale, so
// any missed invalidation is a save/reload divergence that neither the State Hash nor the derived
// rebuild audit can see. Step a world, read the live cache, force a rebuild, read again, compare.
public sealed class LazyCacheProbe(ITestOutputHelper output)
{
    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (string path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Rulesets"), "*.toml").Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileName(path);
            string? only = Environment.GetEnvironmentVariable("PROBE_FILES");
            if (only is null || only.Split(',').Contains(name)) { data.Add(name); }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Live_caches_match_a_fresh_rebuild(string file)
    {
        RulesetLoadResult loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));
        if (loaded.Ruleset is null) { output.WriteLine($"{file}: refused"); return; }

        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, loaded.Ruleset, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        List<string> mismatches = [];
        for (int tick = 1; tick <= 2100; tick++)
        {
            simulation.Step(default);
            if (tick % 50 != 0) { continue; }

            string before = Snapshot(world);
            world.Markets.Invalidate();
            world.HousingBuildings.Invalidate();
            string after = Snapshot(world);
            if (before != after)
            {
                mismatches.Add($"tick {tick}: {Diff(before, after)}");
                if (mismatches.Count > 3) { break; }
            }
        }

        Assert.True(mismatches.Count == 0, $"{file}:\n{string.Join("\n", mismatches)}");
    }

    private static string Diff(string a, string b)
    {
        string[] x = a.Split('\n');
        string[] y = b.Split('\n');
        for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
        {
            if (x[i] != y[i]) { return $"live [{x[i]}] rebuilt [{y[i]}]"; }
        }
        return $"lengths {x.Length} vs {y.Length}";
    }

    private static string Snapshot(World world)
    {
        var text = new System.Text.StringBuilder();
        int housing = world.HousingBuildings.Count(world);
        text.Append("housing ").Append(housing).Append('\n');
        for (int i = 0; i < housing; i++) { text.Append(world.HousingBuildings.Nth(world, i)).Append(' '); }
        text.Append('\n');

        for (int row = 0; row < world.DistrictPools.Rows.SlotCount; row++)
        {
            int sellers = world.Markets.SellerCount(world, row);
            text.Append($"row {row} sellers {sellers} largest {world.Markets.Largest(world, row)}:");
            for (int s = 0; s < sellers; s++)
            {
                var offer = world.Markets.Seller(world, row, s);
                text.Append($" {offer.Bin}/{offer.Business}");
            }
            text.Append('\n');
        }

        for (int bin = 0; bin < world.Bins.Rows.SlotCount; bin++)
        {
            int market = world.Markets.MarketOf(world, bin);
            int pool = world.Markets.PoolRowOf(world, bin);
            if (market >= 0 || pool >= 0) { text.Append($"bin {bin} market {market} pool {pool}\n"); }
        }

        return text.ToString();
    }
}
