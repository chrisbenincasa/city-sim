using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 10 task 5 — the <c>[[policy]]</c> section, and the eleven refusals it brings.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, which is
/// the discipline the whole loader suite is held to and the one <c>adr/0064</c> found a hole in: the
/// loader's single untested guard was invisible to the reader who later decided it did not exist.
/// </para>
/// <para>
/// ⚠ <b>The refusal that is <em>not</em> here is the interesting one.</b> A <c>[[rule]]</c> needs
/// refusal 4 — <em>every money term needs a counterparty</em> — because its money terms are two
/// free-form lists. A Policy states a <c>from</c> and a <c>to</c>, so the same quantity leaves one
/// Bin and enters the other by construction, and there is nothing unbalanced to write.
/// ***A transfer written as a direction cannot leak; one written as two lists has to be checked.***
/// </para>
/// </remarks>
public sealed class PolicyLoadTests
{
    /// <summary>The smallest file that declares money and a Household to hold it.</summary>
    /// <remarks>
    /// <b>Named <c>Currency</c> rather than <c>Money</c>, and it is the second sighting of the same
    /// trap inside one task.</b> A member named after a type makes that type unnameable in the whole
    /// class, so <c>Money.Zero</c> below would not compile — which is exactly what
    /// <c>RulesetLoader.MoneyIn</c> was renamed for an hour earlier. ***A helper named after a type
    /// does not shadow one call site, it shadows the whole class***, and the tell is that the second
    /// author walks into it without having heard about the first.
    /// </remarks>
    private const string Currency = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 3
        bins = [ { resource = "sundries", capacity = 48 } ]
        """;

    private const string Levy = """

        [[policy]]
        name = "levy"
        sweeps = "household"
        interval = 2048
        apply = { derived = "balance", percent = 10 }
        transfer = { from = "local", to = "global", resource = "money", amount = 1 }
        """;

    [Fact]
    public void A_policy_loads_as_a_subject_an_interval_an_apply_count_and_a_direction()
    {
        Ruleset ruleset = Accepted(Currency + Levy);

        PolicyDefinition policy = Assert.Single(ruleset.Policies);

        Assert.Equal(PolicySubject.Household, policy.Subject);
        Assert.Equal(2048u, policy.Interval);
        Assert.True(policy.Apply.IsDerived);
        Assert.Equal(10, policy.Apply.Percent);
        Assert.Equal(Scope.Local, policy.From);
        Assert.Equal(Scope.Global, policy.To);
        Assert.Equal(1, policy.Amount);
        Assert.Equal(ResourceFamily.Money, ruleset.Family(policy.Resource));
    }

    /// <summary>A rebate is the same shape with the two ends the other way round.</summary>
    [Fact]
    public void A_transfer_runs_in_both_directions()
    {
        Ruleset ruleset = Accepted(Currency + Levy.Replace(
            "from = \"local\", to = \"global\"",
            "from = \"global\", to = \"local\"",
            StringComparison.Ordinal));

        Assert.Equal(Scope.Global, ruleset.Policies[0].From);
        Assert.Equal(Scope.Local, ruleset.Policies[0].To);
    }

    /// <summary>A flat quantum is the band form, and <c>min</c> is what applies.</summary>
    [Fact]
    public void A_flat_transfer_is_a_band()
    {
        Ruleset ruleset = Accepted(Currency + Levy.Replace(
            "apply = { derived = \"balance\", percent = 10 }",
            "apply = { min = 100, max = 100 }",
            StringComparison.Ordinal));

        Assert.False(ruleset.Policies[0].Apply.IsDerived);
        Assert.Equal(100, ruleset.Policies[0].Apply.Min);
    }

    [Fact]
    public void A_policy_sweeping_nothing_nameable_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + Levy.Replace(
            "sweeps = \"household\"", "sweeps = \"citizen\"", StringComparison.Ordinal));

        Assert.Contains("is not a population", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>A population <c>02 §4.2</c> declares and this build does not sweep is refused by name.</b>
    /// </summary>
    /// <remarks>
    /// The unusual half: the enum has the member, so accepting it would produce a Policy that
    /// triggers, reaches nobody and reports nothing — the silent non-event <c>02 §4.1</c> bans. A
    /// named hole beats a case that falls through, which is <c>Scope.Pool</c>'s precedent.
    /// </remarks>
    [Theory]
    [InlineData("business")]
    [InlineData("building")]
    public void A_population_the_build_declares_and_does_not_sweep_is_refused(string subject)
    {
        RulesetRefusal refusal = Refused(Currency + Levy.Replace(
            "sweeps = \"household\"",
            $"sweeps = \"{subject}\"",
            StringComparison.Ordinal));

        Assert.Contains("does not sweep", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_policy_with_no_transfer_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + """

