using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 10 task 7 — the one walk over the Bins that the two invariants and the Census share.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is <see cref="Money_in_an_unnamed_place_lands_in_the_residue"/></b>, and
/// it is the only one here that could not be written against the report. Every other assertion holds
/// a decomposition that sums; this one holds that the decomposition <em>says so when it stops
/// decomposing</em>. ⚠ It also makes <c>Elsewhere</c> an exercised counter rather than a column that
/// reads zero because nothing writes it — milestone 6 task 7's finding on <c>TripFate.Stranded</c>,
/// which is the distinction a Census cannot make on its own.
/// </para>
/// <para>
/// <b>The walk is keyed on the Resource being conserved and never on the owner</b>
/// (<c>adr/0114</c>), so a Bin owned by something this ledger does not name is <em>counted</em> and
/// then falls out as the residue. Missing it entirely would be the failure: the money would simply
/// not appear in any row, and a reader cannot see a row that is not printed.
/// </para>
/// </remarks>
public sealed class MoneyLedgerTests
{
    private const int Citizens = 500;

    /// <summary>The endowment reaches the Households and nowhere else.</summary>
    [Fact]
    public void A_founded_city_holds_all_of_its_money_in_households()
    {
        World world = City();
        MoneyLedger ledger = MoneyLedger.Of(world);

        Assert.True(ledger.Representable);
        Assert.True(ledger.Total > 0, "the populator endowed nobody.");

        Assert.Equal(ledger.Total, ledger.Households);
        Assert.Equal(0, ledger.Treasury);
        Assert.Equal(0, ledger.Businesses);
        Assert.Equal(0, ledger.Elsewhere);
    }

    /// <summary>
    /// A conserved Bin owned by a Building is counted, and shows up as the residue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0113</c> says a Building never holds money, so this world is one the build does not
    /// produce</b> — which is the point. The ADR could be reversed, or a fifth owner kind could
    /// arrive, and in either case the money has to appear somewhere in the report on the first
    /// reading. What this pins is that it appears rather than vanishing.
    /// </para>
    /// <para>
    /// ⚠ <b><c>Total</c> moves and the three named holders do not</b>, which is the assertion. A
    /// ledger that quietly filed the Bin under one of the named kinds would keep <c>Elsewhere</c> at
    /// zero and would be wrong in a way no sum could catch.
    /// </para>
    /// </remarks>
    [Fact]
    public void Money_in_an_unnamed_place_lands_in_the_residue()
    {
        World world = City();
        MoneyLedger before = MoneyLedger.Of(world);

        ResourceId money = Money(world);
        Handle<Bin> stray = world.Bins.Create(BinOwnerKind.Building, money, long.MaxValue);

        world.Bins.Move(world.Bins.Rows.Resolve(stray), 1_000);

        MoneyLedger after = MoneyLedger.Of(world);

        Assert.Equal(before.Total + 1_000, after.Total);
        Assert.Equal(before.Households, after.Households);
        Assert.Equal(before.Treasury, after.Treasury);
        Assert.Equal(before.Businesses, after.Businesses);
        Assert.Equal(1_000, after.Elsewhere);
    }

    /// <summary>A Bin holding a Good is not money, however full it is.</summary>
    /// <remarks>
    /// The filter is <c>Ruleset.IsConserved</c>, which is the Money family and nothing else
    /// (<c>adr/0024</c>). A ledger keyed on anything else would count the sundries.
    /// </remarks>
    [Fact]
    public void A_good_is_not_counted()
    {
        World world = City();
        MoneyLedger before = MoneyLedger.Of(world);

        ResourceId good = Good(world);
        Handle<Bin> pantry = world.Bins.Create(BinOwnerKind.Household, good, long.MaxValue);

        world.Bins.Move(world.Bins.Rows.Resolve(pantry), 1_000);

        MoneyLedger after = MoneyLedger.Of(world);

        Assert.Equal(before.Total, after.Total);
        Assert.Equal(before.Households, after.Households);
    }

    /// <summary>
    /// A sum that cannot be represented says so, and says which Bin it ran away on.
    /// </summary>
    /// <remarks>
    /// <b>The figures are then the walk up to that Bin and are not the city's money</b>, which is why
    /// <c>Representable</c> is carried rather than left to a caller to infer from a suspicious total.
    /// <c>Invariant.MoneyIsRepresentable</c> is the reader that reports it; the ledger does not, so
    /// that one bug does not get two names.
    /// </remarks>
    [Fact]
    public void An_unrepresentable_sum_names_the_bin_it_ran_away_on()
    {
        World world = City();

        ResourceId money = Money(world);
        Handle<Bin> vault = world.Bins.Create(BinOwnerKind.Treasury, money, long.MaxValue);
        Handle<Bin> second = world.Bins.Create(BinOwnerKind.Treasury, money, long.MaxValue);

        world.Bins.Move(world.Bins.Rows.Resolve(vault), long.MaxValue);
        world.Bins.Move(world.Bins.Rows.Resolve(second), long.MaxValue);

        MoneyLedger ledger = MoneyLedger.Of(world);

        Assert.False(ledger.Representable);
        Assert.NotEqual(Rows.NoSlot, ledger.Overflowed);
    }

    private static ResourceId Money(World world) => Resource(world, conserved: true);

    private static ResourceId Good(World world) => Resource(world, conserved: false);

    private static ResourceId Resource(World world, bool conserved)
    {
        // Ids run 1..ResourceCount -- Ruleset.ResourceCount's own remark.
        for (ushort id = 1; id <= world.Rules.ResourceCount; id++)
        {
            var candidate = new ResourceId(id);

            if (world.Rules.IsConserved(candidate) == conserved)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"the fixture declares no {(conserved ? "money" : "good")} Resource.");
    }

    private static World City()
    {
        RulesetLoadResult result = RulesetLoader.Parse(Endowed, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(20_260_819);
        var world = new World(Citizens, result.Ruleset!, key);

        SyntheticCity.PopulateInto(world, key, Borough.Core.Quantities.Ticks.Zero);

        return world;
    }

    /// <summary><c>PolicyTests</c>' fixture, minus the circuit: money in Households and nothing moving.</summary>
    private const string Endowed = """
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

        [roads]
        block_tiles = 32
        arterial_count = 0
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000

        [lots]
        lots_per_segment = 5

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000
        """;
}
