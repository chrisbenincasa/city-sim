using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Bin of the District Pool. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct DistrictPool;

/// <summary>
/// Which Bins belong to which District's Pool — the Pool, as rows.
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
/// ⚠ <b>There is no lookup index and task 5 does not need one.</b> <c>Scope.Pool</c> still throws, so
/// every read of this table is cold — opening a Pool, and merging one into its heir. A dense
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

    /// <summary>Records that a Bin belongs to a District's Pool.</summary>
    public Handle<DistrictPool> Create(Handle<District> district, Handle<Bin> bin)
    {
        Handle<DistrictPool> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        District[slot] = district;
        Bin[slot] = bin;

        return handle;
    }
}
