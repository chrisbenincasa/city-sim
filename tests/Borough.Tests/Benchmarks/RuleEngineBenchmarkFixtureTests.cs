using Borough.Core;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// What one invocation of <see cref="RuleEngineBenchmarks"/> actually costs in evaluations.
/// </summary>
/// <remarks>
/// <para>
/// <b>A benchmark's denominator is a claim, and this is the machine that settles it.</b> The
/// tripwire slice 7 publishes is <em>nanoseconds per evaluation</em>, which is a timing divided by a
/// count — and S2 hit the same shape three separate times, most sharply in R3, where a ratio was
/// divided by a denominator that had been measured once, first, and cold. Here the count is not
/// measured cold, it is not measured once, and it is not assumed at all: it is read off the engine's
/// own counter, on the same arrangement the benchmark times.
/// </para>
/// <para>
/// <b>These are assertions about the fixture, not about the engine.</b> If a later change to the
/// Rule engine moves them, the number to correct is the one in <c>plans/0011</c>, and the failure
/// here is the notice that it moved.
/// </para>
/// </remarks>
public sealed class RuleEngineBenchmarkFixtureTests
{
    private const int Buildings = 64;

    /// <summary>
    /// Runs Phase 1 and Phase 2 exactly as the benchmark does, and closes the Tick so the counters
    /// describe the phase that was run rather than a whole Tick that was not.
    /// </summary>
    private static RuleActivity Decide(RuleEngineBenchmarks.Load shape)
    {
        (Simulation simulation, Ticks due) = RuleEngineFixture.Arrange(shape, Buildings);

        simulation.Rules.CollectDue(due);
        simulation.Rules.Evaluate(due);
        simulation.Rules.CloseTick();

        return simulation.Rules.Drain();
    }

    /// <summary>Every Building is due, which is what makes the arrangement a worst Tick.</summary>
    [Theory]
    [InlineData(RuleEngineBenchmarks.Load.Fires)]
    [InlineData(RuleEngineBenchmarks.Load.Starves)]
    [InlineData(RuleEngineBenchmarks.Load.Walks)]
    public void Every_building_is_due_on_the_measured_tick(RuleEngineBenchmarks.Load shape)
    {
        Assert.Equal(Buildings, Decide(shape).Due.Sum);
    }

    /// <summary>
    /// A firing Rule and a starving one are both one evaluation, and they are not the same evaluation.
    /// </summary>
    /// <remarks>
    /// <b>The counts are equal and the costs are not</b>, which is why both are timed. A Rule that
    /// fires walks every term and then divides once per touched Bin; a Rule that starves stops at the
    /// first Bin that cannot carry its delta and returns. So <em>Starves</em> is the cheaper
    /// evaluation and the tripwire must be derived from <em>Fires</em>, or it prices the healthy city
    /// at the broken city's rate.
    /// </remarks>
    [Theory]
    [InlineData(RuleEngineBenchmarks.Load.Fires)]
    [InlineData(RuleEngineBenchmarks.Load.Starves)]
    public void A_flat_ruleset_costs_one_evaluation_per_due_rule(RuleEngineBenchmarks.Load shape)
    {
        RuleActivity activity = Decide(shape);

        Assert.Equal(Buildings, activity.Evaluations.Sum);
        Assert.Equal(0, activity.ChainRungs.Sum);
    }

    /// <summary>
    /// The laddered arrangement is three evaluations and three rungs per due Rule.
    /// </summary>
    /// <remarks>
    /// Head, then two links, then a terminal that is reached and not evaluated — so the two counters
    /// coincide at this depth for two different reasons, and the published figure divides the timing
    /// by the <em>evaluations</em>.
    /// </remarks>
    [Fact]
    public void The_laddered_ruleset_costs_three_evaluations_and_three_rungs_per_due_rule()
    {
        RuleActivity activity = Decide(RuleEngineBenchmarks.Load.Walks);

        Assert.Equal(Buildings * 3, activity.Evaluations.Sum);
        Assert.Equal(Buildings * 3, activity.ChainRungs.Sum);
    }

    /// <summary>
    /// The idle arrangement really does put every Rule on every Tick, which is what
    /// <c>RuleTickBenchmarks</c> divides by.
    /// </summary>
    /// <remarks>
    /// <b>A steady state that is not steady would be invisible in the timing.</b> If the rate-1 re-arm
    /// missed, a later Tick would simply be cheaper, and a benchmark averaging over thousands of Ticks
    /// would report a comfortable mean for a Tick that was mostly empty. So the assertion is made
    /// twice — on the first busy Tick and again after several — and both must see the whole set.
    /// </remarks>
    [Fact]
    public void The_idle_arrangement_puts_every_rule_on_every_tick()
    {
        (Simulation simulation, Ticks _) = RuleEngineFixture.Arrange(
            RuleEngineBenchmarks.Load.Idle, Buildings);

        simulation.VerifyDecideWritesNothing = false;

        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);

        RuleActivity first = simulation.Rules.Drain();

        for (int i = 0; i < 8; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        RuleActivity later = simulation.Rules.Drain();

        // Tick 0 has nothing due (armed at delay 1), Tick 1 has the whole set.
        Assert.Equal(Buildings, first.Due.Sum);
        Assert.Equal(Buildings, first.Due.Peak);

        // Eight Ticks, every one of them carrying the whole set: the re-arm is holding.
        Assert.Equal(Buildings * 8, later.Due.Sum);
        Assert.Equal(Buildings, later.Due.Peak);

        // Two evaluations per due Rule — Phase 2 and Phase 3's re-check — and no chain anywhere.
        Assert.Equal(Buildings * 8 * 2, later.Evaluations.Sum);
        Assert.Equal(0, later.ChainRungs.Sum);
    }

    /// <summary>
    /// The whole due set lands on one Tick, so the peak and the sum are the same number.
    /// </summary>
    /// <remarks>
    /// Worth asserting because it is the property that makes the benchmark a statement about a Tick:
    /// an arrangement spread over two Ticks would time the same total work and price a Tick at half
    /// of it.
    /// </remarks>
    [Fact]
    public void The_measured_work_is_one_tick_of_it()
    {
        RuleActivity activity = Decide(RuleEngineBenchmarks.Load.Fires);

        Assert.Equal(activity.Evaluations.Sum, activity.Evaluations.Peak);
        Assert.Equal(activity.Due.Sum, activity.Due.Peak);
    }
}
