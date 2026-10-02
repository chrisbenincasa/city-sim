using System.Globalization;
using Borough.Appearance;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Headless;

/// <summary>
/// Which Appearance Family each standing Building draws under a Style Preset, counted by kind, and
/// how often each kind falls back. It is the coverage report of the procedural-buildings session (Q5).
/// </summary>
/// <remarks>
/// It checks the preset against the Ruleset first and refuses a preset with errors. The warnings it
/// prints are the kinds that lack a fallback and the kinds a family names that this Ruleset lacks.
/// </remarks>
internal static class AppearanceDump
{
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        StylePresetResult read = StylePresetReader.Read(options.AppearancePath!);
        if (read.Preset is not { } preset)
        {
            foreach (AppearanceDiagnostic error in read.Errors) Console.Error.WriteLine(error);
            return 2;
        }

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out RulesetNames names))
        {
            return 2;
        }

        string[] kinds = [.. Enumerable.Range(1, rules.KindCount).Select(k => names.Kind((byte)k) ?? $"#{k}")];

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        output.WriteLine("# Borough Appearance Family coverage");
        output.WriteLine($"# preset {preset.Name}, {preset.Families.Length} families; {world.Buildings.Rows.LiveCount} Buildings after {options.Ticks} Ticks.");

        output.WriteLine();
        output.WriteLine("## Warnings for this Ruleset");
        IReadOnlyList<AppearanceDiagnostic> warnings = StylePresetReader.CheckAgainst(preset, kinds);
        if (warnings.Count == 0) output.WriteLine("  none");
        foreach (AppearanceDiagnostic warning in warnings) output.WriteLine($"  {warning}");

        var picks = new SortedDictionary<(string Kind, string Family), int>();
        var fallbacks = new SortedDictionary<string, (int Buildings, int Fallback, int Missing)>(StringComparer.Ordinal);
        var unclaimed = new SortedDictionary<(BlockPattern Pattern, int Storeys), (int Buildings, int Frontage0, int Frontage1, int Depth0, int Depth1)>();
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!BuildingFacts.TryOf(world, names, slot, out BuildingFacts facts)) continue;
            FamilyPick pick = FamilyPicker.Pick(preset, key, facts);
            string family = pick.Family?.Id ?? "(missing)";
            picks[(facts.Kind, family)] = picks.GetValueOrDefault((facts.Kind, family)) + 1;
            (int buildings, int fallback, int missing) = fallbacks.GetValueOrDefault(facts.Kind);
            fallbacks[facts.Kind] = (buildings + 1,
                fallback + (pick.Choice == FamilyChoice.Fallback ? 1 : 0),
                missing + (pick.Choice == FamilyChoice.Missing ? 1 : 0));
            if (pick.Choice == FamilyChoice.Eligible) continue;
            (BlockPattern, int) shape = (facts.Pattern, facts.Storeys);
            unclaimed[shape] = unclaimed.TryGetValue(shape, out var seen)
                ? (seen.Buildings + 1, Math.Min(seen.Frontage0, facts.FrontageMetres), Math.Max(seen.Frontage1, facts.FrontageMetres),
                    Math.Min(seen.Depth0, facts.DepthMetres), Math.Max(seen.Depth1, facts.DepthMetres))
                : (1, facts.FrontageMetres, facts.FrontageMetres, facts.DepthMetres, facts.DepthMetres);
        }

        output.WriteLine();
        output.WriteLine("## Buildings by kind and family");
        output.WriteLine("  kind                  family                    buildings");
        foreach (((string kind, string family), int count) in picks)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {kind,-20}  {family,-24}  {count,9}"));
        }

        output.WriteLine();
        output.WriteLine("## Fallback use by kind");
        output.WriteLine("  kind                  buildings  fallback  missing");
        foreach ((string kind, (int buildings, int fallback, int missing)) in fallbacks)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  {kind,-20}  {buildings,9}  {fallback,8}  {missing,7}"));
        }

        output.WriteLine();
        output.WriteLine("## Fallback and missing use by block pattern and storeys");
        output.WriteLine("  pattern               storeys  buildings  frontage m  depth m");
        foreach (((BlockPattern pattern, int storeys), var shape) in unclaimed)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  {pattern,-20}  {storeys,7}  {shape.Buildings,9}  {$"{shape.Frontage0}-{shape.Frontage1}",10}  {$"{shape.Depth0}-{shape.Depth1}",7}"));
        }

        return 0;
    }
}
