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
/// 🔴 <b>THE DEAD INTERIOR IS NOW A PROPERTY OF ONE PATTERN AND NOT OF EVERY BLOCK.</b> This
/// paragraph read <em>"a large block has a proportionally larger dead interior with no number
/// governing it"</em>, unconditionally, and <c>plans/0012</c> had already filed that as Cause 4: ***a
/// response identical whatever the player does is a constant, not a punishment.*** <c>plans/0053</c>
/// step 3 gives it a lever. <see cref="BlockPattern.Detached"/> keeps the leftover ground, where it is
/// <em>correct</em> — back gardens do not meet — and the other two tile their block exactly.
/// </para>
/// <para>
/// <b><c>adr/0078</c> is untouched and Lots still hang on Segments.</b> What it refused is an
/// <b>authored</b> depth key, and there still is not one: a parcel's depth is derived from
/// <c>block_tiles</c> and <c>lots_per_segment</c> by <see cref="BlockPatterns.StripTiles"/>, which are
/// two keys that were already there.
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
    /// How much of a Segment's length at each junction belongs to the cross street, in Tiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE FOUR FACES OF A BLOCK USED TO CLAIM THE CORNER GROUND FOUR TIMES.</b> A Lot is an
    /// <b>address</b> and owns no ground (<c>adr/0078</c>), so nothing here noticed: the face at the
    /// south and the face at the west both laid a Lot beside the same junction, both were legal, and
    /// both were correct as <i>addresses</i>. What has no answer is ***which of them the LAND under
    /// that junction belongs to***, and a shell that has to put a Building somewhere has to invent
    /// one. Two inventions landed on one patch (<c>plans/0049</c> <b>F21</b>).
    /// </para>
    /// <para>
    /// ⚠ <b>SO ONE PAIR OF FACES TAKES THE CORNERS AND THE OTHER YIELDS</b>, which is what a real
    /// corner does — the corner building belongs to one street, and the cross street's terrace begins
    /// after it. <b>East–west keeps, north–south yields.</b> The choice of pair is arbitrary and is
    /// recorded as arbitrary; what is not arbitrary is that <i>some</i> rule must exist, because a
    /// patch of ground cannot carry two Buildings.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>THIS SAID THE RESERVATION WAS NOT A DEPTH, AND <c>plans/0053</c> STEP 3 FOUND THAT IT
    /// IS.</b> The paragraph read <em>"a depth is what the corner is really made of, and this class has
    /// none to offer"</em> — and the quantity it was standing in for was the same quantity all along.
    /// The formula now lives in <see cref="BlockPatterns.StripTiles"/> and this delegates, because a
    /// corner reservation and a parcel's depth written apart are two things that drift.
    /// <c>adr/0078</c> is untouched: what it refused is an <b>authored</b> depth, and this is a
    /// consequence of two keys that already exist.
    /// </para>
    /// <para>
    /// ⚠ <b>The quarter-block cap is what keeps a coarse Ruleset from yielding its whole face.</b> At
    /// <c>lots_per_segment = 1</c> a Lot's frontage <i>is</i> the block, and without the cap the
    /// north–south faces would carry nothing at all.
    /// </para>
    /// </remarks>
    internal static int CornerTiles(int blockTiles, int lotsPerSegment) =>
        BlockPatterns.StripTiles(blockTiles, lotsPerSegment);

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

        // The block remembers it was zoned, BEFORE anything is carved and whether or not anything is.
        // plans/0053 step 1: a block with no Street on any face yields no Lots, and the whole point of
        // recording it here is that the intent survives that -- so a Street laid later finds land that
        // knows what it was painted for. Zoning land the network cannot reach is no longer a command
        // the world forgets.
        int blockSlot = world.ZoneBlock(column, row, zone);

        // plans/0053 step 3. The block's own pattern, and Detached where there is no row to read one
        // off -- which is the shape the subdivider had hard-coded before patterns existed, so a world
        // that never chooses one carves exactly what it carved yesterday.
        BlockPattern pattern = blockSlot == Rows.NoSlot
            ? BlockPattern.Detached
            : (BlockPattern)world.Blocks.Pattern[blockSlot];

        return Carve(world, pattern, column, row, zone);
    }

    /// <summary>
    /// Lays every Lot one pattern yields on one block, skipping face-sides that already carry Lots.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>plans/0053</c> step 3, and it is a rewrite rather than a branch.</b> The four faces used
    /// to be four calls with their sides written out as constants, which is a partition with one shape
    /// compiled into it. <see cref="BlockPatterns.Carve"/> is the partition now; this walks what it
    /// produced and turns each parcel into an Address.
    /// </para>
    /// <para>
    /// <b>The per-<c>(Segment, side)</c> claim is unchanged and is still what makes re-subdivision
    /// preserve what stands.</b> It is tested once per face, before that face's first parcel, because
    /// a claim is a property of the face and not of a Lot.
    /// </para>
    /// <para>
    /// ⚠ <b>A parcel's ground is computed and then dropped.</b> Nothing reads it yet — the footprint
    /// that will is step 5 — and it is computed here anyway because the Address and the ground behind
    /// it have to come out of one function or they can disagree.
    /// </para>
    /// </remarks>
    private static int Carve(World world, BlockPattern pattern, int column, int row, ushort zone)
    {
        StreetGrid streets = world.Roads.Streets;

        int blockTiles = streets.BlockTiles;
        int perSegment = world.Rules.Lots.LotsPerSegment;
        int ceiling = BlockPatterns.Ceiling(perSegment);

        if (ceiling <= 0)
        {
            return 0;
        }

        // TripEngine's idiom, and for its reason: lots_per_segment is Ruleset data bounded only by
        // block_tiles, so a coarse world must not put an unbounded frame on the stack.
        Span<Parcel> parcels = ceiling <= 64 ? stackalloc Parcel[64] : new Parcel[ceiling];

        int count = BlockPatterns.Carve(pattern, column, row, blockTiles, perSegment, parcels);
        int created = 0;

        // The face being laid, and whether this carve has put anything on it. Carve returns parcels
        // in face order, so a change of face is the boundary at which the previous one is closed.
        var face = (BlockFace)byte.MaxValue;
        StreetSide side = StreetSide.Left;
        int segment = Rows.NoSlot;
        bool skipping = true;
        int onFace = 0;

        for (int i = 0; i < count; i++)
        {
            Parcel parcel = parcels[i];

            if (parcel.Face != face)
            {
                Close(world, segment, side, onFace);

                face = parcel.Face;
                side = parcel.Side;
                segment = SegmentOf(streets, face, column, row);
                skipping = segment == Rows.NoSlot || world.Frontage.Claimed(segment, side);
                onFace = 0;
            }

            if (skipping)
            {
                continue;
            }

            (Tiles east, Tiles north) = parcel.Address(column, row, blockTiles);

            Handle<Lot> lot = world.Lots.Create(east, north, zone, side);
            int slot = world.Lots.Rows.Resolve(lot);

            // The derived frontage, written at the site that knows the Segment. World.RebuildDerived
            // recomputes exactly this from the saved position, and a test holds the two to agreement.
            world.Lots.FrontageSlot[slot] = segment + 1;
            world.Lots.FrontageOffset[slot] = parcel.Offset;

            created++;
            onFace++;
        }

        Close(world, segment, side, onFace);

        return created;
    }

    /// <summary>Records that a face's side now carries Lots, if this carve put any there.</summary>
    private static void Close(World world, int segment, StreetSide side, int onFace)
    {
        if (onFace <= 0 || segment == Rows.NoSlot)
        {
            return;
        }

        world.Frontage.Claim(segment, side);

        // Once per face rather than once per Lot: the zoned draw space is rebuilt whole, so what a
        // writer owes it is a flag and never a maintenance step.
        world.LotsAdmitting.Invalidate();
    }

    /// <summary>Which Segment a block's face is, or <see cref="Rows.NoSlot"/> if there is none.</summary>
    private static int SegmentOf(StreetGrid streets, BlockFace face, int column, int row) => face switch
    {
        BlockFace.South => streets.Horizontal(column, row),
        BlockFace.North => streets.Horizontal(column, row + 1),
        BlockFace.West => streets.Vertical(column, row),
        _ => streets.Vertical(column + 1, row),
    };

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
            // so it is recomputed before anything reads it to decide what to lay. The zoned draw
            // space is derived the same way and lost Lots the same way, so it is invalidated here.
            world.LotsAdmitting.Invalidate();

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

        // ⚠ THIS WALKS BLOCKS AND USED TO WALK LOTS, which is plans/0053 step 1's whole point. The
        // old loop asked every fronted Lot which block it belonged to and re-subdivided that block
        // with the Lot's own Zone -- so a block that had lost EVERY Lot was invisible to it and had
        // forgotten it was ever zoned. This method's remarks named that as a real limitation and
        // pointed at a per-Tile zone layer as the fix; a per-BLOCK row is the cheaper one, and it is
        // the unit the verb already acts on.
        //
        // The slot count is read once, before anything is created. SubdivideBlock calls ZoneBlock,
        // which allocates into this very table when a square has no row -- so a live count read
        // inside the condition would grow under the loop. Every row appended by this pass is one
        // already being visited, so stopping at the count taken on the way in loses nothing.
        int before = world.Blocks.Rows.SlotCount;

        for (int slot = 0; slot < before; slot++)
        {
            if (!world.Blocks.Rows.IsLive(slot))
            {
                continue;
            }

            created += SubdivideBlock(
                world,
                world.Blocks.LatticeColumn[slot],
                world.Blocks.LatticeRow[slot],
                world.Blocks.Zone[slot]);
        }

        return created;
    }

}
