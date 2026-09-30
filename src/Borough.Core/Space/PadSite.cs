using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The pad sites a <see cref="BlockPattern.CarParkCentre"/> block carves along one side face, and
/// the ground each pad lays out.
/// </summary>
/// <remarks>
/// A pad runs back from its street: a forecourt with no stalls, then the footprint. A stall band runs
/// along the pad's south side for its whole depth, with its aisle against the footprint. The drawing
/// fills the forecourt by family, so the simulation never says whether it holds pumps or a drive lane.
/// </remarks>
public static class PadSite
{
    /// <summary>How far a pad reaches back from its street edge.</summary>
    public const int DepthTiles = 8;

    /// <summary>How many pads the strip holds.</summary>
    public const int Pads = 3;

    public const int ForecourtTiles = 4;

    public const int StallBandTiles = 4;

    /// <summary>
    /// Narrows the centre's parcel and cuts the strip it gives up into pads. Writes the centre first.
    /// </summary>
    /// <returns>How many parcels it wrote: the centre alone when the block is too small for pads.</returns>
    public static int Split(Parcel centre, BlockGround ground, int streetHalfWidthTiles, bool east, Span<Parcel> into)
    {
        int strip = DepthTiles + streetHalfWidthTiles;
        int netNorth = ground.North + streetHalfWidthTiles;
        int net = ground.Deep - (2 * streetHalfWidthTiles);
        int frontage = IntegerMath.FloorDiv(net, Pads);

        if (centre.Wide.Raw - strip < DepthTiles || frontage <= StallBandTiles)
        {
            into[0] = centre;
            return 1;
        }

        int centreEast = east ? centre.East.Raw : centre.East.Raw + strip;
        int centreWide = centre.Wide.Raw - strip;
        into[0] = centre with
        {
            East = new Tiles(centreEast),
            Wide = new Tiles(centreWide),
            Offset = new Tiles(centreEast - ground.East + IntegerMath.FloorDiv(centreWide, 2)),
        };

        BlockFace face = east ? BlockFace.East : BlockFace.West;
        int stripEast = east ? centre.East.Raw + centre.Wide.Raw - strip : centre.East.Raw;
        int top = centre.North.Raw + centre.Deep.Raw;

        for (int pad = 0; pad < Pads; pad++)
        {
            int from = netNorth + (pad * frontage);
            int to = pad == Pads - 1 ? netNorth + net : from + frontage;
            int north = pad == 0 ? centre.North.Raw : from;
            int end = pad == Pads - 1 ? top : to;

            into[1 + pad] = new Parcel(
                face, BlockPatterns.SideOf(face), new Tiles(from - ground.North + IntegerMath.FloorDiv(to - from, 2)),
                new Tiles(stripEast), new Tiles(north), new Tiles(strip), new Tiles(end - north));
        }

        return 1 + Pads;
    }

    /// <summary>The pad's footprint: behind the forecourt and north of the stall band.</summary>
    public static (Tiles East, Tiles North, Tiles Wide, Tiles Deep) Footprint(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles)
    {
        int west = Max(parcel.East.Raw, ground.East + streetHalfWidthTiles);
        int eastEdge = Min(parcel.East.Raw + parcel.Wide.Raw, ground.East + ground.Wide - streetHalfWidthTiles);
        int south = Max(parcel.North.Raw, ground.North + streetHalfWidthTiles);
        int top = Min(parcel.North.Raw + parcel.Deep.Raw, ground.North + ground.Deep - streetHalfWidthTiles);

        int wide = eastEdge - west - ForecourtTiles;
        int north = south + StallBandTiles;

        if (wide < 1 || top - north < 1)
        {
            return (Tiles.Zero, Tiles.Zero, Tiles.Zero, Tiles.Zero);
        }

        int east = parcel.Face == BlockFace.West ? west + ForecourtTiles : west;

        return (new Tiles(east), new Tiles(north), new Tiles(wide), new Tiles(top - north));
    }

    /// <summary>
    /// The stall band, in the frame <see cref="StallLayout"/> places stalls in: it runs east along
    /// the pad and north toward the footprint.
    /// </summary>
    public static (int East, int North, int Along, int Toward) CarPark(
        int footprintEast, int footprintNorth, int footprintWide, BlockFace face) =>
        (face == BlockFace.West ? footprintEast - ForecourtTiles : footprintEast,
            footprintNorth - StallBandTiles, footprintWide + ForecourtTiles, StallBandTiles);

    public static StallLayout Stalls(int footprintWide, StallSizes sizes) =>
        StallLayout.Of(footprintWide + ForecourtTiles, StallBandTiles, sizes);

    private static int Max(int a, int b) => a > b ? a : b;

    private static int Min(int a, int b) => a < b ? a : b;
}
