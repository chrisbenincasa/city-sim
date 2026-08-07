using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The fields that <b>stopped being Map Layers</b>, and where their queries will live.
/// </summary>
/// <remarks>
/// <para>
/// <b>This file exists because this is the slice where somebody would re-add them by reflex.</b>
/// <c>02 §2.5</c>'s classification procedure is there because <em>"add a Map Layer" was the reflex
/// answer four times running and was the right answer once</em>, and noise and near-road pollution are
/// three of the four times it was wrong. They are not in <see cref="Layer"/>, they have no Cell
/// storage, and no cadence — and an empty file saying so is cheaper than the conversation that
/// otherwise happens after the field is built.
/// </para>
/// <para>
/// <b>Why they are not Layers.</b> They are <b>line sources</b>: short-ranged, logarithmic, 50–300 m,
/// with the whole gradient inside a single 128 m Cell. A Cell-resolution field cannot hold that shape
/// and degrades into <em>is there a road here</em>. A line source is a <b>distance query</b>, exact at
/// Tile resolution, and quantising it to any grid is worse than not quantising it. <b>Finer Cells were
/// considered and rejected</b> — <c>02 §2.5</c> guard rule 2 sends a short-range field to a query, not
/// to a finer grid, and cost was never the obstacle (65k Cells is 1.6 MB).
/// </para>
/// <para>
/// <b>Nothing here is implemented, because there is no Road Graph in Phase 1 and therefore nothing to
/// query.</b> What this slice owes is the note and the two constraints below, so that whoever builds
/// it inherits the properties rather than rediscovering them.
/// </para>
/// </remarks>
public static class LineSourceQueries
{
    /// <summary>
    /// Noise at a Tile. <b>A named hole with two constraints attached.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>1. The query sums; it does not take the nearest source.</b> Noise superposes, so a
    /// nearest-source query understates a Lot caught between two busy roads — and that Lot is precisely
    /// the one a player would ask about. This is the property that nearly broke the field's
    /// classification: <c>02 §2.5</c> question 3 is <em>does it superpose, or does the nearest source
    /// dominate?</em>, and a query is only admissible at all once the answer is <em>superposes</em>.
    /// </para>
    /// <para>
    /// <b>2. It enumerates by loudness, not by road class.</b> The query takes every linear source
    /// within range <em>whose contribution here exceeds the ambient background</em>, where the
    /// background is the local-Street level it already computes. <b>That is a crossover rather than an
    /// authored threshold</b> — nobody tunes a number — and the enumerated set is small <em>by
    /// definition</em>, since standing out above the background is what makes a source enumerable.
    /// </para>
    /// <para>
    /// <b>Enumerating by class would be cheaper and would be wrong in a specific, nameable case.</b>
    /// <c>adr/0034</c> made <c>adr/0014</c> load-bearing for this model retroactively — but for
    /// <em>bimodal traffic volume</em>, not for there being exactly two road classes. <c>adr/0029</c>'s
    /// <b>Separated</b> transit band is already an Arterial and equally rare; the band that stresses
    /// the model is <b>Reserved</b>, which puts Arterial-scale volume onto an ordinary grid Street.
    /// Loudness catches it and class misses it. What would genuinely break the model is a volume
    /// distribution with no gap in it.
    /// </para>
    /// <para>
    /// <b>Swapping the two changes the State Hash</b>, so it is a design change under <c>05 §4</c> and
    /// not an optimisation, however a profile motivates it (<c>02 §2.4</c>).
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Always. There is no Road Graph to query.</exception>
    public static int Noise(Tiles east, Tiles north) =>
        throw new NotSupportedException(
            $"noise at Tile ({east.Raw}, {north.Raw}) is a line-source distance query on the Road "
            + "Graph, which does not exist in Phase 1. It is not a Map Layer (adr/0034) and adding "
            + "one would flatten its whole gradient into a single Cell.");

    /// <summary>
    /// Near-road pollution at a Tile. <b>The same query as <see cref="Noise"/> with different weights.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Line source, 150–300 m (<c>02 §2.4</c>). Both constraints on <see cref="Noise"/> apply
    /// unchanged, and the two share an implementation when one exists.
    /// </para>
    /// <para>
    /// <b>It is a different field from <see cref="Layer.IndustrialPollution"/> and the two must not be
    /// merged back.</b> That merge is the defect <c>adr/0034</c> was written to undo: the old
    /// <c>Pollution</c> row was fed by industry (a point source, kilometres) and by traffic (a line
    /// source, 150 m) through one kernel, so <b>one of them was always wrong</b>. <c>02 §2.5</c> guard
    /// rule 1 states it generally — <em>one field, one geometry, one range</em>.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Always. There is no Road Graph to query.</exception>
    public static int NearRoadPollution(Tiles east, Tiles north) =>
        throw new NotSupportedException(
            $"near-road pollution at Tile ({east.Raw}, {north.Raw}) is a line-source distance query "
            + "on the Road Graph, which does not exist in Phase 1. It is a different field from "
            + "industrial pollution and merging them is the defect adr/0034 undid.");

    /// <summary>
    /// Amenity at a Tile. <b>Not a Layer and not a distance query either — a <em>time</em>.</b>
    /// </summary>
    /// <remarks>
    /// A <b>walkable catchment on the Road Graph</b>: destinations within roughly 400 m <em>on foot</em>,
    /// cached and Epoch-invalidated. It is listed here rather than in <see cref="Layer"/> because it
    /// fails the Layer test for a third distinct reason, and the reason is worth keeping separate from
    /// the other two: its range is a travel time, so no geometry on the Cell grid can express it and no
    /// straight-line distance query can either. <c>02 §2.5</c>'s representation table gives catchments
    /// their own row.
    /// </remarks>
    /// <exception cref="NotSupportedException">Always. There is no Road Graph to walk.</exception>
    public static int Amenity(Tiles east, Tiles north) =>
        throw new NotSupportedException(
            $"amenity at Tile ({east.Raw}, {north.Raw}) is a walkable catchment on the Road Graph — a "
            + "time rather than a distance — which does not exist in Phase 1.");
}
