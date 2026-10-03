using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// <b>The contact between a Lot and a Street it can take access from.</b> A Lot <em>saves</em> its
/// frontage — Segment handle, offset and side (<c>adr/0174</c>) — and this class is what writes it.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>CONTEXT.md</c> → Frontage: <i>"frontage is arithmetic, not a rule"</i></b>, and this class is
/// that sentence. Block geometry decides whether a parcel touches a street at all; nothing here
/// consults a Ruleset about policy, only about the lattice spacing and how many Lots a Segment holds.
/// </para>
/// <para>
/// <b>Four operations, and only one of them runs on a load.</b> <see cref="Attach"/> gives a
/// just-created Lot the Segment under it. <see cref="Sever"/> unfronts the Lots a bulldoze left
/// pointing at nothing. <see cref="Split"/> moves the Lots past a Segment split onto the new
/// Segment. <see cref="Rebuild"/> is the derived half — the per-Segment claim mask, and nothing else.
/// </para>
/// <para>
/// ⚠ <b>Frontage has two homes now, the Lot and the Segment</b>, so every Street edit has to migrate
/// the Lots it touches rather than letting a rebuild find them again. That is the cost
/// <c>adr/0174</c> accepts: a freeform Street has no lattice line to run the old derivation backwards
/// from, and a nearest-Segment search is ambiguous at corners and on curves.
/// </para>
/// </remarks>
public sealed class Frontage
{
    private byte[] _claimed = [];

    /// <summary>
    /// Where the <paramref name="index"/>th Lot on a Segment sits, measured from the A endpoint.
    /// </summary>
    /// <remarks>
    /// <b>Midpoints of equal shares rather than fenceposts</b>, so no Lot lands on an intersection and
    /// the spacing is symmetric about the Segment's centre. At the shipped figures — five Lots on a
    /// 32-Tile block face — that is 3, 9, 16, 22, 28.
    /// </remarks>
    public static Tiles OffsetOf(int index, int lotsPerSegment, int blockTiles) =>
        new(IntegerMath.FloorDiv(blockTiles * ((2 * index) + 1), 2 * lotsPerSegment));

    /// <summary>
    /// Which side of a Segment the <paramref name="index"/>th Lot sits on.
    /// </summary>
    /// <remarks>
    /// <b>Alternating, which is odd-and-even house numbering</b> — and that is not a coincidence
    /// dressed up as one. `CONTEXT.md` → Address says the word <em>Address</em> was chosen <i>"because
    /// a street address is literally this triple: a distance along a street plus an odd or even
    /// side"</i>. Walking a Segment and alternating is that sentence executed, and it is what splits a
    /// Segment's Lots between the two blocks that share it.
    /// </remarks>
    public static StreetSide SideOf(int index) =>
        (index & 1) == 0 ? StreetSide.Left : StreetSide.Right;

    /// <summary>Whether a Segment's Lots on this side have already been laid.</summary>
    public bool Claimed(int segmentSlot, StreetSide side) =>
        segmentSlot >= 0
        && segmentSlot < _claimed.Length
        && (_claimed[segmentSlot] & Bit(side)) != 0;

    /// <summary>Records that a Segment's Lots on this side now exist.</summary>
    public void Claim(int segmentSlot, StreetSide side)
    {
        if (segmentSlot < 0)
        {
            return;
        }

        if (segmentSlot >= _claimed.Length)
        {
            // No Math.Max — BOR0202 bans System.Math outright in the core, and a ternary is what it
            // wants instead. Doubling keeps the growth amortised while the floor keeps it correct for
            // the first claim, when the array is empty.
            int doubled = _claimed.Length * 2;

            Array.Resize(ref _claimed, doubled > segmentSlot ? doubled : segmentSlot + 1);
        }

        _claimed[segmentSlot] |= Bit(side);
    }

