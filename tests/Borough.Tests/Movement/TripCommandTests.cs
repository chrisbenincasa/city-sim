using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Movement;

/// <summary>
/// That <c>CommandKind.Trip</c> puts a Citizen on the road, that Tick phase 4 takes them off it, and
/// that the Fate survives the row.
/// </summary>
/// <remarks>
/// <para>
/// <b>The verb exists because milestone 5b builds Phase 4 before anything generates a Trip</b>
/// (<c>adr/0080</c>): every generator the corpus names is unmilestoned, and a sampled stand-in would
/// have fabricated the origin-destination distribution that every measurement downstream is a property
/// of. So a Trip enters through the Input Log, exactly as a population does.
/// </para>
/// <para>
/// <b>No coordinate in this file is a literal, and that is deliberate.</b> Where the populator puts
/// Buildings is a property of the world seed and of <c>[roads]</c>, and a test that hard-coded a block
/// would fail as a <em>refusal</em> the day either moved — which reads as the verb being broken rather
/// than as the fixture being stale. <see cref="TwoOccupiedBlocks"/> runs a probe and asks the world.
/// </para>
/// </remarks>
public sealed class TripCommandTests
{
    /// <summary>The Tick the Trip is commanded on, after the world has had time to build.</summary>
    private const int Departure = 200;

    /// <summary>
    /// Hash cadence for the runs that do not check a hash. <see cref="Replay.Trace"/> refuses a
    /// non-positive one, so this is <em>as rarely as the method permits</em> rather than a number these
    /// tests care about: what they read is the world at the end, and a State Hash is the most expensive
    /// thing in a Tick (S0a: 32.47 ms at 1M, 2.08 budgets).
    /// </summary>
    private const int HashEvery = 1024;

    /// <summary>
    /// <b>A commanded Trip resolves, and every row it used goes back.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0006</c> in the form <c>TripTable</c>'s own doc comment asks for: <i>"this table needs a
    /// sink, and <em>Trips are transient</em> does not supply one."</i> Transience is a claim about
    /// intent; a live-row count of zero after the journey is the claim being true.
    /// </remarks>
    [Fact]
    public void A_commanded_trip_ends_and_every_row_it_used_goes_back()
    {
        Simulation simulation = RunWithTrip(Departure + 64);

        Assert.Equal(0, simulation.World.Trips.Rows.LiveCount);
        Assert.Equal(0, simulation.World.Legs.Rows.LiveCount);
        Assert.Equal(0, simulation.World.Travellers.Rows.LiveCount);
    }

    /// <summary>
    /// <b>The Fate reaches the Census, and it reaches it before the row is freed.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>plans/0021</c> task 2's standing warning, asserted rather than intended: <i>"a completed
    /// Trip's Fate must reach the Census before the row is freed, or the only durable record of a
    /// failure is gone."</i> Read as a <b>sum over the interval</b> because a Fate is a flow — the
    /// reading drains it, on slice 7 task 9's precedent — so this is the count of Trips that
    /// <em>ended</em>, not of Trips that exist.
    /// </para>
    /// <para>
    /// <b>Observed after the run rather than during it</b>, which is the whole point: by then there is
    /// no Trip row left anywhere in the world, so a counter that read the table would read zero.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_fate_outlives_the_row_it_was_recorded_on()
    {
        Simulation simulation = RunWithTrip(Departure + 64);

        var census = new Census(simulation.World);
        census.Observe(simulation);

        Series completed = census.Series(Metric.Of(TripCounter.Completed, Aggregate.Sum), new Ticks(1));

        Assert.Equal(0, simulation.World.Trips.Rows.LiveCount);
        Assert.Equal(1, completed.Count);
        Assert.Equal(1L, completed.Samples.Span[0].Value);
    }

