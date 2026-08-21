namespace Borough.Tests.Corpus;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// No member carries two doc comments, because the second one belongs to somebody else.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two <c>///</c> blocks with no member between them both attach to the one member that
/// follows.</b> The compiler says nothing — a duplicate <c>&lt;summary&gt;</c> is not a warning — so
/// the member above the pair silently loses its documentation to its neighbour, and the neighbour
/// starts claiming a description of something it is not. There is no third outcome: a doc block
/// either documents the declaration under it or it documents the wrong one.
/// </para>
/// <para>
/// <b>It is a defect of the same shape as <c>plans/0012</c>'s <b>Cause 1</b>, arriving in a
/// doc-comment.</b> What goes wrong is not that a sentence is false when written; it is that a
/// sentence written about one symbol ends up filed under another, which is
/// <c>adr/0093</c>'s <i>a description of the build is where to look</i> pointing at the wrong place.
/// A reader who opens the symbol the corpus told them to open finds prose about a different one.
/// </para>
/// <para>
/// <b>It exists because the failure was found by hand and was systemic.</b> Milestone 11 task 1
/// inserted two members between an existing doc block and its member, and the review that caught it
/// was a human reading the diff. The sweep that followed found <b>forty</b> sites across
/// <b>thirty-one</b> files — eight where a rewritten block was left stacked on the old one, and
/// thirty-two where a member had genuinely lost its documentation to a neighbour. ***A defect
/// nothing can see is found once per reviewer, for ever.***
/// </para>
/// <para>
/// ⚠ <b>It is code-against-code and reads the tree from disk</b>, which is
/// <see cref="RefusalCountTests"/>' shape. Every other mechanical check in this directory reads one
/// document against another, and this class of defect is invisible to all of them because it lives
/// in a doc-comment, which no document-to-document check has ever been able to see.
/// </para>
/// </remarks>
public sealed class DocCommentAttachmentTests
{
    /// <summary>The two trees this governs: what ships, and the suite that holds it.</summary>
    /// <remarks>
    /// <b><c>spikes/</c> is deliberately outside.</b> A spike is a throwaway harness that has already
    /// produced its number and is kept as evidence rather than maintained (<c>PROCESS.md</c>), so
    /// holding it to a documentation rule would be maintaining it.
    /// </remarks>
    private static readonly string[] Trees = ["src", "tests"];

    /// <summary>A doc-comment line, at any indent.</summary>
    private static readonly Regex Doc = new(@"^\s*///", RegexOptions.Compiled);

    [Fact]
    public void No_member_carries_two_doc_comments()
    {
        var wrong = new StringBuilder();
        int sites = 0;

        foreach (string file in SourceFiles())
        {
            string[] lines = File.ReadAllLines(file);
            int start = -1;
            int summaries = 0;

            for (int line = 0; line <= lines.Length; line++)
            {
                bool documenting = line < lines.Length && Doc.IsMatch(lines[line]);

                if (documenting)
                {
                    if (start < 0)
                    {
                        start = line;
                        summaries = 0;
                    }

                    summaries += Occurrences(lines[line]);

                    continue;
                }

                if (start >= 0 && summaries > 1)
                {
                    sites++;
                    wrong.Append(CultureInfo.InvariantCulture, $"  {Relative(file)}:{start + 1} — ")
                        .Append(CultureInfo.InvariantCulture, $"{summaries} summaries, landing on ")
                        .AppendLine(line < lines.Length ? lines[line].Trim() : "end of file");
                }

                start = -1;
            }
        }

        Assert.True(
            sites == 0,
            $"{sites} doc comment(s) attach to a member they do not describe:\n{wrong}\n"
            + "Two /// blocks with nothing between them both bind to the member below, so the one "
            + "above has lost its documentation and the one below is claiming somebody else's. "
            + "Move the upper block to the member it describes, or delete it where the block below "
            + "supersedes it. Do not merge them — that writes a third description nobody wrote.");
    }

