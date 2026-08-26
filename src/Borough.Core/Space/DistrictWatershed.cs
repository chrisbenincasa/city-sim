using Borough.Core.Arithmetic;
using Borough.Core.Invariants;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Finds the Districts: a persistence-seeded watershed over the Building-density field, clipped to a
/// road component.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>adr/0134</c> as an algorithm.</b> The ADR settles that a District is a centre and the
/// basin that drains to it, so the count follows the number of centres rather than a ceiling on
/// extent; a watershed is the operator that turns a field into exactly that. The field is
/// <see cref="BuildingResidency.Density"/> and nothing else — it is not a Map Layer, it is not
/// smoothed, and the decision not to smooth it was taken against a measurement rather than by argument
/// (<c>plans/0037</c> F8).
/// </para>
/// <para>
/// <b>Two floods, and the second is the one that assigns.</b> The first flood asks which maxima are
/// real: when two basins meet at level <em>h</em>, the lower peak dies, and its <em>prominence</em> is
/// its own height minus <em>h</em>. A peak whose prominence clears
/// <see cref="DistrictRuleset.ProminencePercent"/> is a genuine centre and becomes a seed; one that
/// does not is a bump on the side of a bigger hill. The second flood repeats the descent and refuses
/// exactly one merge — the one that would join two sets that each already hold a seed — which is what
/// draws the boundary. ⚠ <b>One flood cannot do this</b>: whether a merge should be refused depends on
/// a prominence that is not known until the whole descent has run.
/// </para>
/// <para>
/// <b>The threshold is relative — a percentage of the dying peak's own height</b> — and that is a
/// decision rather than a convenience. An absolute count would be silently tied to
/// <c>lots_per_segment = 5</c>, which is what makes a built Cell hold ten Buildings on the shipped
/// lattice: a threshold of, say, three would mean <em>a third of a peak</em> today and something else
/// entirely the day the lattice changes. ⚠ <b>It is hash-bearing and unratified</b>; its
/// <c>plans/0002</c> §D1 row names milestone 15 as the ratifier, because the field is flat on every
/// shipped Ruleset and a threshold over a flat field has nothing to discriminate.
/// </para>
/// <para>
/// <b>The road-component clip is constitutive and not a filter.</b> Two Cells never merge across a
/// road-component boundary however dense they both are, because a Pool that two Buildings cannot reach
/// each other through is not one Pool — <c>adr/0134</c> makes the component part of what a District
/// <em>is</em>. ⚠ <b>The component read is the FOOT subgraph</b>, which is the weaker of the two: a
/// world reachable on foot is reachable by car and not conversely, so the foot component is the one
/// that can actually separate a city that drives everywhere. ⚠ <b>No shipped Ruleset exercises the
/// clip</b> — <c>twinned.toml</c> is deliberately one component — so it is held by an in-code fixture
/// that cuts the corridor, and that absence is the point rather than a gap.
/// </para>
/// <para>
/// ⚠ <b>It runs once, at world creation.</b> No persistence, no hysteresis, no damping and no
/// per-evaluation Cell bound: those are milestone 12 task 4, and they are what earn a District its
/// <c>Saved</c> disposition. Nothing here reads the previous extent, so calling this twice on one
/// world produces the same answer twice — which will stop being true at task 4, on purpose.
/// </para>
/// <para>
/// ⚠ <b>It is not <c>RoutingPartition</c> and must not be reused as one</b>
/// (<c>adr/0047</c>).
/// </para>
/// </remarks>
public static class DistrictWatershed
{
    /// <summary>An ordinal that belongs to no basin yet.</summary>
    private const int NoOrdinal = -1;

    /// <summary>
    /// Brings the world's Districts to what the Building-density field currently supports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It reconciles rather than replaces, and milestone 12 task 4 is where that started
    /// mattering.</b> A District keeps its row across a re-evaluation — task 5 hangs Pool Bins off it —
    /// so the answer is applied as a set of changes to what is there, damped and with hysteresis,
    /// rather than as a fresh table. Task 3 cleared and rebuilt because nothing yet depended on the
    /// row surviving.
    /// </para>
    /// <para>
    /// <b>It clears outright in exactly two cases, and both mean <em>this city has no Districts</em></b>:
    /// a Ruleset that does not state <c>[districts]</c>, and a world with nothing built on it. A
    /// Ruleset that stops stating the table is a city that stops having Districts, and a stale row
    /// surviving that reload would be a District with no rule behind it — worse than none, because
    /// <c>Scope.Pool</c> would resolve against it.
    /// </para>
    /// <para>
    /// ⚠ <b>It does not consult the Tick and has no cadence of its own.</b> <see cref="Rules"/>'s
    /// <c>revisit_ticks</c> decides when this is called, and the caller is phase 6 — which is where the
    /// Buildings this reads are created and destroyed. Calling it out of cadence is legitimate and is
    /// what world creation does.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It takes the <see cref="World"/> and it used to take eight tables, and task 5 is what
    /// changed it.</b> The old list was defensible while this only <em>read</em>: a signature that
    /// enumerates what an operator touches is a real property, and it is worth several parameters.
    /// Retiring a Pool ends that — the operator now moves Goods between Bins and wakes whoever was
    /// waiting on the one it frees, which is the world's write path and not a table.
    /// ***A ninth, tenth and eleventh parameter naming the World's own tables is a copy of the World
    /// that drifts***, and the enumeration had already stopped being the point.
    /// </para>
    /// </remarks>
    public static void Evaluate(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        DistrictTable districts = world.Districts;
        DistrictCellTable cells = world.DistrictCells;
        DistrictResidency residency = world.DistrictsInCells;
        DistrictRuleset rules = world.Rules.Districts;

        if (!rules.Runs)
        {
            Clear(world, districts, cells, residency);
            return;
        }

        Basins basins = Collect(world.BuildingsInCells, world.Buildings, world.Lots, world.Roads);

        if (basins.Count == 0)
        {
            Clear(world, districts, cells, residency);
            return;
        }

        int[] order = ByDescendingDensity(basins);

        bool[] seeds = Seed(basins, order, rules.ProminencePercent);
        Proposal proposal = Assign(basins, order, seeds);

        Reconcile(world, districts, cells, residency, basins, proposal, rules);
    }