    /// <summary>
    /// <b>A commanded Trip replays to the same State Hash</b> — which is the whole reason it is a
    /// command rather than a runner switch.
    /// </summary>
    /// <remarks>
    /// <c>CommandKind.Populate</c>'s argument, one verb later: a state change that entered any way but
    /// through the log <i>"is a state change no replay reproduces and no hash divergence explains"</i>.
    /// Three Movement tables now fold into the hash, so this also covers their joining it.
    /// </remarks>
    [Fact]
    public void A_commanded_trip_replays_to_the_same_hashes()
    {
        InputLog log = SessionWithTrip();

        Assert.Equal(
            Replay.Run(log, new Ticks(Departure + 64), hashEvery: 8, GoldenFixtures.Rules()),
            Replay.Run(log, new Ticks(Departure + 64), hashEvery: 8, GoldenFixtures.Rules()));
    }

    /// <summary>
    /// <b>The three Movement tables are in the State Hash</b>, which they were not until
    /// <c>adr/0080</c>.
    /// </summary>
    /// <remarks>
    /// <b>Asserted on <see cref="World.Tables"/> rather than on a hash value</b>, because a hash literal
    /// here would be a second copy of <c>world-hash.txt</c> that drifts. What is being claimed is
    /// structural: tasks 1-3 declared these columns with a saved disposition and nothing folded them,
    /// so the State Hash had a coverage hole exactly the size of the Trip model.
    /// </remarks>
    [Fact]
    public void The_movement_tables_fold_into_the_state_hash()
    {
        World world = GoldenFixtures.Build();

        Assert.Contains(world.Trips.Rows, world.Tables.ToArray());
        Assert.Contains(world.Legs.Rows, world.Tables.ToArray());
        Assert.Contains(world.Travellers.Rows, world.Tables.ToArray());
    }

    /// <summary>
    /// <b>A block with nobody in it is refused by name, never substituted for a nearby one.</b>
    /// </summary>
    /// <remarks>
    /// The <c>Scope.Pool</c> and <c>--zones</c> precedent. A substituted endpoint makes an operator's
    /// mistyped delta indistinguishable from the Trip they meant, and the resulting cost distribution
    /// would be a measurement of the substitution rule.
    /// </remarks>
    [Fact]
    public void A_trip_naming_an_empty_block_is_refused_rather_than_moved()
    {
        (int east, int north) = TwoOccupiedBlocks().Origin;

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
            () => RunWith(Trip(east, north, new TripPayload(0, 100)), Departure + 1));

