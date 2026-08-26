using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One seller in a market row: the Bin holding the stock, and the Business it belongs to.
/// </summary>
/// <remarks>
/// <b>Both, because a purchase settles both legs</b> — the Good leaves the Bin and the money enters
/// the Business's balance, and <c>BinTable.Owner</c> cannot carry the second (<c>adr/0114</c>: it is
/// a <c>Handle&lt;Building&gt;</c> and a Business leaves it unset). ***One seller with two custodians
/// is what <c>adr/0139</c>'s correction refused***, so the pair travels together rather than being
/// looked up twice.
/// </remarks>
/// <param name="Bin">The Bin slot holding the offered stock.</param>
/// <param name="Business">The Business slot that owns it, and is paid.</param>
public readonly record struct Offer(int Bin, int Business);

/// <summary>
/// Where a purchase looks: a District's market row for a Good, and the sellers standing in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two lookups rather than one, because a purchase asks two questions and neither answer was
/// reachable.</b> <c>World.FindDistrictPoolBin</c> is a full walk of the join — fine for opening a
/// Pool and repricing it once a Day, and <see cref="DistrictPoolTable"/>'s own remark says the hot
/// path arrives with the purchase and owes an index. That is the first half. The second half has
/// never existed at all: <c>adr/0139</c> makes a <c>pool</c> term resolve to <em>one seller's Bin</em>
/// and calls a District's sellers <em>"a list"</em>, and there is no path from a District to the
/// Businesses in it — <see cref="DistrictTable"/> has two columns and <c>BusinessTable</c> has no
/// District.
/// </para>
/// <para>
/// <b>⚠ <c>(derived AND rebuilt)</c>, rebuilt whole and lazily, which is <see cref="ZonedLots"/>'s
/// shape and its argument.</b> Membership changes only where a Business's stock Bins change
/// (<c>World.FitBusiness</c> and <c>World.UnfitBusiness</c> are the two chokepoints), where a Pool row
/// is opened or retired, or where the watershed moves a boundary — once a Day, at most
/// <c>[districts] migrate_cells</c> Cells. None of those is the per-Tick churn of a city, so an
/// invalidation flag costs one <c>O(n)</c> pass on the next query rather than one per event.
/// ***Rebuilding whole cannot drift the way incremental maintenance can***, which is the property
/// that matters here: a seller silently missing from a list is a shop nobody can buy from, and no
/// invariant would ever say so.
/// </para>
/// <para>
/// <b>The rebuild is a counting sort, so its order is recoverable from saved state</b> —
/// <see cref="ZonedLots"/>'s own reason. Entries land in ascending Business-slot order within each
/// market row regardless of the order Businesses were created, so a load reproduces this
/// <em>exactly</em>. A seller draw over it is therefore stable across save and reload, which a
/// creation-ordered structure would not be the moment the free list recycled a slot.
/// </para>
/// <para>
/// ⚠ <b>A seller is a Bin and not a Business, and that is forced rather than tidy.</b> A Business may
/// sell several Goods and would need one <c>next</c> per Good to be threaded directly; one Bin is
/// exactly one <c>(Business, Good)</c>, so the Bin is the element whose cardinality matches the
/// question. It is also what a purchase actually wants — <c>adr/0139</c> resolves a term to a Bin.
/// </para>
/// <para>
/// ⚠ <b>Its cost is <em>measurable</em> and this build does not measure it.</b> <c>adr/0139</c> is
/// explicit that the per-firing seller lookup is unmeasured and that <c>adr/0043</c> binds: no
/// document may cite it as decided until a number exists. What is established here is only
/// <em>where</em> the lookup goes. <c>plans/0013</c> owes a row with a <b>measured</b> multiplicand,
/// and the fallback if it does not fit is named in that record — an index on the market row, which is
/// this — rather than a return to a shared store.
/// </para>
/// </remarks>
public sealed class DistrictMarkets
{
    /// <summary>What <see cref="Row"/> answers when a District has no market for a Resource.</summary>
    public const int NoRow = Rows.NoSlot;

    // Slot-plus-one encoded, so a cleared array reads as "no market" rather than as row zero --
    // DistrictResidency's encoding and its reason.
    private int[] _rows = [];

    private int _stride;

    private int[] _starts = [];

    private int[] _cursor = [];

    private int[] _entries = [];

    private int[] _owners = [];

