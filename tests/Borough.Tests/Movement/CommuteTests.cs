using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Movement;

/// <summary>
/// The commute generator and its daily occasion: who walks to work, and on which Tick of the Day.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim under test is not that Trips appear.</b> It is that the occasion is a <em>partition
/// of the population by a derived phase</em> rather than a schedule — so the assertions are about
/// spread, about rebuild exactness, and about the window moving when the Ruleset's peak does. A test
/// that only counted Trips would pass against a generator that sent the whole city out on one Tick,
/// which is the design this task rejected.
/// </para>
/// <para>
/// <b>The shipped Ruleset and the shipped populator</b>, on <c>JobAssignmentTests</c>' reasoning: the
/// geography is the input, and a hand-built two-Building world would be testing
/// <see cref="WalkRouting"/> again.
/// </para>
/// </remarks>
public sealed class CommuteTests
{
    /// <summary>
    /// Long enough that some departure phases have come round, and short enough to stay a unit test.
    /// </summary>
    /// <remarks>
    /// <b>Far short of the window, and deliberately.</b> At <c>commute_peak_factor = 3</c> the window
    /// is 2,731 Ticks, so this run reaches under a fifth of the departure phases — which is what makes
    /// <see cref="Departures_are_spread_across_the_window_rather_than_massed_on_one_tick"/> a real
    /// assertion rather than a tautology.
    /// </remarks>
    private const int TickCount = 512;

    /// <summary>As rarely as <see cref="Replay.Trace"/> permits: nothing here reads a State Hash.</summary>
    private const int HashEvery = 1_024;

    /// <summary>The content hash of the flattened Ruleset. Arbitrary and never loaded from a file.</summary>
    private const ulong FlatHash = 0xF1A7_0000_0000_0001UL;

    // ---- the generator ----------------------------------------------------------------------------

    /// <summary>
    /// <b>Somebody walks to work, and this is the first Trip in the project nobody asked for.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for <c>adr/0081</c>'s second half. Every Trip in the corpus before this
    /// line entered through <c>CommandKind.Trip</c> — <c>adr/0080</c>'s door, built precisely because
    /// no generator existed — so a Trip carrying <see cref="TripPurpose.Commute"/> in a run whose
    /// Input Log holds one <c>populate</c> and nothing else is the whole milestone in one assertion.
    /// </remarks>
    [Fact]
    public void A_citizen_with_a_workplace_walks_to_it_without_being_told_to()
    {
        World world = Run(GoldenFixtures.Rules()).World;

        Assert.True(
            InFlight(world, TripPurpose.Commute) > 0,
            "no Citizen commuted over 512 Ticks of the shipped Ruleset.");
    }

    /// <summary>
    /// <b>A Ruleset with no <c>[jobs]</c> generates no commute, and it is silence rather than a
    /// refusal.</b>
    /// </summary>
    /// <remarks>
    /// <c>rulesets/minimal.toml</c> declared no employment for six slices and was a legitimate file
    /// throughout, so a generator that threw here would make an ordinary Ruleset unloadable. The
    /// refusal that <em>is</em> loud lives at the loader: <c>[jobs]</c> with no
    /// <c>commute_budget_minutes</c> is rejected outright, because a search box derived from an
    /// unstated Budget is S2 R4's uniform draw, which R4 measured is a different city.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_jobs_generates_no_commute()
    {
        World world = Run(WithoutJobs()).World;

        Assert.Equal(0, InFlight(world, TripPurpose.Commute));
    }

    /// <summary>
    /// <b>Departures are spread across the window rather than massed on one Tick</b>, which is the
    /// occasion's whole content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Asserted as <i>no bucket holds a large share</i> rather than as <i>each bucket holds
    /// n ÷ window</i>.</b> The phase is a counter-based draw, so bucket sizes are binomial and pinning
    /// one would make this fail on the dice. What is structural is that a thousand people drawing
    /// uniformly over 2,731 buckets never pile a tenth of themselves onto one.
    /// </para>
    /// <para>
    /// <b>This is the assertion the Event Wheel would also have passed, and that is the point.</b>
    /// The roster is not preferred because it spreads better — it spreads identically — but because
    /// the spread never changes, which is what makes it derivable at all.
    /// </para>
    /// </remarks>
    [Fact]
    public void Departures_are_spread_across_the_window_rather_than_massed_on_one_tick()
    {
        World world = Run(GoldenFixtures.Rules()).World;
        int window = world.Rules.Jobs.CommuteWindow;

        int total = 0;
        int largest = 0;

        for (int phase = 0; phase < window; phase++)
        {
            int here = world.Commutes.CountAt(world.Citizens, phase);

            total += here;
            largest = here > largest ? here : largest;
        }

        Assert.Equal(Live(world.Citizens), total);
        Assert.True(
            largest * 10 < total,
            $"one departure Tick holds {largest} of {total} Citizens, which is not a spread.");
    }

