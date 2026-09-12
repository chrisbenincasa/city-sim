using Borough.Core.Determinism;
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
    /// <summary>
    /// Every Ruleset this project ships, <b>read from the directory rather than listed here</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It was a hand-written list of six and the tree held seven, and nothing could say so.</b>
    /// <c>monetised.toml</c> shipped and was never named here, so the one test that surveys every
    /// shipped file had been surveying all but one of them — and the paragraph above claiming this
    /// fails <i>in the direction that will actually fail, a sixth file added without money</i> was
    /// describing a check that a sixth file had already walked past. ***A hand-written list of the
    /// files in a directory is a copy of that directory, and it goes stale the first time somebody
    /// adds one*** — <c>plans/0012</c> <b>Cause 1</b>, inside the instrument, which is the shape
    /// mechanical check 5 was written against.
    /// </para>
    /// <para>
    /// <b>Generated, on check 5's own precedent</b> — <i>generate the ADR column from the directory,
    /// because only that column is generable and a missing row makes an unassessed decision read as
    /// absent</i>. The <c>.csproj</c> copies <c>rulesets/*.toml</c> by glob, so a file added to the
    /// repository is a case here on the next build with nothing to remember.
    /// </para>
    /// </remarks>
    public static TheoryData<string> Shipped
    {
        get
        {
            TheoryData<string> files = [];

            foreach (string path in Directory
                .EnumerateFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets"), "*.toml")
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                files.Add(System.IO.Path.GetFileName(path));
            }

            Assert.NotEmpty(files);

            return files;
        }
    }

    /// <summary>
    /// A world on any shipped Ruleset opens with one unbounded treasury Bin, holding what the file
    /// founded it with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the whole of what the <c>[[resource]]</c> block buys, and no key in it says so.</b>
    /// <c>World.FitTreasury</c> walks the Ruleset's Resources at world creation and gives the treasury
    /// one Bin per conserved one (<c>adr/0116</c>), so a three-line block is the entire vocabulary a
    /// Ruleset has for making the treasury real — there is no kind to declare it on and nothing to
    /// tune.
    /// </para>
    /// <para>
    /// ⚠ <b>The level is the file's own <c>[treasury] opening_balance</c>, and absent means zero.</b>
    /// This asserted a flat zero for every file while no file could author one. <c>adr/0116</c>'s
    /// empty treasury is still the default and still what <c>levied.toml</c>'s demonstration needs —
    /// what changed is that a world may now be founded with money, so the invariant is <em>the Bin
    /// holds what the Ruleset says</em> rather than <em>the Bin holds nothing</em>.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(Shipped))]
    public void A_world_on_a_shipped_ruleset_opens_with_one_unbounded_treasury_bin(string file)
    {
        (Ruleset rules, RulesetNames names) = Load(file);
        var world = new World(1_000, rules);

        int[] bins = [.. world.TreasuryBins.Walk(TreasuryTable.Slot)];

        Assert.Single(bins);
        Assert.True(rules.IsConserved(world.Bins.Resource[bins[0]]));
        Assert.Equal("money", names.Resource(world.Bins.Resource[bins[0]]));
        Assert.Equal(rules.Treasury.OpeningBalance.Raw, world.Bins.LevelAt(bins[0]));
        Assert.Equal(long.MaxValue, world.Bins.Capacity[bins[0]]);
        Assert.Equal(BinOwnerKind.Treasury, world.Bins.OwnerKind[bins[0]]);
    }

    /// <summary>
    /// Exactly one shipped Ruleset founds its treasury with money, and every other opens empty.
    /// </summary>
    /// <remarks>
    /// <b>The survey, asserted rather than remembered.</b> <c>adr/0116</c> chose an empty opening
    /// treasury so <c>02 §4.2</c>'s exhaustion branch is reachable on the first sweep, and a defaulted
    /// balance would delete that reachability from every file at once. <c>funded.toml</c> overrides it
    /// because a player with no money has no fiscal decision to make, and it is the only file that
    /// should — so this fails when a second one acquires the key, which is the moment to ask whether
    /// the default still holds.
    /// </remarks>
    [Fact]
    public void Only_the_funded_world_opens_with_money()
    {
        var founded = new List<string>();

        foreach (string path in Directory
            .EnumerateFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets"), "*.toml")
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            string file = System.IO.Path.GetFileName(path);
            (Ruleset rules, _) = Load(file);

            if (rules.Treasury.OpeningBalance.Raw != 0)
            {
                founded.Add(file);
            }
        }

        Assert.Equal(["funded.toml"], founded);
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

    // ---- the opening balance --------------------------------------------------------------------

    /// <summary>The smallest complete Ruleset that names money.</summary>
    private const string Named = """
        [[resource]]
        name = "money"
        family = "money"
        """;

    /// <summary>
    /// <c>[treasury] opening_balance</c> reaches the treasury's Bin, and the supply of record with
    /// it.
    /// </summary>
    /// <remarks>
    /// <b>Both halves, because one of them alone is a leak.</b> <c>World.EndowTreasury</c> deposits
    /// and writes <c>MoneySupplyTable.Issued</c> in one call, so a balance that arrived without the
    /// second write would be money the city holds and nothing issued —
    /// <c>Invariant.MoneyIsConserved</c>'s failure, asserted here by running it.
    /// </remarks>
    [Fact]
    public void An_opening_balance_reaches_the_treasury_bin_and_the_money_supply()
    {
        var world = new World(1_000, Parse($"{Named}\n\n[treasury]\nopening_balance = 4194304\n"));

        Assert.Equal(new Money(4_194_304), world.TreasuryBalance());
        Assert.Equal(new Money(4_194_304), world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>A file that states no <c>[treasury]</c> opens with nothing</b>, which is <c>adr/0116</c>'s
    /// empty treasury and the city <c>rulesets/levied.toml</c> demonstrates.
    /// </summary>
    [Fact]
    public void A_ruleset_with_no_treasury_table_opens_the_treasury_empty()
    {
        var world = new World(1_000, Parse(Named));

        Assert.Equal(Money.Zero, world.TreasuryBalance());
        Assert.Equal(Money.Zero, world.MoneySupply.Issued[MoneySupplyTable.Slot]);
    }

    /// <summary>
    /// <b>A reload that moves the opening balance is refused before anything moves.</b>
    /// </summary>
    /// <remarks>
    /// <c>MapLayers.Adopt</c>'s shape, for its reason. A Ruleset is hot-reloadable (<c>adr/0015</c>)
    /// and world creation is not, so re-reading the balance would mint money into a city that has
    /// already spent what it was founded with — and <c>Invariant.MoneyIsConserved</c> would stay
    /// green, because the issuance is recorded.
    /// </remarks>
    [Fact]
    public void A_reload_that_moves_the_opening_balance_is_refused()
    {
        Ruleset opening = Parse($"{Named}\n\n[treasury]\nopening_balance = 1000\n");
        var world = new World(1_000, opening);

        Assert.Throws<InvalidOperationException>(() => world.Adopt(
            Parse($"{Named}\n\n[treasury]\nopening_balance = 2000\n"),
            contentHash: 2,
            Ticks.Zero,
            WorldKey.FromSeed(7)));

        Assert.Same(opening, world.Rules);
        Assert.Equal(new Money(1_000), world.TreasuryBalance());
        Assert.Equal(new Money(1_000), world.MoneySupply.Issued[MoneySupplyTable.Slot]);
    }

    /// <summary>
    /// <b>A reload carrying the same balance mints nothing</b>, which is what makes the refusal a
    /// guard on the value rather than on the table.
    /// </summary>
    [Fact]
    public void A_reload_carrying_the_same_opening_balance_mints_nothing()
    {
        string file = $"{Named}\n\n[treasury]\nopening_balance = 1000\n";
        var world = new World(1_000, Parse(file));

        world.Adopt(Parse(file), contentHash: 2, Ticks.Zero, WorldKey.FromSeed(7));

        Assert.Equal(new Money(1_000), world.TreasuryBalance());
        Assert.Equal(new Money(1_000), world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>The door refuses a negative amount and a world with no money</b>, which is
    /// <c>World.Endow</c>'s pair of refusals on the treasury.
    /// </summary>
    /// <remarks>
    /// The loader refuses both files first, so what reaches these throws is a hand-built Ruleset —
    /// and the door is public, so a fixture author is the caller the message is written for.
    /// </remarks>
    [Fact]
    public void The_treasury_door_refuses_a_negative_amount_and_a_world_with_no_money()
    {
        var world = new World(1_000, Parse(Named));

        Assert.Throws<ArgumentOutOfRangeException>(() => world.EndowTreasury(new Money(-1)));

        var moneyless = new World(1_000);

        Assert.Throws<InvalidOperationException>(() => moneyless.EndowTreasury(new Money(1)));
    }

    /// <summary>A Ruleset written in the test, loaded as the shell loads one.</summary>
    private static Ruleset Parse(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
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
