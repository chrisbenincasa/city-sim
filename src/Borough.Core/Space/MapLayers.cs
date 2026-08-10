using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
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
/// invalidating whenever any input changes, and drifts. See <see cref="Fertility"/> and
/// <see cref="Desirability"/>, which are named holes rather than placeholders returning zero.
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
    private readonly CellResidency _residency = new();

    private LayerRuleset _ruleset;
    private CellRect _pollutionDirty = CellRect.Empty;

    /// <param name="ruleset">The cadence, the rates and the constants. See <see cref="LayerRuleset"/>.</param>
    public MapLayers(LayerRuleset ruleset)
    {
        _cells = new LayerCellTable(InitialCapacity);
        _ruleset = ruleset;
        PollutionKernel = LayerKernels.IndustrialPollution(ruleset.Constants);
    }

    /// <summary>The Cell rows themselves.</summary>
    public LayerCellTable Cells => _cells;

    /// <summary>Which slot holds which Cell.</summary>
    public CellResidency Residency => _residency;

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
    /// <b>Sealing is not here, and its absence is the schedule being honest.</b> It changes on build,
    /// so <see cref="LayerSchedule.For"/> answers <see cref="LayerCadence.Never"/> for it rather than
    /// carrying a period nobody reads.
    /// </remarks>
    public void Step(Ticks tick)
    {
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
            DriftLandValue();
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

        _cells.Sealing[slot] = sealed_ > CellGrid.TilesInCell ? CellGrid.TilesInCell : (int)sealed_;
    }

    /// <summary>How many Tiles in a Cell have been built on. Zero where nothing is resident.</summary>
    public int Sealing(Cells east, Cells north) => Read(_cells.Sealing, east, north);

    /// <summary>
    /// Decays Sealing by one step. <b>Not scheduled, because its rate has no key yet.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §2.4</c>: Sealing decays at a Ruleset rate <b>keyed by terrain type</b> — rock may never
    /// recover, floodplain may recover over hundreds of Days. There is no terrain in Phase 1, so there
    /// is no key to look a rate up by, so <see cref="LayerRates.Default"/> sets the time constant to
    /// zero and this does nothing. The operator exists so that the slice which adds terrain finds the
    /// mechanism rather than inventing it beside the storage.
    /// </remarks>
    public void DecaySealing()
    {
        int tau = _ruleset.Rates.SealingDecayTau;

        if (tau <= 0)
        {
            return;
        }

        for (int slot = 0; slot < _cells.Rows.SlotCount; slot++)
        {
            if (_cells.Rows.IsLive(slot))
            {
                int value = _cells.Sealing[slot];
                _cells.Sealing[slot] = value - IntegerMath.RoundDiv(value, tau);
            }
        }
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
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>Rebuilds the residency index from the Cell rows. What a load calls.</summary>
    public void RebuildDerived() => _residency.Rebuild(_cells);

    /// <summary>
    /// <c>fertility(cell) = terrain suitability − Sealing − pollution</c>. <b>A named hole.</b>
    /// </summary>
    /// <remarks>
    /// <b>It throws rather than returning zero, and the throw is the deliverable.</b> Terrain
    /// suitability needs the world generator, which does not exist; Sealing arrives in task 6. A
    /// placeholder returning zero is a value that will be read, believed, and tuned around — and by the
    /// time the generator lands, something will depend on farms yielding nothing. A hole that fails
    /// loudly is a hole. <c>02 §2.3</c>, <c>plans/0009</c> task 7.
    /// </remarks>
    /// <exception cref="NotSupportedException">Always, until the generator and Sealing exist.</exception>
    public int Fertility(Cells east, Cells north) =>
        throw new NotSupportedException(
            $"fertility at Cell ({east.Raw}, {north.Raw}) composes from terrain suitability, which "
            + "needs the world generator (02 §2.3). Composed at the point of use and never stored.");

    /// <summary>
    /// <c>w₁·land_value − w₂·pollution − w₃·noise + w₄·amenity − w₅·shoreline</c>. <b>A named hole.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived and never stored</b> (<c>02 §2.4</c>): a stored desirability Layer would need
    /// invalidating whenever any input changed, and would drift. Three of its five inputs do not
    /// exist. Noise and amenity both need the Road Graph, and neither is a Map Layer:
    /// </para>
    /// <para>
    /// <b>Noise is a point-of-use distance query and belongs here, not in <see cref="Layer"/>.</b>
    /// When it is built it must <b>sum</b> rather than take the nearest source — noise superposes, and
    /// a nearest-source query understates a Lot caught between two busy roads — and it must enumerate
    /// <b>by loudness rather than by road class</b>: every linear source in range whose contribution
    /// exceeds the ambient background, where the background is the local-Street level it already
    /// computes. That is a crossover rather than an authored threshold, and it is what catches
    /// <c>adr/0029</c>'s Reserved band, which puts Arterial-scale volume on an ordinary grid Street and
    /// which enumeration by class would miss. Near-road pollution is the same query with different
    /// weights.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Always, until the Road Graph exists.</exception>
    public int Desirability(Cells east, Cells north) =>
        throw new NotSupportedException(
            $"desirability at Cell ({east.Raw}, {north.Raw}) composes noise and amenity, which are "
            + "queries on a Road Graph that does not exist in Phase 1 (02 §2.4, adr/0034).");

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
