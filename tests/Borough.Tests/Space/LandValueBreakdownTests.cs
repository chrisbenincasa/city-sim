using System.Diagnostics;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Xunit;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>Where the land value pass actually spends its time.</summary>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class LandValueBreakdownTests(ITestOutputHelper output)
{
    private static Ruleset Load(string file)
    {
        RulesetLoadResult r = RulesetLoader.Load(
            System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file));
        return r.Ruleset ?? throw new InvalidOperationException(r.Describe());
    }

    [Theory]
    [InlineData("bordered.toml", 4_000, 0)]
    [InlineData("bordered.toml", 4_000, 5)]
    public void Where_the_time_goes(string file, int citizens, int vehicles)
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(citizens, Load(file), key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        MapLayers layers = world.Layers;
        RoadGraph graph = world.Roads;
        LayerCellTable cells = layers.Cells;
        DesirabilityWeights weights = world.Rules.Layers.Desirability;

        // This field-only fixture puts Vehicles on evenly spaced Streets without stepping Trips.
        int remaining = vehicles;
        int stride = vehicles > 0 ? graph.Segments.Rows.LiveCount / vehicles : 1;
        for (int slot = 0; slot < graph.Segments.Rows.SlotCount && remaining > 0; slot += stride)
        {
            if (graph.Segments.Rows.IsLive(slot)
                && (RoadKind)graph.Segments.Kind[slot] == RoadKind.Street)
            {
                graph.Segments.VolumeForward[slot] = 1;
                remaining--;
            }
        }
        Assert.Equal(0, remaining);

        output.WriteLine($"PROBE {file}: {cells.Rows.LiveCount} Cell rows, "
            + $"{graph.Segments.Rows.LiveCount} Segments; fixture Vehicles {vehicles}; field threads 1");

        TrafficPresence presence = new();
        var watch = Stopwatch.StartNew();
        presence.Rebuild(graph, weights.NoiseSource.Range);
        watch.Stop();
        Assert.Equal(vehicles, presence.MovingSegments);
        output.WriteLine($"PROBE TrafficPresence.Rebuild        {watch.ElapsedMilliseconds,7} ms "
            + $"(moving segments {presence.MovingSegments}, any {presence.AnyTraffic})");

        watch = Stopwatch.StartNew();
        layers.SetLandValueTargets(graph);
        watch.Stop();
        output.WriteLine($"PROBE SetLandValueTargets (WITH summary) {watch.ElapsedMilliseconds,7} ms");

        // Compare the same field walk without the pass-wide traffic summary.
        watch = Stopwatch.StartNew();
        long sink = 0;
        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot)) { continue; }
            sink += layers.CellDesirability(graph, weights, cells.East[slot], cells.North[slot]);
        }
        watch.Stop();
        output.WriteLine($"PROBE same walk, NO summary              {watch.ElapsedMilliseconds,7} ms "
            + $"(sink {sink})");

        // The walk with everything but the query: what the loop and the Cell reads alone cost.
        watch = Stopwatch.StartNew();
        long reads = 0;
        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot)) { continue; }
            reads += layers.Pollution(cells.East[slot], cells.North[slot]);
        }
        watch.Stop();
        output.WriteLine($"PROBE loop + Pollution read only     {watch.ElapsedMilliseconds,7} ms "
            + $"(sink {reads})");

        watch = Stopwatch.StartNew();
        layers.DriftLandValue();
        watch.Stop();
        output.WriteLine($"PROBE DriftLandValue                 {watch.ElapsedMilliseconds,7} ms");
    }
}
