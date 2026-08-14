using System.Diagnostics;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// <b>What the job-search box actually covers, measured against the city it searches.</b>
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EmploymentEngine"/> draws job candidates from a box of Cells around home, sized from the
/// Commute Budget's ceiling. Its header explains why that size is <em>derived</em> rather than
/// authored, citing S2 R4: <i>"an unbounded or over-wide draw is S2 R4's uniform origin-destination
/// distribution, which R4 measured to be a different city"</i>. <b>That reads as a claim that the box
/// keeps the draw local, and this file exists because it does not.</b> Measured 2026-08-14: the box is
/// <b>44.9×</b> the golden fixture's city by area and holds <b>100.0%</b> of the Buildings in the
/// world, at 4,000, 10,000 and 40,000 Citizens alike.
/// </para>
/// <para>
/// <b>The cause is that the cities are smaller than a commute, and it is not a defect in the
/// derivation.</b> A city's width is roughly <c>sqrt(population ÷ 3700)</c> km, so 4,000 Citizens
/// occupy 1.28 km against the 4.17 km a fifty-minute walk covers. Nothing this project builds is wide
/// enough for the bound to bite until about 160,000 Citizens. <b>It is the same shape as
/// <c>foot_crossing_every</c></b>, which milestone 5a found correct and inert below a threshold of its
/// own — and the same lesson as every unit cost in <c>plans/0013</c>: <b>a bound measured on a fixture
/// is a bound measured on a village.</b>
/// </para>
/// <para>
/// ⚠ <b>And the inertness is structural rather than temporary.</b> The radius is a time times a speed,
/// and the speed is walking only because walking is all that exists. At 50 km/h the same ceiling
/// reaches 41.7 km, so a 1M city 19.2 km across sits well inside it. <b>A box derived from a commute
/// time can only ever filter in a foot-only world</b>, so this reading gets <em>worse</em> when
/// vehicles land, not better. The design question that opens is <c>plans/0002</c> §C; the same mode
/// confusion in the map's own sizing is <c>adr/0089</c>'s third correction and <c>plans/0012</c>
/// <i>Cause 5</i>.
/// </para>
/// <para>
/// <b>Every number here is read out of the production derivation rather than recomputed.</b>
/// <see cref="EmploymentEngine.Radius"/>, <see cref="EmploymentEngine.Home"/> and
/// <see cref="SyntheticCity.PavedTiles"/> are <c>internal</c> for this file's benefit, so a change to
/// any of them moves these readings instead of leaving them agreeing with a stale copy of the old
/// arithmetic. <b>A measurement that re-derives what it is measuring is <c>plans/0012</c> <i>Cause
/// 1</i> with extra steps</b> — and the whole point of this file is that the box's <em>description</em>
/// went on being true after the thing it described stopped being.
/// </para>
/// <para>
/// <b>The assertions state both ends of the ladder rather than the reading that happens to hold
/// today</b>, which is <see cref="EmploymentRungTests"/>'s precedent and the reason that file caught a
/// fixture change nobody announced. A test asserting only <i>the box covers everything</i> would pass
/// for ever, including on the day somebody fixed it.
/// </para>
/// </remarks>
public sealed class JobSearchBoxTests
{
    private const int Ticks = 1_024;

    private const int HashEvery = 1_024;

    /// <summary>The population at which the box measurably stops holding the whole city.</summary>
    private const int FiltersAt = 160_000;

    private readonly ITestOutputHelper _out;

