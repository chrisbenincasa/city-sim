using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 10 task 4: which Lots a Zone Rule looks at on one trigger.
/// </summary>
/// <remarks>
/// <b>The properties here are the ones a wrong sampler would still pass a smoke test with.</b> A
/// sampler that returned the same Lot every time, or the first <c>k</c> slots, or a set that drifted
/// with wall-clock, all produce plausible-looking growth; what separates them is distinctness,
/// reproducibility across runs, and movement across Ticks.
/// </remarks>
public sealed class ZoneSampleTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0xB0A0_1234_5678_9AB0UL);

    private static LotTable Lots(int count)
    {
        var lots = new LotTable(count);

        for (int i = 0; i < count; i++)
        {
            lots.Create(new Tiles(i), new Tiles(0), zone: 1);
        }

        return lots;
    }

    private static int[] Sample(LotTable lots, int size, ulong tick, int rule = 0)
    {
        int[] into = new int[size];
        int found = ZoneSample.Draw(lots, into, Key, new Ticks(tick), rule);

        return into[..found];
    }

    [Fact]
    public void The_same_trigger_draws_the_same_lots()
    {
        LotTable lots = Lots(500);

        Assert.Equal(Sample(lots, 8, tick: 64), Sample(lots, 8, tick: 64));
    }

    /// <summary>
    /// The duplicate rate at the shipped revisit period, which is the measurement task 11c wanted
    /// before deleting the scan that used to prevent one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test is a number rather than a property, and the number is what settled a choice.</b>
    /// <c>ZoneSample.Draw</c> used to scan what it had drawn so far and discard a repeat, which is
    /// <c>O(sample²)</c> and was justified by <em>a sample is a handful of Lots</em>. <c>adr/0059</c>
    /// makes the sample proportional to the map, so the premise died and the two ways out — a stamp
    /// array per Lot, or accepting duplicates — wanted measuring rather than arguing.
    /// </para>
    /// <para>
    /// <b>It is scale-free, which is why one measurement is enough.</b> The duplicate rate depends on
    /// <c>sample ÷ lots</c>, and <c>adr/0059</c> makes that ratio exactly
    /// <c>interval ÷ revisit_ticks</c> — a property of the file, not of the city. At the shipped
    /// <c>32 ÷ 8192</c> it is 1 in 256, and the expected duplicate fraction is half of that whatever
    /// the city size. <b>The bound is deliberately loose against a measured ~0.2%</b>: this exists to
    /// catch the rate becoming a real cost, not to pin an arithmetic identity nothing can move.
    /// </para>
    /// </remarks>
    [Fact]
    public void Duplicates_are_negligible_at_the_shipped_revisit_period()
    {
        const int Count = 120_000;
        const int Size = Count * 32 / 8192;   // ZoneRuleDefinition.SampleFor, at the shipped numbers

        LotTable lots = Lots(Count);
        int drawn = 0;
        int repeats = 0;

        for (ulong tick = 1; tick <= 64; tick++)
        {
            int[] sample = Sample(lots, Size, tick);

            drawn += sample.Length;
            repeats += sample.Length - sample.Distinct().Count();
        }

        Assert.True(repeats * 100 < drawn, $"{repeats} repeats in {drawn} draws is above 1%.");
    }

    [Fact]
    public void A_sample_only_ever_names_live_lots()
    {
        LotTable lots = Lots(200);

        for (int slot = 0; slot < 200; slot += 2)
        {
            lots.Rows.Free(lots.Rows.At(slot));
        }

        for (ulong tick = 1; tick <= 32; tick++)
        {
            Assert.All(Sample(lots, 16, tick), slot => Assert.True(lots.Rows.IsLive(slot)));
        }
    }

    /// <summary>
    /// Successive triggers look elsewhere, which is what makes growth reach the whole city.
    /// </summary>
    /// <remarks>
    /// <b>Also what makes <c>02 §4.2</c>'s rotate-the-scan-start mitigation unnecessary here.</b> That
    /// mitigation exists because a fixed scan order privileges the same low-index Lots for the life of
    /// the city. A sample keyed on the Tick has no fixed order to privilege anything.
    /// </remarks>
    [Fact]
    public void Successive_triggers_sample_different_lots()
    {
        LotTable lots = Lots(500);

        Assert.NotEqual(Sample(lots, 8, tick: 64), Sample(lots, 8, tick: 128));
    }

    /// <summary>
    /// Two Zone Rules triggering on one Tick do not shadow each other.
    /// </summary>
    /// <remarks>
    /// Without the Rule index in the draw's coordinates, every Zone Rule in the Ruleset would evaluate
    /// the identical Lots every trigger — so a second Zone Rule could only ever build where the first
    /// had already declined, which is a contention model nobody chose.
    /// </remarks>
    [Fact]
    public void Two_zone_rules_on_one_tick_sample_differently()
    {
        LotTable lots = Lots(500);

        Assert.NotEqual(Sample(lots, 8, tick: 64, rule: 0), Sample(lots, 8, tick: 64, rule: 1));
    }

    /// <summary>
    /// Over many triggers the sample reaches the whole population rather than a favoured corner.
    /// </summary>
    /// <remarks>
    /// <b>A coverage check rather than a distribution test.</b> It cannot prove uniformity and is not
    /// trying to; what it catches is the family of defects that make a sampler concentrate — a modulus
    /// against the wrong bound, a coordinate that does not vary, a mixer dropped by mistake. Any of
    /// those leaves most of the city unvisited, and any of those would otherwise look like a city that
    /// simply grew slowly.
    /// </remarks>
    [Fact]
    public void Many_triggers_reach_every_lot()
    {
        const int Count = 64;
        LotTable lots = Lots(Count);
        var seen = new HashSet<int>();

        for (ulong tick = 1; tick <= 512; tick++)
        {
            seen.UnionWith(Sample(lots, 4, tick));
        }

        Assert.Equal(Count, seen.Count);
    }

    /// <summary>
    /// A sample larger than the live population is bounded by the draws, not by a hang.
    /// </summary>
    /// <remarks>
    /// <b>Bounded by the sample rather than by the city, which is the reverse of what it used to
    /// be.</b> While duplicates were discarded, drawing 64 times against 3 Lots could return at most
    /// 3; with replacement it returns 64, every one of them one of those 3. The loader refuses the
    /// Ruleset that would ask for this — a revisit period below the interval — so it is a property of
    /// the primitive rather than a shape a city reaches.
    /// </remarks>
    [Fact]
    public void A_sample_wider_than_the_city_names_only_the_city()
    {
        LotTable lots = Lots(3);
        int[] drawn = Sample(lots, 64, tick: 8);

        Assert.Equal(64, drawn.Length);
        Assert.All(drawn, slot => Assert.InRange(slot, 0, 2));
    }

    [Fact]
    public void An_empty_world_samples_nothing()
    {
        Assert.Empty(Sample(new LotTable(16), 8, tick: 8));
    }

    /// <summary>
    /// The sample is on the hot path — it runs every trigger for the life of the city.
    /// </summary>
    [Fact]
    public void Sampling_allocates_nothing()
    {
        LotTable lots = Lots(1_000);
        Span<int> into = stackalloc int[16];

        // Once first, so that nothing being measured is first-call JIT or a lazily built table.
        ZoneSample.Draw(lots, into, Key, new Ticks(1), rule: 0);

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (ulong tick = 2; tick <= 1_000; tick++)
        {
            ZoneSample.Draw(lots, into, Key, new Ticks(tick), rule: 0);
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }
}
