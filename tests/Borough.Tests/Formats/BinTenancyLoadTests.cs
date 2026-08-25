using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 25 task 2: <c>owner</c> on a Bin declaration, and the Rule tenancy derived from it.
/// </summary>
/// <remarks>
/// <b><c>adr/0141</c>'s line, at the parse site.</b> A Bin belongs to the Occupant whose leaving
/// would empty it and to the premises otherwise; the premises declare the capacity either way. The
/// Rule half is <em>derived</em> — a Rule whose local terms all address a tenant's Bins is a tenant's
/// Rule — so there is nothing to author and one thing to refuse: a Rule that addresses both.
/// </remarks>
public sealed class BinTenancyLoadTests
{
    /// <summary>
    /// A dwelling shaped like <c>rulesets/minimal.toml</c>'s: the roof is the landlord's and the
    /// larder is the tenant's.
    /// </summary>
    private const string Tenanted = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "repairs"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 3
        bins = [
          { resource = "sundries", capacity = 48, owner = "occupant" },
          { resource = "repairs",  capacity = 4 },
        ]

        [[rule]]
        name    = "consume"
        kind    = "dwelling"
        rate    = 64
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "sundries", amount = 1 } ]
        outputs = []

        [[rule]]
        name    = "upkeep"
        kind    = "dwelling"
        rate    = 512
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]
        outputs = []
        """;

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

    [Fact]
    public void A_bin_declares_whose_level_it_holds_and_the_premises_keep_the_capacity()
    {
        Ruleset ruleset = Accepted(Tenanted);
        ReadOnlySpan<BinDeclaration> bins = ruleset.BinsOf(1);

        Assert.Equal(2, bins.Length);

        Assert.Equal(BinTenancy.Occupant, bins[0].Tenancy);
        Assert.Equal(48, bins[0].Capacity.Units);

        Assert.Equal(BinTenancy.Premises, bins[1].Tenancy);
        Assert.Equal(4, bins[1].Capacity.Units);
    }

    /// <summary>
    /// The default is the premises', which is what every Ruleset written before this existed meant.
    /// </summary>
    [Fact]
    public void An_absent_owner_leaves_the_bin_with_the_premises()
    {
        Ruleset ruleset = Accepted(Tenanted.Replace(", owner = \"occupant\"", "", StringComparison.Ordinal));

        Assert.Equal(BinTenancy.Premises, ruleset.BinsOf(1)[0].Tenancy);
    }

    [Fact]
    public void A_rule_takes_the_tenancy_of_the_bins_its_local_terms_address()
    {
        Ruleset ruleset = Accepted(Tenanted);

        Assert.Equal(BinTenancy.Occupant, ruleset.Rule(new RuleId(1)).Tenancy);
        Assert.Equal(BinTenancy.Premises, ruleset.Rule(new RuleId(2)).Tenancy);
    }

    /// <summary>
    /// A Rule with no local term belongs to the premises: nothing about it leaves with a tenant.
    /// </summary>
    [Fact]
    public void A_rule_with_no_local_term_is_the_premises()
    {
        Ruleset ruleset = Accepted(Tenanted.Replace(
            """inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]""",
            """inputs  = [ { scope = "pool", resource = "repairs", amount = 1 } ]""",
            StringComparison.Ordinal));

        Assert.Equal(BinTenancy.Premises, ruleset.Rule(new RuleId(2)).Tenancy);
    }

    /// <summary>
    /// The one refusal this feature adds: a Rule that cannot say whose it is.
    /// </summary>
    [Fact]
    public void A_rule_addressing_both_owners_locally_is_refused()
    {
        RulesetRefusal refusal = Refused(Tenanted.Replace(
            """inputs  = [ { scope = "local", resource = "sundries", amount = 1 } ]""",
            """
            inputs  = [
              { scope = "local", resource = "sundries", amount = 1 },
              { scope = "local", resource = "repairs",  amount = 1 },
            ]
            """,
            StringComparison.Ordinal));

        Assert.Contains("two owners", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("consume", refusal.Rule);
        Assert.True(refusal.Line > 0);
    }

    /// <summary>
    /// A fallback chain is a ladder over one Bin, so a link relieving a tenant's Bin from the
    /// premises' is the same refusal arriving one link along.
    /// </summary>
    [Fact]
    public void A_link_filling_a_tenants_bin_from_the_premises_is_refused()
    {
        RulesetRefusal refusal = Refused(Tenanted + """

            [[rule]]
            name    = "restock"
            kind    = "dwelling"
            rate    = 64
            apply   = { min = 1, max = 1 }
            fills   = { scope = "local", resource = "sundries" }
            inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]
            outputs = []
            """);

        Assert.Contains("two owners", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("restock", refusal.Rule);
    }

    [Fact]
    public void An_owner_that_is_not_a_side_of_a_tenancy_is_refused_by_name()
    {
        RulesetRefusal refusal = Refused(
            Tenanted.Replace("owner = \"occupant\"", "owner = \"tenant\"", StringComparison.Ordinal));

        Assert.Contains("'tenant' is not a Bin owner", refusal.Reason, StringComparison.Ordinal);

        // All three, because adr/0166 gave the tenancy a third side and a refusal that named two
        // would send an author looking for a key that exists.
        Assert.Contains("premises, occupant and business", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A shopfront whose stock is the trade's and whose roof is the landlord's, plus the money
    /// declaration that exists only to say whose a money term is.
    /// </summary>
    /// <remarks>
    /// <b>The <c>money</c> row declares no <c>capacity</c> and opens no Bin</b> — <c>OpenBalance</c>
    /// does that, unbounded — and it is here so <c>ApplyTenancies</c> has something to look up for
    /// <c>rent</c>'s local term. ***Without it that Rule derives to the PREMISES***, which is
    /// <c>plans/0044</c> open decision 4 and the reason this fixture exists.
    /// </remarks>
    private const string Trading = """
        [[resource]]
        name = "stock"
        family = "good"

        [[resource]]
        name = "repairs"
        family = "good"

        [[resource]]
        name = "money"
        family = "money"

        [[business]]
        name = "shop"

        [[building]]
        name = "shopfront"
        occupants = 3
        business = "shop"
        bins = [
          { resource = "stock",   capacity = 96, owner = "business" },
          { resource = "repairs", capacity = 4 },
          { resource = "money",                  owner = "business" },
        ]

        [[rule]]
        name    = "sell"
        kind    = "shopfront"
        rate    = 64
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "stock", amount = 1 } ]
        outputs = []

        [[rule]]
        name    = "rent"
        kind    = "shopfront"
        rate    = 512
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local",  resource = "money", amount = 1 } ]
        outputs = [ { scope = "global", resource = "money", amount = 1 } ]

        [[rule]]
        name    = "upkeep"
        kind    = "shopfront"
        rate    = 512
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]
        outputs = []
        """;

    /// <summary>
    /// <c>adr/0166</c>'s third side: a Bin the trade holds, not the family and not the landlord.
    /// </summary>
    [Fact]
    public void A_bin_may_belong_to_the_business_tenanting_the_premises()
    {
        Ruleset ruleset = Accepted(Trading);
        ReadOnlySpan<BinDeclaration> bins = ruleset.BinsOf(1);

        Assert.Equal(3, bins.Length);

        Assert.Equal(BinTenancy.Business, bins[0].Tenancy);
        Assert.Equal(96, bins[0].Capacity.Units);

        Assert.Equal(BinTenancy.Premises, bins[1].Tenancy);
        Assert.Equal(BinTenancy.Business, bins[2].Tenancy);
    }

    /// <summary>
    /// The derivation reaches all three sides, and <c>rent</c> is the one that matters.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>rent</c> is <c>adr/0166</c>'s decline half</b> — a trade going short of money is
    /// <c>Blocking.Supply</c> on a money Bin, which is bankruptcy rather than starvation. **A money
    /// Bin is never declared, so before <c>plans/0044</c> open decision 4 the lookup found nothing
    /// and fell through to the default**: ***the bankruptcy Rule derived to the landlord, silently
    /// and without a refusal.*** This assertion is the whole reason a money Resource may be declared
    /// in <c>[[building]] bins</c> at all.
    /// </remarks>
    [Fact]
    public void A_rule_addressing_money_takes_the_tenancy_that_bin_declaration_states()
    {
        Ruleset ruleset = Accepted(Trading);

        Assert.Equal(BinTenancy.Business, ruleset.Rule(new RuleId(1)).Tenancy);
        Assert.Equal(BinTenancy.Business, ruleset.Rule(new RuleId(2)).Tenancy);
        Assert.Equal(BinTenancy.Premises, ruleset.Rule(new RuleId(3)).Tenancy);
    }

    /// <summary>
    /// ⚠ <b>The failure this fixture was built to name</b>, asserted by removing the declaration
    /// rather than by describing it.
    /// </summary>
    [Fact]
    public void Money_with_no_declaration_leaves_a_rule_addressing_it_with_the_premises()
    {
        Ruleset ruleset = Accepted(
            Trading.Replace(
                "  { resource = \"money\",                  owner = \"business\" },\n",
                "",
                StringComparison.Ordinal));

        Assert.Equal(BinTenancy.Business, ruleset.Rule(new RuleId(1)).Tenancy);
        Assert.Equal(BinTenancy.Premises, ruleset.Rule(new RuleId(2)).Tenancy);
    }

    /// <summary>
    /// A Business's Bin and a Household's are two owners, exactly as a tenant's and the landlord's
    /// are — which is what keeps <c>World.FindLocalBin</c>'s ternary sound without a tag column.
    /// </summary>
    [Fact]
    public void A_rule_addressing_both_a_business_bin_and_an_occupant_bin_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Trading
                .Replace(
                    "{ resource = \"repairs\", capacity = 4 },",
                    "{ resource = \"repairs\", capacity = 4, owner = \"occupant\" },",
                    StringComparison.Ordinal)
                .Replace(
                    "inputs  = [ { scope = \"local\", resource = \"stock\", amount = 1 } ]",
                    "inputs  = [ { scope = \"local\", resource = \"stock\", amount = 1 }, "
                        + "{ scope = \"local\", resource = \"repairs\", amount = 1 } ]",
                    StringComparison.Ordinal));

        Assert.Contains("two owners", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("sell", refusal.Rule);
    }

    [Fact]
    public void An_owner_that_is_not_a_string_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Tenanted.Replace("owner = \"occupant\"", "owner = 1", StringComparison.Ordinal));

        Assert.Contains("owner must be a quoted string", refusal.Reason, StringComparison.Ordinal);
    }
}
