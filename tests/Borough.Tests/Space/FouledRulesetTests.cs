using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// <c>rulesets/fouled.toml</c> — the ninth Ruleset, and the first world in which land value is
/// anything but zero.
/// </summary>
/// <remarks>
/// <para>
/// <b>This file exists because milestone 9 task 4 shipped a producer that produced nothing.</b> The
/// only thing in the build that creates a Cell row is a pollution emission, and no shipped Ruleset
/// emitted any — so the land value column was zero in every world that existed, the mechanism was
/// unobservable, and decision 5's floor named a world that could not supply its own readings.
/// </para>
/// <para>
/// ⚠ <b>So these are not tests of the Ruleset's numbers.</b> They are the check that the world can
/// produce a reading at all, which is the thing <c>adr/0052</c>'s checklist does not ask for and which
/// this milestone has now been caught by twice. Writing the file without them would be the same
/// mistake a third time.
/// </para>
/// </remarks>
public sealed class FouledRulesetTests
{
    private const int Citizens = 4_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(0xF0U);

    private static string Path =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", "fouled.toml");

    private static Ruleset Load()
    {
        RulesetLoadResult result = RulesetLoader.Load(Path);

        return result.Ruleset
            ?? throw new InvalidOperationException($"fouled.toml was refused:\n{result.Describe()}");
    }

    private static World Run(int ticks)
    {
        World world = new(Citizens, Load(), Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

    /// <summary>
    /// <b>Some Cell has a row, which is the whole difference between this Ruleset and the other
    /// eight.</b>
    /// </summary>
    [Fact]
    public void Something_emits_so_the_cell_table_is_not_empty()
    {
        World world = Run(2_048);

        Assert.True(
            world.Layers.Cells.Rows.LiveCount > 0,
            "no Cell has a row, so nothing emitted and the whole file failed at its one job");
    }

    /// <summary>
    /// <b>Land value is negative somewhere, and it VARIES.</b> Decision 5's floor reading 1: a weight
    /// small enough rounds every Cell onto the same value, and a uniform field is visibly working
    /// while carrying no information.
    /// </summary>
    [Fact]
    public void The_field_is_non_zero_and_varies_across_cells()
    {
        World world = Run(2_048);
        HashSet<int> distinct = [];
        int lowest = 0;

        for (int slot = 0; slot < world.Layers.Cells.Rows.SlotCount; slot++)
        {
            if (!world.Layers.Cells.Rows.IsLive(slot))
            {
                continue;
            }

            int value = world.Layers.Cells.LandValue[slot];

            distinct.Add(value);
            lowest = Math.Min(lowest, value);
        }

        Assert.True(lowest < 0, $"nothing is worth less than nothing; the lowest Cell is {lowest}");
        Assert.True(
            distinct.Count > 1,
            $"every Cell holds the same land value ({distinct.Count} distinct), so the field is "
            + "uniform and carries no information even though it is visibly working");
    }

    /// <summary>
    /// <b>Both terms are visible.</b> Decision 5's floor reading 2, and
    /// <c>adr/0123</c>'s concern arriving as a number instead of as an absence: if one term is
    /// negligible beside the other everywhere, this is a one-term field wearing a two-term formula.
    /// </summary>
    [Fact]
    public void Both_terms_of_the_composition_are_visible_in_this_world()
    {
        // ⚠ 08:00, NOT MIDNIGHT, AND THE HOUR IS THE POINT. A Segment's volume is the count of
        // Vehicles on it AT THIS INSTANT, so the noise term is instantaneous while land value is the
        // only thing in the composition that has memory. Probing at Tick 2048 -- midnight, Tick 0 of
        // day two -- reads a city where nobody is driving and reports the noise term as zero
        // everywhere, which is true of that instant and false of the world. Shift starts are 6 to 10
        // (`shift_start_earliest_hour`), and 2048 Ticks is a Day, so this lands at about 07:00.
        World world = Run(681);
        DesirabilityWeights weights = world.Rules.Layers.Desirability;
        int pollutionOnly = 0;
        int noiseOnly = 0;

        for (int slot = 0; slot < world.Layers.Cells.Rows.SlotCount; slot++)
        {
            if (!world.Layers.Cells.Rows.IsLive(slot))
            {
                continue;
            }

            Cells east = world.Layers.Cells.East[slot];
            Cells north = world.Layers.Cells.North[slot];

            pollutionOnly = Math.Max(
                pollutionOnly,
                -world.Layers.CellDesirability(
                    world.Roads, weights with { Noise = 0 }, east, north));
            noiseOnly = Math.Max(
                noiseOnly,
                -world.Layers.CellDesirability(
                    world.Roads, weights with { Pollution = 0 }, east, north));
        }

        Assert.True(pollutionOnly > 0, "the pollution term is zero everywhere");
        Assert.True(noiseOnly > 0, "the noise term is zero everywhere");

        // Within two orders of magnitude of each other. Neither term is carrying the field alone.
        Assert.True(
            pollutionOnly < noiseOnly * 100 && noiseOnly < pollutionOnly * 100,
            $"pollution peaks at {pollutionOnly} and noise at {noiseOnly}: one of them is doing all "
            + "the work and the composition is a one-term field wearing a two-term formula");
    }

    /// <summary>
    /// <b>The noise term is zero at midnight, in a city where every Household owns a car.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This is a property of the composition and not of the Ruleset, and it was found by this
    /// world rather than reasoned to.</b> A Segment's volume is the count of Vehicles on it <em>at
    /// that instant</em>, so the noise term is instantaneous — while land value is the only part of
    /// the composition that has memory. At Tick 2048, midnight at the top of day two, every Segment in
    /// a 100%-car-ownership city is empty and desirability is a one-term field.
    /// </para>
    /// <para>
    /// <b>So the land value target oscillates with the Day, at the Day's own period</b>, and what a
    /// Cell's land value settles on depends on where the 256-Tick cadence lands against the commute
    /// peak. That is milestone 9's <b>decision 6</b> — <em>does the minimum step of one survive a
    /// moving target?</em> — arriving as a measurement before the sitting: ***the target does not stop
    /// moving, so the question is not whether the lag can rest but what it rests around.***
    /// </para>
    /// <para>
    /// It is pinned rather than merely recorded because the day it stops being true, something has
    /// given the noise term memory, and that is a change to what desirability means.
    /// </para>
    /// </remarks>
    [Fact]
    public void Noise_is_zero_at_midnight_and_that_is_the_composition_not_the_world()
    {
        World midnight = Run(2 * (int)Ticks.PerDay);
        World morning = Run(681);
        DesirabilityWeights weights = midnight.Rules.Layers.Desirability;

        Assert.Equal(0, Loudest(midnight, weights));
        Assert.True(
            Loudest(morning, weights) > 0,
            "and the same city at about 08:00 is not silent, so the zero above is the hour");
    }

    private static int Loudest(World world, DesirabilityWeights weights)
    {
        int loudest = 0;

        for (int slot = 0; slot < world.Layers.Cells.Rows.SlotCount; slot++)
        {
            if (!world.Layers.Cells.Rows.IsLive(slot))
            {
                continue;
            }

            loudest = Math.Max(
                loudest,
                -world.Layers.CellDesirability(
                    world.Roads,
                    weights with { Pollution = 0 },
                    world.Layers.Cells.East[slot],
                    world.Layers.Cells.North[slot]));
        }

        return loudest;
    }
}
