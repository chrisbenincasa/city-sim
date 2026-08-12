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
                && world.Buildings.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                total++;
            }
        }

        return total;
    }

    /// <summary>
    /// The shipped Ruleset with its <c>[jobs]</c> table deleted, and with it nothing else.
    /// </summary>
    /// <remarks>
    /// <b>A deletion on the shipped file rather than a Ruleset written here</b>, on
    /// <c>TripCommandTests.RulesWithTripsTable</c>'s reasoning: a test that asks what the shipped city
    /// does with one table removed is asking about the city this repository has. The assertion is what
    /// keeps it honest — <c>[jobs]</c> is last in the file, and a table added after it would otherwise
    /// be silently deleted too.
    /// </remarks>
    private static Ruleset WithoutJobs()
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        int marker = toml.IndexOf("\n[jobs]", StringComparison.Ordinal);

        Assert.True(marker > 0, "the golden Ruleset no longer declares a [jobs] table.");
        Assert.DoesNotContain(
            toml[(marker + 1)..].Split('\n').Skip(1),
            line => line.TrimStart().StartsWith('['));

        RulesetLoadResult result = RulesetLoader.Parse(toml[..marker], "test.toml");

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
            if (!world.Citizens.Rows.IsLive(slot)
                || !world.Buildings.Rows.TryResolve(world.Citizens.Workplace[slot], out int work))
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
                world.Roads,
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

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.TryDeclaredJobs(world.Buildings.Kind[slot], out int jobs))
            {
                continue;
            }

            Assert.True(
                world.Workers.Length(slot) <= jobs,
                $"building {slot} holds {world.Workers.Length(slot)} workers against {jobs} jobs.");
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
    /// shipped Budget is inert on a fixture this size and that is worth knowing.</b> The golden world
    /// is 1,000 Citizens, which the populator houses in about 120 Buildings on one contiguous strip of
    /// blocks — every pair in it is a few minutes' walk apart, so nothing is ever refused for length.
    /// At 10,000 Citizens the same Ruleset reports a steady <c>beyond</c> of 48 per census interval.
    /// <b>So the committed baseline does not reach this branch</b>, which is slice 10 task 11's
    /// finding arriving for the third time: <i>a baseline records what a run did</i>, and a Budget
    /// chosen against the map is not thereby exercised by every world on it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_vacancy_the_walk_cannot_reach_is_counted_and_not_taken()
    {
        Assert.True(Run(WithBudget(2)).Employment.Drain().Beyond.Sum > 0);
    }

    /// <summary>
    /// <b>The shipped Budget refuses nothing on the golden fixture, and that is asserted rather than
    /// left to be discovered.</b>
    /// </summary>
    /// <remarks>
    /// The negative half of the test above, and it exists so that the fact cannot rot silently. If a
    /// future <c>[roads]</c>, population or Budget makes the golden world start refusing walks, this
    /// fails and the note above becomes wrong in a way somebody has to read — which is the opposite of
    /// how the corpus usually finds out that a paragraph has stopped being true.
    /// </remarks>
    [Fact]
    public void The_shipped_budget_is_inert_on_a_world_this_small()
    {
        Assert.Equal(0, Run(GoldenFixtures.Rules()).Employment.Drain().Beyond.Sum);
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
        int narrow = Employed(Run(WithBudget(1)).World);

        Assert.True(
            narrow < wide,
            $"a one-minute Budget employed {narrow} against {wide} at the shipped Budget.");
    }

    /// <summary>The shipped Ruleset with its Commute Budget replaced.</summary>
    private static Ruleset WithBudget(int minutes)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        const string Key = "commute_budget_minutes = 20";

        Assert.Contains(Key, toml, StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(
            toml.Replace(Key, $"commute_budget_minutes = {minutes}", StringComparison.Ordinal),
            "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
