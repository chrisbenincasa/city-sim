using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>What a Tick's Disaster sweep did, for a readout to say out loud.</summary>
/// <remarks>
/// ⚠ <b>Every field is a per-Tick delta and none of them is a total.</b> The totals live on
/// <see cref="DisasterTable.Ruined"/> and <see cref="DisasterTable.Swept"/>, which are saved
/// columns; this is what changed on the Tick a caller is holding.
/// </remarks>
/// <param name="Live">How many Disasters are in progress after the sweep.</param>
/// <param name="Began">How many began on this Tick.</param>
/// <param name="Ended">How many finished receding on this Tick.</param>
/// <param name="Cells">How many Cells are under water after the sweep.</param>
/// <param name="Flooded">How many Cells the water reached on this Tick.</param>
/// <param name="Drained">How many Cells the water left on this Tick.</param>
/// <param name="Ruined">How many Buildings were ruined on this Tick.</param>
/// <param name="Swept">How many Buildings were destroyed outright on this Tick.</param>
public readonly record struct DisasterReading(
    int Live,
    int Began,
    int Ended,
    int Cells,
    int Flooded,
    int Drained,
    int Ruined,
    int Swept);

/// <summary>
/// Schedules Disasters over the Hazard Region and spreads the ones in progress.
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Disaster, <c>01 §5.2</c>, <c>01 §5.3</c>, <c>plans/0045</c> row 12. <b>The
/// first thing in the build that reads <see cref="World.Flood"/></b>, which had stood since
/// milestone 24 as a table nothing fired on.
/// </para>
/// <para>
/// <b>WORLD-SCHEDULED, and the phrase is load-bearing.</b> <see cref="Begin"/> asks the clock and
/// the seed where a flood goes and never asks the city. <c>01 §5.3</c>: a disaster that fired only
/// where there was something to lose would make riverside land cheap-<em>until-you-use-it</em>, and
/// the hazard overlay would be describing a trap rather than a price. ***A player who sites
/// carefully genuinely never sees a flood do anything, and that is the correct reward.***
/// </para>
/// <para>
/// <b>THE SURGE IS A THRESHOLD ON DEPTH AND THAT IS THE WHOLE MODEL.</b> A Hazard Region row holds
/// <em>the flood level minus its ground</em>, so a large depth is low ground. Let <c>L</c> be the
/// water surface expressed on that same scale: a Cell is under water when its depth is at least
/// <c>L</c>. <c>L</c> falls from the seed Cell's own depth to zero over
/// <c>[disasters] flood_rises_over_days</c> and climbs back over
/// <c>flood_recedes_over_days</c>. ⚠ <b>So the flood opens with everything connected to the seed and
/// LOWER already under water, and rises from there</b> — which is what water does, and is why there
/// is no separate notion of a starting radius.
/// </para>
/// <para>
/// <b>Connectivity is what bounds it, and it is the reason the footprint is stored.</b> The wet set
/// is the 4-connected component of <c>{depth ≥ L}</c> containing the seed, grown outward one ring at
/// a time. Two floodplains that never touch are two different floods; a flood in one bay does not
/// appear in the next.
/// </para>
/// <para>
/// ⚠ <b>Both verbs fire and the depth chooses between them.</b> Ground <em>below</em> the flood's
/// origin is <see cref="World.DestroyBuilding"/>d — the Lot vacates and ordinary redevelopment
/// reoccupies it, which is <c>01 §5.2</c>'s own sentence. Ground at or above it is
/// <see cref="World.AbandonBuilding"/>ed — the shell stands as a ruin, on <c>adr/0091</c>'s
/// grounds. ***Neither is a new mechanism and no number was chosen: it is one depth compared against
/// another.***
/// </para>
/// <para>
/// <b>The wet index is scratch and never survives a Tick</b>, which is
/// <see cref="TrafficPresence"/>'s disposition exactly. It is stamped afresh from the saved rows at
/// the top of every sweep that has anything to do, so there is no cross-Tick cache for a load to
/// disagree with and <c>CLAUDE.md</c>'s <em>a structure that lives outside the world is not derived
/// state</em> does not bite. 🔴 <b>The cost is <c>O(footprint)</c> a Tick while a flood is
/// live</b> and is unmeasured at a million Citizens. <b><c>plans/0013</c> carries a row for it as of
/// 2026-08-31</b> (<c>adr/0073</c>), and that row's point is the one this paragraph cannot make on its
/// own: <em>the footprint is ground, so this is the only consumer whose multiplicand does not fall when
/// the population does.</em>
/// </para>
/// </remarks>
public sealed class DisasterEngine
{
    private readonly World _world;
    private readonly WorldKey _key;

