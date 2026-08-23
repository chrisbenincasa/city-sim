using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Bin of the District Pool. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct DistrictPool;

/// <summary>
/// Which Bins belong to which District's Pool, and what each one charges — the Pool, as rows.
/// </summary>
/// <remarks>
/// <para>
/// <b>A join table rather than a column on the Bin, because <see cref="BinTable.Owner"/> cannot hold a
/// District and must not be widened to.</b> That column is a
/// <see cref="HandleColumn{Building}"/> bound to <c>buildings.Rows</c> at construction, so a
/// District-owned Bin cannot address its owner through it. <c>adr/0114</c>'s answer for an owner that
/// does not fit is already in the build twice: a Household and a Business each hold their Bin's handle
/// on the <em>owner</em> row and leave <see cref="BinTable.Owner"/> unset. This is that shape with the
/// cardinality a Pool needs — an actor holds one Bin because money is one Resource, and a District
/// holds one per Good.
/// </para>
/// <para>
/// <b>Saved, and it is the only thing that knows a Pool Bin's District.</b> A Building's Bins hang off
/// a <see cref="Disposition.Derived"/> list that <c>World.RebuildDerived</c> re-threads from the Bins'
/// own owner column; there is no such column here, so this relation comes out of the save already made
/// or it does not come back at all. ⚠ <b>That is a difference in <em>what is recoverable</em> and not a
/// preference</b>: a derived list is only derivable when the element names its owner.
/// </para>
/// <para>
/// <b>It also became the market's own table at task 6, and that was not a second decision.</b>
/// <see cref="Price"/>, <see cref="Rate"/> and <see cref="Consumed"/> are all keyed by
/// <c>(District, Good)</c>, which is this row's identity exactly — so the alternative was a second
/// table with the same key, joined to this one. ***A fact keyed by a row that already exists belongs on
/// that row***, and the join it would have needed is the join this table already is.
/// </para>
/// <para>
/// ⚠ <b>There is no lookup index and tasks 5 and 6 do not need one.</b> <c>Scope.Pool</c> still throws,
/// so every read of this table is cold — opening a Pool, merging one into its heir, and once a Day
/// repricing every row of it, which is a full walk rather than a lookup. A dense
/// <c>(District, Resource) → Bin</c> index is what a <em>hot</em> path wants, and the hot path arrives
/// with the purchase. Building it now would be an index nothing measures, sized against a table nothing
/// walks. <b>Task 7 owes it</b>, on the Tick a pool term resolves for the first time.
/// </para>
/// </remarks>
[Table]
public sealed class DistrictPoolTable
{
    private readonly Rows<DistrictPool> _rows;

