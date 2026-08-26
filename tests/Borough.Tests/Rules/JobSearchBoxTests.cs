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
    /// <b>The box is walked a fixed number of times per seeker, however wide it is</b> — so its width
    /// is a question about which Buildings are reachable rather than about what the pass costs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This asserted a <c>Stopwatch</c> ratio until 2026-08-22, and it was
    /// <c>plans/0003</c> hash-moving queue item 13.</b> It timed two runs and required the wider box to
    /// cost under <b>2.2×</b>, and ***its own comment predicted the failure rate it then had***: <i>"a
    /// band transplanted from a quieter quantity fails one run in ten with nothing wrong under it."</i>
    /// It failed about one run in ten. Under [`plans/0032`](../../../plans/0032-test-tiers.md)'s axis —
    /// <em>on the day it fails, do you find out what broke or paste in the new number?</em> — a
    /// wall-clock ratio is an <b>instrument</b>, and this one sat in the assertion tier wearing an
    /// assertion's clothes. <c>adr/0121</c>: <em>a quiet machine is a control on a capture</em>, so a
    /// test that takes a capture inside the gate has put a capture's controls on the gate.
    /// </para>
    /// <para>
    /// <b>What replaces it is a count, and the count is exact rather than banded.</b>
    /// <c>EmploymentActivity.BoxWalks</c> counts the walks: one per seeker to size the box, then one
    /// per candidate drawn. Measured at both ceilings on 2026-08-22, 40,000 Citizens: <b>33,277
    /// seekers, 133,108 walks, 4.00 per seeker</b> — <em>identical in both arms</em>, against boxes of
    /// <b>841</b> and <b>4,489</b> Cells. So growing the box <b>5.34×</b> adds no walks at all; what it
    /// scales is Cells-per-walk, which is geometry and derivable — <b>112M</b> against <b>598M</b>
    /// Cells visited, printed above for whoever wants the cost figure.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a narrower claim than the one it replaces, and deliberately.</b> The old assertion
    /// was about a <em>share of run time</em> — box walking at ~18% of the pass, failing if it reached
    /// ~28% — and no operation count can reproduce that, because a route search is a variable-cost
    /// search rather than one operation. What is asserted instead is the structural fact underneath it:
    /// <b>the pass walks the box a constant number of times per seeker, and box width does not move
    /// that constant.</b> ***A guard that fires on a real change every time beats one that fires on a
    /// bigger change nine times in ten.*** The time share remains measurable, by whoever wants it, on a
    /// quiet machine, as an instrument.
    /// </para>
    /// <para>
    /// Isolated with <c>[[building]] jobs = 0</c>: no candidate ever has a vacancy, so <c>TryEmploy</c>
    /// walks the box and never routes, and nobody is employed at either ceiling — which is what holds
    /// the seeker count equal across the two arms and makes them comparable at all.
    /// </para>
    /// <para>
    /// ⚠ <b>Two confounds were found in the timing version and are kept here because they are about
    /// measurement rather than about this test.</b> First, it once ran with
    /// <c>Simulation.VerifyDecideWritesNothing</c> left on — an <c>O(world)</c> fold twice a Tick,
    /// identical in both arms — so each arm read ~31 s of which ~29 s was the guard: guard on
    /// <b>1.11×</b>, guard off <b>1.84×</b>. ***A common term that does not move still moves a ratio, by
    /// diluting it.*** Second, an earlier comparison ran the shipped Ruleset at two ceilings, which
    /// changes the box <em>and</em> the number of failed walk searches, so the cheaper box bought more
    /// routing and the two nearly agreed. <b>A cost measured while a second term moves is not a cost of
    /// the first term.</b> Neither confound can reach a count.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_box_is_walked_a_fixed_number_of_times_however_wide_it_is()
    {
        _out.WriteLine("population   ceiling  radius  box(Cells)  seekers  walks  walks/seeker  cells");

        int walksAt20 = 0;
        int walksAt50 = 0;
        int boxAt20 = 0;
        int boxAt50 = 0;

        foreach (int ceiling in new[] { 20, 50 })
        {
            Ruleset rules = WithoutJobs(ceiling);

            Cells radius = EmploymentEngine.Radius(rules.Trips.CommuteBudget, rules.Roads.WalkSpeed);
            CellRect box = CellRect.At(new Cells(CellGrid.WorldCells / 2),
                new Cells(CellGrid.WorldCells / 2)).Dilate(radius).Clamp();

            EmploymentActivity pass = Run(40_000, rules).Employment.Drain();

            _out.WriteLine(
                $"{40_000,-12} {ceiling,-8} {radius.Raw,-7} {box.Count,-11} {pass.Seeking.Sum,-8} "
                + $"{pass.BoxWalks.Sum,-6} "
                + $"{(double)pass.BoxWalks.Sum / pass.Seeking.Sum:F2}          "
                + $"{pass.BoxWalks.Sum * (long)box.Count}");

            Assert.True(pass.Seeking.Sum > 0, "nobody sought work, so nothing was walked.");

            // One walk to size the box, then one per candidate drawn. This is the whole claim: box
            // width is a question about which Buildings are reachable, not about how much the pass
            // does. A walk added anywhere -- a second CountIn, a retry, a candidate loop that sizes
            // the box each time round -- moves this off the nose and says exactly what it cost.
            Assert.Equal(
                pass.Seeking.Sum * (1 + rules.Jobs.Candidates),
                pass.BoxWalks.Sum);

            if (ceiling == 20)
            {
                walksAt20 = (int)pass.BoxWalks.Sum;
                boxAt20 = box.Count;
            }
            else
            {
                walksAt50 = (int)pass.BoxWalks.Sum;
                boxAt50 = box.Count;
            }
        }

        // The arms must actually differ, or everything above passes for the wrong reason.
        Assert.True(
            boxAt50 > boxAt20 * 4,
            $"the two ceilings gave boxes of {boxAt20} and {boxAt50} Cells, which is not the 5.34x "
            + "spread this test compares across. The radius derivation moved.");

        Assert.Equal(walksAt20, walksAt50);
    }

    /// <summary>
    /// <b>On the golden fixture essentially every accepted commute is <see cref="CommuteRung.Fast"/>
    /// and none is refused</b>, which is what a city barely larger than a commute looks like from
    /// inside the instrument.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Measured 2026-08-14 at the shipped ceiling: 2,307 employed, <b>2,307 / 0 / 0</b> across the
    /// three rungs, <c>beyond</c> <b>0</b>. <b>The three-rung instrument reports one value on the
    /// world the baseline commits</b>, so a change that broke the grading entirely would not move
    /// this fixture. <see cref="EmploymentRungTests"/> covers the far end of that ladder; this
    /// states the near end, so the pair is visible from one place.
    /// </para>
    /// <para>
    /// ⚠ <b>IT SAID *EVERY* COMMUTE UNTIL 2026-08-25, AND `adr/0165`'s LAND-USE SPLIT MOVED IT BY
    /// ONE.</b> Re-measured at milestone 26 task 2: <b>2,294 employed, 2,293 / 1 / 0</b>,
    /// <c>beyond</c> still <b>0</b>. The cause is not the commercial land itself but the ground the
    /// city needed to absorb it — <see cref="SyntheticCity"/>'s extent compensation steps the
    /// lattice up by one block-ring, and one commute at the far corner crossed the 20-minute rung.
    /// ***The city stopped being strictly smaller than a commute***, which is a real property this
    /// fixture used to have and no longer does.
    /// </para>
    /// <para>
    /// <b>What is still asserted exactly, and why the softened one is the right half to soften.</b>
    /// <c>beyond</c> and <see cref="CommuteRung.Unsavoury"/> stay pinned at zero:
    /// <c>adr/0095</c> is explicit that <em>only the ceiling refuses</em> and the rungs below it
    /// grade a commute that happens anyway, so a refusal appearing here would be a different claim
    /// entirely and this test would still catch it on the nose. What is relaxed is the
    /// fast-against-moderate line, to <b>99% of accepted commutes Fast</b> — which one crossing in
    /// 2,294 clears by a factor of twenty, and which a grading that had actually broken would miss
    /// by far more. ⚠ <b>A bound and not a baseline</b>: do not re-pin it to the measured 1, because
    /// the count moves with the world and pinning it would make every unrelated world change land
    /// here.
    /// </para>
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

        // The refusal and the worst rung, pinned. adr/0095: only the ceiling refuses.
        Assert.Equal(0, shipped.Unsavoury.Sum);
        Assert.Equal(0, shipped.Beyond.Sum);

        // And the near end as a bound rather than a baseline -- see the remarks for why this is the
        // half that gives, and why re-pinning it to the measured count would be wrong.
        Assert.True(
            shipped.Fast.Sum >= shipped.Employed.Sum - (shipped.Employed.Sum / 100),
            $"{shipped.Fast.Sum} of {shipped.Employed.Sum} accepted commutes were fast, with "
            + $"{shipped.Moderate.Sum} moderate. This fixture is a city barely larger than a "
            + "commute, so a moderate commute is the far corner and a crowd of them is the grading "
            + "having moved or the city having outgrown its Commute Budget.");
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

            if (!simulation.Employment.Home(slot, TravelMode.Foot, out Cells east, out Cells north, out _))
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

        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

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
    /// <remarks>
    /// ⚠ <b>The Shift-start band has to go with the posts</b>, because <c>adr/0101</c>'s loader
    /// refusal is two-way: a kind that employs nobody must not state when its jobs begin. Deleting
    /// only the <c>jobs</c> line refuses at the door, which is the pairing working rather than an
    /// awkwardness — the band and the ceiling are one declaration in two lines.
    /// </remarks>
    private static Ruleset WithoutJobs(int ceiling) => Edit(
        ceiling,
        ("jobs = 8", "jobs = 0"),
        ("shift_start_earliest_hour = 6\nshift_start_latest_hour   = 10", string.Empty));

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
