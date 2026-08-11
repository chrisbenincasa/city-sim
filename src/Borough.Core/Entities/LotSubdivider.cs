using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

/// <summary>
/// Carves zoned land against the Street network into parcels with road frontage —
/// <b><c>02 §2.2</c>'s <i>"Lots are generated, not painted"</i></b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the mechanism three claims in the corpus were true by accident without.</b>
/// <i>Every Building is on the Road Graph by construction</i> was true because there was no Road
/// Graph; <c>adr/0025</c>'s refusal of a road-derived density cap rests on the player getting
/// <i>"dead block interiors"</i> instead, which could not happen because block interiors did not
/// exist; and the Epoch had never been driven by anything a player did.
/// </para>
/// <para>
/// <b>The unit is a block, and that is what makes <c>zone</c> a region verb without a region
/// payload.</b> Zoning a Tile zones the block it falls in, and the block is subdivided against its own
/// four faces. A block with no Street on any face yields <b>no Lots at all</b> — which is
/// <c>02 §2.2</c>'s third rule, <i>"land that cannot be given frontage stays unlotted and
/// undevelopable"</i>, and the one the tests are written against.
/// </para>
/// <para>
/// <b>Lots hang on Segments rather than filling a block, so there is no depth parameter</b>
/// (<c>adr/0078</c>). Everything not on a block face is interior and stays unlotted <em>structurally</em>
/// — not because a depth ran out — so a large block has a proportionally larger dead interior with no
/// number governing it. That is <c>02 §2.2</c>'s <i>"punish the player <b>mechanically</b> rather than
/// through a penalty number"</i> taken at its word.
/// </para>
/// <para>
/// <b>A Segment's Lots are split between the two blocks that share it by parity</b>, which is
/// odd-and-even house numbering (<see cref="Frontage.SideOf"/>). So subdividing one block claims one
/// side of each of its four faces, and the block across the street claims the other — five Lots per
/// Segment in total, which is <c>CONTEXT.md</c> → Address's own working figure and the premise the
/// ~30,000-Segment argument rests on.
/// </para>
/// </remarks>
public static class LotSubdivider
{
    /// <summary>
    /// Subdivides the block containing a Tile, painting <paramref name="zone"/> on what it carves.
    /// </summary>
    /// <returns>How many Lots were created. Zero means the land could not be given frontage.</returns>
    public static int SubdivideAt(World world, Tiles east, Tiles north, ushort zone)
    {
        ArgumentNullException.ThrowIfNull(world);

        StreetGrid streets = world.Roads.Streets;

        if (streets.BlockTiles <= 0)
        {
            return 0;
        }

        int column = Arithmetic.IntegerMath.FloorDiv(east.Raw, streets.BlockTiles);
        int row = Arithmetic.IntegerMath.FloorDiv(north.Raw, streets.BlockTiles);

        return SubdivideBlock(world, column, row, zone);
    }

    /// <summary>
    /// Subdivides one block of the lattice against its four faces.
    /// </summary>
    /// <remarks>
    /// <b>Which side of a face belongs to this block needs no geometry.</b> A horizontal Segment runs
    /// A→B eastward, so <see cref="StreetSide.Left"/> is its north side; a vertical one runs northward,
    /// so Left is its west side. The block therefore takes Left of its south face, Right of its north
    /// face, Right of its west face and Left of its east face — four constants, derived once from the
    /// endpoint order the generator and <c>CommandKind.Connect</c> both use.
    /// </remarks>
    /// <returns>How many Lots were created.</returns>
    public static int SubdivideBlock(World world, int column, int row, ushort zone)
    {
        ArgumentNullException.ThrowIfNull(world);

        StreetGrid streets = world.Roads.Streets;

        if (streets.Blocks <= 0 || column < 0 || row < 0
            || column >= streets.Blocks || row >= streets.Blocks)
        {
            return 0;
        }

        int created = 0;

        created += Face(world, streets.Horizontal(column, row), column, row, StreetAxis.East, StreetSide.Left, zone);
        created += Face(world, streets.Horizontal(column, row + 1), column, row + 1, StreetAxis.East, StreetSide.Right, zone);
        created += Face(world, streets.Vertical(column, row), column, row, StreetAxis.North, StreetSide.Right, zone);
        created += Face(world, streets.Vertical(column + 1, row), column + 1, row, StreetAxis.North, StreetSide.Left, zone);

        return created;
    }

    /// <summary>
    /// Lays one block face's Lots — the Lots on <paramref name="side"/> of one Segment.
    /// </summary>
    /// <remarks>
    /// <b>A claimed side is skipped rather than re-laid</b>, which is what makes re-subdivision
    /// preserve what already stands. The claim is per <c>(Segment, side)</c> and is derived from the
    /// Lots themselves, so a side whose Lots were deleted is free again with nothing having to
    /// remember to release it.
    /// </remarks>
    private static int Face(
        World world, int segment, int column, int row, StreetAxis axis, StreetSide side, ushort zone)
    {
        if (segment == Rows.NoSlot || world.Frontage.Claimed(segment, side))
        {
            return 0;
        }

        int block = world.Roads.Streets.BlockTiles;
        int perSegment = world.Rules.Lots.LotsPerSegment;
        int created = 0;

        for (int index = 0; index < perSegment; index++)
        {
            if (Frontage.SideOf(index) != side)
            {
                continue;
            }

            Tiles offset = Frontage.OffsetOf(index, perSegment, block);

            (Tiles east, Tiles north) = axis == StreetAxis.East
                ? (new Tiles((column * block) + offset.Raw), new Tiles(row * block))
                : (new Tiles(column * block), new Tiles((row * block) + offset.Raw));

            Handle<Lot> lot = world.Lots.Create(east, north, zone, side);
            int slot = world.Lots.Rows.Resolve(lot);

            // The derived frontage, written at the site that knows the Segment. World.RebuildDerived
            // recomputes exactly this from the saved position, and a test holds the two to agreement.
            world.Lots.FrontageSlot[slot] = segment + 1;
            world.Lots.FrontageOffset[slot] = offset;

            created++;
        }

        if (created > 0)
        {
            world.Frontage.Claim(segment, side);
        }

        return created;
    }

