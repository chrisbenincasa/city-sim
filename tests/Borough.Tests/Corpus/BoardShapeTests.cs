using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// The board's own three rules, enforced — because prose asking politely did not hold.
/// </summary>
/// <remarks>
/// <b><c>plans/0000-board.md</c> states three rules about itself, and by 2026-08-22 all three were
/// broken.</b> It is a <em>view</em> over <c>docs/06-roadmap.md</c> (why in this order) and
/// <c>plans/0003-build-plan.md</c> (what is done); it owns nothing and every row is a pointer. Left
/// unchecked it becomes a second ledger, and then the copy that drifts — which is
/// <c>plans/0012</c> <em>Cause 1</em>.
/// <para>
/// <b>The history is the argument for a test rather than another clearing.</b> The file was founded at
/// <b>132</b> lines, reached <b>1,234</b> in four days, was hand-cleared to 900, reached <b>1,504</b>,
/// was hand-cleared to 743, and reached <b>925</b> again. Two clearings, both by hand, both grown back
/// within days. Nothing in <c>tests/Borough.Tests/Corpus/</c> looked at the board through any of it.
/// </para>
/// <para>
/// <b>⚠ A test cannot fix what made the third inflation different, and must not be read as having
/// done so.</b> That one had a structural cause: <c>plans/0003</c> covered Phase 0 and Phase 1 only,
/// so per-milestone status for eleven shipped Phase 2 milestones had no owner and the board grew a
/// 551-line <em>What is next</em> doing a ledger's job. The repair was to give <c>0003</c> a Phase 2
/// ledger. <em><b>A document that declines a layer does not thereby abolish it</b></em> — so what
/// these checks catch is the <em>symptom</em>, early, and the question they should prompt is always
/// <em>which document should have held this?</em>
/// </para>
/// </remarks>
public sealed class BoardShapeTests
{
    private const string BoardPath = "plans/0000-board.md";

    /// <summary>
    /// <b>The board may not exceed this many lines.</b> ~3× the 132-line charter, and far below every
    /// one of the three inflations (1,234 / 1,504 / 925), so it fires as a warning rather than as a
    /// verdict on a file that already needs rewriting.
    /// </summary>
    private const int LineCeiling = 400;

    /// <summary>
    /// <b>And this many bytes.</b> Lines alone are gameable — the 925-line board was 113 KB, because
    /// the bloat was inside table cells and not in the line count.
    /// </summary>
    private const int ByteCeiling = 40 * 1024;

    /// <summary><b>Rule 2's number.</b> The board's own wording, not a value chosen here.</summary>
    private const int SentenceCeiling = 3;

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

    private static string[] BoardLines() =>
        WithoutFences(File.ReadAllLines(Path.Combine(RepoRoot(), BoardPath)));

    /// <summary>Drops fenced blocks, whose contents are commands rather than prose.</summary>
    private static string[] WithoutFences(string[] lines)
    {
        var kept = new List<string>(lines.Length);
        bool fenced = false;

        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                kept.Add(string.Empty);
                continue;
            }

