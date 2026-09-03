using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 11 task 1: <c>[[building]] arrivals_per_day</c>, the key that makes a kind a gate.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="ParkingRulesetLoadTests"/>' discipline.
/// </para>
/// <para>
/// <b>The key carries its denomination in its name</b> — <c>arrivals_per_day</c> rather than
/// <c>throughput</c> — and unlike <c>radius_metres</c>, which took the lesson from another key, this
/// one took it from a decision the milestone had to stop and make.
/// [`adr/0088`] states throughput as <c>min(declared ceiling, Segment capacity)</c> without stating
/// either denominator, and the second is <b>Vehicles per Day</b> while the first, at this milestone,
/// counts <b>Households</b>. ***A formula written down without its units reads as settled and is
/// not.*** <c>plans/0035</c> decision 9.
/// </para>
/// <para>
/// ⚠ <b>The tests hold the consequences of *presence declares the kind*, not the choice itself.</b>
/// <see cref="A_kind_that_states_no_throughput_is_not_a_gate"/> and
/// <see cref="A_gate_that_admits_nobody_is_refused"/> are the pair that makes the single key
/// unambiguous; either one alone would leave a zero meaning two things.
/// </para>
/// </remarks>
public sealed class OutsideConnectionRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset with one Building kind in it.</summary>
    private const string OneKind = """
        [[resource]]
        name = "flour"
        family = "good"

        [[building]]
        name = "dwelling"
        """;

    /// <summary>
    /// The Unplaced Pool's sink, which a Ruleset declaring a gate is refused without.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is appended to the gate fixtures and to no others.</b> A door into the Pool with no
    /// way out of it grows a collection without bound, which <c>adr/0006</c> forbids, so the loader
    /// refuses the pair rather than either half (<c>plans/0035</c> <b>F28</b>). Which is why the two
    /// fixtures below spelling <c>arrivals_per_day = 0</c> and <c>-1</c> do <i>not</i> carry it:
    /// neither of those is a gate, and appending a sink to them would hide the refusal they exist to
    /// watch fire.
    /// </remarks>
    private const string Sink = """

        [placement]
        interval = 32
        revisit_ticks = 1024
        candidates = 3
        gives_up_after_days = 120
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

    /// <summary>A kind stating a throughput is a gate, and the number survives the load.</summary>
    [Fact]
    public void A_kind_that_states_a_throughput_is_a_gate()
    {
        Ruleset ruleset = Accepted($"{OneKind}\narrivals_per_day = 40{Sink}");

        Assert.Equal(40, ruleset.Kind(1).ArrivalsPerDay);
    }

    /// <summary>
    /// A kind that says nothing about arrivals is not a gate, which is every kind in every Ruleset
    /// written before this key existed.
    /// </summary>
    /// <remarks>
    /// <b>Absence is the unset spelling and it is behaviour-preserving.</b> Nine shipped Rulesets
    /// declare kinds and none of them is a door into the city, so omission has to mean *not a gate*
    /// rather than *a gate with an unstated ceiling*.
    /// </remarks>
    [Fact]
    public void A_kind_that_states_no_throughput_is_not_a_gate()
    {
        Ruleset ruleset = Accepted(OneKind);

        Assert.Equal(0, ruleset.Kind(1).ArrivalsPerDay);
    }

    /// <summary>
    /// A stated zero is refused, because it is the one thing the single key could not otherwise say.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what lets presence do duty as the declaration.</b> If zero were accepted, a kind
    /// spelling <c>arrivals_per_day = 0</c> and a kind omitting the key entirely would both read as
    /// *not a gate* — but the author of the first plainly meant to write a gate, and got a Ruleset
    /// that loads clean and does nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>It is the opposite call from <c>parking</c>, deliberately.</b> A tower with no parking is
    /// <c>adr/0009</c>'s own worked example, so there zero is the interesting value. A gate that
    /// admits nobody is not a building anybody designs.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_gate_that_admits_nobody_is_refused()
    {
        RulesetRefusal refusal = Refused($"{OneKind}\narrivals_per_day = 0");

        Assert.Contains("arrivals_per_day", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("never opens", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A negative throughput is refused, on the same reasoning as every other count.</summary>
    [Fact]
    public void A_negative_throughput_is_refused()
    {
        RulesetRefusal refusal = Refused($"{OneKind}\narrivals_per_day = -1");

        Assert.Contains("arrivals_per_day", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The key is independent of every other number on the kind, and a gate may also house and employ.
    /// </summary>
    /// <remarks>
    /// <b>An Outside Connection is an ordinary Building</b> (<c>adr/0088</c>: *"No new table, no new
    /// column, no new mechanism"*), so nothing here may quietly become a kind discriminator. A port
    /// with staff is a coherent thing to author and the loader must not have opinions about it.
    /// </remarks>
    [Fact]
    public void A_gate_is_an_ordinary_kind_and_may_do_everything_else_too()
    {
        Ruleset ruleset = Accepted(
            $"""
            {OneKind}
            arrivals_per_day = 40
            houses = true
            premises = true
            {Sink}
            """);

        KindDefinition kind = ruleset.Kind(1);

        Assert.Equal(40, kind.ArrivalsPerDay);
        Assert.True(kind.Houses);
        Assert.True(kind.Premises);
    }
}
