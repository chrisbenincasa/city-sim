using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Space;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>adr/0081</c>: the commute is the first Trip generator, and a job is taken by satisficing on
/// distance.
/// </summary>
/// <remarks>
/// <para>
/// <b>The fixture is the shipped Ruleset and the shipped populator, which is the whole point.</b>
/// What is under test is not that a search terminates — it is that a city's own geography decides who
/// works where. A hand-built world with two Buildings and a Segment between them would answer a
/// question about <c>WalkRouting</c>, which <c>WalkRoutingTests</c> already answers, and would answer
/// nothing at all about the distribution this milestone exists to produce.
/// </para>
/// <para>
/// <b>No coordinate here is a literal</b>, on <c>TripCommandTests</c>' reasoning: where the populator
/// puts Buildings is a property of the world seed and of <c>[roads]</c>, and a test that hard-coded
/// one would fail as a refusal the day either moved.
/// </para>
/// </remarks>
public sealed class JobAssignmentTests
{
    /// <summary>
    /// Long enough for several passes at <c>interval = 32</c>, and short enough to stay a unit test.
    /// </summary>
    private const int Ticks = 512;

    /// <summary>As rarely as <see cref="Replay.Trace"/> permits: nothing here reads a State Hash.</summary>
    private const int HashEvery = 1_024;

    private static Simulation Run(Ruleset rules, int ticks = Ticks)
    {
        InputLog log = Log();
        Simulation simulation = Replay.Start(log, rules);

        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

        Replay.Trace(simulation, log, new Ticks((ulong)ticks), HashEvery, []);

        return simulation;
    }

