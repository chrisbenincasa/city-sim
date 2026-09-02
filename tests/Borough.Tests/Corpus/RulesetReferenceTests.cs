using Borough.Headless;

namespace Borough.Tests.Corpus;

/// <summary>
/// The committed key reference is what the generator produces today, byte for byte.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>RulesetSchemaTests</c>' sibling over the other generated artefact</b>, and it exists for
/// the reason <c>plans/0050</c> established one level down: thirty Ruleset headers were a second
/// copy of the loader, 88% of that prose was duplicated, and ***nothing in the build could tell***.
/// A reference page describing a format is that failure's largest available instance, so it ships
/// generated and it ships checked.
/// </para>
/// <para>
/// ⚠ <b>It compares BYTES, where <c>RulesetSchemaTests</c> deliberately compares paths and
/// types.</b> That test avoids a byte comparison because a reformat of a JSON schema says nothing
/// about the city and would fail anyway. This one wants the opposite: the file is prose, the whole
/// artefact is generated, and there is no legitimate hand edit to protect — a byte difference means
/// either the generator moved, the notes moved, the loader moved, or somebody edited the page. All
/// four want the same response, which is to regenerate.
/// </para>
/// <para>
/// ⚠ <b>It calls the generator rather than re-deriving the page's shape.</b> A test that rebuilt
/// the layout in its own words would be a second copy of <c>KeyReferenceDump</c> checked against
/// <c>KeyReferenceDump</c>, which is the thing being guarded against, moved out one level. What
/// this asserts is only that the committed bytes are the current output.
/// </para>
/// </remarks>
public sealed class RulesetReferenceTests
{
    [Fact]
    public void The_committed_reference_is_what_the_generator_writes_today()
    {
        string root = RepoRoot();
        string path = Path.Combine(root, "docs", "ruleset-reference.md");

        Assert.True(
            File.Exists(path),
            $"{path} is not there. Generate it with:\n{Command}");

        string committed = File.ReadAllText(path);
        string fresh = KeyReferenceDump.Page(Path.Combine(root, "rulesets"));

        if (committed == fresh)
        {
            return;
        }

        Assert.Fail(
            "docs/ruleset-reference.md is not what --key-reference writes today. The loader, the "
            + "notes or the generator moved and the page did not -- or the page was edited by "
            + "hand, which it says at the top not to do.\n\n"
            + Difference(committed, fresh)
            + "\nRegenerate rather than editing it:\n" + Command);
    }

    /// <summary>
    /// The first line the two differ on, with its neighbours, rather than the whole file.
    /// </summary>
    /// <remarks>
    /// <b>A 900-line diff in an assertion message is a message nobody reads</b>, and the failure is
    /// almost always one key. Naming the line and the two sides of it is enough to say which of the
    /// four causes it was, which is all this message owes the reader before they regenerate.
    /// </remarks>
    private static string Difference(string committed, string fresh)
    {
        string[] was = committed.Split('\n');
        string[] now = fresh.Split('\n');

        for (int line = 0; line < Math.Min(was.Length, now.Length); line++)
        {
            if (was[line] == now[line])
            {
                continue;
            }

            return $"  first difference at line {line + 1}:\n"
                + $"    committed: {Clip(was[line])}\n"
                + $"    generated: {Clip(now[line])}\n";
        }

        return $"  the files agree for {Math.Min(was.Length, now.Length)} lines and then one ends: "
            + $"committed is {was.Length} lines, generated is {now.Length}.\n";
    }

    private static string Clip(string line) =>
        line.Length <= 160 ? line : line[..157] + "...";

    private const string Command =
        "  dotnet run --project src/Borough.Headless -- \\\n"
        + "    --key-reference --ruleset rulesets/minimal.toml > docs/ruleset-reference.md";

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLAUDE.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("no repository root above the test binary.");
    }
}
