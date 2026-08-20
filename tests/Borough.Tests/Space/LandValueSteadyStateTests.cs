using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 9 task 5 — the bound, and decision 6's reading: <b>at steady state, how many Cells are
/// still moving?</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Decision 6 asked whether the minimum-step-of-one survives a target that moves</b>, on the worry
/// that a non-integral fixed point would leave every Cell flickering by ±1 for ever in saved, hashed
/// state. ⚠ <b>That worry is not what the field does, and the measurement is what says so.</b>
/// <c>adr/0122</c> deleted <c>w₁</c>, so the target is exogenous and the gap reaches exactly zero
/// against a constant one — which <c>LayerFieldsTests.Land_value_converges_on_its_target_from_either_side</c>
/// already pins. What moves the field is that the target is <em>not</em> constant, and the motion is
/// four orders of magnitude larger than the ±1 the decision was about.
/// </para>
/// <para>
/// <b>Measured on <c>rulesets/fouled.toml</c>, 4,000 Citizens, eight Days to settle and four Days
/// observed, 32 cadence samples over 262 resident Cells</b> — <b>185 Cells move on a typical sample</b>
/// (min 0, max 212), <b>50 never move at all</b>, the widest peak-to-trough swing is <b>74,373</b> and
/// the mean is <b>22,863</b>, and the field <b>does not trend</b>: the mean value is −567,787 over the
/// first half of the window and −563,871 over the second. All Q16.16, so the widest swing is about
/// <b>1.13 units against a field whose deepest Cell is about −28</b>.
/// </para>
/// </remarks>
public sealed class LandValueSteadyStateTests
{
    private const int Citizens = 4_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(0xF0U);

    private static Ruleset Load()
    {
        string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", "fouled.toml");
        RulesetLoadResult result = RulesetLoader.Load(path);

        return result.Ruleset ?? throw new InvalidOperationException(result.Describe());
    }

