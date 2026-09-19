using System.Globalization;
using Borough.Formats;

namespace Borough.Headless;

/// <summary>
/// Prints what replacing one Ruleset with another would do, before anything runs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Effective values, so a shared definition's edit is visible where it lands.</b> A
/// <c>[[basket]]</c>, <c>[[recipe]]</c> or <c>[[reserve]]</c> expands inside the reader, so the two
/// files can say the same thing about a Rule and still give it different terms. Both candidates are
/// resolved and their Rulesets compared.
/// </para>
/// <para>
/// ⚠ <b>No world, so this is a static comparison and not a migration verdict.</b> It says nothing
/// about a running city's stock, occupancy or solvency, and the report says so itself.
/// </para>
/// </remarks>
internal static class PreviewDump
{
    public static int Print(Options options)
    {
        RulesetSourceResult before = RulesetSource.Load(options.RulesetPaths[0]);
        RulesetSourceResult after = RulesetSource.Load(options.AgainstPath!);

        foreach ((RulesetSourceResult candidate, string path) in
            new[] { (before, options.RulesetPaths[0]), (after, options.AgainstPath!) })
        {
            if (!candidate.Ok)
            {
                Console.Error.WriteLine($"{path} was refused:");
                Console.Error.WriteLine(candidate.Describe());

                return 1;
            }
        }

        RulesetImpact impact = RulesetImpact.Between(before, after);

        if (!impact.Ok)
        {
            Console.Error.WriteLine(impact.Describe());

            return 1;
        }

        Console.Out.Write(Render(impact, options.RulesetPaths[0], options.AgainstPath!));
        Console.Out.Flush();

        return 0;
    }

    internal static string Render(RulesetImpact impact, string before, string after)
    {
        var page = new System.Text.StringBuilder();

        page.Append(CultureInfo.InvariantCulture, $"{before}\n  {impact.BeforeIdentity:x16}\n");
        page.Append(CultureInfo.InvariantCulture, $"{after}\n  {impact.AfterIdentity:x16}\n\n");

        if (!impact.Moves)
        {
            page.Append("Nothing moves. The two candidates resolve to the same Ruleset.\n");

            return page.ToString();
        }

        Counts(page, impact);
        Declarations(page, impact);
        Dependants(page, impact);
        World(page, impact);

        page.Append(
            "\nThis is a static comparison of two Rulesets. It says nothing about a running "
            + "city's\nstock, occupancy or solvency, and no migration was attempted.\n");

        return page.ToString();
    }

    /// <summary>
    /// What each section declares. A single file has no package declarations, so it has no counts
    /// and no dependant list either; its changed values still carry the whole answer.
    /// </summary>
    private static void Counts(System.Text.StringBuilder page, RulesetImpact impact)
    {
        if (impact.Counts.Count == 0)
        {
            page.Append(
                "A single-file Ruleset declares no package members, so neither authored counts\n"
                + "nor the dependants of a shared definition are listed below.\n");

            return;
        }

        page.Append("declared\n");

        foreach (RulesetImpactCount count in impact.Counts)
        {
            string moved = count.Before == count.After
                ? count.After.ToString(CultureInfo.InvariantCulture)
                : $"{count.Before} -> {count.After}";

            page.Append(CultureInfo.InvariantCulture, $"  {count.Section,-16} {moved}\n");
        }
    }

    private static void Declarations(System.Text.StringBuilder page, RulesetImpact impact)
    {
        int unchanged = 0;

        page.Append("\nchanged\n");

        foreach (RulesetImpactDeclaration declaration in impact.Declarations)
        {
            if (declaration.Verdict == RulesetImpactVerdict.Unchanged)
            {
                unchanged++;
                continue;
            }

            page.Append(CultureInfo.InvariantCulture,
                $"  [[{declaration.Section}]] {declaration.Id}  {Spelled(declaration)}\n");

            foreach (RulesetImpactValue value in declaration.Values)
            {
                page.Append(CultureInfo.InvariantCulture,
                    $"    {value.Path,-40} {value.Before ?? "-"} -> {value.After ?? "-"}\n");
            }
        }

        page.Append(CultureInfo.InvariantCulture, $"  ({unchanged} declaration(s) unchanged)\n");
    }

    private static string Spelled(RulesetImpactDeclaration declaration) =>
        declaration.Verdict == RulesetImpactVerdict.Relabelled
            ? $"relabelled '{declaration.WasLabelled}' -> '{declaration.Label}'"
            : declaration.Verdict.ToString().ToLowerInvariant();

    /// <summary>
    /// What each shared definition reaches, including the dependants the edit left alone, because
    /// an untouched dependant is the half of the answer a run cannot show.
    /// </summary>
    private static void Dependants(System.Text.StringBuilder page, RulesetImpact impact)
    {
        if (impact.Dependants.Count == 0)
        {
            return;
        }

        page.Append("\nshared\n");

        string held = "";

        foreach (RulesetImpactDependant dependant in impact.Dependants)
        {
            string definition = $"[[{dependant.Section}]] {dependant.Id}";

            if (definition != held)
            {
                page.Append(CultureInfo.InvariantCulture, $"  {definition}\n");
                held = definition;
            }

            string by = $"{dependant.BySection}/{dependant.ById}";

            page.Append(CultureInfo.InvariantCulture,
                $"    {by,-32} via {dependant.Key,-24} "
                + $"{dependant.Verdict.ToString().ToLowerInvariant()}\n");
        }
    }

    private static void World(System.Text.StringBuilder page, RulesetImpact impact)
    {
        if (impact.World.Count == 0)
        {
            return;
        }

        page.Append("\nelsewhere\n");

        foreach (RulesetImpactValue value in impact.World)
        {
            page.Append(CultureInfo.InvariantCulture,
                $"  {value.Path,-52} {value.Before ?? "-"} -> {value.After ?? "-"}\n");
        }
    }
}
