using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>01 §2</c>'s third verb and <c>adr/0032</c>'s Attended Service — <b>the first Need fed by a
/// journey rather than by a Rule firing.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Two mechanisms are under test and they fail in opposite directions.</b> The verb is a
/// placement that must refuse almost everything: <c>01 §5</c> makes it the design's <em>one</em>
/// placement exception, so a kind that is not a service Building must not reach the ground through
/// it. The engine is an accumulator that must be able to fail: a school nobody can reach has to cost
/// something, or the mechanism is present, correct and invisible.
/// </para>
/// <para>
/// 🔴 <b>The load-bearing assertion in this class is
/// <see cref="A_city_that_has_built_no_school_fails_every_occasion"/></b>, because the engine did the
/// opposite for its first spelling. It returned early where no school stood — so a player who never
/// placed one got no degrade at all, and ***the mechanism rewarded the player for not using it.***
/// A Ruleset declaring no schools has no mechanism; a city that has built none has failed, and only
/// the Ruleset may gate the pass.
/// </para>
/// </remarks>
public sealed class ServiceTests
{
    // Small: every assertion here is a shape rather than a rate, and the two that run a city need
    // only enough Households for one occasion to be distinguishable from none.
    private const int Citizens = 400;
    private const int Seed = 20_260_830;

    // ---- the verb -------------------------------------------------------------------------------

    /// <summary>A school reaches the ground through the verb, and stands on the named Tile.</summary>
    [Fact]
    public void The_verb_raises_a_service_building_on_the_named_tile()
    {
        (World world, Simulation simulation) = City(Schooled);

        int before = world.Buildings.Rows.LiveCount;
        int lot = FirstVacantLot(world);

        Place(simulation, world, lot);

        Assert.Equal(before + 1, world.Buildings.Rows.LiveCount);
        Assert.False(world.Lots.IsVacant(lot));
        Assert.Equal(
            Need.Education,
            world.Rules.ServedBy(world.Buildings.Kind[world.Lots.BuildingOn(lot)]));
    }

    /// <summary>
    /// 🔴 <b>A kind that serves nothing is refused, which is what keeps the exception an
    /// exception.</b>
    /// </summary>
    /// <remarks>
    /// Without this the verb is a general <em>place any Building anywhere</em> command, and pillar 3
    /// — govern-don't-place — is gone by the back door rather than by a decision.
    /// </remarks>
    [Fact]
    public void The_verb_refuses_a_kind_that_serves_nothing()
    {
        (World world, Simulation simulation) = City(Schooled);

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
            () => Place(simulation, world, FirstVacantLot(world), kind: Dwelling));

