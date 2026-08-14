using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 5c task 6: the <c>[traffic]</c> table and every refusal it states.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <c>HouseholdRulesetLoadTests</c>' discipline and for <c>adr/0064</c>'s reason.
/// </para>
/// <para>
/// <b>The three keys are authored as whole numbers because this file has no decimals</b>, and two of
/// them carry their denomination in their names. <c>alpha_percent = 15</c> and
/// <c>clamp_percent = 400</c> would both be off by two orders of magnitude if the names were
/// <c>alpha</c> and <c>clamp</c>, with nothing in the file able to notice — which is the failure
/// <c>adr/0094</c>'s <c>Speed.PerKilometrePerHour</c> literal actually committed.
/// </para>
/// </remarks>
public sealed class TrafficRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule or a road to exist.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
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

    private static string With(string body) => $"{Nothing}\n\n[traffic]\n{body}";

    private const string Textbook = "alpha_percent = 15\nbeta = 4\nclamp_percent = 400";

    // ---- the absent table -----------------------------------------------------------------------

    /// <summary>
    /// <b>The absence means roads never slow down, and every city before 5c task 6 was that city.</b>
    /// </summary>
    /// <remarks>
    /// <c>[jobs]</c>' and <c>[households]</c>' polarity, and here it has an extra property neither of
    /// those has: omitting the table reproduces the <em>previous</em> behaviour of this simulation
    /// exactly. So a Ruleset written before the volume-delay function existed still means what it
    /// meant, which is what keeps a Ruleset a durable artefact rather than a version-stamped one.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_traffic_table_is_a_city_whose_roads_never_slow_down()
    {
        Ruleset rules = Accepted(Nothing);

        Assert.Equal(TrafficRuleset.None, rules.Traffic);
        Assert.False(rules.Traffic.Runs);
    }

    /// <summary>
    /// <b>A free-flow time passes through an absent function unchanged, bit for bit.</b>
    /// </summary>
    /// <remarks>
    /// The stronger half of the test above: <see cref="TrafficRuleset.None"/> being the default value
    /// is a fact about a struct, and this is a fact about what the engine does with it. A
    /// <see cref="Ratio"/> of four times capacity is well past the clamp any authored table would set.
    /// </remarks>
    [Fact]
    public void An_absent_function_returns_free_flow_at_any_load()
    {
        TravelTime freeFlow = TravelTime.FromSeconds(30);

        Assert.Equal(freeFlow, TrafficRuleset.None.Apply(freeFlow, Ratio.FromFraction(4, 1)));
    }

    // ---- what a stated table must state ---------------------------------------------------------

    /// <summary>
    /// <b>All three keys are required, and none of them has a default.</b>
    /// </summary>
    /// <remarks>
    /// A defaulted <c>alpha_percent</c> would sit inside the range of legitimate answers — 15 is the
    /// textbook figure and is exactly what somebody would default it to — so the file could not be read
    /// to find out whether the number was chosen. Session F's placeholder rule.
    /// </remarks>
    [Theory]
    [InlineData("beta = 4\nclamp_percent = 400", "alpha_percent")]
    [InlineData("alpha_percent = 15\nclamp_percent = 400", "beta")]
    [InlineData("alpha_percent = 15\nbeta = 4", "clamp_percent")]
    public void A_traffic_table_missing_any_key_is_refused(string body, string key)
    {
        RulesetRefusal refusal = Refused(With(body));

        Assert.Contains(key, refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The textbook parameters load, and they are the ones spike S2 ran.</b>
    /// </summary>
    /// <remarks>
    /// S2 R8.0 reports <i>an arc at the clamp costs 39.4× free-flow</i>, and
    /// <c>1 + 0.15 × 4⁴ = 39.4</c> exactly — which is how the clamp's value was recovered, since the
    /// spike published the delay and not the ratio that produced it.
    /// </remarks>
    [Fact]
    public void The_textbook_parameters_load()
    {
        Ruleset rules = Accepted(With(Textbook));

        Assert.True(rules.Traffic.Runs);
        Assert.Equal(4, rules.Traffic.Beta);
        Assert.Equal(Ratio.FromFraction(15, 100), rules.Traffic.Alpha);
        Assert.Equal(Ratio.FromFraction(400, 100), rules.Traffic.Clamp);
    }

    // ---- the ranges -----------------------------------------------------------------------------

    /// <summary>
    /// <b>An alpha outside its band is refused, and zero is inside it.</b>
    /// </summary>
    /// <remarks>
    /// Zero is a road that never slows down, which is a real city and is also what omitting the table
    /// says — two ways to spell one thing, and that is tolerated rather than refused because the
    /// refusal would have to be stated in terms of what the author meant.
    /// </remarks>
    [Theory]
    [InlineData(0, true)]
    [InlineData(15, true)]
    [InlineData(10_000, true)]
    [InlineData(-1, false)]
    [InlineData(10_001, false)]
    public void Alpha_is_a_percentage_and_its_band_is_stated(int alpha, bool accepted)
    {
        string toml = With($"alpha_percent = {alpha}\nbeta = 4\nclamp_percent = 400");

        if (accepted)
        {
            Assert.Equal(Ratio.FromFraction(alpha, 100), Accepted(toml).Traffic.Alpha);
            return;
        }

        Assert.Contains("alpha_percent", Refused(toml).Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>Beta's ceiling is arithmetic, not taste — above it the curve stops being computable.</b>
    /// </summary>
    /// <remarks>
    /// A clamped ratio of four raised to the eighth is 65,536, which is exactly the whole range of the
    /// Q16.16 the multiplication is done in. Six leaves headroom for a clamp of ten. <b>The refusal is
    /// therefore a statement about the representation</b>, which is why it is worth a test of its own:
    /// somebody widening <c>Fixed</c> should find this and move it, rather than meeting a saturating
    /// multiply at runtime in a long run.
    /// </remarks>
    [Theory]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(6, true)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    public void Beta_is_a_small_whole_exponent(int beta, bool accepted)
    {
        string toml = With($"alpha_percent = 15\nbeta = {beta}\nclamp_percent = 400");

        if (accepted)
        {
            Assert.Equal(beta, Accepted(toml).Traffic.Beta);
            return;
        }

        Assert.Contains("beta", Refused(toml).Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A clamp below capacity would make the function constant over the range it exists for.</b>
    /// </summary>
    [Theory]
    [InlineData(100, true)]
    [InlineData(400, true)]
    [InlineData(1_000, true)]
    [InlineData(99, false)]
    [InlineData(1_001, false)]
    public void The_clamp_is_a_volume_over_capacity_ratio_of_at_least_one(int clamp, bool accepted)
    {
        string toml = With($"alpha_percent = 15\nbeta = 4\nclamp_percent = {clamp}");

        if (accepted)
        {
            Assert.Equal(Ratio.FromFraction(clamp, 100), Accepted(toml).Traffic.Clamp);
            return;
        }

        Assert.Contains("clamp_percent", Refused(toml).Reason, StringComparison.Ordinal);
    }

    // ---- the table itself -----------------------------------------------------------------------

    /// <summary>
    /// <b>There is one volume-delay function, so two tables of numbers for it is ambiguous.</b>
    /// </summary>
    [Fact]
    public void A_second_traffic_table_is_refused()
    {
        RulesetRefusal refusal = Refused($"{With(Textbook)}\n\n[traffic]\n{Textbook}");

        Assert.Contains("[traffic]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The section list a bad section name is measured against names this one.</b>
    /// </summary>
    [Fact]
    public void The_section_list_in_a_refusal_names_traffic()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[trafic]\nbeta = 4\n");

        Assert.Contains("[traffic]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the curve ------------------------------------------------------------------------------

    /// <summary>
    /// <b>The published S2 figure comes back out of the shipped parameters.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This is the one number in the whole table with a source outside this corpus, and the source
    /// is synthetic.</b> S2 measured on a generated graph with a uniform origin–destination draw, which
    /// R4 established is the longest-trip distribution available. So the parameters are <em>sourced</em>
    /// and not <em>ratified</em> — <c>adr/0052</c> keeps those apart, and the named ratifier is 5c task
    /// 8's long run.
    /// </remarks>
    [Fact]
    public void At_the_clamp_a_segment_costs_what_the_spike_published()
    {
        TrafficRuleset traffic = Accepted(With(Textbook)).Traffic;
        TravelTime freeFlow = TravelTime.FromSeconds(100);

        TravelTime loaded = traffic.Apply(freeFlow, Ratio.FromFraction(4, 1));

        // 1 + 0.15 x 4^4 = 39.4, so a 100-second Segment costs 3,940 seconds. Q16.16 rounding puts
        // this within a second either way, and asserting the band rather than the value is what keeps
        // the test about the curve rather than about the fixed-point format.
        Assert.InRange(loaded.Raw, TravelTime.FromSeconds(3_939).Raw, TravelTime.FromSeconds(3_941).Raw);
    }

    /// <summary>
    /// <b>Past the clamp the curve is flat, which is the clamp's entire job.</b>
    /// </summary>
    /// <remarks>
    /// A road twice as over-capacity is not twice as slow again. Without this the function runs to
    /// five figures and a router comparing two jammed routes is comparing noise — and, more sharply,
    /// the arithmetic overflows before the delay becomes implausible.
    /// </remarks>
    [Fact]
    public void Beyond_the_clamp_more_traffic_costs_nothing_further()
    {
        TrafficRuleset traffic = Accepted(With(Textbook)).Traffic;
        TravelTime freeFlow = TravelTime.FromSeconds(10);

        Assert.Equal(
            traffic.Apply(freeFlow, Ratio.FromFraction(4, 1)),
            traffic.Apply(freeFlow, Ratio.FromFraction(40, 1)));
    }

    /// <summary>
    /// <b>An empty road costs free-flow exactly, and the first vehicle onto it costs nearly that.</b>
    /// </summary>
    /// <remarks>
    /// The lower end matters as much as the upper one: a volume-delay function that charged a lone
    /// vehicle a measurable delay would make every road slower than its own speed limit, and the
    /// speeds are the one group of numbers in this project with a source outside the corpus.
    /// </remarks>
    [Fact]
    public void An_empty_road_costs_exactly_free_flow()
    {
        TrafficRuleset traffic = Accepted(With(Textbook)).Traffic;
        TravelTime freeFlow = TravelTime.FromSeconds(100);

        Assert.Equal(freeFlow, traffic.Apply(freeFlow, Ratio.Zero));

        // One vehicle against a capacity of forty-two: 1 + 0.15 x (1/42)^4 is 1.00000005, which is
        // below what Q16.16 can represent at all. Free-flow is therefore the honest answer rather than
        // a rounding accident, and it is asserted so that a wider fixed point does not silently make
        // every empty road slow.
        Assert.Equal(freeFlow, traffic.Apply(freeFlow, Ratio.FromFraction(1, 42)));
    }

    /// <summary>
    /// <b>The function is monotone increasing in load, which no single reading can establish.</b>
    /// </summary>
    [Fact]
    public void More_traffic_is_never_faster()
    {
        TrafficRuleset traffic = Accepted(With(Textbook)).Traffic;
        TravelTime freeFlow = TravelTime.FromSeconds(60);
        TravelTime previous = freeFlow;

        for (int load = 0; load <= 500; load += 10)
        {
            TravelTime cost = traffic.Apply(freeFlow, Ratio.FromFraction(load, 100));

            Assert.True(cost >= previous, $"a load of {load}% cost less than the load below it.");

            previous = cost;
        }
    }
}
