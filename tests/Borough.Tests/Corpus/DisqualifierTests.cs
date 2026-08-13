using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// <c>plans/0012</c>'s mechanical check 6, for <b>Cause 5</b> — <em>a caveat attached to a number does
/// not travel with it</em>.
/// </summary>
/// <remarks>
/// <b>A figure is written down correctly, with a clause saying what it measures and what it must not be
/// used for. Somebody later needs a number of that shape, finds it, and copies the digits.</b> The
/// clause stays where it was, still correct, doing nothing — and because the two documents agree, no
/// comparison between them can see anything wrong. This is the only failure in the corpus whose copies
/// <em>agree</em>, which is why none of checks 1–5 can reach it.
/// <para>
/// <b>The registry is the table in <c>plans/0012</c>'s Cause 5 section, and it is read rather than
/// mirrored.</b> Hard-coding it here would be <c>plans/0012</c> Cause 1 arriving inside the instrument,
/// which is the objection check 5's own design note raises. Two assertions follow from that: the
/// <em>owner</em> document must contain both the figure and the phrase, so the table cannot drift from
/// the caveat it points at; and every other prose document containing the figure must contain the
/// phrase too.
/// </para>
/// <para>
/// <b>Pinning the particular phrase is the design, and the version specified first did not.</b> That one
/// flagged any distinctive figure appearing in more than one document, and three measurements refuted it
/// on 2026-08-13. The number had travelled <em>rounded</em> — <c>186,624</c> became <c>~186,600</c> — so
/// an exact match would never have fired on its own motivating case. Normalising to three significant
/// figures catches it and also merges <c>1017</c>, <c>1021</c> and <c>1024</c>, across 107 groups of
/// unrelated measurements. And the caveat was not dropped but <b>substituted</b>: the quoting document
/// carried a real, correct, different caveat, so every <em>generic</em> detector passes it. Only naming
/// the clause that had to survive distinguishes the two.
/// </para>
/// </remarks>
public sealed class DisqualifierTests
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

    private const string RegistryMarker = "<!-- disqualifier-registry -->";

    /// <summary>
    /// A backticked token inside a table cell. Cells are read as a <em>list</em> of these rather than
    /// split on a separator, because the values are figures and every plausible separator — the comma
    /// above all — occurs inside them. The first version of this parser split the alternates cell on
    /// commas, turned <c>186,600</c> into <c>186</c> and <c>600</c>, and reported four documents that
    /// were clean. <b>The instrument for Cause 5 was defeated by digit grouping on its first run.</b>
    /// </summary>
    private static readonly Regex Cell =
        new(@"`(?<value>[^`]+)`", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private sealed record Entry(string Figure, string[] Spellings, string Phrase, string Owner);

    /// <summary>
    /// Reads the registry out of <c>plans/0012</c>, starting at the marker comment so that prose above
    /// it containing a pipe table cannot be mistaken for the list.
    /// </summary>
    private static List<Entry> Registry(string root)
    {
        string[] lines = File.ReadAllLines(Path.Combine(root, "plans", "0012-corpus-audit.md"));
        int start = Array.FindIndex(lines, line => line.Contains(RegistryMarker, StringComparison.Ordinal));

        Assert.True(
            start >= 0,
            $"plans/0012 has no `{RegistryMarker}` marker, so check 6 has no registry to read. The "
            + "marker sits immediately above the table in the *Cause 5* section. Without it this test "
            + "silently checks nothing, which is the shape of defect the corpus keeps finding.");

        var entries = new List<Entry>();

        foreach (string line in lines.Skip(start))
        {
            if (!line.StartsWith('|'))
            {
                // The table ends at the first line after it that is not part of one. Before the table
                // starts there are blank lines and a heading, so only break once a row has been read.
                if (entries.Count > 0)
                {
                    break;
                }

                continue;
            }

            string[] cells = line.Split('|', StringSplitOptions.TrimEntries);

            // A row is `| figure | alternates | phrase | owner |`, which splits to six with empty
            // outer cells. The header and its `|---|` separator carry no backticks and fall out here.
            if (cells.Length < 6)
            {
                continue;
            }

            string[] figures = [.. Cell.Matches(cells[1]).Select(match => match.Groups["value"].Value)];
            string[] alternates = [.. Cell.Matches(cells[2]).Select(match => match.Groups["value"].Value)];
            string[] phrase = [.. Cell.Matches(cells[3]).Select(match => match.Groups["value"].Value)];
            string[] owner = [.. Cell.Matches(cells[4]).Select(match => match.Groups["value"].Value)];

            if (figures.Length != 1 || phrase.Length != 1 || owner.Length != 1)
            {
                continue;
            }

            entries.Add(new Entry(figures[0], alternates, phrase[0], owner[0]));
        }

        Assert.NotEmpty(entries);

        return entries;
    }

    /// <summary>
    /// The corpus's prose. <c>spikes/</c> is excluded because its <c>results/</c> files are machine
    /// captures rather than arguments — a capture legitimately holds a raw figure, and demanding a
    /// caveat inside generated output would be asking a benchmark to editorialise.
    /// </summary>
    private static IEnumerable<string> ProseFiles(string root)
    {
        foreach (string directory in new[] { "docs", "plans" })
        {
            foreach (string path in Directory.EnumerateFiles(
                Path.Combine(root, directory), "*.md", SearchOption.AllDirectories))
            {
                yield return path;
            }
        }

        foreach (string name in new[] { "CONTEXT.md", "PROCESS.md", "CLAUDE.md" })
        {
            string path = Path.Combine(root, name);

            if (File.Exists(path))
            {
                yield return path;
            }
        }
    }

    private static bool Mentions(string text, Entry entry) =>
        entry.Spellings.Prepend(entry.Figure)
            .Any(spelling => text.Contains(spelling, StringComparison.Ordinal));

    /// <summary>
    /// The registry cannot point at a caveat that is not there. This is what stops the table becoming a
    /// second copy that drifts: reword the owner's clause and this fails, rather than the row quietly
    /// ceasing to describe anything.
    /// </summary>
    [Fact]
    public void Every_registered_figure_is_disqualified_in_its_own_owner()
    {
        string root = RepoRoot();
        var broken = new List<string>();

        foreach (Entry entry in Registry(root))
        {
            string path = Path.Combine(root, entry.Owner.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(path))
            {
                broken.Add($"{entry.Figure}: owner {entry.Owner} does not exist");
                continue;
            }

            string text = File.ReadAllText(path);

            if (!Mentions(text, entry))
            {
                broken.Add($"{entry.Figure}: owner {entry.Owner} does not contain the figure");
            }
            else if (!text.Contains(entry.Phrase, StringComparison.OrdinalIgnoreCase))
            {
                broken.Add($"{entry.Figure}: owner {entry.Owner} no longer says \"{entry.Phrase}\"");
            }
        }

        Assert.True(
            broken.Count == 0,
            "plans/0012's disqualifier registry has rows whose owner does not carry the caveat they "
            + $"claim: {string.Join("; ", broken)}. Either the owning document was reworded — in which "
            + "case update the row, and check that everything quoting the figure still says the right "
            + "thing — or the row was written against the wrong file. A registry pointing at a caveat "
            + "that is not there is the second copy this check exists to prevent.");
    }

    /// <summary>
    /// Check 6 proper. A figure the corpus knows is a trap must never appear without the clause that
    /// disarms it.
    /// </summary>
    [Fact]
    public void No_registered_figure_is_quoted_without_its_disqualifier()
    {
        string root = RepoRoot();
        List<Entry> registry = Registry(root);
        var bare = new List<string>();

        foreach (string path in ProseFiles(root))
        {
            string text = File.ReadAllText(path);

            foreach (Entry entry in registry)
            {
                if (Mentions(text, entry) && !text.Contains(entry.Phrase, StringComparison.OrdinalIgnoreCase))
                {
                    bare.Add($"{Path.GetRelativePath(root, path).Replace('\\', '/')} quotes "
                        + $"{entry.Figure} without \"{entry.Phrase}\"");
                }
            }
        }

        Assert.True(
            bare.Count == 0,
            $"these documents quote a registered figure with no disqualifier: {string.Join("; ", bare)}."
            + " plans/0012 Cause 5: *a caveat attached to a number does not travel with it* — the figure "
            + "is correct, the clause that says what it must not be used for stayed behind, and because "
            + "the digits agree no other check here can see it. Add the phrase, or if the figure is "
            + "genuinely being used another way, say so explicitly rather than removing the row. Do not "
            + "silence this by rounding the number: adr/0094 quoted 186,624 as ~186,600 and that is "
            + "precisely how the caveat was lost.");
    }
}
