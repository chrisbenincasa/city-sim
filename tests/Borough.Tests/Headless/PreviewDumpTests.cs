using Borough.Formats;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// <c>--against</c> — what replacing one Ruleset with another would do, reported before anything
/// runs.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shipped <c>stocked.toml</c> is the fixture, because it is the demonstration the claim is
/// about.</b> Its header says a basket of 250 gives the larder a capacity of 750 and that moving the
/// basket to 300 moves the capacity to 900. A preview that cannot report that edit is not reporting
/// effective values, and the file's own sentence is the assertion.
/// </para>
/// <para>
/// ⚠ <b>The mode is world-free, so nothing here builds a city.</b> A comparison of two Rulesets
/// makes no claim about a running one, and the page says so in its own last line.
/// </para>
/// </remarks>
public sealed class PreviewDumpTests
{
    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static string Page(string before, string after) =>
        PreviewDump.Render(
            RulesetImpact.Between(RulesetSource.Load(before), RulesetSource.Load(after)),
            before,
            after);

    private static string Edited(string name, string from, string to)
    {
        string path = Path.Combine(Path.GetTempPath(), $"borough-preview-{Guid.NewGuid():N}.toml");

        File.WriteAllText(
            path,
            File.ReadAllText(Ruleset(name)).Replace(from, to, StringComparison.Ordinal));

        return path;
    }

    [Fact]
    public void A_shared_basket_edit_reports_the_capacity_it_derives()
    {
        string after = Edited("stocked.toml", "sundries = 250 }", "sundries = 300 }");

        try
        {
            string page = Page(Ruleset("stocked.toml"), after);

            Assert.Contains("[[rule]] consume  changed", page, StringComparison.Ordinal);
            Assert.Contains("250 -> 300", page, StringComparison.Ordinal);
            Assert.Contains("[[building]] dwelling  changed", page, StringComparison.Ordinal);
            Assert.Contains("750 -> 900", page, StringComparison.Ordinal);
            Assert.Contains("no migration was attempted", page, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(after);
        }
    }

    [Fact]
    public void An_unedited_Ruleset_moves_nothing()
    {
        string page = Page(Ruleset("stocked.toml"), Ruleset("stocked.toml"));

        Assert.Contains("Nothing moves.", page, StringComparison.Ordinal);
    }

    [Fact]
    public void A_preview_needs_exactly_one_ruleset_to_compare_against()
    {
        Assert.True(Options.TryParse(
            ["--ruleset", "a.toml", "--against", "b.toml"], out Options one, out _));

        Assert.Equal(Mode.Preview, one.Mode);
        Assert.Equal("b.toml", one.AgainstPath);

        Assert.False(Options.TryParse(
            ["--ruleset", "a.toml", "--ruleset", "c.toml", "--against", "b.toml"], out _,
            out string? complaint));

        Assert.Contains("exactly one --ruleset", complaint, StringComparison.Ordinal);

        Assert.False(Options.TryParse(["--against", "b.toml"], out _, out complaint));
        Assert.Contains("exactly one --ruleset", complaint, StringComparison.Ordinal);
    }
}
