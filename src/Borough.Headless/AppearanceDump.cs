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

        string[] kinds = [.. Enumerable.Range(1, rules.KindCount).Select(k => names.KindId((byte)k) ?? $"#{k}")];

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
        var unclaimed = new SortedDictionary<(string Kind, BlockPattern Pattern, int Storeys, int Frontage, int Depth), int>();
        var shares = new SortedDictionary<RunShare, int>();
        var street = new List<(ulong Face, StreetSide Side, int Offset, string Family)>();
        var bands = new SortedDictionary<(int Frontage, string Family), int>();
        var terraces = new List<(ulong Face, StreetSide Side, int Offset, string Family, ulong Cell, int Pitch)>();
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
            if (pick.Choice == FamilyChoice.Eligible && pick.Family?.Body?.House is not null && facts.Face != 0)
            {
                RunShare share = FamilyPicker.RunOf(preset, key, facts).Share;
                shares[share] = shares.GetValueOrDefault(share) + 1;
                street.Add((facts.Face, facts.Side, facts.Offset, family));
            }

            if (pick.Choice == FamilyChoice.Eligible && pick.Family is { Body.Terrace: not null } terraced
                && FamilyPicker.TerraceOf(preset, key, facts) is { } terrace)
            {
                bands[(facts.FrontageMetres, family)] = bands.GetValueOrDefault((facts.FrontageMetres, family)) + 1;
                terraces.Add((facts.Face, facts.Side, facts.Offset, family, terrace.Cell, FamilyPicker.TerracePitch(terraced, key, terrace)));
            }

            if (pick.Choice == FamilyChoice.Eligible) continue;
            var shape = (facts.Kind, facts.Pattern, facts.Storeys, facts.FrontageMetres, facts.DepthMetres);
            unclaimed[shape] = unclaimed.GetValueOrDefault(shape) + 1;
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
        output.WriteLine("## Fallback and missing use by kind and lot shape");
        output.WriteLine("  kind                  pattern               storeys  frontage m  depth m  buildings");
        foreach (((string kind, BlockPattern pattern, int storeys, int frontage, int depth), int count) in unclaimed)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  {kind,-20}  {pattern,-20}  {storeys,7}  {frontage,10}  {depth,7}  {count,9}"));
        }

        output.WriteLine();
        output.WriteLine("## House bodies by run");
        output.WriteLine("  run        buildings");
        foreach ((RunShare share, int count) in shares)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {share,-9}  {count,9}"));
        }

        street.Sort();
        int pairs = 0, same = 0;
        for (int i = 1; i < street.Count; i++)
        {
            if (street[i].Face != street[i - 1].Face || street[i].Side != street[i - 1].Side) continue;
            pairs++;
            if (street[i].Family == street[i - 1].Family) same++;
        }

        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"  {same} of {pairs} neighboring house pairs on one face share a family."));

        output.WriteLine();
        output.WriteLine("## Attached houses by frontage and form");
        output.WriteLine("  frontage m  family                    buildings");
        foreach (((int frontage, string family), int count) in bands)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {frontage,10}  {family,-24}  {count,9}"));
        }

        // A form is a family here, and the bar lets neighbors share one only inside a terrace.
        terraces.Sort();
        int neighbors = 0, inside = 0, alike = 0;
        for (int i = 1; i < terraces.Count; i++)
        {
            var (a, b) = (terraces[i - 1], terraces[i]);
            if (a.Face != b.Face || a.Side != b.Side) continue;
            neighbors++;
            if (a.Cell == b.Cell) inside++;
            else if (a.Family == b.Family && Math.Abs(a.Pitch - b.Pitch) <= 5) alike++;
        }

        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"  {neighbors} neighboring attached pairs on one face: {inside} inside a terrace, {alike} across terraces with one form and a pitch within 5 degrees."));
        return 0;
    }
}