    public JobSearchBoxTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// <b>The box is larger than the city it searches, and stays larger until the city outgrows a
    /// walk.</b>
    /// </summary>
    /// <remarks>
    /// Measured 2026-08-14 — box ÷ city by area: <b>44.9×</b> at 4,000, 20.0× at 10,000, 5.0× at
    /// 40,000, 1.2× at 160,000, <b>0.2×</b> at 1,000,000. The crossing is what makes the bound real at
    /// target scale and inert on every fixture, so both ends are asserted.
    /// </remarks>
    [Fact]
    public void The_box_is_larger_than_the_city_it_searches()
    {
        Ruleset rules = GoldenFixtures.Rules();

        Cells radius = EmploymentEngine.Radius(rules.Trips.CommuteBudget, rules.Roads.WalkSpeed);
        CellRect box = CellRect.At(new Cells(radius.Raw), new Cells(radius.Raw))
            .Dilate(radius).Clamp();

        _out.WriteLine($"radius   {radius.Raw} Cells");
        _out.WriteLine($"box      {box.Width.Raw}x{box.Height.Raw} = {box.Count} Cells");
        _out.WriteLine($"map      {CellGrid.WorldCells} Cells a side");
        _out.WriteLine(string.Empty);
        _out.WriteLine("population   paved(Tiles)  city(Cells^2)  box/city");

        long atFixture = 0;
        long atTarget = 0;

        foreach (int population in
            new[] { GoldenFixtures.Population, 10_000, 40_000, FiltersAt, 1_000_000 })
        {
            World world = Build(population, rules).World;

            int pavedTiles = SyntheticCity.PavedTiles(world);
            int pavedCells = (pavedTiles + CellGrid.TilesPerCell - 1) / CellGrid.TilesPerCell;
            long cityCells = (long)pavedCells * pavedCells;

            _out.WriteLine(
                $"{population,-12} {pavedTiles,-13} {cityCells,-14} "
                + $"{(double)box.Count / cityCells:F1}x");

            if (population == GoldenFixtures.Population)
            {
                atFixture = cityCells;
            }

            if (population == 1_000_000)
            {
                atTarget = cityCells;
            }
        }

        Assert.True(
            box.Count > atFixture * 10,
            $"the job-search box ({box.Count} Cells) is no longer an order of magnitude larger than the "
            + $"golden fixture's city ({atFixture} Cells). Either the fixture grew, the ceiling fell or "
            + "the walk got slower -- and EmploymentEngine's header and plans/0002 §C both describe a "
            + "world in which it is.");

        Assert.True(
            box.Count < atTarget,
            $"the box ({box.Count} Cells) now covers a whole 1M city ({atTarget} Cells), so the bound is "
            + "inert at target scale as well as on the fixtures. That is the reading adr/0089's third "
            + "correction predicts for the day a mode faster than walking exists -- if nothing has "
            + "changed about modes, something has changed about the map or the density instead.");
    }

    /// <summary>
    /// <b>Every seeker sees every Building in the city, until about 160,000 Citizens.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reading that matters, and the one <see cref="EmploymentEngine"/>'s header implies is false.
    /// Measured 2026-08-14, seekers holding the whole city in their box: <b>100.0%</b> at 4,000, 10,000
    /// and 40,000; <b>11.7%</b> at 160,000, mean 10,787 Buildings of 13,411.
    /// </para>
    /// <para>
    /// <b>The spread column is why this was misdiagnosed once, and is kept for that reason.</b> Before
    /// <c>plans/0003</c> item 6 the populator paved the whole map and laid Buildings along the first
    /// Lots in row-major order, so the city was a ribbon <b>2 Cells tall</b> and up to 124 long. The box
    /// clipped its length, which looked like a filter working and was an artefact of a degenerate shape
    /// — and at the 1,000-Citizen fixture that shipped with the mechanism, locality was already
    /// <b>100.0%</b>. <b>So the premise was false when it was written</b>, and item 6 gave the city a
    /// better shape while making the bound wider relative to it.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_box_holds_the_whole_city_until_the_city_outgrows_a_walk()
    {
        _out.WriteLine(
            "population   buildings  seekers  in-box(min/mean/max)  whole-city  spread(Cells)");

        double atFixture = Locality(GoldenFixtures.Population);

        Locality(40_000);

        double atFiltering = Locality(FiltersAt);

        Assert.Equal(100.0, atFixture, 1);

        Assert.True(
            atFiltering < 100.0,
            $"at {FiltersAt:N0} Citizens every seeker still holds the whole city in its box "
            + $"({atFiltering:F1}%), so the bound is inert at every population this suite can reach. "
            + "The threshold moved -- check the ceiling, the walk speed and SyntheticCity.PavedTiles.");
    }

