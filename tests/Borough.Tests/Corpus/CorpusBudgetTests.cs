namespace Borough.Tests.Corpus;

/// <summary>
/// The corpus has a sink. <b>Three ceilings, frozen at their 2026-08-26 sizes.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0006</c> — <em>no collection grows with elapsed time</em> — is the rule this project
/// repeats most, and the corpus was the one collection nothing applied it to: 1.17M words of prose
/// against 17,872 lines of executable simulation, 169 ADRs, and 236 of 524 commits changing no code
/// at all. <b>Prose was the only artefact here that could grow without anything going red.</b>
/// </para>
/// <para>
/// <b>These are ratchets, not judgements.</b> Nothing here says a document is wrong or too long. The
/// ceiling is simply today's size, so growth becomes a deliberate act instead of a default one — and
/// the escape hatch is to delete the test in its own commit saying why. That is intended to be
/// visible rather than hard. See <c>plans/0045-amnesty.md</c>, which expires 2026-10-07; delete this
/// class with it.
/// </para>
/// <para>
/// ⚠ <b>Words, and the definition is whitespace-separated runs</b> — what <c>wc -w</c> counts, so a
/// reading taken at a shell and a reading taken here agree. A cap on bytes would move when somebody
/// fixed an encoding; a cap on lines would move when somebody rewrapped a paragraph.
/// </para>
/// </remarks>
public sealed class CorpusBudgetTests
{
    /// <summary>ADR files on 2026-08-26, the day the amnesty opened.</summary>
    private const int AdrCeiling = 169;

    /// <summary><c>plans/0002-open-questions.md</c> on 2026-08-26.</summary>
    private const int OpenQuestionsCeiling = 146_606;

    /// <summary>Every markdown file under <c>docs/</c> and <c>plans/</c> on 2026-08-26.</summary>
    /// <remarks>
    /// ⚠ <b>RE-SEEDED 2026-08-27, and the reason is worth carrying.</b> The figure first written here
    /// was <b>1,171,820</b>, taken at a shell. It became unreproducible within the day: the tree's
    /// markdown was byte-identical to the commit that set it, and both a shell reading and this test
    /// then agreed on <b>1,172,148</b>. The likeliest cause is another session holding an uncommitted
    /// edit at the moment the first reading was taken. ***A ratchet is only as good as the instant its
    /// baseline was captured***, and a baseline captured over somebody else's working tree is not the
    /// repository's. Re-seeded from this test's own count against a clean tree.
    /// </remarks>
    private const int CorpusCeiling = 1_172_148;

    /// <summary>The two directories the corpus ceiling covers.</summary>
    private static readonly string[] Covered = ["docs", "plans"];

    [Fact]
    public void The_ADR_corpus_does_not_grow()
    {
        int found = Directory.GetFiles(Path.Combine(RepoRoot(), "docs", "adr"), "*.md").Length;

        Assert.True(found <= AdrCeiling, Explain("docs/adr/", "ADRs", found, AdrCeiling));
    }

    [Fact]
    public void The_open_questions_do_not_grow()
    {
        int found = Words(File.ReadAllText(
            Path.Combine(RepoRoot(), "plans", "0002-open-questions.md")));

        Assert.True(
            found <= OpenQuestionsCeiling,
            Explain("plans/0002-open-questions.md", "words", found, OpenQuestionsCeiling));
    }

    [Fact]
    public void The_corpus_does_not_grow()
    {
        int found = CorpusFiles().Sum(file => Words(File.ReadAllText(file)));

        Assert.True(
            found <= CorpusCeiling, Explain("docs/ + plans/", "words", found, CorpusCeiling));
    }

    /// <summary>
    /// <b>The diagnostic fires, and its message names the amnesty rather than the ceiling.</b>
    /// </summary>
    /// <remarks>
    /// <c>CLAUDE.md</c>'s rule that every diagnostic ships with a test that writes the violation and
    /// watches it fire. ⚠ <b>The message is the point</b>: a reader who hits this without being told
    /// where the rule came from will read it as an arbitrary limit and raise the constant, which is
    /// the one response that makes it useless.
    /// </remarks>
    [Fact]
    public void The_diagnostic_names_the_amnesty_and_the_way_out()
    {
        string message = Explain("docs/adr/", "ADRs", 170, 169);

        Assert.Contains("170", message, StringComparison.Ordinal);
        Assert.Contains("169", message, StringComparison.Ordinal);
        Assert.Contains("plans/0045-amnesty.md", message, StringComparison.Ordinal);
        Assert.Contains("delete this test", message, StringComparison.Ordinal);
    }

    private static string Explain(string what, string unit, int found, int ceiling) =>
        $"{what} holds {found:N0} {unit} against a ceiling of {ceiling:N0}. The corpus is frozen at "
        + "its 2026-08-26 size while the amnesty runs — see plans/0045-amnesty.md. Raising this "
        + "constant defeats the check; if the growth is genuinely wanted, delete this test in its "
        + "own commit saying why.";

    /// <summary>Every markdown file the ceiling covers, build output and worktrees excluded.</summary>
    private static IEnumerable<string> CorpusFiles()
    {
        string root = RepoRoot();

        return Covered
            .Select(directory => Path.Combine(root, directory))
            .SelectMany(directory =>
                Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    /// <summary>Whitespace-separated runs, which is what <c>wc -w</c> counts.</summary>
    private static int Words(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

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
}
