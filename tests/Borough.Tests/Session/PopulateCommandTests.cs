using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Session;

/// <summary>
/// <c>CommandKind.Populate</c>: spike <c>S0</c>'s verb, and the claims that justify it being a
/// command rather than something the runner does to a world.
/// </summary>
/// <remarks>
/// <para>
/// <b>The point of the design is that replay equivalence comes for free, and a claim nobody checks
/// is not free.</b> A population that entered through Phase 0 is described by the log; one that
/// entered from the shell is a state change no replay reproduces and no hash divergence explains.
/// <see cref="A_populated_world_replays_to_the_same_hash_sequence"/> is that argument, run.
/// </para>
/// <para>
/// <b>The population is not a small number here.</b> These use a few thousand Citizens rather than
/// the million <c>S0</c> measures, because what is under test is the mechanism and not the machine.
/// </para>
/// </remarks>
public sealed class PopulateCommandTests
{
    private const int Population = 5_000;

    private const ulong Seed = 0x5005_0000_0000_0001UL;

    [Fact]
    public void The_verb_fills_the_world_to_its_configured_size()
    {
        Simulation simulation = Run(Log());

        Assert.Equal(Population, simulation.World.Citizens.Rows.LiveCount);
        Assert.Equal(Population * 360 / 1_000, simulation.World.Households.Rows.LiveCount);
        Assert.NotEqual(0, simulation.World.Buildings.Rows.LiveCount);
        Assert.NotEqual(0, simulation.World.Lots.Rows.LiveCount);
    }