    /// <summary>
    /// <b>Every Citizen is in exactly one bucket, and every bucket is inside the window.</b>
    /// </summary>
    /// <remarks>
    /// The two ways a partition stops being one. A Citizen in no bucket never commutes and nothing
    /// reports it; a Citizen in two commutes twice a Day, which is the overlap the milestone's scope
    /// says does not exist. A phase at or beyond the window is a Citizen who never departs at all,
    /// because <c>CommuteEngine.Generate</c> returns before the loop on those Ticks.
    /// </remarks>
    [Fact]
    public void The_roster_partitions_the_population()
    {
        World world = Run(GoldenFixtures.Rules()).World;

        bool[] seen = new bool[world.Citizens.Rows.SlotCount];

        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            foreach (int citizen in world.Commutes.Departing(world.Citizens, phase))
            {
                Assert.True(phase < world.Rules.Jobs.CommuteWindow, "a phase outside the window.");
                Assert.False(seen[citizen], $"Citizen {citizen} is in two departure buckets.");

                seen[citizen] = true;
            }
        }

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            Assert.Equal(world.Citizens.Rows.IsLive(slot), seen[slot]);
        }
    }

    // ---- (derived AND rebuilt) --------------------------------------------------------------------

    /// <summary>
    /// <b>A rebuild reproduces the maintained roster exactly rather than plausibly</b>, which is what
    /// <c>(derived AND rebuilt)</c> claims.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Order and membership both</b>, because the list is walked to create Trips and Trip ids are
    /// folded into the State Hash. A rebuild that got the same people in a different order would give
    /// a reloaded save a different hash trace from the run that wrote it —
    /// <see cref="Tables.IndexList.InsertOrdered"/> is what prevents it, and this is the test of that.
    /// </para>
    /// <para>
    /// The world here has been run, so rows have been allocated, Citizens have taken jobs and the
    /// Ruleset has been read — a rebuild against a freshly populated world would assert far less.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_rebuild_reproduces_the_roster_it_maintained()
    {
        World world = Run(GoldenFixtures.Rules()).World;

        int[][] before = Snapshot(world);

        world.RebuildDerived();

        int[][] after = Snapshot(world);

        for (int phase = 0; phase < before.Length; phase++)
        {
            Assert.Equal(before[phase], after[phase]);
        }
    }

    /// <summary>
    /// <b>Retuning the peak moves the standing city's departures, and it does so at the reload rather
    /// than at the next Citizen born.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0064</c>'s disposition on a third axis — a Bin's capacity, a Building's occupancy, and
    /// now a departure phase are all <em>derived from the Ruleset in force</em>. And it is 5a-bis's
    /// trap in the other direction: a derived structure that cached the old window would read as
    /// <em>absent</em> rather than as <em>stale</em>, so <c>World.Adopt</c> rebuilds this explicitly.
    /// </remarks>
    [Fact]
    public void Retuning_the_peak_rebuckets_the_standing_city()
    {
        Ruleset flat = WithPeak(1);
        InputLog log = Log();

        Simulation simulation = Replay.Start(
            log,
            RulesetCatalogue.Of(
                [GoldenFixtures.RulesetHash, FlatHash], [GoldenFixtures.Rules(), flat]));

        Replay.Trace(simulation, log, new Ticks(TickCount), HashEvery, []);

        World world = simulation.World;
        int narrow = world.Rules.Jobs.CommuteWindow;
        int[][] before = Snapshot(world);

        simulation.Step(new TickInput(default, FlatHash));

        Assert.True(narrow < Ticks.PerDay, "the shipped Ruleset states no peak at all.");
        Assert.Equal(Ticks.PerDay, world.Rules.Jobs.CommuteWindow);

        int[][] after = Snapshot(world);
        bool moved = false;

        for (int phase = 0; phase < before.Length && !moved; phase++)
        {
            moved = !before[phase].AsSpan().SequenceEqual(after[phase]);
        }

        Assert.True(moved, "widening the window to a whole Day moved nobody's departure.");
    }

    /// <summary>
    /// <b>A departure phase is a property of a person, not of a moment.</b>
    /// </summary>
    /// <remarks>
    /// The draw's Tick coordinate is <see cref="Ticks.Zero"/> for this reason, and this is the
    /// assertion that keeps it there. A Citizen whose commute time changed every Day would be
    /// re-rolling a decision <c>CONTEXT.md</c> → Provider List says is made once — <i>how I get to
    /// work is decided when the job is taken, not every morning</i> — and nothing else in the code
    /// would notice, because every individual Day would look perfectly well spread.
    /// </remarks>
    [Fact]
    public void A_phase_is_drawn_from_the_citizen_and_not_from_the_clock()
    {
        WorldKey key = WorldKey.FromSeed(GoldenFixtures.Seed);

        Assert.Equal(
            CommuteRoster.PhaseOf(key, 4_242UL, 2_731),
            CommuteRoster.PhaseOf(key, 4_242UL, 2_731));

        Assert.NotEqual(
            CommuteRoster.PhaseOf(key, 4_242UL, 2_731),
            CommuteRoster.PhaseOf(WorldKey.FromSeed(GoldenFixtures.Seed + 1), 4_242UL, 2_731));
    }

    // ---- the fixture ------------------------------------------------------------------------------

    private static Simulation Run(Ruleset rules)
    {
        InputLog log = Log();
        Simulation simulation = Replay.Start(log, rules);

        Replay.Trace(simulation, log, new Ticks(TickCount), HashEvery, []);

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

    /// <summary>Live Trips of one purpose. Read from the table because a Fate frees the row.</summary>
    /// <remarks>
    /// <b>Counted in flight rather than at release, and that is a property of the run length.</b> A
    /// Trip that ends is released on the Tick it ends, so the only Trips a finished run can be asked
    /// about are the ones still walking — which at a Commute Budget of 20 minutes is most of them at
    /// any instant.
    /// </remarks>
    private static int InFlight(World world, TripPurpose purpose)
    {
        int total = 0;

        for (int slot = 0; slot < world.Trips.Rows.SlotCount; slot++)
        {
            if (world.Trips.Rows.IsLive(slot) && (TripPurpose)world.Trips.Purpose[slot] == purpose)
            {
                total++;
            }
        }

        return total;
    }

    private static int Live(CitizenTable citizens)
    {
        int total = 0;

        for (int slot = 0; slot < citizens.Rows.SlotCount; slot++)
        {
            if (citizens.Rows.IsLive(slot))
            {
                total++;
            }
        }

        return total;
    }

    /// <summary>Every bucket's contents in walk order.</summary>
    private static int[][] Snapshot(World world)
    {
        int[][] buckets = new int[Ticks.PerDay][];

        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            List<int> here = [];

            foreach (int citizen in world.Commutes.Departing(world.Citizens, phase))
            {
                here.Add(citizen);
            }

            buckets[phase] = [.. here];
        }

        return buckets;
    }

    /// <summary>The shipped Ruleset with its peak flattened to a Day with no peak at all.</summary>
    private static Ruleset WithPeak(int factor)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        const string Key = "commute_peak_factor = 3";

        Assert.Contains(Key, toml, StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(
            toml.Replace(Key, $"commute_peak_factor = {factor}", StringComparison.Ordinal),
            "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>The shipped Ruleset with its <c>[jobs]</c> table deleted, and with it nothing else.</summary>
    private static Ruleset WithoutJobs()
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        int marker = toml.IndexOf("\n[jobs]", StringComparison.Ordinal);

        Assert.True(marker > 0, "the golden Ruleset no longer declares a [jobs] table.");

        RulesetLoadResult result = RulesetLoader.Parse(toml[..marker], "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
