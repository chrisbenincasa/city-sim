using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

public readonly record struct ResidentialPlots(
    int FrontageTiles, int DepthTiles, int HouseWidthTiles, int HouseDepthTiles, int HouseStoreys)
{
    private static int Min(int a, int b) => a < b ? a : b;

    public bool Runs => FrontageTiles > 0;

    public bool Applies(BlockPattern pattern) => Runs
        && pattern is BlockPattern.Detached or BlockPattern.Perimeter;

    public int Ceiling(BlockGround ground) =>
        2 * (IntegerMath.FloorDiv(ground.Wide, FrontageTiles)
            + IntegerMath.FloorDiv(ground.Deep, FrontageTiles) + 2);

    public int Carve(BlockGround ground, int streetInset, Span<Parcel> into)
    {
        int written = 0;
        int availableDepth = ground.Deep - 2 * streetInset;
        int depth = Min(DepthTiles, IntegerMath.FloorDiv(availableDepth, 2));
        if (depth <= 0) return 0;
        for (BlockFace face = BlockFace.South; face <= BlockFace.East; face++)
        {
            bool horizontal = face is BlockFace.South or BlockFace.North;
            int low = streetInset + (horizontal ? 0 : depth);
            int high = ground.Along(face) - streetInset - (horizontal ? 0 : depth);
            int reach = high - low;
            int groups = IntegerMath.FloorDiv(reach, FrontageTiles);
            if (groups <= 0) continue;
            int faceDepth = horizontal ? depth
                : Min(DepthTiles, IntegerMath.FloorDiv(ground.Wide - 2 * streetInset, 2));
            if (faceDepth <= 0) continue;
            for (int group = 0; group < groups; group++)
            {
                int from = low + IntegerMath.FloorDiv(group * reach, groups);
                int end = low + IntegerMath.FloorDiv((group + 1) * reach, groups);
                int offset = IntegerMath.FloorDiv(from + end, 2);
                int east = ground.East + (horizontal ? from
                    : face == BlockFace.West ? streetInset : ground.Wide - streetInset - faceDepth);
                int north = ground.North + (!horizontal ? from
                    : face == BlockFace.South ? streetInset : ground.Deep - streetInset - faceDepth);
                into[written++] = new Parcel(face, BlockPatterns.SideOf(face), new Tiles(offset),
                    new Tiles(east), new Tiles(north),
                    new Tiles(horizontal ? end - from : faceDepth),
                    new Tiles(horizontal ? faceDepth : end - from));
            }
        }
        return written;
    }
}