    /// <summary>
    /// Which Cells are under water, stamped with <see cref="_epoch"/> so no pass has to clear it.
    /// </summary>
    /// <remarks>
    /// <b>An epoch stamp rather than a boolean array</b>, because clearing 262,144 entries on a Tick
    /// where nothing moved is a megabyte of memset for an answer nobody asked for. A stale entry
    /// carries an older epoch and reads as dry.
    /// </remarks>
    private readonly int[] _wet = new int[CellGrid.WorldCellCount];

    /// <summary>The frontier, as Cell indices. Grown rather than reallocated.</summary>
    private int[] _frontier = new int[1024];

    /// <summary>Cells the water reached on THIS Tick, as indices, in the order it reached them.</summary>
    /// <remarks>
    /// <b>A list and not a stamped grid, because the consumer walks it rather than probing it.</b>
    /// <see cref="Strike"/> asks <em>what is standing on each Cell the water just took</em>, which
    /// <see cref="BuildingResidency"/> answers per Cell — so the pass is proportional to the ring
    /// the flood moved and not to the city's Lots.
    /// </remarks>
    private int[] _struck = new int[1024];

    /// <summary>The Disaster slot that reached each entry of <see cref="_struck"/>.</summary>
    private int[] _struckBy = new int[1024];

    private int _epoch;
    private int _reached;

    /// <param name="world">The world whose ground floods.</param>
    /// <param name="key">The world seed, which with the Tick is the whole of the schedule.</param>
    public DisasterEngine(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _key = key;
    }

    /// <summary>Begins whatever is due and advances whatever is in progress.</summary>
    /// <remarks>
    /// <b>Silent on every shipped Ruleset but <c>flooded.toml</c></b> — no other declares
    /// <c>[disasters]</c>, so this returns before it looks at a row. It is silent on <c>coastal.toml</c> too, which has the
    /// Hazard Region and no schedule over it: ⚠ <b>a world with a floodplain and no floods is a
    /// world</b>, and the two keys are deliberately separable.
    /// </remarks>
    public DisasterReading Sweep(Ticks tick)
    {
        DisasterRuleset rules = _world.Rules.Disasters;

        if (!rules.Stated)
        {
            return default;
        }

        int began = Begin(rules, tick);

        if (_world.Disasters.Rows.LiveCount == 0)
        {
            return new DisasterReading(0, began, 0, 0, 0, 0, 0, 0);
        }

        Stamp();

        int flooded = 0;
        int drained = 0;
        int ruined = 0;
        int swept = 0;
        int ended = 0;

        _reached = 0;

        for (int slot = 0; slot < _world.Disasters.Rows.SlotCount; slot++)
        {
            if (!_world.Disasters.Rows.IsLive(slot))
            {
                continue;
            }

            (int grew, int shrank, bool over) = Advance(rules, slot, tick);

            flooded += grew;
            drained += shrank;

            if (over)
            {
                ended++;
            }
        }

        if (_reached > 0)
        {
            (ruined, swept) = Strike(tick);
        }

        return new DisasterReading(
            _world.Disasters.Rows.LiveCount,
            began,
            ended,
            _world.Inundations.Rows.LiveCount,
            flooded,
            drained,
            ruined,
            swept);
    }

