using Borough.Appearance;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Formats;

using Xunit.Abstractions;

namespace Borough.Tests.Appearance;

/// <summary>
/// Counts the families the test-street preset picks for the base package's founding city, by kind,
/// family and Lot shape (<c>plans/building-families.md</c>). Box fallbacks print as their choice.
/// </summary>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class FoundingPeriodProbe(ITestOutputHelper output)
{
    private const int Days = 45;

    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Founding_city_family_picks()
    {
        string package = Path.Combine(AppContext.BaseDirectory, "Rulesets", "base");
        RulesetSourceResult source = RulesetSource.Load(Path.Combine(package, "ruleset.toml"));
        Assert.True(source.Ok, source.Diagnostics.Count > 0 ? source.Diagnostics[0].ToString() : "refused");
        StylePresetResult read = StylePresetReader.Read(Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street"));
        Assert.NotNull(read.Preset);

        InputLog log = InputLogCodec.FromText(File.ReadAllText(Path.Combine(package, "founding.borough")));
        Simulation simulation = Replay.Start(log, source.Ruleset!);
        World world = simulation.World;
        for (int tick = 0; tick < Days * Ticks.PerDay; tick++)
        {
            Ticks now = simulation.Tick;
            simulation.Step(new TickInput(log.At(now), log.RulesetHashAt(now)));
            simulation.Employment.Drain();
        }

        var picks = new SortedDictionary<string, int>(StringComparer.Ordinal);
        int total = 0;
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!BuildingFacts.TryOf(world, source.Names, slot, out BuildingFacts facts)) continue;
            FamilyPick pick = FamilyPicker.Pick(read.Preset, simulation.Key, facts);
            string family = pick.Choice == FamilyChoice.Eligible ? pick.Family!.Id : $"({pick.Choice})";
            string row = $"{facts.Kind}\t{family}\t{facts.Pattern}\t{facts.Storeys}\t{facts.FrontageMetres}x{facts.DepthMetres}";
            picks[row] = picks.GetValueOrDefault(row) + 1;
            total++;
        }

        _output.WriteLine($"day {Days}, {total} Buildings");
        _output.WriteLine("count\tkind\tfamily\tpattern\tstoreys\tfrontage x depth m");
        foreach ((string row, int count) in picks) _output.WriteLine($"{count}\t{row}");
    }
}
