using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 27's <c>adr/0006</c> obligation: the Business table does not grow with elapsed time.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The collection is NAMED HERE AND NOT DISCOVERED BY THE RUN</b>, which is <c>plans/0040</c>
/// <b>F44</b>'s correction arriving one milestone later: that milestone's plan named the visible new
/// table and missed the one that was a change in <em>lifetime</em>, and the closing task had to be
/// re-aimed. Milestone 27 introduces <b>three</b> and this file asserts all three:
/// </para>
/// <list type="number">
/// <item><b>The <c>business</c> table itself.</b> Two sources — <c>adr/0148</c>'s instantiation with
/// the premises and <c>adr/0145</c>'s founding by a Household — against two sinks:
/// <c>World.Raze</c> with the premises, and <c>World.Depart</c> at the give-up bound.</item>
/// <item><b>The <c>unpremised</c> pool.</b> An inflow from founding and from a demolition that pools
/// somebody else's tenant, and two outflows: placement, and the bound.</item>
/// <item><b>Every Business's Bins.</b> Created with the row and freed with it, so the <c>bin</c>
/// table's high-water mark is the one that says the freeing happens.</item>
/// </list>
/// <para>
/// ⚠ <b>THE SLOT COUNTS ARE THE EVIDENCE AND THE LIVE COUNTS ARE NOT</b> (<c>plans/0040</c>
/// <b>F45</b>). A live count oscillates and can sit still while the allocator creeps underneath it; a
/// slot count is a high-water mark, so ***a flat slot count under continuous churn is rows being
/// recycled***, which is the claim <c>adr/0006</c> actually makes.
/// </para>
/// <para>
/// 🔴 <b>What bounds this city is the SOURCE EXHAUSTING and not a sink firing, and that is a finding
/// rather than a pass.</b> The give-up bound never fires here — placement re-premises a pooled
/// Business long before 30 Days elapse — so what stops the shop count climbing is that founding is a
/// <em>means test</em> that runs out of means: every founding moves money from a Household into a
/// Business and employs a Citizen, so each one makes the next less likely.
/// ***A bound that rests on a source drying up reopens the day anything refills it***, which is
/// milestone 11's gate and milestone 26's revenue. Recorded in <c>plans/0041</c>.
/// </para>
/// </remarks>
public sealed class BusinessLongRunTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out = output;

    private const int TickCount = 131_072;
    private const int Population = 1_000;
    private const int ReadEvery = 2_048;

    /// <summary>Readings discarded as the transient: the city opens fully built.</summary>
    private const int SettleReadings = 8;

    [Fact]
    public void The_business_table_does_not_grow_with_elapsed_time()
    {
        Reading[] readings = Run(out World world);
        Reading[] tail = readings[SettleReadings..];

        _out.WriteLine(
            $"live {tail[0].Live} -> {tail[^1].Live}, slots {tail[0].Slots} -> {tail[^1].Slots}, "
            + $"pool {tail[0].Pool} -> {tail[^1].Pool}, bins {tail[0].BinSlots} -> {tail[^1].BinSlots}, "
            + $"founded {world.Businesses.Rows.LiveCount} live at the end");

        // Vacuity, and it is not a formality: every assertion below is satisfied perfectly by a world
        // in which no Business was ever created or destroyed.
        Assert.True(tail[0].Live > 0, "no Business ever existed, so there is no collection to bound.");

        Assert.True(
            tail[^1].Pool > 0,
            "nothing ever reached the unpremised pool, so the second collection was never exercised.");

        // The claim, and it is stated as a DECELERATION rather than as a ceiling. The shop count is
        // still rising at the end of this run and saying otherwise would be asserting a plateau the
        // data does not show; what adr/0006 forbids is growth WITH ELAPSED TIME, and a series whose
        // second half grows a small fraction of what its first half grew is not that.
        int half = tail.Length / 2;

        long early = tail[half].Slots - tail[0].Slots;
        long late = tail[^1].Slots - tail[half].Slots;

        Assert.True(
            early > 0,
            $"the business table gained no slots at all over the first half of the tail ({early}), so "
            + "the comparison below is against zero and says nothing. Either the city stopped "
            + "founding before the transient ended or the settle window is now too long.");

        Assert.True(
            late * 4 <= early,
            $"the business table gained {early} slots over the first half of the tail and {late} over "
            + "the second. adr/0006: nothing grows with elapsed time. This city has no sink that "
            + "fires -- the give-up bound never does, because placement re-premises a pooled Business "
            + "first -- so what bounds it is founding running out of Households that can afford one. "
            + "A second half growing as fast as the first means that source has stopped drying up.");

        // The Bins go with the rows. A Business opens a balance Bin at creation and DestroyBusiness
        // frees it, so a bin table that grows while the business table does not is the freeing not
        // happening -- which no business-table assertion above could see.
        long bins = tail[^1].BinSlots - tail[half].BinSlots;
        long earlyBins = tail[half].BinSlots - tail[0].BinSlots;

        Assert.True(
            bins <= earlyBins + (earlyBins / 2) + 1,
            $"the bin table gained {earlyBins} slots over the first half of the tail and {bins} over "
            + "the second. Every Business opens a balance Bin and DestroyBusiness frees it, so a bin "
            + "high-water mark outrunning the business one is a Bin that is not being freed.");
    }

    /// <summary>
    /// ⚠ <b>No Business in the pool holds nothing, over a run long enough for the defect to show.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>FoundingTests</c>' regression at length.</b> An instantiated Business opens at a zero
    /// balance and comes down with its premises (<c>adr/0148</c>), so ***a pooled Business holding
    /// nothing is one that outlived them*** — and before <c>BusinessTable.Origin</c> landed there were
    /// 52 of them here, immortal, because the give-up bound that would have collected them never
    /// fires. **The short test proves the mechanism; this one proves it does not leak slowly.**
    /// </remarks>
    [Fact]
    public void No_business_outlives_the_premises_that_instantiated_it()
    {
        _ = Run(out World world);

        int pooled = 0;
        int broke = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot) || !world.Businesses.IsUnpremised(slot))
            {
                continue;
            }

            pooled++;

            if (world.BalanceOf(world.Businesses.Rows.At(slot)).Raw == 0)
            {
                broke++;
            }
        }

        _out.WriteLine($"pooled={pooled} holding nothing={broke}");

        Assert.True(pooled > 0, "the pool is empty, so this asserts nothing.");

        Assert.True(
            broke == 0,
            $"{broke} of {pooled} pooled Businesses hold nothing after {TickCount:N0} Ticks. Only an "
            + "instantiated trade opens at zero and adr/0148 says it comes down with its premises, so "
            + "each of these outlived them -- and nothing will ever collect one, because the give-up "
            + "bound does not fire in this world.");
    }

    private readonly record struct Reading(int Live, int Slots, int Pool, int BinSlots);

    private static Reading[] Run(out World world)
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "levied.toml"));

        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(0x1E71EDU);

        world = new World(Population, loaded.Ruleset!, key);

        var simulation = new Simulation(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken). PlacementLongRunTests'
            // line and its reason.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        List<Reading> readings = [];

        for (int tick = 0; tick < TickCount; tick++)
        {
            simulation.Step(TickInput.Empty);

            if ((tick + 1) % ReadEvery == 0)
            {
                readings.Add(new Reading(
                    world.Businesses.Rows.LiveCount,
                    world.Businesses.Rows.SlotCount,
                    world.UnpremisedPool.Count,
                    world.Bins.Rows.SlotCount));
            }
        }

        return [.. readings];
    }
}
