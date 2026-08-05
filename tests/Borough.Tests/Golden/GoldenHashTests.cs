using System.Globalization;
using System.Text;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;

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
/// used to compute. These two tests are the only ones anchored to a number recorded on a previous
/// day, and so the only ones that can notice a change nobody was looking for.
/// </para>
/// <para>
/// <b>A failure here is not a bug report.</b> It is a question: <em>did you mean to do that?</em>
/// The answer is often yes, and the procedure for saying so is in <c>README.md</c> in this
/// directory. What the procedure is defending against is the silent case — the refactor that looked
/// hash-preserving, the reordered fold, the column that quietly changed width.
/// </para>
/// <para>
/// <b>The files are compared as parsed structures, never as bytes.</b> Line endings differ between
/// the machines this repository is checked out on and a baseline that failed on a checkout setting
/// would be a baseline people learn to ignore.
/// </para>
/// </remarks>
public sealed class GoldenHashTests
{
    private const string TraceFile = "session-trace.txt";
    private const string WorldFile = "world-hash.txt";

    /// <summary>
    /// The committed session still produces the committed trace, sample for sample.
    /// </summary>
    [Fact]
    public void The_golden_session_reproduces_its_committed_trace()
    {
        Baseline baseline = Baseline.Read(TraceFile);
        ulong[] recorded = baseline.Samples;
        ulong[] observed = Replay.Run(
            GoldenFixtures.Session(),
            new Ticks(GoldenFixtures.Ticks),
            GoldenFixtures.HashEvery);

        if (!observed.AsSpan().SequenceEqual(recorded))
        {
            Assert.Fail(Divergence(recorded, observed) + "\n\n" + FormatTrace(observed));
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
        Baseline baseline = Baseline.Read(TraceFile);
        InputLog session = GoldenFixtures.Session();

        Assert.Equal(session.Seed, baseline.Number("seed"));
        Assert.Equal((ulong)session.Configuration.Citizens, baseline.Number("citizens"));
        Assert.Equal(session.RulesetHash, baseline.Number("ruleset"));
        Assert.Equal((ulong)GoldenFixtures.Ticks, baseline.Number("ticks"));
        Assert.Equal((ulong)GoldenFixtures.HashEvery, baseline.Number("hash-every"));
        Assert.Equal(GoldenFixtures.Ticks / GoldenFixtures.HashEvery, baseline.Samples.Length);
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
        Baseline baseline = Baseline.Read(WorldFile);
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
            Assert.Fail(string.Join('\n', complaints) + "\n\n" + FormatWorld(world, observed));
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
    private static string[] Differs(string key, ulong observed, Baseline baseline)
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
                     + "Re-run this session at hash-every 1 to name the Tick exactly.";
            }
        }

        return "the trace differs, and yet no sample does — which cannot happen.";
    }

    /// <summary>
    /// Renders the observed trace as the file it would be committed as, so a deliberate re-baseline
    /// is a copy from the failure message rather than a script nobody reviews.
    /// </summary>
    /// <remarks>
    /// <b>There is deliberately no self-regenerating switch.</b> An environment variable that
    /// rewrites the baseline is one CI misconfiguration away from a baseline that approves every
    /// change it sees, which is a baseline that has stopped being one.
    /// </remarks>
    private static string FormatTrace(ulong[] observed)
    {
        InputLog session = GoldenFixtures.Session();
        var text = new StringBuilder();

        text.Append(Header("session trace", "GoldenFixtures.Session()"));
        text.Append(CultureInfo.InvariantCulture, $"seed 0x{session.Seed:X16}\n");
        text.Append(CultureInfo.InvariantCulture, $"citizens {session.Configuration.Citizens}\n");
        text.Append(CultureInfo.InvariantCulture, $"ruleset 0x{session.RulesetHash:X16}\n");
        text.Append(CultureInfo.InvariantCulture, $"ticks {GoldenFixtures.Ticks}\n");
        text.Append(CultureInfo.InvariantCulture, $"hash-every {GoldenFixtures.HashEvery}\n");
        text.Append("--\n");

        for (int i = 0; i < observed.Length; i++)
        {
            int tick = (i + 1) * GoldenFixtures.HashEvery;
            text.Append(CultureInfo.InvariantCulture, $"{tick} 0x{observed[i]:X16}\n");
        }

        return $"to re-baseline deliberately, {TraceFile} becomes:\n\n{text}";
    }

    /// <inheritdoc cref="FormatTrace"/>
    private static string FormatWorld(World world, ulong observed)
    {
        var text = new StringBuilder();

        text.Append(Header("world hash", "GoldenFixtures.Build()"));
        text.Append(CultureInfo.InvariantCulture, $"population {GoldenFixtures.Population}\n");
        text.Append(CultureInfo.InvariantCulture, $"lots {world.Lots.Rows.LiveCount}\n");
        text.Append(CultureInfo.InvariantCulture, $"buildings {world.Buildings.Rows.LiveCount}\n");
        text.Append(CultureInfo.InvariantCulture, $"households {world.Households.Rows.LiveCount}\n");
        text.Append(CultureInfo.InvariantCulture, $"citizens {world.Citizens.Rows.LiveCount}\n");
        text.Append(CultureInfo.InvariantCulture, $"hash 0x{observed:X16}\n");

        return $"to re-baseline deliberately, {WorldFile} becomes:\n\n{text}";
    }

    private static string Header(string what, string source) =>
        $"# Borough golden {what} -- format 1\n"
        + $"# Recorded from {source}. Regenerating this file is a deliberate,\n"
        + "# signed act: follow the procedure in README.md beside it.\n";

    /// <summary>
    /// A committed baseline file: named header values, and an optional body of samples.
    /// </summary>
    /// <remarks>
    /// <b>Line-oriented text, legible without tooling</b> — the same argument <c>adr/0039</c> made
    /// for the Input Log, and for the same reason. A baseline is read at the moment a build has just
    /// gone red, which is the moment least worth needing a tool for.
    /// </remarks>
    private sealed class Baseline
    {
        private readonly Dictionary<string, ulong> _header;

        private Baseline(Dictionary<string, ulong> header, ulong[] samples)
        {
            _header = header;
            Samples = samples;
        }

        /// <summary>The body, in file order. Empty for a file that is all header.</summary>
        public ulong[] Samples { get; }

        public static Baseline Read(string name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", name);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"the golden baseline {name} is missing. It is committed, not generated: "
                    + "a run that cannot find it has a build problem, not a hash problem.",
                    path);
            }

            var header = new Dictionary<string, ulong>(StringComparer.Ordinal);
            var samples = new List<ulong>();
            bool body = false;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();

                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (line == "--")
                {
                    body = true;
                    continue;
                }

                string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (fields.Length != 2)
                {
                    throw new FormatException($"{name}: '{line}' is not a pair.");
                }

                if (body)
                {
                    samples.Add(Parse(fields[1]));
                }
                else
                {
                    header[fields[0]] = Parse(fields[1]);
                }
            }

            return new Baseline(header, [.. samples]);
        }

        /// <summary>The header value under <paramref name="key"/>.</summary>
        public ulong Number(string key) =>
            _header.TryGetValue(key, out ulong value)
                ? value
                : throw new KeyNotFoundException($"the baseline has no '{key}' line.");

        private static ulong Parse(string value) =>
            value.StartsWith("0x", StringComparison.Ordinal)
                ? ulong.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : ulong.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
