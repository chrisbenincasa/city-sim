using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// <b>How much of a footprint is floor</b> — the daylight bound on a Building's plan.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>A footprint was floor and that made a big block into one enormous Building</b>
/// (<c>plans/0053</c>). Occupancy divides floor area, and floor area was
/// <c>wide × deep × storeys</c> over the whole rectangle — so a parcel that grew with
/// <c>block_tiles</c> grew a Building's capacity with its <em>area</em>. On
/// <c>rulesets/severance.toml</c>, whose block is 256 Tiles rather than 32, a detached parcel is
/// 48 × 48 and its Building held about 150 Households. ***Four hundred people lived in two
/// buildings***, and the world stopped severing because there was nothing left to sever.
/// </para>
/// <para>
/// <b>The bound is DAYLIGHT and it is not a tuning number.</b> A habitable room needs a window and
/// reaches about 7 m from one; a plan two rooms deep with circulation between them is about 16 m
/// across before the middle of it is dark. A Tile is <c>CellGrid.MetresPerTile</c> = 4 m, so
/// <see cref="DaylightTiles"/> is <b>4</b>, and no point in a Building may be further than that
/// from an outside wall. ⚠ <b>A design constant of the same kind the Cell is</b> — it comes from
/// the Tile's size and from how far light travels, so a Ruleset key would be a designer knob on a
/// fact. <c>adr/0015</c> asks that a number a designer would want to change is Ruleset data; this
/// is not one.
/// </para>
/// <para>
/// <b>What a plan too deep for that becomes is a RING, which is a courtyard building.</b> A
/// rectangle whose shorter side is within twice the daylight depth is solid; a deeper one keeps a
/// perimeter of that thickness and loses its middle. ⚠ <b>The middle is not floor and it is not
/// garden either</b> — this bounds the <em>capacity</em>; what Sealing takes is still the whole
/// footprint, because a courtyard is enclosed ground rather than open country.
/// </para>
/// <para>
/// ⚠ <b>It changes the shipped lattice only for slabs.</b> At <c>block_tiles = 32</c> a detached
/// parcel is 6 × 6 and a terrace 6 × 16, both under the bound and both unchanged; a slab's 16 × 16
/// keeps 192 Tiles of its 256. ***So the anchor the capacity rates were derived from does not
/// move***, and what moves is the form that was implausible.
/// </para>
/// </remarks>
public static class BuildingPlan
{
    /// <summary>
    /// <b>How far floor may lie from an outside wall</b>, in Tiles — 4, which is 16 m.
    /// </summary>
    public const int DaylightTiles = 4;

    /// <summary>
    /// The floor one storey of a <paramref name="wide"/> × <paramref name="deep"/> footprint carries.
    /// </summary>
    public static int HabitableTiles(Tiles wide, Tiles deep) =>
        HabitableTiles(wide.Raw, deep.Raw);

    /// <summary>
    /// The floor one storey of a <paramref name="wide"/> × <paramref name="deep"/> footprint carries.
    /// </summary>
    public static int HabitableTiles(int wide, int deep)
    {
        if (wide < 1 || deep < 1)
        {
            return 0;
        }

        int hollowWide = wide - (2 * DaylightTiles);
        int hollowDeep = deep - (2 * DaylightTiles);

        if (hollowWide < 1 || hollowDeep < 1)
        {
            return wide * deep;
        }

        return (wide * deep) - (hollowWide * hollowDeep);
    }
}
