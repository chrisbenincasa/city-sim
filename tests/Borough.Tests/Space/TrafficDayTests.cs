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

[Trait(Tier.Key, Tier.Instrument)]
public sealed class TrafficDayTests(ITestOutputHelper output)
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
    [InlineData("congested.toml", 16_000, 2_100)]
    [InlineData("minimal.toml", 4_000, 2_100)]
    public void When_does_anybody_drive(string file, int citizens, int ticks)
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(citizens, Load(file), key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        Simulation sim = new(world, key) { VerifyDecideWritesNothing = false };
        RoadSegmentTable segments = world.Roads.Segments;

        long peak = 0;
        int peakTick = -1;
        int firstTick = -1;
        var hourly = new long[24];
        var hourlySamples = new long[24];

        for (int tick = 0; tick < ticks; tick++)
        {
            sim.Step(default);

            long total = 0;

            for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
            {
                if (!segments.Rows.IsLive(slot)) continue;
                long v = (long)segments.VolumeForward[slot] + segments.VolumeBackward[slot];
                if (v > 0) { total += v; }
            }

            int hour = (int)((long)tick * 24 / Ticks.PerDay) % 24;
            hourly[hour] += total;
            hourlySamples[hour]++;

            if (total > 0 && firstTick < 0) { firstTick = tick; }
            if (total > peak) { peak = total; peakTick = tick; }
        }

        output.WriteLine($"PROBE === {file}, {citizens} Citizens, {ticks} Ticks ===");
        output.WriteLine($"PROBE Ticks.PerDay = {Ticks.PerDay}, one Tick = "
            + $"{86_400.0 / Ticks.PerDay:F1} s, so tick 400 = {Clock(400)}");
        output.WriteLine($"PROBE first Vehicle on a Segment: tick {firstTick} "
            + $"({(firstTick < 0 ? "NEVER" : Clock(firstTick))})");
        output.WriteLine($"PROBE peak Vehicles in motion:   {peak} at tick {peakTick} "
            + $"({(peakTick < 0 ? "-" : Clock(peakTick))})");
        output.WriteLine("PROBE hour  mean Vehicles in motion");

        for (int h = 0; h < 24; h++)
        {
            if (hourlySamples[h] == 0) { continue; }

            double mean = (double)hourly[h] / hourlySamples[h];
            string bar = new('#', (int)Math.Min(60, mean * 2));
            output.WriteLine($"PROBE {h:D2}:00 {mean,8:F2}  {bar}");
        }
    }
}
