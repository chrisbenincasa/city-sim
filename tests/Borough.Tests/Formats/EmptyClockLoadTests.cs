using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// <c>plans/0045</c> queue item 13: <c>[[building]] abandoned_when_empty_after_days</c> at the parse
/// site.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, which is
/// <c>adr/0048</c>'s discipline and what <c>RefusalCountTests</c> counts.
/// </para>
/// <para>
/// 🔴 <b>Two of the four here could not be checked where the value is parsed, and that is the
/// interesting half.</b> The sink obligation is a property of <em>which keys a kind states</em> —
/// two of them can now abandon a Building and either leaves a shell — and the revisit relation is a
/// property of <b>a kind against a table it cannot see</b>. ***A refusal that needs two readers has
/// to live where both have run***, which is why one is in <c>ReadKinds</c> and one is in
/// <c>ReadPlacement</c>.
/// </para>
/// </remarks>
public sealed class EmptyClockLoadTests
{
    /// <summary>
    /// The smallest file that can state the key: one Good, one kind, and a placement pass.
    /// </summary>
    /// <remarks>
    /// <b><c>[placement]</c> is not decoration here.</b> The revisit relation is only checkable
    /// against a stated pass, so a fixture without one would exercise three of the four refusals and
    /// silently skip the fourth.
    /// </remarks>
    private const string Emptying = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        houses = true
        premises = true
        abandoned_when_empty_after_days = 20
        collapses_after_days = 10

        [placement]
        interval      = 32
        revisit_ticks = 1024
        candidates    = 3
        """;

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }

    /// <summary>The key reaches the kind, in Ticks, and the file's unit is Days.</summary>
    [Fact]
    public void The_clock_is_authored_in_days_and_arrives_in_ticks()
    {
        Ruleset ruleset = Accepted(Emptying);

        Assert.Equal(20 * Ticks.PerDay, ruleset.Kind(1).AbandonedWhenEmptyAfterTicks);
    }

    /// <summary>Absent means a kind of this sort stands empty for ever.</summary>
    /// <remarks>
    /// <b>Both keys go together</b>, because a lone <c>collapses_after_days</c> is refused by the
    /// pair check below.
    /// </remarks>
    [Fact]
    public void A_kind_stating_neither_key_stands_empty_for_ever()
    {
        Ruleset ruleset = Accepted(
            Emptying
                .Replace("abandoned_when_empty_after_days = 20\n", "", StringComparison.Ordinal)
                .Replace("collapses_after_days = 10\n", "", StringComparison.Ordinal));

        Assert.Equal(0, ruleset.Kind(1).AbandonedWhenEmptyAfterTicks);
    }

    /// <summary>
    /// 🔴 <b>Zero is refused rather than meaning <em>never</em>, and that is not the usual polarity.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0069</c> has construction house nobody</b>, so a Building is empty from the Tick it
    /// is raised — a clock of zero abandons every Building on the sweep after it goes up, which is a
    /// city nobody meant to author. ***Omitting the key is how a kind says it stands empty for
    /// ever***, and the refusal says so rather than letting the two spellings both look like
    /// intentions.
    /// </remarks>
    [Fact]
    public void A_clock_of_zero_is_refused_because_absence_is_how_never_is_spelled()
    {
        RulesetRefusal refusal = Refused(Emptying.Replace(
            "abandoned_when_empty_after_days = 20",
            "abandoned_when_empty_after_days = 0",
            StringComparison.Ordinal));

        Assert.Contains("must be positive", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("adr/0069", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shell's sink is owed by the new key exactly as it is by <c>condemn_after_days</c>.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0006</c> at the parse site.</b> Two keys can now abandon a Building and either of
    /// them leaves a standing shell, so the sink is required if <em>either</em> is stated — the pair
    /// check became a three-way one in the same edit that added this key.
    /// </remarks>
    [Fact]
    public void An_empty_clock_with_no_collapse_duration_is_refused()
    {
        RulesetRefusal refusal = Refused(
            Emptying.Replace("collapses_after_days = 10\n", "", StringComparison.Ordinal));

        Assert.Contains("can abandon a Building", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("adr/0006", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// And the converse: a sink with nothing that could fill it is refused, naming both thresholds.
    /// </summary>
    [Fact]
    public void A_collapse_duration_with_neither_threshold_is_refused()
    {
        RulesetRefusal refusal = Refused(Emptying.Replace(
            "abandoned_when_empty_after_days = 20\n", "", StringComparison.Ordinal));

        Assert.Contains("abandoned_when_empty_after_days", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("condemn_after_days", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔴 <b>The refusal neither reader could make alone: a clock shorter than the pass that fills
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>revisit_ticks</c> is authored as how long the placement pass takes to look at everybody
    /// waiting once</b> (<c>adr/0059</c>), and placement is the only thing that fills a Building. A
    /// clock that expires inside one period therefore gives a Building a lifetime drawn from the
    /// <em>cadence</em> rather than from the Ruleset — the class of defect <c>adr/0059</c> exists to
    /// refuse one level up. ⚠ <b>One period is a floor and not a comfortable margin</b>: the sample
    /// is drawn with replacement, so about <c>1/e</c> of the Pool goes unlooked-at in any one period.
    /// The loader refuses what is wrong by construction and leaves the headroom to the author.
    /// </remarks>
    [Fact]
    public void A_clock_shorter_than_the_placement_revisit_is_refused()
    {
        // A whole Day is 2,048 Ticks, so a revisit period of 4,096 is two Days and swallows it.
        RulesetRefusal refusal = Refused(
            Emptying
                .Replace(
                    "abandoned_when_empty_after_days = 20",
                    "abandoned_when_empty_after_days = 1",
                    StringComparison.Ordinal)
                .Replace("revisit_ticks = 1024", "revisit_ticks = 4096", StringComparison.Ordinal));

        Assert.Contains("revisit_ticks", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("adr/0069", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Exactly one period over the line is accepted, which is where the refusal stops.</summary>
    /// <remarks>
    /// <b>Both sides of a relation are asserted</b>, because a check written with the comparison the
    /// wrong way round refuses everything and every test that only writes a violation still passes.
    /// </remarks>
    [Fact]
    public void A_clock_longer_than_the_revisit_is_accepted()
    {
        Ruleset ruleset = Accepted(
            Emptying
                .Replace(
                    "abandoned_when_empty_after_days = 20",
                    "abandoned_when_empty_after_days = 1",
                    StringComparison.Ordinal)
                .Replace("revisit_ticks = 1024", "revisit_ticks = 2047", StringComparison.Ordinal));

        Assert.Equal(Ticks.PerDay, ruleset.Kind(1).AbandonedWhenEmptyAfterTicks);
    }
}
