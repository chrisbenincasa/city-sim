using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Tables;

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
/// <b>Noise and near-road pollution are built as of milestone 9 task 1</b>; <see cref="Amenity"/> is
/// not, and its blocker is neither of the two this file was written under. ⚠ <b>The sentence that
/// stood here — <em>nothing here is implemented, because there is no Road Graph in Phase 1</em> — was
/// stale from slice 5a, which shipped the Road Graph, and it was one of three saying so in this file
/// alone.</b> The two constraints below were the point of the file and they survive verbatim; they are
/// what the implementation was written against rather than a description added after it.
/// </para>
/// </remarks>
/// <summary>
/// What separates one line-source field from another: how far it carries, and how loud a unit of flow
/// is. <b>The parameters, and there are only two.</b>
/// </summary>
/// <param name="Range">
/// The cutoff. Beyond it a source contributes nothing. Authored in <b>metres</b> by the Ruleset per
/// <c>02 §2.5</c> question 2 and converted once; a range nobody can source is a balance hazard.
/// </param>
/// <param name="IntensityPerFlow">
/// Q16.16 intensity radiated by one Vehicle per Tick of flow at one Tile's distance.
/// <para>
/// ⚠ <b>It is NOT a pure scale, and the obvious reasoning that it is one is wrong.</b> Under a plain
/// logarithm it would be — scaling the argument shifts every level by a constant. The level is
/// <see cref="Transcendental.Log1P"/>, which is linear below unity and logarithmic above it, so this
/// parameter decides <b>which regime the city sits in</b>. Set too low, every intensity lands in the
/// linear stretch, two equal sources come out exactly twice as loud, and the field is the
/// physically-wrong linear sum the logarithm was chosen to prevent — while still looking like a level.
/// </para>
/// </param>
public readonly record struct LineSource(Tiles Range, int IntensityPerFlow);

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
    /// <para>
    /// <b>3. It returns a level, and the sum happens underneath it.</b> <c>02 §2.4</c> says the falloff
    /// is <b>logarithmic</b> and, separately, that the query <b>sums</b> — and it never says in which
    /// domain the sum happens. <b>Summing log-domain values is wrong</b>: two equal sources are half a
    /// bel louder, not twice as loud. So intensities are accumulated linearly and
    /// <see cref="Transcendental.Log1P"/> is applied <b>once, at the end</b>, which makes the wrong
    /// arithmetic unreachable rather than merely discouraged. <c>Log1P</c> and not <c>Log</c> because
    /// silence must return <b>zero</b> and not a large negative number — a Cell nobody drives past is
    /// not quiet, it is silent, and that zero is <em>true</em> rather than a placeholder
    /// (<c>adr/0123</c>).
    /// </para>
    /// <para>
    /// <b>The source strength is a flow and not a headcount.</b> A Segment's volume columns hold the
    /// Vehicles standing on it; what a road radiates goes with the Vehicles <em>passing</em>, so the
    /// standing count is divided by free-flow crossing time. That is the same conversion
    /// <see cref="RoadSegmentTable.LoadOf"/> makes, for the same reason, and <b>free-flow rather than
    /// actual time is deliberate in both</b>: pricing against the current delay would make the answer
    /// depend on itself.
    /// </para>
    /// <para>
    /// ⚠ <b>The one-Tile floor on distance is arithmetic and not a model parameter.</b> It stops a
    /// division by zero for a query standing on the carriageway; it is not <c>02 §2.4</c>'s
    /// <em>50–300 m</em>, which is a **range band** and — like the industrial kernel's 1–10 km — is far
    /// too wide to be a number. It is authored as one range, in metres, by the Ruleset, and it is
    /// unratified.
    /// </para>
    /// </remarks>
    public static int Noise(RoadGraph graph, LineSource source, Tiles east, Tiles north) =>
        Level(graph, source, east, north);

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
    /// <para>
    /// <b>It is the same call with a different <see cref="LineSource"/>, and that is the whole of the
    /// difference.</b> Two fields sharing one implementation is what <c>02 §2.4</c> asks for; two
    /// fields sharing one <em>kernel</em> is the defect <c>adr/0034</c> undid, and they are not the
    /// same thing — the weights and the range are the parameters, and neither field reaches the
    /// other's.
    /// </para>
    /// </remarks>
    public static int NearRoadPollution(RoadGraph graph, LineSource source, Tiles east, Tiles north) =>
        Level(graph, source, east, north);

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
    /// <para>
    /// ⚠ <b>Its blocker is not the one this message used to name, and the correction is specific.</b>
    /// The Road Graph shipped in 5a and Businesses shipped in milestone 10 — <c>BusinessTable</c> holds
    /// <c>building</c>, <c>balance</c> and <c>building_next</c>, and <b>no kind</b>. Amenity is the count
    /// of <em>distinct Business types</em> reachable on foot, so what is missing is that a Business has
    /// no type to be distinct in. One column and a catchment query, both milestone 15's
    /// (<c>adr/0123</c>).
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Always, until a Business has a kind.</exception>
    public static int Amenity(Tiles east, Tiles north) =>
        throw new NotSupportedException(
            $"amenity at Tile ({east.Raw}, {north.Raw}) is a walkable catchment on the Road Graph — a "
            + "time rather than a distance. The Road Graph exists; what does not is a kind on a "
            + "Business, so there are no distinct types to count (milestone 15, adr/0123).");

    /// <summary>
    /// The shared implementation. <b>Sums intensities, then takes one logarithm.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The source set is found two ways because the Road Graph stores it two ways.</b>
    /// <see cref="StreetGrid"/> holds lattice Streets, so those are reached by arithmetic over a window
    /// of blocks — no search, and the window is exact rather than generous. Everything else — every
    /// Arterial, every off-lattice Street — is in <see cref="StreetGrid.OffLatticeCount"/> and is
    /// scanned linearly. ⚠ <b>A lattice-only query would have been silently quiet</b> around exactly the
    /// loudest roads, since <c>02 §2.4</c> names <em>Arterials within ~300 m</em> as a source.
    /// </para>
    /// <para>
    /// <b>The background is computed first and every other source is measured against it.</b> That is
    /// <c>02 §2.4</c>'s enumerate-by-loudness rule: a source joins the sum only where its own
    /// contribution exceeds the level the Tile's own frontage Street already puts there. <b>Nobody
    /// authors that threshold</b> — it is a crossover, and the enumerated set is small by definition
    /// because standing out above the background is what makes a source enumerable.
    /// </para>
    /// <para>
    /// ⚠ <b>The frontage Street is in the sum unconditionally and is not tested against itself.</b> It
    /// is the background; a source cannot exceed itself, and dropping it would silence the road the
    /// query is standing on.
    /// </para>
    /// </remarks>
    private static int Level(RoadGraph graph, LineSource source, Tiles east, Tiles north)
    {
        ArgumentNullException.ThrowIfNull(graph);

        StreetGrid streets = graph.Streets;
        int block = streets.BlockTiles;
        int range = source.Range.Raw;

        if (range <= 0)
        {
            return 0;
        }

        // The window is ceil(range / block) blocks each way: a Segment more than that many block steps
        // off cannot have a point within range, because a block IS the lattice pitch.
        int window = block > 0 ? IntegerMath.CeilDiv(range, block) : 0;
        int column = block > 0 ? IntegerMath.FloorDiv(east.Raw, block) : 0;
        int row = block > 0 ? IntegerMath.FloorDiv(north.Raw, block) : 0;

        // Pass one: the nearest local Street, which is what sets the ambient background.
        int local = Rows.NoSlot;
        int nearest = int.MaxValue;

        for (int c = column - window; block > 0 && c <= column + window; c++)
        {
            for (int r = row - window; r <= row + window; r++)
            {
                Nearer(graph, streets.Horizontal(c, r), east, north, ref local, ref nearest);
                Nearer(graph, streets.Vertical(c, r), east, north, ref local, ref nearest);
            }
        }

        // An off-lattice STREET is still a local Street and still sets the background; an Arterial is a
        // thing that stands out FROM the background and must never be it. A world whose Streets do not
        // align to the declared lattice has no lattice entries at all, and skipping this loop would give
        // it no background -- which disables the crossover silently, exactly as Frontage.Locate did.
        for (int index = 0; index < streets.OffLatticeCount; index++)
        {
            int slot = streets.OffLatticeAt(index);

            if (slot != Rows.NoSlot && (RoadKind)graph.Segments.Kind[slot] == RoadKind.Street)
            {
                Nearer(graph, slot, east, north, ref local, ref nearest);
            }
        }

        int background = local == Rows.NoSlot ? 0 : Contribution(graph, source, local, east, north);

        // Pass two: everything that stands above it. The background is in the sum unconditionally.
        long total = background;

        for (int c = column - window; block > 0 && c <= column + window; c++)
        {
            for (int r = row - window; r <= row + window; r++)
            {
                total += Above(graph, source, streets.Horizontal(c, r), local, background, east, north);
                total += Above(graph, source, streets.Vertical(c, r), local, background, east, north);
            }
        }

        for (int index = 0; index < streets.OffLatticeCount; index++)
        {
            total += Above(graph, source, streets.OffLatticeAt(index), local, background, east, north);
        }

        // Saturate rather than overflow. A level is a logarithm, so the clamp costs a fraction of a
        // bel at the very top and the alternative is a checked() throw inside a read-only query.
        int summed = total > int.MaxValue ? int.MaxValue : (int)total;

        return Transcendental.Log1P(summed);
    }

    /// <summary>Keeps the nearer of a candidate Street and the best so far.</summary>
    /// <remarks>
    /// ⚠ <b><see cref="Frontage.Locate"/> looks like this function and is not.</b> It answers <em>which
    /// Segment does this Address front</em>, and returns nothing at all unless the Tile lies exactly on
    /// a lattice line — correct for an Address, which is never anywhere else, and silently zero for the
    /// arbitrary Tile a field query asks about. Using it here made the background zero everywhere except
    /// on the carriageway, which disabled the enumerate-by-loudness rule without failing anything.
    /// </remarks>
    private static void Nearer(
        RoadGraph graph, int slot, Tiles east, Tiles north, ref int best, ref int nearest)
    {
        if (slot == Rows.NoSlot)
        {
            return;
        }

        int distance = DistanceTiles(graph, slot, east, north);

        if (distance >= 0 && distance < nearest)
        {
            nearest = distance;
            best = slot;
        }
    }

    /// <summary>Clamps to <see cref="int.MaxValue"/> so a very loud road stays representable.</summary>
    private static long Saturate(long value) => value > int.MaxValue ? int.MaxValue : value;

    /// <summary>A source's contribution, or zero unless it stands above the background.</summary>
    private static int Above(
        RoadGraph graph, LineSource source, int slot, int frontage, int background, Tiles east, Tiles north)
    {
        if (slot == Rows.NoSlot || slot == frontage)
        {
            return 0;
        }

        int contribution = Contribution(graph, source, slot, east, north);

        return contribution > background ? contribution : 0;
    }

    /// <summary>
    /// One Segment's intensity at a Tile. <b>Flow over distance, and zero outside the range.</b>
    /// </summary>
    private static int Contribution(RoadGraph graph, LineSource source, int slot, Tiles east, Tiles north)
    {
        RoadSegmentTable segments = graph.Segments;

        long volume = (long)segments.VolumeForward[slot] + segments.VolumeBackward[slot];

        if (volume <= 0)
        {
            return 0;
        }

        TravelTime freeFlow = segments.FreeFlowOver(slot);

        if (freeFlow.Raw <= 0 || freeFlow.IsImpassable)
        {
            return 0;
        }

        int distance = DistanceTiles(graph, slot, east, north);

        if (distance < 0 || distance > source.Range.Raw)
        {
            return 0;
        }

        // Vehicles per Tick of flow, from the Vehicles standing on the Segment: the same occupancy-to-
        // rate conversion RoadSegmentTable.LoadOf makes, and against free-flow time for the same reason.
        //
        // In long, and SATURATED at each step rather than checked. Fixed.Mul throws on overflow, and a
        // read-only query that throws on a busy road is worse than one that reports a very loud one --
        // the same reasoning RoadSegmentTable.MaxVolume records, where a clamp bounds the arithmetic
        // and never the model. Saturating the flow first is what keeps the product below long's range:
        // two int-ranged operands multiply to at most 4.6e18 against long's 9.2e18.
        // Fixed.One TWICE: the numerator is a plain count and the denominator is Q16.16, so one factor
        // converts the count and the other cancels the denominator's scale. Dropping the second is a
        // silent 65,536x error that leaves every intensity far below unity -- where Log1P is linear and
        // the level stops being a level. A test asserting that two sources are SUB-linear caught it; one
        // asserting a number would have been rewritten to match.
        long flow = Saturate(IntegerMath.RoundDiv(volume * Fixed.One * Fixed.One, freeFlow.Raw));
        long scaled = Saturate((flow * source.IntensityPerFlow) >> Fixed.FractionalBits);

        // One Tile is the floor, and it is a representation artefact rather than a tuning number: a
        // query standing on the carriageway is at distance zero and 1/0 is not a loud road.
        return (int)IntegerMath.RoundDiv(scaled, distance < 1 ? 1 : distance);
    }

    /// <summary>
    /// Tiles from a point to the nearest point of a Segment. <b>Exact at Tile resolution, which is the
    /// property that made this a query rather than a Layer.</b>
    /// </summary>
    /// <remarks>
    /// Integer throughout and rounded once, at the projection. The closest point of a finite segment is
    /// the projection clamped to the endpoints, which is why a Tile beyond a Segment's end measures to
    /// the end and not to the infinite line through it.
    /// </remarks>
    private static int DistanceTiles(RoadGraph graph, int slot, Tiles east, Tiles north)
    {
        RoadSegmentTable segments = graph.Segments;
        RoadNodeTable nodes = graph.Nodes;

        if (!nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
            || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
        {
            return -1;
        }

        long ax = nodes.East[a].Raw;
        long ay = nodes.North[a].Raw;
        long dx = nodes.East[b].Raw - ax;
        long dy = nodes.North[b].Raw - ay;

        long px = east.Raw - ax;
        long py = north.Raw - ay;

        long length = (dx * dx) + (dy * dy);
        long closestX = ax;
        long closestY = ay;

        if (length > 0)
        {
            long projection = (px * dx) + (py * dy);

            projection = projection < 0 ? 0 : projection > length ? length : projection;

            closestX = ax + IntegerMath.RoundDiv(projection * dx, length);
            closestY = ay + IntegerMath.RoundDiv(projection * dy, length);
        }

        long offX = east.Raw - closestX;
        long offY = north.Raw - closestY;

        return (int)IntegerMath.SqrtFloor((offX * offX) + (offY * offY));
    }
}
