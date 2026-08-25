using Borough.Core.Arithmetic;
using Borough.Core.Determinism;

namespace Borough.Core.Space;

/// <summary>
/// Places Woodland across the map, from the <see cref="WorldKey"/> alone.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0159</c>, milestone 24 task 8a. <b>A world-creation pass of its own</b>, run beside
/// <see cref="TerrainGenerator.LayInto"/> and — like it — <b>not because anything downstream reads
/// it</b>. <c>adr/0090</c> closes the generator's remit around <em>terrain, Woodland, hazard regions
/// and the Outside Connections</em>, and this is the second of those four.
/// </para>
/// <para>
/// <b>It authors no number, which is the same constraint <see cref="TerrainGenerator"/> works
/// under.</b> The octave ladder is derived from the map being a power of two Cells across, the
/// amplitudes from the octave doubling, and the scale from <see cref="CellGrid.TilesInCell"/> — the
/// endpoint <em>is</em> the coefficient, which is <see cref="MapLayers.Fertility"/>'s Sealing term
/// arriving a second time. ⚠ <b>The Ruleset does not shape this pass at all</b>, so a file that
/// states no <c>[terrain]</c> still gets forest.
/// </para>
/// <para>
/// <b>It scales against <see cref="ValueNoise.Ceiling"/> rather than against the range the key
/// realised, and the difference from terrain is the design content.</b> Terrain bands against its
/// realised range so that <em>all five types exist</em> cannot become a property of the seed.
/// Woodland must do the opposite: <c>adr/0022</c> requires that <em>"a heavily forested seed is a
/// Materials-rich, farmland-poor start"</em>, so how much forest a world has is exactly the thing
/// that should vary from key to key. Self-normalising here would make every world equally wooded and
/// delete that sentence. ***The same noise, read two ways, on purpose.***
/// </para>
/// <para>
/// <b>Nothing is subtracted for Sealing here and nothing needs to be.</b> This pass runs before any
/// ground is built on — <c>SyntheticCity</c> lays roads and Buildings afterwards — so every Cell is
/// unsealed when it is written, and <see cref="MapLayers.Seal"/> is what takes Woodland back as the
/// city arrives. ⚠ <b>The ordering is load-bearing</b>: run this after <c>LayLand</c> and it would
/// plant forest on top of the roads.
/// </para>
/// <para>
/// <b>There is no already-laid refusal</b>, for <see cref="TerrainGenerator"/>'s reason: this pass
/// writes every Cell unconditionally from the key, so running it twice produces the identical map and
/// there is nothing for a guard to protect. The world-creation contract is enforced one level up by
/// <c>SyntheticCity.RefuseIfPopulated</c>.
/// </para>
/// </remarks>
public static class WoodlandGenerator
{
    /// <summary>Writes one Woodland Tile count per Cell.</summary>
    /// <param name="woodland">The table to fill. Every row is written.</param>
    /// <param name="key">The world's key. The only source of variation.</param>
    public static void LayInto(WoodlandCellTable woodland, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(woodland);

        int[] field = ValueNoise.Field(key, PurposeTag.Woodland);

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                int at = field[CellGrid.Index(new Cells(east), new Cells(north))];

                // A proportion of the Cell's own Tile count, rounded rather than truncated because a
                // truncation biases every Cell the same way -- MapLayers.Fertility's rule for the
                // identical shape. `long` because the product reaches 65,025 x 1,024, which is 66.6M
                // and fits an int, but only just; the wider intermediate costs nothing and removes
                // the question.
                int tiles = (int)IntegerMath.RoundDiv(
                    (long)at * CellGrid.TilesInCell, ValueNoise.Ceiling);

                woodland.Lay(new Cells(east), new Cells(north), tiles);
            }
        }
    }
}
