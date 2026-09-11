using Borough.Core.Entities;
using Borough.Core.Rules;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0072</c> D13 and D14 — <b>what a targeted relief takes off a profit-tax bill</b>, and
/// the two properties that make overlapping reliefs safe to author.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The claim worth the file is that application order is unobservable.</b> Reliefs are
/// declared in a list and a second one can be added to a Ruleset years after the first; if the
/// arithmetic composed them one after the other, inserting a relief above another in the file would
/// change what every qualifying Business pays, with nothing in the diff to suggest it.
/// </para>
/// <para>
/// ⚠ <b>The other claim is that a relief can never pay anybody.</b> A relief is revenue forgone, so
/// the floor is a bill of zero and not a payment out — that is a subsidy's job and a subsidy has a
/// funding ceiling precisely because it spends.
/// </para>
/// </remarks>
public sealed class TaxReliefTests
{
    private const byte Grocer = 0;
    private const byte Foundry = 1;

    [Fact]
    public void A_business_owing_nothing_gains_nothing_however_generous_the_relief()
    {
        Assert.Equal(0, TaxRelief.Relieved(0, 100));
    }

    [Fact]
    public void A_relief_never_turns_a_bill_into_a_payment()
    {
        Assert.Equal(0, TaxRelief.Relieved(40, 100));
    }

    [Fact]
    public void A_quarter_off_forty_leaves_thirty()
    {
        Assert.Equal(30, TaxRelief.Relieved(40, 25));
    }

    /// <summary>D14's worked example: two 25% reliefs against 40 save 20 in total.</summary>
    [Fact]
    public void Two_overlapping_reliefs_add_rather_than_compound()
    {
        (Ruleset rules, PolicyTable governed) = Catalogue(
            Relief(25, Grocer),
            Relief(25, Grocer));

        int percent = TaxRelief.PercentFor(rules, governed, Grocer);

        Assert.Equal(50, percent);
        Assert.Equal(20, TaxRelief.Relieved(40, percent));
    }

    /// <summary>
    /// Compounding would give 43.75% and floor to a different bill; the sum gives exactly half.
    /// </summary>
    [Fact]
    public void Compounding_would_give_a_different_answer_and_does_not_happen()
    {
        long compounded = TaxRelief.Relieved(TaxRelief.Relieved(40, 25), 25);

        Assert.Equal(23, compounded);
        Assert.NotEqual(compounded, TaxRelief.Relieved(40, 50));
    }

    [Fact]
    public void Reliefs_summing_past_the_whole_bill_are_capped_at_the_whole_bill()
    {
        (Ruleset rules, PolicyTable governed) = Catalogue(
            Relief(60, Grocer),
            Relief(70, Grocer));

        Assert.Equal(100, TaxRelief.PercentFor(rules, governed, Grocer));
        Assert.Equal(0, TaxRelief.Relieved(40, 100));
    }

    /// <summary>Order cannot change the result, which is the whole of D14.</summary>
    [Fact]
    public void Declaring_the_same_reliefs_in_the_other_order_pays_the_same()
    {
        (Ruleset one, PolicyTable governedOne) = Catalogue(
            Relief(10, Grocer), Relief(35, Grocer), Relief(5, Grocer));

        (Ruleset other, PolicyTable governedOther) = Catalogue(
            Relief(5, Grocer), Relief(10, Grocer), Relief(35, Grocer));

        Assert.Equal(
            TaxRelief.PercentFor(one, governedOne, Grocer),
            TaxRelief.PercentFor(other, governedOther, Grocer));
    }

    [Fact]
    public void A_relief_naming_a_trade_reaches_that_trade_and_no_other()
    {
        (Ruleset rules, PolicyTable governed) = Catalogue(Relief(25, Foundry));

        Assert.Equal(25, TaxRelief.PercentFor(rules, governed, Foundry));
        Assert.Equal(0, TaxRelief.PercentFor(rules, governed, Grocer));
    }

    [Fact]
    public void A_relief_naming_no_trade_reaches_every_trade()
    {
        (Ruleset rules, PolicyTable governed) = Catalogue(Relief(25, TradeKind.Any));

        Assert.Equal(25, TaxRelief.PercentFor(rules, governed, Grocer));
        Assert.Equal(25, TaxRelief.PercentFor(rules, governed, Foundry));
    }

    /// <summary>A charge sitting in the same catalogue is not a relief and must not be summed.</summary>
    [Fact]
    public void Only_reliefs_count_towards_the_relieved_share()
    {
        (Ruleset rules, PolicyTable governed) = Catalogue(
            Relief(25, Grocer),
            new PolicyDefinition(
                PolicySubject.Business, 2048, default, Scope.Local, Scope.Global,
                default, 40, Tool: PolicyTool.Charge, Trade: Grocer));

        Assert.Equal(25, TaxRelief.PercentFor(rules, governed, Grocer));
    }

    /// <summary>The player's rate wins over the authored one, as it does for every Policy.</summary>
    [Fact]
    public void A_governed_relief_rate_wins_over_the_authored_one()
    {
        (Ruleset rules, PolicyTable governed) = Catalogue(Relief(25, Grocer));

        governed.Govern(0, 60);

        Assert.Equal(60, TaxRelief.PercentFor(rules, governed, Grocer));
    }

    [Fact]
    public void A_catalogue_with_no_relief_in_it_forgoes_nothing()
    {
        (Ruleset rules, PolicyTable governed) = Catalogue();

        Assert.Equal(0, TaxRelief.PercentFor(rules, governed, Grocer));
        Assert.Equal(40, TaxRelief.Relieved(40, 0));
    }

    // ---- the fixture ----------------------------------------------------------------------------

    private static PolicyDefinition Relief(int percent, byte trade) =>
        new(
            PolicySubject.Business, 2048, default, Scope.Local, Scope.Local,
            default, percent, Tool: PolicyTool.Relief, Trade: trade);

    private static (Ruleset Rules, PolicyTable Governed) Catalogue(params PolicyDefinition[] policies)
    {
        var rules = new Ruleset(
            resources: [],
            rules: [],
            kinds: [],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],
            zoneRules: [])
        {
            Policies = policies,
        };

        return (rules, new PolicyTable(rules));
    }
}
