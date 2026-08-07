using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
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
    /// Populating draws no randomness, so the world seed cannot reach it.
    /// </summary>
    /// <remarks>
    /// <b>Stated as a test because the failure is silent and one-directional.</b> Somebody adding
    /// variety to the fixture with a convenient <c>draw()</c> would correlate it with whatever
    /// simulation decision shares the <c>purpose_tag</c>, and nothing else in the suite would notice.
    /// This fails the moment the fixture starts drawing.
    /// </remarks>
    [Fact]
    public void The_city_is_a_function_of_its_size_and_not_of_the_seed()
    {
        ulong[] first = Replay.Run(Log(Seed), new Ticks(4), hashEvery: 4);
        ulong[] second = Replay.Run(Log(Seed ^ 0xFFFF_FFFF_FFFF_FFFFUL), new Ticks(4), hashEvery: 4);

        Assert.Equal(first, second);
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
        Simulation simulation = Replay.Start(log);
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
