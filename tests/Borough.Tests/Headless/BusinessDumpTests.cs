using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// Milestone 27 task 10: <c>--business</c>, the twelfth runner mode.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is the refusal</b> — <see cref="A_world_with_no_economic_actor_is_refused"/>
/// — because <c>plans/0040</c> <b>F43</b> is what this mode was written against: milestone 25 went to
/// show its mechanism and ***the thing to look at did not exist in any shipped world***. A dump that
/// printed a table of zeroes on such a file would have reproduced that failure rather than caught it.
/// </para>
/// <para>
/// ⚠ <b>The refusal's test is whether a Business can be CREATED, not whether one is declared</b>, and
/// <c>rulesets/tenanted.toml</c> is the file that separates the two: it names two trades and
/// instantiates neither.
/// </para>
/// <para>
/// ⚠ <b>The run is short and the assertions are shapes rather than figures.</b> Every count in this
/// picture moves with the seed and with how far the run got; what is asserted is that each panel is
/// reached and that the two the milestone's risk turns on are non-zero.
/// </para>
/// </remarks>
public sealed class BusinessDumpTests
{
    private const string Ticks = "6144";

    private const string Population = "1000";

    /// <summary>Every panel is reached, which is what says the wiring runs at all.</summary>
    [Fact]
    public void The_picture_has_all_five_panels()
    {
        string report = Dump();

        Assert.Contains("How many there are, and where", report, StringComparison.Ordinal);
        Assert.Contains("The stock over time", report, StringComparison.Ordinal);
        Assert.Contains("What moved, over the whole run", report, StringComparison.Ordinal);
        Assert.Contains("What they hold", report, StringComparison.Ordinal);
        Assert.Contains("Who works in them", report, StringComparison.Ordinal);
        Assert.Contains("What read them", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The milestone's four claims are all visible in one world, which is what F43 asks.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Founding is the one that would silently read zero.</b> The Census carries no
    /// <c>Founded</c> counter, so the dump keeps the running total by hand across its own
    /// observations — and the first draft handed the Census the whole <c>Simulation</c>, which drains
    /// every engine, and printed <b>0 founded</b> in a world visibly full of founded shops.
    /// ***A flow that reads zero looks exactly like a mechanism that did not run.***
    /// </remarks>
    [Fact]
    public void The_four_quarters_of_the_milestones_risk_are_all_non_zero()
    {
        string report = Dump();

        Assert.True(Value(report, "live Businesses") > 0, "the city created no Business.");
        Assert.True(Value(report, "founded by a Household") > 0, "nobody founded one.");
        Assert.True(Value(report, "holding something") > 0, "nothing funded one.");
        Assert.True(Value(report, "Citizens employed by a Business") > 0, "nobody works in one.");
        Assert.Equal(1, Value(report, "Policies sweeping Businesses"));
    }

    /// <summary>
    /// ⚠ <b>A Ruleset in which no Business can be created is refused rather than dumped.</b>
    /// </summary>
    /// <remarks>
    /// <c>tenanted.toml</c> declares two trades and builds neither — nothing in it names a trade on a
    /// <c>[[building]]</c> kind and it states no <c>[founding]</c> — so every panel would be a row of
    /// zeroes, which reads as a broken dump rather than as a file that authors no economic actor. This
    /// is <c>--money</c>'s refusal on <c>--land-value</c>'s polarity.
    /// </remarks>
    [Fact]
    public void A_world_with_no_economic_actor_is_refused()
    {
        // ⚠ HAND-BUILT, AND NO SHIPPED FILE CAN STAND IN FOR IT. The first draft of this test used
        // rulesets/tenanted.toml on the reasoning that it declares two trades and builds neither --
        // and adr/0148 put `business = "shop"` on the dwelling kind of ALL SIXTEEN shipped files, so
        // every one of them instantiates. That is the milestone's own answer to plans/0040 F43 read
        // back: there is no shipped world without an economic actor in it any more.
        string text = File.ReadAllText(Ruleset("minimal.toml"))
            .Replace("business = \"shop\"", "", StringComparison.Ordinal);

        string path = Path.Combine(Path.GetTempPath(), $"borough-tradeless-{Guid.NewGuid():N}.toml");

        try
        {
            File.WriteAllText(path, text);

            (int code, string report) = Run(path);

            Assert.Equal(3, code);
            Assert.Contains(
                "nothing in a world on it ever creates a Business", report, StringComparison.Ordinal);
            Assert.Contains("levied.toml", report, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A world that instantiates but never founds is dumped, and its founding row is honestly zero.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The refusal is about whether a Business can EXIST, not about whether all four panels have
    /// something in them.</b> <c>minimal.toml</c>'s dwelling declares a trade, so the city creates one
    /// — the first quarter of the risk — and nothing in it founds, employs money or reads a balance.
    /// ***A picture of one quarter is worth printing; a picture of none is not.***
    /// </remarks>
    [Fact]
    public void A_world_that_instantiates_and_never_founds_still_prints()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"));

        Assert.Equal(0, code);
        Assert.True(Value(report, "live Businesses") > 0, "minimal.toml instantiated no trade.");
        Assert.Equal(0, Value(report, "founded by a Household"));
        Assert.Equal(0, Value(report, "Policies sweeping Businesses"));
        Assert.Contains("the fourth quarter of this milestone's risk", report, StringComparison.Ordinal);
    }

    // ---- the fixture ---------------------------------------------------------------------------

    /// <summary>
    /// Cached, on <c>ArrivalDumpTests</c>' reasoning: four tests agree about one run, and building it
    /// four times measures the builder rather than the city.
    /// </summary>
    private static readonly Lazy<string> Report = new(() =>
    {
        (int code, string report) = Run(Ruleset("levied.toml"));

        Assert.Equal(0, code);

        return report;
    });

    private static string Dump() => Report.Value;

    /// <summary>The number a labelled flow row carries.</summary>
    /// <remarks>
    /// ⚠ <b>Parsed rather than matched as a formatted substring.</b> The first draft asserted on
    /// <c>"founded by a Household</c> + thirty spaces + <c>0"</c>, which couples every assertion here
    /// to a column width — so a cosmetic change to the report would fail four tests for a reason none
    /// of them is about, and a *widened* column would make a <c>DoesNotContain</c> pass while the
    /// number stayed zero.
    /// </remarks>
    private static long Value(string report, string label)
    {
        foreach (string line in report.Split('\n'))
        {
            if (!line.TrimStart().StartsWith(label, StringComparison.Ordinal))
            {
                continue;
            }

            string tail = line[(line.IndexOf(label, StringComparison.Ordinal) + label.Length)..];

            return long.Parse(tail.Replace(",", "", StringComparison.Ordinal).Trim(),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        Assert.Fail($"the report has no row labelled '{label}'.");
        return 0;
    }

    private static string Ruleset(string file) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", file);

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Ticks)
    {
        Assert.True(
            Options.TryParse(
                ["--business", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = BusinessDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
