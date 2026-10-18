namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>One firm's standing arrangement with one provider of one service. Empty for <c>Citizen</c>'s reason.</summary>
public readonly struct ServiceContract;

/// <summary>
/// Which provider each firm buys each Professional Service from (<c>adr/0179</c>).
/// </summary>
/// <remarks>
/// <para>
/// A firm keeps its provider while the provider can still supply it, and picks a new one only when it
/// cannot. That is the difference from a Good, whose seller is drawn afresh on every purchase.
/// </para>
/// <para>
/// The provider is a Hinterland, by its index in <see cref="Ruleset.Hinterlands"/>. A contract whose
/// index no longer names a supplying Hinterland is re-matched on the next purchase, so a hot reload
/// that drops or reorders Hinterlands needs no repair pass.
/// </para>
/// </remarks>
[Table]
public sealed class ServiceContractTable
{
    private readonly Rows<ServiceContract> _rows;

    /// <param name="capacity">Initial row count; it grows.</param>
    /// <param name="businesses">The table <see cref="Business"/> handles are resolved against.</param>
    public ServiceContractTable(int capacity, BusinessTable businesses)
    {
        ArgumentNullException.ThrowIfNull(businesses);

        _rows = new Rows<ServiceContract>("service_contract", capacity, Buffering.OneCopy);

        Buyer = _rows.SavedHandle("buyer", businesses.Rows);
        Service = _rows.Saved<ResourceId>("service", Touch.Cold);
        Hinterland = _rows.Saved<int>("hinterland", Touch.Cold);
        Since = _rows.Saved<Ticks>("since", Touch.Cold);
        BuyerNext = _rows.Derived<int>("buyer_next");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<ServiceContract> Rows => _rows;

    /// <summary>The firm that buys.</summary>
    public HandleColumn<Business> Buyer { get; }

    /// <summary>The service bought.</summary>
    public Column<ResourceId> Service { get; }

    /// <summary>The providing Hinterland's index in <see cref="Ruleset.Hinterlands"/>.</summary>
    public Column<int> Hinterland { get; }

    /// <summary>The Tick this provider was chosen.</summary>
    public Column<Ticks> Since { get; }

    /// <summary>The next contract of the same buyer, threaded by <c>World.BusinessContracts</c>.</summary>
    public Column<int> BuyerNext { get; }
}
