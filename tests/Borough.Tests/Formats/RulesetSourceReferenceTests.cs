using Borough.Core.Rules;
using Borough.Formats;
using static Borough.Tests.Formats.SourcePackage;

namespace Borough.Tests.Formats;

/// <summary>
/// Plan 0077's typed resolver: a package's cross-member references resolved in Formats, against the
/// ids it collected, before the lowered text reaches the single-file reader.
/// </summary>
public sealed class RulesetSourceReferenceTests
{
    private const string Hinterland = """
        [[hinterland]]
        id = "outside"

        [[hinterland.population]]
        stage = "elder"
        """;

    [Fact]
    public void A_bin_naming_an_undeclared_good_is_located_at_the_key()
    {
        RulesetDiagnostic refusal = Refused(
            RulesetDiagnosticCode.Reference,
            ("dwelling.toml", Dwelling.Replace("\"repairs\"", "\"repair\"", StringComparison.Ordinal)),
            ("goods.toml", Goods),
            ("rules.toml", Consumption));

        Assert.Equal("dwelling.toml", refusal.Path);
        Assert.Equal(8, refusal.Line);
        Assert.Equal(5, refusal.Column);
        Assert.Equal("building", refusal.Section);
        Assert.Equal("dwelling", refusal.Id);
        Assert.Contains("resource names 'repair'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("no [[resource]]", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reference_resolves_only_in_its_own_namespace()
    {
        const string Rule = """
            [[rule]]
            id      = "consume"
            kind    = "sundries"
            rate    = 64
            apply   = { min = 1, max = 1 }
            inputs  = [ { scope = "local", resource = "sundries", amount = 1 } ]
            outputs = []
            """;

        RulesetDiagnostic refusal = Refused(
            RulesetDiagnosticCode.Reference,
            ("dwelling.toml", Dwelling),
            ("goods.toml", Goods),
            ("rules.toml", Rule));

        Assert.Equal("rules.toml", refusal.Path);
        Assert.Equal("rule", refusal.Section);
        Assert.Equal("consume", refusal.Id);
        Assert.Contains("kind names 'sundries'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("no [[building]]", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_nested_declaration_refers_through_its_owner()
    {
        RulesetDiagnostic refusal = Refused(
            RulesetDiagnosticCode.Reference,
            ("dwelling.toml", Dwelling),
            ("goods.toml", Goods),
            ("outside.toml", Hinterland),
            ("rules.toml", Consumption));

        Assert.Equal("outside.toml", refusal.Path);
        Assert.Equal(5, refusal.Line);
        Assert.Equal("hinterland", refusal.Section);
        Assert.Equal("outside", refusal.Id);
        Assert.Contains("stage names 'elder'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("no [[life_stage]]", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_nested_declaration_reaches_a_target_declared_in_another_member()
    {
        RulesetSourceResult result = Resolve(
            ("dwelling.toml", Dwelling),
            ("goods.toml", Goods),
            ("outside.toml", Hinterland),
            ("rules.toml", Consumption),
            ("stages.toml", "[[life_stage]]\nid = \"elder\""));

        Assert.DoesNotContain(
            result.Diagnostics, d => d.Code == RulesetDiagnosticCode.Reference);
    }

    /// <summary>
    /// A package that did not collect cleanly reports its collection diagnostics alone.
    /// </summary>
    /// <remarks>
    /// Deleting the dwelling's id leaves both Rules naming a kind nothing declares. Reporting those
    /// as unresolved references would charge one mistake three times, and the two Rules are correct.
    /// </remarks>
    [Fact]
    public void Collection_diagnostics_come_without_the_references_they_would_break()
    {
        RulesetSourceResult result = Resolve(
            ("dwelling.toml", Dwelling.Replace("id = \"dwelling\"", "", StringComparison.Ordinal)),
            ("goods.toml", Goods),
            ("rules.toml", Consumption));

        Assert.Contains(result.Diagnostics, d => d.Code == RulesetDiagnosticCode.Id);
        Assert.DoesNotContain(
            result.Diagnostics, d => d.Code == RulesetDiagnosticCode.Reference);
    }

    [Fact]
    public void The_hosts_refusal_names_the_section_and_the_column()
    {
        RulesetSourceResult result = Resolve(
            ("dwelling.toml", Dwelling.Replace("\"repairs\"", "\"repair\"", StringComparison.Ordinal)),
            ("goods.toml", Goods),
            ("rules.toml", Consumption));

        RulesetRefusal refusal = Assert.Single(result.ToLoadResult().Refusals);

        Assert.StartsWith(
            "dwelling.toml:8:5: [[building]] 'dwelling': ",
            refusal.ToString(),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A shared definition is a member like any other: a Rule and a Bin in one file resolve into a
    /// basket and a reserve declared in another, whichever order the members sort in.
    /// </summary>
    private const string SharedDefinitions = """
        [[basket]]
        id = "basic"
        label = "Weekly basics"
        owner = "occupant"
        use_per_day = { sundries = 250 }

        [[reserve]]
        id = "standard"
        days = 3

        [[recipe]]
        id = "restock"
        inputs = []
        outputs = [ { scope = "local", resource = "repairs", amount = 1 } ]
        """;

    private const string SizedDwelling = """
        [[building]]
        id = "dwelling"
        houses = true
        premises = true
        bins = [
          { resource = "sundries", reserve = { profile = "standard", basket = "basic" }, owner = "occupant" },
          { resource = "repairs",  capacity = 4 },
        ]

        [[rule]]
        id     = "consume"
        kind   = "dwelling"
        rate   = 32
        apply  = { min = 1, max = 1 }
        basket = "basic"

        [[rule]]
        id     = "restock"
        kind   = "dwelling"
        rate   = 64
        apply  = { min = 1, max = 2 }
        recipe = "restock"
        """;

    [Fact]
    public void A_basket_and_a_reserve_resolve_across_members()
    {
        RulesetSourceResult result = Accepted(
            ("dwelling.toml", SizedDwelling),
            ("goods.toml", Goods),
            ("shared.toml", SharedDefinitions));

        Assert.Equal(750, result.Ruleset!.BinsOf(1)[0].Capacity.Units);
        Assert.True(result.Ruleset.Inputs(new RuleId(1))[0].PerDay);

        Term made = Assert.Single(result.Ruleset.Outputs(new RuleId(2)).ToArray());

        Assert.Equal("repairs", result.Names.Resource(made.Bin.Resource));
        Assert.Equal(1, made.Amount);
        Assert.False(made.PerDay);

        // The label reaches the shell's names, and the id never does.
        Assert.Contains(
            result.Declarations, d => d.Section == "basket" && d.Id == "basic");
    }

    [Theory]
    [InlineData("\nbasket = \"basic\"", "\nbasket = \"pantry\"", "basket")]
    [InlineData("profile = \"standard\"", "profile = \"deep\"", "reserve")]
    [InlineData("recipe = \"restock\"", "recipe = \"bake\"", "recipe")]
    public void A_shared_definition_reference_names_its_own_section(
        string written, string broken, string target)
    {
        RulesetDiagnostic refusal = Refused(
            RulesetDiagnosticCode.Reference,
            ("dwelling.toml", SizedDwelling.Replace(written, broken, StringComparison.Ordinal)),
            ("goods.toml", Goods),
            ("shared.toml", SharedDefinitions));

        Assert.Equal("dwelling.toml", refusal.Path);
        Assert.Contains($"no [[{target}]]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every reference names a key the single-file reader actually reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The resolver runs ahead of the reader, so a key that moved would leave a reference
    /// resolving nothing while the reader refuses it again in its own words.</b> The surface is
    /// unioned over every shipped Ruleset, which is the bound <c>RulesetKeyNoteTests</c> and the
    /// generated reference work under too.
    /// </para>
    /// <para>
    /// ⚠ <b>One key is held by name instead, because no shipped Ruleset writes it</b> and
    /// <c>KeySurface</c> records only what a file asks for. <c>RulesetLoaderTests</c> and
    /// <c>BinTenancyLoadTests</c> cover the reader's side of a Rule's <c>fills</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_reference_names_a_key_the_reader_reads()
    {
        SortedSet<string> surface = Surface();
        var undemonstrated = new SortedSet<string>(StringComparer.Ordinal);

        foreach (RulesetSourceReference reference in RulesetSourceReferences.All)
        {
            string[] path = reference.Key.Split('.');
            string context = Context(reference.Section, surface);

            for (int i = 0; i < path.Length - 1; i++)
            {
                context += $" {path[i].TrimEnd('[', ']')}";
            }

            string key = $"{context} {path[^1]}";

            if (!surface.Contains(key))
            {
                undemonstrated.Add(key);
            }

            Context(reference.Target, surface);
        }

        Assert.Equal(["[[rule]] fills resource"], undemonstrated);
    }

    /// <summary>The bracketed spelling of <paramref name="section"/>, asserting the surface has one.</summary>
    private static string Context(string section, SortedSet<string> surface)
    {
        string repeated = $"[[{section}]]";

        if (surface.Any(key => key.StartsWith(repeated + " ", StringComparison.Ordinal)))
        {
            return repeated;
        }

        string single = $"[{section}]";

        Assert.True(
            surface.Any(key => key.StartsWith(single + " ", StringComparison.Ordinal)),
            $"no shipped Ruleset declares a section spelled {repeated} or {single}.");

        return single;
    }

    /// <summary>Every key the loader reads, unioned over every shipped Ruleset.</summary>
    private static SortedSet<string> Surface()
    {
        string folder = Path.Combine(RepoRoot(), "rulesets");
        var keys = new SortedSet<string>(StringComparer.Ordinal);

        foreach (string file in Directory.GetFiles(folder, "*.toml").OrderBy(p => p, StringComparer.Ordinal))
        {
            foreach ((string section, IReadOnlyDictionary<string, RulesetKeyKind> inside)
                in RulesetLoader.KeySurface(File.ReadAllText(file), Path.GetFileName(file)))
            {
                if (!section.StartsWith('['))
                {
                    continue;
                }

                foreach (string key in inside.Keys)
                {
                    keys.Add($"{section} {key}");
                }
            }
        }

        return keys;
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLAUDE.md")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        return directory!.FullName;
    }
}
