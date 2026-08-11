using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// <b>The contact between a Lot and a Street it can take access from</b> — derived from the Road
/// Graph, rebuilt on the Epoch, and never saved (<c>adr/0078</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b><c>CONTEXT.md</c> → Frontage: <i>"frontage is arithmetic, not a rule"</i></b>, and this class is
/// that sentence. Block geometry decides whether a parcel touches a street at all; nothing here
/// consults a Ruleset about policy, only about the lattice spacing and how many Lots a Segment holds.
/// </para>
/// <para>
/// <b>The derivation runs backwards from the Lot's saved coordinates, which is what makes it a
/// derivation rather than a second copy.</b> A Lot laid on a horizontal Street has
/// <c>north ≡ 0 (mod block_tiles)</c> and <c>east</c> strictly between two intersections; a Lot on a
/// vertical Street has exactly the reverse. <b>The two sets are disjoint and neither contains an
/// intersection</b>, so a saved position names at most one lattice edge and the Segment on it is a
/// lookup. That is the whole reason frontage can be thrown away and recomputed instead of maintained.
/// </para>
/// <para>
/// <b>The side is saved and the rest is derived, and the split is not arbitrary.</b> A point on a line
/// is on both sides of it, so the side is the one part of an Address that the coordinates genuinely do
/// not carry — which is <c>adr/0074</c>'s <i>"one saved bit on that place"</i> reached from the other
/// direction. It would <em>also</em> be recoverable from the offset, since the subdivider alternates
/// sides as it walks a Segment — but only under the <c>lots_per_segment</c> in force, and that is
/// hot-reloadable tuning. Deriving side from spacing would make retuning the spacing silently move
/// which side of the street every standing Building is on.
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
    /// Rebuilds every Lot's frontage from the Street lattice, and the per-Segment claim mask with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Wholesale, and called from <c>World.RebuildDerived</c> and after every road edit.</b> A Lot
    /// whose Street is gone comes out of this with no frontage and keeps its position, which is
    /// <c>adr/0079</c>: the Building stands, the Address becomes <see cref="Address.None"/>, and
    /// nothing anywhere holds a handle to a freed Segment.
    /// </para>
    /// <para>
    /// <b>The claim mask is rebuilt here rather than maintained</b>, so that a Lot deleted by
    /// re-subdivision releases its side of a Segment without anything having to remember to say so.
    /// It is derived from the Lots, which are saved, so it survives a reload by being recomputed.
    /// </para>
    /// </remarks>
    public void Rebuild(LotTable lots, StreetGrid streets)
    {
        ArgumentNullException.ThrowIfNull(lots);
        ArgumentNullException.ThrowIfNull(streets);

        Array.Clear(_claimed);

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            int segment = Locate(
                streets, lots.East[slot], lots.North[slot], out Tiles offset);

            lots.FrontageSlot[slot] = segment + 1;
            lots.FrontageOffset[slot] = offset;

            if (segment != Rows.NoSlot)
            {
                Claim(segment, (StreetSide)lots.Side[slot]);
            }
        }
    }

    /// <summary>
    /// Which Street a position fronts, and how far along it — or <see cref="Rows.NoSlot"/>.
    /// </summary>
    /// <remarks>
    /// <b>A position exactly on an intersection fronts nothing</b>, and that is deliberate rather than
    /// an edge case left to fall out. `CONTEXT.md` → Address is emphatic that an Address is
    /// <i>"never a Node"</i>; a Lot at a corner would have to choose between two Segments, and the
    /// choice would be an arbitrary tie-break that the State Hash would then carry forever.
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

        int column = IntegerMath.FloorDiv(east.Raw, block);
        int row = IntegerMath.FloorDiv(north.Raw, block);
        int alongEast = east.Raw - (column * block);
        int alongNorth = north.Raw - (row * block);

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

    private static byte Bit(StreetSide side) => (byte)(side == StreetSide.Left ? 1 : 2);
}
