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

    [Fact]
    public void Every_adr_has_a_row_in_the_coverage_map()
    {
        string root = RepoRoot();

        string[] written =
        [
            .. Directory.EnumerateFiles(Path.Combine(root, "docs", "adr"), "????-*.md")
                .Select(path => Path.GetFileName(path)[..4])
                .Order(StringComparer.Ordinal),
        ];

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
}
