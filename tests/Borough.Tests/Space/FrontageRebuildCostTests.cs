using System.Diagnostics;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// What rebuilding every Lot's frontage costs, and what share of a whole rebuild it is.
/// <b>An instrument.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>It exists as a BASELINE and not as a budget.</b>
/// [`plans/0052`](../../../plans/0052-the-parcel.md) proposes a parcel derived the way frontage is,
/// rebuilt in the same pass, and names the rebuild cost as <b>Q3</b> — <em>"the one number in this
/// plan that would change the design if it were wrong"</em>, unmeasured on both sides. ***A figure
/// taken after the change, with nothing to compare it against, cannot say whether the cost is
/// new.*** This is the before.
/// </para>
/// <para>
/// ⚠ <b>It is NOT a Tick cost and does not belong in <c>plans/0013</c>.</b> The rebuild runs on the
/// Epoch — at world creation, on a load, and after a road edit — never inside <c>step()</c>. What it
/// prices is how long a world takes to become usable again, which is a different budget with no
/// ceiling written down. <c>adr/0078</c> names <em>"frontage becoming expensive to rebuild"</em> as
/// its own revisit trigger and nothing had ever asked.
/// </para>
/// <para>
/// ⚠ <b>It asserts nothing about the clock and cannot fail on a noisy machine.</b> The figure names
/// the reference machine or it is not a figure (<c>adr/0106</c>), and a runner may report that this
/// broke but may never supply a number a document quotes (<c>adr/0121</c>). Its two assertions are
/// that there were Lots to rebuild and that the rebuild reached them — the pair that would be
/// silently zero if the fixture stopped populating.
/// </para>
/// <para>
/// ⚠ <b>The share column is the point, not the milliseconds.</b> <c>World.RebuildDerived</c> clears
/// thirty-odd columns and walks five tables besides frontage, so frontage being a small fraction of
/// it is what says a second derived quantity of the same shape is affordable. **Read the share.**
/// </para>
/// <para>
/// 🔴 <b>THE WHOLE REBUILD IS O(CAPACITY) AND NOT O(LIVE), AND THE READING SAYS SO.</b> The first
/// run went <b>1.935ms at 132 Lots</b> to <b>2.228ms at 2,184</b> — a sixteenfold rise in the work
/// against a **15%** rise in the time. It is the <c>Span.Clear()</c> block at the top of
/// <c>RebuildDerived</c>, which clears whole columns whatever fraction of them is occupied.
/// ***So the marginal cost of one more derived quantity is its clear, not its walk*** — and a
/// parcel's clear is the same shape as the three <c>LotTable</c> columns already there.
/// </para>
/// <para>
/// ⚠ <b>The same effect makes the small readings misleading in the other column.</b>
/// <c>Frontage.Rebuild</c> opens with <c>Array.Clear(_claimed)</c>, which is O(Segment slots) rather
/// than O(Lots), so at 1,000 Citizens the figure is mostly that clear. **Read the per-Lot marginal
/// from the large rows and never from the small ones** — this is <c>plans/0012</c> Cause 5's shape
/// arriving inside one table: the digits are fine and what they measure changes down the column.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class FrontageRebuildCostTests(ITestOutputHelper output)
{
    /// <summary>Sizes, chosen to span two orders of magnitude so the scaling is readable.</summary>
    private static readonly int[] Sizes = [1_000, 4_000, 16_000, 64_000];

    /// <summary>Repeats per reading. The pass is short enough that one call is mostly clock.</summary>
    private const int Repeats = 32;

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    /// <summary>How many Lots came out of the pass with an Address.</summary>
    private static int WithFrontage(World world)
    {
        int found = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.HasFrontage(slot))
            {
                found++;
            }
        }

        return found;
    }

    [Fact]
    public void What_a_frontage_rebuild_costs_today()
    {
        Ruleset ruleset = Load("minimal.toml");

        output.WriteLine("Frontage.Rebuild against World.RebuildDerived, rulesets/minimal.toml.");
        output.WriteLine($"{Repeats} repeats per reading, best of, Release.");
        output.WriteLine(string.Empty);
        output.WriteLine("citizens     lots  fronted   frontage    whole    share");
        output.WriteLine("-----------------------------------------------------------");

        foreach (int citizens in Sizes)
        {
            WorldKey key = WorldKey.FromSeed(0x5EA1U);
            World world = new(citizens, ruleset, key);
            SyntheticCity.PopulateInto(world, key, Ticks.Zero);

            int lots = world.Lots.Rows.LiveCount;
            int fronted = WithFrontage(world);

            // Warmed before either clock, because the first call through a cold method is measuring
            // the JIT and not the pass. Both are warmed, not just the one being read first.
            world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
            world.RebuildDerived();

            long frontage = long.MaxValue;
            long whole = long.MaxValue;

            for (int i = 0; i < Repeats; i++)
            {
                long before = Stopwatch.GetTimestamp();
                world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
                long took = Stopwatch.GetTimestamp() - before;

                frontage = took < frontage ? took : frontage;
            }

            for (int i = 0; i < Repeats; i++)
            {
                long before = Stopwatch.GetTimestamp();
                world.RebuildDerived();
                long took = Stopwatch.GetTimestamp() - before;

                whole = took < whole ? took : whole;
            }

            double frontageMs = frontage * 1000.0 / Stopwatch.Frequency;
            double wholeMs = whole * 1000.0 / Stopwatch.Frequency;
            double share = wholeMs > 0 ? frontageMs * 100.0 / wholeMs : 0;

            output.WriteLine(
                $"{citizens,8}  {lots,7}  {fronted,7}  {frontageMs,8:F3}ms {wholeMs,7:F3}ms "
                + $"{share,6:F1}%");

            // The pair that would be silently zero if the fixture stopped populating, which is the
            // failure this instrument could otherwise report as a very fast rebuild.
            Assert.True(lots > 0, $"{citizens} Citizens carved no Lots, so nothing was timed.");
            Assert.True(
                fronted > 0,
                $"{lots} Lots and none has frontage, so Rebuild reached nothing.");
        }

        output.WriteLine(string.Empty);
        output.WriteLine("Best-of rather than mean: the pass is deterministic and does no I/O, so a");
        output.WriteLine("slow reading is the machine and not the work. adr/0106 -- this names no");
        output.WriteLine("machine, so nothing here may be quoted until it is taken on the reference");
        output.WriteLine("one and the machine is written down beside it.");
    }
}
