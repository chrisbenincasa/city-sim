using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Tests.Golden;

/// <summary>
/// The golden-hash baseline: a committed session and a committed world, each with the hash it
/// produced when somebody last said it was correct.
/// </summary>
/// <remarks>
/// <para>
/// <b>The point is not that the hash never moves. It is that it never moves without somebody saying
/// so.</b> Every other determinism test in this project is a closed loop — it runs something twice
/// and checks the two agree, which stays true no matter how far the simulation drifts from what it
/// used to compute. These are the only tests anchored to a number recorded on a previous day, and so
/// the only ones that can notice a change nobody was looking for.
/// </para>
/// <para>
/// <b>A failure here is not a bug report.</b> It is a question: <em>did you mean to do that?</em>
/// The answer is often yes, and the procedure for saying so is in <c>README.md</c> in this
/// directory. What the procedure defends against is the silent case — the refactor that looked
/// hash-preserving, the reordered fold, the column that quietly changed width.
/// </para>
/// <para>
/// <b>The files are read through <see cref="HashTrace"/>, which is also what the runner writes.</b>
/// One format and one reader, so a re-baseline is a command whose output is committed rather than a
/// transcription between two shapes — and comparison is over parsed structure rather than bytes,
/// because line endings differ between the machines this repository is checked out on.
/// </para>
/// </remarks>
public sealed class GoldenHashTests
{
    private const string TraceFile = "session-trace.txt";
    private const string WorldFile = "world-hash.txt";
    private const string SessionFile = "session.borough";

    /// <summary>The command that regenerates <c>session-trace.txt</c>. Named by the failure message.</summary>
    private const string Regenerate =
        "dotnet run --project src/Borough.Headless -- "
        + "--log tests/Borough.Tests/Golden/session.borough --ruleset rulesets/minimal.toml "
        + "--ticks 256 --hash-every 8 "
        + "--out tests/Borough.Tests/Golden/session-trace.txt";

    /// <summary>
    /// The committed session still produces the committed trace, sample for sample.
    /// </summary>
    [Fact]
    public void The_golden_session_reproduces_its_committed_trace()
    {
        HashTrace baseline = Read(TraceFile);
        ulong[] recorded = baseline.Hashes();
        ulong[] observed = Replay.Run(
            GoldenFixtures.Session(),
            new Ticks(GoldenFixtures.Ticks),
            GoldenFixtures.HashEvery,
            GoldenFixtures.Rules());

        if (!observed.AsSpan().SequenceEqual(recorded))
        {
            Assert.Fail(
                Divergence(recorded, observed)
                + $"\n\nIf you meant it, re-baseline with:\n\n  {Regenerate}\n\n"
                + "and say why in the commit message. README.md beside this file has the rest.");
        }
    }

    /// <summary>
    /// The committed trace still describes the session the fixture builds.
    /// </summary>
    /// <remarks>
    /// Without this, a fixture edited in the same commit as a regenerated trace would pass silently
    /// while the two artefacts had quietly stopped describing the same session. It is also the error
    /// message worth having: <em>the seed moved</em> is a diagnosis, and thirty-two differing hashes
    /// is a puzzle.
    /// </remarks>
    [Fact]
    public void The_committed_trace_still_describes_the_fixture_session()
    {
        HashTrace baseline = Read(TraceFile);
        InputLog session = GoldenFixtures.Session();

        Assert.Equal(session.Seed, baseline.Number("seed"));
        Assert.Equal((ulong)session.Configuration.Citizens, baseline.Number("citizens"));
        Assert.Equal(session.RulesetHash, baseline.Number("ruleset"));
        Assert.Equal((ulong)session.Count, baseline.Number("commands"));
        Assert.Equal((ulong)GoldenFixtures.Ticks, baseline.Number("ticks"));
        Assert.Equal((ulong)GoldenFixtures.HashEvery, baseline.Number("hash-every"));
        Assert.Equal(GoldenFixtures.Ticks / GoldenFixtures.HashEvery, baseline.Samples.Count);
    }

