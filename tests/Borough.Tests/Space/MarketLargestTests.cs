using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// A market row's cached largest seller stock always equals the stock read seller by seller.
/// </summary>
public sealed class MarketLargestTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0);

    [Fact]
    public void The_cached_largest_stock_matches_a_walk_of_the_sellers_every_tick()
    {
        var world = new World(2_000, Load("pictured.toml"), Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int changes = 0;
        long[] previous = [];

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(default);

            int rows = world.DistrictPools.Rows.SlotCount;

            if (previous.Length < rows)
            {
                Array.Resize(ref previous, rows);
            }

            for (int row = 0; row < rows; row++)
            {
                if (!world.DistrictPools.Rows.IsLive(row))
                {
                    continue;
                }

                long largest = world.Markets.Largest(world, row);

                Assert.True(
                    largest == world.Markets.Stock(world, row).Largest,
                    $"market row {row} at Tick {world.Tick.Raw} caches a largest stock of {largest}, "
                    + $"and its sellers hold at most {world.Markets.Stock(world, row).Largest}.");

                if (largest != previous[row])
                {
                    changes++;
                    previous[row] = largest;
                }
            }
        }

        Assert.True(changes > 0, "no market row's largest stock ever moved, so nothing was compared.");
    }

    private static Ruleset Load(string file)
    {
        string body = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));
        RulesetLoadResult result = RulesetLoader.Parse(body, file);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"{file} was refused, so this test cannot run:\n{result.Describe()}");
    }
}
