using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>[capacity] floor_tiles_per_place</c> — <b>the school can be full, and being full is a
/// different city from being unreachable.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Before this key one school served a million people.</b> <c>ServiceEngine</c> satisficed —
/// it stopped at the first school inside the Fast rung (<c>adr/0017</c>) — and in a city small enough
/// for everything to be Fast that is the first school in slot order, for everybody, for ever. A
/// reading of <c>--school --citizens 2000 --schools 4</c> put all 109 families with a child in one
/// of the four and left the other three serving nobody, and ***the reach panel called that 100%.***
/// ⚠ <b>That break is gone and this class holds the assertion that replaced it</b> — see
/// <see cref="Both_schools_teach_somebody_when_neither_is_full"/>. The ceiling and the ordering are
/// two repairs to one reading, and only the first of them is a Ruleset key.
/// </para>
/// <para>
/// <b>What the ceiling buys is not scarcity, it is DISTRIBUTION.</b> A full school is skipped and
/// the family walks on to the next one, so where the schools stand starts to matter and not only how
/// many there are. Scarcity is the case beyond that, and it is the third failure counter.
/// </para>
/// <para>
/// ⚠ <b>Every fixture here sets the rate to a Cell, which is the range's ceiling</b>, because
/// <see cref="CapacityRuleset.Holds"/> floors at one wherever there is any floor at all — so the
/// tightest legal rate gives every school in the fixture exactly <b>one</b> place, whatever the
/// generator gave it to stand on. ***A test that had to predict a parcel's floor area would be
/// asserting on the subdivider***, which is not what is under test.
/// </para>
/// </remarks>
public sealed class ServiceCapacityTests
{
    // ServiceTests' figure, for its reason: every assertion here is a shape, and the classes share
    // no fixture on purpose -- this one has to vary the [capacity] table, which that one holds fixed.
    private const int Citizens = 400;
    private const int Seed = 20_260_830;

    private const byte Dwelling = 1;
    private const byte School = 2;

    // ---- the ceiling ----------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>One place means one attendance a Day, and everybody else is turned away.</b>
    /// </summary>
    /// <remarks>
    /// The load-bearing assertion in this class. Without it the key parses, the column advances and
    /// nothing is ever refused — which is the shape <c>[traffic]</c> had for a whole milestone.
    /// </remarks>
    [Fact]
    public void A_school_takes_no_more_than_its_places_in_a_day()
    {
        (World world, Simulation simulation) = City(Bounded);

        Place(simulation, world, FirstVacantLot(world));
        StepToDay(simulation, 4);

        Assert.Equal(1, world.DeclaredPlaces(OnlySchool(world)));
        Assert.Equal(1, simulation.Services.Attended);
        Assert.True(simulation.Services.Full > 0, "nobody was turned away from a school of one.");
    }

    /// <summary>
    /// <b>A family turned away at the door is <c>full</c> and never <c>unreached</c>.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The two counters are the whole reason the fullness test sits AFTER the route</b> in
    /// <c>ServiceEngine.Reach</c>. Asking the cheap question first would file a school behind an
    /// Arterial under <em>full</em>, and the player would build a second school to fix a road.
    /// </remarks>
    [Fact]
    public void The_family_turned_away_is_counted_full_and_not_unreached()
    {
        (World world, Simulation simulation) = City(Bounded);

        Place(simulation, world, FirstVacantLot(world));
        StepToDay(simulation, 4);

        Assert.True(simulation.Services.Full > 0, "nobody was turned away.");
        Assert.Equal(0, simulation.Services.Unreached);
        Assert.Equal(0, simulation.Services.NoService);
    }

    /// <summary>
    /// <b>A second school takes the overflow</b> — a full candidate is skipped and the walk goes on.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the behaviour the ceiling exists for and the one a naive spelling loses.</b> A
    /// <c>Reach</c> that failed on a full school would turn the family away with a school standing
    /// empty next door, which is worse than having no ceiling at all.
    /// </remarks>
    [Fact]
    public void A_family_walks_on_to_the_second_school_when_the_first_is_full()
    {
        (World one, Simulation withOne) = City(Bounded);
        (World two, Simulation withTwo) = City(Bounded);

        Place(withOne, one, FirstVacantLot(one));

        foreach (int lot in TwoVacantLots(two))
        {
            Place(withTwo, two, lot);
        }

        StepToDay(withOne, 4);
        StepToDay(withTwo, 4);

        Assert.Equal(1, withOne.Services.Attended);
        Assert.Equal(2, withTwo.Services.Attended);
        Assert.True(
            withTwo.Services.Full < withOne.Services.Full,
            "the second school took nobody, so a full candidate was not being skipped.");
    }