    /// <summary>
    /// <b>The committed <c>.borough</c> file is the session the fixture builds</b> — the codec test
    /// task 4 set up for task 5 to inherit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the round trip that matters, and it is stronger than <c>InputLogCodecTests</c>'
    /// because nothing here writes the file first. A codec checked only against its own output
    /// agrees with itself by construction; this one is checked against a file that was on disk
    /// before the run started, which is the situation a bug report actually creates.
    /// </para>
    /// <para>
    /// It also closes <c>adr/0039</c>'s loop from the other end. The whole argument for a fifth
    /// project was that a log written by one shell must replay in another; the evidence for that
    /// property is a file, parsed by the one codec, reproducing a hash sequence recorded
    /// independently of it.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_committed_session_file_parses_to_the_fixture_and_reproduces_the_trace()
    {
        using var reader = new StreamReader(Path(SessionFile));
        InputLog parsed = InputLogCodec.Read(reader);
        InputLog fixture = GoldenFixtures.Session();

        Assert.Equal(fixture.Seed, parsed.Seed);
        Assert.Equal(fixture.Configuration.Citizens, parsed.Configuration.Citizens);
        Assert.Equal(fixture.RulesetHash, parsed.RulesetHash);
        Assert.Equal(fixture.Count, parsed.Count);

        for (int i = 0; i < fixture.Count; i++)
        {
            (Ticks tick, Command command) = fixture.Entry(i);
            (Ticks parsedTick, Command parsedCommand) = parsed.Entry(i);

            Assert.Equal(tick, parsedTick);
            Assert.Equal(command.Kind, parsedCommand.Kind);
            Assert.Equal(command.East, parsedCommand.East);
            Assert.Equal(command.North, parsedCommand.North);
            Assert.Equal(command.Zone, parsedCommand.Zone);
        }

        Assert.Equal(
            Read(TraceFile).Hashes(),
            Replay.Run(
                parsed,
                new Ticks(GoldenFixtures.Ticks),
                GoldenFixtures.HashEvery,
                GoldenFixtures.Rules()));
    }

    /// <summary>
    /// <b>The committed Ruleset is the one the committed session names.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The session records a Ruleset content hash and the runner refuses a run whose supplied
    /// Ruleset does not match it. That check is only as good as the file it is taken over, and this
    /// is the line that keeps the two together: edit <c>rulesets/minimal.toml</c> and this fails with
    /// the number to paste into <see cref="GoldenFixtures.RulesetHash"/> — at which point the trace
    /// has to be regenerated too, which is the whole point.
    /// </para>
    /// <para>
    /// <b>It also catches the change that is not an edit.</b> A Ruleset that stopped being copied
    /// beside the test assembly, or one loaded from the source tree instead, would make every other
    /// test here pass over a file the runner never reads.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_golden_ruleset_is_the_one_the_session_names()
    {
        ulong observed = RulesetFile.HashOf(GoldenFixtures.RulesetPath);

        Assert.True(
            observed == GoldenFixtures.RulesetHash,
            $"the golden Ruleset hashes to 0x{observed:X16} and the session names "
            + $"0x{GoldenFixtures.RulesetHash:X16}. If you meant to change the Ruleset, put that "
            + "number in GoldenFixtures.RulesetHash and in session.borough, then re-baseline the "
            + $"trace:\n\n  {Regenerate}");
    }

    /// <summary>
    /// The committed world still hashes to the committed number, and still has the rows it had.
    /// </summary>
    /// <remarks>
    /// The row counts are recorded beside the hash because a hash tells you only that something
    /// moved. If a Household stopped taking its members with it, this line says so and the hash line
    /// does not.
    /// </remarks>
    [Fact]
    public void The_golden_world_reproduces_its_committed_hash()
    {
        HashTrace baseline = Read(WorldFile);
        World world = GoldenFixtures.Build();
        ulong observed = world.HashState();

        string[] complaints =
        [
            .. Differs("population", (ulong)GoldenFixtures.Population, baseline),
            .. Differs("lots", (ulong)world.Lots.Rows.LiveCount, baseline),
            .. Differs("buildings", (ulong)world.Buildings.Rows.LiveCount, baseline),
            .. Differs("households", (ulong)world.Households.Rows.LiveCount, baseline),
            .. Differs("citizens", (ulong)world.Citizens.Rows.LiveCount, baseline),
            .. Differs("hash", observed, baseline),
        ];

        if (complaints.Length > 0)
        {
            Assert.Fail(string.Join('\n', complaints) + "\n\n" + Regenerated(world, observed));
        }
    }