    // The reverse of _entries: which market row a Bin is offered in, slot-plus-one encoded. It is
    // what World.RingMarket asks on every deposit -- a seller's stock Bin does not know its Business
    // (BinTable.Owner is a Building handle and is unset for one), so the walk back would otherwise be
    // over the whole Business table. plans/0044 names this lookup as one the purchase owes.
    private int[] _marketOf = [];

    private bool _stale = true;

    /// <summary>Marks the index out of date. The next query rebuilds it.</summary>
    public void Invalidate() => _stale = true;

    /// <summary>
    /// The <see cref="DistrictPoolTable"/> row for a District's Good, or <see cref="NoRow"/>.
    /// </summary>
    public int Row(World world, int districtSlot, ResourceId resource)
    {
        ArgumentNullException.ThrowIfNull(world);

        Ensure(world);

        if (districtSlot < 0 || resource.Raw == 0 || resource.Raw >= _stride)
        {
            return NoRow;
        }

        int index = (districtSlot * _stride) + resource.Raw;

        return index < _rows.Length ? _rows[index] - 1 : NoRow;
    }

    /// <summary>How many sellers stand in one market row.</summary>
    public int SellerCount(World world, int poolRow)
    {
        ArgumentNullException.ThrowIfNull(world);

        Ensure(world);

        return poolRow < 0 || poolRow + 1 >= _starts.Length
            ? 0
            : _starts[poolRow + 1] - _starts[poolRow];
    }

