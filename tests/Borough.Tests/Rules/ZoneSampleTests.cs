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
    /// <c>02 §5.3</c>'s criticism of UrbanSim: sampling with replacement double-counts an
    /// alternative, so a repeated draw is discarded rather than evaluated twice.
    /// </summary>
    [Fact]
    public void One_trigger_never_draws_a_lot_twice()
    {
        LotTable lots = Lots(40);

        // A sample close to the population size, which is where collisions are common enough that a
        // sampler with replacement would show them almost every trigger.
        for (ulong tick = 1; tick <= 64; tick++)
        {
            int[] drawn = Sample(lots, 32, tick);

            Assert.Equal(drawn.Length, drawn.Distinct().Count());
        }
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
    /// A sample larger than the live population returns the population, not a hang.
    /// </summary>
    [Fact]
    public void A_sample_wider_than_the_city_is_bounded_by_the_city()
    {
        LotTable lots = Lots(3);
        int[] drawn = Sample(lots, 64, tick: 8);

        Assert.True(drawn.Length <= 3);
        Assert.Equal(drawn.Length, drawn.Distinct().Count());
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