    /// <summary>
    /// Starts a flood if the interval has come round — <b>from the clock and the seed, and from
    /// nothing else.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The place is a uniform draw over the Hazard Region's rows</b>, which is the only
    /// distribution available that owes nothing to the city. ⚠ <b>It is not uniform over
    /// <em>ground</em>, and the difference is real</b>: the rows are a band above the waterline, so
    /// a world with a long shallow coast has more of them there and is likelier to be seeded there.
    /// That is a property of the terrain and not a thumb on the scale.
    /// </para>
    /// <para>
    /// ⚠ <b>Tick zero never floods.</b> The modulus would fire on the Tick the world opens, before
    /// anything is standing, which is a flood over an empty map that nobody could have sited against.
    /// </para>
    /// </remarks>
    private int Begin(in DisasterRuleset rules, Ticks tick)
    {
        int interval = rules.FloodEveryTicks;

        if (interval <= 0 || tick.Raw == 0 || tick.Raw % (ulong)interval != 0)
        {
            return 0;
        }

        FloodCellTable hazard = _world.Flood;
        int candidates = hazard.Rows.LiveCount;

        if (candidates == 0)
        {
            // A Ruleset stating [disasters] over a world with no floodplain. The loader refuses that
            // pairing, so what reaches here is a world built IN CODE -- and answering "nothing
            // happens" is the only defined thing to do with a draw over an empty set.
            return 0;
        }

        ulong draw = Randomness.Draw(_key, 0, tick, PurposeTag.DisasterSeed);
        int wanted = (int)(draw % (ulong)candidates);
        int seed = Rows.NoSlot;

        // Walking to the Nth LIVE row rather than indexing the Nth slot. The Hazard Region is written
        // once by the generator and nothing ever frees a row, so today the two are the same walk --
        // and a table that acquired a free list would silently start drawing dead Cells.
        for (int slot = 0; slot < hazard.Rows.SlotCount; slot++)
        {
            if (hazard.Rows.IsLive(slot) && wanted-- == 0)
            {
                seed = slot;
                break;
            }
        }

        if (seed == Rows.NoSlot)
        {
            return 0;
        }

        Handle<Disaster> flood = _world.Disasters.Create(
            DisasterTable.Flood,
            hazard.East[seed],
            hazard.North[seed],
            hazard.Depth[seed],
            tick);

        _world.Inundations.Create(
            hazard.East[seed], hazard.North[seed], hazard.Depth[seed], tick, flood);

        return 1;
    }

