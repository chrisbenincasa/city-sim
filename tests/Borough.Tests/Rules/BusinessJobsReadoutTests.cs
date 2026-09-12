using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// A Business reads the posts it declares — <c>plans/0070</c> decision 10.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Without this a service grant cannot be written per job, and a flat one underfunds every
/// school whose tenancy share of the floor declares six posts rather than five.</b> The posts come
/// off the floor (<c>World.DeclaredJobs</c>), so the Readout is the only way a <c>[[policy]]</c> can
/// price a grant against the size of the institution it is funding.
/// </para>
/// <para>
/// ⚠ <b>DECLARED and not filled, which is the line between this and a subsidy.</b> A subsidy pays
/// <c>rate × workers</c>; a transfer whose apply count is read off this pays <c>rate × posts</c>, so
/// a school funded for six teachers draws six teachers' funding whether or not it has hired them.
/// </para>
/// </remarks>
public sealed class BusinessJobsReadoutTests
{
    private static readonly ReadoutId Jobs = new((ushort)Readout.Jobs);

    [Fact]
    public void The_name_a_ruleset_writes_resolves_to_the_declared_readout()
    {
        Assert.True(ReadoutNames.TryResolve("jobs", out ReadoutId id));
        Assert.Equal((ushort)Readout.Jobs, id.Raw);
        Assert.Equal("jobs", ReadoutNames.NameOf(Readout.Jobs));
        Assert.True(Readouts.IsDeclared(id));
    }

    [Fact]
    public void A_business_is_the_only_scope_this_readout_can_be_read_against()
    {
        Assert.True(Readouts.IsReadableAgainst(Jobs, ReadoutScope.Business));

        // A Building's floor is shared between its tenants, so the posts belong to the tenancy; a
        // Household holds no post at all.
        Assert.False(Readouts.IsReadableAgainst(Jobs, ReadoutScope.Building));
        Assert.False(Readouts.IsReadableAgainst(Jobs, ReadoutScope.Household));
    }

    /// <summary>
    /// A silent zero would be a Policy that succeeds doing nothing and re-arms for ever, which is
    /// the non-event <c>02 §4.1</c> bans. The id is refused before either entry point reads a row,
    /// so the Household slot below is immaterial.
    /// </summary>
    [Fact]
    public void The_two_other_entry_points_throw_rather_than_reading_zero()
    {
        (World world, int building, int _) = Tenanted();

        Assert.Throws<InvalidOperationException>(() => Readouts.Read(world, building, Jobs));
        Assert.Throws<InvalidOperationException>(
            () => Readouts.ReadHousehold(world, household: 0, Jobs));
    }

    /// <summary>
    /// The Readout is <c>World.DeclaredJobs</c> and not a second derivation beside it, which is what
    /// keeps a grant priced on the posts the hiring pass actually fills.
    /// </summary>
    [Fact]
    public void A_business_reads_the_posts_its_tenancy_share_of_the_floor_declares()
    {
        (World world, int _, int business) = Tenanted();

        int declared = world.DeclaredJobs(business);

        Assert.True(declared > 0, "the fixture's premises declare no post.");
        Assert.Equal(declared, Readouts.ReadBusiness(world, business, Jobs));
    }

    /// <summary>
    /// A trade in the Unplaced Pool holds no Building, so it has no floor and declares no post.
    /// </summary>
    [Fact]
    public void An_unpremised_business_reads_zero_rather_than_throwing()
    {
        (World world, int _, int _) = Tenanted();

        // The SAME declared trade as the tenanted one, so the zero is the missing floor and not a
        // trade the Ruleset never named.
        Handle<Business> homeless = world.CreateBusiness(default, kind: 1);

        Assert.Equal(
            0, Readouts.ReadBusiness(world, world.Businesses.Rows.Resolve(homeless), Jobs));
    }

    // ---- the grant it prices --------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>The shipped funding Policy pays a school its declared posts times the rate</b>, which
    /// is its full payroll whatever size the school came out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rate and the apply count are read off <c>rulesets/funded.toml</c> rather than written
    /// here, so a retuned grant fails this on the number that moved rather than on a copy of it.
    /// </para>
    /// <para>
    /// ⚠ <b>The sweep is run directly rather than stepped to.</b> <c>WageEngine</c> pays out of the
    /// same till in the same Tick, so a delta measured across a <c>Step</c> would be the grant less
    /// whatever a payday took.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_funded_schools_grant_is_its_declared_posts_times_the_rate()
    {
        (World world, Simulation simulation, WorldKey key) = FundedCity();

        PolicyDefinition funding = Assert.Single(world.Rules.Policies);

        Assert.True(funding.Apply.IsDerived, "the shipped grant is not derived from a Readout.");
        Assert.Equal((ushort)Readout.Jobs, funding.Apply.Derived.Raw);
        Assert.Equal(100, funding.Apply.Percent);

        int lot = FirstVacantLot(world);

        simulation.Step(new TickInput(
            [Command.Service(world.Lots.East[lot], world.Lots.North[lot], School)], 0));

        int trade = TeachingIn(world, world.Lots.BuildingOn(lot));
        int posts = world.DeclaredJobs(trade);

        Assert.True(posts > 0, "the school the city placed declares no post.");

        Handle<Business> school = world.Businesses.Rows.At(trade);
        long before = world.BalanceOf(school).Raw;

        new PolicyEngine(world, key).Sweep(new Ticks(Ticks.PerDay));

        Assert.Equal(before + (posts * funding.Amount), world.BalanceOf(school).Raw);
    }