            kept.Add(fenced ? string.Empty : line);
        }

        return [.. kept];
    }

    private static readonly Regex Separator =
        new(@"^\|[\s:\-|]+\|\s*$", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private static readonly Regex CodeSpan =
        new(@"`[^`\n]*`", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>Markdown links, whose URLs carry punctuation that is not prose.</summary>
    private static readonly Regex Link =
        new(@"\[[^\]]*\]\([^)]*\)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>
    /// <b>A sentence ends at <c>.</c>, <c>!</c> or <c>?</c> followed by space, bold, or end of cell.</b>
    /// Requiring the follower is what keeps <c>8.72 ms</c>, <c>v/c</c>, <c>S2 R5.4</c> and
    /// <c>0.0%</c> — of which the board has many — from reading as sentence ends.
    /// </summary>
    private static readonly Regex SentenceEnd =
        new(@"[.!?](?:\s|\*\*|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>Cells short enough to be a label rather than prose are not counted.</summary>
    private const int CellProseThreshold = 40;

    /// <summary>Every table row on the board, paired with its 1-based line number.</summary>
    private static IEnumerable<(int Line, string Text)> TableRows(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith('|') && !Separator.IsMatch(lines[i]))
            {
                yield return (i + 1, lines[i]);
            }
        }
    }

    /// <summary>
    /// Splits a table row into its cells.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The trailing <c>|</c> is OPTIONAL and this used to require it</b>, dropping the last cell
    /// of any row that omitted one — <c>i &lt; parts.Length - 1</c> exists to skip the empty string
    /// after a trailing pipe, and a row without one has a real cell in that position instead.
    /// <b>Found 2026-08-26 by milestone 26 task 7</b>: exactly one row on the board lacked the pipe,
    /// it was row 1, and the cell it hid was <b>1,724 characters and seven sentences</b> against a
    /// ceiling of three. ⚠ <b>The busiest cell on the board was the one cell rule 2 could not see</b>,
    /// and it had been growing there for days while the check reported green.
    /// <para>
    /// ***A parser that assumes well-formed input fails silently on the row that needs it most***,
    /// because the row somebody keeps appending to is the row whose punctuation eventually goes wrong.
    /// The repair is to normalise the row rather than to fix the board, so the next omission costs
    /// nothing.
    /// </para>
    /// </remarks>
    private static IEnumerable<string> Cells(string row)
    {
        string trimmed = row.Trim();

        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        foreach (string part in trimmed.Split('|'))
        {
            yield return part.Trim();
        }
    }

    /// <summary>Strips what is not prose, so punctuation inside a URL or a symbol cannot match.</summary>
    private static string Prose(string text) => Link.Replace(CodeSpan.Replace(text, string.Empty), string.Empty);

    // ---- the three rules, as pure functions over lines, so a synthetic board can exercise them ----

    /// <summary><b>Rule 1.</b> Reports every line that poses a question.</summary>
    internal static List<string> Rule1Violations(string[] lines)
    {
        var found = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            if (Prose(lines[i]).Contains('?'))
            {
                found.Add($"{BoardPath}:{i + 1} — {lines[i].Trim()}");
            }
        }

        return found;
    }

    /// <summary><b>Rule 2.</b> Reports every table cell over the sentence ceiling.</summary>
    internal static List<string> Rule2Violations(string[] lines)
    {
        var found = new List<string>();

        foreach ((int line, string row) in TableRows(lines))
        {
            foreach (string cell in Cells(row))
            {
                if (cell.Length < CellProseThreshold)
                {
                    continue;
                }

                int sentences = SentenceEnd.Count(cell);

                if (sentences > SentenceCeiling)
                {
                    found.Add($"{BoardPath}:{line} — {sentences} sentences, {cell.Length} chars");
                }
            }
        }

        return found;
    }

    /// <summary><b>Rule 3.</b> Reports every table row that has already closed.</summary>
    internal static List<string> Rule3Violations(string[] lines)
    {
        var found = new List<string>();

        foreach ((int line, string row) in TableRows(lines))
        {
            string prose = Prose(row);

            if (prose.Contains('✅') || prose.Contains("~~", StringComparison.Ordinal))
            {
                found.Add($"{BoardPath}:{line} — {row.Trim()[..Math.Min(90, row.Trim().Length)]}…");
            }
        }

        return found;
    }

    /// <summary>
    /// <b>Rule 1 — the board does not hold an open question.</b> Every one belongs to
    /// <c>plans/0002-open-questions.md</c>, which is the file named for them.
    /// </summary>
    /// <remarks>
    /// <b>The board once held 63 open questions while <c>0002</c> held none.</b> That is the failure
    /// this rule exists for, and it is not a small one: a question on a view is a question nobody
    /// triages, because the view is read for <em>what is next</em> and skimmed past everywhere else.
    /// <para>
    /// <b>⚠ This check finds the literal form only, and that limit is stated rather than hidden.</b> A
    /// question mark is detectable; a question written as a statement — <em>"the payee is unsolved"</em>
    /// — is not, and no regex will find it. So a green result here is evidence about punctuation and
    /// not a certificate that the rule is kept. <em><b>A mechanical check is a floor and never a
    /// verdict.</b></em>
    /// </para>
    /// </remarks>
    [Fact]
    public void The_board_poses_no_question()
    {
        List<string> offenders = Rule1Violations(BoardLines());

        Assert.True(
            offenders.Count == 0,
            $"{BoardPath} rule 1 — *do not write an open question here*. These lines pose one:\n  "
            + string.Join("\n  ", offenders)
            + "\n\nMove it to plans/0002-open-questions.md, typed *measurable* or *arguable* under "
            + "adr/0043, and leave a pointer here if the row needs one. If the question mark is "
            + "rhetorical, rephrase it: the board is read for what to do next, and a reader cannot "
            + "tell your rhetorical question from a real one.");
    }

    /// <summary>
    /// <b>Rule 2 — a cell is at most three sentences.</b> The reasoning belongs to the slice plan, the
    /// spike plan or the ADR, and the board links to it.
    /// </summary>
    /// <remarks>
    /// <b>This is the rule that actually detects inflation, because inflation happens inside cells.</b>
    /// On 2026-08-22 the board had ten cells over the ceiling; the worst was <b>15 sentences and 3,986
    /// characters</b> in a single table cell, and a second held 12 sentences and 2,994. The line count
    /// barely moved while that was happening, which is why <see cref="The_board_stays_scannable"/>
    /// alone would not have caught it.
    /// <para>
    /// <b>Cells under <see cref="CellProseThreshold"/> characters are not counted</b> — those are
    /// labels, dates and pointers, and a rule about prose should not fire on <c>none</c> or
    /// <c>2026-08-19</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_board_cell_exceeds_three_sentences()
    {
        List<string> offenders = Rule2Violations(BoardLines());

        Assert.True(
            offenders.Count == 0,
            $"{BoardPath} rule 2 — *a cell here is at most three sentences*. Over the ceiling:\n  "
            + string.Join("\n  ", offenders)
            + "\n\nThe reasoning belongs to the document that owns the work — the slice plan, the "
            + "spike plan, the ADR — and this file links to it. If there is no such document, that "
            + "is the finding: the cell is long because a layer has no owner, which is exactly how "
            + "the 551-line *What is next* happened. Give it a home first, then point at it.");
    }

    /// <summary>
    /// <b>Rule 3 — a closed row leaves.</b> Closed rows go to <c>plans/0000a-board-archive.md</c>, one
    /// line each, naming the document that holds the full record.
    /// </summary>
    /// <remarks>
    /// <b>A view that carries its own history stops being scannable</b>, which is the whole reason
    /// <c>0000a</c> exists. Both hand-clearings were mostly closed-row narrative: ~400 lines of it went
    /// on 2026-08-15 alone.
    /// <para>
    /// <b>The check is for ✅ and <c>~~strikethrough~~</c> inside a table row</b>, which is how this
    /// corpus marks a thing as done. Prose may say ✅ freely — a paragraph reporting that a gate
    /// cleared is a statement about the present, while a ✅ <em>row</em> is a row that has stopped
    /// being next.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_closed_row_has_left_the_board()
    {
        List<string> offenders = Rule3Violations(BoardLines());

        Assert.True(
            offenders.Count == 0,
            $"{BoardPath} rule 3 — *a closed row leaves*. These rows have closed:\n  "
            + string.Join("\n  ", offenders)
            + "\n\nMove each to plans/0000a-board-archive.md as ONE line naming the document that "
            + "owns the record, and delete it here. Do not summarise the finding on the way out — "
            + "0000a is an index, and a one-line summary of somebody else's sentence is plans/0012 "
            + "Cause 5 by construction.");
    }

    /// <summary>
    /// <b>The board stays scannable.</b> A ceiling on the whole file, in lines and in bytes.
    /// </summary>
    /// <remarks>
    /// <b>This is the backstop, not the detector.</b> Rule 2 catches inflation while it is still one
    /// cell; this fires only once the file as a whole has gone. It is here because all three
    /// inflations were noticed by a human reading the file and thinking <em>this has got long</em>,
    /// which is not a mechanism.
    /// <para>
    /// <b>Two ceilings, because either alone is gameable.</b> The 925-line board was 113 KB — 122
    /// bytes a line — so a byte ceiling catches cell bloat that a line ceiling sleeps through, and a
    /// line ceiling catches the sprawl of many short rows that a byte ceiling would allow.
    /// </para>
    /// <para>
    /// <b>⚠ Neither number is a budget anybody may spend up to.</b> The board was 221 lines and 14 KB
    /// after the 2026-08-22 clearing, and the charter was 132 lines. <em><b>A ceiling set well above
    /// the target is a tripwire and not a target</b></em> — if this fires, the question is not
    /// <em>which lines do I delete</em> but <em>which document should have held this?</em>
    /// </para>
    /// </remarks>
    [Fact]
    public void The_board_stays_scannable()
    {
        string path = Path.Combine(RepoRoot(), BoardPath);
        int lines = File.ReadAllLines(path).Length;
        long bytes = new FileInfo(path).Length;

        Assert.True(
            lines <= LineCeiling && bytes <= ByteCeiling,
            $"{BoardPath} is {lines} lines and {bytes:N0} bytes, against a ceiling of "
            + $"{LineCeiling} lines and {ByteCeiling:N0} bytes.\n\n"
            + "The board is a VIEW over docs/06-roadmap.md and plans/0003-build-plan.md and it owns "
            + "nothing. It has inflated three times — to 1,234, 1,504 and 925 lines — and the first "
            + "two were hand-cleared and grew back within days, because clearing treats the symptom.\n\n"
            + "Before deleting anything, ask which document should have held it. The 2026-08-22 "
            + "inflation was a 551-line *What is next* doing a ledger's job, because plans/0003 "
            + "covered Phase 0 and Phase 1 only and Phase 2 status had no owner. A document that "
            + "declines a layer does not thereby abolish it.\n\n"
            + "And check nothing is lost that lives ONLY here before you cut: the 2026-08-22 "
            + "clearing found five such findings and relocated them to plans/0012 and plans/0035.");
    }

    /// <summary>
    /// <b>Every rule above fires on a board that breaks it.</b> A check nobody has watched fail is a
    /// check nobody knows the polarity of.
    /// </summary>
    /// <remarks>
    /// <b>This is <c>CLAUDE.md</c>'s <em>every diagnostic ships with a test that writes the violation
    /// and watches it fire</em>, applied to a document check.</b> It also pins the two false positives
    /// the sentence counter was actually at risk of: <c>8.72 ms at 1M</c> and <c>v/c peaks at 0.44</c>
    /// are one sentence each, not three and two — a decimal point is not a full stop, and the board is
    /// full of both.
    /// </remarks>
    [Fact]
    public void The_three_checks_fire_on_a_board_that_breaks_each_rule()
    {
        string[] clean =
        [
            "| | Track | Task |",
            "|---|---|---|",
            "| **1** | code | Milestone 12 — the District Pool, scoping under way in `0037`. |",
            "The good one is **8.72 ms a Tick at 1M**, and `v/c` peaks at 0.44 at every population.",
        ];

        Assert.Empty(Rule1Violations(clean));
        Assert.Empty(Rule2Violations(clean));
        Assert.Empty(Rule3Violations(clean));

        string[] poses = ["Should the Pool be a market or a Bin lookup, and who pays the haulage?"];
        Assert.Single(Rule1Violations(poses));

        string[] overlong =
        [
            "| | Task |",
            "|---|---|",
            "| **1** | One sentence here. Two sentences here. Three sentences here. Four is over. |",
        ];
        Assert.Single(Rule2Violations(overlong));

        string[] closed =
        [
            "| | Milestone |",
            "|---|---|",
            "| **11** | ✅ **DONE 2026-08-21**, all nine tasks and all ten decisions settled. |",
        ];
        Assert.Single(Rule3Violations(closed));

        // 🔴 THE TRAILING PIPE IS OPTIONAL, AND REQUIRING IT HID THE BOARD'S LARGEST CELL FOR DAYS.
        // Cells() skips one empty part at the end to account for a trailing '|'; a row without one has
        // a real cell there instead. On 2026-08-26 exactly one row on the board omitted it, it was row
        // 1, and the cell it hid was seven sentences and 1,724 characters against a ceiling of three.
        // ⚠ Both spellings must report the SAME violation, which is what this pair asserts -- a check
        // that reads one dialect of its own input format is a check with a silent exemption in it.
        string[] noTrailingPipe =
        [
            "| | Task |",
            "|---|---|",
            "| **1** | One sentence here. Two sentences here. Three sentences here. Four is over.",
        ];
        Assert.Single(Rule2Violations(noTrailingPipe));

        // A link's URL and a code span carry punctuation that is not prose, and must not be read as any.
        string[] punctuationInside =
        [
            "See [the ADR](../docs/adr/0134-a-district-is-a-centre.md?x=1) and `Math.Max(a, b)`.",
        ];
        Assert.Empty(Rule1Violations(punctuationInside));
    }
}
