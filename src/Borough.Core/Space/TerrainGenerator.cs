using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// Places the five <see cref="TerrainKind"/>s across the map, from the <see cref="WorldKey"/> alone.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0158</c>, milestone 24 task 2. <b>A world-creation pass of its own</b>, called from
/// <see cref="Entities.SyntheticCity.PopulateInto"/> between the already-populated refusal and
/// <c>LayLand</c> (<c>plans/0042</c> decision 3) — terrain goes first because it is the ground, and
/// <b>not because anything downstream reads it</b>: roads do not avoid water (<c>adr/0021</c>),
/// Woodland is not an obstacle, and buildable grade does not ship (<c>adr/0157</c>).
/// </para>
/// <para>
/// ⚠ <b>Height is computed here and stored nowhere</b> (<c>adr/0157</c>, and <c>adr/0021</c> as
/// amended: <em>terrain height is not state</em>). It lives in a local array for the length of this
/// call and dies with it. What survives is one <see cref="TerrainKind"/> per Cell.
/// </para>
/// <para>
/// <b>It authors no number, and that is a constraint rather than a boast.</b> Every tuning number in
/// this project is Ruleset data (<c>adr/0015</c>) and every hash-bearing one needs a named ratifier
/// on the day it is written (<c>adr/0052</c>); a generator whose shape came from constants in this
/// file would be both. So every quantity below is <b>derived</b> — the octave count from the map
/// being a power of two Cells across, the amplitudes from the octave doubling, and the five bands
/// from there being five terrain types. ⚠ <b>The Ruleset does not shape this pass at all</b>, which
/// is what <see cref="Rules.TerrainRuleset.Kinds"/> relies on when it refuses a file that prices only
/// some of the ground.
/// </para>
/// <para>
/// <b>There is no already-laid refusal, unlike <see cref="RoadGenerator.LayInto"/>, and the
/// difference is the hazard rather than the diligence.</b> Laying roads twice <em>appends</em> to a
/// standing graph, so the second call corrupts what the first built. This pass writes every Cell
/// unconditionally from the key, so running it twice on one world produces the identical map — there
/// is nothing for a guard to protect. The world-creation contract is still enforced, one level up, by
/// <c>SyntheticCity.RefuseIfPopulated</c>.
/// </para>
/// </remarks>
public static class TerrainGenerator
{
    /// <summary>
    /// Writes every Cell's terrain type. <b>A pure function of <paramref name="key"/>.</b>
    /// </summary>
    /// <param name="terrain">The dense per-Cell table. Every row is overwritten.</param>
    /// <param name="key">The world key, and the only input. The same key gives the same map.</param>
    public static void LayInto(TerrainCellTable terrain, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(terrain);

        // The field is ValueNoise's and the BANDING below is this pass's -- see that class on why the
        // two shipped callers read the same noise oppositely.
        int[] height = ValueNoise.Field(key, PurposeTag.TerrainType);

        // The band edges come from the range this key actually produced rather than from the range
        // the sum COULD produce, and the difference is what makes the pass self-normalising. A sum of
        // uniforms is bell-shaped, so fifths of the theoretical range would put nearly every Cell in
        // the middle band and the two tails might be empty on a given key -- "all five types exist"
        // would then be a property of the seed. Against the realised range the lowest Cell and the
        // highest Cell are in the outer bands by construction, whatever the key.
        int low = height[0];
        int high = height[0];

        for (int cell = 1; cell < height.Length; cell++)
        {
            if (height[cell] < low) { low = height[cell]; }
            if (height[cell] > high) { high = height[cell]; }
        }

        // Five bands of equal WIDTH, not equal area, and the distinction is the whole shape of the
        // map. Equal area would make each type a fifth of the world -- a fifth of it rock, which is
        // a choice nobody made. Equal width against a bell-shaped field leaves the middle band
        // holding most of the Cells and the outer two holding few, so `Ordinary` is most of the map
        // and rock and marsh are uncommon BY CONSTRUCTION rather than by a share anybody authored.
        int span = high - low;
        int first = low + IntegerMath.RoundDiv(span, 5);
        int second = low + IntegerMath.RoundDiv(span * 2, 5);
        int third = low + IntegerMath.RoundDiv(span * 3, 5);
        int fourth = low + IntegerMath.RoundDiv(span * 4, 5);

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                int at = height[CellGrid.Index(new Cells(east), new Cells(north))];

                // Ordered along the height axis, low to high, and the ordering is the design content
                // rather than a tuning: water collects in the lowest ground, alluvium is deposited
                // just above it, soil thins as the ground rises and the highest ground is bare. It is
                // adr/0022's own rock-and-floodplain pairing laid out on one axis.
                TerrainKind kind =
                    at < first ? TerrainKind.Marsh
                    : at < second ? TerrainKind.Floodplain
                    : at < third ? TerrainKind.Ordinary
                    : at < fourth ? TerrainKind.ThinSoil
                    : TerrainKind.Rock;

                terrain.Set(new Cells(east), new Cells(north), kind);
            }
        }
    }
}
