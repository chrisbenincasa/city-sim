using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// House style, for the two things that have actually gone wrong.
/// </summary>
/// <remarks>
/// <b>Both checks exist because a defect survived in the corpus undetected, and neither is a taste
/// rule dressed up as a test.</b> The corpus is ~28,000 lines of prose against ~19,600 of simulation,
/// so a prose defect has no compiler and no reviewer — the only thing that can find one is something
/// that runs.
/// <para>
/// <b>Prettier is not the alternative and cannot be.</b> It rewrites <c>*emphasis*</c> to
/// <c>_emphasis_</c> with no option to stop it, and it pads every table cell in a column to the
/// width of the widest — which on <c>plans/0000-board.md</c>, whose <em>Do these next</em> table has
/// a 4,166-character cell, takes the file from 82,450 bytes to 182,405. See <c>.prettierignore</c>.
/// </para>
/// </remarks>
public sealed class MarkdownStyleTests
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

    /// <summary>Every markdown file the corpus is written in, build output excluded.</summary>
    private static IEnumerable<string> CorpusFiles(string root)
    {
        foreach (string directory in (string[])["docs", "plans"])
        {
            foreach (string path in Directory.EnumerateFiles(
                Path.Combine(root, directory), "*.md", SearchOption.AllDirectories))
            {
                if (!IsBuildOutput(path, root))
                {
                    yield return path;
                }
            }
        }

        foreach (string name in (string[])["CONTEXT.md", "CLAUDE.md", "PROCESS.md"])
        {
            string path = Path.Combine(root, name);

            if (File.Exists(path))
            {
                yield return path;
            }
        }
    }

    private static bool IsBuildOutput(string path, string root)
    {
        string relative = Path.GetRelativePath(root, path);

        return relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                   StringComparison.Ordinal)
            || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                   StringComparison.Ordinal);
    }

    /// <summary>A table's second line: <c>|---|---|</c>, with optional alignment colons.</summary>
    private static readonly Regex Separator =
        new(@"^\|[\s:\-|]+\|\s*$", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>Drops fenced code blocks, whose contents are not prose and may contain anything.</summary>
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

    /// <summary>
    /// <b>A markdown table renders only if its rows are contiguous and the second one is a
    /// separator.</b> Anything between two rows — a blank line, a paragraph, a blockquote — ends the
    /// table, and every row after it renders as literal pipe text.
    /// </summary>
    /// <remarks>
    /// <b>This is the defect that made the check worth writing, and it is invisible to a reader of the
    /// source.</b> On 2026-08-12 the board's <em>Do these next</em> table — the file's whole reason for
    /// existing, and the first thing every cold start reads — was found split by <b>seven</b> blank
    /// lines, so everything from row 2 onward had been rendering as raw pipes for an unknown length of
    /// time. <c>plans/0002</c>'s coverage map was in <b>three</b> fragments, two of them orphaned by an
    /// interleaved blockquote. Nobody had noticed, because in a plain-text read the rows look perfect.
    /// <para>
    /// <b>The rule is stated as <em>every run of table rows begins with a header and a separator</em>
    /// rather than as <em>no blank line inside a table</em></b>, because the second phrasing cannot see
    /// the blockquote case — the fragment after it is not adjacent to a blank line at all. Stating the
    /// invariant instead of the symptom catches both, and it still permits two genuinely separate
    /// tables in succession, since the second one carries its own header.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_table_is_split_into_fragments_that_will_not_render()
    {
        var broken = new List<string>();

        foreach (string path in CorpusFiles(RepoRoot()))
        {
            string[] lines = WithoutFences(File.ReadAllLines(path));
            int index = 0;

            while (index < lines.Length)
            {
                if (!lines[index].StartsWith('|'))
                {
                    index++;
                    continue;
                }

                int start = index;

                while (index < lines.Length && lines[index].StartsWith('|'))
                {
                    index++;
                }

                bool headed = index - start >= 2 && Separator.IsMatch(lines[start + 1]);

                if (!headed)
                {
                    broken.Add(
                        $"{Path.GetRelativePath(RepoRoot(), path)}:{start + 1} — "
                        + $"{index - start} row(s) with no header separator");
                }
            }
        }

        Assert.True(
            broken.Count == 0,
            "these runs of table rows will not render as a table, because a markdown table must be "
            + "contiguous and its second line must be the |---|---| separator. Everything listed "
            + "renders as literal pipe text:\n  " + string.Join("\n  ", broken)
            + "\n\nUsually the cause is a blank line between two rows, or a blockquote written "
            + "between them. Close the gap, or move the interrupting block below the table.");
    }

    /// <summary>Emphasis outside code spans, requiring a boundary so snake_case cannot match.</summary>
    private static readonly Regex UnderscoreEmphasis = new(
        @"(?<![\w`\\])_(?![\s_])([^_`\n]{1,200}?)(?<![\s\\])_(?![\w`])",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    private static readonly Regex CodeSpan =
        new(@"`[^`\n]*`", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <summary>
    /// <b>Emphasis is <c>*asterisk*</c>, and the rule was checked against the corpus before it was
    /// imposed on it.</b> Across 118 files there were <b>four</b> exceptions, all of them
    /// <c>_Avoid_:</c> lines in <c>CONTEXT.md</c>, and they were normalised rather than exempted.
    /// </summary>
    /// <remarks>
    /// <b>The point is not consistency for its own sake — it is that a formatter will otherwise
    /// rewrite the corpus and nobody will see it in the diff.</b> A global <c>formatOnSave</c> sent
    /// the board through Prettier on 2026-08-12 and converted ~150 lines of emphasis, buried in the
    /// same diff as a deliberate 999-line deletion. That is the shape to catch: churn that is
    /// invisible because it is riding along with real work.
    /// <para>
    /// The pattern strips code spans first and demands a non-word boundary either side, so
    /// <c>lots_per_segment</c>, <c>World._tables</c> and <c>revisit_ticks</c> — of which this corpus
    /// has thousands — cannot match. It is deliberately blind to underscores inside fenced blocks and
    /// links.
    /// </para>
    /// </remarks>
    [Fact]
    public void Emphasis_is_written_with_asterisks()
    {
        var offenders = new List<string>();
        string root = RepoRoot();

        foreach (string path in CorpusFiles(root))
        {
            string[] lines = WithoutFences(File.ReadAllLines(path));

            for (int i = 0; i < lines.Length; i++)
            {
                Match match = UnderscoreEmphasis.Match(CodeSpan.Replace(lines[i], string.Empty));

                if (match.Success)
                {
                    offenders.Add(
                        $"{Path.GetRelativePath(root, path)}:{i + 1} — _{match.Groups[1].Value}_");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "the corpus writes emphasis as *emphasis*; these use _underscores_:\n  "
            + string.Join("\n  ", offenders)
            + "\n\nIf a formatter did this, it is Prettier — it rewrites *x* to _x_ with no option "
            + "to prevent it, which is why .prettierignore refuses it *.md and .vscode/settings.json "
            + "turns formatOnSave off for markdown. Revert the reformat rather than accepting it: it "
            + "also pads table cells, and on the board that is +100 KB of trailing spaces.");
    }
}
