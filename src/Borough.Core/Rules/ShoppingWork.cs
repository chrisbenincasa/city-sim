using Borough.Core.Space;
using Borough.Core.Movement;

namespace Borough.Core.Rules;

public sealed class ShoppingWork
{
    public RouteWork Estimates { get; } = new();
    public BuildingQueryWork Selection { get; } = new();
    public long Considered { get; set; }
    public long Starts { get; set; }
    public long NoKnownShop { get; set; }
    public long Unreachable { get; set; }
    public long DiscoveryCalls { get; set; }
    public long DiscoveryWithoutAddition { get; set; }
    public long Draws { get; set; }
    public long DrawsWithoutBusiness { get; set; }
    public long BusinessesChecked { get; set; }
    public long NoSaleBin { get; set; }
    public long AlreadyKnown { get; set; }
    public long DiscoveryRouteRejected { get; set; }
    public long ProvidersAdded { get; set; }
    public void Reset()
    {
        Estimates.Reset(); Selection.Reset();
        Considered = Starts = NoKnownShop = Unreachable = 0;
        DiscoveryCalls = DiscoveryWithoutAddition = Draws = DrawsWithoutBusiness = 0;
        BusinessesChecked = NoSaleBin = AlreadyKnown = DiscoveryRouteRejected = ProvidersAdded = 0;
    }
}
