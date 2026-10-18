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
/// The provider is either a city office or a Hinterland. <see cref="Provider"/> names the office;
/// when it names none, <see cref="Hinterland"/> gives the index in <see cref="Ruleset.Hinterlands"/>.
/// A contract whose provider no longer supplies — a freed office, an office out of units, an index
/// that no longer names a gated Hinterland — is re-matched on the next purchase, so neither a
/// demolition nor a hot reload that drops or reorders Hinterlands needs a repair pass.
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

        // Severable, because an office can be demolished under its clients. The stale handle is the
        // fact the client re-matches on, exactly as Businesses.Building reads as premises gone.
        Provider = _rows.SavedHandle(
            "provider", businesses.Rows, Touch.Cold, Reference.Severable);

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

    /// <summary>The providing office, or none when a Hinterland provides.</summary>
    public HandleColumn<Business> Provider { get; }

    /// <summary>
    /// The providing Hinterland's index in <see cref="Ruleset.Hinterlands"/>, or <c>-1</c> when
    /// <see cref="Provider"/> names an office.
    /// </summary>
    public Column<int> Hinterland { get; }

    /// <summary>The Tick this provider was chosen.</summary>
    public Column<Ticks> Since { get; }

    /// <summary>The next contract of the same buyer, threaded by <c>World.BusinessContracts</c>.</summary>
    public Column<int> BuyerNext { get; }
}