    /// <summary>The meter is a RATE, so it resets and the second Day teaches somebody too.</summary>
    /// <remarks>
    /// <b><c>BuildingTable.AttendedDay</c> is what makes this true</b>, and a per-call bound would
    /// pass every other assertion in this class while failing this one: the school would take its one
    /// pupil on the first Day and stand empty for ever after.
    /// </remarks>
    [Fact]
    public void The_meter_resets_on_the_next_day()
    {
        (World world, Simulation simulation) = City(Bounded);

        Place(simulation, world, FirstVacantLot(world));
        StepToDay(simulation, 4);

        Assert.Equal(1, simulation.Services.Attended);

        StepToDay(simulation, 5);

        Assert.Equal(1, simulation.Services.Attended);
        Assert.Equal(1, world.Buildings.AttendedToday[OnlySchool(world)]);
    }

    // ---- the absence ----------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>No rate means no school is ever full, which is the OPPOSITE of what the other three
    /// rates' absence means.</b>
    /// </summary>
    /// <remarks>
    /// ***Every Ruleset shipped before this key existed is this world***, so the day the key landed
    /// none of them changed behaviour. The other reading — absent means no places — would have
    /// emptied every school in the corpus silently.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_rate_never_fills_a_school()
    {
        (World world, Simulation simulation) = City(Unbounded);

        Place(simulation, world, FirstVacantLot(world));
        StepToDay(simulation, 4);

        Assert.Equal(0, simulation.Services.Full);
        Assert.True(simulation.Services.Attended > 1, "only one family attended an unbounded school.");
        Assert.True(world.HasServicePlace(OnlySchool(world), day: 4), "an unbounded school filled.");
    }

    /// <summary>
    /// <b>The tally advances in a world with no ceiling</b>, because that is the number a designer
    /// needs in order to choose one.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A meter nobody can read is not evidence for the number that would bound it.</b> The rate
    /// in <c>rulesets/schooled.toml</c> was chosen against a reading taken exactly this way, and a
    /// column that only advanced once a ceiling existed could not have supplied it.
    /// </remarks>
    [Fact]
    public void The_tally_advances_even_where_nothing_bounds_it()
    {
        (World world, Simulation simulation) = City(Unbounded);

        Place(simulation, world, FirstVacantLot(world));
        StepToDay(simulation, 4);

        Assert.Equal(
            simulation.Services.Attended, world.Buildings.AttendedToday[OnlySchool(world)]);
        Assert.Equal(0, world.DeclaredPlaces(OnlySchool(world)));
    }

    // ---- the derivation -------------------------------------------------------------------------

    /// <summary>
    /// <b>Places are the ground over the rate</b> — an identity, so it cannot drift from whatever
    /// the subdivider happens to hand out.
    /// </summary>
    /// <remarks>
    /// <c>plans/0053</c> step 3 arriving at the one capacity that had escaped it: ***a bigger school
    /// teaches more children***, which is what makes siting a decision rather than a formality.
    /// </remarks>
    [Fact]
    public void The_places_are_the_ground_over_the_rate()
    {
        (World world, Simulation simulation) = City(Roomy);

        Place(simulation, world, FirstVacantLot(world));

        int school = OnlySchool(world);
        int floor = world.FloorTilesOf(school);

        Assert.True(floor > 0, "the school stands on no ground.");
        Assert.Equal(
            CapacityRuleset.Holds(floor, world.Rules.Capacity.FloorTilesPerPlace),
            world.DeclaredPlaces(school));
    }