    /// <summary>
    /// Re-parcels every block the Street network can now reach, preserving everything that stands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>02 §2.2</c>: <i>"Re-subdivision happens when the street network changes, and must preserve
    /// existing Buildings — only vacant land re-parcels."</i></b> This runs after a road edit, and the
    /// preservation rule is keyed on <b>occupancy</b> rather than on frontage — which is
    /// <c>adr/0079</c>, and which <c>plans/0022</c> task 7 had the wrong way round.
    /// </para>
    /// <para>
    /// <b>Three things happen and they are deliberately asymmetric.</b> A vacant Lot that has lost its
    /// frontage is <b>freed</b> — it is land again, and re-parcels if a Street returns. An
    /// <b>occupied</b> Lot that has lost its frontage is <b>kept</b>, with no Address, and its Building
    /// stands: the consequence is milestone 5b's, where a Trip to an Address that does not exist ends
    /// <i>no route found</i>. And a face that has gained a Street is laid, which is the case a
    /// subdivider that only ever ran once could never reach.
    /// </para>
    /// </remarks>
    /// <returns>How many Lots were created, and how many were freed.</returns>
    public static (int Created, int Freed) Resubdivide(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        int freed = 0;

        // The frontage columns and the claim mask are already current — the road edit rebuilt them —
        // so a Lot reading as unfronted here has genuinely lost its Street.
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)
                || world.Lots.HasFrontage(slot)
                || !world.Lots.IsVacant(slot))
            {
                continue;
            }

            world.Lots.Rows.Free(world.Lots.Rows.At(slot));
            freed++;
        }

        if (freed > 0)
        {
            // Freeing releases claims, and the mask is derived from the Lots rather than maintained,
            // so it is recomputed before anything reads it to decide what to lay.
            world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
        }

        return (Relot(world), freed);
    }

    /// <summary>
    /// Re-lays every zoned block face that now has a Street and no Lots.
    /// </summary>
    /// <remarks>
    /// <b>A block's Zone is read off the Lots that survived on it</b>, because land does not carry a
    /// permission set of its own — <c>02 §2.2</c> puts the Zone on the Lot. So a block that was zoned
    /// and then lost <em>every</em> Lot has forgotten it was zoned, and a Street run back through it
    /// yields nothing until the player zones again. That is a real limitation and it is named here
    /// rather than hidden: the fix is a per-Tile zone layer, which is <c>02 §2.1</c>'s <i>Tile: zone
    /// designation</i> and which nothing has ever built.
    /// </remarks>
    private static int Relot(World world)
    {
        int created = 0;

        // The slot count is read once, before anything is created. Lots laid by this loop are
        // themselves fronted and would otherwise be revisited — terminating, because the claim mask
        // refuses a second lay, but only after walking the table again for every block it touched.
        int before = world.Lots.Rows.SlotCount;

        for (int slot = 0; slot < before; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)
                || !world.Lots.HasFrontage(slot)
                || !BlockOf(world, slot, out int column, out int row))
            {
                continue;
            }

            created += SubdivideBlock(world, column, row, world.Lots.Zone[slot]);
        }

        return created;
    }

    /// <summary>
    /// Which block a Lot belongs to — <b>which is not the block its coordinates floor into</b>.
    /// </summary>
    /// <remarks>
    /// <b>A Segment borders two blocks and the side is what decides.</b> A Lot on the north side of a
    /// horizontal Street belongs to the block above it; one on the south side belongs to the block
    /// below, whose row index is one lower. Flooring the Lot's own position answers the first case and
    /// silently gets the second wrong — and the failure is invisible, because the block it names is a
    /// real neighbouring block that will simply be subdivided instead.
    /// </remarks>
    private static bool BlockOf(World world, int slot, out int column, out int row)
    {
        StreetGrid streets = world.Roads.Streets;
        int block = streets.BlockTiles;

        column = 0;
        row = 0;

        if (block <= 0)
        {
            return false;
        }

        int east = world.Lots.East[slot].Raw;
        int north = world.Lots.North[slot].Raw;
        var side = (StreetSide)world.Lots.Side[slot];

        column = Arithmetic.IntegerMath.FloorDiv(east, block);
        row = Arithmetic.IntegerMath.FloorDiv(north, block);

        if (north == row * block)
        {
            // A horizontal Street. Left is its north side, so the block above; Right is the one below.
            if (side == StreetSide.Right)
            {
                row--;
            }
        }
        else
        {
            // A vertical Street. Right is its east side, so this block; Left is the one to the west.
            if (side == StreetSide.Left)
            {
                column--;
            }
        }

        return column >= 0 && row >= 0 && column < streets.Blocks && row < streets.Blocks;
    }
}
