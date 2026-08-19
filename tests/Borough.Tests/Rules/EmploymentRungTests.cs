using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>adr/0095</c>: the Commute Budget's three rungs, and the two facts about them that are worth
/// holding to a test rather than to a paragraph.
/// </summary>
/// <remarks>
/// <para>
/// <b>The second test asserts a <em>negative</em> and states the population at which it stops being
/// true</b>, which is 5b-bis task 4's precedent and the reason this file exists at all. That task
/// wrote down <i>the Budget refuses nothing on a world this small</i> so the fact could not rot; three
/// days later <c>plans/0003</c> item 6 raised the fixture's population for an unrelated reason and the
/// assertion failed, which is the only way anybody found out that the committed baseline had started
/// reaching a branch it had never reached. <b>A negative nobody wrote down is a change nobody can
/// see.</b>
/// </para>
/// <para>
/// ⚠ <b>The rungs report <em>Separation</em> and nothing else, so a test at scale is evidence about
/// one of two mechanisms.</b> <c>01 §4</c> names two scarcities that read as long commutes —
/// Congestion, which is road capacity, and Separation, which is distance — and a walk Leg cannot
/// carry the first by construction (<c>03 §3.7</c>). So the test below is named for the driver it
/// exercises: <b>a bigger city spreads, and spreading is what moves these counters today.</b>
/// </para>
/// </remarks>
public sealed class EmploymentRungTests
{
    private const int Ticks = 1_024;

    private const int HashEvery = 1_024;

    /// <summary>
    /// <b><c>employed</c> is exactly the three rungs summed, which is a free consistency check on the
    /// instrument.</b>
    /// </summary>
    /// <remarks>
    /// A Census counter is an instrument rather than state, so a derivable reading is not the
    /// duplication <c>adr/0064</c> warns about. Keeping <c>employed</c> alongside the three rungs buys
    /// this assertion: an assignment that took a job without landing on a rung, or landed on two,
    /// breaks it — and both are the shape of defect a <see cref="CommuteRung"/> comparison could
    /// introduce silently.
    /// </remarks>
    [Fact]
    public void Employed_is_exactly_the_three_rungs_summed()
    {
        EmploymentActivity activity = Assign(GoldenFixtures.Population);

        Assert.True(activity.Employed.Sum > 0, "nobody was employed, so nothing was checked.");
        Assert.Equal(
            activity.Employed.Sum,
            activity.Fast.Sum + activity.Moderate.Sum + activity.Unsavoury.Sum);
    }

    /// <summary>
    /// ⚠ <b>The unsavoury rung is unoccupied in a city this small, and occupying it takes ten times
    /// the population.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is a property of the cities this project can build, not of the numbers.</b> The paved
    /// extent is derived from population (<c>SyntheticCity.PavedTiles</c>), so a 4,000-Citizen city is
    /// 1.3 km across and a 10,000-Citizen one 1.9 km, against a fifty-minute walking ceiling that
    /// reaches 4.2 km. <b>Nobody can live far enough from work to be graded badly.</b> Measured over
    /// 1,024 Ticks on the shipped Ruleset, the unsavoury count runs 0, 0, 10, 131, 738 at 10,000,
    /// 20,000, 40,000, 80,000 and 160,000 Citizens.
    /// </para>
    /// <para>
    /// <b>The fixture is not inflated to fill the rung and the rung values are not lowered to fit the
    /// fixture.</b> A rung is a <em>vocabulary</em>: calling a twenty-minute commute <i>unsavoury</i>
    /// would bend the words to fit a village, and the words are the whole mechanism. What this test
    /// does instead is state both ends of the ladder so that a change to either — a bigger fixture, a
    /// denser generator, a slower walk — is visible the day it lands.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_unsavoury_rung_is_empty_in_a_small_city_and_occupies_at_ten_times_the_population()
    {
        EmploymentActivity small = Assign(GoldenFixtures.Population);

        Assert.Equal(0, small.Unsavoury.Sum);

        EmploymentActivity large = Assign(GoldenFixtures.Population * 10);

        Assert.True(
            large.Unsavoury.Sum > 0,
            "no commute in a city ten times the golden fixture reaches the unsavoury rung, so the top "
            + "band is unreachable in every world this suite can build. Either the paved extent "
            + "shrank, the walk got faster, or the ceiling rose.");

        Assert.True(
            large.Moderate.Sum > small.Moderate.Sum,
            "a city ten times the size grades no more commutes as moderate, so spreading is not "
            + "moving the rungs and the grading is measuring something else.");
    }

    /// <summary>
    /// <b>A Ruleset whose ceiling nobody can exceed puts every commute on the fast rung</b>, which is
    /// the control for the test above.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Without it, <c>unsavoury == 0</c> on the small city is consistent with the rung never being
    /// written at all. This asserts the counters are wired by moving the <em>rungs</em> rather than
    /// the city: with the fast edge at one minute and the ceiling unchanged, the same world that
    /// reported nothing but fast reports moderate and unsavoury instead.
    /// </para>
    /// <para>
    /// ⚠ <b>And employment itself moves, which <c>adr/0095</c> says it should not.</b> That ADR's
    /// table gives the fast rung <i>"nothing"</i> and the moderate rung <i>"nothing mechanically"</i>,
    /// on the ground that only the ceiling refuses. <b>The early exit makes that false</b>: the search
    /// stops on the first <see cref="CommuteRung.Fast"/> candidate it draws, so where the fast edge
    /// sits decides how many candidates get looked at, which decides who takes which vacancy. Measured
    /// here as <b>2,307 against 2,301</b> employed on an identical city — small, real, and hash-bearing.
    /// The assertion is therefore <em>near</em> rather than <em>equal</em>, and the tolerance is what
    /// records the size of the effect.
    /// </para>
    /// </remarks>
    [Fact]
    public void Moving_the_rungs_moves_the_counters_on_a_city_that_did_not_change()
    {
        EmploymentActivity shipped = Assign(GoldenFixtures.Population);
        EmploymentActivity graded = Assign(GoldenFixtures.Population, WithRungs(1, 2, 50));

        Assert.True(shipped.Fast.Sum > 0);
        Assert.Equal(0, shipped.Unsavoury.Sum);

        Assert.True(
            graded.Unsavoury.Sum > 0,
            "with the fast edge at one minute and the ceiling unchanged, the same commutes are still "
            + "accepted and none of them is graded worse -- so the rung is not being read.");

        // Not equal: the fast edge governs the early exit, so moving it changes how many candidates
        // are drawn and therefore which vacancy each seeker lands on. Within a percent is what that
        // second-order effect is worth on this fixture; a larger gap means the rungs have stopped
        // being a grading and started being a search policy.
        long difference = Math.Abs(shipped.Employed.Sum - graded.Employed.Sum);

        Assert.True(
            difference * 100 < shipped.Employed.Sum,
            $"regrading moved employment by {difference} of {shipped.Employed.Sum}, which is more "
            + "than the early exit alone should account for.");
    }

    private static EmploymentActivity Assign(int population, Ruleset? rules = null)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, rules ?? GoldenFixtures.Rules());

        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

        Replay.Trace(simulation, log, new Ticks(Ticks), HashEvery, []);

        return simulation.Employment.Drain();
    }

    /// <summary>The shipped Ruleset with all three rungs replaced.</summary>
    private static Ruleset WithRungs(int fast, int moderate, int ceiling)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        (string Key, string Replacement)[] keys =
        [
            ("commute_fast_minutes = 20", $"commute_fast_minutes = {fast}"),
            ("commute_moderate_minutes = 40", $"commute_moderate_minutes = {moderate}"),
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
