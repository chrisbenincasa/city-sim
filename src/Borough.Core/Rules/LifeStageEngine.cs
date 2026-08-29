namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// What one Day's worth of Life Stage transitions is, and what it reports.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>A stage advances and nothing else happens</b>, which is <c>plans/0046</c> stage 1 exactly.
/// <c>adr/0011</c> gives the table two <em>decisions</em> — how many children, and when to dissolve
/// — and both are later stages: dissolution at 2, generation at 3. ***So a run of this ends with
/// every Household in the chain's terminal stage and the population unchanged***, which is the
/// correct outcome rather than an unfinished one. The ordering is the safety property: a source
/// without a sink is <c>adr/0006</c>, a sink without a source merely empties, and an emptying city is
/// bounded below by zero — so the city is allowed to die before it is allowed to breed.
/// </para>
/// <para>
/// <b>The transition itself is three writes and a draw.</b> Read the stage, look up its successor,
/// write it, and re-arm on a freshly drawn countdown. There is no composition change, no Citizen
/// created or destroyed and no money moved, because every one of those belongs to a stage that has
/// not landed.
/// </para>
/// </remarks>
public readonly record struct LifeStageReading(int Advanced, int Retired)
{
    /// <summary>Whether this Day did anything at all.</summary>
    public bool Ran => Advanced > 0 || Retired > 0;
}

/// <summary>Advances every Household whose Life Stage countdown ends today.</summary>
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
    /// 3,000 of them at a million Citizens; the work is a lookup, a draw and a re-arm.
    /// </para>
    /// <para>
    /// <b>Drained to exhaustion, and a re-armed Household cannot come back round.</b>
    /// <see cref="LifeStageWheel.Arm"/> refuses an arming due today, so a Household that transitions
    /// here lands in a bucket this loop has already left — the same property
    /// <see cref="EventWheel.Arm"/>'s zero-delay refusal buys the Rule drain.
    /// </para>
    /// <para>
    /// ⚠ <b>A terminal stage is popped and NOT re-armed</b>, which is how a Household leaves this
    /// wheel for good. It keeps its <c>life_stage</c> and its <c>next_stage_day</c> — the second is
    /// now history rather than a claim — and nothing wakes it again until stage 2 gives the terminal
    /// stages a dissolution decision.
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
        int retired = 0;

        for (int slot = _world.LifeStages.PopDue(today);
             slot != Rows.NoSlot;
             slot = _world.LifeStages.PopDue(today))
        {
            byte stage = _world.Households.LifeStage[slot];

            // A Household whose stage is out of range for the Ruleset in force. Reachable through a
            // hot reload that shortened the stage table (adr/0015) rather than through anything the
            // simulation does, and it is dropped off the wheel rather than thrown on: a Ruleset the
            // designer has just narrowed is not a corrupt world.
            if (stage == 0 || stage > _world.Rules.LifeStageCount)
            {
                retired++;
                continue;
            }

            byte next = _world.Rules.LifeStage(stage).NextStage;

            if (next == 0)
            {
                retired++;
                continue;
            }

            _world.Households.LifeStage[slot] = next;
            _world.ArmLifeStageAt(slot, today);
            advanced++;
        }

        return new LifeStageReading(advanced, retired);
    }
}