    /// <summary>
    /// Every doc comment closes the tags it opens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same defect one level down, and it was found by the sweep the test above paid for.</b>
    /// A <c>&lt;/remarks&gt;</c> typed where a <c>&lt;/para&gt;</c> belonged closes the block early
    /// and strands every paragraph after it <em>outside</em> the remarks; a <c>&lt;para&gt;</c> opened
    /// twice does the same to the paragraph structure. Six sites had it, and the compiler is silent
    /// about all of them.
    /// </para>
    /// <para>
    /// ⚠ <b>What it costs is only visible where the docs are rendered</b>, which is an IDE tooltip
    /// and a generated reference — neither of which anybody in this project reads, which is exactly
    /// why it accumulated. ***A malformation nobody's tooling surfaces is a malformation nobody
    /// fixes.***
    /// </para>
    /// <para>
    /// <b>Counted per block rather than per file</b>, because a stray closer in one member's comment
    /// would otherwise be cancelled out by a stray opener in another's and the file would balance
    /// while both were wrong.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_doc_comment_closes_what_it_opens()
    {
        var wrong = new StringBuilder();
        int blocks = 0;

        foreach (string file in SourceFiles())
        {
            string[] lines = File.ReadAllLines(file);
            int start = -1;
            var text = new StringBuilder();

            for (int line = 0; line <= lines.Length; line++)
            {
                if (line < lines.Length && Doc.IsMatch(lines[line]))
                {
                    if (start < 0)
                    {
                        start = line;
                        text.Clear();
                    }

                    text.AppendLine(lines[line]);

                    continue;
                }

                if (start >= 0)
                {
                    string block = text.ToString();

                    foreach (string tag in Paired)
                    {
                        int opened = Count(block, $"<{tag}>");
                        int closed = Count(block, $"</{tag}>");

                        if (opened == closed)
                        {
                            continue;
                        }

                        blocks++;
                        wrong.Append(CultureInfo.InvariantCulture, $"  {Relative(file)}:{start + 1} — ")
                            .AppendLine(CultureInfo.InvariantCulture,
                                $"{opened} <{tag}> against {closed} </{tag}>");
                    }
                }

                start = -1;
            }
        }

        Assert.True(
            blocks == 0,
            $"{blocks} doc comment tag(s) do not balance:\n{wrong}\n"
            + "A </remarks> typed where a </para> belonged closes the block early and strands every "
            + "paragraph after it outside the remarks. Nothing in the build says so, because a doc "
            + "comment is not parsed unless documentation is being generated.");
    }

    /// <summary>The block tags a doc comment opens and must close.</summary>
    private static readonly string[] Paired = ["summary", "remarks", "para"];

    /// <summary>How many times one string occurs in another.</summary>
    private static int Count(string haystack, string needle)
    {
        int found = 0;

        for (int at = haystack.IndexOf(needle, StringComparison.Ordinal);
            at >= 0;
            at = haystack.IndexOf(needle, at + 1, StringComparison.Ordinal))
        {
            found++;
        }

        return found;
    }

    /// <summary>How many summaries one line opens.</summary>
    private static int Occurrences(string line) => Count(line, "<summary>");

    private static IEnumerable<string> SourceFiles()
    {
        string root = RepoRoot();

        foreach (string tree in Trees)
        {
            string path = Path.Combine(root, tree);

            Assert.True(Directory.Exists(path), $"{path} is not there.");

            foreach (string file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                // Generated output, not source. It is a copy of what is above it by definition, so
                // holding it to a rule about authorship would be holding the compiler to it.
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                yield return file;
            }
        }
    }

    private static string Relative(string file) =>
        Path.GetRelativePath(RepoRoot(), file).Replace(Path.DirectorySeparatorChar, '/');

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
            "no directory above the test assembly contains docs/adr. This test reads the tree from "
            + "disk, so it cannot run from a detached output directory.");
    }
}
