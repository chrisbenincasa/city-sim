using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/hungry.toml</c> — the only shipped world that wires a Good to a Need, and the second
/// file <b>nothing in the build ran</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Three <c>src/</c> files name it and all three mentions are doc-comment prose.</b> Found by
/// <c>plans/0050</c>'s demonstration census: no test loaded it and no code path did either, so the
/// one file carrying <c>[[resource]] need</c> and a <c>[needs]</c> table was held up by nothing but
/// the two anonymous folder sweeps that assert it parses.
/// </para>
/// <para>
/// ⚠ <b><c>NeedTests</c> owns the mechanism and this owns the FILE.</b> The depth-is-a-duration
/// repair is asserted there against a hand-built fixture; what was missing is that the shipped world
/// still reaches the mechanism at all. ***A fixture proves the code and a shipped file proves the
/// world can get there***, and <c>choosy.toml</c> is the standing example of the second failing while
/// every test of the first passed.
/// </para>
/// <para>
/// ⚠ <b>The assertion is a deficit and never a depth.</b> The depth is
/// <c>tick − StarvedSince</c> recomputed on a daily sweep, so it is bounded by the world's own
/// rehousing cycle rather than by the model — <c>plans/0045</c> measured deepest <b>−31</b> then
/// <b>−11</b> on different runs of the same idea. ***A number that moves with the run is an
/// instrument's and not an assertion's*** (<c>plans/0032</c>), so this asks only that Households go
/// hungry, which is the thing the file exists to make reachable.
/// </para>
/// </remarks>
public sealed class HungryRulesetTests
{
    private const int Citizens = 2_000;
    private const int Ticks_ = 4_096;

    private static readonly WorldKey Key = WorldKey.FromSeed(0xF0U);

    private static World Run(string file)
    {
        string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file);
        RulesetLoadResult parsed = RulesetLoader.Load(path);
        Ruleset rules = parsed.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{parsed.Describe()}");

        World world = new(Citizens, rules, Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Core.Quantities.Ticks.Zero);

        for (int tick = 0; tick < Ticks_; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

    private static (int Hungry, int Live) Deficit(World world)
    {
        int hungry = 0;
        int live = 0;

        for (int slot = 0; slot < world.Households.Rows.Capacity; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            live++;

            if (world.Households.Sustenance[slot] < 0)
            {
                hungry++;
            }
        }

        return (hungry, live);
    }

    /// <summary>
    /// <b>Households on this world go hungry, and on the world it is a diff of they cannot.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>evicted.toml</c> is the control because <c>hungry.toml</c> IS <c>evicted.toml</c> plus
    /// the two keys.</b> Both starve their tenants; only one has a Need for the starving to register
    /// in. ***So the control is not a fed city — it is the same hunger with nothing measuring it***,
    /// which is exactly the state the whole build was in before <c>[needs]</c> shipped.
    /// </remarks>
    [Fact]
    public void The_hungry_world_registers_a_deficit_where_its_parent_cannot()
    {
        (int hungry, int live) = Deficit(Run("hungry.toml"));
        (int control, _) = Deficit(Run("evicted.toml"));

        Assert.True(live > 0, "hungry.toml houses nobody, so nothing below measures anything.");

        Assert.True(
            hungry > 0,
            $"no Household of {live} on hungry.toml is in Sustenance deficit after {Ticks_} Ticks. "
                + "This is the only shipped file stating [needs] and `need` on a Resource, and its "
                + "tenants' larder is filled by nothing -- so a zero here is the Need failing to "
                + "move rather than a city that is fed. RuleEngine.MoveNeed is the writer and "
                + "RuleEngine.RefreshNeed is the daily recompute.");

        Assert.True(
            control == 0,
            $"{control} Households on evicted.toml are in deficit. That file states no [needs] and "
                + "no Resource carries a `need`, so its Sustenance column can only be zero -- a "
                + "non-zero reading means the Need is being written by something that is not the "
                + "Ruleset, and the comparison above stops meaning anything.");
    }
}
