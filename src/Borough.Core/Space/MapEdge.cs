using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// Which side of the bounded map a position sits on, or <see cref="None"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four, because the Hinterland is per edge and there are four of them</b> (<c>adr/0088</c>).
/// <c>CONTEXT.md</c> → Hinterland is *"the economy behind one map edge, shared by every Outside
/// Connection on that edge"*, so this enum is not a compass — it is the **market selector**. That is
/// why it is a named quantity rather than a pair of booleans on a query: <i>which edge</i> is the one
/// economic decision an Outside Connection's siting makes, and it wants a name a Hinterland row can
/// be keyed on.
/// </para>
/// <para>
/// <b>It exists because nothing in the build knew where the boundary was.</b> Under
/// <c>adr/0021</c> the map is bounded and procedural, and <see cref="CellGrid.WorldTiles"/> has been
/// the extent since the grid was written — but no symbol anywhere in <c>Borough.Core</c> asked
/// whether a position was <em>on</em> it. Milestone 11 task 1 is the first caller.
/// </para>
/// <para>
/// ⚠ <b><see cref="None"/> is zero and it is reachable, so it is not a placeholder inside the range
/// of real answers.</b> Almost every position in the city is on no edge at all — that is the common
/// case rather than an error — which is the opposite of the trap <c>adr/0101</c>'s Shift band had to
/// dodge, where the defaulted value was also a legitimate one.
/// </para>
/// </remarks>
public enum MapEdge : byte
{
    /// <summary>Not on the boundary. <b>The common case.</b></summary>
    None = 0,

    /// <summary>The east axis at zero.</summary>
    West = 1,

    /// <summary>The east axis at <see cref="CellGrid.WorldTiles"/>.</summary>
    East = 2,

    /// <summary>The north axis at zero.</summary>
    South = 3,

    /// <summary>The north axis at <see cref="CellGrid.WorldTiles"/>.</summary>
    North = 4,
}

/// <summary>
/// Where the bounded map ends.
/// </summary>
/// <remarks>
/// <para>
/// <b>Derived from <see cref="CellGrid.WorldTiles"/> and from nothing else, which is the point.</b>
/// <c>adr/0088</c> constrains an Outside Connection to *"a position constrained to an edge"* and the
/// obvious reading is a **band** — within so many Tiles of the boundary. That would be a
/// hash-bearing world-creation number needing a named ratifier under <c>adr/0052</c>, chosen for a
/// mechanism whose only requirement is that the gate face outward. <b>The exact boundary needs no
/// number at all</b>, because the lattice already lands on it: a Street on lattice line 0 puts its
/// Lots at coordinate <b>0</b> exactly, and one on the last line puts them at
/// <see cref="CellGrid.WorldTiles"/> exactly (<c>LotSubdivider.Face</c> — the position is
/// <c>row × block</c>, with no set-back term). ***A constraint that can be stated exactly does not
/// get a tolerance.***
/// </para>
/// </remarks>
public static class MapEdges
{
    /// <summary>
    /// How many edges the position at <paramref name="east"/>, <paramref name="north"/> touches, and
    /// which one when it touches exactly one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A count rather than a bool, because two is a real answer and it is not the same failure as
    /// zero.</b> A corner Lot touches two edges, and under <c>adr/0088</c> the edge <em>selects a
    /// market</em> — so a gate on a corner would draw from two Hinterlands with nothing to say which,
    /// where a gate in the middle of the city draws from none. Those are different sentences for the
    /// caller to write, and a single <c>bool</c> would collapse them into
    /// <i>placement failed</i>.
    /// </para>
    /// <para>
    /// ⚠ <b>Two is the maximum and the map's shape is what guarantees it.</b> The bound is
    /// rectangular, so a position satisfies at most one constraint per axis. This returns a count
    /// rather than a set because no caller has wanted the pair, and a corner is refused rather than
    /// resolved.
    /// </para>
    /// </remarks>
    /// <param name="east">Position along the east axis, in whole Tiles.</param>
    /// <param name="north">Position along the north axis, in whole Tiles.</param>
    /// <param name="edge">The edge touched, when exactly one is; <see cref="MapEdge.None"/> otherwise.</param>
    /// <returns>0, 1 or 2.</returns>
    public static int Touching(Tiles east, Tiles north, out MapEdge edge)
    {
        MapEdge acrossEast = east.Raw switch
        {
            0 => MapEdge.West,
            CellGrid.WorldTiles => MapEdge.East,
            _ => MapEdge.None,
        };

        MapEdge acrossNorth = north.Raw switch
        {
            0 => MapEdge.South,
            CellGrid.WorldTiles => MapEdge.North,
            _ => MapEdge.None,
        };

        if (acrossEast != MapEdge.None && acrossNorth != MapEdge.None)
        {
            edge = MapEdge.None;
            return 2;
        }

        edge = acrossEast != MapEdge.None ? acrossEast : acrossNorth;

        return edge == MapEdge.None ? 0 : 1;
    }
}
