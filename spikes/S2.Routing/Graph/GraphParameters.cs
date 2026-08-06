namespace S2.Routing.Graph;

/// <summary>
/// Where a Segment's <c>volume / capacity</c> is stored: once for the run of road, or once for each
/// direction of travel along it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is parameterised because <c>plans/0010</c> forbids settling it here.</b> The gate
/// section is explicit: <i>"R0 must parameterise it rather than assume it."</i> A Segment carries
/// about four Lanes and Lanes are directional queues, so a Segment jammed inbound at the morning
/// peak reads roughly half-loaded if the two directions are summed — which makes Stress understate
/// exactly when it matters and promote to Microscopic late.
/// </para>
/// <para>
/// <b>It reaches R0 as a footprint axis and R2 as a correctness one.</b> Per direction is twice the
/// volume column and nothing else, so R0 can price it exactly; what it buys is not visible until
/// there is volume to attribute. Building it in now is what stops it from being a retrofit — the
/// three structural consequences the plan names (a directed graph, an ordered cache key, an
/// asymmetric matrix) are all already true of this graph regardless of which scope is chosen.
/// </para>
/// </remarks>
internal enum VolumeScope
{
    /// <summary>One counter per Segment, summed over both directions.</summary>
    PerSegment,

    /// <summary>One counter per direction of travel. Twice the storage.</summary>
    PerDirection,
}

/// <summary>
/// The generator's swept parameters. <c>plans/0010</c> R0: <i>"Parameterise Segment count and report
/// footprint as a curve, as K0 did for the Microscopic Cap. The ~30,000 figure is a placeholder and
/// S2 is the first thing able to say what road density a 268 km² city at 1M actually implies."</i>
/// </summary>
/// <remarks>
/// Nothing here is a chosen value. Every field is an axis, and the report is a curve against it —
/// the precedent being K0's sweep of the unset Microscopic Cap, which the plan calls good.
/// </remarks>
/// <param name="MapTiles">Tiles across the map. 4096 unless a rung is deliberately testing the fallback.</param>
/// <param name="BlockTiles">
/// Street grid spacing in Tiles — the block. <b>The axis that sets Segment count</b>, and therefore
/// the axis on which road density is reported. A Cell is 32 Tiles, so a rung at 32 is one Street on
/// every Cell boundary.
/// </param>
/// <param name="ArterialCount">
/// How many freeform Arterials cross the map. <b>The axis R0's admissibility verdict is owed
/// against</b> — a diagonal Arterial is what makes the true network distance shorter than Manhattan,
/// so this is the dial that decides where the tight heuristics stop being safe.
/// </param>
/// <param name="ArterialJunctionSpacingTiles">
/// Tiles of Arterial between authored Junction pieces. The Arterial's Segment length.
/// </param>
/// <param name="FootCrossingEverySevered">
/// A foot crossing at every <i>n</i>th Street the Arterial severs, or 0 for none.
/// <b>Severance's dial.</b> Between junctions an Arterial carries no pedestrian edges at all, so
/// this decides whether a neighbourhood is cut off from the shops that served it.
/// </param>
/// <param name="FootPathsPerThousandBlocks">
/// Foot-only cut-throughs per thousand blocks. <c>CONTEXT.md</c> → Segment: the foot-only Segments
/// are <i>"few and they are the edges Severance turns on, so nothing may size the graph by omitting
/// them."</i> Zero is a legitimate rung; leaving the axis out is not.
/// </param>
/// <param name="VolumeScope">Where <c>volume / capacity</c> lives. Swept, never assumed.</param>
/// <param name="Seed">The world seed. Counter-based, so a rung is the same graph forever.</param>
internal readonly record struct GraphParameters(
    int MapTiles,
    int BlockTiles,
    int ArterialCount,
    int ArterialJunctionSpacingTiles,
    int FootCrossingEverySevered,
    int FootPathsPerThousandBlocks,
    VolumeScope VolumeScope,
    ulong Seed)
{
    /// <summary>
    /// The working rung — a Street on every Cell boundary, which is the density that reproduces
    /// <c>CONTEXT.md</c>'s ~30,000-Segment placeholder, and enough Arterials to sever with.
    /// </summary>
    public static GraphParameters Working => new(
        MapTiles: Units.MapTiles,
        BlockTiles: Units.CellTiles,
        ArterialCount: 8,
        ArterialJunctionSpacingTiles: 512,
        FootCrossingEverySevered: 4,
        FootPathsPerThousandBlocks: 40,
        VolumeScope: VolumeScope.PerSegment,
        Seed: CounterHash.Seed);
}