    /// <param name="capacity">Initial row count — one per Good per District, and both are few.</param>
    /// <param name="districts">The table <see cref="District"/> handles are resolved against.</param>
    /// <param name="bins">The table <see cref="Bin"/> handles are resolved against.</param>
    public DistrictPoolTable(int capacity, DistrictTable districts, BinTable bins)
    {
        ArgumentNullException.ThrowIfNull(districts);
        ArgumentNullException.ThrowIfNull(bins);

        _rows = new Rows<DistrictPool>("district_pool", capacity, Buffering.OneCopy);

        District = _rows.SavedHandle("district", districts.Rows);
        Bin = _rows.SavedHandle("bin", bins.Rows);
        Price = _rows.Saved<Money>("price", Touch.Cold);
        Rate = _rows.Saved<long>("rate", Touch.Cold);
        Consumed = _rows.Saved<long>("consumed", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<DistrictPool> Rows => _rows;

    /// <summary>The District whose Pool holds this Bin.</summary>
    public HandleColumn<District> District { get; }

    /// <summary>
    /// The Bin. <b>Its Resource is read off the Bin rather than copied here</b> — a second column
    /// stating it would be a fact with two homes, and the one that drifts is the one nothing reads.
    /// </summary>
    public HandleColumn<Bin> Bin { get; }

    /// <summary>
    /// <b>What this Good costs in this District</b> (<c>adr/0135</c>, milestone 12 task 6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Seeded at <c>Ruleset.ImportCeiling</c> and never authored.</b> <c>04 §4</c> wants a price
    /// *"not set by the player and not authored in the Ruleset"*, and this is the column that makes
    /// that true — what a designer authors is the ceiling and the damping, and the price falls out.
    /// ⚠ <b>A Pool nobody trades in stays at the ceiling for ever</b>, which is not a stub: it is what
    /// keeps <c>adr/0045</c>'s ladder monotone from Tick 0 without anybody choosing a seed.
    /// </para>
    /// <para>
    /// <b><c>(saved AND hashed)</c>, and it has to be.</b> It is a damped average of its own history —
    /// <see cref="MarketRuleset.Reprice"/> reads the standing price to compute the next one — so a
    /// world that reloaded without it would resume from a different price and diverge. ***A quantity
    /// whose next value depends on its current one cannot be derived from the world's shape.***
    /// </para>
    /// <para>
    /// ⚠ <b><see cref="Touch.Cold"/> and it is not a guess</b>: it is read and written once per Day
    /// per row, which is once in 2048 Ticks, and a purchase reading it is task 7's traffic and not
    /// this column's.
    /// </para>
    /// </remarks>
    public Column<Money> Price { get; }

    /// <summary>
    /// <b>The smoothed consumption rate, in units of the Good per Day.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The denominator of the tâtonnement's cover ratio</b> — how long <see cref="Bin"/>'s level
    /// would last at the rate the District has recently been drawing. <see cref="MarketRuleset.Smooth"/>
    /// folds each Day's <see cref="Consumed"/> into it, and the decay is the designer's.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a rate and <see cref="Consumed"/> is a bucket, and keeping them apart is deliberate.</b>
    /// One column that decayed in place would hold a rate multiplied by a constant nobody could name
    /// the units of, and ***a number nobody can name the units of is <c>plans/0012</c> Cause 5 built
    /// rather than quoted.*** Two columns cost one <c>long</c> a row and the units stay sayable.
    /// </para>
    /// </remarks>
    public Column<long> Rate { get; }

    /// <summary>
    /// <b>What has been drawn from this Pool since the last recompute</b> — the current Day's bucket,
    /// zeroed by every recompute.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>NOTHING WRITES THIS AT TASK 6 and the column ships anyway.</b> <c>Scope.Pool</c> still
    /// throws, so no Rule can draw from a Pool and every Day's bucket is zero — which
    /// <see cref="MarketRuleset.Reprice"/> reads as *no trades*, keeps the price, and leaves the
    /// market visibly correct and visibly inert. ⚠ <b>That is the shape this corpus has accepted
    /// twice</b> — milestone 9's land-value producer and task 5's Pool Bins — and it is accepted here
    /// for the same reason: <b>task 7 supplies the writer</b>, and a purchase settling at a price
    /// needs the price to already exist.
    /// </remarks>
    public Column<long> Consumed { get; }

    /// <summary>
    /// Records that a Bin belongs to a District's Pool, at a starting price.
    /// </summary>
    /// <remarks>
    /// <b><paramref name="price"/> is the caller's because the ceiling is the Ruleset's</b> — this
    /// table holds no <c>Ruleset</c> and must not learn to, so the seed arrives from
    /// <c>World.CreateDistrictPoolBin</c> where the Ruleset is in hand.
    /// </remarks>
    /// <param name="district">Whose Pool this is.</param>
    /// <param name="bin">The Bin holding the stock.</param>
    /// <param name="price">What the Good costs on the day the Pool opens.</param>
    public Handle<DistrictPool> Create(Handle<District> district, Handle<Bin> bin, Money price)
    {
        Handle<DistrictPool> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        District[slot] = district;
        Bin[slot] = bin;
        Price[slot] = price;
        Rate[slot] = 0;
        Consumed[slot] = 0;

        return handle;
    }
}