    /// <summary>
    /// <b>The box is not where the pass spends its time</b>, so its width is a behavioural question
    /// rather than a cost one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Isolated with <c>[[building]] jobs = 0</c>: no candidate ever has a vacancy, so <c>TryEmploy</c>
    /// walks the box and never routes, and nobody is employed at either ceiling, which holds the seeker
    /// count equal. Measured 2026-08-14 — a <b>5.34×</b> box costs <b>4.7%</b> at 4,000 Citizens and
    /// <b>6.1%</b> at 40,000.
    /// </para>
    /// <para>
    /// ⚠ <b>The comparison this replaces was confounded, and the confound cancelled rather than
    /// added.</b> Running the shipped Ruleset at two ceilings changes the box <em>and</em> the number of
    /// failed walk searches — <c>beyond</c> is 26,131 against 935 at 40,000 — so the cheaper box bought
    /// more routing and the two nearly agreed. <b>A cost measured while a second term moves is not a
    /// cost of the first term</b>: <c>adr/0073</c>'s corollary, on a measurement rather than on a
    /// primitive.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_box_is_not_where_the_pass_spends_its_time()
    {
        _out.WriteLine("population   ceiling  radius  box(Cells)  run(ms)");

        double narrow = 0;
        double wide = 0;
        long narrowBox = 0;
        long wideBox = 0;

        foreach (int ceiling in new[] { 20, 50 })
        {
            Ruleset rules = WithoutJobs(ceiling);

            Cells radius = EmploymentEngine.Radius(rules.Trips.CommuteBudget, rules.Roads.WalkSpeed);
            CellRect box = CellRect.At(new Cells(CellGrid.WorldCells / 2),
                new Cells(CellGrid.WorldCells / 2)).Dilate(radius).Clamp();

            // Once to warm, once to read. Same process, same machine, same seeker count -- so the
            // ratio survives a slow CI box where an absolute would not.
            Run(40_000, rules);

            var clock = Stopwatch.StartNew();
            Run(40_000, rules);
            clock.Stop();

            _out.WriteLine(
                $"{40_000,-12} {ceiling,-8} {radius.Raw,-7} {box.Count,-11} "
                + $"{clock.Elapsed.TotalMilliseconds:F0}");

            if (ceiling == 20)
            {
                narrow = clock.Elapsed.TotalMilliseconds;
                narrowBox = box.Count;
            }
            else
            {
                wide = clock.Elapsed.TotalMilliseconds;
                wideBox = box.Count;
            }
        }

        double boxRatio = (double)wideBox / narrowBox;
        double costRatio = wide / narrow;

        _out.WriteLine($"box x{boxRatio:F2}, cost x{costRatio:F2}");

        Assert.True(
            costRatio < 1.5,
            $"growing the box {boxRatio:F2}x cost {costRatio:F2}x, so walking the box has become a "
            + "material share of the pass. It was 1.06x when this was written, which is what made the "
            + "box's width a question about behaviour rather than about the Tick budget.");
    }

    /// <summary>
    /// <b>On the golden fixture every accepted commute is <see cref="CommuteRung.Fast"/> and none is
    /// refused</b>, which is what a city smaller than a commute looks like from inside the instrument.
    /// </summary>
    /// <remarks>
    /// Measured 2026-08-14 at the shipped ceiling: 2,307 employed, <b>2,307 / 0 / 0</b> across the three
    /// rungs, <c>beyond</c> <b>0</b>. <b>The three-rung instrument reports one value on the world the
    /// baseline commits</b>, so a change that broke the grading entirely would not move this fixture.
    /// <see cref="EmploymentRungTests"/> covers the far end of that ladder; this states the near end, so
    /// the pair is visible from one place.
    /// </remarks>
    [Fact]
    public void Every_commute_on_the_golden_fixture_is_fast_and_none_is_refused()
    {
        EmploymentActivity shipped = Run(GoldenFixtures.Population, GoldenFixtures.Rules())
            .Employment.Drain();

        _out.WriteLine(
            $"considered {shipped.Considered.Sum}, seeking {shipped.Seeking.Sum}, "
            + $"employed {shipped.Employed.Sum}, beyond {shipped.Beyond.Sum}, "
            + $"{shipped.Fast.Sum}/{shipped.Moderate.Sum}/{shipped.Unsavoury.Sum}");

        Assert.True(shipped.Employed.Sum > 0, "nobody was employed, so nothing was checked.");

        Assert.Equal(shipped.Employed.Sum, shipped.Fast.Sum);
        Assert.Equal(0, shipped.Moderate.Sum);
        Assert.Equal(0, shipped.Unsavoury.Sum);
        Assert.Equal(0, shipped.Beyond.Sum);
    }

