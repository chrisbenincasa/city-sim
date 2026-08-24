using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell's value of one Layer: <c>adr/0002</c>'s hot query result, as a value type.
/// </summary>
/// <remarks>
/// <b>A struct, so a span of them is a flat block and the query allocates nothing.</b> The coordinates
/// come back with the value because the box a host asks about is clamped to the map, so the caller
/// cannot reconstruct the position from the index alone without repeating the clamp — and a query whose
/// result has to be decoded against arithmetic the caller re-derives is a query with two
/// implementations.
/// </remarks>
/// <param name="East">The Cell's east coordinate.</param>
/// <param name="North">The Cell's north coordinate.</param>
/// <param name="Value">The Layer's value there, normalised.</param>
public readonly record struct LayerReading(Cells East, Cells North, int Value);

/// <summary>
/// The weights and the noise parameters <see cref="MapLayers.Desirability"/> composes with.
/// </summary>
/// <param name="Pollution">Q16.16 <c>w₂</c>. Subtracts.</param>
/// <param name="Noise">Q16.16 <c>w₃</c>. Subtracts.</param>
/// <param name="NoiseSource">The range and intensity the noise query carries.</param>
/// <remarks>
/// ⚠ <b>Two weights and not five</b> — <c>w₁</c> was deleted (<c>adr/0122</c>) and <c>w₄</c> amenity and
/// <c>w₅</c> shoreline have no term to weigh (<c>adr/0123</c>). ⚠ <b>Both are unratified and each owes
/// TWO entries in <c>plans/0002</c> §D1</b>, a reachable floor and an owed real ratifier, because
/// nothing in the city reads land value and the quantity that would refute a scale is a consumer's
/// (<c>adr/0125</c>).
/// </remarks>
public readonly record struct DesirabilityWeights(int Pollution, int Noise, LineSource NoiseSource)
{
    /// <summary>
    /// The shipped starting point. <b>Every number here is unratified and each owes two <c>§D1</c>
    /// entries.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Range 300 m</b> — the outer end of <c>02 §2.4</c>'s <em>50–300 m</em>. ⚠ That band is
    /// <b>six times wide</b> and is the same defect <c>plans/0012</c> already records against the
    /// industrial kernel's 1–10 km: it is not a number, it is the absence of one.
    /// </para>
    /// <para>
    /// <b>Intensity 4.0, and it is the one figure here with a derivation.</b> A Street at its stated
    /// capacity — 3,600 Vehicles an hour, so about 42 a Tick — must sit in <c>Log1P</c>'s
    /// <em>logarithmic</em> stretch across the whole range rather than its linear one, or the field is
    /// the physically-wrong linear sum the logarithm was chosen to prevent. Measured: at 1.0 a capacity
    /// Street falls under unity by about 150 m; at 4.0 it stays above it out to the full 300.
    /// </para>
    /// <para>
    /// <b>Both weights 1.0, and that is deliberately neutral rather than derived.</b> Measured
    /// magnitudes put the two terms within one order of magnitude — pollution reaches about 12 in kernel
    /// units under a strong source, noise about 3 beside a capacity Street — so a 1:1 start leaves both
    /// <em>visible</em>, which is the only property anything can check today.
    /// ⚠ <b>Nothing distinguishes them beyond that, and nothing can</b>: the quantity that would refute
    /// a weight is produced by a consumer of land value, and there is no consumer (<c>adr/0125</c>).
    /// </para>
    /// </remarks>
    public static DesirabilityWeights Default { get; } = new(
        Fixed.One,
        Fixed.One,
        new LineSource(Tiles.FromMetres(300), Fixed.FromInt(4)));
}

/// <summary>
/// The weights <see cref="MapLayers.Fertility"/> composes with. <b>One, and that is the decision.</b>
/// </summary>
/// <param name="Pollution">Q16.16 <c>w_p</c>. Subtracts.</param>
/// <remarks>
/// <para>
/// <c>adr/0155</c>, milestone 24 task 5. 🔴 <b>There is no Sealing weight here and its absence is the
/// decision.</b> <c>w_s</c> is <b>derived</b> from an endpoint — <c>CONTEXT.md</c> → Sealing makes a
/// Cell at <see cref="CellGrid.TilesInCell"/> one whose every Tile is built on, so it has no farmland
/// — which pins the term at <c>base × Sealing / 1024</c>. ***A coefficient with an endpoint is not a
/// tuning knob, and offering it as one invites a Ruleset to state that a fully paved Cell still
/// farms.***
/// </para>
/// <para>
/// <b>A record of one field rather than a bare <c>int</c> parameter</b>, on
/// <see cref="DesirabilityWeights"/>'s shape: it is where <c>w₄</c>'s sibling goes when a term is
/// added, and it keeps the two compositions reading alike.
/// </para>
/// </remarks>
public readonly record struct FertilityWeights(int Pollution)
{
    /// <summary>
    /// The shipped starting point. 🔴 <b>Unratified, and it owes a <c>plans/0002</c> §D1 row.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>4%, and it is anchored rather than derived.</b> Pollution reaches about <b>12</b> in kernel
    /// units under a strong source (measured — see <see cref="DesirabilityWeights.Default"/>), and
    /// <c>adr/0022</c>'s Evidence specimen is the one place in the corpus that says what a plume
    /// should <em>cost</em> a farm: <em>"41% — ground sealed 12%, pollution from Eastfield Industrial
    /// 47%"</em>. At <c>w_p = 0.04</c> a Cell under that source loses <b>0.48</b>, which is that
    /// sentence within rounding.
    /// </para>
    /// <para>
    /// ⚠ <b>The specimen is a mock-up and is being leaned on anyway, which is stated rather than
    /// hidden.</b> What earns it the weight it is carrying is that its <em>other</em> half was checked
    /// and held: <c>plans/0042</c> <b>F7</b> measured mean Sealing at <b>6.3%</b> and a peak Cell at
    /// <b>11.4%</b> against the specimen's <em>ground sealed 12%</em>. ***A mock-up whose one testable
    /// number came back right is better evidence than an invented ratio***, and it is still not a
    /// measurement of this one.
    /// </para>
    /// <para>
    /// ⚠ <b>Percent is a coarse unit here and the ratifier may reopen it, not just the value.</b> At
    /// the peak magnitude one step is <b>0.12</b> of the whole fertility scale, so nothing between
    /// 0.36 and 0.48 is expressible. It stays a percent because <c>adr/0048</c> refuses a decimal on
    /// the path in and every other authored fraction in this corpus is one.
    /// </para>
    /// <para>
    /// 🔴 <b>Nothing consumes Fertility, so nothing can refute this yet</b> — no milestone in
    /// <c>06</c> builds a farm. The named ratifier is milestone 24's long run on a world with varied
    /// terrain and an emitting source, and the refuting reading is stated in both directions in
    /// <c>plans/0002</c> §D1.
    /// </para>
    /// </remarks>
    public static FertilityWeights Default { get; } = new(IntegerMath.RoundDiv(Fixed.FromInt(4), 100));
}

