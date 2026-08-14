using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// <c>plans/0012</c>'s mechanical check 5, and the sibling of <see cref="CitationTests"/> pointed the
/// other way.
/// </summary>
/// <remarks>
/// <b><see cref="CitationTests"/> asks whether an ADR is cited from outside <c>docs/adr</c>; this asks
/// whether the corpus's one coverage map knows the ADR exists.</b> The map is
/// <see href="../../plans/0002-open-questions.md">§F2</see>, and <c>adr/0043</c> cites its green marks
/// as evidence about what has been examined — which a map missing rows cannot carry, because a
/// decision nobody has assessed then reads as <b>absent</b> rather than as <b>unexamined</b>, and
/// those are opposite conclusions.
/// <para>
/// <b>This exists because the map has stopped tracking new ADRs three times and said so itself.</b> It
/// was rebuilt on 2026-08-10 on the finding that it had stopped at <c>adr/0043</c>, missing
/// twenty-two; by that evening it had stopped at <c>0059</c>; and the box recording that wrote its own
/// trigger — <em>"if it stops at some number a third time, the map wants generating from the directory
/// rather than writing."</em> On 2026-08-12 it was found stopped at <c>0070</c>, missing eleven, with
/// its header still claiming <em>"69 written, numbered to `0070`"</em>. Four separate pieces of work
/// added ADRs and none added a row.
/// </para>
/// <para>
/// <b>Only the row is checked, and that is deliberate.</b> The <em>State</em> and <em>Note</em>
/// columns are judgements and cannot be generated; what a machine can guarantee is that every
/// decision appears, so the judgement is <em>visibly missing</em> instead of silently absent. Three
/// observations of one mechanism agree: a hand-maintained index of a directory is maintained by
/// whoever remembers, and across four sittings nobody did.
/// </para>
/// </remarks>
public sealed class CoverageMapTests
{
    /// <summary>Walks up from the test assembly until the corpus is found.</summary>
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs", "adr")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "no directory above the test assembly contains docs/adr. This test reads the corpus "
            + "from disk, so it cannot run from a detached output directory.");
    }

    /// <summary>
    /// The four-digit codes in a row's first cell. One is a single ADR; two are an inclusive range,
    /// which is how the map groups decisions a single session took together — <c>`0023`–`0027`</c>.
    /// </summary>
    private static readonly Regex RowCodes = new(
        @"^\|\s*(?<codes>(?:`\d{4}`\s*[–-]?\s*)+)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private static readonly Regex Code =
        new(@"\d{4}", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>
    /// §F2's header sentence — <c>"96 written, numbered to `0097`"</c>. The count and the highest
    /// number are two facts about the directory stated in prose, which is why they are asserted here
    /// rather than read.
    /// </summary>
    private static readonly Regex Header = new(
        @"\*\*(?<written>\d+) written, numbered to `(?<highest>\d{4})`",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    /// <summary>Every ADR file's four-digit code, ascending.</summary>
    private static string[] Written(string root) =>
    [
        .. Directory.EnumerateFiles(Path.Combine(root, "docs", "adr"), "????-*.md")
            .Select(path => Path.GetFileName(path)[..4])
            .Order(StringComparer.Ordinal),
    ];

    [Fact]
    public void Every_adr_has_a_row_in_the_coverage_map()
    {
        string root = RepoRoot();

        string[] written = Written(root);

        Assert.NotEmpty(written);

        var mapped = new HashSet<string>(StringComparer.Ordinal);

        foreach (string line in File.ReadAllLines(
            Path.Combine(root, "plans", "0002-open-questions.md")))
        {
            Match row = RowCodes.Match(line);

            if (!row.Success)
            {
                continue;
            }

            int[] codes =
            [
                .. Code.Matches(row.Groups["codes"].Value)
                    .Select(match => int.Parse(match.Value)),
            ];

            // One code is a single row; two are an inclusive range covering everything between,
            // including numbers that are reserved and unwritten. Only written ADRs are asserted on,
            // so a range spanning `0028` is correct rather than a false positive.
            for (int number = codes[0]; number <= codes[^1]; number++)
            {
                mapped.Add(number.ToString("0000"));
            }
        }

        string[] missing = [.. written.Where(number => !mapped.Contains(number))];

        Assert.True(
            missing.Length == 0,
            $"these ADRs have no row in plans/0002 §F2, the coverage map: {string.Join(", ", missing)}. "
            + "The map is what adr/0043 cites as evidence for what has been examined, so an ADR "
            + "missing from it reads as *absent* rather than as *unexamined* — which is the opposite "
            + "conclusion. Add a row with its state and the session or slice that settled it. This "
            + "has now happened three times; plans/0012's mechanical check 5 is the standing fix, and "
            + "this test is it.");
    }

    /// <summary>
    /// The map's header states how many ADRs exist and which is highest. Both are facts about
    /// <c>docs/adr</c> written in prose, so both drift.
    /// </summary>
    /// <remarks>
    /// <b>The rows and the count are two different facts, and only the rows were being held.</b> On
    /// 2026-08-14 the header read <em>"83 written, numbered to `0084`"</em> against 96 files numbered
    /// to <c>0097</c> — a fourth drift — while
    /// <see cref="Every_adr_has_a_row_in_the_coverage_map"/> was green, because every one of those
    /// thirteen decisions did have a row. The failure is not a missing assessment but a **reader**
    /// one: a count is the thing skimmed, so a stale one reports the map as ending thirteen decisions
    /// before it does.
    /// <para>
    /// <c>adr/0093</c>'s amendment names the shape — <em>a count of the instruments is itself a fact
    /// stored in prose</em> — and it was coined about a list of these very checks. This is that rule
    /// applied to the header sitting above one of them.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_coverage_maps_header_counts_the_adrs_that_exist()
    {
        string root = RepoRoot();
        string[] written = Written(root);

        Assert.NotEmpty(written);

        string map = File.ReadAllText(Path.Combine(root, "plans", "0002-open-questions.md"));
        Match header = Header.Match(map);

        Assert.True(
            header.Success,
            "plans/0002 §F2 has no header of the form \"**N written, numbered to `NNNN`**\". That "
            + "sentence is what a reader skims to learn how far the coverage map reaches, so it is "
            + "held here rather than left to drift. If it has been deliberately reworded, reword this "
            + "test with it.");

        string highest = written[^1];
        int count = written.Length;

        Assert.True(
            header.Groups["written"].Value == count.ToString()
            && header.Groups["highest"].Value == highest,
            $"plans/0002 §F2's header says \"{header.Groups["written"].Value} written, numbered to "
            + $"`{header.Groups["highest"].Value}`\"; docs/adr holds {count} files numbered to "
            + $"`{highest}`. The rows may well all be present — that is the sibling check, and it has "
            + "been green through every one of these drifts. What goes stale is the count above them, "
            + "and a reader who trusts it concludes the map stops where the number says. This is the "
            + "fourth time; update the sentence.");
    }
}