    /// <summary>
    /// <b>The field oscillates, it does not trend, and the oscillation is the Day rather than the
    /// dead band.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The distinguishing quantity is the SIZE of the motion, not its presence.</b> A minimum
    /// step of one moves a Cell by <b>one raw unit</b> — 1/65,536 of a land value point. The observed
    /// swing at the widest Cell is <b>74,373</b> raw units, so whatever is moving the field, it is not
    /// the dead-band repair: it is the target itself, moving with the Day because the noise term is
    /// instantaneous (see <c>FouledRulesetTests</c>, and the noise term is zero at midnight).
    /// </para>
    /// <para>
    /// <b>What this test is for is the trend, which is <c>adr/0006</c>'s actual requirement.</b> Land
    /// value is the awkward magnitude: it is <em>supposed</em> to move, so flatness is asserted on the
    /// mean over a window rather than on any Cell's value. ***A quantity that oscillates is flat when
    /// its average over a period is; a quantity that trends is not, however small each step was.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_field_oscillates_within_a_bound_and_does_not_trend()
    {
        World world = new(Citizens, Load(), Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int settle = 8 * (int)Ticks.PerDay;
        int window = 4 * (int)Ticks.PerDay;
        Dictionary<int, int> low = [];
        Dictionary<int, int> high = [];
        long earlySum = 0;
        long lateSum = 0;
        int earlyCount = 0;
        int lateCount = 0;

        for (int tick = 0; tick < settle + window; tick++)
        {
            simulation.Step(default);

            if (tick <= settle || (tick % 256) != 16)
            {
                continue;
            }

            for (int slot = 0; slot < world.Layers.Cells.Rows.SlotCount; slot++)
            {
                if (!world.Layers.Cells.Rows.IsLive(slot))
                {
                    continue;
                }

                int value = world.Layers.Cells.LandValue[slot];

                low[slot] = low.TryGetValue(slot, out int lo) ? Math.Min(lo, value) : value;
                high[slot] = high.TryGetValue(slot, out int hi) ? Math.Max(hi, value) : value;

                if (tick < settle + (window / 2))
                {
                    earlySum += value;
                    earlyCount++;
                }
                else
                {
                    lateSum += value;
                    lateCount++;
                }
            }
        }

        int widest = 0;
        int moving = 0;

        foreach (int slot in low.Keys)
        {
            int swing = high[slot] - low[slot];

            widest = Math.Max(widest, swing);

            if (swing > 0)
            {
                moving++;
            }
        }

        // It moves, and by far more than a dead-band repair could move it.
        Assert.True(moving > 0, "nothing moved at all, so the target has stopped moving too");
        Assert.True(
            widest > 1_000,
            $"the widest swing is {widest} raw units. Under about a thousand this would be the "
            + "minimum-step-of-one flutter decision 6 was about rather than the Day, and the "
            + "conclusion recorded on this class would need retaking");

        // And it does not trend. The two halves of the window average within 5% of each other.
        long early = earlySum / earlyCount;
        long late = lateSum / lateCount;
        long drift = Math.Abs(late - early);

        Assert.True(
            drift * 20 <= Math.Abs(early),
            $"the mean land value went from {early} to {late} across one four-Day window, a drift of "
            + $"{drift}. adr/0006: no magnitude trends upward at steady state, and land value is the "
            + "one that is allowed to move -- so the flatness is asserted on the average and this is "
            + "the average failing");
    }

    /// <summary>
    /// <b>The composition survives a Cell holding the most pollution the invariant permits.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It did not, and this test is the reason the arithmetic in <c>MapLayers.Desirability</c>
    /// changed.</b> <c>Invariant.LayerMagnitudeIsBounded</c> bounds a Cell's source at
    /// <c>SeparableKernel.SourceCeiling</c>, which is about <b>327,000</b> at the shipped radius. The
    /// composition then lifted that count into Q16.16 with <c>Fixed.FromInt</c>, which is
    /// <c>checked</c> and throws above <b>32,767</b>. ***So there was a factor of ten between what the
    /// invariant called a legal world and what the reader of that world could represent***, and the
    /// symptom would have been an <c>OverflowException</c> raised wherever somebody happened to read a
    /// Cell.
    /// </para>
    /// <para>
    /// <b>The repair is a conversion removed rather than a type widened.</b> Pollution is a count and
    /// the weight is a ratio, so their product is already Q16.16 and the count never needed lifting.
    /// <c>Fixed.Mul</c>'s own remark — <em>the fix is a range assertion at the call site, not a wider
    /// type</em> — is right, and the call site's defect was the conversion.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_composition_survives_the_most_pollution_the_invariant_permits()
    {
        RoadGraph graph = new(RoadFixtures.Roads(blockTiles: 32, arterials: 0));
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(32);
        Cells north = new(32);

        // ⚠ A REGION AT THE CEILING, NOT ONE CELL, and the difference is the whole reachability
        // argument. The kernel is normalised, so one Cell at the ceiling contributes only
        // source/gain -- about 4,041 -- to its own read value, which is comfortably representable.
        // The invariant permits EVERY Cell to hold the ceiling, and a full kernel support of them
        // sums back to the ceiling itself. ***A bound stated per Cell is not a bound on what a Cell
        // reads***, because a diffused field is a sum over its neighbours.
        int radius = layers.PollutionKernel.Radius.Raw;

        for (int up = -radius; up <= radius; up++)
        {
            for (int across = -radius; across <= radius; across++)
            {
                layers.EmitPollution(
                    new Cells(east.Raw + across),
                    new Cells(north.Raw + up),
                    layers.PollutionKernel.SourceCeiling);
            }
        }

        layers.Step(Ticks.Zero, graph);

        int composed = layers.Desirability(
            graph,
            DesirabilityWeights.Default,
            CellGrid.ToTiles(east),
            CellGrid.ToTiles(north));

        Assert.True(
            layers.Pollution(east, north) > short.MaxValue,
            $"the Cell reads {layers.Pollution(east, north)}, under the 32,767 that Fixed.FromInt "
            + "can lift -- so this world no longer reaches the case and the test has stopped biting");
        Assert.True(composed < 0, $"the filthiest legal Cell composed to {composed}");
    }
}
