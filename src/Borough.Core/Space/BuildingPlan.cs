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
/// <see cref="DaylightTiles"/> is <b>4</b>: ***a WING is four Tiles thick, lit from both of its
/// faces.*** ⚠ <b>A design constant of the same kind the Cell is</b> — it comes from the Tile's size
/// and from how far light travels, so a Ruleset key would be a designer knob on a fact.
/// <c>adr/0015</c> asks that a number a designer would want to change is Ruleset data; this is not
/// one.
/// </para>
/// <para>
/// 🔴 <b>THAT SENTENCE READ <em>"no point may be further than <see cref="DaylightTiles"/> from an
/// outside wall"</em> UNTIL <c>plans/0053</c> step 4, AND IT DISAGREED WITH THE ARITHMETIC UNDER
/// IT BY ONE TILE.</b> A wing of thickness <c>t</c> lit from both faces has its middle at
/// <c>t/2</c> from each, so *16 m across* and *16 m away from a wall* are a factor of two apart —
/// and the code has always cut a wing of <b>4</b>, which is the first reading. ***The prose was
/// wrong and the number was right***, so this is a correction to a sentence and not to a city: no
/// hash moved for it. ⚠ <b>It is worth its paragraph because the wrong reading is the plausible
/// one</b> — it would have made the wing 5 Tiles and every capacity in the game rise, and nothing
/// but this note would have said which of the two the constant meant.
/// </para>
/// <para>
/// <b>What a plan too deep for that becomes is a RING, which is a courtyard building.</b> A
/// rectangle whose shorter side is within twice the wing thickness is solid; a deeper one keeps a
/// perimeter of that thickness and loses its middle. ⚠ <b>The middle is not floor and it is not
/// garden either</b> — this bounds the <em>capacity</em>; what Sealing takes is still the whole
/// footprint, because a courtyard is enclosed ground rather than open country.
/// </para>
/// <para>
/// 🔴 <b>A HOLE UNDER <see cref="DaylightTiles"/> ACROSS IS NOT A HOLE, and that threshold arrived
/// in step 4 because the shell had to DRAW this.</b> The arithmetic alone hollows at 9 × 9 and the
/// first holes it makes are <b>one Tile</b> wide — measured on <c>rulesets/platted.toml</c>, the
/// commonest hollow footprint was <c>14 × 9</c> with a hole of <c>6 × 1</c>. ***A four-metre gap
/// between two sixteen-metre wings is a light well and not a courtyard***, and a drawing that
/// opened it would read as a crack in the roof rather than as a space. The threshold is the wing's
/// own thickness — <b>a gap narrower than the thing bounding it is a slot rather than a place</b> —
/// so no second constant arrives.
/// </para>
/// <para>
/// ⚠ <b>It moves the capacity as well as the drawing, and it has to.</b> A 12 × 9 footprint holds
/// 108 Tiles of floor now and held 104; ***the alternative was a Building whose floor said ring and
/// whose picture said box***, which is the disagreement <c>plans/0052</c> stage 1 spent itself
/// closing one level out. <b>One function answers both</b> — <see cref="Hollow"/> — so they cannot
/// part company again.
/// </para>
/// <para>
/// 🔴 <b>AN L-PLAN IS <em>UNDESIGNED</em> RATHER THAN UNBUILT, AND THE CORNER IS WHY</b>
/// (<c>adr/0070</c>). The shape that would produce one is a Building fronting two Streets at a
/// block's corner — and a corner parcel does not hold the ground round the corner; the cross face's
/// parcel does, because a pattern is a <b>partition</b>. Drawing an arm there would put a wall on a
/// neighbour's plot, which is exactly the two-footprints defect step 2 exists to prevent.
/// <c>BlockPattern</c> already refused the mitre in the same words: ***at a real corner one street
/// wins.*** So an L needs a partition that hands a corner to one Address, which is a change to the
/// pattern set and not to this file.
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

        return Hollow(wide, deep, out int holeWide, out int holeDeep)
            ? (wide * deep) - (holeWide * holeDeep)
            : wide * deep;
    }

    /// <summary>
    /// <b>Whether this footprint is a ring, and how big its courtyard is.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE ONE FUNCTION BOTH THE CAPACITY AND THE DRAWING READ</b> (<c>plans/0053</c> step 4).
    /// <see cref="HabitableTiles"/> subtracts what this returns and <c>Borough.Godot</c> draws four
    /// wings around it, so a Building cannot count a courtyard it does not show or show one it did
    /// not count. ***That is the same defect twice already*** — the parcel against the footprint at
    /// <c>plans/0052</c> stage 1, and the height against the occupancy at step 3 — and the repair
    /// both times was one derivation with two readers rather than two derivations.
    /// </para>
    /// <para>
    /// <b>The wings are <see cref="DaylightTiles"/> thick and the hole is what is left</b>, so a
    /// footprint hollows only where the leftover is itself at least a wing thick on both axes:
    /// <c>2 × 4 + 4 = 12</c> Tiles, which is <b>48 m</b>. ⚠ <b>Below that it is SOLID and not
    /// merely undrawn</b> — see the remarks on the type.
    /// </para>
    /// <para>
    /// ⚠ <b>There is no odd Tile to arbitrate, and that is a property of measuring from the EDGES.</b>
    /// Each wing takes <see cref="DaylightTiles"/> off its own side and the hole is whatever remains,
    /// so the partition is exact for every footprint. ***A centred hole of a chosen size would have
    /// needed a rule about the remainder***, and this one cannot.
    /// </para>
    /// <para>
    /// 🔴 <b>IT BOUNDS THE CAPACITY AND SAYS NOTHING ABOUT WHETHER THE FORM IS BUILDABLE.</b> On
    /// <c>rulesets/severance.toml</c>, whose block is 256 Tiles, this returns a ring around a hole of
    /// <b>117 × 40</b> — a single Address holding a courtyard 468 m by 160 m, which is a city block
    /// and not a building. ***The floor is right and the shape is absurd***, and the cause is the
    /// pattern handing one Address a quarter of a superblock rather than anything here. Filed in
    /// <c>plans/0053</c>.
    /// </para>
    /// </remarks>
    /// <param name="wide">The footprint's extent eastward, in Tiles.</param>
    /// <param name="deep">Its extent northward, in Tiles.</param>
    /// <param name="holeWide">The courtyard's extent eastward, or zero.</param>
    /// <param name="holeDeep">The courtyard's extent northward, or zero.</param>
    /// <returns><c>true</c> where the plan is a ring.</returns>
    public static bool Hollow(int wide, int deep, out int holeWide, out int holeDeep)
    {
        holeWide = 0;
        holeDeep = 0;

        if (wide < 1 || deep < 1)
        {
            return false;
        }

        int spareWide = wide - (2 * DaylightTiles);
        int spareDeep = deep - (2 * DaylightTiles);

        if (spareWide < DaylightTiles || spareDeep < DaylightTiles)
        {
            return false;
        }

        holeWide = spareWide;
        holeDeep = spareDeep;

        return true;
    }
}
