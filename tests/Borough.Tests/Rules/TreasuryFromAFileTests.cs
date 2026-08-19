using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 10 tasks 2 and 4c: the shipped Rulesets declare a conserved Resource, and a world on one
/// opens with a treasury.
/// </summary>
/// <remarks>
/// <para>
/// <b>A shipped file has to be held to something or it is a literal nothing held to a file.</b> That
/// is 5a-bis's finding about <c>minimal-tuned.toml</c>, whose stated content hash guarded a file no
/// test ever opened, and the repair takes the same shape here: the claims the files make in their own
/// headers are the assertions in this class.
/// </para>
/// <para>
/// ⚠ <b>Task 2 put money in one new file and task 4c put it in all five, which changed what the
/// negative half can be asserted over.</b> <c>adr/0114</c> made a balance a Bin, and a Bin exists only
/// for a Resource a Ruleset names — so on a moneyless file a Household cannot hold money <em>at
/// all</em>, where a column held it whatever the file said. Every shipped Ruleset therefore names
/// money, and the moneyless case moved to a Ruleset built here. ***Making a quantity conditional on
/// the Ruleset turns every fixture's Ruleset into a statement about what that fixture can test.***
/// </para>
/// <para>
/// <b>The negative half is still the load-bearing one</b> — a treasury with a Bin in it is evidence
/// that <c>World.FitTreasury</c> read the Ruleset only if a treasury with <em>none</em> is what a file
/// naming no money produces. What it lost by moving off a shipped file is that it no longer also
/// asserts the survey finding; <see cref="Every_shipped_ruleset_names_money"/> carries that half now,
/// and carries it in the direction that will actually fail — a sixth file added without money.
/// </para>
/// </remarks>
public sealed class TreasuryFromAFileTests
{
    /// <summary>Every Ruleset this project ships.</summary>
    public static TheoryData<string> Shipped =>
        ["minimal.toml", "minimal-tuned.toml", "severance.toml", "congested.toml", "diagnosed.toml"];

    /// <summary>
    /// A world on any shipped Ruleset opens with one treasury Bin, empty and unbounded.
    /// </summary>
    /// <remarks>
    /// <b>This is the whole of what the <c>[[resource]]</c> block buys, and no key in it says so.</b>
    /// <c>World.FitTreasury</c> walks the Ruleset's Resources at world creation and gives the treasury
    /// one Bin per conserved one (<c>adr/0116</c>), so a three-line block is the entire vocabulary a
    /// Ruleset has for making the treasury real — there is no table to author, no kind to declare it
    /// on, and nothing to tune.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Shipped))]
    public void A_world_on_a_shipped_ruleset_opens_with_one_empty_unbounded_treasury_bin(string file)
    {
        (Ruleset rules, RulesetNames names) = Load(file);
        var world = new World(1_000, rules);

        int[] bins = [.. world.TreasuryBins.Walk(TreasuryTable.Slot)];

        Assert.Single(bins);
        Assert.True(rules.IsConserved(world.Bins.Resource[bins[0]]));
        Assert.Equal("money", names.Resource(world.Bins.Resource[bins[0]]));
        Assert.Equal(0, world.Bins.LevelAt(bins[0]));
        Assert.Equal(long.MaxValue, world.Bins.Capacity[bins[0]]);
        Assert.Equal(BinOwnerKind.Treasury, world.Bins.OwnerKind[bins[0]]);
    }

    /// <summary>
    /// Every shipped Ruleset declares exactly one money Resource, and it is spelled the same way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The survey milestone 10 opened on, asserted rather than remembered.</b> Before task 2 every
    /// <c>[[resource]]</c> in the tree declared <c>family = "good"</c> and the only
    /// <c>family = "money"</c> anywhere was inside a loader test, so the machinery that refuses a
    /// badly-authored currency had never met an authored one.
    /// </para>
    /// <para>
    /// ⚠ <b>Exactly one, because <c>World.TryMoneyResource</c> throws on a second.</b> An actor's
    /// balance is a single saved Bin handle, so a second conserved Resource is a decision about what a
    /// Household holds two of — which <c>adr/0114</c>'s revisit trigger already calls a decision rather
    /// than a detail. A file that quietly added one would give every actor a balance in the first
    /// currency and none in the second, and every conservation sum would still add up, because money
    /// in a Resource nobody can hold is money nothing can lose.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(Shipped))]
    public void Every_shipped_ruleset_names_money(string file)
    {
        (Ruleset rules, RulesetNames names) = Load(file);

        var conserved = new List<string>();

        for (int raw = 1; raw <= rules.ResourceCount; raw++)
        {
            var resource = new ResourceId((ushort)raw);

            if (rules.IsConserved(resource))
            {
                conserved.Add(names.Resource(resource) ?? "<unnamed>");
            }
        }

        Assert.Equal<string[]>(["money"], [.. conserved]);
    }

    /// <summary>
    /// A Ruleset that names no money gives the treasury no Bins, and its Households no balance.
    /// </summary>
    /// <remarks>
    /// <b>The negative that makes the positive mean something</b>, and it asserts both ends of
    /// <c>adr/0114</c> at once: no treasury Bin, and no actor balance either. The second is the one
    /// that is new — a Household's money used to be a column, which exists whatever the file says, and
    /// is now a Bin, which does not. An unset balance handle is legitimately unset rather than
    /// dangling, which is why <c>Reference.Required</c> is still the right declaration for it.
    /// </remarks>
    [Fact]
    public void A_ruleset_that_names_no_money_gives_no_treasury_bin_and_no_balance()
    {
        var world = new World(1_000);

        Assert.Equal<int[]>([], [.. world.TreasuryBins.Walk(TreasuryTable.Slot)]);

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);

        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);
        Handle<Business> business = world.CreateBusiness(building);

        Assert.True(world.Households.Balance[world.Households.Rows.Resolve(household)].IsNone);
        Assert.True(world.Businesses.Balance[world.Businesses.Rows.Resolve(business)].IsNone);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A shipped Ruleset, loaded from beside the test assembly as the runner loads it.</summary>
    private static (Ruleset Rules, RulesetNames Names) Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(Path(file));

        Assert.True(
            result.Ok,
            $"rulesets/{file} was refused:\n  "
            + result.Describe());

        return (result.Ruleset!, result.Names);
    }

    private static string Path(string file) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file);
}
