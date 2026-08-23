namespace Borough.Tests.Corpus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// The roadmap's milestone table and the build plan's Phase 2 ledger name the same milestones.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>plans/0012</c>'s mechanical check 13, and only its cheap half.</b> The check asks
/// that milestone <em>status</em> agree between the two documents; status is prose in both, so what
/// is asserted here is the <em>row set</em> — every milestone the roadmap names has a ledger row and
/// the reverse. ⚠ <b>That is strictly weaker than the check it implements</b>, and it is built
/// because the stronger half needs a machine-readable line per milestone that check 2 has been
/// waiting for since the sweep began. <em>Deferring the whole is what left check 2 unbuilt.</em>
/// </para>
/// <para>
/// <b>The founding case.</b> On 2026-08-22 milestone 12 was capped at task 6 and milestones 25 and
/// 26 were appended to <c>06</c>. The Phase 2 ledger went on reading <c>LIVE. Scoped and
/// decomposed — ten tasks</c> for a whole commit, and had no rows for 25 or 26 at all. Every corpus
/// check passed while the two disagreed, because they all compare <b>links and shapes</b> and none
/// compares a <b>claim</b>. This one catches the half a shape can carry.
/// </para>
/// <para>
/// ⚠ <b>The ledger may fold not-started milestones into a range row</b> — <c>13</c>–<c>17</c>,
/// <c>19</c>–<c>24</c> — and that is deliberate rather than a gap, since no plan document is owed
/// until a row is next. Ranges are expanded here, so a folded milestone counts as named.
/// </para>
/// </remarks>
public sealed partial class LedgerAgreementTests
{
    /// <summary>A table row's leading cell: <c>| **12** |</c>, <c>| **5a-bis** |</c>.</summary>
    [GeneratedRegex(@"^\|\s*(?<cell>[^|]*?)\s*\|")]
    private static partial Regex LeadingCell { get; }

    /// <summary>A milestone name inside that cell — a digit run, optionally lettered.</summary>
    [GeneratedRegex(@"\*\*(?<name>\d{1,3}[a-z]*(?:-bis)?)\*\*")]
    private static partial Regex Name { get; }

    /// <summary>An inclusive range of whole-number milestones, <c>**13**–**17**</c>.</summary>
    [GeneratedRegex(@"\*\*(?<from>\d{1,3})\*\*\s*[–-]\s*\*\*(?<to>\d{1,3})\*\*")]
    private static partial Regex Range { get; }

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
    /// The milestone names in the one table whose header line starts with <paramref name="header"/>.
    /// </summary>
    /// <remarks>
    /// <b>The section anchor is load-bearing and the header alone is not enough</b> — <c>06</c> carries
    /// the identical header for Phase 1 and Phase 2, and matching the first silently compared the wrong
    /// table. Reading then stops at the first line that is not a table row, so a second table further
    /// down the file — the spike ledger, in <c>0003</c>'s case — is never swept in.
    /// </remarks>
    private static HashSet<string> Milestones(string path, string section, string header)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        string[] lines = File.ReadAllLines(path);

        int anchor = Array.FindIndex(
            lines, line => line.StartsWith(section, StringComparison.Ordinal));

        Assert.True(
            anchor >= 0,
            $"{path} has no heading starting \"{section}\". This test keys on it because the table "
            + "header alone is ambiguous — 06 carries the same header for Phase 1 and Phase 2, and "
            + "matching the first one silently compared the wrong table.");

        int start = Array.FindIndex(
            lines, anchor, line => line.StartsWith(header, StringComparison.Ordinal));

        Assert.True(
            start >= 0,
            $"{path} has no table under \"{section}\" whose header starts \"{header}\". The header "
            + "was renamed or the table was restructured.");

        for (int line = start + 2; line < lines.Length; line++)
        {
            if (!lines[line].StartsWith('|'))
            {
                break;
            }

            Match cell = LeadingCell.Match(lines[line]);

            if (!cell.Success)
            {
                continue;
            }

            string text = cell.Groups["cell"].Value;

            foreach (Match range in Range.Matches(text))
            {
                int from = int.Parse(range.Groups["from"].Value);
                int to = int.Parse(range.Groups["to"].Value);

                for (int number = from; number <= to; number++)
                {
                    found.Add(number.ToString());
                }
            }

            foreach (Match name in Name.Matches(text))
            {
                found.Add(name.Groups["name"].Value);
            }
        }

        return found;
    }

    [Fact]
    public void The_roadmap_and_the_phase_two_ledger_name_the_same_milestones()
    {
        string root = RepoRoot();

        HashSet<string> roadmap = Milestones(
            Path.Combine(root, "docs", "06-roadmap.md"),
            "## Phase 2",
            "| # | Milestone | Risk retired |");

        HashSet<string> ledger = Milestones(
            Path.Combine(root, "plans", "0003-build-plan.md"),
            "### The Phase 2 ledger",
            "| # | Milestone | Gate | Plan | State |");

        Assert.NotEmpty(roadmap);
        Assert.NotEmpty(ledger);

        string[] unledgered = [.. roadmap.Except(ledger).Order(StringComparer.Ordinal)];
        string[] unplanned = [.. ledger.Except(roadmap).Order(StringComparer.Ordinal)];

        Assert.True(
            unledgered.Length == 0 && unplanned.Length == 0,
            "docs/06-roadmap.md and plans/0003-build-plan.md disagree about which milestones exist.\n"
            + (unledgered.Length == 0
                ? string.Empty
                : $"  in 06 and not in 0003's Phase 2 ledger: {string.Join(", ", unledgered)}\n")
            + (unplanned.Length == 0
                ? string.Empty
                : $"  in 0003's Phase 2 ledger and not in 06: {string.Join(", ", unplanned)}\n")
            + "\n06 answers *what is next* and 0003 answers *what is done*, so a milestone in one and "
            + "not the other means one of those questions has a wrong answer. This is plans/0012 "
            + "mechanical check 13, and it is Cause 1 on the milestone axis: every document that "
            + "stores status drifts. Adding a milestone means adding a row to both — or, for a "
            + "not-started one, folding it into 0003's range row.");
    }
}