    /// <summary>A kind that serves nobody has no places, however much floor it has.</summary>
    /// <remarks>
    /// ⚠ <b>Zero is two different cities on this method and callers must not read it as one</b> — a
    /// kind that serves nothing against a world with no rate. <see cref="World.HasServicePlace"/> is
    /// the question with the answer in it; this one is only the quantity.
    /// </remarks>
    [Fact]
    public void A_kind_that_serves_nothing_has_no_places()
    {
        (World world, _) = City(Roomy);

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.Kind[slot] == Dwelling)
            {
                Assert.True(world.FloorTilesOf(slot) > 0, "a dwelling stands on no ground.");
                Assert.Equal(0, world.DeclaredPlaces(slot));

                return;
            }
        }

        Assert.Fail("the generated city stood no dwelling.");
    }

    // ---- the loader -----------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>The rate is refused where no kind serves anything.</b>
    /// </summary>
    /// <remarks>
    /// <c>TryAttendedRates</c>' refusal from the other side, and this key needs it more than that one
    /// does: ***its absence is invisible by design***, so an inert statement of it reads on the page
    /// as a ceiling this world has and no run would ever say otherwise.
    /// </remarks>
    [Fact]
    public void The_rate_is_refused_where_no_kind_serves()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            WithPlaceRate(12)
                .Replace(SchoolKind, string.Empty, StringComparison.Ordinal)
                .Replace(AttendedNeeds, BoughtNeeds, StringComparison.Ordinal),
            "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("floor_tiles_per_place", result.Describe(), StringComparison.Ordinal);
        Assert.Contains("serves", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>A stated zero is refused, because absence is how you mean it.</summary>
    [Fact]
    public void A_stated_zero_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            WithPlaceRate(1024).Replace(
                "floor_tiles_per_place = 1024",
                "floor_tiles_per_place = 0",
                StringComparison.Ordinal),
            "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("floor_tiles_per_place", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>And a rate past a Cell, which is the other three rates' ceiling unchanged.</summary>
    [Fact]
    public void A_rate_past_a_cell_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            WithPlaceRate(1024).Replace(
                "floor_tiles_per_place = 1024",
                "floor_tiles_per_place = 1025",
                StringComparison.Ordinal),
            "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("floor_tiles_per_place", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>The shipped file that states it loads, which is what makes the rest non-vacuous.</summary>
    [Fact]
    public void The_shipped_school_ruleset_states_the_rate()
    {
        string path = Path.Combine(RepoRoot(), "rulesets", "schooled.toml");
        RulesetLoadResult result = RulesetLoader.Parse(File.ReadAllText(path), path);

        Assert.True(result.Ok, result.Describe());
        Assert.True(
            result.Ruleset!.Capacity.FloorTilesPerPlace > 0,
            "rulesets/schooled.toml no longer states floor_tiles_per_place.");
    }

    // ---- who gets the place -----------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>The one place goes to the family living nearest the school.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ceiling is what made this askable at all.</b> With no scarcity the order of admission
    /// has no consequence, so the bias could not be seen — and when the ceiling arrived the answer
    /// was <em>slot order</em>, which is the order Households were created in. ***The oldest
    /// families in the city took the school every Day and the newest never did.***
    /// </para>
    /// <para>
    /// ⚠ <b>The second assertion is a vacuity guard and not a claim about the design.</b> If the
    /// nearest applicant were also the first applicant in slot order, this would pass against the
    /// very code it exists to refuse. A red one there means the generated city moved, not that
    /// admission did.
    /// </para>
    /// <para>
    /// 🔴 <b>THE SCHOOL IS SITED AT THE FAR END OF THE CITY, AND THAT IS THE GUARD RATHER THAN A
    /// DETAIL.</b> Placed on the first vacant Lot it lands beside the first Households the generator
    /// made, ***so slot order and distance order agree and the fixture asks nothing.*** The first
    /// spelling did exactly that and its guard went red on a passing mechanism, which is the
    /// cheapest possible reminder that ***a test of an ordering has to be built somewhere the two
    /// orderings disagree.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_place_goes_to_the_nearest_family_and_not_the_oldest_household()
    {
        (World world, Simulation simulation) = City(Bounded);

        Place(simulation, world, FarApartVacantLots(world)[1]);

        int school = OnlySchool(world);

        StepToDay(simulation, 4);

        List<(int Slot, TravelTime Cost)> applicants = Applicants(world, school);

        Assert.True(applicants.Count > 1, "one applicant cannot be admitted ahead of anybody.");
        Assert.Equal(1, world.DeclaredPlaces(school));

        int admitted = Admitted(world);
        TravelTime nearest = applicants.Min(a => a.Cost);

        Assert.Equal(nearest, applicants.Single(a => a.Slot == admitted).Cost);

        Assert.NotEqual(
            applicants.Min(a => a.Slot),
            admitted);
    }

    /// <summary>
    /// 🔴 <b>Two schools are two schools, and the second one is not left standing empty.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the reading that opened the whole question, as an assertion.</b>
    /// <c>--school --citizens 2000 --schools 4</c> put all 109 families with a child in ONE of the
    /// four: the walk stopped at the first <c>Fast</c>-rung candidate in slot order (<c>adr/0017</c>),
    /// and a city small enough for everything to be Fast has exactly one such candidate for
    /// everybody, for ever. ***And the reach panel called that 100%***, because a share-of-occasions
    /// number cannot see which Building delivered them.
    /// </para>
    /// <para>
    /// ⚠ <b>No ceiling in this fixture, deliberately.</b> The ceiling spreads the load by turning
    /// families away; this asserts the load spreads with nobody turned away at all, so it is the
    /// <em>choice</em> under test and never the capacity.
    /// </para>
    /// </remarks>
    [Fact]
    public void Both_schools_teach_somebody_when_neither_is_full()
    {
        (World world, Simulation simulation) = City(Unbounded);

        int[] lots = FarApartVacantLots(world);

        foreach (int lot in lots)
        {
            Place(simulation, world, lot);
        }

        StepToDay(simulation, 4);

        List<int> schools = Schools(world);

        Assert.Equal(2, schools.Count);
        Assert.Equal(0, simulation.Services.Full);
        Assert.All(
            schools,
            slot => Assert.True(
                world.Buildings.AttendedToday[slot] > 0,
                $"school {slot} taught nobody, so the whole city walked to the other one."));
    }

    // ---- fixtures -------------------------------------------------------------------------------

    /// <summary>
    /// The Household the school took, which is the one whose Education went UP.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Read off the Need rather than off a Trip</b>, because the Need is what the occasion is
    /// for: <c>ServiceEngine.Serve</c> recovers on an attendance and <c>Fail</c> degrades on
    /// everything else, so in a one-place city exactly one applicant is still at the ideal.
    /// </para>
    /// <para>
    /// 🔴 <b>AT ZERO AND NOT ABOVE IT.</b> <c>RuleEngine.Write</c> clamps a Need at the ideal, which
    /// is <c>0</c> — ***the column is a depth below satisfaction and never a stock of it*** — so the
    /// family that attends every Day never rises, it simply never falls. ⚠ <b>And the applicant
    /// filter is load-bearing for the same reason</b>: a Household with no child has no occasion, so
    /// it sits at zero having never been asked.
    /// </para>
    /// </remarks>
    private static int Admitted(World world)
    {
        int found = -1;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot)
                || Child(world, slot) < 0
                || world.Households.Education[slot] < 0)
            {
                continue;
            }

            Assert.True(found < 0, "two Households attended a school holding one place.");
            found = slot;
        }

        Assert.True(found >= 0, "nobody attended.");

        return found;
    }

    /// <summary>
    /// Every Household with an occasion, and what the walk to this school costs it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Filtered on the rung and not on the box</b>, which is <c>ServiceEngine.Reach</c>'s own
    /// order: the box is a straight-line bound that over-supplies candidates, and the Commute Budget
    /// is the thing that actually refuses one.
    /// </remarks>
    private static List<(int Slot, TravelTime Cost)> Applicants(World world, int school)
    {
        List<(int Slot, TravelTime Cost)> found = [];
        var scratch = new WalkScratch();

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            int child = Child(world, slot);

            if (child < 0
                || !world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out int home))
            {
                continue;
            }

            TravelMode mode = world.ModeOf(child);

            TravelTime cost = WalkRouting.Cost(
                world.Roads, mode, world.AccessPoint(home, mode), world.AccessPoint(school, mode),
                world.Rules.Trips.CrossingCost, scratch);

            if (world.Rules.Trips.TryRung(cost, out _))
            {
                found.Add((slot, cost));
            }
        }

        return found;
    }

    /// <summary>The first child in this Household, or <c>-1</c> — <c>ServiceEngine.Traveller</c>'s rule.</summary>
    private static int Child(World world, int slot)
    {
        foreach (int member in world.Members.Walk(slot))
        {
            if (world.Citizens.Age[member] == 0)
            {
                return member;
            }
        }

        return -1;
    }

    private static List<int> Schools(World world)
    {
        List<int> found = [];

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.Kind[slot] == School)
            {
                found.Add(slot);
            }
        }

        return found;
    }

    private static int OnlySchool(World world)
    {
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.Kind[slot] == School)
            {
                return slot;
            }
        }

        Assert.Fail("no school stands.");
        return -1;
    }

    private static int FirstVacantLot(World world) => TwoVacantLots(world)[0];

    /// <summary>
    /// Two vacant Lots as far apart as this world has, so the two schools are two PLACES.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><see cref="TwoVacantLots"/> returns the first two in slot order, which are neighbours
    /// — and two schools on one Segment are one school for every purpose the ordering has.</b> The
    /// first spelling of <see cref="Both_schools_teach_somebody_when_neither_is_full"/> used it and
    /// went red against working code: every family's nearest school really was the same one, and a
    /// tie really does go to the lower Building slot. ***A test of where the city walks has to put
    /// somewhere else to walk to on the map.***
    /// </remarks>
    private static int[] FarApartVacantLots(World world)
    {
        int first = FirstVacantLot(world);
        int far = -1;
        long best = -1;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || !world.Lots.IsVacant(slot) || slot == first)
            {
                continue;
            }

            long east = world.Lots.East[slot].Raw - world.Lots.East[first].Raw;
            long north = world.Lots.North[slot].Raw - world.Lots.North[first].Raw;
            long apart = (east < 0 ? -east : east) + (north < 0 ? -north : north);

            if (apart > best)
            {
                best = apart;
                far = slot;
            }
        }

        Assert.True(far >= 0, "the generated city left fewer than two vacant Lots.");

        return [first, far];
    }

    /// <summary>Two vacant Lots, far enough apart in slot order to be different Buildings.</summary>
    private static int[] TwoVacantLots(World world)
    {
        List<int> vacant = [];

        for (int slot = 0; slot < world.Lots.Rows.SlotCount && vacant.Count < 2; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot))
            {
                vacant.Add(slot);
            }
        }

        Assert.True(vacant.Count == 2, "the generated city left fewer than two vacant Lots.");

        return [.. vacant];
    }

    private static void Place(Simulation simulation, World world, int lot)
    {
        Command command = Command.Service(world.Lots.East[lot], world.Lots.North[lot], School);

        simulation.Step(new TickInput([command], 0));
    }

    /// <summary>
    /// Steps until the attendance pass on Day <paramref name="day"/> has just run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The counters are per-Tick and are cleared at the top of every pass</b>, so a reading
    /// taken one Tick late is a reading of zero — and every assertion in this class is such a
    /// reading.
    /// </para>
    /// <para>
    /// 🔴 <b>The bound is <c>&lt;=</c> and not <c>&lt;</c>, which is the whole of what this helper
    /// gets right.</b> <c>Simulation.Step</c> runs the phases for the CURRENT Tick and calls
    /// <c>World.Advance</c> afterwards, so a loop that stops when the clock first READS the boundary
    /// has stopped one step before the pass on it. ***The first spelling of this did, and it turned
    /// six real assertions red against working code*** — the tell was
    /// <c>BuildingTable.AttendedToday</c> holding 48 while <c>ServiceEngine.Attended</c> held zero,
    /// which is a per-Day tally beside a per-Tick one and cannot both be true of one moment.
    /// </para>
    /// </remarks>
    private static void StepToDay(Simulation simulation, int day)
    {
        ulong target = (ulong)day * Ticks.PerDay;

        while (simulation.Tick.Raw <= target)
        {
            simulation.Step(default);
        }
    }

    private static string RepoRoot()
    {
        DirectoryInfo? here = new(AppContext.BaseDirectory);

        while (here is not null && !File.Exists(Path.Combine(here.FullName, "CLAUDE.md")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);

        return here!.FullName;
    }

    private static (World World, Simulation Simulation) City(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, result.Ruleset!, key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    private const string SchoolKind = """

        [[building]]
        name = "school"
        serves = "education"
        """;

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

    private const string BoughtNeeds = """

        [needs]
        sustenance_degrade   = 1
        sustenance_recover   = 1
        satisfaction_degrade = 1
        satisfaction_recover = 1
        floor = -1000
        """;

    /// <summary>A world with schools and no ceiling on them. Every shipped file but one.</summary>
    private const string Unbounded = Staged + LifeStages + SchoolKind + AttendedNeeds;

    /// <summary>
    /// The same world at the range's tightest rate, so every school holds exactly <b>one</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Rewritten into the ONE <c>[capacity]</c> table rather than appended as a second one</b>,
    /// which TOML refuses — so the two bounded fixtures differ from <see cref="Unbounded"/> in a
    /// single key and in nothing else.
    /// </remarks>
    private static readonly string Bounded = WithPlaceRate(1024);

    /// <summary>And at the loosest, so a school's places are its whole floor.</summary>
    private static readonly string Roomy = WithPlaceRate(1);

    /// <summary>Adds <c>floor_tiles_per_place</c> to the fixture's existing capacity table.</summary>
    private static string WithPlaceRate(int rate) =>
        Unbounded.Replace(
            "floor_tiles_per_parking_space = 6",
            $"floor_tiles_per_parking_space = 6\n        floor_tiles_per_place = {rate}",
            StringComparison.Ordinal);

    private const string Staged = """
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
        floor_tiles_per_occupant      = 6
        floor_tiles_per_job           = 1
        floor_tiles_per_parking_space = 6

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
