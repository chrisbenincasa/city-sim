using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/thinned.toml</c> — the first threshold, run <b>as a file and at city scale</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Found by <c>plans/0050</c>'s demonstration census: nothing executed this file.</b>
/// <c>OccupancySheddingTests</c> covers the mechanism thoroughly and covers it on a <em>hand-built
/// one-house fixture</em> — one Building, four Households, a Zone Rule that judges and never builds.
/// ⚠ <b>What that cannot ask is whether the loop still closes in a world where placement refills what
/// shedding empties</b>, and that is the only thing this file was written to show.
/// </para>
/// <para>
/// <b>The A/B is the whole test, because the file is a three-line diff.</b>
/// <c>declining.toml</c> plus <c>sheds_occupant_after_days</c>, plus the headroom that lets a Building
/// empty before it is condemned, plus <c>upkeep</c>'s <c>apply</c> becoming
/// <c>{ derived = "occupancy" }</c> — and its header says the third line is the one nobody would
/// guess: ***without it, shedding does not lower the demand that caused the shedding***, and the
/// Building is condemned anyway, later and emptier.
/// </para>
/// <para>
/// ⚠ <b>Zero abandoned is not enough on its own, for
/// <see cref="MaintainedRulesetTests"/>'s reason one file along.</b> A city that never goes short
/// abandons nobody too. The second assertion is what tells them apart: on this Ruleset the Unplaced
/// Pool has exactly one door — a Household losing its home — so a Pool that fills while <b>nothing is
/// abandoned</b> is a Building that shed an Occupant and stayed standing, and nothing else in the
/// build can produce it.
/// </para>
/// <para>
/// ⚠ <b>1,000 Citizens over 8,192 Ticks, the smallest row of the census in the file's own header.</b>
/// This is an assertion rather than an instrument (<c>plans/0032</c>) — it asks whether the mechanism
/// still works, not what the published figures are.
/// </para>
/// </remarks>
public sealed class ThinnedRulesetTests
{
    private const int Citizens = 1_000;
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

    /// <summary>Runs a world and reports what it lost, and how many were looking for a home.</summary>
    /// <remarks>
    /// ⚠ <b>The Pool is sampled every Tick and the PEAK is kept.</b> Placement rehouses a shed
    /// Household within a cycle or two, so an end-of-run snapshot reads zero on a world that shed
    /// hundreds — which is the reading that would make this test pass for the wrong reason.
    /// </remarks>
    private static (int Abandoned, int PoolPeak) Run(string file)
    {
        World world = new(Citizens, Load(file), Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Core.Quantities.Ticks.Zero);

        int peak = 0;

        for (int tick = 0; tick < Ticks_; tick++)
        {
            simulation.Step(default);

            if (world.UnplacedPool.Count > peak)
            {
                peak = world.UnplacedPool.Count;
            }
        }

        int abandoned = 0;

        for (int slot = 0; slot < world.Buildings.Rows.Capacity; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.IsAbandoned(slot))
            {
                abandoned++;
            }
        }

        return (abandoned, peak);
    }

    /// <summary>
    /// <b>The first threshold corrects where the second only kills, and it holds at city scale.</b>
    /// </summary>
    [Fact]
    public void The_thinned_city_keeps_its_stock_where_the_declining_one_loses_it()
    {
        (int declining, _) = Run("declining.toml");
        (int thinned, _) = Run("thinned.toml");

        Assert.True(
            declining > 0,
            $"declining.toml abandoned {declining} Buildings over {Ticks_} Ticks at {Citizens} "
                + "Citizens. It is the control arm and it is supposed to decay -- a zero here means "
                + "the comparison below proves nothing, and the defect is in decline rather than in "
                + "thinned.toml.");

        Assert.True(
            thinned == 0,
            $"thinned.toml abandoned {thinned} Buildings against declining.toml's {declining} on the "
                + "same seed. Its claim is that shedding an Occupant LOWERS the demand that caused "
                + "the shedding, so the Building stops failing rather than dying slower. Check "
                + "`upkeep`'s apply first: with a fixed band instead of { derived = \"occupancy\" } "
                + "the demand does not move when somebody leaves and this file becomes "
                + "declining.toml with extra steps, which is what its header predicts.");
    }

    /// <summary>
    /// <b>Households really do lose their homes, so the zero above is the loop and not an idle city.</b>
    /// </summary>
    [Fact]
    public void The_thinned_city_sheds_into_a_pool_that_nothing_else_could_have_filled()
    {
        (int abandoned, int peak) = Run("thinned.toml");

        Assert.True(
            peak > 1,
            $"the Unplaced Pool peaked at {peak} over {Ticks_} Ticks, with {abandoned} Buildings "
                + "abandoned. Nothing was ever shed, so "
                + "the zero abandonment beside this proves nothing: a city that never goes short and "
                + "a city whose dwellings thin out and recover both keep all their stock, and only "
                + "the second is what this file exists to show. This Ruleset states no gate and no "
                + "[[life_stage]], so losing a home is the Pool's only door -- read the header, "
                + "which predicts hundreds of sheds at this size.");
    }
}