    /// <summary>Frees every District row and empties the index.</summary>
    /// <remarks>
    /// <b>Every District here dies with no heir, and that is the truth rather than a shortcut.</b>
    /// This runs when the city has no Districts at all — no <c>[districts]</c> table, or nothing built
    /// — so there is no row for a Pool to be handed to. <c>World.RetirePool</c> is asked with the unset
    /// handle and <see cref="Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool"/> decides whether that was
    /// allowed, which is the same question asked in the same place as on the reconciliation path.
    /// </remarks>
    private static void Clear(
        World world, DistrictTable districts, DistrictCellTable cells, DistrictResidency residency)
    {
        for (int slot = cells.Rows.SlotCount - 1; slot >= 0; slot--)
        {
            if (cells.Rows.IsLive(slot))
            {
                cells.Rows.Free(cells.Rows.At(slot));
            }
        }

        for (int slot = districts.Rows.SlotCount - 1; slot >= 0; slot--)
        {
            if (districts.Rows.IsLive(slot))
            {
                world.RetirePool(slot, default);

                districts.Rows.Free(districts.Rows.At(slot));
            }
        }

        residency.Rebuild(cells);
    }

    /// <summary>
    /// The built Cells, in Cell-index order, with each one's density and road component.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A full scan of the Cell grid rather than a walk of the Buildings</b>, and the reason is
    /// determinism rather than speed. The ordinals this produces are the identity every array below is
    /// keyed by, and a walk of the Building table would number them in Building-slot order — which
    /// depends on the order rows were allocated and recycled, so a saved-and-reloaded city could
    /// number the same Cells differently and tie-break a merge the other way. Cell-index order is a
    /// property of the map. The scan is 262,144 loads, once, at world creation.
    /// </para>
    /// <para>
    /// <b>A Cell's component is its lowest-slot Building's</b>, which is
    /// <see cref="BuildingResidency.NthIn"/> at ordinal zero because that list is kept in slot order.
    /// A Cell whose Buildings sit in two components is possible — 128 m of ground can straddle a cut —
    /// and it resolves to one of them rather than to both. ⚠ <b>That is a tie-break and not a
    /// finding</b>: nothing measures how often it happens, because no shipped Ruleset has two
    /// components at all.
    /// </para>
    /// </remarks>
    private static Basins Collect(
        BuildingResidency density, BuildingTable buildings, LotTable lots, RoadGraph roads)
    {
        int[] ordinalOf = new int[CellGrid.WorldCellCount];
        int[] held = HeldForTrade(lots, density);

        List<int> cellIndex = [];
        List<int> heights = [];
        List<int> components = [];

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells e = new(east);
                Cells n = new(north);

                int height = density.Density(e, n) + held[CellGrid.Index(e, n)];

                if (height <= 0)
                {
                    continue;
                }

                ordinalOf[CellGrid.Index(e, n)] = cellIndex.Count + 1;

                cellIndex.Add(CellGrid.Index(e, n));
                heights.Add(height);
                components.Add(ComponentOf(density, buildings, lots, roads, e, n));
            }
        }

        return new Basins(
            [.. cellIndex], [.. heights], [.. components], ordinalOf);
    }

    /// <summary>
    /// Vacant Lots zoned for a trade, per Cell — <b>settlement that is standing empty on purpose</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Without this a commercial block reads as a HOLE in the density field, and the watershed
    /// shatters.</b> <c>adr/0165</c>'s land-use split leaves one block in
    /// <c>SyntheticCity.TradeBlockStride</c> permitted to a trade and unbuilt, and a block's Lots sit
    /// on the faces it shares with its neighbours — so a trade block does not merely zero its own
    /// Cell, it **thins the Cells around it**. Measured on `minimal.toml` at 16,000 Citizens before
    /// this existed: an interior Cell holding 10 Buildings fell to **5**, a one-lattice world reported
    /// **8** concentrations instead of one, and `twinned.toml` reported 6 and 12 instead of two.
    /// </para>
    /// <para>
    /// <b>The field measures where the settlement IS, and land a Zone holds vacant for a shop is
    /// inside the settlement.</b> That is why this counts rather than smooths. ***A kernel would have
    /// been answering noise that is not there***: <c>plans/0037</c> **F8** measured the field before
    /// refusing to smooth it — *"the field is not noisy, it is flat"* — and that argument survives
    /// this change intact, where a radius would have replaced it with a fifth hash-bearing number
    /// <c>adr/0134</c> does not enumerate and nothing in milestone 26 could ratify.
    /// </para>
    /// <para>
    /// ⚠ <b>It counts VACANT trade Lots only, so the height does not move when a shop is built</b> —
    /// the Lot leaves this count and arrives in <see cref="BuildingResidency.Density"/> in the same
    /// instant. ***A District that grew when its first shop opened would be a District reacting to
    /// construction rather than to settlement.***
    /// </para>
    /// <para>
    /// ⚠ <b>Vacant HOUSING land is deliberately NOT counted, and the asymmetry is the point.</b>
    /// Trade land is vacant because a permission set is holding it; housing land is vacant because
    /// nobody wanted it. ***The first is a statement about the city and the second is the absence of
    /// one***, which is why the extent stays *built Cells only* everywhere else (<c>plans/0037</c>).
    /// </para>
    /// </remarks>
    private static int[] HeldForTrade(LotTable lots, BuildingResidency density)
    {
        int[] held = new int[CellGrid.WorldCellCount];

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot)
                || !lots.IsVacant(slot)
                || (lots.Zone[slot] & LotTable.Trade) == 0)
            {
                continue;
            }

            Cells east = CellGrid.ToCells(lots.East[slot]);
            Cells north = CellGrid.ToCells(lots.North[slot]);

            if (!HasBuiltNeighbour(density, east, north))
            {
                continue;
            }

            held[CellGrid.Index(east, north)]++;
        }

        return held;
    }

    /// <summary>
    /// Whether this Cell or one of its eight neighbours holds a Building.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The clause that keeps <em>held for a trade</em> meaning INSIDE A SETTLEMENT rather than
    /// merely ZONED</b>, and it was found by a test rather than by argument.
    /// <c>DistrictReevaluationTests.A_district_whose_centre_is_demolished_is_destroyed</c> razes every
    /// Building east of an authored gap and expects the eastern District to die. Without this it
    /// **survived its own demolition**: the commercial Lots were still zoned, still vacant, still
    /// counted, and a District went on standing over ground with nothing on it.
    /// </para>
    /// <para>
    /// <b>Zoning is a statement about a city, and it stops being one when the city goes.</b> A
    /// commercial block among houses is land the settlement is holding for a shop; the same block
    /// with every neighbour razed is a line on a map. ***The permission set did not change and what it
    /// MEANS did***, which is why this reads the Buildings around it rather than the bits on it.
    /// </para>
    /// <para>
    /// ⚠ <b>Eight-neighbour and not four</b>, because a block's Lots sit on the faces it shares with
    /// its neighbours, so a trade block's diagonal is as much its surroundings as its orthogonal is.
    /// </para>
    /// </remarks>
    private static bool HasBuiltNeighbour(BuildingResidency density, Cells east, Cells north)
    {
        for (int dn = -1; dn <= 1; dn++)
        {
            for (int de = -1; de <= 1; de++)
            {
                int e = east.Raw + de;
                int n = north.Raw + dn;

                if (e < 0 || n < 0 || e >= CellGrid.WorldCells || n >= CellGrid.WorldCells)
                {
                    continue;
                }

                if (density.Density(new Cells(e), new Cells(n)) > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// The Foot component of the first Building in a Cell that has an Address, or <c>Unlabelled</c>.
    /// </summary>
    /// <remarks>
    /// <b>The first that HAS one, rather than simply the first.</b> A Lot sitting exactly on an
    /// intersection fronts nothing — <c>CONTEXT.md</c> → Address is emphatic that an Address is
    /// <em>never a Node</em>, and <see cref="Frontage.Locate"/> returns nothing for such a position
    /// rather than tie-breaking between two Segments. Taking the lowest-slot Building unconditionally
    /// made every Cell in a hand-built fixture answer <c>Unlabelled</c> while every Cell was in fact on
    /// a Street, which is a Cell reporting the property of one of its Buildings rather than its own.
    /// </remarks>
    private static int ComponentOf(
        BuildingResidency density,
        BuildingTable buildings,
        LotTable lots,
        RoadGraph roads,
        Cells east,
        Cells north)
    {
        CellRect cell = CellRect.At(east, north);
        int here = density.CountIn(cell);

        for (int ordinal = 0; ordinal < here; ordinal++)
        {
            int building = density.NthIn(cell, buildings, ordinal);

            if (building == Rows.NoSlot
                || !lots.Rows.TryResolve(buildings.Lot[building], out int lot))
            {
                continue;
            }

            int found = ComponentAt(lots, roads, lot);

            if (found != RoadConnectivity.Unlabelled)
            {
                return found;
            }
        }

        // ⚠ AND THEN THE GROUND HELD FOR A TRADE, which has no Building to ask. A Cell whose only
        // occupants are vacant commercial Lots is in the settlement -- HeldForTrade gave it a height
        // -- so it must also be able to name a road component, or it is a Cell the field admits and
        // the clip immediately severs. ***That is how a fixed density field still produced three
        // Districts where there is one lattice***: the height was right and the component was
        // Unlabelled, and an unlabelled Cell may never merge with anything.
        //
        // A vacant Lot HAS a frontage -- the subdivider gave it one, because frontage is the
        // geometric precondition for a Lot existing at all (CONTEXT.md -> Frontage) -- so this asks
        // the same question of the same column by a different route.
        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot)
                || !lots.IsVacant(slot)
                || (lots.Zone[slot] & LotTable.Trade) == 0
                || CellGrid.ToCells(lots.East[slot]).Raw != east.Raw
                || CellGrid.ToCells(lots.North[slot]).Raw != north.Raw)
            {
                continue;
            }

            int found = ComponentAt(lots, roads, slot);

            if (found != RoadConnectivity.Unlabelled)
            {
                return found;
            }
        }

        return RoadConnectivity.Unlabelled;
    }

    /// <summary>The Foot component of one Lot's frontage, or <c>Unlabelled</c>.</summary>
    /// <remarks>
    /// <b>Factored out because two walks now ask it</b> — the Buildings in a Cell, and the ground a
    /// Zone holds vacant for a trade — and a second copy would be the place the two answers drift.
    /// </remarks>
    private static int ComponentAt(LotTable lots, RoadGraph roads, int lot)
    {
        int frontage = lots.FrontageSlot[lot];

        if (frontage == 0 || !roads.Segments.Rows.IsLive(frontage - 1))
        {
            return RoadConnectivity.Unlabelled;
        }

        return roads.Nodes.Rows.TryResolve(roads.Segments.NodeA[frontage - 1], out int node)
            ? roads.Nodes.FootComponent[node]
            : RoadConnectivity.Unlabelled;
    }

    /// <summary>
    /// Whether two Cells may drain into one another. <b>An unlabelled Cell may never merge, including
    /// with another unlabelled Cell.</b>
    /// </summary>
    /// <remarks>
    /// <b>Unlabelled is <em>unknown</em>, and unknown must not be an equivalence class.</b> A Cell
    /// answers <c>Unlabelled</c> when nothing in it stands on a Street the connectivity pass reached —
    /// which says that its reachability is unknown, not that it shares a component with every other
    /// such Cell across the map. Comparing the two labels for equality made <c>Unlabelled</c> behave as
    /// the largest component in the world and silently bridged two genuinely separate ones; a fixture
    /// with two islands in it came out as one District with every assertion about the field passing.
    /// ⚠ <b>The consequence is visible rather than silent</b>: an unlabelled Cell becomes a District of
    /// its own, which is a strange answer somebody will notice, and the alternative was a merge nobody
    /// would.
    /// </remarks>
    private static bool Reaches(int a, int b) =>
        a != RoadConnectivity.Unlabelled && a == b;

    /// <summary>
    /// The ordinals sorted by density descending, ties in Cell-index order.
    /// </summary>
    /// <remarks>
    /// <b>A counting sort, because the key is small and the tie-break has to be exact.</b> Density is
    /// a Building count, so the key range is bounded by the Buildings in one Cell; and the ordinals go
    /// in already in Cell-index order, so a stable pass over them keeps that as the tie-break without
    /// a comparator. No general sort is called: <c>05 §4</c>'s determinism requirement is not that the
    /// sort be stable in this run but that it be the same sort in every run of every build.
    /// </remarks>
    private static int[] ByDescendingDensity(Basins basins)
    {
        int tallest = 0;

        foreach (int height in basins.Heights)
        {
            if (height > tallest)
            {
                tallest = height;
            }
        }

        int[] counts = new int[tallest + 2];

        foreach (int height in basins.Heights)
        {
            counts[height]++;
        }

        // Running total from the top down, so that height `tallest` starts at offset zero.
        int[] start = new int[tallest + 2];
        int running = 0;

        for (int height = tallest; height >= 1; height--)
        {
            start[height] = running;
            running += counts[height];
        }

        int[] order = new int[basins.Count];
        int[] cursor = (int[])start.Clone();

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            order[cursor[basins.Heights[ordinal]]++] = ordinal;
        }

        return order;
    }

    /// <summary>
    /// Which maxima are real: the first flood, which merges everything and records prominence.
    /// </summary>
    private static bool[] Seed(Basins basins, int[] order, int percent)
    {
        UnionFind sets = new(basins.Count);

        int[] peak = new int[basins.Count];
        bool[] active = new bool[basins.Count];
        bool[] seeds = new bool[basins.Count];

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            peak[ordinal] = ordinal;
        }

        foreach (Level level in Levels(basins, order))
        {
            for (int k = level.From; k < level.To; k++)
            {
                active[order[k]] = true;
            }

            for (int k = level.From; k < level.To; k++)
            {
                int here = order[k];

                foreach (int there in basins.Neighbours(here))
                {
                    if (!active[there]
                        || !Reaches(basins.Components[here], basins.Components[there]))
                    {
                        continue;
                    }

                    int a = sets.Find(here);
                    int b = sets.Find(there);

                    if (a == b)
                    {
                        continue;
                    }

                    // The lower peak dies here; the higher one carries on. A tie keeps `a`, which is
                    // the newly activated Cell's set -- arbitrary, and fixed rather than arbitrary at
                    // run time, which is the only property determinism asks of it.
                    bool keepA = basins.Heights[peak[a]] >= basins.Heights[peak[b]];

                    int high = keepA ? a : b;
                    int low = keepA ? b : a;

                    int dying = basins.Heights[peak[low]];
                    int prominence = dying - level.Height;

                    // Relative: prominence as a percentage of the dying peak's own height, compared
                    // without a division so nothing rounds.
                    if (prominence * 100 >= dying * percent)
                    {
                        seeds[peak[low]] = true;
                    }

                    sets.Union(low, high);
                    peak[sets.Find(high)] = peak[high];
                }
            }
        }

        // Every set that survives the whole descent has a maximum nothing ever drowned. Its prominence
        // is its full height -- there is no saddle -- so it is a centre at any threshold, and this is
        // what guarantees the assignment below leaves no Cell ownerless.
        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            if (sets.Find(ordinal) == ordinal)
            {
                seeds[peak[ordinal]] = true;
            }
        }

        return seeds;
    }

    /// <summary>
    /// Which centre each Cell drains to, at what level, and how contested that answer is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The second flood, which refuses seed-to-seed merges</b> — that refusal is the watershed line
    /// and the only line there is.
    /// </para>
    /// <para>
    /// <b>It also produces the two numbers hysteresis needs, and they cost one array and one scalar.</b>
    /// A Cell's <em>win level</em> is the flood level at which its basin reached it. Its <em>rival
    /// level</em> is the level at which that basin first touched another owned basin, clamped to the win
    /// level — because reachability along Cells of at least a given density is symmetric, so once two
    /// basins have met at level <em>h</em>, <b>everything either of them gains BELOW <em>h</em> is
    /// reached by both at the same level.</b> ⚠ ***The watershed's answer for such a Cell is a scan
    /// order and not a finding***, which is precisely the tie <c>adr/0134</c> says a Cell must never
    /// change District on.
    /// </para>
    /// <para>
    /// <b>The consequence is the behaviour you would want and did not have to ask for</b>: a Cell deep
    /// inside a basin has a large margin and follows the field, and a Cell out at the boundary has a
    /// margin of zero and is held wherever it already was.
    /// </para>
    /// </remarks>
    private static Proposal Assign(Basins basins, int[] order, bool[] seeds)
    {
        UnionFind sets = new(basins.Count);
        Members members = new(basins.Count);

        int[] owner = new int[basins.Count];
        int[] touch = new int[basins.Count];
        int[] winLevel = new int[basins.Count];
        bool[] active = new bool[basins.Count];

        Array.Fill(owner, NoOrdinal);

        foreach (Level level in Levels(basins, order))
        {
            // Activation and seeding both precede every merge at this level, so that a seed is in its
            // set before the set can be joined to anything.
            for (int k = level.From; k < level.To; k++)
            {
                int here = order[k];

                active[here] = true;

                if (seeds[here])
                {
                    owner[here] = here;
                    winLevel[here] = level.Height;
                    members.Clear(here);
                }
            }

            for (int k = level.From; k < level.To; k++)
            {
                int here = order[k];

                foreach (int there in basins.Neighbours(here))
                {
                    if (!active[there]
                        || !Reaches(basins.Components[here], basins.Components[there]))
                    {
                        continue;
                    }

                    int a = sets.Find(here);
                    int b = sets.Find(there);

                    if (a == b)
                    {
                        continue;
                    }

                    // The watershed line: two basins that each already drain to a centre stay two
                    // basins for ever. The level it happens at is what hysteresis reads.
                    if (owner[a] != NoOrdinal && owner[b] != NoOrdinal)
                    {
                        Touch(touch, a, level.Height);
                        Touch(touch, b, level.Height);
                        continue;
                    }

                    // The OWNED side is kept as the root, unlike the first flood where the taller peak
                    // is. Two things ride on it: `touch` is per basin and would be lost if an owned
                    // root were reparented onto an unowned one, and the walk below has to know which
                    // list is the one that has just become owned.
                    bool ownedA = owner[a] != NoOrdinal;
                    int winner = ownedA ? a : b;
                    int loser = ownedA ? b : a;

                    members.Splice(winner, loser);
                    sets.Union(loser, winner);

                    if (owner[winner] != NoOrdinal)
                    {
                        // The loser's Cells become owned at this level, and only now. Walking them is
                        // O(1) amortised over the whole flood -- a Cell becomes owned exactly once,
                        // and the list is emptied behind the walk.
                        members.Stamp(winner, winLevel, level.Height);
                    }

                    owner[winner] = owner[winner] != NoOrdinal ? owner[winner] : owner[loser];
                    if (touch[loser] > touch[winner])
                    {
                        touch[winner] = touch[loser];
                    }
                }
            }
        }

        int[] centre = new int[basins.Count];
        int[] margin = new int[basins.Count];

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            int root = sets.Find(ordinal);

            centre[ordinal] = owner[root];

            int reached = touch[root];
            int rival = reached == 0 || reached > winLevel[ordinal] ? winLevel[ordinal] : reached;

            rival = reached == 0 ? 0 : rival;

            margin[ordinal] = winLevel[ordinal] - rival;
        }

        return new Proposal(centre, winLevel, margin);
    }

    /// <summary>Records the highest level at which a basin has met a rival.</summary>
    /// <remarks>
    /// <b>Highest, which is the first</b> — the descent visits levels in order, so an already-set value
    /// was set higher up and is the one that counts.
    /// </remarks>
    private static void Touch(int[] touch, int root, int height)
    {
        if (touch[root] == 0)
        {
            touch[root] = height;
        }
    }

    /// <summary>What one flood proposes: a centre per Cell, and how firmly it means it.</summary>
    private sealed record Proposal(int[] Centre, int[] WinLevel, int[] Margin);

    /// <summary>
    /// Brings the world's Districts to what the flood proposes, damped and with hysteresis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is one path and not two, and the first evaluation is the degenerate case of it.</b> A
    /// world with no Districts reconciles against nothing: every Cell is joining its first District, so
    /// no Cell is migrating, so neither the band nor the bound has anything to hold. ***A separate
    /// first-run path would be a second implementation of the same reconciliation, differing only in
    /// what it had not been told about***, and the day it drifted from this one nothing would say so.
    /// </para>
    /// <para>
    /// <b>Identity travels through the centre Cell.</b> A basin inherits whichever District currently
    /// owns the Cell its centre falls in, first claimant in Cell-index order; a basin whose centre is
    /// unowned or already claimed opens a new District. ⚠ **It reads one Cell rather than the whole
    /// extent**, so it is stable exactly while a centre moves inside its own old ground and no further
    /// — which is the property a Pool Bin needs, since task 5 hangs Bins off this row.
    /// </para>
    /// <para>
    /// 🔴 <b>A District no basin claims is destroyed, and at task 5 that becomes a real question.</b>
    /// Today it holds nothing, so nothing is lost. The moment it holds Pool Bins, destroying it
    /// destroys Goods and money, which <c>adr/0024</c> forbids outright — so the merge path is built
    /// here and the transfer is owed there, deliberately and in that order.
    /// </para>
    /// </remarks>
    private static void Reconcile(
        World world,
        DistrictTable districts,
        DistrictCellTable cells,
        DistrictResidency residency,
        Basins basins,
        Proposal proposal,
        DistrictRuleset rules)
    {
        Handle<District>[] opened = new Handle<District>[basins.Count];
        bool[] isOpen = new bool[basins.Count];

        // Snapshotted BEFORE anything is created, because Create may hand back a slot a previous
        // evaluation freed -- and a recycled slot indexed against a list taken afterwards would read as
        // a District nobody claimed and be destroyed the moment it was opened.
        int standing = districts.Rows.SlotCount;
        bool[] wasLive = new bool[standing];
        bool[] claimed = new bool[standing];

        for (int slot = 0; slot < standing; slot++)
        {
            wasLive[slot] = districts.Rows.IsLive(slot);
        }

        // Pass 1: identity. Ascending, which is Cell-index order, so which of two basins claims a
        // contested incumbent is a property of the map rather than of the flood.
        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            int seat = proposal.Centre[ordinal];

            if (seat != ordinal || isOpen[seat])
            {
                continue;
            }

            Handle<District> incumbent =
                residency.Of(cells, basins.East(seat), basins.North(seat));

            bool inherits = districts.Rows.TryResolve(incumbent, out int held)
                && held < standing
                && !claimed[held];

            if (inherits)
            {
                int held2 = districts.Rows.Resolve(incumbent);

                claimed[held2] = true;

                districts.CentreEast[held2] = basins.East(seat);
                districts.CentreNorth[held2] = basins.North(seat);

                opened[seat] = incumbent;
            }
            else
            {
                opened[seat] = districts.Create(basins.East(seat), basins.North(seat));
            }

            isOpen[seat] = true;
        }

        // Pass 2: which Districts are dying. A Cell in one of them moves for free, because the
        // alternative is membership of a row that will not exist.
        bool[] dying = new bool[standing];

        for (int slot = 0; slot < standing; slot++)
        {
            dying[slot] = wasLive[slot] && !claimed[slot];
        }

        // Pass 3: the Cells. Three of the four outcomes are unconditional; only a Cell moving from one
        // living District to another consults the band and the bound.
        List<int> migrants = [];

        for (int ordinal = 0; ordinal < basins.Count; ordinal++)
        {
            int seat = proposal.Centre[ordinal];

            if (seat == NoOrdinal)
            {
                continue;
            }

            Cells east = basins.East(ordinal);
            Cells north = basins.North(ordinal);

            int slot = residency.Slot(east, north);
            Handle<District> want = opened[seat];

            if (slot == DistrictResidency.NotResident)
            {
                File(cells, residency, east, north, want);
                continue;
            }

            Handle<District> have = cells.District[slot];

            if (have == want)
            {
                continue;
            }

            if (!districts.Rows.TryResolve(have, out int incumbentSlot)
                || incumbentSlot >= standing
                || dying[incumbentSlot])
            {
                cells.District[slot] = want;
                continue;
            }

            migrants.Add(ordinal);
        }

        Migrate(cells, residency, basins, proposal, opened, migrants, rules);

        // Pass 4: Cells that are no longer built leave every District, and then a District nothing
        // claims goes. In that order, because a Cell row holds a handle to the row it names.
        Evict(world, cells, residency, basins);

        // Over `standing` rather than over SlotCount, which has grown: a District opened above is by
        // construction one somebody claimed, and indexing `dying` past its own length is what walking
        // the table would do.
        for (int slot = standing - 1; slot >= 0; slot--)
        {
            if (!dying[slot] || !districts.Rows.IsLive(slot))
            {
                continue;
            }

            // Succession, and it is the same Cell that decided identity in pass 1 -- a District IS its
            // centre (adr/0134), so the row that inherited the centre is the row that inherited the
            // District. Read AFTER the Cell passes, because the whole question is who owns that ground
            // now. It answers the unset handle when the centre is no longer in any District, which is
            // an heirless death and is RetirePool's to judge rather than this loop's to prevent.
            Handle<District> heir = residency.Of(
                cells, districts.CentreEast[slot], districts.CentreNorth[slot]);

            world.RetirePool(slot, heir);

            districts.Rows.Free(districts.Rows.At(slot));
        }
    }

    /// <summary>
    /// Moves as many contested Cells as the band admits and the bound allows, most decisive first.
    /// </summary>
    /// <remarks>
    /// <b>Most decisive first, ties in Cell-index order, and the order is the decision.</b> Taking them
    /// in Cell-index order would make a boundary migrate from the south-west corner outwards, which is
    /// a fact about the array and not about the city; taking them by margin makes it move where the
    /// field is most sure, which is what <em>migrates rather than jumps</em> is supposed to look like.
    /// The comparison is a total order, so no sort's stability is being relied on.
    /// </remarks>
    private static void Migrate(
        DistrictCellTable cells,
        DistrictResidency residency,
        Basins basins,
        Proposal proposal,
        Handle<District>[] opened,
        List<int> migrants,
        DistrictRuleset rules)
    {
        migrants.Sort((a, b) =>
        {
            int byMargin = proposal.Margin[b].CompareTo(proposal.Margin[a]);

            return byMargin != 0 ? byMargin : a.CompareTo(b);
        });

        int moved = 0;

        foreach (int ordinal in migrants)
        {
            if (moved >= rules.MigrateCells)
            {
                break;
            }

            // adr/0134's band, and the comparison is done without a division so nothing rounds. The
            // scale is the Cell's own win level rather than the peak's, because what is being asked is
            // how decisive the field is HERE.
            if (proposal.Margin[ordinal] * 100 < proposal.WinLevel[ordinal] * rules.HysteresisPercent)
            {
                continue;
            }

            int slot = residency.Slot(basins.East(ordinal), basins.North(ordinal));

            cells.District[slot] = opened[proposal.Centre[ordinal]];
            moved++;
        }
    }

    /// <summary>Frees the membership row of every Cell that no longer holds a Building.</summary>
    private static void Evict(
        World world, DistrictCellTable cells, DistrictResidency residency, Basins basins)
    {
        for (int slot = cells.Rows.SlotCount - 1; slot >= 0; slot--)
        {
            if (!cells.Rows.IsLive(slot) || basins.Holds(cells.East[slot], cells.North[slot]))
            {
                continue;
            }

            cells.Rows.Free(cells.Rows.At(slot));
        }

        residency.Rebuild(cells);

        // The post-condition, asked HERE because here is where it is true. adr/0134 makes the extent
        // built Cells only, and that is a statement about what an evaluation PRODUCES rather than about
        // the world -- a Building comes down between evaluations and the row it leaves behind is the
        // cadence working, not a defect. What it guards is the loop above: it is the only thing between
        // a demolished Cell and a membership row that outlives every Building on it for ever, and a
        // reconciliation reordered three mechanisms later would drop it in silence.
        WorldInvariants.DistrictExtentIsBuiltGround(world, world.Invariants);
    }

    /// <summary>Files a Cell under a District for the first time.</summary>
    private static void File(
        DistrictCellTable cells,
        DistrictResidency residency,
        Cells east,
        Cells north,
        Handle<District> district)
    {
        Handle<DistrictCell> row = cells.Create(east, north, district);

        residency.Add(east, north, cells.Rows.Resolve(row));
    }

    /// <summary>
    /// The Cells of each set, as an intrusive list, so that a set becoming owned can stamp them.
    /// </summary>
    /// <remarks>
    /// <b>An intrusive index list over flat arrays</b>, which is what <c>05 §4</c> requires of every
    /// variable-length collection in this project — and here it also happens to be the only shape that
    /// makes the stamp affordable, since a Cell becomes owned exactly once and the list is emptied
    /// behind the walk.
    /// </remarks>
    private sealed class Members
    {
        private readonly int[] _head;
        private readonly int[] _tail;
        private readonly int[] _next;

        public Members(int count)
        {
            _head = new int[count];
            _tail = new int[count];
            _next = new int[count];

            for (int i = 0; i < count; i++)
            {
                _head[i] = i;
                _tail[i] = i;
                _next[i] = NoOrdinal;
            }
        }

        /// <summary>Empties a set's list, because its Cells are already stamped.</summary>
        public void Clear(int root) => _head[root] = NoOrdinal;

        /// <summary>Hangs one set's list off another's, in <c>O(1)</c>.</summary>
        public void Splice(int winner, int loser)
        {
            if (_head[loser] == NoOrdinal)
            {
                return;
            }

            if (_head[winner] == NoOrdinal)
            {
                _head[winner] = _head[loser];
                _tail[winner] = _tail[loser];
            }
            else
            {
                _next[_tail[winner]] = _head[loser];
                _tail[winner] = _tail[loser];
            }

            _head[loser] = NoOrdinal;
        }

        /// <summary>Stamps every Cell in a set with the level it became owned at, then empties it.</summary>
        public void Stamp(int root, int[] winLevel, int height)
        {
            for (int ordinal = _head[root]; ordinal != NoOrdinal; ordinal = _next[ordinal])
            {
                winLevel[ordinal] = height;
            }

            _head[root] = NoOrdinal;
        }
    }

    /// <summary>The runs of equal density in <paramref name="order"/>, tallest first.</summary>
    private static IEnumerable<Level> Levels(Basins basins, int[] order)
    {
        int from = 0;

        while (from < order.Length)
        {
            int height = basins.Heights[order[from]];
            int to = from;

            while (to < order.Length && basins.Heights[order[to]] == height)
            {
                to++;
            }

            yield return new Level(height, from, to);

            from = to;
        }
    }

    /// <summary>One flood step: every Cell at one density.</summary>
    private readonly record struct Level(int Height, int From, int To);

    /// <summary>
    /// The built Cells as a compact array, and the map back from a Cell to its place in it.
    /// </summary>
    private sealed class Basins(int[] cellIndex, int[] heights, int[] components, int[] ordinalOf)
    {
        /// <summary>How many Cells hold at least one Building.</summary>
        public int Count => cellIndex.Length;

        /// <summary>Each Cell's Building count, by ordinal.</summary>
        public int[] Heights => heights;

        /// <summary>Each Cell's road component, by ordinal.</summary>
        public int[] Components => components;

        /// <summary>A Cell's east coordinate, by ordinal.</summary>
        public Cells East(int ordinal) => new(cellIndex[ordinal] % CellGrid.WorldCells);

        /// <summary>A Cell's north coordinate, by ordinal.</summary>
        public Cells North(int ordinal) =>
            new(IntegerMath.FloorDiv(cellIndex[ordinal], CellGrid.WorldCells));

        /// <summary>
        /// The built Cells sharing an edge with this one — four-connected, never eight.
        /// </summary>
        /// <remarks>
        /// <b>Four-connected because eight-connected lets two basins touch at a corner</b>, and a
        /// corner is not a route: two Buildings diagonally across a Cell boundary are 128 m apart on
        /// the map and however far apart the Street lattice makes them. The whole point of the clip
        /// below is that adjacency in this operator has to mean something a Citizen could walk.
        /// </remarks>
        public IEnumerable<int> Neighbours(int ordinal)
        {
            int index = cellIndex[ordinal];
            int east = index % CellGrid.WorldCells;
            int north = IntegerMath.FloorDiv(index, CellGrid.WorldCells);

            if (east > 0 && Built(index - 1))
            {
                yield return Ordinal(index - 1);
            }

            if (east < CellGrid.WorldCells - 1 && Built(index + 1))
            {
                yield return Ordinal(index + 1);
            }

            if (north > 0 && Built(index - CellGrid.WorldCells))
            {
                yield return Ordinal(index - CellGrid.WorldCells);
            }

            if (north < CellGrid.WorldCells - 1 && Built(index + CellGrid.WorldCells))
            {
                yield return Ordinal(index + CellGrid.WorldCells);
            }
        }

        /// <summary>
        /// Whether a Cell holds a Building. <b>An unbuilt neighbour is not a neighbour</b> — it has no
        /// ordinal, so every array in the flood would be indexed at minus one.
        /// </summary>
        private bool Built(int index) => ordinalOf[index] != 0;

        /// <summary>Whether a Cell holds a Building, by coordinate. What eviction asks.</summary>
        public bool Holds(Cells east, Cells north) =>
            CellGrid.Contains(east, north) && ordinalOf[CellGrid.Index(east, north)] != 0;

        private int Ordinal(int index) => ordinalOf[index] - 1;
    }

    /// <summary>
    /// Union-find over the ordinals, with union by size and path halving.
    /// </summary>
    /// <remarks>
    /// <b>Local to this file rather than shared with <see cref="RoadConnectivity"/></b>, which has its
    /// own. The two are answering different questions — that one labels a graph's components, this one
    /// merges basins during a descent and has to be told which side survives — so the shared thing
    /// would be four lines of <c>Find</c> and a disagreement about the rest.
    /// </remarks>
    private sealed class UnionFind
    {
        private readonly int[] _parent;
        private readonly int[] _size;

        public UnionFind(int count)
        {
            _parent = new int[count];
            _size = new int[count];

            for (int i = 0; i < count; i++)
            {
                _parent[i] = i;
                _size[i] = 1;
            }
        }

        public int Find(int node)
        {
            while (_parent[node] != node)
            {
                _parent[node] = _parent[_parent[node]];
                node = _parent[node];
            }

            return node;
        }

        /// <summary>
        /// Joins two sets. ⚠ <b>Not by size</b> — the caller has already decided which root survives,
        /// and a rank heuristic that overrode it would silently discard the surviving peak.
        /// </summary>
        public void Union(int loser, int winner)
        {
            int a = Find(loser);
            int b = Find(winner);

            if (a == b)
            {
                return;
            }

            _parent[a] = b;
            _size[b] += _size[a];
        }
    }
}
