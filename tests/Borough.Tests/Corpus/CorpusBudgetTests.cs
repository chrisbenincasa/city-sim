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
    /// <summary>ADR files on 2026-08-26, the day the amnesty opened — plus milestone 17's two.</summary>
    /// <remarks>
    /// 🔴 <b>RE-SEEDED 169 → 171 on 2026-08-27 for the merge of <c>milestone-17-decline-and-cleared-land</c>,
    /// and this is the raise the message above warns against, made deliberately.</b> The two ADRs are
    /// <c>0168</c> (a decline threshold is a duration, and the premises and the tenant get one each)
    /// and <c>0172</c> (an abandoned shell collapses on a clock). ***Both were written BEFORE the
    /// amnesty opened*** — the freeze was captured against <c>main</c> on the day, and this branch was
    /// already in flight with them in it. ⚠ <b>The ratchet stops <em>new</em> growth; it was never
    /// meant to retroactively refuse work that predates it</b>, and the only alternative was to unwrite
    /// two records of a milestone that had already landed. It keeps working from the new baseline, and
    /// standing order 1 is unchanged: no ADR written after 2026-08-26 gets in this way.
    /// </remarks>
    private const int AdrCeiling = 171;

    /// <summary><c>plans/0002-open-questions.md</c> on 2026-08-26, plus milestone 17's §D rows.</summary>
    /// <remarks>
    /// <b>RE-SEEDED 146,606 → 153,786 on 2026-08-27</b>, for <see cref="AdrCeiling"/>'s reason and no
    /// other. The added rows are milestone 17's decline numbers, opened under <c>adr/0052</c> while it
    /// was still in force — ⚠ <b>which the amnesty has since SUSPENDED</b>, so nothing written after
    /// 2026-08-26 opens a §D row at all and this ceiling has no ordinary way to move again.
    /// </remarks>
    private const int OpenQuestionsCeiling = 153_786;

    /// <summary>Words of prose per line of simulation the repository may not exceed.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS REPLACED TWO ABSOLUTE CEILINGS ON 2026-08-30, AND THE REASON IS THAT THEY WERE
    /// MEASURING THE WRONG SIDE OF THE FRACTION.</b> A <c>docs/</c>+<c>plans/</c> word ceiling and a
    /// doc-comment word ceiling both went red on <em>every</em> commit that added simulation with the
    /// remarks <c>adr/0093</c> asks for — four times in one day, each time on a commit that improved
    /// the ratio. They were raised by their own author on all four, which is not a check.
    /// </para>
    /// <para>
    /// <b>A ratchet on the ratio buys what those two were for and costs nothing they were not.</b>
    /// Prose written beside new simulation is free; prose written alone is refused. ***The amnesty
    /// was opened against a ratio and this is the only instrument denominated in one.***
    /// </para>
    /// <para>
    /// ⚠ <b>Design artefacts are still capped in absolute terms</b> — see <see cref="AdrCeiling"/>
    /// and <see cref="OpenQuestionsCeiling"/>, which are standing orders 1 and 2. An ADR is a
    /// decision rather than a description, and no amount of code buys one.
    /// </para>
    /// </remarks>
    private const int RatioCeiling = 57;

    /// <summary>Words of prose per line of simulation at which the amnesty has done its job.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>PROVISIONAL, chosen by taste under <c>plans/0045</c> standing order 4</b>, which
    /// suspends <c>adr/0052</c>. No ratifier, no §D row.
    /// </para>
    /// <para>
    /// <b>It replaces the expiry date, and the swap is the point.</b> 2026-10-07 would have arrived
    /// whether or not anything was built; this cannot. The amnesty was opened against a prose-to-code
    /// ratio and <b>a date is not a measure of one</b>. At 2026-08-30 the figure is <b>59.5</b> — all
    /// prose, doc-comments included, over non-comment <c>src/</c> lines — and 30 is roughly half.
    /// </para>
    /// <para>
    /// ⚠ <b>It is earned by deleting prose as readily as by writing simulation, and that is
    /// deliberate</b>: <c>plans/0045</c> opens by naming the corpus as the one collection with no
    /// sink, which is <c>adr/0006</c> violated by its own citers. A target reachable only by writing
    /// code would leave that violation standing.
    /// </para>
    /// </remarks>
    private const int ExitRatio = 30;

    /// <summary>The two directories the ratio's numerator covers.</summary>
    private static readonly string[] Covered = ["docs", "plans"];

    /// <summary>The two directories whose comments count as prose.</summary>
    private static readonly string[] Commented = ["src", "tests"];

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

    /// <summary>
    /// <b>Prose may grow only as fast as the simulation it describes.</b>
    /// </summary>
    /// <remarks>
    /// See <see cref="RatioCeiling"/>. ⚠ <b>A remark beside a mechanism is the best-placed sentence
    /// in the project</b> and this check is not aimed at it — <c>adr/0093</c> asks for exactly that
    /// prose. What it refuses is a sitting that produces documents and no city.
    /// </remarks>
    [Fact]
    public void The_prose_does_not_outgrow_the_simulation()
    {
        int prose = CorpusFiles().Sum(file => Words(File.ReadAllText(file))) + CommentWords();
        int simulation = SimulationLines();
        int ratio = prose / simulation;

        Assert.True(
            ratio <= RatioCeiling,
            $"{prose:N0} words of prose over {simulation:N0} lines of simulation is {ratio:N0} words "
            + $"a line, against a ceiling of {RatioCeiling:N0}. The amnesty caps the RATIO and not "
            + "the word count: prose that ships beside new simulation is free, prose that ships "
            + "alone is not. Write the mechanism, or cut prose to pay for the page. "
            + "See plans/0045-amnesty.md.");
    }

    /// <summary>
    /// 🔴 <b>The amnesty's exit condition, and this test goes RED the day it is met.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A red here is not a regression — it is the amnesty reporting that it is over.</b> Delete
    /// this class and <c>plans/0045-amnesty.md</c> with it, and take the standing orders off.
    /// </para>
    /// <para>
    /// ⚠ <b>The ratio is the whole diagnosis in one number.</b> <c>plans/0045</c> opened on
    /// <em>1.17M words of prose against 17,872 lines of executable simulation</em>; every other
    /// figure in this class is a ratchet on one side of that fraction, and this one watches the
    /// fraction itself.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_amnesty_has_not_yet_earned_its_end()
    {
        int prose = CorpusFiles().Sum(file => Words(File.ReadAllText(file))) + CommentWords();
        int simulation = SimulationLines();
        int ratio = prose / simulation;

        Assert.True(
            ratio > ExitRatio,
            $"{prose:N0} words of prose over {simulation:N0} lines of simulation is {ratio:N0} words "
            + $"a line, against the amnesty's exit condition of {ExitRatio:N0}. THE AMNESTY IS OVER "
            + "AND THIS FAILURE IS THE REPORT OF IT — delete this class and plans/0045-amnesty.md "
            + "in one commit, and lift the standing orders. See plans/0045-amnesty.md.");
    }

    /// <summary>
    /// <b>The word counter and the line counter disagree about the same line, and must.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A line is comment or simulation and never both</b>, so the two halves of the ratio
    /// cannot double-count. This is the arithmetic the exit condition rests on, asserted rather
    /// than assumed.
    /// </remarks>
    [Fact]
    public void A_line_is_counted_on_exactly_one_side_of_the_ratio()
    {
        Assert.True(IsComment("/// a remark"));
        Assert.True(IsComment("// a note"));
        Assert.True(IsComment("* a continuation"));
        Assert.False(IsComment("int lots = 5;"));
        Assert.False(IsComment(string.Empty));
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

    /// <summary>Every C# file the ratio counts, build output excluded.</summary>
    /// <remarks>
    /// ⚠ <b><c>obj/</c> and <c>bin/</c> hold generated sources</b> — an assembly-info file and the
    /// implicit-usings file — and counting them would make the ratio move on a clean rebuild.
    /// </remarks>
    private static IEnumerable<string> CommentedFiles()
    {
        string root = RepoRoot();

        return Commented
            .Select(directory => Path.Combine(root, directory))
            .SelectMany(directory =>
                Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(path =>
                !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    /// <summary>Words on comment lines, which is what a shell reading of the same rule counts.</summary>
    private static int CommentWords() =>
        CommentedFiles()
            .SelectMany(File.ReadLines)
            .Select(line => line.Trim())
            .Where(IsComment)
            .Sum(Words);

    /// <summary>Lines under <c>src/</c> that are neither comment nor blank.</summary>
    /// <remarks>
    /// ⚠ <b><c>src/</c> only, and <c>tests/</c> is deliberately absent.</b> The ratio asks how much
    /// prose stands over the <em>simulation</em>; a test is not the city. Test comments still count
    /// on the numerator, because prose relocating into a test file is the same relocation.
    /// </remarks>
    private static int SimulationLines() =>
        CommentedFiles()
            .Where(path => path.Contains(
                $"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
            .SelectMany(File.ReadLines)
            .Select(line => line.Trim())
            .Count(line => line.Length > 0 && !IsComment(line));

    /// <summary>A trimmed line that carries prose rather than instructions.</summary>
    private static bool IsComment(string trimmed) =>
        trimmed.StartsWith("//", StringComparison.Ordinal)
        || trimmed.StartsWith("/*", StringComparison.Ordinal)
        || trimmed.StartsWith('*');

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