/// <summary>
/// The Map Layers: sparse Cell storage, the staggered schedule, and incremental re-diffusion.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what Phase 5 drives, and it holds the three things a Layer needs that a table cannot
/// carry</b> — the residency index, the cadence, and the record of which sources have changed since
/// the last recomputation. None of them is per-row state, so none of them is a column.
/// </para>
/// <para>
/// <b>Composition is not here, and its absence is the design.</b> Desirability and Fertility are
/// composed at the point of use and never stored (<c>02 §2.4</c>): a stored composite needs
/// invalidating whenever any input changes, and drifts. <see cref="Fertility"/> is a named hole
/// rather than a placeholder returning zero; <see cref="Desirability"/> composes, and is bounded
/// above by zero until amenity arrives (<c>adr/0123</c>).
/// </para>
/// </remarks>
public sealed class MapLayers
{
    /// <summary>
    /// Initial row count. <b>Not derived from the Citizen count, unlike every other table.</b>
    /// </summary>
    /// <remarks>
    /// Layer residency scales with developed <em>area</em> and with the reach of whatever has been
    /// emitted into it, neither of which is linear in population — a single factory on an empty map
    /// residents its whole halo, and a million Citizens in towers resident very little more than ten
    /// thousand would. Sizing it per thousand Citizens would be a derivation with nothing behind it,
    /// so it starts small and grows.
    /// </remarks>
    private const int InitialCapacity = 1_024;

    private readonly LayerCellTable _cells;
    private readonly TerrainCellTable _terrain;
    private readonly WoodlandCellTable _woodland;

    /// <summary>What the terrain table folded to when the ground was laid. See <see cref="LayTerrain"/>.</summary>
    private ulong _terrainLaidFold;
    private readonly CellResidency _residency = new();

    /// <summary>Scratch for the land value pass. Rebuilt each pass; never saved, never hashed.</summary>
    private readonly TrafficPresence _traffic = new();

    private LayerRuleset _ruleset;
    private CellRect _pollutionDirty = CellRect.Empty;

    /// <param name="ruleset">The cadence, the rates and the constants. See <see cref="LayerRuleset"/>.</param>
    public MapLayers(LayerRuleset ruleset)
    {
        _cells = new LayerCellTable(InitialCapacity);
        _terrain = new TerrainCellTable();
        _woodland = new WoodlandCellTable();

        // An unpopulated world's ground is all Ordinary, and that is what its ground IS rather than a
        // placeholder -- so the baseline is taken here and a world that is never populated still has
        // a fingerprint to be checked against.
        _terrainLaidFold = _terrain.Fingerprint();
        _ruleset = ruleset;
        PollutionKernel = LayerKernels.IndustrialPollution(ruleset.Constants);
    }

    /// <summary>The Cell rows themselves.</summary>
    public LayerCellTable Cells => _cells;

    /// <summary>Which slot holds which Cell.</summary>
    public CellResidency Residency => _residency;

    /// <summary>
    /// What sort of ground every Cell is. <b>Dense, and it has no residency index.</b>
    /// </summary>
    /// <remarks>
    /// Here rather than on the <c>World</c> because <see cref="Fertility"/> is the term that reads it
    /// and Fertility composes here. ⚠ <b>It is not a Map Layer</b> — nothing diffuses it, nothing
    /// schedules it and <see cref="Step"/> never touches it. It shares this class for the reason
    /// <see cref="LayerCellTable.Sealing"/> does: this is where per-Cell ground lives.
    /// </remarks>
    public TerrainCellTable Terrain => _terrain;

    /// <summary>
    /// Lays the ground from the world key, and records it as the state nothing may change.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One call rather than two, and that is the point.</b> Generating the terrain and recording
    /// its fingerprint must happen together — a caller that did the first and forgot the second would
    /// leave a world that reports as corrupt at the end of its run, which is a failure with no
    /// relation to its cause. ***Two steps that must not come apart belong behind one door.***
    /// </para>
    /// <para>
    /// <c>plans/0042</c> decision 3 places the call in
    /// <see cref="Entities.SyntheticCity.PopulateInto"/>, between the already-populated refusal and
    /// <c>LayLand</c>.
    /// </para>
    /// </remarks>
    public void LayTerrain(WorldKey key)
    {
        TerrainGenerator.LayInto(_terrain, key);

        _terrainLaidFold = _terrain.Fingerprint();
    }

    /// <summary>
    /// Whether anything has written the terrain table since the ground was laid.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is what pays for the Decide guard skipping that table</b> — see
    /// <see cref="Entities.World.TablesAPhaseCanWrite"/>. ⚠ <b>It asks *has it changed*, not *does it
    /// match the world key*</b>: an unpopulated world has never been laid, and a loaded world was
    /// restored rather than generated, so a key comparison reports both as corrupt. A load restoring
    /// the wrong terrain is <c>adr/0112</c>'s job and is already done.
    /// </remarks>
    public bool TerrainIsUnchangedSinceLaid() => _terrain.Fingerprint() == _terrainLaidFold;

    /// <summary>
    /// How many of every Cell's Tiles are wooded. <b>Dense, and it has no residency index.</b>
    /// </summary>
    /// <remarks>
    /// Here for <see cref="Terrain"/>'s reason — this is where per-Cell ground lives — and in a table
    /// of its own for <see cref="WoodlandCellTable"/>'s. ⚠ <b>It is not a Map Layer</b>: nothing
    /// diffuses it, nothing schedules it, and <see cref="Step"/> never touches it.
    /// </remarks>
    public WoodlandCellTable Woodland => _woodland;

    /// <summary>Plants the world's forest from the world key.</summary>
    /// <remarks>
    /// <para>
    /// <b>A second door beside <see cref="LayTerrain"/> rather than the same one, and the reason is
    /// that the two tables have opposite mutability contracts.</b> Terrain is laid once and a
    /// fingerprint is taken so that <see cref="TerrainIsUnchangedSinceLaid"/> can pay for the Decide
    /// guard skipping it. <b>Woodland is written by the running city</b> — every
    /// <see cref="Seal"/> may take some — so a fingerprint over it would be a check that fails as
    /// soon as anybody builds. ***Putting them behind one door would mean one of the two contracts
    /// had to be weakened to fit.***
    /// </para>
    /// <para>
    /// ⚠ <b>It must run before <c>LayLand</c> and it authors no number.</b> Every Cell is unsealed
    /// when this writes, so the Sealing ceiling is trivially satisfied and the pass does not consult
    /// it; run after the roads, it would plant forest on top of them.
    /// </para>
    /// </remarks>
    public void LayWoodland(WorldKey key) => WoodlandGenerator.LayInto(_woodland, key);

    /// <summary>How many of one Cell's Tiles are wooded. <b>0 to <see cref="CellGrid.TilesInCell"/>.</b></summary>
    /// <remarks>
    /// Named for the count rather than for the thing, because <see cref="Woodland"/> is the table and
    /// one of the two had to say which it was.
    /// </remarks>
    public int WoodedTiles(Cells east, Cells north) => _woodland.At(east, north);

    /// <summary>The cadence and rates this world reads its Layers with.</summary>
    public LayerRuleset Ruleset => _ruleset;

    /// <summary>The staggered cadence. Shorthand for <c>Ruleset.Schedule</c>.</summary>
    public LayerSchedule Schedule => _ruleset.Schedule;

