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

        // plans/0053 steps 3 and 4. SELECTION HAPPENS AT THE FIRST CARVE AND NEVER AGAIN: a block
        // that has been carved keeps what it was carved with, whatever its band says now, because the
        // pattern is a historical fact about conditions that are gone. A block that has not takes what
        // its band asks for. In a Ruleset with no [[band]] that is Detached on every block, which is
        // the shape the subdivider had hard-coded before patterns existed.
        BlockPattern pattern = world.PatternOf(blockSlot, out bool chosen);

        if (!chosen && blockSlot != Rows.NoSlot)
        {
            pattern = BlockPatterns.ForBand(
                world.Blocks.Band[blockSlot],
                world.Rules.Bands.Length,
                world.Roads.Streets.BlockTiles,
                world.Rules.Lots.LotsPerSegment,
                world.Key,
                column,
                row,
                world.Rules.Lots.PatternSpread);

            world.PatternBlock(column, row, pattern);
        }

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

        int count = BlockPatterns.Carve(
            world.Key, pattern, column, row, blockTiles, perSegment, parcels);
        int created = 0;

        // A property of the BLOCK and hoisted out of the loop, which is what it is: every Building
        // on one block stands the same number of storeys before the per-parcel draw.
        int patternStoreys =
            BlockPatterns.Storeys(pattern, blockTiles, perSegment, world.Rules.Lots.StoreysPerRung);

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

            // plans/0052 stage 1. The ground, written beside the Address by the one function that
            // produced both -- which is the whole reason Carve returns a rectangle rather than an
            // offset. World.RebuildParcels recomputes exactly this from the block's saved pattern.
            world.Lots.ParcelEast[slot] = parcel.East;
            world.Lots.ParcelNorth[slot] = parcel.North;
            world.Lots.ParcelWide[slot] = parcel.Wide;
            world.Lots.ParcelDeep[slot] = parcel.Deep;

            // And the footprint, which is the parcel inset by four drawn setbacks. Written here for
            // the same reason the parcel is -- so a Lot is never live with ground and no building
            // line on it -- and recomputed identically by World.RebuildParcels.
            (Quantities.Tiles footEast, Quantities.Tiles footNorth, Quantities.Tiles footWide,
                Quantities.Tiles footDeep) = world.Rules.Lots.Footprint(
                    world.Key, parcel.East, parcel.North, parcel.Wide, parcel.Deep);

            world.Lots.FootprintEast[slot] = footEast;
            world.Lots.FootprintNorth[slot] = footNorth;
            world.Lots.FootprintWide[slot] = footWide;
            world.Lots.FootprintDeep[slot] = footDeep;
            world.Lots.Storeys[slot] = Rules.LotRuleset.StoreysOn(
                world.Key, parcel.East, parcel.North, patternStoreys,
                world.Rules.Lots.StoreysPerRung);

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
    /// <b>Re-plats one block onto the pattern its band now asks for</b> — <c>adr/0025</c>'s
    /// redevelopment, and <c>plans/0053</c> step 4.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two gates, and each answers a different question.</b> <b>Vacancy is the PERMISSION</b>:
    /// <c>02 §2.2</c> says <em>"only vacant land re-parcels"</em>, so a single standing Building stops
    /// the whole block — which is <c>adr/0025</c>'s <em>"upzoning a built block does nothing until its
    /// Buildings go, which is how redevelopment becomes a real endgame activity rather than a
    /// formality"</em>. <b>The ratchet is the TERMINATION</b>: see below.
    /// </para>
    /// <para>
    /// 🔴 <b><c>plans/0053</c> Q3 — what stops carve and re-carve oscillating — ANSWERED HERE, and the
    /// answer is a ratchet rather than hysteresis.</b> A pattern may only be replaced by one
    /// <b>further up the ladder</b>. That is monotone, it is bounded above by the ladder's length, and
    /// so it terminates in at most <c>BlockPatterns.Count</c> steps.
    /// ⚠ <b>Hysteresis was the obvious answer and it was refused</b>: a hysteresis band is a width, a
    /// width is a number, and a hash-bearing number invented to damp a mechanism is exactly what
    /// <c>adr/0052</c> asks for a ratifier for and nobody could name one.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>IT RATCHETED ON GROUND CLAIMED AND THAT PROXY BROKE THE DAY THE SET GREW.</b> Claimed
    /// ground is monotone across the first three rungs by luck rather than by construction, and the
    /// two coarse patterns are the counter-example: <see cref="BlockPattern.Courtyard"/> claims
    /// <b>880</b> Tiles of a 32-Tile block against <see cref="BlockPattern.BackToBack"/>'s
    /// <b>1,024</b>, because its middle third is a courtyard — ***so the denser form claimed less and
    /// the ratchet would have refused every intensification into it.*** ⚠ <b>The ladder itself is what
    /// the ratchet always meant</b>, and reading it through <see cref="BlockPatterns.Rung"/> removes a
    /// proxy rather than replacing one: it is still monotone, still bounded, and it no longer depends
    /// on a coincidence about areas.
    /// </para>
    /// <para>
    /// ⚠ <b>THE RATCHET AND THE SELECTION READ ONE FUNCTION AND THAT IS THE POINT OF IT BEING A
    /// FUNCTION.</b> <see cref="BlockPatterns.ForBand"/> indexes the ladder and this compares
    /// positions on it, so the two cannot disagree about which way is up — ***the disagreement that
    /// would look like upzoning a block and watching nothing happen is now unwriteable rather than
    /// merely tested for.***
    /// </para>
    /// <para>
    /// <b>It is also what a real city does.</b> Re-platting is an intensification: a block is
    /// re-divided to get more out of it. ***Nobody re-plats a block in order to use less of it*** —
    /// land that stops being wanted is abandoned, not re-surveyed into bigger lots, and abandonment is
    /// a different mechanism with a different name.
    /// </para>
    /// <para>
    /// ⚠ <b>THE CONSEQUENCE IS THAT A BLOCK NEVER MOVES BACK DOWN THE LADDER.</b> A slab never
    /// returns to a terrace and a terrace never returns to a suburb. <b>That is a real limit and it is
    /// the ratchet working rather than failing</b> — land that stops being wanted is <em>abandoned</em>,
    /// not re-surveyed into bigger lots, and abandonment is a different mechanism with a different
    /// name. ⚠ <b>It is also now strictly weaker than what stood here</b>: two patterns at the same
    /// intensity could never replace each other under either rule, and under this one two patterns at
    /// the same <em>area</em> can, provided the ladder separates them.
    /// </para>
    /// <para>
    /// 🔴 <b>NOTHING CALLS THIS ON THE OCCASION THAT MATTERS YET.</b> It runs from
    /// <see cref="Resubdivide"/>, so a road edit is the trigger; the occasion that ought to trigger it
    /// is <b>the block's last Building going</b>, which happens inside a Tick at
    /// <c>ZoneRuleEngine.Condemn</c> and has no hook. ***So redevelopment is available and is not
    /// scheduled***, and that is named here rather than left to be discovered.
    /// </para>
    /// </remarks>
    /// <returns>How many Lots the re-plat created. Zero means nothing was re-platted.</returns>
    public static int RecarveBlock(World world, int column, int row)
    {
        ArgumentNullException.ThrowIfNull(world);

        StreetGrid streets = world.Roads.Streets;

        if (streets.Blocks <= 0 || !world.BlockIndex.Contains(column, row))
        {
            return 0;
        }

        int blockSlot = world.BlockIndex.Slot(column, row);

        if (blockSlot == Space.BlockResidency.NotResident)
        {
            return 0;
        }

        BlockPattern carved = world.PatternOf(blockSlot, out bool chosen);

        if (!chosen)
        {
            return 0;
        }

        int blockTiles = streets.BlockTiles;
        int perSegment = world.Rules.Lots.LotsPerSegment;

        BlockPattern wanted = BlockPatterns.ForBand(
            world.Blocks.Band[blockSlot], world.Rules.Bands.Length, blockTiles, perSegment,
            world.Key, column, row, world.Rules.Lots.PatternSpread);

        if (wanted == carved)
        {
            return 0;
        }

        // The ratchet, and it reads the ladder rather than the area -- see the remarks, which carry
        // the counter-example that retired the area. Strictly greater, so a sideways move is not a
        // re-plat and a move down the ladder is refused outright.
        if (BlockPatterns.Rung(wanted, blockTiles, perSegment)
            <= BlockPatterns.Rung(carved, blockTiles, perSegment))
        {
            return 0;
        }

        // 02 §2.2's permission, and it is the whole block rather than the Lot: a re-plat moves every
        // boundary on the block, so one standing Building refuses all of it.
        if (!Vacant(world, column, row))
        {
            return 0;
        }

        Clear(world, column, row);

        world.PatternBlock(column, row, wanted);

        // The claim mask is derived from the Lots, so it has to be recomputed before anything reads it
        // to decide what to lay -- otherwise the faces this just emptied still read as claimed.
        world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
        world.LotsAdmitting.Invalidate();

        return SubdivideBlock(world, column, row, world.Blocks.Zone[blockSlot]);
    }

    /// <summary>Whether every Lot on a block is vacant, which is what lets it re-plat.</summary>
    private static bool Vacant(World world, int column, int row)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && !world.Lots.IsVacant(slot)
                && Frontage.BlockOf(
                    world.Roads.Streets, world.Lots.East[slot], world.Lots.North[slot],
                    (StreetSide)world.Lots.Side[slot], out int at, out int on)
                && at == column && on == row)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Frees every Lot on a block, which a re-plat does before it lays new ones.</summary>
    /// <remarks>
    /// <b>Every Lot, not every vacant one.</b> <see cref="Vacant"/> has already refused the block if
    /// anything stands on it, so this cannot destroy an Address a Building is on — and freeing only
    /// the vacant ones would leave the old boundaries half in place, which is neither pattern.
    /// </remarks>
    private static void Clear(World world, int column, int row)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && Frontage.BlockOf(
                    world.Roads.Streets, world.Lots.East[slot], world.Lots.North[slot],
                    (StreetSide)world.Lots.Side[slot], out int at, out int on)
                && at == column && on == row)
            {
                world.Lots.Rows.Free(world.Lots.Rows.At(slot));
            }
        }
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
            // so it is recomputed before anything reads it to decide what to lay. The zoned draw
            // space is derived the same way and lost Lots the same way, so it is invalidated here.
            world.LotsAdmitting.Invalidate();

            world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
        }

        return (Relot(world), freed);
    }

    /// <summary>
    /// Re-lays every zoned block face that now has a Street and no Lots, and re-plats the blocks
    /// entitled to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ✅ <b>THE LIMITATION THIS PARAGRAPH USED TO NAME IS DISCHARGED.</b> It read: <em>"a block's
    /// Zone is read off the Lots that survived on it… so a block that was zoned and then lost every
    /// Lot has forgotten it was zoned"</em>, and pointed at a per-Tile zone layer as the fix.
    /// <c>plans/0053</c> step 1 took the cheaper one — <b>a per-block row</b>, which is the unit the
    /// verb already acts on — and this walks those rows.
    /// </para>
    /// <para>
    /// <b><see cref="RecarveBlock"/> runs first, and the order carries the reason.</b> A re-plat frees
    /// a block's Lots and lays new ones, so a re-lay of the same block afterwards would find its faces
    /// claimed and do nothing. ⚠ <b>A road edit is the only occasion that reaches this</b>, and it is
    /// not the occasion redevelopment wants — see <see cref="RecarveBlock"/>'s last paragraph.
    /// </para>
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

            int column = world.Blocks.LatticeColumn[slot];
            int row = world.Blocks.LatticeRow[slot];

            // plans/0053 step 4. A re-plat that fires lays the block's whole set, so the re-lay below
            // finds every face claimed and adds nothing -- which is why the two compose rather than
            // needing an either-or.
            created += RecarveBlock(world, column, row);
            created += SubdivideBlock(world, column, row, world.Blocks.Zone[slot]);
        }

        return created;
    }

}
