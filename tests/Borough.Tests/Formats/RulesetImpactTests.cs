using Borough.Formats;
using static Borough.Tests.Formats.SourcePackage;

namespace Borough.Tests.Formats;

/// <summary>
/// Plan 0077's impact preview: what replacing one resolved candidate with another would do,
/// compared statically and reported against the typed source ids.
/// </summary>
public sealed class RulesetImpactTests
{
    private const string Shared = """
        [[basket]]
        id = "basic"
        owner = "occupant"
        use_per_day = { sundries = 250 }

        [[reserve]]
        id = "standard"
        days = 3
        """;

    private const string Dwellings = """
        [[building]]
        id = "dwelling"
        houses = true
        premises = true
        bins = [
          { resource = "sundries", reserve = { profile = "standard", basket = "basic" }, owner = "occupant" },
          { resource = "repairs",  capacity = 4 },
        ]

        [[building]]
        id = "shed"
        premises = true
        bins = [ { resource = "repairs", capacity = 8 } ]

        [[rule]]
        id     = "consume"
        kind   = "dwelling"
        rate   = 32
        apply  = { min = 1, max = 1 }
        basket = "basic"

        [[rule]]
        id      = "upkeep"
        kind    = "shed"
        rate    = 512
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]
        outputs = []
        """;

    private static RulesetImpact Between(string before, string after) =>
        RulesetImpact.Between(
            Accepted(("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", before)),
            Accepted(("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", after)));

    private static RulesetImpactDeclaration Of(RulesetImpact impact, string section, string id) =>
        Assert.Single(impact.Declarations, d => d.Section == section && d.Id == id);

    [Fact]
    public void An_unedited_candidate_moves_nothing()
    {
        RulesetImpact impact = Between(Shared, Shared);

        Assert.True(impact.Ok);
        Assert.False(impact.Moves);
        Assert.Empty(impact.World);
        Assert.All(impact.Declarations, d => Assert.Equal(RulesetImpactVerdict.Unchanged, d.Verdict));
    }

    [Fact]
    public void A_shared_consumption_edit_reaches_its_dependants_and_nothing_else()
    {
        RulesetImpact impact = Between(
            Shared, Shared.Replace("sundries = 250", "sundries = 500", StringComparison.Ordinal));

        Assert.True(impact.Ok);

        // The basket supplies the Rule's input term and derives the Bin's capacity, so both move.
        Assert.Equal(RulesetImpactVerdict.Changed, Of(impact, "rule", "consume").Verdict);
        Assert.Equal(RulesetImpactVerdict.Changed, Of(impact, "building", "dwelling").Verdict);

        // Nothing that does not name the basket moves, and neither does the world.
        Assert.Equal(RulesetImpactVerdict.Unchanged, Of(impact, "rule", "upkeep").Verdict);
        Assert.Equal(RulesetImpactVerdict.Unchanged, Of(impact, "building", "shed").Verdict);
        Assert.Empty(impact.World);

        RulesetImpactValue amount = Assert.Single(
            Of(impact, "rule", "consume").Values, v => v.Path == "inputs[0].Amount");

        Assert.Equal("250", amount.Before);
        Assert.Equal("500", amount.After);
    }

    [Fact]
    public void A_local_exception_keeps_its_days_and_recomputes_its_capacity()
    {
        RulesetSourceResult before = Accepted(
            ("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", Shared));

        RulesetSourceResult after = Accepted(
            ("dwelling.toml", Dwellings.Replace(
                "{ profile = \"standard\", basket = \"basic\" }",
                "{ profile = \"standard\", basket = \"basic\", days = 5 }",
                StringComparison.Ordinal)),
            ("goods.toml", Goods),
            ("shared.toml", Shared));

        RulesetImpact impact = RulesetImpact.Between(before, after);

        RulesetImpactValue capacity = Assert.Single(
            Of(impact, "building", "dwelling").Values, v => v.Path == "bins[0].Capacity.Units");

        Assert.Equal("750", capacity.Before);
        Assert.Equal("1250", capacity.After);

        // The Rule reads the same basket at the same rate, so the exception reaches only the Bin.
        Assert.Equal(RulesetImpactVerdict.Unchanged, Of(impact, "rule", "consume").Verdict);
    }

    [Fact]
    public void A_dependant_is_listed_against_the_definition_it_names()
    {
        RulesetImpact impact = Between(
            Shared, Shared.Replace("sundries = 250", "sundries = 500", StringComparison.Ordinal));

        Assert.Equal(
            ["building/dwelling Changed", "rule/consume Changed"],
            impact.Dependants
                .Where(d => d.Section == "basket" && d.Id == "basic")
                .Select(d => $"{d.BySection}/{d.ById} {d.Verdict}")
                .ToArray());
    }

    [Fact]
    public void An_added_declaration_is_counted_and_carries_its_values()
    {
        const string Extra = """

            [[basket]]
            id = "spare"
            owner = "occupant"
            use_per_day = { repairs = 8 }
            """;

        RulesetImpact impact = Between(Shared, Shared + Extra);

        Assert.Equal(RulesetImpactVerdict.Added, Of(impact, "basket", "spare").Verdict);
        Assert.Equal(
            new RulesetImpactCount("basket", 1, 2),
            Assert.Single(impact.Counts, c => c.Section == "basket"));

        // An unreferenced shared definition reaches no Rule, so nothing else moves.
        Assert.Equal(RulesetImpactVerdict.Unchanged, Of(impact, "rule", "consume").Verdict);
    }

