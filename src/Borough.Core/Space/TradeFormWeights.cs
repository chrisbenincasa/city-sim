namespace Borough.Core.Space;

/// <summary>
/// How often each trade form is drawn within its band's tier, relative to the other forms there.
/// A <see cref="BlockPattern.DeckedSupermarket"/> is drawn as a <see cref="BlockPattern.Supermarket"/>,
/// and a <see cref="BlockPattern.GalleryPrecinct"/> as a <see cref="BlockPattern.Precinct"/>.
/// </summary>
public readonly record struct TradeFormWeights(
    int CarParkCentre, int SalesYard, int ShopHouseParade, int Supermarket, int Precinct, int HighStreetBlock)
{
    public static TradeFormWeights Even => new(1, 1, 1, 1, 1, 1);

    public int Of(BlockPattern form) => form switch
    {
        BlockPattern.CarParkCentre => CarParkCentre,
        BlockPattern.SalesYard => SalesYard,
        BlockPattern.ShopHouseParade => ShopHouseParade,
        BlockPattern.Supermarket or BlockPattern.DeckedSupermarket => Supermarket,
        BlockPattern.Precinct or BlockPattern.GalleryPrecinct => Precinct,
        BlockPattern.HighStreetBlock => HighStreetBlock,
        _ => 0,
    };
}
