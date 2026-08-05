using Borough.Core;

namespace Borough.Tests.Session;

/// <summary>
/// The phase ordering and the concurrency column, which are the determinism contract rather than an
/// implementation detail (<c>02 §1.1</c>).
/// </summary>
public sealed class TickPhaseTests
{
    [Fact]
    public void The_eight_phases_are_numbered_in_execution_order()
    {
        Assert.Equal(0, (int)TickPhase.Input);
        Assert.Equal(1, (int)TickPhase.Wake);
        Assert.Equal(2, (int)TickPhase.Decide);
        Assert.Equal(3, (int)TickPhase.Settle);
        Assert.Equal(4, (int)TickPhase.Move);
        Assert.Equal(5, (int)TickPhase.Layers);
        Assert.Equal(6, (int)TickPhase.Growth);
        Assert.Equal(7, (int)TickPhase.Commit);

        Assert.Equal(Phases.Count, Enum.GetValues<TickPhase>().Length);
    }

    /// <summary>
    /// The permission column, transcribed from <c>02 §1.1</c>.
    /// </summary>
    /// <remarks>
    /// <b>This test is a copy of the design document and that is its purpose.</b> Changing what a
    /// phase may do is a change to the determinism contract, so it should require editing an assertion
    /// that names the document, not merely editing a table that nothing reads.
    /// </remarks>
    [Theory]
    [InlineData(TickPhase.Input, PhaseConcurrency.Serial)]
    [InlineData(TickPhase.Wake, PhaseConcurrency.Serial)]
    [InlineData(TickPhase.Decide, PhaseConcurrency.ParallelReadOnly)]
    [InlineData(TickPhase.Settle, PhaseConcurrency.Serial)]
    [InlineData(TickPhase.Move, PhaseConcurrency.Parallel)]
    [InlineData(TickPhase.Layers, PhaseConcurrency.Parallel)]
    [InlineData(TickPhase.Growth, PhaseConcurrency.Serial)]
    [InlineData(TickPhase.Commit, PhaseConcurrency.Serial)]
    public void Permission_is_what_the_simulation_model_states(
        TickPhase phase,
        PhaseConcurrency permitted) =>
        Assert.Equal(permitted, Phases.Permits(phase));

    /// <summary>
    /// <b>Permission is an upper bound, and this is the test that says so.</b> Phase 1 runs everything
    /// serially while <c>02 §1.1</c> permits two phases to be parallel — that is a legal gap, not a
    /// defect, and <c>05 §6</c> is the document that will close it. Running a phase in parallel where
    /// the design says serial would be the real failure, and it is this comparison that would catch it.
    /// </summary>
    [Fact]
    public void This_build_runs_no_phase_more_concurrently_than_it_may()
    {
        for (int i = 0; i < Phases.Count; i++)
        {
            var phase = (TickPhase)i;
            PhaseConcurrency runs = Phases.Runs(phase);

            if (runs == PhaseConcurrency.Serial)
            {
                continue;
            }

            Assert.Equal(Phases.Permits(phase), runs);
        }
    }

    /// <summary>
    /// Decide keeps its read-only obligation even though nothing is parallel yet — the obligation is
    /// what lets every entity table stay single-buffered (<c>adr/0037</c>), which is as true at one
    /// thread as at eight.
    /// </summary>
    [Fact]
    public void Decide_is_read_only_even_though_this_build_is_serial()
    {
        Assert.Equal(PhaseConcurrency.ParallelReadOnly, Phases.Runs(TickPhase.Decide));

        for (int i = 0; i < Phases.Count; i++)
        {
            var phase = (TickPhase)i;

            if (phase != TickPhase.Decide)
            {
                Assert.NotEqual(PhaseConcurrency.ParallelReadOnly, Phases.Permits(phase));
            }
        }
    }
}
