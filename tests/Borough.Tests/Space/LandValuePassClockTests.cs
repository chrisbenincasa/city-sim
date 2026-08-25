using System.Diagnostics;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Xunit;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// What the land value target pass costs <b>at each hour it fires</b>.
/// </summary>
/// <remarks>
/// The 6,578 ms in <c>plans/0013</c> was taken at <b>Tick 16, which is 00:11</b> — the hour of the
/// day with the least traffic in the city. <c>LineSourceQueries.Contribution</c> returns zero
/// without measuring a distance when a Segment carries no Vehicles, so a midnight reading is the
/// <b>cheapest</b> the pass can be, not a typical one.
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class LandValuePassClockTests(ITestOutputHelper output)
{
    private static Ruleset Load(string file)
    {
        RulesetLoadResult r = RulesetLoader.Load(
            System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file));
        return r.Ruleset ?? throw new InvalidOperationException(r.Describe());
    }

    private static string Clock(int tick)
    {
        long seconds = (long)tick * 86_400 / Ticks.PerDay;
        return $"{seconds / 3600 % 24:D2}:{seconds % 3600 / 60:D2}";
    }

    [Theory]
    [InlineData("bordered.toml", 4_000)]
    public void What_the_pass_costs_by_hour(string file, int citizens)
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(citizens, Load(file), key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        Simulation sim = new(world, key) { VerifyDecideWritesNothing = false };
        RoadSegmentTable segments = world.Roads.Segments;

        output.WriteLine($"PROBE === {file}, {citizens} Citizens, "
            + $"{world.Layers.Cells.Rows.LiveCount} Cell rows, "
            + $"{segments.Rows.LiveCount} Segments ===");
        output.WriteLine("PROBE  tick   clock   vehicles-in-motion   pass ms");

        for (int tick = 0; tick < 2_100; tick++)
        {
            bool due = tick % 256 == 16;

            long moving = 0;

            if (due)
            {
                for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
                {
                    if (!segments.Rows.IsLive(slot)) { continue; }
                    moving += (long)segments.VolumeForward[slot] + segments.VolumeBackward[slot];
                }
            }

            var watch = Stopwatch.StartNew();
            sim.Step(default);
            watch.Stop();

            if (due)
            {
                output.WriteLine($"PROBE {tick,6}   {Clock(tick)}   {moving,18}   "
                    + $"{watch.ElapsedMilliseconds,7}");
            }
        }
    }
}