    private static InputLog Log()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        return builder.Build();
    }

    /// <summary>How many Citizens hold a Workplace that still stands.</summary>
    private static int Employed(World world)
    {
        int total = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                total++;
            }
        }

        return total;
    }

    /// <summary>The golden Ruleset with its <c>[jobs]</c> section taken out.</summary>
    /// <remarks>
    /// <b>It excises the section rather than truncating the file at it, and the change was forced by
    /// a guard that worked.</b> This helper used to cut everything from <c>[jobs]</c> to the end and
    /// assert that nothing followed — correct only while <c>[jobs]</c> happened to be the last section
    /// in <c>minimal.toml</c>, which milestone 7 task 2 ended by appending <c>[parking]</c>. The
    /// assertion fired rather than the fixture silently losing a second table, which is the whole
    /// value of having written it. ***A fixture that depends on a file's section order is depending on
    /// something no document promises***, so this now names the section it removes.
    /// </remarks>
    private static Ruleset WithoutJobs()
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        int marker = toml.IndexOf("\n[jobs]", StringComparison.Ordinal);

        Assert.True(marker > 0, "the golden Ruleset no longer declares a [jobs] table.");

        // The next section header at column 0, which is where [jobs] ends. Searching from past the
        // marker's own newline so the header itself is not the match.
        int next = toml.IndexOf("\n[", marker + 2, StringComparison.Ordinal);
        string without = next < 0 ? toml[..marker] : toml[..marker] + toml[next..];

        // The header line, not the text: minimal.toml's prose names [jobs] in other sections'
        // comments, so a substring check finds a table that is no longer declared.
        Assert.DoesNotContain(
            without.Split('\n'),
            line => string.Equals(line.Trim(), "[jobs]", StringComparison.Ordinal));

        RulesetLoadResult result = RulesetLoader.Parse(without, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    // ---- the mechanism ---------------------------------------------------------------------------

    /// <summary>
    /// <b>A Citizen with no Workplace acquires one, and until this pass existed nothing in the
    /// simulation could give them one.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for <c>adr/0081</c>. Task 2 deleted <c>SyntheticCity</c>'s workplace
    /// stride — the only writer of <c>CitizenTable.Workplace</c> there had ever been, and one that
    /// handed out 1,000 jobs no Ruleset granted — so between task 2 and this line a Citizen's
    /// Workplace was written by nothing at all.
    /// </remarks>
    [Fact]
    public void A_citizen_with_no_workplace_takes_one()
    {
        Simulation simulation = Run(GoldenFixtures.Rules());
        EmploymentActivity activity = simulation.Employment.Drain();

        Assert.True(Employed(simulation.World) > 0);
        Assert.True(activity.Employed.Sum > 0);
        Assert.True(activity.Seeking.Sum >= activity.Employed.Sum);
        Assert.True(activity.Considered.Sum >= activity.Seeking.Sum);
    }

    /// <summary>
    /// <b>Every Workplace anybody holds is within the Commute Budget on foot.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The claim the whole milestone rests on, checked against the Road Graph rather than against
    /// the search box.</b> The box is a straight-line bound and the Budget is a network cost, so a
    /// Building inside the box may be a long way round; asserting only that the box was respected
    /// would pass a pass that never routed at all.
    /// </para>
    /// <para>
    /// It is re-routed here rather than remembered, which also makes it a check that the walk is
    /// <em>stable</em>: the cost is recomputed after several hundred Ticks of construction and
    /// demolition, so a route that only existed at the moment of hiring would fail this.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_workplace_is_inside_the_commute_budget_on_foot()
    {
        Simulation simulation = Run(GoldenFixtures.Rules());

        World world = simulation.World;
        TripRuleset trips = world.Rules.Trips;
        WalkScratch scratch = new();
        int checkedPairs = 0;

        Assert.True(trips.HasCommuteBudget, "the golden Ruleset states no Commute Budget.");

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            // Two hops since adr/0141. The Budget is a claim about a *walk*, and a Business has no
            // location of its own — so what is priced is the premises it borrows, and an unpremised
            // employer is skipped rather than counted: there is no walk to refuse.
            if (!world.Citizens.Rows.IsLive(slot)
                || !world.Businesses.Rows.TryResolve(
                    world.Citizens.Workplace[slot], out int employer)
                || !world.Buildings.Rows.TryResolve(
                    world.Businesses.Building[employer], out int work))
            {
                continue;
            }

            // A dwelling demolished under a worker is the severed case, not a violation: the Citizen
            // is homeless and re-enters the housing queue, and their Workplace outlives the move.
            if (!world.Households.Rows.TryResolve(
                    world.Citizens.HouseholdOf[slot], out int household)
                || !world.Buildings.Rows.TryResolve(
                    world.Households.Dwelling[household], out int home))
            {
                continue;
            }

            TravelTime cost = WalkRouting.Cost(
                world.Roads, TravelMode.Foot,
                world.PedestrianAccessPoint(home),
                world.PedestrianAccessPoint(work),
                trips.CrossingCost,
                scratch);

            Assert.True(
                trips.WithinBudget(cost),
                $"citizen {slot} walks {cost.Raw} to work against a Budget of "
                + $"{trips.CommuteBudget.Raw}.");

            checkedPairs++;
        }

        Assert.True(checkedPairs > 0, "nobody was employed, so nothing was checked.");
    }

    /// <summary>
    /// <b>Nobody is employed past the ceiling their workplace's kind declares.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0068</c> and <c>adr/0081</c> meeting, exactly as <c>adr/0068</c> and <c>adr/0069</c>
    /// meet in <c>PlacementTests</c>. The declared <c>jobs</c> count is what makes assignment stop,
    /// and it is the only thing that does — there is no other cap anywhere in the pass.
    /// </remarks>
    [Fact]
    public void Nobody_is_employed_past_the_declared_ceiling()
    {
        Simulation simulation = Run(GoldenFixtures.Rules());
        World world = simulation.World;

        // Over Businesses rather than Buildings since adr/0141: the ceiling is declared by the trade
        // and the worker list hangs off the Business, so a walk over Buildings here would be asking
        // the premises a question only the tenant can answer.
        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot)
                || !world.TryDeclaredJobs(world.Businesses.Kind[slot], out int jobs))
            {
                continue;
            }

            Assert.True(
                world.Workers.Length(slot) <= jobs,
                $"business {slot} holds {world.Workers.Length(slot)} workers against {jobs} jobs.");
        }
    }

    /// <summary>
    /// <b>A Ruleset with no <c>[jobs]</c> table employs nobody, and says nothing happened.</b>
    /// </summary>
    /// <remarks>
    /// <c>[placement]</c>'s polarity, and the assertion pairs with the one above it: the counters must
    /// be zero as well as the outcome, because a pass that ran and found nothing and a pass that never
    /// ran are two different cities and only the counters tell them apart.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_jobs_table_employs_nobody()
    {
        Simulation simulation = Run(WithoutJobs());
        EmploymentActivity activity = simulation.Employment.Drain();

        Assert.Equal(0, Employed(simulation.World));
        Assert.Equal(0, activity.Considered.Sum);
        Assert.Equal(0, activity.Employed.Sum);
    }

    // ---- the geography ---------------------------------------------------------------------------

    /// <summary>
    /// <b>The Commute Budget refuses vacancies, and the refusals are counted rather than silent.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the counter that reports the shape of the Road Graph, and it is the reason the
    /// candidate search is two stages.</b> The box supplies a Building; the walk decides whether it
    /// can be reached. A pass that pre-filtered candidates by reachability would employ exactly the
    /// same people and report <c>beyond = 0</c>, which is a city where Severance has no observable
    /// consequence.
    /// </para>
    /// <para>
    /// ⚠ <b>It is asserted at a Budget of two minutes rather than at the shipped twenty, because the
    /// shipped Budget barely bites on a fixture this size.</b> Measured across populations under the
    /// shipped geometry, <c>beyond</c> over 512 Ticks runs <b>0, 0, 3, 213</b> at 1,000, 2,000, 4,000
    /// and 8,000 Citizens — so the golden fixture, at 4,000, sits on the first rung that refuses
    /// anything at all, and refuses 3 walks out of 2,000 considered. <b>Two minutes is what makes the
    /// counter a measurement rather than a sighting.</b>
    /// </para>
    /// <para>
    /// <b>The fixture reached this branch by being raised for another reason.</b> It was 1,000
    /// Citizens until queue item 6, which raised it to 4,000 so that the Zone Rule's create branch
    /// would be exercised by the committed session; the Budget refusal came with it, unasked. That is
    /// slice 10 task 11's finding running <i>forwards</i> — <i>a baseline records what a run did</i>,
    /// so a change that widens what the run reaches is invisible in it too, and this one would have
    /// gone unnoticed had the negative assertion below not failed.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_vacancy_the_walk_cannot_reach_is_counted_and_not_taken()
    {
        Assert.True(Run(WithCeiling(3)).Employment.Drain().Beyond.Sum > 0);
    }

    /// <summary>
    /// <b>The shipped Budget is inert on a small world and binds on a large one, and the assertion is
    /// the ladder rather than any one rung.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test has now flipped twice in two days, which is what made a ladder the right shape for
    /// it.</b> It was written by 5b-bis task 4 as <c>Beyond.Sum == 0</c> on a 1,000-Citizen fixture, so
    /// that a future <c>[roads]</c>, population or Budget making the golden world start refusing walks
    /// would have to be read by somebody. Queue item 6 raised the fixture to 4,000 and it began
    /// refusing <b>3 of 2,000</b>; the assertion was flipped to <c>&gt; 0</c>. Item 7 moved the clock
    /// the next day and it went back to <b>0</b> — not because travel got cheaper, but because
    /// <c>revisit_ticks</c> is Day-denominated, so the Zone Rule's derived sample quadrupled and the
    /// building stock at Tick 512 is a different city.
    /// </para>
    /// <para>
    /// <b>Three of two thousand was never a property worth asserting.</b> It is a knife edge, and a
    /// knife edge flips on any change at all — which is <c>PlacementLongRunTests</c>' lesson from the
    /// same week arriving here: <i>a test that draws once cannot tell an outlier from a regression</i>.
    /// What is stable, and what this milestone actually claims, is the <b>shape</b>: the Budget refuses
    /// nothing in a small city and refuses steadily in a large one, so it is a real filter rather than
    /// either a formality or a wall. Measured over 512 Ticks: <b>0, 0, 0, 208</b> at 1,000, 2,000,
    /// 4,000 and 8,000 Citizens.
    /// </para>
    /// <para>
    /// ⚠ <b>So the committed baseline does not reach the refusal branch again</b>, and that is recorded
    /// rather than engineered around. Raising the fixture until it did would be sizing the golden world
    /// to a branch instead of to what it is for.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_ceiling_is_inert_on_a_small_city_and_binds_on_a_large_one()
    {
        Assert.Equal(0, Beyond(GoldenFixtures.Population));

        Assert.True(
            Beyond(GoldenFixtures.Population * 10) > 0,
            "the ceiling refuses nothing even at ten times the golden fixture, so it is not a filter "
            + "on any world this suite runs. Either the geometry shrank or the ceiling rose.");
    }

    /// <summary>Refusals over <see cref="Ticks"/> at a population, on the shipped Ruleset.</summary>
    private static long Beyond(int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, GoldenFixtures.Rules());

        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

        Replay.Trace(simulation, log, new Ticks((ulong)Ticks), HashEvery, []);

        return simulation.Employment.Drain().Beyond.Sum;
    }

    /// <summary>
    /// <b>A tighter Budget employs fewer people, because it is also a smaller search box.</b>
    /// </summary>
    /// <remarks>
    /// <b>One number doing one thing.</b> The Budget is both what a Citizen will accept and how far
    /// they look, since looking beyond what could be accepted is looking where nothing can be found.
    /// The alternative — an authored radius alongside an authored Budget — is two hash-bearing numbers
    /// that can contradict each other, and the contradiction is silent: a radius smaller than the
    /// Budget makes the Budget inert, and one larger wastes the search.
    /// </remarks>
    [Fact]
    public void A_tighter_budget_employs_fewer_people()
    {
        int wide = Employed(Run(GoldenFixtures.Rules()).World);
        int narrow = Employed(Run(WithCeiling(3)).World);

        Assert.True(
            narrow < wide,
            $"a three-minute ceiling employed {narrow} against {wide} at the shipped one.");
    }

    /// <summary>The shipped Ruleset with its Commute Budget's ceiling replaced.</summary>
    /// <remarks>
    /// <b>All three rungs are substituted, not just the ceiling</b> (<c>adr/0095</c>). The shipped
    /// file states 20/40/50, so replacing only the last key with anything below 40 produces a set the
    /// loader refuses — the substitution would fail for exactly the tight ceilings these tests exist
    /// to try. The lower rungs go to 1 and 2 because these assertions are about the <em>ceiling</em>,
    /// which is the only edge that refuses anything, and a rung that grades nothing cannot affect
    /// them. <b>Three is therefore the tightest ceiling any test can ask for</b>, since the rungs must
    /// strictly increase from at least a minute.
    /// </remarks>
    private static Ruleset WithCeiling(int minutes)
    {
        Assert.True(minutes >= 3, "the tightest authorable ceiling is 3 minutes (adr/0095).");

        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        (string Key, string Replacement)[] keys =
        [
            ("commute_fast_minutes = 20", "commute_fast_minutes = 1"),
            ("commute_moderate_minutes = 40", "commute_moderate_minutes = 2"),
            ("commute_budget_minutes = 50", $"commute_budget_minutes = {minutes}"),
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