    /// <summary>The kernel industrial pollution is convolved with. Built once, at world creation.</summary>
    /// <remarks>
    /// <b>An instance property since slice 8, and the change is the point rather than a
    /// consequence.</b> It used to be static, built from a <c>const</c> nothing could change; the
    /// radius is Ruleset data in <c>adr/0015</c>'s <em>world-creation</em> category, so it is read
    /// from the file, frozen here for this world's life, and a reload that changes it is refused.
    /// </remarks>
    public SeparableKernel PollutionKernel { get; }

    /// <summary>
    /// Puts a reloaded Ruleset's Layer data in force. The tuning half only.
    /// </summary>
    /// <remarks>
    /// <b>The world-creation half is not merely skipped, it is checked.</b> A caller that reached
    /// here with a different kernel radius has already got past the loader's refusal and
    /// <c>RulesetShape</c>, so this is the last place it could be caught — and the failure it prevents
    /// is silent: every Cell not re-diffused would be read at the wrong scale, producing a plausible
    /// field that is simply wrong. <c>adr/0015</c>'s revisit trigger names silently ignoring as the
    /// failure mode, and a swap is exactly where that would happen.
    /// <para>
    /// <b>Cells, not metres, and the loader compares the same way.</b> The radius is authored in
    /// metres and used in Cells; two authored figures that round to one Cell count produce one kernel,
    /// so refusing between them would refuse a reload that reinterprets nothing. Comparing in the
    /// units the state was recorded in <em>is</em> <c>adr/0015</c>'s membership test rather than an
    /// approximation of it — and it is what lets <see cref="PollutionKernel"/> stay the one built at
    /// world creation, because an accepted reload provably cannot change it.
    /// </para>
    /// </remarks>
    internal void Adopt(LayerRuleset ruleset)
    {
        Cells radius = CellGrid.FromMetres(ruleset.Constants.IndustrialPollutionMetres);

        if (radius != PollutionKernel.Radius)
        {
            throw new InvalidOperationException(
                $"this world's pollution kernel reaches {PollutionKernel.Radius.Raw} Cells and the "
                + $"reloaded Ruleset declares {ruleset.Constants.IndustrialPollutionMetres} m, which "
                + $"is {radius.Raw}. Every Cell is stored in units of the kernel it was diffused "
                + "through, so this is a world-creation constant (adr/0015) and changing it mid-run "
                + "reinterprets the whole map.");
        }

        _ruleset = ruleset;
    }

    /// <summary>Whether any source has changed since pollution was last recomputed.</summary>
    public bool PollutionIsDirty => !_pollutionDirty.IsEmpty;

    /// <summary>
    /// Adds to what a Cell emits, and residents everything its plume can reach.
    /// </summary>
    /// <remarks>
    /// <b>The halo is made resident here rather than during diffusion, and that ordering is the point
    /// of doing it at all.</b> Diffusion writes output to every Cell within one kernel radius of a
    /// changed source; a Cell with no row has nowhere to put it. Allocating during the convolution
    /// would mean a pass that mutates the structure it is walking, which is the shape of bug that
    /// survives every test written against a small map.
    /// </remarks>
    public void EmitPollution(Cells east, Cells north, int amount)
    {
        CellRect halo = CellRect.At(east, north).Dilate(PollutionKernel.Radius);

        _residency.Ensure(_cells, halo);

        int slot = _residency.Ensure(_cells, east, north);
        _cells.PollutionSource[slot] += amount;

        MarkPollutionDirty(east, north);
    }

    /// <summary>Records that a Cell's pollution sources changed, without changing them.</summary>
    public void MarkPollutionDirty(Cells east, Cells north) =>
        _pollutionDirty = _pollutionDirty.Union(CellRect.At(east, north));

    /// <summary>Phase 5: recompute whichever Layers this Tick's cadence is due for.</summary>
    /// <remarks>
    /// <para>
    /// ✅ <b>Sealing is here as of milestone 24 task 4, and it was not before.</b> This paragraph used
    /// to say its absence was the schedule being honest — it changes on build, so it had no cadence.
    /// It has one now, because <em>recovery</em> is not the same event as sealing: ground unbuilt on
    /// heals whether or not anything is built anywhere, so the pass that heals it has to be on a clock
    /// rather than at a write site. <c>02 §2.4</c>, <c>adr/0044</c>.
    /// </para>
    /// <para>
    /// <b>Its cadence is a Day and the other two are not, which is deliberate.</b> The rate is stated
    /// in Days by the design (<c>CONTEXT.md</c> → Sealing), so the pass ticks in Days and the tau is a
    /// count of Days with nothing to convert. See <see cref="LayerSchedule.Sealing"/>.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick being stepped.</param>
    /// <param name="graph">The Road Graph land value's noise term reads.</param>
    /// <param name="terrain">
    /// The <c>[[terrain]]</c> table this world's Ruleset states. Sealing's recovery rate is keyed by
    /// terrain type, so the pass cannot look one up without it; a Ruleset stating no
    /// <c>[[terrain]]</c> heals nowhere, which is <see cref="TerrainRuleset.None"/>.
    /// </param>
    public void Step(Ticks tick, RoadGraph graph, TerrainRuleset terrain)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (Schedule.IsDue(Layer.IndustrialPollution, tick))
        {
            // Absorbed first, then diffused, so the field a Tick publishes is the convolution of the
            // sources as they stand after that Tick's absorption rather than one cadence behind it.
            // The order is hash-bearing and there is no second candidate: diffusing first would
            // publish a field whose sources no longer exist.
            DecayPollution();
            DiffusePollution();
        }

        if (Schedule.IsDue(Layer.LandValue, tick))
        {
            // Retargeted first, then moved, for the same reason absorption precedes diffusion: a Cell
            // steps toward the desirability that holds on THIS Tick rather than the one that held a
            // cadence ago. The order is hash-bearing and the alternative is a whole cadence of lag
            // added to a lag that is already the point of the column.
            SetLandValueTargets(graph);
            DriftLandValue();
        }

        if (Schedule.IsDue(Layer.Sealing, tick))
        {
            DecaySealing(terrain);
        }