    /// <summary>Share of seekers whose box holds every Building in the world, as a percentage.</summary>
    private double Locality(int population)
    {
        Simulation simulation = Run(population, GoldenFixtures.Rules());
        World world = simulation.World;

        Cells radius = EmploymentEngine.Radius(
            world.Rules.Trips.CommuteBudget, world.Rules.Roads.WalkSpeed);

        int live = world.Buildings.Rows.LiveCount;
        int seekers = 0;
        int whole = 0;
        long sum = 0;
        int min = int.MaxValue;
        int max = 0;

        int eastMin = int.MaxValue, eastMax = 0, northMin = int.MaxValue, northMax = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            if (!simulation.Employment.Home(slot, out Cells east, out Cells north, out _))
            {
                continue;
            }

            CellRect box = CellRect.At(east, north).Dilate(radius).Clamp();
            int inBox = world.BuildingsInCells.CountIn(box);

            seekers++;
            sum += inBox;
            min = Math.Min(min, inBox);
            max = Math.Max(max, inBox);

            eastMin = Math.Min(eastMin, east.Raw);
            eastMax = Math.Max(eastMax, east.Raw);
            northMin = Math.Min(northMin, north.Raw);
            northMax = Math.Max(northMax, north.Raw);

            if (inBox == live)
            {
                whole++;
            }
        }

        double mean = seekers == 0 ? 0 : (double)sum / seekers;
        double share = seekers == 0 ? 0 : 100.0 * whole / seekers;

        _out.WriteLine(
            $"{population,-12} {live,-10} {seekers,-8} "
            + $"{(min == int.MaxValue ? 0 : min)}/{mean:F0}/{max,-12} {share:F1}%       "
            + $"{eastMax - eastMin + 1}x{northMax - northMin + 1}");

        return share;
    }

    /// <summary>A world with the Ruleset adopted and nothing stepped — Tick 0, as populated.</summary>
    private static Simulation Build(int population, Ruleset rules) =>
        Replay.Start(Log(population), rules);

    private static Simulation Run(int population, Ruleset rules)
    {
        InputLog log = Log(population);
        Simulation simulation = Replay.Start(log, rules);

        Replay.Trace(simulation, log, new Ticks(Ticks), HashEvery, []);

        return simulation;
    }

    private static InputLog Log(int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        return builder.Build();
    }

    /// <summary>The ceiling moved and every job removed, so the pass samples and never routes.</summary>
    private static Ruleset WithoutJobs(int ceiling) => Edit(ceiling, ("jobs = 8", "jobs = 0"));

    private static Ruleset Edit(int ceiling, params (string Key, string Replacement)[] extra)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        (string Key, string Replacement)[] keys =
        [
            .. extra,
            ("commute_fast_minutes = 20", $"commute_fast_minutes = {ceiling / 5}"),
            ("commute_moderate_minutes = 40", $"commute_moderate_minutes = {ceiling / 2}"),
            ("commute_budget_minutes = 50", $"commute_budget_minutes = {ceiling}"),
        ];

        foreach ((string key, string replacement) in keys)
        {
            Assert.Contains(key, toml, StringComparison.Ordinal);
            toml = toml.Replace(key, replacement, StringComparison.Ordinal);
        }

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
