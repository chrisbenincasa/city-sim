using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/maintained.toml</c> — the recovery pole, and the file <b>nothing in the build ran</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>It was referenced by NOTHING in <c>src/</c> or <c>tests/</c>, and <c>CLAUDE.md</c> quotes a
/// census out of it.</b> Found by <c>plans/0050</c>'s demonstration census. The claim standing in the
/// repository map — ***one Rule is the whole difference between a city that loses half its stock and
/// one that loses none*** — is the sharpest sentence anywhere in that cell, and the only thing holding
/// it was that somebody had once run the file by hand. ⚠ <b>Its two anonymous folder sweeps assert it
/// LOADS</b> (<c>TerrainRulesetLoadTests</c>, <c>RulesetSchemaTests</c>) and say nothing about the
/// city, so a change that quietly stopped <c>maintain</c> from firing would have gone red nowhere.
/// </para>
/// <para>
/// <b>The A/B is the whole test, because the file IS a diff.</b> <c>maintained.toml</c> is
/// <c>declining.toml</c> plus the <c>maintain</c> Rule and the two edits that let it bite, so the
/// only honest assertion runs both worlds on one seed at one size and compares. ***A world asserted
/// alone would pass with the mechanism deleted and the failure removed with it.***
/// </para>
/// <para>
/// ⚠ <b>The second test is what stops the first from being satisfied by a city that never starves.</b>
/// Zero condemned is the right answer for a world where nothing is ever short AND for a world that
/// heals, and those are different cities. <c>adr/0053</c> — <em>recovery is total rather than a debt
/// worked off</em> — is only demonstrated by the second, so the pressure has to be observed non-zero.
/// </para>
/// <para>
/// ⚠ <b>2,000 Citizens over 8,192 Ticks, not the 10,000 over 32,768 the repository map quotes.</b>
/// This is an assertion and not an instrument (<c>plans/0032</c>): it asks whether the mechanism still
/// works, and re-deriving the published census on every run would price a constant nobody moved. The
/// shape holds at both sizes and only the shape is asserted.
/// </para>
/// </remarks>
public sealed class MaintainedRulesetTests
{
    private const int Citizens = 2_000;
    private const int Ticks_ = 8_192;

    /// <summary>One seed for both arms — the comparison is meaningless on two.</summary>
    private static readonly WorldKey Key = WorldKey.FromSeed(0xF0U);

    private static Ruleset Load(string file)
    {
        string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file);
        RulesetLoadResult result = RulesetLoader.Load(path);

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static World Run(string file)
    {
        World world = new(Citizens, Load(file), Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Core.Quantities.Ticks.Zero);

        for (int tick = 0; tick < Ticks_; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

    private static int Abandoned(World world)
    {
        int count = 0;

        for (int slot = 0; slot < world.Buildings.Rows.Capacity; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.IsAbandoned(slot))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// <b>One Rule is the difference between a city that loses stock and one that loses none.</b>
    /// </summary>
    [Fact]
    public void The_maintained_city_loses_nothing_where_the_declining_one_loses_stock()
    {
        int declining = Abandoned(Run("declining.toml"));
        int maintained = Abandoned(Run("maintained.toml"));

        Assert.True(
            declining > 0,
            $"declining.toml abandoned {declining} Buildings over {Ticks_} Ticks at {Citizens} "
                + "Citizens. It is the control arm and it is supposed to decay -- a zero here means "
                + "the comparison below proves nothing, and the defect is in decline rather than in "
                + "maintained.toml.");

        Assert.True(
            maintained == 0,
            $"maintained.toml abandoned {maintained} Buildings against declining.toml's {declining} "
                + "on the same seed. Its whole claim is that giving `repairs` a producer removes the "
                + "failure outright (adr/0053: recovery is total rather than a debt worked off), so "
                + "any abandonment here is the `maintain` Rule not reaching the Bin `upkeep` draws "
                + "on. Check --evidence before changing this number: the first spelling of that Rule "
                + "wrote exactly the Bin's capacity and DEADLOCKED the Building.");
    }

    /// <summary>
    /// <b>The dwellings really do go short, so the zero above is recovery and not an idle city.</b>
    /// </summary>
    [Fact]
    public void The_maintained_city_starves_and_recovers_rather_than_never_starving()
    {
        World world = Run("maintained.toml");
        bool everStarved = false;

        for (int slot = 0; slot < world.RuleInstances.Rows.Capacity; slot++)
        {
            if (world.RuleInstances.Rows.IsLive(slot) && world.RuleInstances.IsStarving(slot))
            {
                everStarved = true;
                break;
            }
        }

        Assert.True(
            everStarved,
            "no Rule Instance on maintained.toml is under any pressure at all, so the zero "
                + "abandonment beside this proves nothing: a city that never goes short and a city "
                + "that heals both abandon nobody, and only the second is what this file exists to "
                + "show. Either `upkeep` stopped draining or `maintain` is filling faster than it "
                + "can empty -- read the file's header, which predicts both.");
    }
}