        Assert.Contains("placement exception", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>And a kind this Ruleset never declared.</summary>
    [Fact]
    public void The_verb_refuses_an_undeclared_kind()
    {
        (World world, Simulation simulation) = City(Schooled);

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
            () => Place(simulation, world, FirstVacantLot(world), kind: 200));

        Assert.Contains("does not declare", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Tile with no vacant Lot is refused rather than resolved to a neighbour.
    /// </summary>
    /// <remarks>
    /// <b><c>Demolish</c>'s exactness, and for its reason</b>: <c>lots_per_segment</c> is five, so
    /// <em>the Lot in this block</em> names up to twenty of them — and a school landing on somebody
    /// else's plot because the click resolved to the first is worse than a refusal.
    /// </remarks>
    [Fact]
    public void The_verb_refuses_a_tile_with_no_vacant_lot()
    {
        (World world, Simulation simulation) = City(Schooled);

        Command command = Command.Service(new Tiles(9_000), new Tiles(9_000), School);

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
            () => simulation.Step(new TickInput([command], 0)));

        Assert.Contains("no vacant Lot", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>The payload fits the four fields, which is <c>adr/0118</c>'s own test.</summary>
    /// <remarks>
    /// <b>The catchment <c>01 §2</c> names is not in it</b>, because <c>adr/0032</c> demoted coverage
    /// from mechanism to overlay — so the field that would not have fitted turned out not to be a
    /// payload at all, and <c>InputLogCodec.Version</c> does not move.
    /// </remarks>
    [Fact]
    public void The_payload_is_a_place_and_a_kind()
    {
        Command command = Command.Service(new Tiles(64), new Tiles(96), School);

        Assert.Equal(CommandKind.Service, command.Kind);
        Assert.Equal(64, command.East.Raw);
        Assert.Equal(96, command.North.Raw);
        Assert.Equal(School, (byte)command.Zone);
    }

    // ---- the engine -----------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>A city that declares schools and has built none fails every occasion.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the assertion the first spelling of <c>ServiceEngine</c> failed</b>, and it failed
    /// silently: with no school standing the pass returned before it looked at a Household, so
    /// Education stayed pinned at zero in exactly the city the mechanism exists to describe.
    /// ***A Ruleset gates the pass; the state of the city never does.***
    /// </remarks>
    [Fact]
    public void A_city_that_has_built_no_school_fails_every_occasion()
    {
        (World world, Simulation simulation) = City(Schooled);

        Step(simulation, Ticks.PerDay * 3);

        Assert.True(
            Deepest(world) < 0,
            "no Household lost any Education in a city with children and no school.");
    }

    /// <summary>
    /// And the degrade is the Ruleset's rate per Day, not a step per anything else.
    /// </summary>
    /// <remarks>
    /// <b>Stated as a bound rather than an equality</b>, because a Household's first occasion is the
    /// first Day boundary after its children arrive and this class does not fix when that is. What it
    /// pins is that three Days cannot cost more than three Days' worth — an accumulator moving per
    /// Tick, or twice per pass, breaks this and nothing else in the class would notice.
    /// </remarks>
    [Fact]
    public void The_degrade_is_the_rate_per_day()
    {
        (World world, Simulation simulation) = City(Schooled);

        Step(simulation, Ticks.PerDay * 3);

        Assert.True(
            Deepest(world) >= -3 * EducationDegrade,
            $"three Days cost more than three Days' worth: {Deepest(world)}.");
    }

    /// <summary>
    /// 🔴 <b>A Household with no child has no occasion, so nothing moves.</b>
    /// </summary>
    /// <remarks>
    /// <b>Not the same statement as <em>it is well served</em>, and the column cannot tell them
    /// apart.</b> Degrading a childless Household would make Education a reading of the city's
    /// demographics wearing the name of its schools. ⚠ <b>The world here declares no
    /// <c>[[life_stage]]</c></b>, which is the strongest form of the case: every Citizen carries
    /// <c>Age == 0</c> because nothing ever wrote the column, and a pass that read that as
    /// <em>child</em> would report a universal schooling crisis in nineteen shipped worlds.
    /// </remarks>
    [Fact]
    public void A_world_without_demographics_schools_nobody()
    {
        (World world, Simulation simulation) = City(SchooledWithoutStages);

        Step(simulation, Ticks.PerDay * 3);

        Assert.Equal(0, Deepest(world));
    }

    /// <summary>A Ruleset that declares no service kind runs no pass at all.</summary>
    [Fact]
    public void A_ruleset_with_no_service_kind_moves_nothing()
    {
        (World world, Simulation simulation) = City(Unschooled);

        Step(simulation, Ticks.PerDay * 3);

        Assert.Equal(0, Deepest(world));
    }

    /// <summary>
    /// And a school within reach stops the fall, which is what makes every assertion above
    /// non-vacuous.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Without this the whole class passes against an engine that only ever degrades</b>, and
    /// the recover rate would be a Ruleset key nothing read.
    /// </remarks>
    [Fact]
    public void A_school_within_reach_stops_the_fall()
    {
        (World unschooled, Simulation withoutOne) = City(Schooled);
        (World schooled, Simulation withOne) = City(Schooled);

        Place(withOne, schooled, FirstVacantLot(schooled));

        Step(withoutOne, Ticks.PerDay * 3);
        Step(withOne, Ticks.PerDay * 3);

        Assert.True(Deepest(unschooled) < 0, "the unschooled city did not decline.");
        Assert.Equal(0, Deepest(schooled));
    }

    // ---- the loader -----------------------------------------------------------------------------

    /// <summary>
    /// <c>[[resource]] need = "education"</c> is still refused, and now it says where to go.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The refusal survived and its reason did not.</b> It used to end <em>"its degradation
    /// rule is owed and DELIBERATELY UNDESIGNED"</em>; <c>docs/deferred.md</c> named the trigger that
    /// would end that, and this is it. ***A refusal that points somewhere is a different sentence
    /// from one that only says no.***
    /// </remarks>
    [Fact]
    public void A_resource_still_cannot_feed_an_attended_need()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            Unschooled.Replace(
                "name = \"sundries\"",
                "name = \"sundries\"\n        need = \"education\"",
                StringComparison.Ordinal),
            "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("serves", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>A Dispatched Service is refused by name, and told which mode it is in.</summary>
    /// <remarks>
    /// <b>Named rather than generic</b>, because <c>adr/0070</c> turns on the difference: a generic
    /// <em>not a Need</em> reads as <em>not yet</em>, and nobody may reason from that.
    /// </remarks>
    [Fact]
    public void A_dispatched_service_is_refused_by_name()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            Schooled.Replace("serves = \"education\"", "serves = \"fire\"", StringComparison.Ordinal),
            "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("DISPATCHED", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// An attendance rate with no kind to reach it is refused, and so is a kind with no rate.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0130</c>'s rule for <c>gives_up_after_days</c>, one table along.</b> Both directions
    /// are a Ruleset that loads clean and does nothing: a rate nothing can reach reads on the page as
    /// a mechanism the world has, and a school in a world with no rate is a Building that stands
    /// while nothing moves.
    /// </remarks>
    [Fact]
    public void The_attendance_rates_and_the_serving_kind_are_required_of_each_other()
    {
        RulesetLoadResult orphanRate = RulesetLoader.Parse(
            Staged + LifeStages + AttendedNeeds, "test.toml");

        Assert.False(orphanRate.Ok);
        Assert.Contains("no [[building]] declares serves", orphanRate.Describe(),
            StringComparison.Ordinal);

        RulesetLoadResult orphanKind = RulesetLoader.Parse(
            Staged + LifeStages + SchoolKind + BoughtNeeds, "test.toml");

        Assert.False(orphanKind.Ok);
        Assert.Contains("education_degrade", orphanKind.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The loader's own walk over the kinds and <c>Ruleset.ServesAny</c> agree.
    /// </summary>
    /// <remarks>
    /// <b>Two walks over one array, one Ruleset construction step apart</b> — the loader cannot ask a
    /// <c>Ruleset</c> that does not exist yet. A disagreement would refuse a legal file or admit an
    /// unreachable rate, and nothing else asks.
    /// </remarks>
    [Fact]
    public void The_loader_and_the_ruleset_agree_on_which_needs_are_served()
    {
        Ruleset rules = Parse(Schooled);

        Assert.True(rules.ServesAny(Need.Education));
        Assert.False(rules.ServesAny(Need.Health));
        Assert.False(rules.ServesAny(Need.None));
        Assert.Equal(Need.Education, rules.ServedBy(School));
        Assert.Equal(Need.None, rules.ServedBy(Dwelling));
    }

    /// <summary>
    /// <c>rulesets/schooled.toml</c> loads, and declares what its header says it declares.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Nothing else in the suite opens a shipped Ruleset</b>, so until this one the
    /// demonstration files were checked only by somebody running one.
    /// </remarks>
    [Fact]
    public void The_shipped_school_ruleset_loads_and_declares_a_school()
    {
        string path = Path.Combine(RepoRoot(), "rulesets", "schooled.toml");

        RulesetLoadResult result = RulesetLoader.Parse(File.ReadAllText(path), path);

        Assert.True(result.Ok, result.Describe());

        Ruleset rules = result.Ruleset!;

        Assert.True(rules.ServesAny(Need.Education));
        Assert.False(rules.ServesAny(Need.Health));
        Assert.True(rules.DeclaresLifeStages);
        Assert.True(rules.Needs.Attends);
        Assert.True(rules.Needs.DegradeOf(Need.Education) > 0);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "rulesets")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("no rulesets/ directory above the test assembly.");
        return string.Empty;
    }

    // ---- fixtures -------------------------------------------------------------------------------

    private const byte Dwelling = 1;
    private const byte School = 2;
    private const int EducationDegrade = 2;

    private static int Deepest(World world)
    {
        int deepest = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Households.Education[slot] < deepest)
            {
                deepest = world.Households.Education[slot];
            }
        }

        return deepest;
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

    private static void Place(Simulation simulation, World world, int lot, byte kind = School)
    {
        Command command = Command.Service(world.Lots.East[lot], world.Lots.North[lot], kind);

        simulation.Step(new TickInput([command], 0));
    }

    private static void Step(Simulation simulation, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }
    }

    private static Ruleset Parse(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static (World World, Simulation Simulation) City(string toml)
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, Parse(toml), key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    /// <summary>The <c>[[building]]</c> that makes a kind a service Building.</summary>
    private const string SchoolKind = """

        [[building]]
        name = "school"
        serves = "education"
        """;

    /// <summary>The <c>[needs]</c> table with the attended pair in it.</summary>
    private const string AttendedNeeds = """

        [needs]
        sustenance_degrade   = 1
        sustenance_recover   = 1
        satisfaction_degrade = 1
        satisfaction_recover = 1
        education_degrade    = 2
        education_recover    = 2
        floor = -1000
        """;

    /// <summary>The same table with the attended pair taken out.</summary>
    private const string BoughtNeeds = """

        [needs]
        sustenance_degrade   = 1
        sustenance_recover   = 1
        satisfaction_degrade = 1
        satisfaction_recover = 1
        floor = -1000
        """;

    /// <summary>
    /// A world with demographics, a school kind and an education rate. <b>No school stands.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Its <c>young</c> stage is one Day long</b>, where <c>rulesets/aged.toml</c>'s is 24–32.
    /// A test that had to run a whole generation to see one child would be a four-minute assertion,
    /// and <c>KindDefinition.CondemnAfterTicks</c>' remark establishes that a fixture may hold any
    /// duration a shipped file may not.
    /// </remarks>
    private const string Schooled = Staged + LifeStages + SchoolKind + AttendedNeeds;

    /// <summary><see cref="Schooled"/>'s world with the demographics taken out.</summary>
    private const string SchooledWithoutStages = Staged + SchoolKind + AttendedNeeds;

    /// <summary>Demographics and no service kind at all.</summary>
    private const string Unschooled = Staged + LifeStages + BoughtNeeds;

    private const string Staged = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 4
        bins = [ { resource = "sundries", capacity = 48 } ]

        [[zone_rule]]
        name          = "housing"
        kind          = "dwelling"
        zone          = 0
        interval      = 32
        revisit_ticks = 2048

        [placement]
        interval      = 32
        revisit_ticks = 1024
        candidates    = 3

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

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000
        """;

    /// <summary><c>adr/0011</c>'s chain, compressed so one child arrives on the second Day.</summary>
    private const string LifeStages = """

        [[life_stage]]
        name          = "young"
        duration_days = 1
        spread_days   = 0
        next          = "family"
        childless     = "childless"
        children_min  = 1
        children_max  = 2
        adult_age_min_days = 1
        adult_age_max_days = 160

        [[life_stage]]
        name          = "family"
        duration_days = 100
        spread_days   = 0

        [[life_stage]]
        name          = "childless"
        duration_days = 100
        spread_days   = 0
        """;
}
