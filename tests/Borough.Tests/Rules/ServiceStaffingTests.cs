using System.Globalization;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <b>A school with no teachers teaches nobody</b> — <c>adr/0026</c>'s <em>understaffing degrades
/// service quality proportionally</em>, built.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Before this, cutting a school's funding changed nothing a player could see.</b>
/// <c>World.DeclaredPlaces</c> divided floor area by <c>[capacity] floor_tiles_per_place</c> and
/// asked nothing about staff; <c>WageEngine.Bankrupt</c> leaves the premises standing rather than
/// abandoning them, and <c>ServiceEngine.Gather</c> skips only an abandoned Building. So a school
/// whose till had been drained and whose teachers had been dismissed taught the same children on
/// the same Day. ***The demonstration the funding circuit is named after produced nothing to
/// watch.***
/// </para>
/// <para>
/// ⚠ <b>The staff are read off the floor and never off a catchment.</b> <c>adr/0026</c>'s other
/// half — a school needing teachers in proportion to the children near it — is unbuilt, and nothing
/// here derives a wanted headcount from demand. The denominator is
/// <c>[capacity] floor_tiles_per_job</c> over the tenancy the trade holds, which the Ruleset states
/// and a Building's ground decides.
/// </para>
/// <para>
/// <b>The fixture is <c>EmploymentTests</c>'s</b> — a hand-carved Lot rather than a generated city —
/// because every assertion here is an exact place count and ***a test that had to predict a
/// subdivider's parcel would be asserting on the subdivider.*** <see cref="Ground"/> over
/// <see cref="PerTenancy"/> gives the school two tenancies, so <c>World.Fit</c> instantiates the
/// declared trade with room left beside it; <see cref="JobRate"/> gives that trade four posts and
/// <see cref="PlaceRate"/> gives the Building twelve places.
/// </para>
/// </remarks>
public sealed class ServiceStaffingTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x8000_0032UL);

    private const byte Dwelling = 1;
    private const byte School = 2;

    /// <summary>The trade the school kind comes with, in the Business namespace (<c>adr/0141</c>).</summary>
    private const byte Tuition = 1;

    /// <summary>A trade no Building declares, so a Business of it can only ever be a tenant.</summary>
    private const byte Sundries = 2;

    /// <summary>How much floor the school stands on, in Tiles.</summary>
    private const int Ground = 48;

    /// <summary>
    /// One tenancy's share of it — <b>half, so the school holds two</b> and the declared trade takes
    /// one with room beside it.
    /// </summary>
    /// <remarks>
    /// <c>World.Fit</c> refuses to instantiate a kind's trade where the ground leaves room for one
    /// tenant only, so a school of one tenancy would be a school with no teachers for the fixture's
    /// reason rather than for the city's.
    /// </remarks>
    private const int PerTenancy = Ground / 2;

    /// <summary>Four posts on the trade — <c>PerTenancy / JobRate</c>, so halving the staff divides.</summary>
    private const int JobRate = 6;

    private const int Jobs = PerTenancy / JobRate;

    /// <summary>Twelve places on the Building — <c>Ground / PlaceRate</c>, a multiple of the posts.</summary>
    private const int PlaceRate = 4;

    private const int Places = Ground / PlaceRate;

    // ---- the scaling ----------------------------------------------------------------------------

    /// <summary>
    /// <b>A fully staffed school keeps every place its floor holds.</b>
    /// </summary>
    /// <remarks>
    /// The control for every other test here. Without it a failure elsewhere reads as <em>staffing
    /// scales</em> when it might be <em>the fixture never had the places</em>.
    /// </remarks>
    [Fact]
    public void A_fully_staffed_school_keeps_every_place_its_floor_holds()
    {
        (World world, int school, int trade) = City(Staffed);

        Hire(world, trade, Jobs);

        Assert.Equal(Ground, world.FloorTilesOf(school));
        Assert.Equal(Jobs, world.DeclaredJobs(trade));
        Assert.Equal(Places, world.DeclaredPlaces(school));
    }

    /// <summary>
    /// 🔴 <b>Half the teachers teach half the places</b>, which is the ADR's word
    /// <em>proportionally</em>.
    /// </summary>
    /// <remarks>
    /// The one place count in this class that could not be produced by a boolean — a school that was
    /// open or shut would pass every other test here and fail this one.
    /// </remarks>
    [Fact]
    public void Half_the_teachers_teach_half_the_places()
    {
        (World world, int school, int trade) = City(Staffed);

        Hire(world, trade, Jobs / 2);

        Assert.Equal(Places / 2, world.DeclaredPlaces(school));
    }

    /// <summary>And one teacher in four teaches a quarter of them.</summary>
    [Fact]
    public void One_teacher_in_four_teaches_a_quarter_of_the_places()
    {
        (World world, int school, int trade) = City(Staffed);

        Hire(world, trade, 1);

        Assert.Equal(Places / Jobs, world.DeclaredPlaces(school));
    }

    /// <summary>
    /// <b>A staffless school turns everybody away</b>, and its Building is still standing.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>HasServicePlace</c> is the assertion that matters to a player</b>, because that is
    /// what <c>ServiceEngine.Reach</c> asks. The place count is the quantity behind it and the two
    /// are read together here on purpose.
    /// </remarks>
    [Fact]
    public void A_school_that_hired_nobody_teaches_nobody()
    {
        (World world, int school, _) = City(Staffed);

        Assert.Equal(0, world.DeclaredPlaces(school));
        Assert.False(world.HasServicePlace(school, day: 0));
        Assert.True(world.Buildings.Rows.IsLive(school), "the school stopped standing.");
    }

    /// <summary>
    /// 🔴 <b>A school whose trade folded teaches nobody, and that is what this row exists for.</b>
    /// </summary>
    /// <remarks>
    /// <c>WageEngine.Bankrupt</c> winds a Business up and leaves its premises standing, so the lost
    /// trade is the only trace a defunded school carries. ***Cut the funding, drain the till, wind
    /// the trade up, and the places go to zero where a player can see them.***
    /// </remarks>
    [Fact]
    public void A_school_whose_trade_folded_teaches_nobody()
    {
        (World world, int school, int trade) = City(Staffed);

        Hire(world, trade, Jobs);

        Assert.Equal(Places, world.DeclaredPlaces(school));

        world.DestroyBusiness(world.Businesses.Rows.At(trade));

        Assert.Equal(0, world.DeclaredPlaces(school));
        Assert.True(world.Buildings.Rows.IsLive(school), "the school stopped standing.");
    }

    /// <summary>
    /// <b>A shop that moved into a school is not its teachers.</b>
    /// </summary>
    /// <remarks>
    /// <c>premises</c> is a permission and <c>adr/0147</c> counts one tenancy ceiling over both kinds
    /// of tenant, so a trade the school never declared may stand in it — and a staffed one must not
    /// re-open a school whose own trade has gone. The match is
    /// <c>BusinessTable.Origin</c> naming this Building, which is <c>adr/0148</c>'s pairing and not a
    /// flag.
    /// </remarks>
    [Fact]
    public void A_shop_that_moved_into_a_school_is_not_its_teachers()
    {
        (World world, int school, int trade) = City(Staffed);

        world.DestroyBusiness(world.Businesses.Rows.At(trade));

        Handle<Business> tenant = world.CreateBusiness(world.Buildings.Rows.At(school), Sundries);

        Hire(world, world.Businesses.Rows.Resolve(tenant), Jobs);

        Assert.Equal(0, world.DeclaredPlaces(school));
    }

    // ---- what the scaling does not touch --------------------------------------------------------

    /// <summary>
    /// 🔴 <b>A service kind declaring NO trade keeps its full places</b>, and every shipped school
    /// world is that world.
    /// </summary>
    /// <remarks>
    /// <c>rulesets/schooled.toml</c>'s <c>school</c> declares no <c>business</c>, so a scaling that
    /// applied to it would have changed the meaning of every school reading the corpus holds.
    /// ***Staffing can only scale a service whose staff the Ruleset states.***
    /// </remarks>
    [Fact]
    public void A_service_kind_declaring_no_trade_keeps_its_full_places()
    {
        (World world, int school, int trade) = City(Unstaffed);

        Assert.Equal(Rows.NoSlot, trade);
        Assert.Equal(Places, world.DeclaredPlaces(school));
        Assert.True(world.HasServicePlace(school, day: 0));
    }

    /// <summary>
    /// <b>A world with no employment ceiling scales nothing</b>, because there is no ratio to read.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The zero-jobs guard, and it answers the floor's count rather than zero.</b>
    /// <c>World.TryDeclaredJobs</c> answers zero only where a <c>[capacity]</c> rate the employment
    /// ceiling needs is absent — which is a world nobody can be short of staff in, not a school that
    /// lost its own. Scaling to zero there would shut every school in the file.
    /// </remarks>
    [Fact]
    public void A_world_with_no_employment_ceiling_scales_nothing()
    {
        (World world, int school, int trade) = City(Unpaid);

        Assert.Equal(0, world.DeclaredJobs(trade));
        Assert.Equal(Places, world.DeclaredPlaces(school));
        Assert.True(world.HasServicePlace(school, day: 0));
    }

    // ---- the fixture ----------------------------------------------------------------------------

    /// <summary>Employs <paramref name="workers"/> Citizens in <paramref name="tradeSlot"/>.</summary>
    /// <remarks>
    /// <b>Hired by hand rather than by the assignment pass</b>, which would make the staff a function
    /// of a route and a Commute Budget — two things no assertion here is about. The Citizens live in
    /// the fixture's dwelling because <c>CitizenTable.HouseholdOf</c> is not severable.
    /// </remarks>
    private static void Hire(World world, int tradeSlot, int workers)
    {
        Handle<Business> employer = world.Businesses.Rows.At(tradeSlot);
        Handle<Household> household = world.Households.Rows.At(0);

        for (int i = 0; i < workers; i++)
        {
            world.Employ(world.CreateCitizen(household), employer, Ticks.Zero);
        }

        Assert.Equal(workers, world.Workers.Length(tradeSlot));
    }

    /// <summary>
    /// A dwelling and a school, side by side — the school's own trade, or
    /// <see cref="Rows.NoSlot"/> where its kind declares none.
    /// </summary>
    private static (World World, int School, int Trade) City(string toml)
    {
        var world = new World(1_000, Load(toml), Key);

        Handle<Lot> home = world.Lots.Create(
            new Tiles(0), new Tiles(0), zone: 1, wide: new Tiles(Ground), deep: new Tiles(1));
        Handle<Lot> site = world.Lots.Create(
            new Tiles(64), new Tiles(0), zone: 1, wide: new Tiles(Ground), deep: new Tiles(1));

        Handle<Building> dwelling = world.CreateBuilding(home, Dwelling, Ticks.Zero, Key);
        world.CreateHousehold(dwelling, lifeStage: 0);

        Handle<Building> school = world.CreateBuilding(site, School, Ticks.Zero, Key);
        int schoolSlot = world.Buildings.Rows.Resolve(school);

        return (world, schoolSlot, OwnTrade(world, schoolSlot));
    }

    /// <summary>The Business the school instantiated itself, by <c>BusinessTable.Origin</c>.</summary>
    private static int OwnTrade(World world, int schoolSlot)
    {
        foreach (int business in world.BuildingBusinesses.Walk(schoolSlot))
        {
            if (world.Buildings.Rows.TryResolve(world.Businesses.Origin[business], out int origin)
                && origin == schoolSlot)
            {
                return business;
            }
        }

        return Rows.NoSlot;
    }

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>What a trade needs stated of it before anybody may hold a post in it.</summary>
    private const string Posts =
        "shift_start_earliest_hour = 6\nshift_start_latest_hour = 10";

    private static readonly string Template = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        houses = true
        premises = true

        [[building]]
        name = "school"
        serves = "education"
        premises = true
        TRADE

        [[business]]
        name = "tuition"
        POSTS

        [[business]]
        name = "sundries"
        POSTS

        [needs]
        sustenance_degrade   = 1
        sustenance_recover   = 1
        satisfaction_degrade = 1
        satisfaction_recover = 1
        education_degrade    = 2
        education_recover    = 2
        floor = -1000

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [capacity]
        floor_tiles_per_occupant = TENANCY
        JOBRATE
        floor_tiles_per_place    = PLACE
        """
        .Replace("TENANCY", PerTenancy.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
        .Replace("PLACE", PlaceRate.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    /// <summary>A school whose kind comes with a teaching trade.</summary>
    private static readonly string Staffed = Fixture("business = \"tuition\"", Posts, JobRate);

    /// <summary>The same school with no trade at all — every shipped school world.</summary>
    private static readonly string Unstaffed = Fixture("", Posts, JobRate);

    /// <summary>
    /// The same school with its trade and <b>no <c>floor_tiles_per_job</c></b>, so no trade in the
    /// world has a post to be short of.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The Shift-start band goes with the key</b>: <c>adr/0101</c>'s refusal is two-way, and a
    /// band in a world that employs nobody is refused at the parse site.
    /// </remarks>
    private static readonly string Unpaid = Fixture("business = \"tuition\"", "", 0);

    /// <summary>
    /// The fixture with its three variables filled — whether the school kind declares a trade,
    /// whether a trade states its Shift band, and the employment rate the world states.
    /// </summary>
    private static string Fixture(string trade, string posts, int jobRate) => Template
        .Replace("TRADE", trade, StringComparison.Ordinal)
        .Replace("POSTS", posts, StringComparison.Ordinal)
        .Replace(
            "JOBRATE",
            jobRate > 0
                ? "floor_tiles_per_job      = "
                    + jobRate.ToString(CultureInfo.InvariantCulture)
                : "",
            StringComparison.Ordinal);
}