    /// <summary>
    /// The populated city has nowhere to build and nobody to build for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Found while building slice 10's create predicate, and it is why the Zone Rule cannot be
    /// exercised by any world this project can currently produce.</b> <c>SyntheticCity</c> creates
    /// exactly one Lot per Building and houses every Household, so the Lot table has no vacancy and
    /// the Unplaced Pool has no member — two of the create predicate's three terms are false
    /// everywhere, permanently.
    /// </para>
    /// <para>
    /// <b>Recorded as a test rather than a note because it is the shape of the fixture and not a
    /// defect in it.</b> What makes vacant land is the Lot subdivider, which is milestone 5a's and has
    /// no milestone in Phase 1 (<c>plans/0012</c>); what makes a Household seek a home is demolition,
    /// which is slice 10's own next task. So the growth cycle closes on itself and cannot be entered
    /// from a standing start — and if this assertion ever fails, the reason the golden session builds
    /// nothing has changed and that trace needs rereading.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_populated_city_has_no_vacant_lot_and_an_empty_pool()
    {
        World world = Run(Log()).World;

        Assert.Equal(world.Buildings.Rows.LiveCount, world.Lots.Rows.LiveCount);
        Assert.Equal(0, world.UnplacedPool.Count);

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            Assert.False(world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot));
        }
    }

    /// <summary>
    /// The claim the whole shape rests on: a log carrying a Populate reproduces its city exactly.
    /// </summary>
    [Fact]
    public void A_populated_world_replays_to_the_same_hash_sequence()
    {
        InputLog log = Log();

        ulong[] first = Replay.Run(log, new Ticks(16), hashEvery: 1);
        ulong[] second = Replay.Run(log, new Ticks(16), hashEvery: 1);

        Assert.Equal(first, second);
    }

    /// <summary>
    /// Populating draws no randomness <b>except to lay the ground</b>, so the world seed reaches the
    /// terrain and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Stated as a test because the failure is silent and one-directional.</b> Somebody adding
    /// variety to the fixture with a convenient <c>draw()</c> would correlate it with whatever
    /// simulation decision shares the <c>purpose_tag</c>, and nothing else in the suite would notice.
    /// This fails the moment the fixture starts drawing.
    /// </para>
    /// <para>
    /// ⚠ <b>It said <em>and not of the seed</em>, full stop, until milestone 24 task 2, and the
    /// exception is a decision rather than a regression.</b> <c>adr/0157</c> makes the terrain type
    /// column a function of the <c>WorldKey</c> and <c>adr/0021</c> makes the map procedural, so a
    /// city whose ground did <em>not</em> move with its seed would be the defect. What the seed must
    /// still not reach is everything else — the Lots, the Buildings, the Households, the Citizens.
    /// </para>
    /// <para>
    /// <b>So the assertion moved from the State Hash to the tables under it</b>, and it is stronger
    /// for it: the old form said <em>nothing differs</em> and could only ever be relaxed to
    /// <em>something differs</em>, where this one names the tables that may. A second table starting
    /// to draw fails it, which the hash comparison could no longer do at all.
    /// </para>
    /// <para>
    /// ✅ <b>And a second table did start to draw, one milestone task later.</b> <c>adr/0158</c> makes
    /// Woodland a <c>WorldKey</c>-derived column too, and this test failed on the day it landed —
    /// ***the paragraph above was written as a prediction and paid out as one***. The exception list is
    /// now two tables and each is asserted to move <em>separately</em>: they are drawn on different
    /// <c>purpose_tag</c>s, so folding them together would pass whenever either one moved, which is the
    /// exact correlation <c>PurposeTag.Woodland</c> exists to refuse.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_city_is_a_function_of_its_size_and_the_seed_reaches_only_the_ground()
    {
        World first = Run(Log(Seed)).World;
        World second = Run(Log(Seed ^ 0xFFFF_FFFF_FFFF_FFFFUL)).World;

        Assert.Equal(
            ExceptGround(first), ExceptGround(second));

        Assert.NotEqual(
            Fold(first.Layers.Terrain.Rows), Fold(second.Layers.Terrain.Rows));

        // Asserted separately rather than folded together with terrain, because the two are drawn on
        // DIFFERENT purpose tags and a single comparison would pass if either one moved. That is the
        // correlation PurposeTag.Woodland exists to refuse, and this is where it is checkable.
        Assert.NotEqual(
            Fold(first.Layers.Woodland.Rows), Fold(second.Layers.Woodland.Rows));
    }

    /// <summary>Every table's fold but the terrain one, in composition order.</summary>
    private static ulong[] ExceptGround(World world)
    {
        var folds = new List<ulong>();

        foreach (Rows table in world.Tables)
        {
            // The two tables the WorldKey is ALLOWED to reach, and the list is the assertion. Terrain
            // joined at milestone 24 task 2 (adr/0157), Woodland at task 8a (adr/0158) -- and this
            // method's own doc-comment predicted the second arrival before it happened: "a second
            // table starting to draw fails it". It did, and the failure was the test working.
            if (ReferenceEquals(table, world.Layers.Terrain.Rows)
                || ReferenceEquals(table, world.Layers.Woodland.Rows))
            {
                continue;
            }

            folds.Add(Fold(table));
        }

        return [.. folds];
    }

    private static ulong Fold(Rows table)
    {
        ulong hash = 0;
        table.Fold(ref hash);

        return hash;
    }

    /// <summary>
    /// A second Populate is refused rather than doubling the city.
    /// </summary>
    /// <remarks>
    /// The failure it prevents is not a crash but a plausible wrong answer: a run reporting footprint
    /// and Tick figures for twice the population every table was sized against, and reporting them as
    /// success.
    /// </remarks>
    [Fact]
    public void Populating_a_second_time_is_refused()
    {
        InputLogBuilder builder = Builder();
        builder.Append(new Ticks(0), Command());
        builder.Append(new Ticks(1), Command());

        Assert.Throws<InvalidOperationException>(() =>
            Replay.Run(builder.Build(), new Ticks(4), hashEvery: 4));
    }

    [Fact]
    public void A_populated_world_satisfies_the_end_of_run_invariants()
    {
        Simulation simulation = Run(Log());

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// The verb survives the round trip through the on-disk log, which is what makes an <c>S0</c> run
    /// reproducible by somebody who has only the file.
    /// </summary>
    [Fact]
    public void A_populate_command_survives_the_codec()
    {
        InputLog written = Log();
        InputLog read = InputLogCodec.FromText(InputLogCodec.ToText(written));

        (Ticks tick, Command command) = read.Entry(0);

        Assert.Equal(1, read.Count);
        Assert.Equal(0UL, tick.Raw);
        Assert.Equal(CommandKind.Populate, command.Kind);
        Assert.Equal(Population, read.Configuration.Citizens);
    }

    /// <summary>
    /// The format version did not move for a verb, so a reader that does not know one says so by name.
    /// </summary>
    [Fact]
    public void An_unknown_verb_is_refused_by_name_rather_than_by_version()
    {
        string text = InputLogCodec.ToText(Log()).Replace(
            "populate", "conjure", StringComparison.Ordinal);

        FormatException complaint = Assert.Throws<FormatException>(() => InputLogCodec.FromText(text));

        Assert.Contains("conjure", complaint.Message, StringComparison.Ordinal);
    }

    private static Simulation Run(InputLog log)
    {
        Simulation simulation = Replay.Start(log, Ruleset.Empty);
        Replay.Trace(simulation, log, new Ticks(8), hashEvery: 8, trace: []);
        return simulation;
    }

    private static InputLog Log(ulong seed = Seed)
    {
        InputLogBuilder builder = Builder(seed);
        builder.Append(new Ticks(0), Command());
        return builder.Build();
    }

    private static InputLogBuilder Builder(ulong seed = Seed) =>
        new(seed, new WorldConfiguration(Population), ContentHash.None);

    private static Command Command() =>
        new(CommandKind.Populate, new Tiles(0), new Tiles(0));
}