    /// <summary>
    /// Rebuilds the per-Segment claim mask from the Lots' saved frontage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The claim mask is rebuilt rather than maintained</b>, so that a Lot deleted by
    /// re-subdivision releases its side of a Segment without anything having to remember to say so.
    /// It is derived from the Lots, which are saved, so it survives a reload by being recomputed.
    /// </para>
    /// <para>
    /// 🔴 <b>It writes no saved state</b>, which is what lets <c>World.RebuildDerived</c> call it. A
    /// Lot whose Street is gone contributes nothing here and keeps its position, which is
    /// <c>adr/0079</c>: the Building stands and the Address becomes <see cref="Address.None"/>.
    /// </para>
    /// </remarks>
    public void Rebuild(LotTable lots)
    {
        ArgumentNullException.ThrowIfNull(lots);

        Array.Clear(_claimed);

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            int segment = lots.FrontageOn(slot);

            if (segment != Rows.NoSlot)
            {
                Claim(segment, (StreetSide)lots.Side[slot]);
            }
        }
    }

    /// <summary>
    /// Saves the lattice Segment under every Lot that has no frontage.
    /// </summary>
    /// <remarks>
    /// <b>A creation-time pass, and the one thing that gives frontage back after a re-lay.</b> It
    /// writes saved state, so nothing on the load path may call it — a Lot's frontage comes out of
    /// the save already. A Lot that already fronts something is left alone, which is what keeps a
    /// Lot on the Segment it was carved against.
    /// </remarks>
    /// <returns>How many Lots gained frontage.</returns>
    public static int Attach(LotTable lots, StreetGrid streets, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(lots);
        ArgumentNullException.ThrowIfNull(streets);
        ArgumentNullException.ThrowIfNull(segments);

        int fronted = 0;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || lots.HasFrontage(slot))
            {
                continue;
            }

            int segment = Locate(streets, lots.East[slot], lots.North[slot], out Tiles offset);

            if (segment == Rows.NoSlot || !segments.Rows.IsLive(segment))
            {
                continue;
            }

            lots.Front(slot, segments.Rows.At(segment), offset);
            fronted++;
        }

        return fronted;
    }

    /// <summary>
    /// Unfronts every Lot whose Segment has been freed under it.
    /// </summary>
    /// <remarks>
    /// <b>A severed handle already reads as no frontage</b>, so this is about what the row holds
    /// rather than about what it answers: a bulldoze is the city changing, and the state it leaves is
    /// <em>this Lot fronts nothing</em> rather than <em>this Lot fronts a Street that is gone</em>.
    /// </remarks>
    /// <returns>How many Lots lost frontage.</returns>
    public static int Sever(LotTable lots)
    {
        ArgumentNullException.ThrowIfNull(lots);

        int severed = 0;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || lots.FrontageSegment[slot].IsNone || lots.HasFrontage(slot))
            {
                continue;
            }

            lots.Unfront(slot);
            severed++;
        }

        return severed;
    }

    /// <summary>
    /// Moves the Lots past a Segment split onto the Segment the split created.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0174</c>'s split rule.</b> The original keeps its id and the A part,
    /// <paramref name="retainedTiles"/> long; a Lot sitting beyond that fronts
    /// <paramref name="created"/> at its offset minus that length. The side does not move, because
    /// both parts run in the original's A→B direction. ⚠ <b>The claim mask does not follow on its
    /// own</b> — a caller that reads it calls <see cref="Rebuild"/> afterwards.
    /// </remarks>
    /// <returns>How many Lots moved.</returns>
    public static int Split(
        LotTable lots, RoadSegmentTable segments, int original, int created, Tiles retainedTiles)
    {
        ArgumentNullException.ThrowIfNull(lots);
        ArgumentNullException.ThrowIfNull(segments);

        Handle<RoadSegment> target = segments.Rows.At(created);
        int moved = 0;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || lots.FrontageOn(slot) != original)
            {
                continue;
            }

            Tiles offset = lots.FrontageOffset[slot];

            if (offset.Raw <= retainedTiles.Raw)
            {
                continue;
            }

            lots.Front(slot, target, new Tiles(offset.Raw - retainedTiles.Raw));
            moved++;
        }

        return moved;
    }

    /// <summary>
    /// Which Street a position fronts, and how far along it — or <see cref="Rows.NoSlot"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A creation-time helper, and the lattice is its whole domain.</b> <see cref="Attach"/> is the
    /// only caller in the simulation; a Lot's frontage is read off the Lot once it has one.
    /// </para>
    /// <para>
    /// <b>A position exactly on an intersection fronts nothing</b>, and that is deliberate rather than
    /// an edge case left to fall out. `CONTEXT.md` → Address is emphatic that an Address is
    /// <i>"never a Node"</i>; a Lot at a corner would have to choose between two Segments, and the
    /// choice would be an arbitrary tie-break that the State Hash would then carry forever.
    /// </para>
    /// </remarks>
    public static int Locate(StreetGrid streets, Tiles east, Tiles north, out Tiles offset)
    {
        ArgumentNullException.ThrowIfNull(streets);

        offset = Tiles.Zero;

        int block = streets.BlockTiles;

        if (block <= 0 || east.Raw < 0 || north.Raw < 0)
        {
            return Rows.NoSlot;
        }

        int column = streets.Lattice.LineAt(east.Raw);
        int row = streets.Lattice.LineAt(north.Raw);
        int alongEast = east.Raw - streets.Lattice.EdgeOf(column);
        int alongNorth = north.Raw - streets.Lattice.EdgeOf(row);

        if (alongNorth == 0 && alongEast != 0)
        {
            offset = new Tiles(alongEast);
            return streets.Horizontal(column, row);
        }

        if (alongEast == 0 && alongNorth != 0)
        {
            offset = new Tiles(alongNorth);
            return streets.Vertical(column, row);
        }

        return Rows.NoSlot;
    }

    /// <summary>
    /// Which lattice block a Lot belongs to — <b>the inverse of <see cref="LotSubdivider"/>'s four
    /// faces</b>, and a pure function of state the Lot already saves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A POSITION ALONE CANNOT ANSWER THIS, and the side is what closes it.</b> Two blocks share
    /// every face line: the Segment at lattice row <c>r</c> is the north block's south face and the
    /// south block's north face, and both lay Lots along it. So a Lot at <c>north = r × block</c>
    /// belongs to block row <c>r</c> or to <c>r − 1</c> depending on <b>which side of the Street it
    /// stands on</b>, which is exactly what <c>adr/0074</c> says an Address is for.
    /// </para>
    /// <para>
    /// <b>The four constants are <see cref="LotSubdivider.SubdivideBlock"/>'s, read backwards.</b> A
    /// horizontal Segment runs eastward so Left is its north side, and a block takes Left of its
    /// <em>south</em> face — therefore a Left Lot on the Segment at row <c>r</c> is block row <c>r</c>,
    /// and a Right one is block row <c>r − 1</c>. A vertical Segment runs northward so Left is its
    /// west side, and a block takes Right of its <em>west</em> face — so the columns invert the other
    /// way round.
    /// </para>
    /// <para>
    /// ⚠ <b>It reads no derived state and rebuilds nothing</b>, which is why <c>plans/0053</c> step 2
    /// ships without the <c>LotTable.BlockSlot</c> column that plan proposed. A column would cost a
    /// clear on every <c>RebuildDerived</c> — <c>FrontageRebuildCostTests</c> measured that pass as
    /// <c>O(capacity)</c> and dominated by its clears — to save ten integer operations on a sample of
    /// three. ***The column is the right answer the day something wants this in bulk, and nothing
    /// does.***
    /// </para>
    /// </remarks>
    /// <returns><c>true</c> when the Lot fronts a lattice Segment at all.</returns>
    public static bool BlockOf(
        StreetGrid streets, Tiles east, Tiles north, StreetSide side, out int column, out int row) =>
        BlockOf(streets, east, north, side, out column, out row, out _);

    /// <inheritdoc cref="BlockOf(StreetGrid, Tiles, Tiles, StreetSide, out int, out int)"/>
    /// <remarks>
    /// <b>The overload that also names the FACE</b>, which is what pairs a Lot with its parcel:
    /// <see cref="BlockPatterns.Carve"/> produces parcels keyed by face and offset, and a Lot carries
    /// a position and a side. ⚠ <b>The face falls out of the same two tests</b> — a horizontal Segment
    /// runs eastward so Left is its north side, which makes the block's SOUTH face; a vertical one
    /// runs northward so Left is its west side, which makes the block's EAST face.
    /// </remarks>
    public static bool BlockOf(
        StreetGrid streets, Tiles east, Tiles north, StreetSide side,
        out int column, out int row, out BlockFace face)
    {
        ArgumentNullException.ThrowIfNull(streets);

        column = 0;
        row = 0;
        face = BlockFace.South;

        int block = streets.BlockTiles;

        if (block <= 0 || east.Raw < 0 || north.Raw < 0)
        {
            return false;
        }

        column = streets.Lattice.LineAt(east.Raw);
        row = streets.Lattice.LineAt(north.Raw);

        int alongEast = east.Raw - streets.Lattice.EdgeOf(column);
        int alongNorth = north.Raw - streets.Lattice.EdgeOf(row);

        // A horizontal face. The same test Locate makes, and for the same reason: a position exactly
        // on an intersection fronts nothing and belongs to no face.
        if (alongNorth == 0 && alongEast != 0)
        {
            if (side == StreetSide.Right)
            {
                row--;
                face = BlockFace.North;
            }

            return row >= 0;
        }

        if (alongEast == 0 && alongNorth != 0)
        {
            face = BlockFace.West;

            if (side == StreetSide.Left)
            {
                column--;
                face = BlockFace.East;
            }

            return column >= 0;
        }

        return false;
    }

    private static byte Bit(StreetSide side) => (byte)(side == StreetSide.Left ? 1 : 2);
}