    /// <summary>
    /// One complaint if a recorded value moved, none if it did not.
    /// </summary>
    /// <remarks>
    /// Collected rather than asserted one at a time, because the first mismatch is rarely the
    /// informative one — <em>the hash moved and so did the Citizen count</em> is a diagnosis, and
    /// stopping at the count would have withheld half of it.
    /// </remarks>
    private static string[] Differs(string key, ulong observed, HashTrace baseline)
    {
        ulong recorded = baseline.Number(key);

        return observed == recorded
            ? []
            : [$"{key}: 0x{observed:X} observed, 0x{recorded:X} committed."];
    }

    /// <summary>
    /// Names the first sample that moved, and the window of Ticks the change entered in.
    /// </summary>
    /// <remarks>
    /// <b>This is the bisection property being spent rather than merely proven.</b> A sampled trace
    /// narrows the change to a cadence-wide window and no further, so it says so and names the next
    /// move — claiming the exact Tick would be a precision the sampling does not have, and the first
    /// person to trust it would lose an afternoon in the wrong Tick.
    /// </remarks>
    private static string Divergence(ulong[] recorded, ulong[] observed)
    {
        if (recorded.Length != observed.Length)
        {
            return $"the trace has {observed.Length} samples and {recorded.Length} are committed.";
        }

        for (int i = 0; i < recorded.Length; i++)
        {
            if (recorded[i] != observed[i])
            {
                int after = (i + 1) * GoldenFixtures.HashEvery;
                int from = (i * GoldenFixtures.HashEvery) + 1;

                return $"sample {i}, the state after {after} Ticks: "
                     + $"0x{observed[i]:X16} observed, 0x{recorded[i]:X16} committed.\n"
                     + $"The change entered somewhere in Ticks {from}..{after}. "
                     + "Re-run this session at --hash-every 1 to name the Tick exactly.";
            }
        }

        return "the trace differs, and yet no sample does — which cannot happen.";
    }

    /// <summary>
    /// Renders the world baseline as the file it would be committed as.
    /// </summary>
    /// <remarks>
    /// <b>The session's baseline is regenerated by the runner and this one is not</b>, because the
    /// runner runs sessions and this world is built by hand rather than played. It is an asymmetry
    /// that goes away by itself: once slice 7 gives the player verbs that raise Buildings, the
    /// session covers what this fixture covers and the fixture is deleted.
    /// </remarks>
    private static string Regenerated(World world, ulong observed)
    {
        string text = HashTrace.Create()
            .With("population", (ulong)GoldenFixtures.Population)
            .With("lots", (ulong)world.Lots.Rows.LiveCount)
            .With("buildings", (ulong)world.Buildings.Rows.LiveCount)
            .With("households", (ulong)world.Households.Rows.LiveCount)
            .With("citizens", (ulong)world.Citizens.Rows.LiveCount)
            .With("hash", observed)
            .ToText(
                "Borough golden world hash. Recorded from GoldenFixtures.Build().",
                "Regenerating this file is a deliberate, signed act: follow the",
                "procedure in README.md beside it.");

        return $"If you meant it, {WorldFile} becomes:\n\n{text}";
    }

    private static HashTrace Read(string name)
    {
        using var reader = new StreamReader(Path(name));
        return HashTrace.Read(reader);
    }

    private static string Path(string name)
    {
        string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Golden", name);

        return File.Exists(path)
            ? path
            : throw new FileNotFoundException(
                $"the golden baseline {name} is missing. It is committed, not generated: "
                + "a run that cannot find it has a build problem, not a hash problem.",
                path);
    }
}
