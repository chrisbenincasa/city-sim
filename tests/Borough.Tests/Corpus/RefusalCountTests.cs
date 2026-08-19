using System.Text.RegularExpressions;

namespace Borough.Tests.Corpus;

/// <summary>
/// The Ruleset loader's refusal count of record is the loader's own, and this is the line that keeps
/// them together.
/// </summary>
/// <remarks>
/// <para>
/// <b>The corpus's first document-to-<em>code</em> check.</b> Every other mechanical check it has —
/// citations, the coverage map, link resolution, ledger citations, markdown style, the disqualifier
/// registry — reads one document against another, which is why a number describing the build could
/// drift indefinitely with every test green.
/// </para>
/// <para>
/// <b>It is here because that drift is measured rather than feared.</b>
/// <c>adr/0048</c> calls itself the count of record, and it has been corrected once already, on
/// 2026-08-11, from *eleven at load and a twelfth on reload* to *twenty-two and a twenty-third*. On
/// 2026-08-18 a recount put the load figure at **58** before this milestone added one — and
/// **17 of those 36 uncounted refusals sat in `[[rule]]`, `[[resource]]` and `[[building]]`, all of
/// which existed on the day of the correction**. So the number was an undercount of its own scope
/// the day it was last written down: ***a count corrected by adding what you remember adding is
/// still a count nobody has taken.***
/// </para>
/// <para>
/// <b>It counts sites, not semantic refusals, and the difference is deliberate.</b> Whether a guard
/// is a design refusal or plumbing is a judgement, and a judgement cannot be a test. A
/// <c>Refuse(</c> call site is a fact, and it is the fact that moves when somebody adds a guard —
/// so this fails on the day a refusal is written, which is the day the enumeration in
/// <c>adr/0048</c> can still be extended from memory.
/// </para>
/// </remarks>
public sealed class RefusalCountTests
{
    /// <summary>The sentence in <c>adr/0048</c> this test reads, as a pattern.</summary>
    private static readonly Regex Stated = new(
        @"refusal sites in `RulesetLoader\.cs`: \*\*(\d+)\*\*", RegexOptions.Compiled);

    /// <summary>
    /// <c>adr/0048</c> states the number of refusal sites the loader actually has.
    /// </summary>
    [Fact]
    public void The_count_of_record_is_the_loaders_own_count()
    {
        string root = RepoRoot();
        int sites = RefusalSites(root);
        string adr = Path.Combine(
            root,
            "docs",
            "adr",
            "0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-"
                + "into-the-core.md");

        Assert.True(File.Exists(adr), $"{adr} is not there. The count of record has moved file.");

        Match match = Stated.Match(File.ReadAllText(adr));

        Assert.True(
            match.Success,
            "adr/0048 no longer carries the sentence this test reads. It must contain, verbatim, "
                + $"'refusal sites in `RulesetLoader.cs`: **{sites}**' — that is the count of record, "
                + "and this is the only check that holds it to the build.");

        int stated = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(
            stated == sites,
            $"RulesetLoader.cs has {sites} Refuse( call sites and adr/0048 states {stated}. If you "
                + "added a refusal, put the new number there AND add it to that ADR's enumeration — "
                + "the number without the list is what drifted from 22 to 58 with nothing noticing.");
    }

    /// <summary>Every <c>Refuse(</c> in the loader that is a call rather than the declaration.</summary>
    private static int RefusalSites(string root)
    {
        string path = Path.Combine(root, "src", "Borough.Formats", "RulesetLoader.cs");

        Assert.True(File.Exists(path), $"{path} is not there. The loader has moved.");

        return File.ReadAllLines(path)
            .Where(line => line.Contains("Refuse(", StringComparison.Ordinal))
            .Count(line => !line.Contains("void Refuse(", StringComparison.Ordinal));
    }

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
