namespace S2.Routing.Routing;

/// <summary>
/// Where a Building meets the network: an offset along a Segment, never a node.
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Access Point makes this structural rather than a matter of taste: <i>"five
/// Buildings share a Segment at the working figures, so promoting Access Points to nodes would split
/// every Segment five ways and put the Road Graph at 150,000–300,000 edges instead of ~30,000. A
/// routing query is therefore <c>(Segment, offset) → (Segment, offset)</c> rather than node-to-node,
/// which is the query shape everything downstream must be measured on."</i>
/// </para>
/// <para>
/// <b>Which is why the denominator is not a node-to-node search.</b> <c>plans/0010</c> R0 is explicit:
/// a node-to-node denominator <i>"measures a query the game never issues, and every figure in this
/// spike divides by it."</i> The search therefore seeds its open set with <b>both endpoints of the
/// origin Segment</b> at their partial costs and terminates on <b>either endpoint of the goal
/// Segment plus the offset remainder</b> — and R0 reports that bootstrap and termination arithmetic
/// separately from the search, so later tasks can tell fixed overhead from work.
/// </para>
/// </remarks>
/// <param name="Segment">The Segment the Building sits on.</param>
/// <param name="OffsetTiles">
/// Tiles along the Segment from <c>SegmentNodeA</c>. In <c>[0, SegmentLengthTiles]</c>.
/// </param>
internal readonly record struct AccessPoint(int Segment, int OffsetTiles);