        if (Schedule.IsDue(Layer.Woodland, tick))
        {
            RegrowWoodland();
        }
    }

    /// <summary>
    /// Recomputes pollution over the changed sources and their halo. <b>Exact, not approximate.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bit-identical to <see cref="RediffusePollution"/>, and that is the acceptance criterion
    /// rather than a hope.</b> The kernel has bounded support, so an output Cell reads no source
    /// further than <see cref="SeparableKernel.Radius"/> away; recomputing every Cell within that
    /// radius of a changed source therefore recomputes everything that could have moved, and reads the
    /// same sources it would have read in a full pass. Nothing is being approximated. If the two ever
    /// merely agree closely, the scheme has silently become a relaxation and saves will diverge.
    /// </para>
    /// <para>
    /// <b>The dirty region is a bounding rectangle, which is a deliberate over-approximation and is
    /// worth naming as one.</b> Two edits at opposite corners of the map dilate to a rectangle
    /// covering both and everything between, so the incremental path degrades to roughly a full
    /// recompute rather than to two small ones. That costs work and never costs correctness — a
    /// superset of the affected Cells still produces identical values. The refinement, when a profile
    /// asks for it, is a bounded list of rectangles; it is not here because nothing has measured that
    /// the single rectangle is the problem.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The region actually recomputed, or <see cref="CellRect.Empty"/> if nothing was dirty.
    /// </returns>
    public CellRect DiffusePollution()
    {
        if (_pollutionDirty.IsEmpty)
        {
            return CellRect.Empty;
        }

        // Returned rather than discarded, because the halo is the claim. "Only the changed Cells and
        // their surroundings were recomputed" is checkable only if the caller can see which ones, and
        // the headless dump prints it beside the two fields for exactly that reason.
        CellRect recomputed = _pollutionDirty.Dilate(PollutionKernel.Radius).Clamp();

        Diffuse(recomputed);
        _pollutionDirty = CellRect.Empty;

        return recomputed;
    }

    /// <summary>Recomputes pollution over the whole map, ignoring what is dirty.</summary>
    /// <remarks>
    /// <b>Not called by the Tick.</b> It is the reference the incremental path is checked against, and
    /// what a load would call before anything has been marked. Keeping both behind one implementation
    /// — <see cref="LayerDiffusion"/> over two rectangles — is what makes the comparison a test of the
    /// halo arithmetic rather than of two hand-written convolutions agreeing.
    /// </remarks>
    public void RediffusePollution()
    {
        Diffuse(CellRect.World);
        _pollutionDirty = CellRect.Empty;
    }

    /// <summary>
    /// Industrial pollution at a Cell, normalised. Zero where nothing is resident.
    /// </summary>
    /// <remarks>
    /// The single stated rounding of the whole scheme happens here, at the point of use. See
    /// <see cref="SeparableKernel.Normalise"/> for why it cannot happen any earlier.
    /// </remarks>
    public int Pollution(Cells east, Cells north)
    {
        int slot = _residency.Slot(east, north);

        return slot == CellResidency.NotResident
            ? 0
            : PollutionKernel.Normalise(_cells.Pollution[slot]);
    }

    /// <summary>What a Cell emits before diffusion. Zero where nothing is resident.</summary>
    public int PollutionSource(Cells east, Cells north)
    {
        int slot = _residency.Slot(east, north);

        return slot == CellResidency.NotResident ? 0 : _cells.PollutionSource[slot];
    }

    /// <summary>
    /// Moves every Cell's land value one step toward its target. <b>The momentum.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §2.4</c>: land value is stored <em>because it has momentum</em> — it moves slowly toward
    /// the current desirability rather than tracking it, which is both realistic and a stabiliser
    /// against oscillation. A first-order lag with a whole-number time constant is the cheapest thing
    /// that has that shape, and the rounding is stated.
    /// </para>
    /// <para>
    /// <b>Nothing writes the target in Phase 1, so this converges to zero and stays there.</b> That is
    /// the input being absent rather than the mechanism being a placeholder — see
    /// <see cref="Desirability"/>, which is where a caller looking for a non-zero target is told why
    /// there is not one, and which refuses instead of answering. The mechanism runs on its cadence
    /// anyway so that the schedule, the double buffer and the end-of-run bound are exercised by every
    /// long run rather than only by their own tests.
    /// </para>
    /// <para>
    /// <b>It writes the write half and swaps, because it reads its own previous value.</b> This is the
    /// column that makes <see cref="LayerCellTable"/> satisfy <c>adr/0037</c>'s antecedent; pollution
    /// reads a different column from the one it writes and would not have needed it.
    /// </para>
    /// </remarks>
    public void DriftLandValue()
    {
        int tau = _ruleset.Rates.LandValueTau;

        if (tau <= 0)
        {
            return;
        }

        _cells.Rows.PrepareBack();

        for (int slot = 0; slot < _cells.Rows.SlotCount; slot++)
        {
            if (!_cells.Rows.IsLive(slot))
            {
                continue;
            }

            int value = _cells.LandValue[slot];
            int gap = _cells.LandValueTarget[slot] - value;

            _cells.LandValue.AtBack(slot) = value + Step(gap, tau);
        }

        _cells.Rows.SwapBuffers();
    }

    /// <summary>Land value at a Cell. Zero everywhere until something computes desirability.</summary>
    public int LandValue(Cells east, Cells north) => Read(_cells.LandValue, east, north);

    /// <summary>
    /// Sets the desirability a Cell's land value is moving toward. <b>Nothing in Phase 1 calls this.</b>
    /// </summary>
    /// <remarks>
    /// It is public because the momentum operator is only testable against a target somebody supplied,
    /// and because the slice that computes desirability should find the landing site already built
    /// rather than have to decide where it goes. See <see cref="Desirability"/> for what will compute
    /// the argument.
    /// </remarks>
    public void SetLandValueTarget(Cells east, Cells north, int target) =>
        _cells.LandValueTarget[_residency.Ensure(_cells, east, north)] = target;

    /// <summary>
    /// Records that Tiles in a Cell have been built on. <b>Clamped to the Cell's own Tile count.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CONTEXT.md</c> → Sealing: the count of Tiles in a Cell ever built on. One house seals
    /// 1/1024 of its Cell, that denominator being <see cref="CellGrid.TilesInCell"/> — which
    /// <c>adr/0034</c> made a property of the Cell rather than of the Chunk.
    /// </para>
    /// <para>
    /// <b>The clamp is the <c>adr/0006</c> bound, made structural at the only write site.</b> A Cell
    /// cannot have more Tiles built on it than it has Tiles, so this cannot be the accumulator with no
    /// sink that <c>adr/0003</c>'s extension warns about — and the end-of-run tier checks it anyway,
    /// because a bound maintained at one write site stops holding the day somebody adds a second.
    /// </para>
    /// </remarks>
    public void Seal(Cells east, Cells north, int tiles)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tiles);

        int slot = _residency.Ensure(_cells, east, north);
        long sealed_ = (long)_cells.Sealing[slot] + tiles;
        int now = sealed_ > CellGrid.TilesInCell ? CellGrid.TilesInCell : (int)sealed_;

        _cells.Sealing[slot] = now;

        // Building over forest clears it (CONTEXT.md -> Zone), and this is where that happens: not a
        // verb, not an event, and nothing announces it. adr/0158 -- a Cell's Tiles are ONE budget, so
        // Sealing rising IS Woodland falling once the two would overlap. The Timber is forfeited
        // rather than harvested, which is the cost the design chose over a refusal.
        //
        // Clamped rather than decremented by `tiles`. Sealing saturates at the Cell, so a Seal that
        // overran would otherwise take more Woodland than there were Tiles to take -- and the two
        // counts are only comparable at all because they share a denominator.
        int room = CellGrid.TilesInCell - now;

        if (_woodland.At(east, north) > room)
        {
            _woodland.Set(east, north, room);
        }
    }

    /// <summary>How many Tiles in a Cell have been built on. Zero where nothing is resident.</summary>
    public int Sealing(Cells east, Cells north) => Read(_cells.Sealing, east, north);

    /// <summary>
    /// Heals one scheduled step of Sealing everywhere, at the rate that Cell's terrain type states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §2.4</c>: Sealing decays at a Ruleset rate <b>keyed by terrain type</b> — rock may never
    /// recover, floodplain may recover over hundreds of Days. ✅ <b>Milestone 24 task 4 supplied the
    /// key.</b> Until then this read one global time constant pinned at zero, because there was no
    /// terrain to key a rate by; the tau now comes from
    /// <see cref="TerrainRuleset.SealingDecayTau"/>, one per type, looked up per Cell.
    /// </para>
    /// <para>
    /// 🔴 <b>The step is floored at one Tile while the value is positive, and that floor is a fix
    /// rather than a rounding preference.</b> <c>value -= RoundDiv(value, tau)</c> is exponential
    /// decay in integers and it <em>stalls</em>: the decrement rounds to zero once
    /// <c>value &lt; tau ÷ 2</c>, so ground settles at a permanent residue of about half the tau and
    /// never reaches bare. Worse, a tau above <c>2 × </c><see cref="CellGrid.TilesInCell"/> subtracts
    /// nothing on the <em>first</em> update, so a fully-sealed Cell never moves at all. Measured
    /// before the fix: tau 8 stalled at 3, tau 64 at 31, tau 600 at 299, and tau 2400 never moved.
    /// ***Nothing caught it because this method had no caller and every shipped file stated a tau of
    /// zero*** — the shape <c>adr/0043</c> is about, a claim nobody had a machine for.
    /// </para>
    /// <para>
    /// <b>The floor makes the tail linear rather than exponential and that is accepted, not
    /// overlooked.</b> What the design states is an endpoint in Days — <em>"floodplain may recover
    /// over hundreds of Days"</em> — and an exponential that never arrives cannot deliver an endpoint
    /// at all. A curve that is exponential where the quantity is large and one Tile a Day where it is
    /// small reaches bare ground in <c>tau × ln(TilesInCell) + tau ÷ 2</c> updates, which is a number
    /// somebody can check against the sentence.
    /// </para>
    /// <para>
    /// <b>Zero means never, and it is reached by a type stating it rather than by a default.</b> Rock
    /// states 0 (<c>CONTEXT.md</c>: <em>rock may never recover</em>), and a Ruleset with no
    /// <c>[[terrain]]</c> at all heals nowhere — which is what every world before this one did, so no
    /// standing city changes shape because this method gained a caller.
    /// </para>
    /// </remarks>
    /// <param name="terrain">The <c>[[terrain]]</c> table this world's Ruleset states.</param>
    public void DecaySealing(TerrainRuleset terrain)
    {
        if (!terrain.Stated)
        {
            return;
        }

        for (int slot = 0; slot < _cells.Rows.SlotCount; slot++)
        {
            if (!_cells.Rows.IsLive(slot))
            {
                continue;
            }

            int value = _cells.Sealing[slot];

            if (value <= 0)
            {
                continue;
            }

            int tau = terrain.SealingDecayTau(_terrain.At(_cells.East[slot], _cells.North[slot]));

            if (tau <= 0)
            {
                continue;
            }

            int step = IntegerMath.RoundDiv(value, tau);

            _cells.Sealing[slot] = value - (step < 1 ? 1 : step);
        }
    }

    /// <summary>
    /// Puts back one pass of forest everywhere there is room for it. <c>adr/0022</c>, <c>adr/0158</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is the constant <c>adr/0022</c> calls load-bearing by name</b> — <em>"the first
    /// response is more reboot levers, not faster regrowth; regrowth speed is the load-bearing
    /// constant and loosening it deletes the arc"</em> — <b>and until milestone 24 task 8b it had no
    /// owner at all</b>: no ADR, no <c>plans/0002</c> row, no ratifier and no Ruleset key, carried for
    /// the life of the project by that one sentence.
    /// </para>
    /// <para>
    /// <b>The ceiling is <see cref="WoodlandCellTable.Potential"/> and the room is Sealing, and a Cell
    /// gets the smaller.</b> Growing toward the bare Cell would turn every unbuilt Cell into full
    /// forest given time and erase the seed's character, which is the property <c>adr/0022</c> put
    /// Woodland in for. Ignoring Sealing would break <c>adr/0158</c>'s <c>Woodland + Sealing ≤
    /// TilesInCell</c> — ***the one budget the ground has*** — from the only writer that raises
    /// Woodland.
    /// </para>
    /// <para>
    /// <b>A dense whole-map walk, and that is named rather than hidden.</b> <c>02 §10</c> calls a
    /// whole-world sweep the wrong shape and <c>plans/0042</c> <b>F7</b> is this milestone getting
    /// burned by one already. It is taken here because ***forest grows where the city is not***, so
    /// the sparse residency index is precisely the wrong set: the Cells with Layer rows are the Cells
    /// something happened to. The cost is measured rather than asserted and belongs to
    /// <c>plans/0013</c>.
    /// </para>
    /// <para>
    /// <b>Zero means never and is reached by the Ruleset saying nothing.</b> Every world before this
    /// one had a ratchet with no release, which is <c>adr/0006</c>'s concern wearing the other sign —
    /// and it is still reachable, because a file that states no regrowth is a legitimate world and not
    /// a misconfiguration.
    /// </para>
    /// </remarks>
    public void RegrowWoodland()
    {
        int step = _ruleset.Rates.WoodlandTilesPerPass;

        if (step <= 0)
        {
            return;
        }

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            int standing = _woodland.Tiles[cell];
            int ceiling = _woodland.Potential[cell];

            if (standing >= ceiling)
            {
                continue;
            }

            // The room Sealing leaves. Read through the residency index rather than through the
            // Cell query, because that query re-derives the slot from a coordinate this loop already
            // holds -- and it is read at all only for Cells with forest still owed, which on a
            // generated map is a small fraction of the walk.
            int room = CellGrid.TilesInCell - SealingAt(cell);

            if (ceiling > room)
            {
                ceiling = room;
            }

            if (standing >= ceiling)
            {
                continue;
            }

            int grown = standing + step;

            _woodland.Tiles[cell] = grown > ceiling ? ceiling : grown;
        }
    }

    /// <summary>Sealing at a Cell index, or zero where no Layer row exists.</summary>
    private int SealingAt(int cell)
    {
        int slot = _residency.SlotAt(cell);

        return slot < 0 ? 0 : _cells.Sealing[slot];
    }

    /// <summary>
    /// Absorbs one cadence's worth of every Cell's pollution source. <c>adr/0051</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The source is a stock, not a running total.</b> <c>+=</c> is right across <em>emitters</em>
    /// — twenty factories in one Cell must sum — and wrong across <em>firings</em>, where it silently
    /// converts a strength into an elapsed-time counter. This is the removal that reconciles the two:
    /// a quantity per firing against a continuous absorption gives a level proportional to the
    /// <em>rate</em>, which is what <c>02 §2.4</c> says a source field holds. The ceiling is therefore
    /// emergent rather than authored, and <see cref="WorldInvariants.LayerMagnitudesAreBounded"/> goes
    /// back to being the overflow guard it always was.
    /// </para>
    /// <para>
    /// <b>Every decayed Cell is marked dirty, and that cost is the decision's, not this method's.</b>
    /// A decaying source is a changing source, so the incremental set converges on the occupied set
    /// and slice 6's exact re-diffusion stops being a saving wherever industry stands. <c>adr/0051</c>
    /// names this and routes the size of it to a machine rather than to an argument; the fallback it
    /// reserves — a decay cadence coarser than the diffusion cadence — is a third hash-bearing number
    /// and is deliberately not taken here.
    /// </para>
    /// <para>
    /// <b>In place rather than through the back buffer</b>, which is <see cref="DecaySealing"/>'s form
    /// and not <see cref="DriftLandValue"/>'s. The double buffer exists for a column that reads its
    /// own previous value <em>while Phase 5 runs in parallel</em>; decay reads and writes one Cell
    /// with no reference to any other, so there is no cross-Cell read to protect and a swap would buy
    /// nothing.
    /// </para>
    /// </remarks>
    public void DecayPollution()
    {
        int tau = _ruleset.Rates.PollutionTau;

        if (tau <= 0)
        {
            return;
        }

        for (int slot = 0; slot < _cells.Rows.SlotCount; slot++)
        {
            if (!_cells.Rows.IsLive(slot))
            {
                continue;
            }

            int value = _cells.PollutionSource[slot];

            if (value == 0)
            {
                continue;
            }

            _cells.PollutionSource[slot] = value - Step(value, tau);

            MarkPollutionDirty(_cells.East[slot], _cells.North[slot]);
        }
    }

    /// <summary>
    /// <c>layer_cells(aabb, layer)</c> — <c>adr/0002</c>'s hot query. Values, one per Cell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Allocation-free, and the caller owns the buffer.</b> It writes value types into a span the
    /// caller supplied and returns how many it wrote; nothing here allocates, nothing is cached, and
    /// there is no collection to keep. This is the first hot entry point the project has and it sets
    /// the pattern for the rest — <c>adr/0002</c>'s hot flavour is <em>a bounded box the host supplies,
    /// answered every frame</em>, and a query that allocated per frame would be answering it in the
    /// garbage collector's time rather than the host's.
    /// </para>
    /// <para>
    /// <b>Ids and numbers only, and no strings.</b> The Layer arrives as an enum and the readings leave
    /// as Cell coordinates and integers. <c>adr/0002</c>'s real leak vector is not <c>using Godot;</c>
    /// — it is a method that returns a formatted string because a panel wanted one, and an overlay is
    /// exactly the caller that would ask.
    /// </para>
    /// <para>
    /// <b>Values are normalised, so a caller never sees kernel units.</b> Pre-normalised storage is an
    /// internal consequence of exact superposition (<see cref="SeparableKernel.Normalise"/>) and not
    /// something a reader should have to know; a query that handed out raw kernel units would make
    /// every consumer responsible for a rounding decision this slice already made once.
    /// </para>
    /// </remarks>
    /// <param name="area">The box, in Cells. Clamped to the map.</param>
    /// <param name="layer">Which Layer to read.</param>
    /// <param name="into">Where the readings go. Truncated if it is too small; see the return value.</param>
    /// <returns>How many readings were written.</returns>
    public int LayerCells(CellRect area, Layer layer, Span<LayerReading> into)
    {
        CellRect box = area.Clamp();
        int written = 0;

        for (int north = box.North.Raw; north < box.NorthEnd.Raw; north++)
        {
            for (int east = box.East.Raw; east < box.EastEnd.Raw; east++)
            {
                if (written == into.Length)
                {
                    return written;
                }

                Cells x = new(east);
                Cells y = new(north);

                into[written++] = new LayerReading(x, y, Value(layer, x, y));
            }
        }

        return written;
    }

    /// <summary>How large a buffer <see cref="LayerCells"/> needs to answer a box completely.</summary>
    /// <remarks>
    /// <b>The query truncates rather than throwing, and this is what makes that safe.</b> A host that
    /// asks for a box larger than its buffer gets a prefix and a count, which is the right behaviour
    /// for a per-frame query whose box follows a camera; a host that wants all of it sizes with this
    /// first. Neither case allocates inside the query.
    /// </remarks>
    public static int LayerCellCount(CellRect area) => area.Clamp().Count;

    /// <summary>One Layer's value at one Cell, normalised. The switch <see cref="LayerCells"/> reads.</summary>
    public int Value(Layer layer, Cells east, Cells north) => layer switch
    {
        Layer.IndustrialPollution => Pollution(east, north),
        Layer.LandValue => LandValue(east, north),
        Layer.Sealing => Sealing(east, north),
        Layer.Woodland => _woodland.At(east, north),
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>Rebuilds the residency index from the Cell rows. What a load calls.</summary>
    public void RebuildDerived()
    {
        _residency.Rebuild(_cells);

        // The load path, and the easiest of the three to forget: a load RESTORES the terrain rows and
        // never runs the generator, so without this a loaded world reports as having been written to
        // on the Tick it was loaded. adr/0157, milestone 24 task 2.
        _terrainLaidFold = _terrain.Fingerprint();
    }

    /// <summary>
    /// <c>fertility(cell) = base − base·Sealing/1024 − w_p·pollution</c>, Q16.16, <b>composed here
    /// and never stored.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0155</c>, milestone 24 task 5. <b>A proportion</b>: <see cref="Fixed.One"/> is fully
    /// fertile, so the result reads as a percentage and each subtracted term is already the
    /// percentage that term cost — which is what makes <c>adr/0022</c>'s Evidence specimen,
    /// <em>"41% — ground sealed 12%, pollution from Eastfield Industrial 47%"</em>, fall out with no
    /// conversion and no denominator anybody has to name. ***The scale was decided by the readout
    /// rather than by the storage.***
    /// </para>
    /// <para>
    /// <b>Weighted, because unweighted it was never an implementation.</b> The three terms are in
    /// three units — a Ruleset fraction, a Tile count of 0–1024, and a stock measuring about 12 in
    /// kernel units — so a bare subtraction lets Sealing outweigh pollution by roughly <b>85:1</b> on
    /// the strength of the representation alone.
    /// </para>
    /// <para>
    /// <b>Pollution is a count and the weight is a ratio, so the product is already Q16.16 and the
    /// count is never lifted into it</b> — <see cref="Desirability"/>'s rule, and its overflow lesson
    /// with it: lifting first throws at a magnitude <see cref="Invariant"/>'s
    /// <c>LayerMagnitudeIsBounded</c> calls legal.
    /// </para>
    /// <para>
    /// 🔴 <b>The Sealing term has no weight and reads its coefficient off an endpoint.</b> A Cell at
    /// <see cref="CellGrid.TilesInCell"/> has every Tile built on and therefore no farmland, which
    /// pins the term at exactly Base Fertility when the Cell is full. See <see cref="FertilityWeights"/>.
    /// </para>
    /// <para>
    /// ⚠ <b>It goes negative and does NOT clamp.</b> Sealing decays, so <c>base − 1.4·base</c> and
    /// <c>base − 3·base</c> are two Cells at very different distances from farming again, and a clamp
    /// makes them one number. ⚠ <b>The decomposition is not the reason</b> — Evidence reads the terms
    /// — ***it is the ordering between exhausted Cells***, which is what <c>adr/0022</c>'s cyclical
    /// land-use arc runs on. A consumer wanting <em>is there a farm here</em> takes <c>≤ 0</c> and
    /// loses nothing.
    /// </para>
    /// <para>
    /// <b>It saturates rather than throwing</b>, on <c>LineSourceQueries.Saturate</c>'s reasoning that
    /// a read-only query must not throw on a world somebody is allowed to build. ⚠ <b>It still throws
    /// when the RULESET prices no ground, and that is a different thing.</b> Saturation is about a
    /// value; <see cref="Rules.TerrainRuleset.BaseFertility"/>'s refusal is about a <em>declaration</em>
    /// that is absent — it fires on every Cell alike and immediately, which is the shape a
    /// configuration error should have rather than a number somebody reads and believes.
    /// </para>
    /// </remarks>
    /// <param name="terrain">The <c>[[terrain]]</c> table this world's Ruleset states.</param>
    /// <param name="weights">Q16.16 <c>w_p</c>. See <see cref="FertilityWeights"/>.</param>
    /// <param name="east">The Cell, east.</param>
    /// <param name="north">The Cell, north.</param>
    /// <exception cref="InvalidOperationException">The Ruleset states no <c>[[terrain]]</c>.</exception>
    public int Fertility(
        TerrainRuleset terrain, FertilityWeights weights, Cells east, Cells north)
    {
        int ceiling = terrain.BaseFertility(_terrain.At(east, north));

        // base x Sealing / 1024, and the divisor is CellGrid.TilesInCell rather than a number of its
        // own -- the endpoint IS the coefficient. Rounded rather than shifted because the term is a
        // proportion of the ceiling and a truncation biases every Cell the same way.
        long sealed_ = IntegerMath.RoundDiv(
            (long)ceiling * Sealing(east, north), CellGrid.TilesInCell);

        long polluted = (long)weights.Pollution * Pollution(east, north);

        long total = ceiling - sealed_ - polluted;

        return total < int.MinValue ? int.MinValue : total > int.MaxValue ? int.MaxValue : (int)total;
    }

    /// <summary>
    /// <c>− w₂·pollution − w₃·noise</c>. <b>Two of four terms, and the two that are missing are not
    /// missing alike.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Land value is not a term in its own target</b> (<c>adr/0122</c>). It was, and the field
    /// drifts toward this composition, so <c>w₁</c> was a gain of <c>1/(1 − w₁)</c> on the remaining
    /// terms rather than a fifth weight. The momentum operator supplies the persistence it looked like
    /// it was for.
    /// </para>
    /// <para>
    /// ⚠ <b>AMENITY IS ABSENT AND IT IS THE ONLY POSITIVE TERM, so this is bounded above by zero</b>
    /// (<c>adr/0123</c>). A Cell rests at zero when it is clean, quiet and empty, and below zero
    /// everywhere else — <b>the most valuable land in the city is land far from everything.</b> That is
    /// not a hole that fails loudly; it is a working mechanism that says something false about cities,
    /// and it is the opposite of the failure the named-hole discipline was built for. Amenity needs a
    /// <b>kind</b> on a Business, at milestone 15. <b>Shoreline is absent too and differently</b>: it is
    /// zero, and zero is true of every world that exists, because nothing places water until milestone
    /// 24 (<c>adr/0124</c>). ***Absent in the world and absent in the build are not the same absence.***
    /// Neither is defaulted to zero; both are out of the formula.
    /// </para>
    /// <para>
    /// <b>Derived and never stored</b> (<c>02 §2.4</c>): a stored desirability Layer would need
    /// invalidating whenever any input changed, and would drift. Noise is not a Map Layer in any
    /// version of this field:
    /// </para>
    /// <para>
    /// <b>Noise is a point-of-use distance query and belongs here, not in <see cref="Layer"/>.</b>
    /// It <b>sums</b> rather than taking the nearest source — noise superposes, and a nearest-source
    /// query understates a Lot caught between two busy roads — and it enumerates
    /// <b>by loudness rather than by road class</b>: every linear source in range whose contribution
    /// exceeds the ambient background, where the background is the local-Street level it already
    /// computes. That is a crossover rather than an authored threshold, and it is what catches
    /// <c>adr/0029</c>'s Reserved band, which puts Arterial-scale volume on an ordinary grid Street and
    /// which enumeration by class would miss. Near-road pollution is the same query with different
    /// weights.
    /// </para>
    /// <para>
    /// <b>It composes at a TILE, and the Cell that stores land value samples it.</b> Pollution is a
    /// Cell Layer and upsamples; noise is exact at Tile resolution and its whole gradient fits inside
    /// one Cell. ⚠ <b>Composing at the Cell would have collapsed the sub-Cell term</b>, which is the
    /// <em>degrades into is there a road here</em> outcome <c>adr/0034</c> sorted fields by geometry to
    /// avoid — and the shipped geometry makes the obvious sample the worst one: a Cell is 32 Tiles and
    /// <c>[roads] block_tiles</c> is 32, so <b>Streets run along Cell edges and a Cell's centre is
    /// systematically the quietest Tile in it.</b> How a Cell samples this is a stated, hash-bearing
    /// decision and it belongs to the producer, not here.
    /// </para>
    /// <para>
    /// <b>Q16.16, and not land-value units.</b> The sum is a weighted count plus a weighted logarithm;
    /// neither is in the units the land value column stores until the weights say so, and the rounding
    /// belongs where the value is stored rather than where it is computed.
    /// </para>
    /// </remarks>
    /// <param name="near">
    /// An optional presence map letting the noise query skip a Tile no traffic reaches.
    /// <b>Null means do the full scan</b>, so nothing that omits it changes its answer.
    /// </param>
    public int Desirability(
        RoadGraph graph,
        DesirabilityWeights weights,
        Tiles east,
        Tiles north,
        TrafficPresence? near = null)
    {
        ArgumentNullException.ThrowIfNull(graph);

        Cells cellEast = CellGrid.ToCells(east);
        Cells cellNorth = CellGrid.ToCells(north);

        // POLLUTION IS A COUNT AND THE WEIGHT IS A RATIO, so the product is already Q16.16 and the
        // count is never lifted into it. Fixed.Mul(w, Fixed.FromInt(p)) is arithmetically the same
        // thing and OVERFLOWS AT p > 32,767, which is a tenth of what
        // Invariant.LayerMagnitudeIsBounded permits a Cell to hold -- so the composition used to
        // throw on a world the invariant calls legal. That is a missing conversion rather than a
        // width: Fixed.Mul's own remark says the fix for an out-of-range product is a range assertion
        // and not a wider type, and it is right, because the defect here was the conversion.
        long pollution = (long)weights.Pollution * Pollution(cellEast, cellNorth);
        long noise = ((long)weights.Noise
            * LineSourceQueries.Noise(graph, weights.NoiseSource, east, north, near))
            >> Fixed.FractionalBits;

        // Both terms subtract, and there is no term that adds. See the remark above: this is a
        // disamenity field until milestone 15, and its maximum is clean, quiet, empty ground.
        //
        // Saturated rather than checked, on LineSourceQueries.Saturate's reasoning: a read-only query
        // must not throw on a world somebody is allowed to build. The thing that catches a world gone
        // mad is Invariant.LayerMagnitudeIsBounded at end of run, and it is a better instrument for it
        // than an exception raised wherever somebody happened to read a Cell.
        long total = -pollution - noise;

        return total < int.MinValue ? int.MinValue : total > int.MaxValue ? int.MaxValue : (int)total;
    }

    /// <summary>
    /// How many Tiles a Cell samples per axis when it reduces <see cref="Desirability"/> to one
    /// number. <b>Two, so four in all, and it is a design constant rather than Ruleset data.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is hash-bearing and it is not a designer's number</b>, which is the pair of properties
    /// <c>adr/0015</c> does not have a slot for. A designer tunes what a road costs and how loud it
    /// is; nobody tunes the order of a quadrature rule, and exposing it as a key would invite a
    /// reload that silently rewrites every Cell's target. So it is a <c>const</c> here, and the reason
    /// it is not a <c>const</c>-where-Ruleset-data-belongs defect is written down rather than assumed.
    /// </para>
    /// <para>
    /// <b>Two is the lowest order that avoids both systematic errors the shipped geometry offers.</b>
    /// A Cell is <see cref="CellGrid.TilesPerCell"/> = 32 Tiles and <c>[roads] block_tiles</c> is 32,
    /// so the lattice lines land on Cell edges: <b>a single centre sample is systematically the
    /// quietest Tile in the Cell, and a corner sample is systematically the loudest.</b> Four
    /// quadrant-centre samples — the midpoint rule on a 2×2 subdivision — sit at Tile offsets 8 and
    /// 24, on neither the edge nor the centre, and are symmetric under the Cell's own symmetry group,
    /// so no orientation of the lattice is privileged.
    /// </para>
    /// <para>
    /// ⚠ <b>Two is a claim a measurement settles, and the measurement was run rather than argued</b>
    /// (<c>adr/0043</c>). See <c>CellDesirabilitySamplingTests</c>, which computes the field at this
    /// order and at order 4 over the same world and asserts they agree: if a term arrives that varies
    /// faster than a quadrant, that test goes red rather than this comment going quietly wrong.
    /// </para>
    /// </remarks>
    public const int DesirabilitySamplesPerAxis = 2;

    /// <summary>
    /// A Cell's desirability: the mean of <see cref="Desirability"/> over its quadrant centres.
    /// </summary>
    /// <remarks>
    /// <b>This is the reduction from Tile resolution to Cell resolution, and it is the whole of the
    /// sampling decision</b> — see <see cref="DesirabilitySamplesPerAxis"/> for why the sample set is
    /// what it is. The mean rather than the minimum or the maximum, because land value is what an
    /// ordinary Address in the Cell experiences and not what its worst Tile does; a nearest-dominates
    /// reduction belongs to a field whose sources do not superpose, and <c>02 §2.5</c> question 3
    /// already answered <em>superposes</em> for both terms.
    /// </remarks>
    public int CellDesirability(
        RoadGraph graph,
        DesirabilityWeights weights,
        Cells east,
        Cells north,
        TrafficPresence? near = null)
    {
        int stride = IntegerMath.FloorDiv(CellGrid.TilesPerCell, DesirabilitySamplesPerAxis);
        Tiles originEast = CellGrid.ToTiles(east) + new Tiles(IntegerMath.FloorDiv(stride, 2));
        Tiles originNorth = CellGrid.ToTiles(north) + new Tiles(IntegerMath.FloorDiv(stride, 2));

        int total = 0;

        for (int up = 0; up < DesirabilitySamplesPerAxis; up++)
        {
            for (int across = 0; across < DesirabilitySamplesPerAxis; across++)
            {
                total += Desirability(
                    graph,
                    weights,
                    originEast + new Tiles(across * stride),
                    originNorth + new Tiles(up * stride),
                    near);
            }
        }

        return IntegerMath.RoundDiv(
            total, DesirabilitySamplesPerAxis * DesirabilitySamplesPerAxis);
    }

    /// <summary>
    /// Points every resident Cell's land value at its current desirability. <b>The producer, and
    /// <see cref="SetLandValueTarget"/>'s first caller that is not a test.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It walks the rows that exist and creates none</b>, which is why it takes no rectangle and
    /// why it cannot be the thing that makes the Cell table grow with elapsed time (<c>adr/0006</c>).
    /// ⚠ <b>The consequence is worth stating rather than discovering</b>: a Cell has a row because
    /// something emitted pollution into it or sealed a Tile in it, so <b>a Cell carrying roads and
    /// nothing else has no land value at all</b> — not a low one, no row. That is the right set today,
    /// because sealing follows construction and land value is only read where there are Buildings; it
    /// stops being the right set the moment something values empty ground.
    /// </para>
    /// <para>
    /// <b>Q16.16, carried straight through from <see cref="Desirability"/>.</b> The land value column
    /// stores desirability's units because its target is desirability; there is no second scale and
    /// no conversion, and inventing one here would be a number nobody asked for.
    /// </para>
    /// </remarks>
    public void SetLandValueTargets(RoadGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        DesirabilityWeights weights = _ruleset.Desirability;

        // ONE LINEAR SCAN OF THE SEGMENT TABLE, against a pass that queries it four times per Cell.
        // Rebuilt here rather than cached across Ticks: it is scratch, so no load has to rebuild it
        // and no derived column can go quietly unpopulated. See TrafficPresence.
        _traffic.Rebuild(graph, weights.NoiseSource.Range);

        for (int slot = 0; slot < _cells.Rows.SlotCount; slot++)
        {
            if (!_cells.Rows.IsLive(slot))
            {
                continue;
            }

            _cells.LandValueTarget[slot] =
                CellDesirability(graph, weights, _cells.East[slot], _cells.North[slot], _traffic);
        }
    }

    /// <summary>
    /// One step of a first-order integer lag, with the dead band removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>RoundDiv</c> rather than a shift, because the gap is signed.</b> A shift floors, so a
    /// land value above its target would creep down by one for ever while one below it stalled a unit
    /// short. An asymmetric lag is a directional bias in the same family as the smear the double
    /// buffer exists to remove.
    /// </para>
    /// <para>
    /// <b>And a minimum step of one, because an integer lag otherwise has a dead band — which is path
    /// dependence in stored state.</b> <c>RoundDiv(3, 8)</c> is zero, so without this a Cell settles
    /// up to <c>tau/2</c> short of its target and <em>on whichever side it approached from</em>. Two
    /// cities with identical desirability would then hold different land values according to their
    /// histories, which under <c>05 §4</c> is two cities. It cannot oscillate: the step never exceeds
    /// the gap, because a gap of one moves by one.
    /// </para>
    /// </remarks>
    private static int Step(int gap, int tau)
    {
        int step = IntegerMath.RoundDiv(gap, tau);

        if (step != 0)
        {
            return step;
        }

        return gap > 0 ? 1 : gap < 0 ? -1 : 0;
    }

    private int Read(Column<int> column, Cells east, Cells north)
    {
        int slot = _residency.Slot(east, north);

        return slot == CellResidency.NotResident ? 0 : column[slot];
    }

    private void Diffuse(CellRect output)
    {
        _cells.Rows.PrepareBack();

        LayerDiffusion.Run(
            _cells,
            _residency,
            PollutionKernel,
            _cells.PollutionSource,
            _cells.PollutionPass,
            _cells.Pollution,
            output);

        _cells.Rows.SwapBuffers();
    }
}
