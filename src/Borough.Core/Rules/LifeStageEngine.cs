namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// What one Day's worth of Life Stage transitions is, and what it reports.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>A stage advances, or the Household ends</b> — <c>plans/0046</c> stages 1 and 2. What is still
/// missing is the <em>source</em>: nothing is born, so a run of this ends with an <b>empty city</b>.
/// ***That is the correct outcome at stage 2 rather than an unfinished one***, and the ordering is
/// the safety property: a source without a sink is <c>adr/0006</c>, a sink without a source merely
/// empties, and an emptying city is bounded below by zero — so the city is allowed to die before it
/// is allowed to breed. Stage 3 gives Mature Family's exit a cohort of Young Households and the
/// population stops falling.
/// </para>
/// <para>
/// ⚠ <b>Dissolution here is SCHEDULED and <c>adr/0011</c> calls it a decision.</b> That ADR gives the
/// stage table two decisions — how many children, and whether to dissolve — and the discrete-choice
/// machinery either of them would read is <em>unbuilt</em> under <c>adr/0070</c>. So what ships is
/// the clock: ***a terminal stage's countdown ending IS the dissolution***. The draw is still there,
/// in the countdown's window, which is why the terminal stages do not empty in lockstep. Replacing
/// the schedule with a choice moves no structure — it changes what is consulted at this branch.
/// </para>
/// <para>
/// <b>An advance is three writes and a draw</b> — read the stage, look up its successor, write it,
/// re-arm on a freshly drawn countdown. <b>A dissolution is <see cref="World.Dissolve"/></b>, which
/// moves the estate to the treasury and destroys the row and its members.
/// </para>
/// </remarks>
/// <param name="Advanced">Households that moved to a successor stage.</param>
/// <param name="Dissolved">Households that reached a terminal stage and ended.</param>
/// <param name="Dropped">
/// Households taken off the wheel without either — a stage id the Ruleset in force no longer
/// declares. ⚠ <b>Counted apart from <paramref name="Dissolved"/> on purpose</b>: a drop is a hot
/// reload's footprint and a dissolution is the mechanism, and a single column would have made the
/// first look like the second in every readout.
/// </param>
public readonly record struct LifeStageReading(int Advanced, int Dissolved, int Dropped)
{
    /// <summary>Whether this Day did anything at all.</summary>
    public bool Ran => Advanced > 0 || Dissolved > 0 || Dropped > 0;
}

/// <summary>Advances or dissolves every Household whose Life Stage countdown ends today.</summary>
internal sealed class LifeStageEngine(World world)
{
    private readonly World _world = world;

    /// <summary>
    /// Runs one Day's transitions, or nothing at all on the 2,047 Ticks that are not midnight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Day boundary is the whole schedule.</b> A stage countdown is denominated in Days by
    /// <c>adr/0011</c>, so there is no finer occasion to fire on and no phase to spread across —
    /// which is why <see cref="LifeStageWheel"/> has no fine tier and this method has no cascade.
    /// ⚠ <b>Every Household due on a Day therefore transitions on that Day's first Tick.</b> Order
    /// 3,000 of them at a million Citizens; an advance is a lookup, a draw and a re-arm, and a
    /// dissolution is a row and its members.
    /// </para>
    /// <para>
    /// <b>Drained to exhaustion, and a re-armed Household cannot come back round.</b>
    /// <see cref="LifeStageWheel.Arm"/> refuses an arming due today, so a Household that transitions
    /// here lands in a bucket this loop has already left — the same property
    /// <see cref="EventWheel.Arm"/>'s zero-delay refusal buys the Rule drain.
    /// </para>
    /// <para>
    /// ⚠ <b>The row is POPPED before it is dissolved, and that is load-bearing.</b>
    /// <see cref="World.Dissolve"/> frees the Household row, and a freed row still linked into a
    /// bucket is <c>plans/0035</c> <b>F29</b> — the next allocation of that slot gets inserted into a
    /// list it is already in. The pop happens first here and <see cref="World.Dissolve"/> unlinks
    /// again for the callers that are not this one.
    /// </para>
    /// </remarks>
    public LifeStageReading Sweep(Ticks tick)
    {
        if (!_world.Rules.DeclaresLifeStages || tick.Raw % (ulong)Ticks.PerDay != 0UL)
        {
            return default;
        }

        long today = LifeStageWheel.DayOf(tick);
        int advanced = 0;
        int dissolved = 0;
        int dropped = 0;

        for (int slot = _world.LifeStages.PopDue(today);
             slot != Rows.NoSlot;
             slot = _world.LifeStages.PopDue(today))
        {
            byte stage = _world.Households.LifeStage[slot];

            // A Household whose stage is out of range for the Ruleset in force. Reachable through a
            // hot reload that shortened the stage table (adr/0015) rather than through anything the
            // simulation does, and it is dropped off the wheel rather than thrown on: a Ruleset the
            // designer has just narrowed is not a corrupt world. ⚠ It is NOT dissolved -- a designer
            // deleting a table row must not kill the Households standing in it.
            if (stage == 0 || stage > _world.Rules.LifeStageCount)
            {
                dropped++;
                continue;
            }

            byte next = _world.Rules.LifeStage(stage).NextStage;

            if (next == 0)
            {
                _world.Dissolve(_world.Households.Rows.At(slot), tick);
                dissolved++;
                continue;
            }

            _world.Households.LifeStage[slot] = next;
            _world.ArmLifeStageAt(slot, today);
            advanced++;
        }

        return new LifeStageReading(advanced, dissolved, dropped);
    }
}
