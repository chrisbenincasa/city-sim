using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 12 task 6: the Pool price — <c>adr/0135</c>'s damped tâtonnement.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Nothing in the build can draw from a Pool while <c>Scope.Pool</c> throws</b>, so every rate
/// below is put there by hand. That is the same standing the task ships in: on every Ruleset that
/// exists, the consumption bucket is zero for ever, the recompute reads that as *no trades*, and
/// every price sits at the ceiling it opened at. ***The suite is the only evidence this mechanism
/// works, and it will stay that way until task 7 supplies a writer.***
/// </para>
/// <para>
/// <b>The arithmetic is tested on <see cref="MarketRuleset"/> and the cadence on the
/// <see cref="World"/>, and the split is deliberate.</b> <see cref="MarketRuleset.Reprice"/> is a
/// pure function of four numbers, so its properties — the cover ratio, the cap, the two clamps and
/// what a rate of zero means — need no city at all. What the world has to be asked is a different
/// question: whether the Day boundary fires, whether the bucket resets, and whether a new Pool opens
/// at the ceiling.
/// </para>
/// </remarks>
public sealed class PoolPriceTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static readonly ResourceId Sundries = new(1);

    /// <summary>The shipped file's own damping — <c>rulesets/twinned.toml</c>.</summary>
    private static readonly MarketRuleset Shipped = new(DecayPercent: 50, MoveCapPercent: 10);

    /// <summary>A ceiling round enough that every ratio below is exact.</summary>
    private static readonly Money Ceiling = new(1_000);

    // ---- the tâtonnement, as arithmetic ----------------------------------------------------------

    /// <summary>
    /// One Day of cover prices at the ceiling; ten Days price at a tenth of it.
    /// </summary>
    /// <remarks>
    /// <b>The cover ratio IS the mechanism</b>, and this is the assertion that says what it means:
    /// the target is <c>ceiling / cover</c>, where cover is how many Days the standing level lasts at
    /// the standing rate. ⚠ <b>The cap is set to 100 here so the target is reached in one step</b> —
    /// this test is about where the price is heading and the next one is about how fast it may get
    /// there.
    /// </remarks>
    [Theory]
    [InlineData(0, 10, 1_000)]
    [InlineData(10, 10, 1_000)]
    [InlineData(20, 10, 500)]
    [InlineData(100, 10, 100)]
    [InlineData(5, 10, 1_000)]
    public void The_target_is_the_ceiling_over_the_days_of_cover(long level, long rate, long expected)
    {
        var undamped = new MarketRuleset(DecayPercent: 0, MoveCapPercent: 100);

        Assert.Equal(new Money(expected), undamped.Reprice(Ceiling, Ceiling, level, rate));
    }

    /// <summary>A price never leaves the range between nothing and the ceiling.</summary>
    /// <remarks>
    /// <b>The upper clamp is what makes the ceiling a ceiling</b>, and it is reached by taking
    /// <c>max(level, rate)</c> as the denominator rather than by clamping afterwards — cover below
    /// one Day would otherwise price ABOVE what importing costs, which is <c>adr/0050</c>'s runaway
    /// with a bound written next to it.
    /// </remarks>
    [Fact]
    public void A_price_never_leaves_the_range_between_nothing_and_the_ceiling()
    {
        var undamped = new MarketRuleset(DecayPercent: 0, MoveCapPercent: 100);

        Assert.Equal(Ceiling, undamped.Reprice(Ceiling, Ceiling, level: 0, rate: 10));
        Assert.Equal(Ceiling, undamped.Reprice(Ceiling, Ceiling, level: 1, rate: 1_000));

        Money floored = undamped.Reprice(Ceiling, Ceiling, level: 1_000_000, rate: 1);

        Assert.True(floored >= Money.Zero, $"the price went to {floored.Raw}.");
    }

    /// <summary>The cap bounds one Day's step to a share of the ceiling.</summary>
    /// <remarks>
    /// <b>The shipped damping needs ten Days to cross the whole range</b>, which is the property the
    /// number was chosen for: slow enough that somebody watching a price move could say what moved
    /// it. ⚠ <b>The floor is reached and then stayed at</b> — a cap is a bound on the step and never
    /// a bound on the destination.
    /// </remarks>
    [Fact]
    public void The_cap_bounds_one_days_step()
    {
        Money price = Ceiling;

        // A hugely over-supplied Pool: the target is the floor and only the cap is in the way.
        for (int day = 1; day <= 10; day++)
        {
            price = Shipped.Reprice(price, Ceiling, level: 1_000_000, rate: 1);

            Assert.Equal(new Money(1_000 - (day * 100)), price);
        }

        Assert.Equal(Money.Zero, Shipped.Reprice(price, Ceiling, level: 1_000_000, rate: 1));
    }

    /// <summary>A market with no trades in it keeps its price.</summary>
    /// <remarks>
    /// <b>No trades is an absence of information and not evidence of scarcity</b>, and the two are
    /// easy to conflate because both leave the bucket at zero. ***An empty Pool nobody has ever drawn
    /// from says nothing about what the Good is worth***, so the standing price stands.
    /// </remarks>
    [Fact]
    public void A_rate_of_zero_keeps_the_price_rather_than_raising_it()
    {
        var half = new Money(500);

        Assert.Equal(half, Shipped.Reprice(half, Ceiling, level: 0, rate: 0));
        Assert.Equal(half, Shipped.Reprice(half, Ceiling, level: 10_000, rate: 0));
    }

    /// <summary>A Ruleset with no <c>[market]</c> never moves a price.</summary>
    [Fact]
    public void A_ruleset_with_no_market_table_never_moves_a_price()
    {
        var half = new Money(500);

        Assert.Equal(half, MarketRuleset.None.Reprice(half, Ceiling, level: 0, rate: 100));
    }

    /// <summary>The rate is an exponential moving average of the Day's own draw.</summary>
    /// <remarks>
    /// <b>Both arguments and the answer are units per Day</b>, which is the reason the bucket and the
    /// rate are two columns rather than one. ⚠ <b>Zero decay is the Day's own draw exactly</b>, and
    /// it is a legitimate if twitchy market rather than a switched-off mechanism.
    /// </remarks>
    /// <remarks>
    /// 🔴 <b>The last two rows are the ones that caught something.</b> They round rather than floor,
    /// and the version that floored made a Pool drawn at one unit a Day indistinguishable from a Pool
    /// nobody had touched — with the threshold moving whenever the damping was retuned.
    /// <see cref="MarketRuleset.Smooth"/> carries the argument and what it costs.
    /// ***This mechanism's dead zone was found by writing down a number the test could check, not by
    /// reading the expression.***
    /// </remarks>
    [Theory]
    [InlineData(50, 0, 100, 50)]
    [InlineData(50, 100, 0, 50)]
    [InlineData(50, 40, 40, 40)]
    [InlineData(0, 999, 12, 12)]
    [InlineData(90, 100, 0, 90)]
    [InlineData(50, 0, 1, 1)]
    [InlineData(50, 1, 0, 1)]
    public void The_rate_is_a_moving_average_of_the_days_draw(
        int decay, long standing, long consumed, long expected)
    {
        var market = new MarketRuleset(decay, MoveCapPercent: 10);

        Assert.Equal(expected, market.Smooth(standing, consumed));
    }

    // ---- the cadence, on a world -----------------------------------------------------------------

    private static string Body(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

    private static Ruleset Twinned()
    {
        RulesetLoadResult result = RulesetLoader.Parse(Body("twinned.toml"), "twinned.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"twinned.toml was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static int FirstPoolRow(World world)
    {
        for (int slot = 0; slot < world.DistrictPools.Rows.SlotCount; slot++)
        {
            if (world.DistrictPools.Rows.IsLive(slot))
            {
                return slot;
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>
    /// <c>twinned.toml</c>'s two Goods take their ceiling from different edges, and the file's own
    /// numbers are what say so.
    /// </summary>
    /// <remarks>
    /// <b>This is the shipped file held to its own header.</b> That header claims sundries is
    /// cheapest north and repairs cheapest east, so a <c>min</c> that returned the first table would
    /// be right about one Good and wrong about the other. ***A claim a Ruleset comment makes about
    /// its own numbers is a claim a test can hold it to.***
    /// </remarks>
    [Fact]
    public void The_shipped_file_prices_its_two_goods_from_different_edges()
    {
        Ruleset rules = Twinned();

        Assert.Equal(2, rules.Hinterlands.Length);
        Assert.Equal(new Money(100), rules.ImportCeiling(Sundries));
        Assert.Equal(new Money(200), rules.ImportCeiling(new ResourceId(2)));

        // The cheapest sundries is the north table and the cheapest repairs is the east one.
        Assert.True(rules.ImportPrice(0, Sundries) < rules.ImportPrice(1, Sundries));
        Assert.True(rules.ImportPrice(1, new ResourceId(2)) < rules.ImportPrice(0, new ResourceId(2)));
    }

    /// <summary>A Pool opens at the ceiling, which is why the tâtonnement needed no seed.</summary>
    /// <remarks>
    /// <b>A Pool with no local supply in it should cost what importing costs</b>, and a Pool nobody
    /// has traded in has no local supply by construction. ⚠ <b>That is what discharged
    /// <c>adr/0135</c>'s third possible §D row</b> — an initial price — before it was ever written
    /// down. ***The seed is not a choice; it is the answer the mechanism gives when asked before
    /// anything has happened.***
    /// </remarks>
    [Fact]
    public void A_pool_opens_at_the_import_ceiling()
    {
        var world = new World(2_000, Twinned(), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int row = FirstPoolRow(world);

        Assert.NotEqual(Rows.NoSlot, row);

        for (int slot = 0; slot < world.DistrictPools.Rows.SlotCount; slot++)
        {
            if (!world.DistrictPools.Rows.IsLive(slot)) continue;

            int bin = world.Bins.Rows.Resolve(world.DistrictPools.Bin[slot]);

            Assert.Equal(
                world.Rules.ImportCeiling(world.Bins.Resource[bin]),
                world.DistrictPools.Price[slot]);
        }
    }

    /// <summary>The recompute falls on a Day boundary and nowhere else.</summary>
    /// <remarks>
    /// ⚠ <b>Tick 0 is excluded because no Day has elapsed to consume anything in</b> — the first
    /// recompute would read a rate of zero and keep the seeded price, which is the same answer
    /// arrived at more expensively.
    /// </remarks>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2_047, false)]
    [InlineData(2_048, true)]
    [InlineData(4_096, true)]
    public void The_recompute_falls_on_a_day_boundary(ulong tick, bool expected)
    {
        Assert.Equal(expected, Twinned().Market.RepricesOn(new Ticks(tick)));
    }

    /// <summary>The Day's bucket is zeroed and the rate carries.</summary>
    /// <remarks>
    /// <b>This is the only place the two columns can be told apart</b>, and the assertion is that
    /// they are: the bucket goes to nothing and the rate takes on half of what it held, which is
    /// <c>twinned.toml</c>'s own <c>decay_percent</c>. ⚠ <b>Nothing in the build wrote the bucket</b>
    /// — this test did, standing in for task 7.
    /// </remarks>
    [Fact]
    public void The_recompute_zeroes_the_days_bucket_and_carries_the_rate()
    {
        var world = new World(2_000, Twinned(), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int row = FirstPoolRow(world);

        world.DistrictPools.Rate[row] = 0;
        world.DistrictPools.Consumed[row] = 40;

        world.RepriceDistrictPools();

        Assert.Equal(0, world.DistrictPools.Consumed[row]);
        Assert.Equal(20, world.DistrictPools.Rate[row]);

        world.RepriceDistrictPools();

        Assert.Equal(10, world.DistrictPools.Rate[row]);
    }

    /// <summary>An empty Pool that is being drawn from prices at the ceiling and stays there.</summary>
    /// <remarks>
    /// <b>The ceiling is what keeps <c>adr/0045</c>'s ladder monotone.</b> A Pool that cannot meet
    /// its own demand is the case where a local price would run away, and this is the assertion that
    /// it does not — importing is always available at the anchor, so nothing inside the city can ever
    /// cost more than that.
    /// </remarks>
    [Fact]
    public void A_pool_that_cannot_meet_its_demand_stays_at_the_ceiling()
    {
        var world = new World(2_000, Twinned(), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int row = FirstPoolRow(world);
        int bin = world.Bins.Rows.Resolve(world.DistrictPools.Bin[row]);
        Money ceiling = world.Rules.ImportCeiling(world.Bins.Resource[bin]);

        world.DistrictPools.Consumed[row] = 1_000;

        for (int day = 0; day < 20; day++)
        {
            world.RepriceDistrictPools();
            world.DistrictPools.Consumed[row] = 1_000;
        }

        Assert.Equal(ceiling, world.DistrictPools.Price[row]);
    }

    /// <summary>A glut walks the price down to nothing, and the glut is the SELLERS' stock.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS TEST DEPOSITED INTO THE POOL'S OWN BIN UNTIL 2026-08-26, WHICH IS THE ONE PLACE A
    /// GLUT CANNOT BE.</b> It passed, and it proved nothing about a city: <c>adr/0139</c> makes a Pool
    /// a market and not a store, so that Bin is empty in every row of every world, and
    /// <c>World.RepriceDistrictPools</c> was handing exactly that zero to <c>Reprice</c> as its cover.
    /// ***The suite asserted the mechanism through a channel no Ruleset can reach***, which is why no
    /// price had ever moved on any world and nothing here said so. <c>adr/0171</c>;
    /// <see cref="Depositing_into_the_pools_own_bin_moves_no_price"/> pins the defect so it cannot
    /// return.
    /// </para>
    /// <para>
    /// <b>The glut needs no deposit at all, and that is the point.</b> A tier-0 Provider city over-supplies
    /// itself: the sellers hold more sundries than the import ceiling is worth, so one unit a Day of
    /// draw is many Days of cover and the target is zero. ⚠ <b>Zero is the floor on purpose</b> — a
    /// Provider selling into a saturated market earns less than it spent, and bankruptcy is the
    /// observable that tells this market from a decorative one (<c>plans/0037</c> decision 4, settled
    /// with the user in the room).
    /// </para>
    /// </remarks>
    [Fact]
    public void A_glut_walks_the_price_to_nothing()
    {
        var world = new World(2_000, Load("oversupplied.toml"), Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int row = Rows.NoSlot;

        for (int tick = 0; tick < 24_576 && row == Rows.NoSlot; tick++)
        {
            simulation.Step(default);
            row = GluttedRow(world);
        }

        Assert.True(
            row != Rows.NoSlot,
            "no market row ever held more stock than its ceiling is worth, so there is no glut to "
            + "price and this test's premise has gone. A tier-0 Provider city is supposed to "
            + "over-supply itself (adr/0170 condition 4); check that oversupplied.toml still states "
            + "no build_threshold_days.");

        for (int day = 0; day < 40; day++)
        {
            world.DistrictPools.Consumed[row] = 1;
            world.RepriceDistrictPools();
        }

        Assert.Equal(Money.Zero, world.DistrictPools.Price[row]);
    }

    /// <summary>
    /// 🔴 Filling the Pool's own Bin moves no price, because a Pool is a market and not a store.
    /// </summary>
    /// <remarks>
    /// <b>This is the defect <see cref="A_glut_walks_the_price_to_nothing"/> used to be written as, kept
    /// as an assertion so that it cannot come back as a repair.</b> A future author looking for
    /// somewhere to put a market's inventory has one obviously-shaped place to put it, and it is the
    /// wrong one: <c>adr/0139</c> spent a record deciding that stock stays with the seller, and the
    /// Pool's Bin exists to be a wait target and nothing else (<c>adr/0167</c>). ***If this ever fails,
    /// somebody has made a Pool a store again*** — read <c>adr/0139</c> and <c>adr/0171</c> before
    /// changing it.
    /// </remarks>
    [Fact]
    public void Depositing_into_the_pools_own_bin_moves_no_price()
    {
        var world = new World(2_000, Twinned(), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int row = FirstPoolRow(world);
        int bin = world.Bins.Rows.Resolve(world.DistrictPools.Bin[row]);
        Money ceiling = world.Rules.ImportCeiling(world.Bins.Resource[bin]);

        world.Deposit(world.DistrictPools.Bin[row], 10_000_000, Ticks.Zero);

        for (int day = 0; day < 40; day++)
        {
            world.DistrictPools.Consumed[row] = 1;
            world.RepriceDistrictPools();
        }

        Assert.Equal(ceiling, world.DistrictPools.Price[row]);
    }

    /// <summary>
    /// The first live market row whose sellers hold more than its ceiling is worth.
    /// </summary>
    /// <remarks>
    /// <b>More than the ceiling, and not merely more than nothing, because that is what makes the
    /// target zero.</b> The target is <c>ceiling × rate ÷ cover</c> under floor division, so a draw of
    /// one Day against a cover above the ceiling's own magnitude rounds to nothing. ***A row with one
    /// unit in it is a market, not a glut***, and waiting for the second condition is what stops this
    /// test depending on which Tick the first seller happened to open.
    /// </remarks>
    private static int GluttedRow(World world)
    {
        for (int slot = 0; slot < world.DistrictPools.Rows.SlotCount; slot++)
        {
            if (!world.DistrictPools.Rows.IsLive(slot)
                || !world.Bins.Rows.TryResolve(world.DistrictPools.Bin[slot], out int bin))
            {
                continue;
            }

            if (world.Markets.Stock(world, slot).Held
                > world.Rules.ImportCeiling(world.Bins.Resource[bin]).Raw)
            {
                return slot;
            }
        }

        return Rows.NoSlot;
    }

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Parse(Body(file), file);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"{file} was refused, so this test cannot run:\n{result.Describe()}");
    }
}
