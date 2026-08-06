namespace S2.Routing.Graph;

/// <summary>
/// Which modes may traverse an arc. <c>CONTEXT.md</c> → Road Graph: <i>"One graph, with mode masks.
/// Pedestrian and vehicle edges are the same structure, tagged by which modes may traverse them —
/// not two parallel networks."</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>The mask is on the arc, not on the Segment, and the difference is not pedantry.</b>
/// <c>CONTEXT.md</c> → Segment lists the mode mask among a Segment's attributes, which is exactly
/// right while every Segment is bidirectional for every mode it permits. A one-way street is not:
/// it carries cars in one direction and pedestrians in both. Holding the mask on the Segment forces
/// a choice between a second arc set for foot — which is the two-parallel-networks structure the
/// corpus rejects by name — and a one-way street nobody may walk down.
/// </para>
/// <para>
/// So each directed arc carries the mask valid in that direction, and the Segment carries the union
/// of its arcs'. That is still one graph, one arc set and one Epoch. The Segment's own mask stays
/// meaningful for every reader that asks <i>what is this road for</i> rather than <i>may I go this
/// way</i>.
/// </para>
/// <para>
/// <b>This is also where Severance lives.</b> An Arterial's arcs carry <see cref="Car"/> and not
/// <see cref="Foot"/>, so a pedestrian route across one genuinely does not exist except at an
/// authored Junction piece that grants a crossing. Nobody deletes a pedestrian route; the mask
/// simply never granted one.
/// </para>
/// </remarks>
[Flags]
internal enum Modes
{
    /// <summary>Impassable. Not generated; present so a zeroed mask reads as a bug rather than as foot.</summary>
    None = 0,

    /// <summary>On foot. <c>adr/0008</c> makes a walk a simulated Leg, so this is not decoration.</summary>
    Foot = 1 << 0,

    /// <summary>By car.</summary>
    Car = 1 << 1,
}
