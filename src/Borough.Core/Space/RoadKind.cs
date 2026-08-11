namespace Borough.Core.Space;

/// <summary>
/// What a Segment is, as the design names it. <b>The Segment's declared kind, from which its
/// free-flow speed and capacity are derived rather than frozen.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>adr/0068</c>'s shape applied to a road</b> — <i>a Building's occupancy is declared
/// by its kind and derived from the Ruleset in force</i> — and <c>adr/0064</c>'s — <i>a Bin's
/// capacity is derived AND rebuilt from the Ruleset in force rather than frozen when the Building was
/// raised</i>. Both ADRs corrected the same defect: a number copied out of the Ruleset into a row at
/// construction, which a designer then cannot retune without demolishing the city that holds the old
/// copy. Freezing a free-flow speed into a Segment would reintroduce it in a new table, and it would
/// break <c>adr/0015</c>'s acceptance test in the one place the player would most notice: change a
/// speed limit, and every commute already on the map keeps the old one.
/// </para>
/// <para>
/// <b>The Road Graph stays uniform, which is what <c>06</c> milestone 5a's risk is about.</b> A kind
/// is not a second graph and not a second edge set: routing reads <see cref="RoadSegmentTable.FreeFlow"/>
/// and the mode masks, never this. It exists so a Ruleset can say <em>Arterials run at 90</em> once
/// instead of writing 90 into every row, and <c>CONTEXT.md</c> already names all three as distinct
/// objects with distinct rules.
/// </para>
/// </remarks>
public enum RoadKind : byte
{
    /// <summary>
    /// A grid-snapped road — <c>CONTEXT.md</c> → Street, <i>"the common case"</i>. The Road Graph
    /// falls out of the Tile grid directly and intersections between Streets are trivial.
    /// </summary>
    Street = 0,

    /// <summary>
    /// A freeform road between authored Junction pieces — <c>CONTEXT.md</c> → Arterial,
    /// <i>"deliberately rare"</i>. Carries no pedestrian Arcs, which is what makes Severance
    /// emergent; grants no frontage, so nothing zones onto one (<c>adr/0014</c>).
    /// </summary>
    Arterial = 1,

    /// <summary>
    /// A foot-only Segment — a block cut-through, a pedestrian precinct, or a crossing kept where an
    /// Arterial severed a Street. <c>CONTEXT.md</c> → Segment: <i>"few, and they are the edges
    /// Severance turns on, so nothing may size the graph by omitting them."</i>
    /// </summary>
    FootPath = 2,
}
