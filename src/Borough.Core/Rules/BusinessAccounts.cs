namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;

/// <summary>
/// Where a Business's revenue and expenses are recognised — <c>plans/0072</c> D15's simplified
/// accrual, in one place.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Nothing here moves a single unit of anything.</b> Every method writes accumulators and a
/// stock Bin's cost; none of them calls <c>World.Deposit</c> or <c>World.Withdraw</c>, and none of
/// them touches <c>MoneySupplyTable.Issued</c>. ***Accounting records what happened; it is not one
/// of the things that happens.*** That is what keeps <c>Invariant.MoneyIsConserved</c> green with
/// all of this live, and it is the property to check first if it ever goes red.
/// </para>
/// <para>
/// <b>A separate class rather than a method on <see cref="BusinessTable"/>, because there are two
/// questions and only one of them is about a table.</b> The table owns <em>when a Day rolls</em>.
/// This owns <em>what counts</em> — and what counts is a policy with an argument behind it, stated
/// once here rather than re-derived at each of the three sale sites.
/// </para>
/// <para>
/// ⚠ <b>The hard half is what does NOT count</b>, because a Business's till is credited by four
/// things and only one of them is a sale:
/// </para>
/// <list type="bullet">
/// <item><description><b>Founding capital</b> — <c>World.Found</c> moves a Household's money into a
/// new trade's till. Nothing was delivered, so it is not revenue; it is the Household buying the
/// Business.</description></item>
/// <item><description><b>A <c>[[policy]]</c> transfer</b> — <c>PolicyEngine.Move</c> sweeps a
/// Business's balance in either direction. A grant is not a sale and a levy is not a cost of
/// goods.</description></item>
/// <item><description><b>A Bin Rule's money output</b> — a Rule may name money as an output and
/// credit a till from nothing. That is the money supply moving, not a trade making a
/// sale.</description></item>
/// <item><description><b>A wage</b> — money leaving the till, which <em>is</em> an expense, but on
/// the Day it was <em>earned</em> rather than on the payday it was paid. See
/// <see cref="Wages"/>.</description></item>
/// </list>
/// </remarks>
public static class BusinessAccounts
{
    /// <summary>The Day a Tick falls in, as the accumulators store it.</summary>
    /// <remarks>
    /// <b><c>ushort</c> on <c>CitizenTable.LastPaidDay</c>'s precedent</b> — a Day column in this
    /// build is two bytes, and at <see cref="Ticks.PerDay"/> that is longer than any run.
    /// </remarks>
    public static ushort DayOf(Ticks tick) =>
        (ushort)IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);

    /// <summary>
    /// Goods left <paramref name="stockBin"/> and <paramref name="payment"/> arrived for them:
    /// revenue, and the share of what that stock cost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The two legs are independent and both are real.</b> A delivery at a price of zero is
    /// revenue of zero against a genuine cost of goods — stock given away — and stock that arrived
    /// free is revenue against a cost of nothing. Neither is a special case.
    /// </para>
    /// <para>
    /// ⚠ <b><paramref name="level"/> is the Bin's level BEFORE the Goods left</b>, which every
    /// caller has to capture before it withdraws. <see cref="BinTable.DrawCost"/> carries why.
    /// </para>
    /// </remarks>
    /// <param name="world">The world being accounted for.</param>
    /// <param name="business">The selling Business's slot.</param>
    /// <param name="stockBin">The Bin the Goods came out of.</param>
    /// <param name="quantity">How many units left.</param>
    /// <param name="level">That Bin's level before they left.</param>
    /// <param name="payment">What was paid for them.</param>
    /// <param name="tick">The Tick the delivery happened on.</param>
    public static void Deliver(
        World world, int business, int stockBin, long quantity, long level, long payment, Ticks tick)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Businesses.Rows.IsLive(business))
        {
            return;
        }

        ushort today = DayOf(tick);

        long cost = stockBin >= 0 && world.Bins.Rows.IsLive(stockBin)
            ? world.Bins.DrawCost(stockBin, quantity, level)
            : 0;

        world.Businesses.Earn(business, today, payment);
        world.Businesses.Expend(business, today, cost);
    }

    /// <summary>
    /// A service was delivered for <paramref name="payment"/>: revenue, and no cost of goods,
    /// because there were no Goods.
    /// </summary>
    /// <remarks>
    /// <b>Tuition is the shipped case</b> — <c>SchoolingEngine.Charge</c> takes a Day's fee and no
    /// stock Bin falls. ⚠ <b>That is not the same thing as a sale whose stock happened to cost
    /// nothing</b>: there is no Bin to draw down here at all, so nothing can be wrong about which
    /// Bin was drawn.
    /// </remarks>
    public static void Serve(World world, int business, long payment, Ticks tick)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Businesses.Rows.IsLive(business))
        {
            world.Businesses.Earn(business, DayOf(tick), payment);
        }
    }

    /// <summary>
    /// Stock arrived in <paramref name="stockBin"/> and <paramref name="payment"/> was paid for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is the leg most easily written the wrong way round, so it is a method of its own
    /// with a name that cannot be confused for <see cref="Deliver"/>.</b> A Business paying a Pool
    /// is <em>buying</em>: the money leaving its till is stock arriving, and it belongs on the Bin's
    /// cost. Recording it as revenue would invert the sign of every profit figure in the city.
    /// </para>
    /// <para>
    /// ⚠ <b>It is not an expense yet</b> (<c>plans/0072</c> D15). It becomes one when the stock is
    /// sold, a share at a time.
    /// </para>
    /// <para>
    /// ⚠ <b>Guarded on the Bin's owner being a Business</b>, because a Household buys from a Pool
    /// too — <c>rulesets/shopping.toml</c>'s <c>restock</c> is a Household's — and a larder is not
    /// trading stock.
    /// </para>
    /// </remarks>
    public static void Stock(World world, int stockBin, long payment)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (stockBin >= 0
            && world.Bins.Rows.IsLive(stockBin)
            && world.Bins.OwnerKind[stockBin] == BinOwnerKind.Business)
        {
            world.Bins.AddCost(stockBin, payment);
        }
    }

    /// <summary>
    /// One Day's gross wage bill, expensed on the Day it was earned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>On the Day, and deliberately not on the payday</b> (<c>plans/0072</c> D15 read against
    /// D23). A trade on a seven-Day pay period that expensed its payroll where it paid it would
    /// post one enormous loss and six clean profits every week, and a schedule denominated in
    /// profit-per-Day would read that as a city of failing businesses taxed on six Days in seven.
    /// ***The pay period is a property of when money moves and must not reach what a Day was
    /// worth.***
    /// </para>
    /// <para>
    /// ⚠ <b>Gross, including the portion withheld for the Citizen's income tax</b> (D15 again). What
    /// the employer gives up is the whole wage; that the treasury takes part of it on the way is the
    /// Citizen's affair and D16's, not a discount on the employer's cost.
    /// </para>
    /// </remarks>
    public static void Wages(World world, int business, long gross, Ticks tick)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Businesses.Rows.IsLive(business))
        {
            world.Businesses.Expend(business, DayOf(tick), gross);
        }
    }

    /// <summary>What a Business made on the Day <paramref name="tick"/> falls in.</summary>
    /// <remarks>
    /// <see cref="BusinessTable.ProfitOn"/> carries the ordering obligation this inherits: a Day's
    /// figures stand until the first recognition of the next Day clears them.
    /// </remarks>
    public static long ProfitOn(World world, int business, Ticks tick)
    {
        ArgumentNullException.ThrowIfNull(world);

        return world.Businesses.Rows.IsLive(business)
            ? world.Businesses.ProfitOn(business, DayOf(tick))
            : 0;
    }
}