    /// <summary>One seller in a market row, by ordinal.</summary>
    public Offer Seller(World world, int poolRow, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(world);

        Ensure(world);

        int start = _starts[poolRow];

        if (ordinal < 0 || ordinal >= _starts[poolRow + 1] - start)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ordinal),
                ordinal,
                "there is no seller at that ordinal in this market row. Ask SellerCount first: an "
                + "empty market is a District nobody sells that Good in, which is a shortage rather "
                + "than an error.");
        }

        return new Offer(_entries[start + ordinal], _owners[start + ordinal]);
    }

    /// <summary>
    /// The market row a Bin is offered in, or <see cref="NoRow"/> if it is nobody's stock.
    /// </summary>
    /// <remarks>
    /// <b>What a deposit asks.</b> <c>adr/0139</c> has a blocked buyer wait on the market row and a
    /// seller's deposit ring it, and a deposit arrives holding one Bin slot and nothing else.
    /// </remarks>
    public int MarketOf(World world, int binSlot)
    {
        ArgumentNullException.ThrowIfNull(world);

        Ensure(world);

        return binSlot >= 0 && binSlot < _marketOf.Length ? _marketOf[binSlot] - 1 : NoRow;
    }

    /// <summary>Rebuilds both lookups from the Pool rows and the Businesses standing in them.</summary>
    public void Rebuild(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        RebuildRows(world);
        RebuildSellers(world);

        _stale = false;
    }

    private void RebuildRows(World world)
    {
        _stride = world.Rules.ResourceCount + 1;

        int needed = world.Districts.Rows.SlotCount * _stride;

        if (_rows.Length < needed)
        {
            _rows = new int[needed];
        }

        Array.Clear(_rows);

        DistrictPoolTable pools = world.DistrictPools;

        for (int row = 0; row < pools.Rows.SlotCount; row++)
        {
            if (!pools.Rows.IsLive(row)
                || !world.Districts.Rows.TryResolve(pools.District[row], out int districtSlot)
                || !world.Bins.Rows.TryResolve(pools.Bin[row], out int bin))
            {
                continue;
            }

            _rows[(districtSlot * _stride) + world.Bins.Resource[bin].Raw] = row + 1;
        }
    }

    private void RebuildSellers(World world)
    {
        int buckets = world.DistrictPools.Rows.SlotCount + 1;

        if (_starts.Length < buckets)
        {
            _starts = new int[buckets];
            _cursor = new int[buckets];
        }

        Array.Clear(_starts);

        int businesses = world.Businesses.Rows.SlotCount;
        int total = 0;

        // Counting sort, pass one, counted into starts[row + 1] so the prefix sum below turns the
        // same array into the runs without a second buffer -- ZonedLots' idiom exactly.
        for (int slot = 0; slot < businesses; slot++)
        {
            int districtSlot = DistrictOf(world, slot);

            if (districtSlot < 0)
            {
                continue;
            }

            for (Handle<Bin> at = world.Businesses.BinHead[slot]; !at.IsNone;)
            {
                int bin = world.Bins.Rows.Resolve(at);
                at = world.Bins.OwnerNext[bin];

                int row = OfferedIn(world, districtSlot, bin);

                if (row >= 0)
                {
                    _starts[row + 1]++;
                    total++;
                }
            }
        }

        for (int row = 1; row < buckets; row++)
        {
            _starts[row] += _starts[row - 1];
        }

        if (_entries.Length < total)
        {
            _entries = new int[total];
            _owners = new int[total];
        }

        if (_marketOf.Length < world.Bins.Rows.SlotCount)
        {
            _marketOf = new int[world.Bins.Rows.SlotCount];
        }

        Array.Clear(_marketOf);
        Array.Copy(_starts, _cursor, buckets);

        // Pass two, walking Business slots ascending again, so each run comes out in slot order.
        for (int slot = 0; slot < businesses; slot++)
        {
            int districtSlot = DistrictOf(world, slot);

            if (districtSlot < 0)
            {
                continue;
            }

            for (Handle<Bin> at = world.Businesses.BinHead[slot]; !at.IsNone;)
            {
                int bin = world.Bins.Rows.Resolve(at);
                at = world.Bins.OwnerNext[bin];

                int row = OfferedIn(world, districtSlot, bin);

                if (row >= 0)
                {
                    _owners[_cursor[row]] = slot;
                    _entries[_cursor[row]++] = bin;
                    _marketOf[bin] = row + 1;
                }
            }
        }
    }

    /// <summary>
    /// The District a Business stands in, or <see cref="Rows.NoSlot"/> if it stands nowhere.
    /// </summary>
    /// <remarks>
    /// <b>Premises, then Lot, then Cell, then the residency index</b> — the chain
    /// <c>RuleEngine.Emit</c> already walks for a Map emission, and the one
    /// <see cref="DistrictResidency.Of"/>'s own remark says a Building's District goes through.
    /// ⚠ <b>An <em>unpremised</em> Business stands in no District and therefore sells nothing</b>
    /// (<c>adr/0142</c>), which is the right answer rather than a gap: its stock was destroyed with
    /// its tenancy and all it holds is its balance.
    /// </remarks>
    private static int DistrictOf(World world, int businessSlot)
    {
        if (!world.Businesses.Rows.IsLive(businessSlot)
            || !world.Buildings.Rows.TryResolve(
                world.Businesses.Building[businessSlot], out int buildingSlot)
            || !world.Lots.Rows.TryResolve(world.Buildings.Lot[buildingSlot], out int lotSlot))
        {
            return Rows.NoSlot;
        }

        Handle<District> district = world.DistrictsInCells.Of(
            world.DistrictCells,
            CellGrid.ToCells(world.Lots.East[lotSlot]),
            CellGrid.ToCells(world.Lots.North[lotSlot]));

        return world.Districts.Rows.TryResolve(district, out int districtSlot)
            ? districtSlot
            : Rows.NoSlot;
    }

    /// <summary>
    /// The market row a Business's Bin is offered in, or <see cref="Rows.NoSlot"/> if it is not stock.
    /// </summary>
    /// <remarks>
    /// <b>A Good the Business itself owns, and nothing else.</b> A balance is conserved and is not
    /// merchandise; an <c>owner = "occupant"</c> Bin belongs to the Household living there and hangs
    /// off <c>HouseholdTable</c> rather than here; a premises Bin is the landlord's. So the test is
    /// the owner kind the Bin was created with plus the Resource's family, and
    /// <c>adr/0139</c>'s <em>"a <c>local</c> output is the Building's to keep, a <c>pool</c> output is
    /// the Building's to sell"</em> needs no third state: ***in this build a Business-owned Good Bin
    /// IS the offer***, which is why <c>rulesets/provisioned.toml</c>'s <c>stock</c> Rule writes
    /// <c>local</c> and its header says so.
    /// </remarks>
    private int OfferedIn(World world, int districtSlot, int bin)
    {
        ResourceId resource = world.Bins.Resource[bin];

        if (world.Bins.OwnerKind[bin] != BinOwnerKind.Business
            || world.Rules.Family(resource) != ResourceFamily.Good)
        {
            return Rows.NoSlot;
        }

        return _rows[(districtSlot * _stride) + resource.Raw] - 1;
    }

    private void Ensure(World world)
    {
        if (_stale)
        {
            Rebuild(world);
        }
    }
}