    /// <summary>
    /// A value holding a dense id is reported as the id its author wrote.
    /// </summary>
    /// <remarks>
    /// A dense id moves when a declaration is inserted, so <c>Resource.Raw 2 -&gt; 4</c> asks the
    /// reader to count declarations to find out what changed.
    /// </remarks>
    [Fact]
    public void A_dense_id_is_reported_as_the_authored_one()
    {
        RulesetImpact impact = RulesetImpact.Between(
            Accepted(("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", Shared)),
            Accepted(
                ("dwelling.toml", Dwellings.Replace(
                    "inputs  = [ { scope = \"local\", resource = \"repairs\", amount = 1 } ]",
                    "inputs  = [ { scope = \"local\", resource = \"sundries\", amount = 1 } ]",
                    StringComparison.Ordinal)),
                ("goods.toml", Goods),
                ("shared.toml", Shared)));

        RulesetImpactValue resource = Assert.Single(
            Of(impact, "rule", "upkeep").Values, v => v.Path == "inputs[0].Bin.Resource.Raw");

        Assert.Equal("repairs", resource.Before);
        Assert.Equal("sundries", resource.After);

        // A Rule a Building runs is named the same way, for the same reason.
        Assert.All(
            Of(impact, "building", "shed").Values.Where(v => v.Path.StartsWith("rules[")),
            v => Assert.Equal("upkeep", v.After));
    }

    [Fact]
    public void A_relabelled_declaration_is_not_a_changed_one()
    {
        RulesetImpact impact = Between(
            Shared, Shared.Replace("id = \"basic\"", "id = \"basic\"\nlabel = \"Weekly basics\"",
                StringComparison.Ordinal));

        RulesetImpactDeclaration basket = Of(impact, "basket", "basic");

        Assert.Equal(RulesetImpactVerdict.Relabelled, basket.Verdict);
        Assert.Equal("Weekly basics", basket.Label);
        Assert.Equal("basic", basket.WasLabelled);
        Assert.Empty(basket.Values);
    }

    [Fact]
    public void A_removed_declaration_reports_what_it_held()
    {
        string without = Dwellings
            .Replace("id = \"shed\"", "id = \"outhouse\"", StringComparison.Ordinal)
            .Replace("\"shed\"\n", "\"outhouse\"\n", StringComparison.Ordinal);

        RulesetImpact impact = RulesetImpact.Between(
            Accepted(("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", Shared)),
            Accepted(("dwelling.toml", without), ("goods.toml", Goods), ("shared.toml", Shared)));

        RulesetImpactDeclaration shed = Of(impact, "building", "shed");

        Assert.Equal(RulesetImpactVerdict.Removed, shed.Verdict);
        Assert.All(shed.Values, v => Assert.Null(v.After));
    }

    [Fact]
    public void The_report_is_bound_to_both_content_identities()
    {
        RulesetSourceResult before = Accepted(
            ("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", Shared));

        RulesetSourceResult after = Accepted(
            ("dwelling.toml", Dwellings),
            ("goods.toml", Goods),
            ("shared.toml", Shared.Replace("days = 3", "days = 4", StringComparison.Ordinal)));

        RulesetImpact impact = RulesetImpact.Between(before, after);

        Assert.Equal(before.Capture!.ContentHash, impact.BeforeIdentity);
        Assert.Equal(after.Capture!.ContentHash, impact.AfterIdentity);
        Assert.NotEqual(impact.BeforeIdentity, impact.AfterIdentity);
    }

    /// <summary>
    /// One id in two sections is two declarations, not a collision.
    /// </summary>
    /// <remarks>
    /// <c>adr/0048</c> separates namespaces by the typed table rather than by the fold, so
    /// <c>basic</c> as a basket and <c>basic</c> as a Building are distinct and both keyed by
    /// <c>ContentHash.Of(UTF8("basic"))</c>. The collision guard must not read that as one id
    /// declared twice.
    /// </remarks>
    [Fact]
    public void One_id_in_two_sections_is_not_a_collision()
    {
        const string Named = """
            [[building]]
            id = "basic"
            premises = true
            bins = [ { resource = "repairs", capacity = 8 } ]
            """;

        RulesetImpact impact = RulesetImpact.Between(
            Accepted(("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", Shared)),
            Accepted(
                ("dwelling.toml", Dwellings + "\n" + Named),
                ("goods.toml", Goods),
                ("shared.toml", Shared)));

        Assert.True(impact.Ok, impact.Describe());
        Assert.Equal(RulesetImpactVerdict.Added, Of(impact, "building", "basic").Verdict);
        Assert.Equal(RulesetImpactVerdict.Unchanged, Of(impact, "basket", "basic").Verdict);
    }

    [Fact]
    public void A_refused_candidate_has_no_report()
    {
        RulesetImpact impact = RulesetImpact.Between(
            Accepted(("dwelling.toml", Dwellings), ("goods.toml", Goods), ("shared.toml", Shared)),
            Resolve(
                ("dwelling.toml", Dwellings), ("goods.toml", Goods),
                ("shared.toml", Shared.Replace("days = 3", "days = -1", StringComparison.Ordinal))));

        Assert.False(impact.Ok);
        Assert.Empty(impact.Declarations);
        Assert.Contains("the replacement was refused", impact.Describe(), StringComparison.Ordinal);
    }
}
