namespace Borough.Core.Space;

/// <summary>
/// Which modes may traverse an Arc. <c>CONTEXT.md</c> → Road Graph: <i>"One graph, with mode masks.
/// Pedestrian and vehicle edges are the same structure, tagged by which modes may traverse them —
/// not two parallel networks."</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>The mask is saved on the Arc and the Segment's is derived from it, and the direction of that
/// derivation is a decision rather than a detail.</b> <c>CONTEXT.md</c> → Segment lists the mode mask
/// among a Segment's attributes, which is exactly right while every Segment is bidirectional for
/// every mode it permits. A one-way street is not: it carries cars one way and pedestrians both.
/// Holding the mask on the Segment forces a choice between a second Arc set for foot — which is the
/// two-parallel-networks structure the corpus rejects by name — and a one-way street nobody may walk
/// down. So each direction carries the mask valid in it, and the Segment carries the union. That is
/// still one graph, one Arc set and one Epoch, and the Segment's own mask stays meaningful for every
/// reader asking <i>what is this road for</i> rather than <i>may I go this way</i>. See
/// <c>adr/0072</c>.
/// </para>
/// <para>
/// <b>This is also where Severance lives.</b> An Arterial's Arcs carry <see cref="Car"/> and not
/// <see cref="Foot"/>, so a pedestrian route across one genuinely does not exist except at an
/// authored Junction piece that grants a crossing. Nobody deletes a pedestrian route; the mask simply
/// never granted one, which is what makes Severance emergent rather than scripted (<c>03 §3.7</c>).
/// </para>
/// <para>
/// <b>Stored as a <see cref="byte"/> and widened at every use.</b> Two bits are declared and the
/// column is per-Arc, so the width is a footprint decision on the largest table in the graph; the
/// enum is <see cref="int"/>-backed because that is what C# flag arithmetic wants and narrowing once
/// at the write site is cheaper to read than casting at every test.
/// </para>
/// </remarks>
[Flags]
public enum TravelMode
{
    /// <summary>
    /// Impassable. <b>Never written by the generator</b> — present so that a zeroed mask reads as an
    /// unwritten row rather than as <see cref="Foot"/>.
    /// </summary>
    None = 0,

    /// <summary>On foot. <c>adr/0008</c> makes a walk a simulated Leg, so this is not decoration.</summary>
    Foot = 1 << 0,

    /// <summary>By car.</summary>
    Car = 1 << 1,

    /// <summary>Both — an ordinary Street, in either direction.</summary>
    Any = Foot | Car,
}