            [[policy]]
            name = "levy"
            sweeps = "household"
            interval = 2048
            apply = { min = 1, max = 1 }
            """);

        Assert.Contains("no transfer", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>pool</c> and <c>map</c> are refused by name rather than resolving to a named hole at run
    /// time.
    /// </summary>
    /// <remarks>
    /// A pool term is a <em>purchase</em> whose payment is implicit at the prevailing price
    /// (<c>adr/0050</c>), so a Policy writing one would author the money side of a trade the design
    /// says is never authored. A map term is a Layer emission and holds nothing.
    /// </remarks>
    [Theory]
    [InlineData("pool")]
    [InlineData("map")]
    [InlineData("treasury")]
    public void An_end_of_a_transfer_that_is_not_local_or_global_is_refused(string scope)
    {
        RulesetRefusal refusal = Refused(Currency + Levy.Replace(
            "from = \"local\"", $"from = \"{scope}\"", StringComparison.Ordinal));

        Assert.Contains("is not an end of a transfer", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_transfer_from_a_bin_to_itself_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + Levy.Replace(
            "to = \"global\"", "to = \"local\"", StringComparison.Ordinal));

        Assert.Contains("nets to zero", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A Policy moves money, and a city-wide larder of a Good is a different mechanism.</summary>
    [Fact]
    public void A_transfer_of_a_good_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + Levy.Replace(
            "resource = \"money\"", "resource = \"sundries\"", StringComparison.Ordinal));

        Assert.Contains("is not money", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_transfer_of_nothing_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + Levy.Replace(
            "amount = 1", "amount = 0", StringComparison.Ordinal));

        Assert.Contains("is not a quantity per application", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>A Readout of the wrong scope is refused, in both directions.</b>
    /// </summary>
    /// <remarks>
    /// The check is new in kind: every Readout before <c>balance</c> hung off a Building, so a Bin
    /// Rule could name any declared one and the question never arose. Both of these name a real
    /// Readout with no row here to read it from, and the interpreter throws on either — so the loader
    /// says it with a file and a line, which is <c>adr/0048</c>'s division of labour.
    /// </remarks>
    [Fact]
    public void A_policy_naming_a_building_scoped_readout_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + Levy.Replace(
            "derived = \"balance\"", "derived = \"occupancy\"", StringComparison.Ordinal));

        Assert.Contains("Building-scoped Readout", refusal.Reason, StringComparison.Ordinal);
    }

    /// <inheritdoc cref="A_policy_naming_a_building_scoped_readout_is_refused"/>
    [Fact]
    public void A_bin_rule_naming_a_household_scoped_readout_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + """

            [[rule]]
            name    = "levy"
            kind    = "dwelling"
            rate    = 10
            apply   = { derived = "balance", percent = 10 }
            inputs  = []
            outputs = [ { scope = "local", resource = "sundries", amount = 1 } ]
            """);

        Assert.Contains("Household-scoped Readout", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the opening balance band -------------------------------------------------------------

    private const string Households = """

        [households]
        car_ownership_percent = 0
        opening_balance_min = 100
        opening_balance_max = 1000
        """;

    [Fact]
    public void An_opening_balance_band_loads()
    {
        Ruleset ruleset = Accepted(Currency + Households);

        Assert.Equal(new Money(100), ruleset.Households.OpeningBalanceMin);
        Assert.Equal(new Money(1000), ruleset.Households.OpeningBalanceMax);
        Assert.True(ruleset.Households.Endows);
    }

    /// <summary>
    /// Omitting both keys is a city where the populator endows nobody, which is every Ruleset written
    /// before this task and is therefore behaviour-preserving.
    /// </summary>
    [Fact]
    public void Omitting_the_band_endows_nobody()
    {
        Ruleset ruleset = Accepted(Currency + "\n[households]\ncar_ownership_percent = 0\n");

        Assert.False(ruleset.Households.Endows);
        Assert.Equal(Money.Zero, ruleset.Households.OpeningBalanceMax);
    }

    /// <summary>One end of a band has two readings, and they are different cities.</summary>
    [Theory]
    [InlineData("opening_balance_min = 100")]
    [InlineData("opening_balance_max = 1000")]
    public void One_end_of_the_band_alone_is_refused(string key)
    {
        RulesetRefusal refusal = Refused(
            Currency + $"\n[households]\ncar_ownership_percent = 0\n{key}\n");

        Assert.Contains("states one end of it", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_opening_balance_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + Households.Replace(
            "opening_balance_min = 100", "opening_balance_min = -1", StringComparison.Ordinal));

        Assert.Contains("a stock is never", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void An_inverted_band_is_refused()
    {
        RulesetRefusal refusal = Refused(Currency + Households.Replace(
            "opening_balance_max = 1000", "opening_balance_max = 99", StringComparison.Ordinal));

        Assert.Contains("below opening_balance_min", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>Endowing in a file that names no money is refused at load rather than throwing at world
    /// creation.</b>
    /// </summary>
    /// <remarks>
    /// A balance is a Bin and a Bin exists only for a declared Resource (<c>adr/0114</c>), so
    /// <c>World.Endow</c> would throw on the first Household. The loader can see both halves — the
    /// families are read in the first pass — so it says so with a file and a line.
    /// </remarks>
    [Fact]
    public void A_band_in_a_ruleset_that_names_no_money_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "sundries"
            family = "good"

            [[building]]
            name = "dwelling"
            occupants = 3
            bins = [ { resource = "sundries", capacity = 48 } ]

            [households]
            car_ownership_percent = 0
            opening_balance_min = 100
            opening_balance_max = 1000
            """);

        Assert.Contains("endows Households and names no money", refusal.Reason, StringComparison.Ordinal);
    }

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }
}
