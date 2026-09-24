using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

public sealed class AppearanceDumpTests
{
    private static readonly string Preset = Path.Combine(AppContext.BaseDirectory, "Appearance", "test-street");

    private static (int Code, string Report) Run(params string[] arguments)
    {
        Assert.True(Options.TryParse(arguments, out Options? options, out string? complaint), complaint);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        return (AppearanceDump.Run(options!, output), output.ToString());
    }

    [Fact]
    public void Every_Building_is_counted_once_by_kind_and_family()
    {
        (int code, string report) = Run("--ruleset", Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"),
            "--citizens", "400", "--ticks", "1", "--appearance", Preset);

        Assert.Equal(0, code);
        string[] lines = report.Split('\n');
        int header = int.Parse(lines[1].Split(' ')[^5], CultureInfo.InvariantCulture);
        int families = lines
            .SkipWhile(l => l != "## Buildings by kind and family").Skip(2)
            .TakeWhile(l => l.Length > 0)
            .Sum(l => int.Parse(l.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1], CultureInfo.InvariantCulture));

        Assert.True(header > 0);
        Assert.Equal(header, families);
        Assert.Contains("  dwelling              w1-workplace", report, StringComparison.Ordinal);
    }

    [Fact]
    public void The_mode_needs_one_ruleset()
    {
        Assert.False(Options.TryParse(["--appearance", Preset], out _, out string? complaint));
        Assert.Contains("--appearance needs exactly one --ruleset", complaint, StringComparison.Ordinal);
    }
}
