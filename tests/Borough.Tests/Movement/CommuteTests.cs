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

        int total = 0;
        int largest = 0;

        // Over the whole Day rather than over a window, because adr/0101 removed the window: a
        // Workplace's Shift start can fall anywhere its kind's band permits, and the outbound phase
        // is that less the Citizen's own commute, which can carry it across midnight.
        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            int here = world.Commutes.CountAt(world.Citizens, phase);

            total += here;
            largest = here > largest ? here : largest;
        }

        // The roster holds the EMPLOYED and not the population, which is the other half of adr/0101's
        // change to this class's contract: hours come from a job, so somebody without one has none.
        Assert.Equal(Employed(world), total);
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

        bool[] out_ = new bool[world.Citizens.Rows.SlotCount];
        bool[] back = new bool[world.Citizens.Rows.SlotCount];

        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            foreach (int citizen in world.Commutes.Departing(world.Citizens, phase))
            {
                Assert.False(out_[citizen], $"Citizen {citizen} is in two departure buckets.");
                out_[citizen] = true;
            }

            foreach (int citizen in world.Commutes.Returning(world.Citizens, phase))
            {
                Assert.False(back[citizen], $"Citizen {citizen} is in two return buckets.");
                back[citizen] = true;
            }
        }

        // ⚠ Both partitions or neither, and this is the assertion that catches the ordering defect
        // World.Employ warns about: a Citizen re-rostered around a rewritten Workplace handle lands
        // in one list and is stranded in the other's old bucket, which no count of either alone sees.
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            bool employed = world.Citizens.Rows.IsLive(slot)
                && world.Buildings.Rows.IsValid(world.Citizens.Workplace[slot]);

            Assert.Equal(employed, out_[slot]);
            Assert.Equal(employed, back[slot]);
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
    /// <b>Retuning the Shift band moves the standing city's departures, at the reload rather than at
    /// the next Citizen born.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0064</c>'s disposition again — a Bin's capacity, a Building's occupancy, a kind's jobs,
    /// car ownership and now both halves of a commute's timing are <em>derived from the Ruleset in
    /// force</em>. And it is 5a-bis's trap in the other direction: a derived structure that cached the
    /// old band would read as <em>absent</em> rather than as <em>stale</em>, so <c>World.Adopt</c>
    /// rebuilds this explicitly instead of leaving it to the next write.
    /// </remarks>
    [Fact]
    public void Retuning_the_shift_band_rebuckets_the_standing_city()
    {
        Ruleset longer = WithShiftHours(12, 12);
        InputLog log = Log();

        Simulation simulation = Replay.Start(
            log,
            RulesetCatalogue.Of(
                [GoldenFixtures.RulesetHash, FlatHash], [GoldenFixtures.Rules(), longer]));

        Replay.Trace(simulation, log, new Ticks(TickCount), HashEvery, []);

        World world = simulation.World;
        int[] before = PhaseOf(Snapshot(world), world);
        int[] beforeReturn = PhaseOf(SnapshotReturn(world), world);
        ulong[] beforeEmployer = EmployerOf(world);

        simulation.Step(new TickInput(default, FlatHash));

        int[] after = PhaseOf(Snapshot(world), world);
        int[] afterReturn = PhaseOf(SnapshotReturn(world), world);
        ulong[] afterEmployer = EmployerOf(world);

        // ⚠ Per Citizen and only where the EMPLOYER did not change, which is a real narrowing rather
        // than a weakening. Adoption happens inside a Tick, so the Step that reloads the Ruleset also
        // runs the Zone Rules, placement and the assignment pass: some Citizens take a job, lose one
        // to a demolition or move to another Building on that very Tick, and their departure moves
        // for a reason that has nothing to do with the band. Comparing bucket CONTENTS folds those
        // two causes together and cannot tell them apart. The claim is about a standing Citizen with
        // a standing employer, so that is what is compared.
        bool leftHomeSame = true;
        bool leftWorkMoved = false;
        int held = 0;

        for (int slot = 0; slot < before.Length && slot < after.Length; slot++)
        {
            if (beforeEmployer[slot] == 0 || beforeEmployer[slot] != afterEmployer[slot])
            {
                continue;
            }

            held++;

            // ⚠ The RETURN phase is what has to move, and the outbound one is what must not. A Shift
            // length is the gap between the two departures, so retuning it moves when people leave
            // work and cannot move when they leave home -- and a test that only looked at the
            // outbound list would pass on a Ruleset reload that did nothing at all.
            leftHomeSame &= before[slot] == after[slot];
            leftWorkMoved |= beforeReturn[slot] != afterReturn[slot];
        }

        Assert.True(held > 0, "nobody held the same job across the reload, so nothing was compared.");
        Assert.True(leftHomeSame, "retuning the Shift length moved somebody's departure from home.");
        Assert.True(leftWorkMoved, "fixing every Shift at 12 hours moved nobody's departure from work.");
    }

    /// <summary>
    /// <b>Both draws are properties of a thing, not of a moment.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each draw's Tick coordinate is <see cref="Ticks.Zero"/> for this reason, and this is the
    /// assertion that keeps it there. A Shift length that changed every Day would be re-rolling a
    /// decision <c>CONTEXT.md</c> → Provider List says is made once — <i>how I get to work is decided
    /// when the job is taken, not every morning</i> — and nothing else in the code would notice,
    /// because every individual Day would look perfectly well spread.
    /// </para>
    /// <para>
    /// <b>Both are keyed on the world seed as well</b>, which is what makes two cities from two seeds
    /// different cities rather than the same city relabelled.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_shift_draws_are_stable_and_keyed_on_the_seed()
    {
        WorldKey key = WorldKey.FromSeed(GoldenFixtures.Seed);
        WorldKey other = WorldKey.FromSeed(GoldenFixtures.Seed + 1);
        JobRuleset jobs = GoldenFixtures.Rules().Jobs;

        Assert.Equal(jobs.ShiftLengthOf(key, 4_242UL), jobs.ShiftLengthOf(key, 4_242UL));

        KindDefinition kind = new(0, 0, 0, 0)
        {
            ShiftStartEarliestHour = 0,
            ShiftStartLatestHour = 23,
        };

        Assert.Equal(
            CommuteRoster.ShiftStartOf(key, 4_242UL, kind),
            CommuteRoster.ShiftStartOf(key, 4_242UL, kind));

        Assert.NotEqual(
            CommuteRoster.ShiftStartOf(key, 4_242UL, kind),
            CommuteRoster.ShiftStartOf(other, 4_242UL, kind));
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
    /// <summary>A bucket snapshot inverted: the phase each Citizen sits at, or −1 for none.</summary>
    private static int[] PhaseOf(int[][] buckets, World world)
    {
        int[] phases = new int[world.Citizens.Rows.SlotCount];

        Array.Fill(phases, -1);

        for (int phase = 0; phase < buckets.Length; phase++)
        {
            foreach (int citizen in buckets[phase])
            {
                phases[citizen] = phase;
            }
        }

        return phases;
    }

    /// <summary>
    /// Each Citizen's employer as a <b>monotonic id</b> rather than a slot, and 0 for none.
    /// </summary>
    /// <remarks>
    /// The id rather than the slot because a slot is recycled: a Building demolished on the reload
    /// Tick and another raised into its row would read as <em>the same employer</em> and would put a
    /// Citizen whose job genuinely changed into the comparison.
    /// </remarks>
    private static ulong[] EmployerOf(World world)
    {
        ulong[] employers = new ulong[world.Citizens.Rows.SlotCount];

        for (int slot = 0; slot < employers.Length; slot++)
        {
            employers[slot] = world.Citizens.Rows.IsLive(slot)
                && world.Buildings.Rows.TryResolve(world.Citizens.Workplace[slot], out int workplace)
                    ? world.Buildings.Rows.IdAt(workplace)
                    : 0;
        }

        return employers;
    }

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
    private static Ruleset WithShiftHours(int min, int max)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        const string Key = "shift_hours_min = 6\nshift_hours_max = 10";

        Assert.Contains(Key, toml, StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(
            toml.Replace(
                Key,
                $"shift_hours_min = {min}\nshift_hours_max = {max}",
                StringComparison.Ordinal),
            "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>How many Citizens hold a job the Building table still resolves.</summary>
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

    /// <summary>Every return bucket's contents in walk order.</summary>
    private static int[][] SnapshotReturn(World world)
    {
        int[][] buckets = new int[Ticks.PerDay][];

        for (int phase = 0; phase < Ticks.PerDay; phase++)
        {
            List<int> here = [];

            foreach (int citizen in world.Commutes.Returning(world.Citizens, phase))
            {
                here.Add(citizen);
            }

            buckets[phase] = [.. here];
        }

        return buckets;
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