    /// <summary>Stamps the wet index from the saved rows. Scratch, and rebuilt every sweep.</summary>
    private void Stamp()
    {
        InundationTable wet = _world.Inundations;

        _epoch++;

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            if (wet.Rows.IsLive(slot))
            {
                _wet[CellGrid.Index(wet.East[slot], wet.North[slot])] = _epoch;
            }
        }
    }

    /// <summary>Moves one Disaster's water — out over new ground, or back off the old.</summary>
    private (int Grew, int Shrank, bool Over) Advance(
        in DisasterRuleset rules, int slot, Ticks tick)
    {
        DisasterTable floods = _world.Disasters;

        if (!tick.TrySubtract(floods.Began[slot], out Ticks age))
        {
            return (0, 0, false);
        }

        int seedDepth = floods.SeedDepth[slot];
        long elapsed = (long)age.Raw;
        long rises = rules.FloodRisesOverTicks;
        long recedes = rules.FloodRecedesOverTicks;
        Handle<Disaster> cause = floods.Rows.At(slot);

        if (elapsed > rises + recedes)
        {
            // OVER. Everything this flood still holds drains at once, and the row goes with it.
            //
            // 🔴 UNCONDITIONAL, AND `seedDepth + 1` STOOD HERE AND LEAKED. Withdraw frees the Cells
            // BELOW the surge, and ground deeper than the seed is never below it -- the surge climbs
            // only back to where it started. So the deep half of every flood stayed wet for ever:
            // measured at 5,140 Cells standing after three floods had ended and a fourth had reached
            // 291. ⚠ THE MECHANISM WAS RIGHT AND THE SENTENCE ABOUT IT WAS BACKWARDS -- the deepest
            // ground IS the last to dry, which is why the recession cannot be what takes it, and the
            // comment that stood here had reasoned its way to the opposite conclusion from the same
            // fact. adr/0006, caught by the dump's own last line rather than by a test.
            int left = Withdraw(cause, int.MaxValue);

            floods.Rows.Free(cause);

            return (0, left, true);
        }

        // THE SURGE, and both halves are the same line read in two directions. Rising, the threshold
        // walks from the seed's own depth down to zero, so the water surface climbs from the seed's
        // ground to the flood level. Receding, it walks back. Integer division throughout: a
        // threshold is a depth and depths are the height field's own units (adr/0157).
        int surge = elapsed <= rises
            ? seedDepth - (int)IntegerMath.FloorDiv(seedDepth * elapsed, rises)
            : (int)IntegerMath.FloorDiv(seedDepth * (elapsed - rises), recedes);

        return elapsed <= rises
            ? (Spread(slot, cause, surge, tick), 0, false)
            : (0, Withdraw(cause, surge), false);
    }

    /// <summary>
    /// Grows one flood's footprint out to the current surge — <b>a flood fill, bounded by the
    /// Hazard Region and by connectivity.</b>
    /// </summary>
    /// <remarks>
    /// <b>The first ring is every Cell the flood already holds</b>, because the threshold has moved
    /// since the last sweep and any of them may now have an eligible neighbour. After that it is
    /// only the Cells added by the previous ring, which is what keeps a Tick's work proportional to
    /// what actually moved rather than to the whole footprint. ⚠ <b>E, N, W, S order, and it is
    /// hash-bearing</b> — the rows are allocated in the order the Cells are met, which is
    /// <see cref="WaterGenerator"/>'s rule one table along.
    /// </remarks>
    private int Spread(int flood, Handle<Disaster> cause, int surge, Ticks tick)
    {
        InundationTable wet = _world.Inundations;
        int ring = 0;

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            if (wet.Rows.IsLive(slot) && wet.Cause[slot] == cause)
            {
                Push(ref _frontier, ref ring, CellGrid.Index(wet.East[slot], wet.North[slot]));
            }
        }

        int grew = 0;

        while (ring > 0)
        {
            int at = _frontier[--ring];
            var east = new Cells(at % CellGrid.WorldCells);
            var north = new Cells(IntegerMath.FloorDiv(at, CellGrid.WorldCells));

            grew += Reach(flood, cause, surge, tick, new Cells(east.Raw + 1), north, ref ring);
            grew += Reach(flood, cause, surge, tick, east, new Cells(north.Raw + 1), ref ring);
            grew += Reach(flood, cause, surge, tick, new Cells(east.Raw - 1), north, ref ring);
            grew += Reach(flood, cause, surge, tick, east, new Cells(north.Raw - 1), ref ring);
        }

        return grew;
    }

    /// <summary>Floods one neighbour if it is floodplain, low enough, and not already wet.</summary>
    private int Reach(
        int flood, Handle<Disaster> cause, int surge, Ticks tick, Cells east, Cells north,
        ref int ring)
    {
        int depth = _world.FloodInCells.DepthAt(_world.Flood, east, north);

        // Zero is NOT floodplain rather than a shallow one -- FloodCellTable.Create refuses a
        // non-positive depth precisely so this test needs no second question. Off-map answers zero
        // too, so a flood reaching the world's edge stops there without a bounds check.
        if (depth == 0 || depth < surge)
        {
            return 0;
        }

        int at = CellGrid.Index(east, north);

        if (_wet[at] == _epoch)
        {
            return 0;
        }

        _wet[at] = _epoch;
        _world.Inundations.Create(east, north, depth, tick, cause);
        Push(ref _frontier, ref ring, at);

        int struck = _reached;

        Push(ref _struck, ref struck, at);
        Push(ref _struckBy, ref _reached, flood);

        return 1;
    }

    /// <summary>Drains every Cell one flood holds that has come back above the surge.</summary>
    /// <remarks>
    /// <b>Shallow ground dries first</b>, which is the rise run backwards and needs no separate
    /// ordering: a Cell leaves when its depth falls below the threshold, and the threshold climbs.
    /// ⚠ <b>Nothing is rebuilt or un-ruined when the water goes.</b> A Building the flood took is
    /// gone, and what puts something back on the Lot is the ordinary Zone Rule — <c>01 §5.2</c>'s
    /// <em>"recovery time is the Bill axis reading the disaster back to the player"</em>.
    /// </remarks>
    private int Withdraw(Handle<Disaster> cause, int surge)
    {
        InundationTable wet = _world.Inundations;
        int drained = 0;

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            if (!wet.Rows.IsLive(slot) || wet.Cause[slot] != cause || wet.Depth[slot] >= surge)
            {
                continue;
            }

            _wet[CellGrid.Index(wet.East[slot], wet.North[slot])] = 0;
            wet.Rows.Free(wet.Rows.At(slot));
            drained++;
        }

        return drained;
    }

    /// <summary>
    /// Ruins or destroys everything standing on ground the water reached this Tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Per Cell the water took, and not per Lot in the city.</b>
    /// <see cref="BuildingResidency"/> already answers <em>what stands on this Cell</em> in
    /// <c>O(1)</c> — it is what <c>adr/0069</c>'s placement and <c>adr/0134</c>'s watershed both
    /// read — so a flood's damage costs the ring it moved rather than a whole-city walk. ⚠ <b>The
    /// first spelling walked every Lot</b>, which is the same answer at the price of the city's
    /// size on a Tick where the water moved four Cells.
    /// </para>
    /// <para>
    /// ⚠ <b>The Building's Cell is its LOT's Cell, and the shell's setback is not consulted.</b> A
    /// Lot is an address point on a Segment and has no depth (<c>adr/0078</c>), so where a Building
    /// stands is a question only the Lot can answer. The renderer invents a thickness the city does
    /// not have, and that invention must not become a fact a flood reads.
    /// </para>
    /// </remarks>
    private (int Ruined, int Swept) Strike(Ticks tick)
    {
        BuildingTable buildings = _world.Buildings;
        DisasterTable floods = _world.Disasters;
        // ⚠ GENEROUS ON PURPOSE, BECAUSE `In` TRUNCATES IN SILENCE. It fills the span and returns,
        // so a Cell holding more Buildings than this span has room for would have the remainder
        // survive a flood invisibly -- and how many a Cell holds is Ruleset data rather than a
        // constant: a Cell is 32 Tiles, `[roads] block_tiles` is 32 in every shipped file and a block
        // carries four Segments of `[lots] lots_per_segment`, so ~20 today and ~320 at a block of 8.
        Span<int> here = stackalloc int[512];
        int ruined = 0;
        int swept = 0;

        for (int entry = 0; entry < _reached; entry++)
        {
            var east = new Cells(_struck[entry] % CellGrid.WorldCells);
            var north = new Cells(IntegerMath.FloorDiv(_struck[entry], CellGrid.WorldCells));
            int flood = _struckBy[entry];

            if (!floods.Rows.IsLive(flood))
            {
                // ⚠ DEFENSIVE, AND IT CANNOT FIRE TODAY -- said plainly rather than left to read as
                // a case somebody met. Advance either spreads a flood or ends it and never both on
                // one Tick, so a flood that put a Cell in this list is still live when Strike walks
                // it; and Begin runs before the loop, so no freed slot is recycled underneath us
                // either. What it guards is the ordering rather than a bug: a future pass that ended
                // a flood after spreading it would attribute damage to a freed row's counter, and
                // the row would read as whatever the next Disaster to take that slot happened to be.
                continue;
            }

            int found = _world.BuildingsInCells.In(CellRect.At(east, north), buildings, here);
            int depth = _world.FloodInCells.DepthAt(_world.Flood, east, north);

            for (int at = 0; at < found; at++)
            {
                int building = here[at];

                if (!buildings.Rows.IsLive(building))
                {
                    continue;
                }

                // THE FORK, AND IT IS ONE DEPTH AGAINST ANOTHER. Ground lower than the flood's own
                // origin sits under the deepest water this flood will ever hold and is swept away;
                // ground at or above the origin is ruined and the shell stands. Both are existing
                // verbs and neither number was chosen -- 01 §5.2's "no severity constant is authored
                // anywhere", kept.
                if (depth > floods.SeedDepth[flood])
                {
                    _world.DestroyBuilding(buildings.Rows.At(building), tick);
                    floods.Swept[flood]++;
                    swept++;
                }
                else if (!buildings.IsAbandoned(building))
                {
                    // An already-abandoned shell is skipped rather than re-abandoned:
                    // AbandonBuilding stamps AbandonedSince, and restamping it would reset the
                    // collapse clock every time the tide came in -- a ruin made immortal by the
                    // thing that ruined it.
                    _world.AbandonBuilding(buildings.Rows.At(building), tick);
                    floods.Ruined[flood]++;
                    ruined++;
                }
            }
        }

        return (ruined, swept);
    }

    /// <summary>Pushes a Cell onto a stack, growing it if it is full.</summary>
    private static void Push(ref int[] stack, ref int height, int at)
    {
        if (height == stack.Length)
        {
            Array.Resize(ref stack, stack.Length * 2);
        }

        stack[height++] = at;
    }
}