        Assert.Contains("no occupied Building", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Trip whose endpoints resolve to one Building is refused.</b>
    /// </summary>
    /// <remarks>
    /// It would otherwise be a Trip of no distance, which completes immediately and contributes a zero
    /// to every distribution 5b-bis is going to measure — a plausible number produced by a wrong
    /// command, which is the shape session F named as the placeholder that cannot announce itself.
    /// </remarks>
    [Fact]
    public void A_trip_that_starts_where_it_ends_is_refused()
    {
        (int east, int north) = TwoOccupiedBlocks().Origin;

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
            () => RunWith(Trip(east, north, new TripPayload(0, 0)), Departure + 1));

        Assert.Contains("both endpoints", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Trip commanded against a Ruleset with no <c>[trips]</c> is refused rather than costed at
    /// zero.</b>
    /// </summary>
    /// <remarks>
    /// <b>The refusal exists because zero is a legitimate crossing cost.</b> It is
    /// <c>adr/0074</c>'s rung 1 — the city where the shop opposite is the shop next door — so a zero
    /// standing in for <em>nobody authored one</em> would be indistinguishable from a decision, which
    /// is session F's finding that <i>a placeholder inside the range of legitimate answers cannot
    /// announce itself</i>. Every other endpoint refusal in this verb has the same shape: an answer
    /// that looks like an answer is worse than no answer.
    /// </remarks>
    [Fact]
    public void A_trip_commanded_against_a_ruleset_with_no_trips_table_is_refused()
    {
        Blocks blocks = TwoOccupiedBlocks();

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
            () => RunWith(
                Trip(
                    blocks.Origin.East,
                    blocks.Origin.North,
                    new TripPayload(
                        (sbyte)(blocks.Destination.East - blocks.Origin.East),
                        (sbyte)(blocks.Destination.North - blocks.Origin.North))),
                Departure + 1,
                RulesWithTripsTable(null)));

        Assert.Contains("no [trips]", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Trip longer than the Commute Budget ends <see cref="TripFate.ExceededCommuteBudget"/>,
    /// which nothing in this repository could reach until the Budget was Ruleset data.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Fate was declared by <c>adr/0076</c> and structurally unreachable: the set is closed at
    /// four and one of the four had no producer, so a third of the Trip model's failure vocabulary
    /// was documentation. The Budget in this fixture is <b>one minute</b>, which no Trip between two
    /// blocks can make — a block is 32 Tiles and takes a minute and a half to walk at 5 km/h.
    /// </para>
    /// <para>
    /// <b>No shipped Ruleset states a Budget</b>, so this Fate stays unreached in the golden baseline
    /// and in every long run until this milestone has measured the percentile it is. That is the
    /// intended state and not an omission: a Budget authored before the distribution exists is the
    /// number <c>adr/0052</c> forbids.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_trip_beyond_the_commute_budget_ends_over_budget_rather_than_completing()
    {
        Blocks blocks = TwoOccupiedBlocks();

        Simulation simulation = RunWith(
            Trip(
                blocks.Origin.East,
                blocks.Origin.North,
                new TripPayload(
                    (sbyte)(blocks.Destination.East - blocks.Origin.East),
                    (sbyte)(blocks.Destination.North - blocks.Origin.North))),
            Departure + 64,
            RulesWithTripsTable("""
                [trips]
                crossing_seconds = 30
                commute_budget_minutes = 1
                """));

        var census = new Census(simulation.World);
        census.Observe(simulation);

        Series overBudget = census.Series(
            Metric.Of(TripCounter.ExceededCommuteBudget, Aggregate.Sum), new Ticks(1));
        Series completed = census.Series(
            Metric.Of(TripCounter.Completed, Aggregate.Sum), new Ticks(1));

        Assert.Equal(1L, overBudget.Samples.Span[0].Value);
        Assert.Equal(0L, completed.Samples.Span[0].Value);
        Assert.Equal(0, simulation.World.Travellers.Rows.LiveCount);
    }

    /// <summary>
    /// <b>The same Trip under the shipped Ruleset completes</b>, which is what makes the assertion
    /// above about the Budget rather than about the fixture.
    /// </summary>
    /// <remarks>
    /// A control, and it is not redundant with
    /// <c>A_commanded_trip_ends_and_every_row_it_used_goes_back</c>: that one runs the shipped
    /// Ruleset through the shipped path, and this one runs the <em>same substitution machinery</em>
    /// with a Budget generous enough to pass. Without it, a substitution that quietly produced a
    /// broken world would read as the Budget working.
    /// </remarks>
    [Fact]
    public void The_same_trip_under_a_generous_budget_completes()
    {
        Blocks blocks = TwoOccupiedBlocks();

        Simulation simulation = RunWith(
            Trip(
                blocks.Origin.East,
                blocks.Origin.North,
                new TripPayload(
                    (sbyte)(blocks.Destination.East - blocks.Origin.East),
                    (sbyte)(blocks.Destination.North - blocks.Origin.North))),
            Departure + 64,
            RulesWithTripsTable("""
                [trips]
                crossing_seconds = 30
                commute_budget_minutes = 1440
                """));

        var census = new Census(simulation.World);
        census.Observe(simulation);

        Series completed = census.Series(
            Metric.Of(TripCounter.Completed, Aggregate.Sum), new Ticks(1));

        Assert.Equal(1L, completed.Samples.Span[0].Value);
    }

    /// <summary>
    /// <b>The payload survives the sixteen bits it is packed into, negatives included.</b>
    /// </summary>
    /// <remarks>
    /// The negative half is the case worth having: a destination west or south of the origin is a sign
    /// extension away from being a destination 200 blocks east, and that failure would look like a
    /// refusal rather than like a wrong number.
    /// </remarks>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, -1)]
    [InlineData(-128, 127)]
    [InlineData(127, -128)]
    [InlineData(-1, -1)]
    public void The_payload_survives_the_zone_word(int east, int north)
    {
        var payload = new TripPayload((sbyte)east, (sbyte)north);

        Assert.Equal(payload, TripPayload.Decode(payload.Encode()));
    }

    /// <summary>Runs the golden world with one Trip in it, to the given Tick.</summary>
    /// <summary>
    /// The commanded Trip, in a city that generates none of its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The <c>[jobs]</c> deletion is what makes <em>one</em> Trip a countable thing, and before
    /// 5b-bis task 5 it was countable because nothing in the simulation could generate a Trip at
    /// all.</b> <c>A_commanded_trip_ends_and_every_row_it_used_goes_back</c> read the table's live
    /// count and <c>The_fate_outlives_the_row_it_was_recorded_on</c> read a Census sum, and both were
    /// true <em>by absence</em> — the commute generator made the shipped Ruleset produce Trips of its
    /// own on the same Ticks, and both assertions started measuring two mechanisms at once.
    /// </para>
    /// <para>
    /// <b>The isolation is the honest fix rather than a widened assertion.</b> What these two tests
    /// claim is that a commanded Trip's rows come back and its Fate outlives them — claims about
    /// <em>this</em> Trip. Asserting <c>≥ 1</c> against a city that also commutes would keep them
    /// green while measuring nothing in particular, which is the failure
    /// <c>GoldenSessionCoverageTests</c>' preamble is about.
    /// </para>
    /// </remarks>
    private static Simulation RunWithTrip(int ticks)
    {
        Blocks blocks = TwoOccupiedBlocks();

        return RunWith(
            Trip(
                blocks.Origin.East,
                blocks.Origin.North,
                new TripPayload(
                    (sbyte)(blocks.Destination.East - blocks.Origin.East),
                    (sbyte)(blocks.Destination.North - blocks.Origin.North))),
            ticks,
            WithoutJobs());
    }

    /// <summary>The golden Ruleset with its <c>[jobs]</c> table deleted, and with it nothing else.</summary>
    private static Ruleset WithoutJobs()
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        int marker = toml.IndexOf("\n[jobs]", StringComparison.Ordinal);

        Assert.True(marker > 0, "the golden Ruleset no longer declares a [jobs] table.");

        RulesetLoadResult result = RulesetLoader.Parse(toml[..marker], "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static InputLog SessionWithTrip()
    {
        Blocks blocks = TwoOccupiedBlocks();

        return With(
            Trip(
                blocks.Origin.East,
                blocks.Origin.North,
                new TripPayload(
                    (sbyte)(blocks.Destination.East - blocks.Origin.East),
                    (sbyte)(blocks.Destination.North - blocks.Origin.North))));
    }

    private static Simulation RunWith(Command command, int ticks) =>
        RunWith(command, ticks, GoldenFixtures.Rules());

    private static Simulation RunWith(Command command, int ticks, Ruleset rules)
    {
        InputLog log = With(command);
        Simulation simulation = Replay.Start(log, rules);

        Replay.Trace(simulation, log, new Ticks((ulong)ticks), HashEvery, []);

        return simulation;
    }

    /// <summary>
    /// The golden Ruleset with its <c>[trips]</c> table replaced by <paramref name="table"/>, or
    /// deleted when it is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A substitution on the shipped file rather than a Ruleset written here</b>, on
    /// <c>RoadRulesetLoadTests</c>' reasoning: a test that asks what the shipped city does with one
    /// number changed is asking about the city this repository has, and a hand-rolled Ruleset is
    /// asking about a different one.
    /// </para>
    /// <para>
    /// <b>Everything after <c>[trips]</c> goes with it, and <c>[jobs]</c> is the only thing there —
    /// which is not an accident of ordering but a constraint of the schema.</b> A <c>[jobs]</c> table
    /// is refused in a Ruleset with no <c>commute_budget_minutes</c>, because the assignment pass
    /// derives its search box from the Budget, so the two cannot be separated: replacing the
    /// <c>[trips]</c> table while keeping <c>[jobs]</c> would produce a Ruleset the loader rejects for
    /// half of this file's cases. The assertion below is what keeps the deletion honest — a
    /// <em>third</em> table added after these would otherwise be silently dropped, which is a fixture
    /// that stops testing what it says.
    /// </para>
    /// </remarks>
    private static Ruleset RulesWithTripsTable(string? table)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        int marker = toml.IndexOf("\n[trips]", StringComparison.Ordinal);

        Assert.True(marker > 0, "the golden Ruleset no longer declares a [trips] table.");
        Assert.Equal(
            ["[jobs]"],
            toml[(marker + 1)..]
                .Split('\n')
                .Skip(1)
                .Select(line => line.TrimEnd())
                .Where(line => line.StartsWith('[')));

        RulesetLoadResult result = RulesetLoader.Parse(
            table is null ? toml[..marker] : $"{toml[..marker]}\n{table}", "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static InputLog With(Command command)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));
        builder.Append(new Ticks(Departure), command);

        return builder.Build();
    }

    private static Command Trip(int blockEast, int blockNorth, TripPayload payload)
    {
        int block = GoldenFixtures.Rules().Roads.BlockTiles;

        return new Command(
            CommandKind.Trip,
            new Tiles(blockEast * block),
            new Tiles(blockNorth * block),
            payload.Encode());
    }

    /// <summary>
    /// Two blocks the populated world has put an occupied Building in, close enough to walk between.
    /// </summary>
    /// <remarks>
    /// <b>A probe run rather than a literal.</b> Where Buildings land is a property of the seed and of
    /// <c>[roads]</c>, so asking the world is the only form of this that does not become a stale
    /// constant. It runs the same log the tests do, up to the same Tick, so what it sees is what the
    /// command will see.
    /// </remarks>
    private static Blocks TwoOccupiedBlocks()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog probe = builder.Build();
        Simulation simulation = Replay.Start(probe, GoldenFixtures.Rules());

        Replay.Trace(simulation, probe, new Ticks(Departure), HashEvery, []);

        World world = simulation.World;
        int block = world.Roads.Streets.BlockTiles;

        (int East, int North)? first = null;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)
                || world.Lots.IsVacant(slot)
                || !world.Lots.HasFrontage(slot))
            {
                continue;
            }

            (int East, int North) here = (
                IntegerMath.FloorDiv(world.Lots.East[slot].Raw, block),
                IntegerMath.FloorDiv(world.Lots.North[slot].Raw, block));

            if (first is null)
            {
                first = here;
                continue;
            }

            if (here == first.Value)
            {
                continue;
            }

            int deltaEast = here.East - first.Value.East;
            int deltaNorth = here.North - first.Value.North;

            if (deltaEast is >= -128 and <= 127 && deltaNorth is >= -128 and <= 127)
            {
                return new Blocks(first.Value, here);
            }
        }

        throw new InvalidOperationException(
            "the populated world offers no two distinct blocks with a fronted, occupied Lot in each, "
            + "so nothing in this file can command a Trip. The fixture is stale rather than the verb "
            + "being broken -- check what populate builds and what [roads] lays.");
    }

    private readonly record struct Blocks(
        (int East, int North) Origin, (int East, int North) Destination);
}