    // ---- the fixture ----------------------------------------------------------------------------

    /// <summary>
    /// A world whose one trade has premises with a floor, so the posts are a reading rather than a
    /// zero. The kind declares <c>premises</c> and the trade is declared, which is what
    /// <c>TryDeclaredJobs</c> asks before it divides the floor.
    /// </summary>
    private static (World World, int Building, int Business) Tenanted()
    {
        RulesetLoadResult parsed = RulesetLoader.Parse(
            """
            [[resource]]
            name = "money"
            family = "money"

            [[resource]]
            name = "sundries"
            family = "good"

            [[business]]
            name = "teaching"
            wage_per_day = 4096
            pay_period_days = 7
            shift_start_earliest_hour = 6
            shift_start_latest_hour = 10

            [[building]]
            name = "school"
            premises = true
            business = "teaching"
            bins = [ { resource = "money", owner = "business" } ]

            [capacity]
            floor_tiles_per_occupant = 16
            floor_tiles_per_job = 3
            """,
            "jobs.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        var world = new World(1_000, parsed.Ruleset!);

        // A footprint wide enough that the floor holds a tenancy: below
        // `floor_tiles_per_occupant` there is no tenancy to take a share of the floor, and the
        // posts would be zero for a reason this file is not about.
        Handle<Lot> lot = world.Lots.Create(
            new Tiles(1), new Tiles(2), zone: 0, wide: new Tiles(8), deep: new Tiles(4));

        // Both kind sets are 1-based -- `Declares(0)` is false, and a Business of kind 0 is a trade
        // the Ruleset does not name and therefore declares no post.
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Business> business = world.CreateBusiness(building, kind: 1);

        return (
            world,
            world.Buildings.Rows.Resolve(building),
            world.Businesses.Rows.Resolve(business));
    }

    /// <summary>The <c>school</c> kind in <c>funded.toml</c>, which declares <c>dwelling</c> first.</summary>
    private const byte School = 2;

    /// <summary>The <c>teaching</c> trade, in the Business namespace (<c>adr/0141</c>).</summary>
    private const byte Teaching = 1;

    /// <summary>
    /// The shipped funded world, with a treasury that can pay.
    /// </summary>
    /// <remarks>
    /// <b>The shipped file rather than one authored here</b>, because what is under test is the
    /// grant <c>rulesets/funded.toml</c> states. ⚠ <b>It opens with an empty treasury</b>, so an
    /// opening balance is appended where the file states none: the claim is the SIZE of the grant,
    /// and a dry treasury pays nothing at all.
    /// </remarks>
    private static (World World, Simulation Simulation, WorldKey Key) FundedCity()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "funded.toml"));

        // The header LINE and not the text: the file's own prose names `[treasury] opening_balance`
        // in two comments, so a substring check finds a table that is not declared.
        bool founded = toml.Split('\n').Any(
            line => string.Equals(line.Trim(), "[treasury]", StringComparison.Ordinal));

        if (!founded)
        {
            toml += "\n[treasury]\nopening_balance = 1000000\n";
        }

        RulesetLoadResult parsed = RulesetLoader.Parse(toml, "funded.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        var key = WorldKey.FromSeed(0x8000_0020UL);
        var world = new World(500, parsed.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation, key);
    }

    private static int FirstVacantLot(World world)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot))
            {
                return slot;
            }
        }

        Assert.Fail("the generated city left no vacant Lot to place a school on.");
        return -1;
    }

    /// <summary>The <c>teaching</c> Business standing in <paramref name="building"/>.</summary>
    private static int TeachingIn(World world, int building)
    {
        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.Businesses.Kind[slot] == Teaching
                && world.Buildings.Rows.TryResolve(world.Businesses.Building[slot], out int premises)
                && premises == building)
            {
                return slot;
            }
        }

        Assert.Fail("the school the city placed holds no teaching Business.");
        return -1;
    }
}
