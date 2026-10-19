namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>One office's daily allowance of one Professional Service. Row type of the table below.</summary>
public readonly struct ServiceAllowance;

/// <summary>
/// What each office may still sell of each Professional Service today (<c>adr/0179</c>).
/// </summary>
/// <remarks>
/// <para>
/// A provider's capacity is a Day's worth of units rather than a stock. Each application of the
/// office's Rule adds its output amount to <see cref="MadeToday"/>, each sale adds to
/// <see cref="SoldToday"/>, and the difference is what is left. Both reset at the Day boundary, so
/// unsold capacity expires rather than accumulating — a service is never stocked and no Bin holds it.
/// </para>
/// <para>
/// The reset is lazy: a row whose <see cref="FlowDay"/> is behind the current Day reads as zero left,
/// and the next unit made rolls it. A sweep over every row at each boundary would cost one walk per
/// Day to reach the same state, and the row that matters is the one somebody asks about.
/// </para>
/// <para>
/// One row per (office, service), opened the first time the office makes units and freed when its
/// Business is destroyed. So the table is bounded by offices × services, and
/// <see cref="Provider"/> is <see cref="Reference.Severable"/> for the window between a Business
/// being freed and <c>World.DestroyBusiness</c> clearing the rows that name it.
/// </para>
/// </remarks>
[Table]
public sealed class ServiceAllowanceTable
{
    private readonly Rows<ServiceAllowance> _rows;

    /// <param name="capacity">Initial row count; it grows.</param>
    /// <param name="businesses">The table <see cref="Business"/> handles are resolved against.</param>
    public ServiceAllowanceTable(int capacity, BusinessTable businesses)
    {
        ArgumentNullException.ThrowIfNull(businesses);

        _rows = new Rows<ServiceAllowance>("service_allowance", capacity, Buffering.OneCopy);

        // Every column is Touch.Wake, the declaration default: all five are read in one scan when a
        // firm transacts for a service, and none of them on an ordinary Tick.
        Provider = _rows.SavedHandle("provider", businesses.Rows, reference: Reference.Severable);
        Service = _rows.Saved<ResourceId>("service");
        MadeToday = _rows.Saved<long>("made_today");
        SoldToday = _rows.Saved<long>("sold_today");
        FlowDay = _rows.Saved<int>("flow_day");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<ServiceAllowance> Rows => _rows;

    /// <summary>The office whose capacity this is.</summary>
    public HandleColumn<Business> Provider { get; }

    /// <summary>The service it makes.</summary>
    public Column<ResourceId> Service { get; }

    /// <summary>Units its Rule has made on <see cref="FlowDay"/>.</summary>
    public Column<long> MadeToday { get; }

    /// <summary>Units its clients have bought on <see cref="FlowDay"/>.</summary>
    public Column<long> SoldToday { get; }

    /// <summary>The Day the two counters above count. Behind today means the row is spent.</summary>
    public Column<int> FlowDay { get; }
}
