using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Determinism;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0071</c>: a childhood turns into a Skill Tier, a university is the only route past Tier 2,
/// and the tier changes what a Citizen is paid and hired for.
/// </summary>
/// <remarks>
/// <para>
/// <b>No test of any of this existed before this file.</b> The mechanism landed across
/// <c>SchoolingEngine</c>, <c>EmploymentEngine.Progress</c>, <c>WorkSchedule.Graded</c> and three new
/// <c>Ruleset</c> records with nothing in the suite watching any of it.
/// </para>
/// <para>
/// <b>Three fixture families, chosen per claim rather than one fixture for the whole file.</b> The
/// pure blend on <see cref="SchoolingRuleset"/> needs no <see cref="World"/> at all. Attendance,
/// experience, the credential wall and the wage grading are hand-built worlds in the
/// <c>EmploymentTests</c> style — a Citizen and a Business with nothing else standing, because what
/// is under test is arithmetic and a refusal rather than geography. Enrolment needs a real, routable
/// city, so those tests run on <see cref="SyntheticCity"/> and place the school themselves, on
/// <c>ServiceCapacityTests</c>' precedent.
/// </para>
/// </remarks>
public sealed class SchoolingTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x5C40_0071UL);

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    // =================================================================================================
    // The blend, and the two tier cuts. Pure SchoolingRuleset -- no World.
    // =================================================================================================

    /// <summary>
    /// The score is 70% attended Days and 30% Need depth, both read at their own extremes, and
    /// secondary attendance below the primary gate counts for nothing at all.
    /// </summary>
    /// <remarks>
    /// <c>adr/0104</c>'s blend, checked at the boundaries a designer actually reasons from rather than
    /// on an arbitrary midpoint: a childhood that got everything, one that got nothing, and one that
    /// attended secondary school without ever having cleared primary.
    /// </remarks>
    [Fact]
    public void Score_blends_attendance_and_need_depth_and_gates_secondary_on_the_primary_floor()
    {
        var schooling = new SchoolingRuleset(
            AttendanceWeightPercent: 70,
            FullAttendanceDays: 40,
            PrimaryGateDays: 20,
            Tier2Score: 50,
            UniversityDays: 16);

        const int floor = -1000;

        // Full attendance and a Need that never fell from the ideal: both terms read 100.
        Assert.Equal(100, schooling.Score(primaryDays: 20, secondaryDays: 40, depth: 0, floor));

        // No attendance at all and a Need collapsed to the floor: both terms read 0.
        Assert.Equal(0, schooling.Score(primaryDays: 20, secondaryDays: 0, depth: floor, floor));

        // Short of the primary gate: 1,000 secondary Days -- far past full attendance -- count for
        // NOTHING, so only the 30-point depth term survives.
        Assert.Equal(30, schooling.Score(primaryDays: 19, secondaryDays: 1_000, depth: 0, floor));
    }

    /// <summary>
    /// <see cref="SchoolingRuleset.TierOf"/> never returns below the floor, promotes exactly at the
    /// cut, and a Ruleset with no <c>[schooling]</c> table leaves every Citizen at the floor rather
    /// than at a tier that does not exist.
    /// </summary>
    [Fact]
    public void TierOf_floors_below_the_cut_promotes_at_it_and_never_goes_below_the_floor_with_no_table()
    {
        var schooling = new SchoolingRuleset(70, 40, 20, Tier2Score: 50, UniversityDays: 16);

        Assert.Equal(SchoolingRuleset.FloorTier, schooling.TierOf(49));
        Assert.Equal(2, schooling.TierOf(50));

        // Runs is false, so even the highest possible score forms nobody above the floor -- there is
        // no tier 0, and an unscored world is stuck at tier 1 rather than below it.
        Assert.Equal(SchoolingRuleset.FloorTier, SchoolingRuleset.None.TierOf(100));
    }

    // =================================================================================================
    // Attendance. A hand-built world: one Citizen, one primary, one secondary.
    // =================================================================================================

    private const byte AttendDwelling = 1;
    private const byte AttendPrimary = 2;
    private const byte AttendSecondary = 3;

    private const string AttendanceRuleset = """
        [[building]]
        name = "dwelling"
        houses = true

        [[building]]
        name   = "primary"
        serves = "education"
        level  = 1

        [[building]]
        name   = "secondary"
        serves = "education"
        level  = 2
        """;

    private static (World World, int Citizen, int Primary, int Secondary) AttendanceCity()
    {
        var world = new World(4, Load(AttendanceRuleset), Key);

        Handle<Lot> home = world.Lots.Create(
            new Tiles(0), new Tiles(0), zone: 0, wide: new Tiles(24), deep: new Tiles(1));
        Handle<Building> dwelling = world.CreateBuilding(home, AttendDwelling, Ticks.Zero, Key);
        Handle<Household> household = world.CreateHousehold(dwelling, lifeStage: 0);
        Handle<Citizen> citizen = world.CreateCitizen(household);

        Handle<Lot> primaryLot = world.Lots.Create(
            new Tiles(64), new Tiles(0), zone: 0, wide: new Tiles(24), deep: new Tiles(1));
        Handle<Building> primary = world.CreateBuilding(primaryLot, AttendPrimary, Ticks.Zero, Key);

        Handle<Lot> secondaryLot = world.Lots.Create(
            new Tiles(128), new Tiles(0), zone: 0, wide: new Tiles(24), deep: new Tiles(1));
        Handle<Building> secondary = world.CreateBuilding(
            secondaryLot, AttendSecondary, Ticks.Zero, Key);

        return (
            world,
            world.Citizens.Rows.Resolve(citizen),
            world.Buildings.Rows.Resolve(primary),
            world.Buildings.Rows.Resolve(secondary));
    }

    /// <summary>
    /// Attendance is credited to the level the SCHOOL teaches, never to whatever the Citizen's own
    /// stage is, and a Citizen carries no notion of stage here at all.
    /// </summary>
    [Fact]
    public void Attendance_is_credited_to_the_schools_own_level_not_the_citizens()
    {
        (World world, int citizen, int primary, int secondary) = AttendanceCity();

        world.RecordAttendance(citizen, primary);
        world.RecordAttendance(citizen, primary);
        world.RecordAttendance(citizen, secondary);

        Assert.Equal(2, world.Citizens.SchoolingPrimary[citizen]);
        Assert.Equal(1, world.Citizens.SchoolingSecondary[citizen]);
    }

    /// <summary>
    /// The counter saturates at its width rather than wrapping past it back to zero.
    /// </summary>
    /// <remarks>
    /// <c>adr/0006</c>'s extension to quantities: a counter that wraps reads as a Citizen with a long
    /// history reporting as one with none, which is the one failure that would look like the
    /// mechanism working.
    /// </remarks>
    [Fact]
    public void Attendance_saturates_at_the_maximum_rather_than_wrapping()
    {
        (World world, int citizen, int primary, _) = AttendanceCity();

        world.Citizens.SchoolingPrimary[citizen] = ushort.MaxValue;

        world.RecordAttendance(citizen, primary);

        Assert.Equal(ushort.MaxValue, world.Citizens.SchoolingPrimary[citizen]);
    }

    // =================================================================================================
    // Experience. A hand-built world: an employed Citizen accrues, an idle one does not.
    // =================================================================================================

    private const string ExperienceRuleset = """
        [[building]]
        name = "dwelling"
        houses = true

        [[building]]
        name   = "school"
        serves = "education"
        level  = 1

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [jobs]
        interval = 32
        revisit_ticks = 1024
        candidates = 3
        shift_hours_min = 6
        shift_hours_max = 10
        arrive_early_max_minutes = 15
        experience_per_day = 64
        tier2_experience = 1000000
        unschooled_experience_percent = 50

        [schooling]
        attendance_weight_percent = 70
        full_attendance_days      = 40
        primary_gate_days         = 20
        tier2_score               = 50
        university_days           = 16
        """;

    private static void RunUntilDay(Simulation simulation, int day)
    {
        ulong target = (ulong)day * Ticks.PerDay;

        while (simulation.Tick.Raw <= target)
        {
            simulation.Step(default);
        }
    }

    /// <summary>
    /// One Day worked earns a schooled Citizen the full rate, an unschooled one the Ruleset's
    /// discount of it, and an idle Citizen nothing at all.
    /// </summary>
    /// <remarks>
    /// <c>tier2_experience</c> is set far out of reach so the promotion ceiling this test is not
    /// about cannot fire and blur the reading -- <see cref="Promotion_lands_at_tier_two_experience_and_never_carries_past_it"/>
    /// is where that half lives.
    /// </remarks>
    [Fact]
    public void Experience_accrues_only_while_employed_and_slower_for_an_unschooled_citizen()
    {
        var world = new World(6, Load(ExperienceRuleset), Key);
        var simulation = new Simulation(world, Key);

        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), zone: 0, wide: new Tiles(24), deep: new Tiles(1));
        Handle<Building> dwelling = world.CreateBuilding(lot, kind: 1, Ticks.Zero, Key);
        Handle<Household> household = world.CreateHousehold(dwelling, lifeStage: 0);

        Handle<Citizen> schooled = world.CreateCitizen(household);
        Handle<Citizen> unschooled = world.CreateCitizen(household);
        Handle<Citizen> idle = world.CreateCitizen(household);

        // Unpremised: adr/0145 makes this a legitimate steady state, and Progress asks only whether
        // the Workplace handle resolves, never where it stands.
        Handle<Business> employer = world.CreateBusiness(default, kind: 0);

        world.Employ(schooled, employer, Ticks.Zero);
        world.Employ(unschooled, employer, Ticks.Zero);

        int schooledSlot = world.Citizens.Rows.Resolve(schooled);
        int unschooledSlot = world.Citizens.Rows.Resolve(unschooled);
        int idleSlot = world.Citizens.Rows.Resolve(idle);

        world.Citizens.ChildhoodScore[schooledSlot] = 80;
        world.Citizens.ChildhoodScore[unschooledSlot] = 10;

        // Three Day boundaries fire: Tick 0, 2048 and 4096.
        RunUntilDay(simulation, 2);

        Assert.Equal(0, world.Citizens.Experience[idleSlot]);
        Assert.Equal(3 * 64, world.Citizens.Experience[schooledSlot]);
        Assert.Equal(3 * 32, world.Citizens.Experience[unschooledSlot]);
    }

    private const string PromotionRuleset = """
        [[building]]
        name = "dwelling"
        houses = true

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [jobs]
        interval = 32
        revisit_ticks = 1024
        candidates = 3
        shift_hours_min = 6
        shift_hours_max = 10
        arrive_early_max_minutes = 15
        experience_per_day = 4096
        tier2_experience = 4096
        """;

    /// <summary>
    /// Promotion lands the Tick experience first reaches the cut, keeps accruing past it, and never
    /// carries a Citizen to Tier 3 however far past the cut it runs.
    /// </summary>
    /// <remarks>
    /// <c>adr/0104</c> keeps the credential a wall: experience is the only route from 1 to 2 and no
    /// route at all from 2 to 3, which is what would let a patient player skip ever building a school.
    /// </remarks>
    [Fact]
    public void Promotion_lands_at_tier_two_experience_and_never_carries_past_it()
    {
        var world = new World(4, Load(PromotionRuleset), Key);
        var simulation = new Simulation(world, Key);

        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), zone: 0, wide: new Tiles(24), deep: new Tiles(1));
        Handle<Building> dwelling = world.CreateBuilding(lot, kind: 1, Ticks.Zero, Key);
        Handle<Household> household = world.CreateHousehold(dwelling, lifeStage: 0);
        Handle<Citizen> worker = world.CreateCitizen(household);
        Handle<Business> employer = world.CreateBusiness(default, kind: 0);

        world.Employ(worker, employer, Ticks.Zero);

        int slot = world.Citizens.Rows.Resolve(worker);

        Assert.Equal(SchoolingRuleset.FloorTier, world.Citizens.SkillTier[slot]);

        RunUntilDay(simulation, 0);

        Assert.Equal(2, world.Citizens.SkillTier[slot]);

        RunUntilDay(simulation, 10);

        Assert.Equal(2, world.Citizens.SkillTier[slot]);
        Assert.True(
            world.Citizens.Experience[slot] > 4096,
            "experience stopped accruing once it crossed the promotion threshold.");
    }

    // =================================================================================================
    // The credential wall. A real, routable city -- EmploymentEngine.Assign is the first Trip
    // generator (adr/0081), and a Citizen with no real Address in reach of anything is never SEEKING
    // at all, so this needs SyntheticCity rather than a hand-built pair of Buildings.
    // =================================================================================================

    private const byte JobsDwelling = 1;
    private const byte JobsWorkplace = 2;
    private const int JobsCitizens = 100;

    private static string JobsCityRuleset(int requiresTier) => $$"""
        [[building]]
        name = "dwelling"
        houses = true

        [[building]]
        name     = "workplace"
        premises = true
        business = "workplace"

        [[business]]
        name = "workplace"
        {{(requiresTier > 0 ? $"requires_tier = {requiresTier}" : string.Empty)}}
        shift_start_earliest_hour = 6
        shift_start_latest_hour = 10

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
        setback_tiles = 2

        [capacity]
        floor_tiles_per_occupant = 6
        # The range's ceiling, so Holds() floors at exactly 1 job whatever floor area the generator
        # happened to give the workplace -- ServiceCapacityTests' same trick on floor_tiles_per_place.
        floor_tiles_per_job      = 1024

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [jobs]
        interval = 1
        revisit_ticks = 1
        candidates = 3
        shift_hours_min = 6
        shift_hours_max = 10
        arrive_early_max_minutes = 15
        """;

    private static (World World, Simulation Simulation) JobsCity(int requiresTier)
    {
        Ruleset rules = Load(JobsCityRuleset(requiresTier));
        var world = new World(JobsCitizens, rules, Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot))
            {
                world.CreateBuilding(world.Lots.Rows.At(slot), JobsWorkplace, Ticks.Zero, Key);

                return (world, simulation);
            }
        }

        Assert.Fail("the generated city left no vacant Lot for the workplace.");

        return (world, simulation);
    }

    /// <summary>
    /// <c>[[business]] requires_tier</c> refuses a Citizen below it and leaves them
    /// <see cref="EmploymentState.BelowCredential"/>, and a vacancy that is merely FULL leaves
    /// <see cref="EmploymentState.NoVacancy"/> instead -- the two must not be the same reading.
    /// </summary>
    /// <remarks>
    /// <c>02 §5.4</c>: the credential is a filter beside the vacancy question, never a fact learned
    /// from failing one, so a wall and a full building have to leave two different reasons behind.
    /// </remarks>
    [Fact]
    public void A_credential_wall_refuses_below_the_required_tier_and_a_full_vacancy_refuses_with_no_vacancy()
    {
        (World below, Simulation belowSim) = JobsCity(requiresTier: 3);

        // interval = revisit_ticks = 1, so one Tick samples every Citizen once.
        belowSim.Step(default);

        bool anyBelowCredential = false;

        for (int slot = 0; slot < below.Citizens.Rows.SlotCount; slot++)
        {
            if (below.Citizens.Rows.IsLive(slot)
                && (EmploymentState)below.Citizens.Employment[slot] == EmploymentState.BelowCredential)
            {
                anyBelowCredential = true;

                break;
            }
        }

        Assert.True(anyBelowCredential, "no Citizen was refused for lacking the required tier.");

        (World full, Simulation fullSim) = JobsCity(requiresTier: 0);

        fullSim.Step(default);

        int employed = 0;
        bool anyNoVacancy = false;

        for (int slot = 0; slot < full.Citizens.Rows.SlotCount; slot++)
        {
            if (!full.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            switch ((EmploymentState)full.Citizens.Employment[slot])
            {
                case EmploymentState.Employed:
                    employed++;
                    break;
                case EmploymentState.NoVacancy:
                    anyNoVacancy = true;
                    break;
            }
        }

        // Exactly one job stands (floor_tiles_per_job is the range's ceiling), so exactly one Citizen
        // takes it and everybody else who reached the building found it already taken.
        Assert.Equal(1, employed);
        Assert.True(anyNoVacancy, "nobody found the single job already taken.");
    }

    // =================================================================================================
    // The wage grading. A hand-built world: one Citizen, no employer needed at all.
    // =================================================================================================

    private const string GradedRuleset = """
        [[building]]
        name = "dwelling"
        houses = true

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [jobs]
        interval = 32
        revisit_ticks = 1024
        candidates = 3
        shift_hours_min = 6
        shift_hours_max = 10
        arrive_early_max_minutes = 15
        wage_tier_percent = [100, 160, 260]
        experience_per_day = 64
        tier2_experience = 2048
        experience_premium_percent = 40
        """;

    /// <summary>
    /// <see cref="WorkSchedule.Graded"/> applies the tier percentage first and the experience
    /// premium second: a Tier 1 Citizen with no experience earns exactly the posted rate, and a
    /// Tier 2 Citizen at the experience ceiling earns 160% of it plus the full 40% premium on TOP of
    /// that 160%, not on top of the posted rate.
    /// </summary>
    [Fact]
    public void Graded_wages_apply_the_tier_percentage_and_then_the_experience_premium()
    {
        var world = new World(4, Load(GradedRuleset), Key);

        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), zone: 0, wide: new Tiles(24), deep: new Tiles(1));
        Handle<Building> dwelling = world.CreateBuilding(lot, kind: 1, Ticks.Zero, Key);
        Handle<Household> household = world.CreateHousehold(dwelling, lifeStage: 0);
        Handle<Citizen> citizen = world.CreateCitizen(household);
        int slot = world.Citizens.Rows.Resolve(citizen);

        const long posted = 10_000;

        world.Citizens.SkillTier[slot] = 1;
        world.Citizens.Experience[slot] = 0;

        Assert.Equal(posted, WorkSchedule.Graded(world, slot, posted));

        world.Citizens.SkillTier[slot] = 2;
        world.Citizens.Experience[slot] = 2048;

        // 10,000 * 160% = 16,000; 16,000 * 140% (the full 40% premium) = 22,400.
        Assert.Equal(22_400, WorkSchedule.Graded(world, slot, posted));
    }

    // =================================================================================================
    // Enrolment, tuition, drop-out and graduation. A real, routable city -- SyntheticCity, generated,
    // with the school placed by hand on ServiceCapacityTests' precedent.
    // =================================================================================================

    private const byte SchoolDwelling = 1;
    private const byte SchoolUniversity = 2;
    private const byte SchoolCollege = 3;

    private const int TuitionPerDay = 512;
    private const int UniversityDays = 16;

    private const int SchoolCityCitizens = 400;

    private const string SchoolCityRuleset = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        houses = true
        premises = true
        bins = [ { resource = "sundries", capacity = 48 } ]

        [[building]]
        name   = "university"
        serves = "education"
        level  = 3

        [[building]]
        name     = "college"
        serves   = "education"
        level    = 3
        premises = true
        business = "tuition"
        bins = [
            { resource = "money", owner = "business" },
        ]

        [[business]]
        name = "tuition"
        tuition_per_day = 512

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
        setback_tiles = 2

        [capacity]
        floor_tiles_per_occupant = 6

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 0

        [schooling]
        attendance_weight_percent = 70
        full_attendance_days      = 40
        primary_gate_days         = 20
        tier2_score               = 50
        university_days           = 16
        """;

    private static (World World, Simulation Simulation) SchoolCity()
    {
        Ruleset rules = Load(SchoolCityRuleset);
        var world = new World(SchoolCityCitizens, rules, Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return (world, simulation);
    }

    /// <summary>Every housed Household, by slot.</summary>
    private static List<int> HousedHouseholds(World world)
    {
        List<int> found = [];

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out _))
            {
                found.Add(slot);
            }
        }

        return found;
    }

    /// <summary>Builds a school of <paramref name="kind"/> on the first vacant Lot the city left.</summary>
    private static int PlaceSchool(World world, byte kind)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot))
            {
                Handle<Building> building = world.CreateBuilding(
                    world.Lots.Rows.At(slot), kind, Ticks.Zero, Key);

                return world.Buildings.Rows.Resolve(building);
            }
        }

        Assert.Fail("the generated city left no vacant Lot to build a school on.");

        return -1;
    }

    /// <summary>Marks a Household as having a school-leaver deciding, with an adult to decide with.</summary>
    /// <remarks>
    /// ⚠ <b>Age is written directly rather than drawn</b>, because this Ruleset states no
    /// <c>[[life_stage]]</c> table -- <see cref="World.CreateCitizen"/> then draws zero, which
    /// <see cref="SchoolingEngine"/>'s <c>FirstAdult</c> reads as a child. Setting it here is the
    /// fixture's job, not the mechanism's.
    /// </remarks>
    private static void MakeConsidering(World world, int householdSlot)
    {
        world.Households.State[householdSlot] = (byte)HouseholdState.Considering;

        foreach (int member in world.Members.Walk(householdSlot))
        {
            world.Citizens.Age[member] = 9_000;
        }
    }

    private static long Purse(World world, int householdSlot) =>
        world.Bins.Rows.TryResolve(world.Households.Balance[householdSlot], out int bin)
            ? world.Bins.LevelAt(bin)
            : 0;

    private static void DrainPurse(World world, Ticks now, int householdSlot)
    {
        if (world.Bins.Rows.TryResolve(world.Households.Balance[householdSlot], out int bin))
        {
            long level = world.Bins.LevelAt(bin);

            if (level > 0)
            {
                world.Withdraw(world.Bins.Rows.At(bin), level, now);
            }
        }
    }

    /// <summary>The Business tenanting a Building, or -1.</summary>
    private static int TenantOf(World world, int buildingSlot)
    {
        foreach (int tenant in world.BuildingBusinesses.Walk(buildingSlot))
        {
            return tenant;
        }

        return -1;
    }

    /// <summary>
    /// A Household marked <see cref="HouseholdState.Considering"/> and housed within reach of a
    /// PUBLIC university enrols there, is left <see cref="HouseholdState.InEducation"/>, and every
    /// member is dismissed and marked <see cref="EmploymentState.Studying"/>.
    /// </summary>
    [Fact]
    public void A_household_enrols_publicly_when_a_place_is_reachable_and_its_adults_stop_working()
    {
        (World world, Simulation simulation) = SchoolCity();
        int university = PlaceSchool(world, SchoolUniversity);

        List<int> housed = HousedHouseholds(world);

        Assert.True(housed.Count > 0, "the generated city housed nobody.");

        foreach (int household in housed)
        {
            MakeConsidering(world, household);
        }

        RunUntilDay(simulation, 0);

        int enrolled = housed.Find(
            h => (HouseholdState)world.Households.State[h] == HouseholdState.InEducation);

        Assert.True(enrolled >= 0, "no considering Household reached the public university.");
        Assert.True(
            world.Buildings.Rows.TryResolve(world.Households.University[enrolled], out int enrolledAt));
        Assert.Equal(university, enrolledAt);

        foreach (int member in world.Members.Walk(enrolled))
        {
            Assert.Equal((byte)EmploymentState.Studying, world.Citizens.Employment[member]);
            Assert.False(world.Businesses.Rows.IsValid(world.Citizens.Workplace[member]));
        }

        Assert.True(simulation.LastSchooling.EnrolledPublic > 0);
    }

    /// <summary>
    /// With no public place standing, a Household affording the WHOLE course takes the private
    /// college instead, and tuition moves from its purse to the college's till one Day at a time,
    /// with the money conserved throughout.
    /// </summary>
    [Fact]
    public void A_household_takes_the_private_college_when_no_public_place_stands_and_pays_tuition_daily()
    {
        (World world, Simulation simulation) = SchoolCity();
        int college = PlaceSchool(world, SchoolCollege);

        List<int> housed = HousedHouseholds(world);
        int household = housed[0];

        MakeConsidering(world, household);
        world.Endow(
            world.Households.Rows.At(household), new Money((long)TuitionPerDay * UniversityDays));

        RunUntilDay(simulation, 0);

        Assert.Equal((byte)HouseholdState.InEducation, world.Households.State[household]);
        Assert.True(
            world.Buildings.Rows.TryResolve(world.Households.University[household], out int enrolledAt));
        Assert.Equal(college, enrolledAt);
        Assert.True(simulation.LastSchooling.EnrolledPrivate > 0);

        int business = TenantOf(world, college);

        Assert.True(business >= 0, "the college stands with no tenant Business.");
        Assert.True(world.Bins.Rows.TryResolve(world.Businesses.Balance[business], out int till));

        long businessBefore = world.Bins.LevelAt(till);
        long householdBefore = Purse(world, household);

        // The enrolment Day itself charges nothing -- Continue() runs ahead of Enrol(), so the first
        // Day a Household is In Education is the first Day it is charged for.
        RunUntilDay(simulation, 1);

        Assert.Equal(businessBefore + TuitionPerDay, world.Bins.LevelAt(till));
        Assert.Equal(householdBefore - TuitionPerDay, Purse(world, household));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A Household that cannot cover the WHOLE course outright is turned away rather than started on
    /// credit.
    /// </summary>
    [Fact]
    public void A_household_that_cannot_cover_the_whole_private_course_is_turned_away()
    {
        (World world, Simulation simulation) = SchoolCity();
        PlaceSchool(world, SchoolCollege);

        List<int> housed = HousedHouseholds(world);
        int household = housed[0];

        MakeConsidering(world, household);
        world.Endow(
            world.Households.Rows.At(household),
            new Money(((long)TuitionPerDay * UniversityDays) - 1));

        RunUntilDay(simulation, 0);

        Assert.Equal((byte)HouseholdState.None, world.Households.State[household]);
        Assert.False(world.Buildings.Rows.TryResolve(world.Households.University[household], out _));
        Assert.True(simulation.LastSchooling.TurnedAway > 0);
    }

    /// <summary>
    /// A privately-enrolled Household whose purse runs dry mid-course leaves without a degree: the
    /// Skill Tier is unchanged and the state returns to ordinary rather than to anything special.
    /// </summary>
    [Fact]
    public void A_privately_enrolled_household_whose_purse_empties_drops_out_without_a_degree()
    {
        (World world, Simulation simulation) = SchoolCity();
        PlaceSchool(world, SchoolCollege);

        List<int> housed = HousedHouseholds(world);
        int household = housed[0];

        MakeConsidering(world, household);
        world.Endow(
            world.Households.Rows.At(household), new Money((long)TuitionPerDay * UniversityDays));

        RunUntilDay(simulation, 0);

        Assert.Equal((byte)HouseholdState.InEducation, world.Households.State[household]);

        int member = -1;

        foreach (int m in world.Members.Walk(household))
        {
            member = m;

            break;
        }

        Assert.True(member >= 0);

        byte tierBefore = world.Citizens.SkillTier[member];

        // Emptied before the next Day's charge, so today's tuition finds nothing to take.
        DrainPurse(world, simulation.Tick, household);

        RunUntilDay(simulation, 1);

        Assert.Equal((byte)HouseholdState.None, world.Households.State[household]);
        Assert.False(world.Buildings.Rows.TryResolve(world.Households.University[household], out _));
        Assert.Equal(tierBefore, world.Citizens.SkillTier[member]);
    }

    /// <summary>
    /// A Household that stays enrolled for the full course graduates: the Skill Tier reaches
    /// <see cref="SchoolingRuleset.TopTier"/> and only then, not before.
    /// </summary>
    [Fact]
    public void A_full_course_confers_the_top_tier_on_graduation()
    {
        (World world, Simulation simulation) = SchoolCity();
        PlaceSchool(world, SchoolUniversity);

        List<int> housed = HousedHouseholds(world);
        int household = housed[0];

        MakeConsidering(world, household);

        RunUntilDay(simulation, 0);

        Assert.Equal((byte)HouseholdState.InEducation, world.Households.State[household]);

        int member = -1;

        foreach (int m in world.Members.Walk(household))
        {
            member = m;

            break;
        }

        Assert.True(member >= 0);
        Assert.True(world.Citizens.SkillTier[member] < SchoolingRuleset.TopTier);

        RunUntilDay(simulation, UniversityDays);

        Assert.Equal((byte)HouseholdState.None, world.Households.State[household]);
        Assert.False(world.Buildings.Rows.TryResolve(world.Households.University[household], out _));
        Assert.Equal(SchoolingRuleset.TopTier, world.Citizens.SkillTier[member]);
    }

    // =================================================================================================
    // Inheritance. A hand-built world: one parent Household, two children, no schools at all.
    // =================================================================================================

    private const string InheritanceRuleset = """
        [[resource]]
        name = "money"
        family = "money"

        [[building]]
        name = "dwelling"
        houses = true
        """;

    /// <summary>
    /// A child leaving home opens a new Household holding an equal share of the parent's balance --
    /// one share kept and one per child -- and the money supply does not move.
    /// </summary>
    /// <remarks>
    /// <c>World.SpawnChildren</c>'s own remark: without this transfer the private half of the
    /// university is dead, because a generated Household otherwise opens at zero and could never
    /// afford tuition.
    /// </remarks>
    [Fact]
    public void A_child_leaving_home_inherits_an_equal_share_and_money_is_conserved()
    {
        var world = new World(20, Load(InheritanceRuleset), Key);

        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), zone: 0, wide: new Tiles(24), deep: new Tiles(1));
        Handle<Building> dwelling = world.CreateBuilding(lot, kind: 1, Ticks.Zero, Key);
        Handle<Household> parent = world.CreateHousehold(dwelling, lifeStage: 0);

        Handle<Citizen> firstChild = world.Bear(parent);
        Handle<Citizen> secondChild = world.Bear(parent);

        world.Endow(parent, new Money(300));

        long issuedBefore = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

        int spawned = world.SpawnChildren(parent, becomes: 0, Ticks.Zero);

        Assert.Equal(2, spawned);

        int parentSlot = world.Households.Rows.Resolve(parent);

        int firstHousehold = world.Households.Rows.Resolve(
            world.Citizens.HouseholdOf[world.Citizens.Rows.Resolve(firstChild)]);
        int secondHousehold = world.Households.Rows.Resolve(
            world.Citizens.HouseholdOf[world.Citizens.Rows.Resolve(secondChild)]);

        // 300 split three ways: one share kept by the parent, one per child.
        Assert.Equal(100, Purse(world, parentSlot));
        Assert.Equal(100, Purse(world, firstHousehold));
        Assert.Equal(100, Purse(world, secondHousehold));

        Assert.Equal(issuedBefore, world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);

        world.Invariants.RunEndOfRun(world);
    }
}
