namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// Tick phase 6, ahead of placement: the Outside deciding, on its own, that somebody wants to come.
/// </summary>
/// <remarks>
/// Phase 6 order: recovery, free-stock snapshot, expiry, connection loss, review, admission,
/// then fresh occasions. Cancelled reservations are available to fresh occasions next Tick.
/// No work runs without an immigration Ruleset.
/// </remarks>
public sealed class HinterlandEngine
{
    private readonly World _world;
    private readonly WorldKey _key;
    private readonly PlacementEngine _placement;

    /// <summary>
    /// How many Households each group may be asked for this pass, captured after recovery.
    /// </summary>
    /// <remarks>
    /// Captured after recovery. Same-Tick cancellations must not earn fresh reconsideration credit.
    /// </remarks>
    private int[] _allowance = [];

    /// <param name="world">The city and the Outside behind it.</param>
    /// <param name="key">The world seed.</param>
    /// <param name="placement">Whose choice model decides whether a family wants to come.</param>
    public HinterlandEngine(World world, WorldKey key, PlacementEngine placement)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(placement);

        _world = world;
        _key = key;
        _placement = placement;
    }

    /// <summary>Runs one Tick of the Outside, edge by edge.</summary>
    public void Sweep(Ticks now)
    {
        ImmigrationRuleset immigration = _world.Rules.Immigration;

        if (!immigration.Stated)
        {
            return;
        }

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            MapEdge which = HinterlandTable.EdgeAt(edge);

            Recover(edge, immigration);
            Capture(edge);
            Expire(edge, which, immigration, now);
            DropDisconnected(edge, which);
            Review(edge, which, immigration, now);
            AdmitWaiting(edge, which, now);
            Present(edge, which, immigration, now);
            _world.RetireEmptyHinterlandGroups(which);
        }
    }

    /// <summary>Moves each group towards its resting count, in whichever direction it is off it.</summary>
    private void Recover(int edge, in ImmigrationRuleset immigration)
    {
        HinterlandPopulationTable groups = _world.HinterlandPopulation;
        HinterlandTable edges = _world.Hinterlands;

        long span = immigration.RecoveryTicks;

        foreach (int slot in groups.Groups(edges).Walk(edge))
        {
            long off = (long)groups.Target[slot] - groups.Stock[slot];

            if (!immigration.Recovers || off == 0)
            {
                groups.RecoveryNumerator[slot] = 0;
                groups.RecoveryDirection[slot] = 0;
                continue;
            }

            sbyte direction = off > 0 ? (sbyte)1 : (sbyte)-1;

            // A group that crosses its resting count starts again from zero. A fraction of a
            // Household on its way in must not be spent as a fraction of one on its way out.
            if (groups.RecoveryDirection[slot] != direction)
            {
                groups.RecoveryNumerator[slot] = 0;
                groups.RecoveryDirection[slot] = direction;
            }

            long numerator = checked(groups.RecoveryNumerator[slot] + (off > 0 ? off : -off));
            long whole = IntegerMath.FloorDiv(numerator, span);

            groups.RecoveryNumerator[slot] = numerator - checked(whole * span);

            if (whole == 0)
            {
                continue;
            }

            int members = groups.Members(slot);

            if (direction > 0)
            {
                int added = (int)(whole < off ? whole : off);

                groups.Stock[slot] += added;
                groups.Replenished[slot] += added;

                edges.ReplenishedHouseholds[edge] += added;
                edges.ReplenishedPeople[edge] += (long)added * members;
                edges.ReplenishedToday[edge] += added;

                continue;
            }

            // Only unreserved stock can turn over; blocked removals accrue no debt for a later Tick.
            long excess = -off;
            long free = groups.Free(slot);
            long wanted = whole < excess ? whole : excess;
            int dropped = (int)(wanted < free ? wanted : free);

            if (dropped <= 0)
            {
                continue;
            }

            groups.Stock[slot] -= dropped;
            groups.Turnover[slot] += dropped;

            edges.TurnoverHouseholds[edge] += dropped;
            edges.TurnoverPeople[edge] += (long)dropped * members;
            edges.TurnoverToday[edge] += dropped;
        }
    }

    /// <summary>Records how many Households each group may be asked for this pass.</summary>
    private void Capture(int edge)
    {
        HinterlandPopulationTable groups = _world.HinterlandPopulation;
        int slots = groups.Rows.SlotCount;

        if (_allowance.Length < slots)
        {
            _allowance = new int[slots];
        }

        foreach (int slot in groups.Groups(_world.Hinterlands).Walk(edge))
        {
            _allowance[slot] = groups.Free(slot);
        }
    }

    /// <summary>Sends home the families that have waited the whole of the authored wait.</summary>
    /// <remarks>
    /// FIFO join order and a shared current duration let expiry stop at the first unexpired row.
    /// </remarks>
    private void Expire(int edge, MapEdge which, in ImmigrationRuleset immigration, Ticks now)
    {
        HinterlandQueueTable queue = _world.HinterlandQueue;
        LinkedIndexList admissions = queue.Admissions(_world.Hinterlands);

        long wait = immigration.QueueWaitTicks;

        while (true)
        {
            int slot = admissions.PeekFront(edge);

            if (slot == Rows.NoSlot || (long)(now.Raw - queue.Since[slot].Raw) < wait)
            {
                return;
            }

            Cancel(edge, which, slot);
            _world.Hinterlands.ExpiredToday[edge]++;
        }
    }

    /// <summary>Sends home everybody waiting at an edge that has lost every door.</summary>
    /// <remarks>
    /// Release every reservation when the edge loses its last gate. Restoring a gate starts fresh
    /// occasions.
    /// </remarks>
    private void DropDisconnected(int edge, MapEdge which)
    {
        if (_world.Hinterlands.GateHead[edge] != 0)
        {
            return;
        }

        LinkedIndexList admissions = _world.HinterlandQueue.Admissions(_world.Hinterlands);

        while (true)
        {
            int slot = admissions.PeekFront(edge);

            if (slot == Rows.NoSlot)
            {
                return;
            }

            Cancel(edge, which, slot);
            _world.Hinterlands.ConnectionLostToday[edge]++;
        }
    }

    /// <summary>Lets the families whose scheduled review is due look at the city again.</summary>
    private void Review(int edge, MapEdge which, in ImmigrationRuleset immigration, Ticks now)
    {
        HinterlandQueueTable queue = _world.HinterlandQueue;
        LinkedIndexList reviews = queue.Reviews(_world.Hinterlands);

        long interval = immigration.QueueReconsiderTicks;
        int slot = reviews.PeekFront(edge);

        while (slot != Rows.NoSlot)
        {
            if ((long)(now.Raw - queue.Reviewed[slot].Raw) < interval)
            {
                return;
            }

            int next = reviews.After(slot);

            queue.Reviewed[slot] = now;
            _world.Hinterlands.ReviewedToday[edge]++;

            // Only a willing comparison retains the reservation. Review never extends Since.
            if (Decides(which, slot, now) != ProspectOutcome.Willing)
            {
                Cancel(edge, which, slot);
                _world.Hinterlands.ChangedMindToday[edge]++;
            }
            else
            {
                reviews.MoveToBack(edge, slot);
            }

            slot = next;
        }
    }

    /// <summary>Serves an edge's queue in order, for as long as a door has room.</summary>
    private void AdmitWaiting(int edge, MapEdge which, Ticks now)
    {
        HinterlandQueueTable queue = _world.HinterlandQueue;
        LinkedIndexList admissions = queue.Admissions(_world.Hinterlands);

        while (true)
        {
            int slot = admissions.PeekFront(edge);

            // Check quota first so a full gate does not trigger a housing comparison each Tick.
            if (slot == Rows.NoSlot || !HasRoom(edge, now))
            {
                return;
            }

            // One comparison per Tick. A family reviewed a moment ago keeps that answer rather than
            // drawing a second one because a door happened to open on the same Tick.
            if (queue.Compared[slot].Raw != now.Raw
                && Decides(which, slot, now) != ProspectOutcome.Willing)
            {
                Cancel(edge, which, slot);
                _world.Hinterlands.ChangedMindToday[edge]++;
                continue;
            }

            if (!TryAdmit(edge, ProspectOf(which, slot), reserved: true, now))
            {
                return;
            }

            queue.Leave(_world.Hinterlands, which, slot);
            _world.Hinterlands.AdmittedToday[edge]++;
        }
    }

    /// <summary>Accrues each group's occasions and presents the families they come to.</summary>
    private void Present(int edge, MapEdge which, in ImmigrationRuleset immigration, Ticks now)
    {
        HinterlandPopulationTable groups = _world.HinterlandPopulation;

        int start = StartOf(edge);
        int slot = start;

        while (slot != Rows.NoSlot)
        {
            int next = Following(slot);
            Occasions(edge, which, slot, immigration, now);
            slot = next;
        }

        slot = Head(edge);

        while (slot != Rows.NoSlot && slot != start)
        {
            int next = Following(slot);
            Occasions(edge, which, slot, immigration, now);
            slot = next;
        }

        // Next Tick starts one group further on, so a fixed declaration order does not hand the same
        // composition every last vacancy in the city.
        int following = start == Rows.NoSlot ? Rows.NoSlot : Following(start);

        if (following == Rows.NoSlot)
        {
            following = Head(edge);
        }

        _world.Hinterlands.GroupCursor[edge] =
            following == Rows.NoSlot ? default : groups.Rows.At(following);
    }

    /// <summary>Turns one group's waiting Households into whole reconsideration occasions.</summary>
    private void Occasions(
        int edge, MapEdge which, int slot, in ImmigrationRuleset immigration, Ticks now)
    {
        HinterlandPopulationTable groups = _world.HinterlandPopulation;

        int free = _allowance[slot];
        HinterlandComposition composition = groups.CompositionAt(slot);

        // A group with nobody free earns nothing, and one with no adult to lead a Household presents
        // nobody at all -- its stock is ineligible rather than repeatedly declining.
        if (free <= 0 || composition.Adults == 0)
        {
            if (groups.Free(slot) == 0)
            {
                groups.ReconsiderNumerator[slot] = 0;
            }

            return;
        }

        long interval = immigration.ReconsiderTicks;
        long numerator = checked(groups.ReconsiderNumerator[slot] + free);
        long occasions = IntegerMath.FloorDiv(numerator, interval);

        groups.ReconsiderNumerator[slot] = numerator - checked(occasions * interval);

        for (long occasion = 0; occasion < occasions && _allowance[slot] > 0; occasion++)
        {
            if (Consider(edge, which, slot, composition, now))
            {
                _allowance[slot]--;
            }
        }
    }

    /// <summary>One family, presented once.</summary>
    /// <returns>Whether it wanted to come, which is what an occasion costs the group's allowance.</returns>
    private bool Consider(
        int edge, MapEdge which, int slot, in HinterlandComposition composition, Ticks now)
    {
        HinterlandTable edges = _world.Hinterlands;

        ulong sequence = checked(edges.Sequence[edge] + 1);
        edges.Sequence[edge] = sequence;

        edges.OccasionsToday[edge]++;

        int door = _world.Buildings.Gates(edges).PeekFront(edge);

        // No door on this edge is a city this family cannot see into. It is not a refusal and it is
        // not a comparison; nothing about the stock moves.
        if (door == Rows.NoSlot)
        {
            edges.NoConnectionToday[edge]++;
            return false;
        }

        if (!_world.Rules.TryHinterland(which, out HinterlandDefinition source))
        {
            edges.NoConnectionToday[edge]++;
            return false;
        }

        // Mixed with the edge, so the same count behind two edges is two families.
        ulong identity = Randomness.Mix(sequence ^ ((ulong)(byte)which << 56));

        ArrivalProspect prospect = ArrivalProspect.Of(
            _key,
            source,
            _world.HinterlandPopulation.Rows.At(slot),
            which,
            composition,
            identity);

        switch (_placement.Compare(prospect, _world.Buildings.Rows.At(door), now))
        {
            case ProspectOutcome.NoSample:
                edges.NoSampleToday[edge]++;
                return false;

            case ProspectOutcome.StayedOutside:
                edges.StayedOutsideToday[edge]++;
                return false;

            default:
                break;
        }

        edges.WillingToday[edge]++;

        if (TryAdmit(edge, prospect, reserved: false, now))
        {
            edges.AdmittedToday[edge]++;
            return true;
        }

        // Every door is full today. The family waits, holding one Household of its group against a
        // second draw, and is served in this order when room appears.
        _world.HinterlandPopulation.Reserved[slot]++;

        _world.HinterlandQueue.Join(
            edges, which, _world.HinterlandPopulation.Rows.At(slot), prospect.Purse, identity, now);

        edges.QueuedToday[edge]++;

        return true;
    }

    /// <summary>
    /// Whether an <c>Arrive</c> command could ever be asking for somebody who exists.
    /// </summary>
    /// <remarks>
    /// Accept either an authored composition or a matching live group; exhaustion is not a mismatch.
    /// </remarks>
    /// <param name="which">The edge the named gate stands on.</param>
    /// <param name="stage">The Life Stage the payload asks for.</param>
    /// <param name="members">How many people the payload asks for, adults and children together.</param>
    public bool CanRequest(MapEdge which, byte stage, int members)
    {
        if (which == MapEdge.None || !_world.Rules.TryHinterland(which, out HinterlandDefinition outside))
        {
            return false;
        }

        HinterlandPopulationTable groups = _world.HinterlandPopulation;

        foreach (int slot in groups.Groups(_world.Hinterlands).Walk(HinterlandTable.SlotOf(which)))
        {
            if (Matches(groups, slot, stage, members))
            {
                return true;
            }
        }

        for (int entry = 0; entry < outside.PopulationCount; entry++)
        {
            HinterlandPopulationDefinition declared =
                _world.Rules.HinterlandPopulations[outside.PopulationFirst + entry];

            int adults = declared.AdultsTier1 + declared.AdultsTier2 + declared.AdultsTier3;

            if (declared.Stage == stage && adults > 0 && adults + declared.Children == members)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Presents the families an <c>Arrive</c> command asked for, out of the stock behind its edge.
    /// </summary>
    /// <remarks>
    /// Extra reconsideration occasions use the same stock, comparison and quota as autonomous
    /// arrivals. Exhaustion stops the request without inventing people or refusing the command.
    /// </remarks>
    /// <param name="which">The edge the named gate stands on.</param>
    /// <param name="stage">The Life Stage the payload asks for.</param>
    /// <param name="members">How many people the payload asks for, adults and children together.</param>
    /// <param name="households">How many families to present.</param>
    /// <param name="now">The Tick the request is made on.</param>
    public void Request(MapEdge which, byte stage, int members, int households, Ticks now)
    {
        if (which == MapEdge.None || !_world.Rules.Immigration.Stated)
        {
            return;
        }

        int edge = HinterlandTable.SlotOf(which);

        for (int asked = 0; asked < households; asked++)
        {
            int slot = Draw(edge, stage, members, now);

            if (slot == Rows.NoSlot)
            {
                return;
            }

            _world.Hinterlands.RequestedToday[edge]++;

            Consider(edge, which, slot, _world.HinterlandPopulation.CompositionAt(slot), now);
        }
    }

    /// <summary>Picks one matching group, weighted by how many of its Households are unpromised.</summary>
    /// <remarks>
    /// Weight by unreserved stock. Each presentation advances the saved sequence for the next draw.
    /// </remarks>
    private int Draw(int edge, byte stage, int members, Ticks now)
    {
        HinterlandPopulationTable groups = _world.HinterlandPopulation;
        IndexList list = groups.Groups(_world.Hinterlands);

        long free = 0;

        foreach (int slot in list.Walk(edge))
        {
            if (Matches(groups, slot, stage, members))
            {
                free += groups.Free(slot);
            }
        }

        if (free <= 0)
        {
            return Rows.NoSlot;
        }

        long taken = (long)(Randomness.Draw(
            _key, _world.Hinterlands.Sequence[edge], now, PurposeTag.RequestedFamily) % (ulong)free);

        long running = 0;

        foreach (int slot in list.Walk(edge))
        {
            if (!Matches(groups, slot, stage, members))
            {
                continue;
            }

            running += groups.Free(slot);

            if (taken < running)
            {
                return slot;
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>Whether a group holds the family a payload names.</summary>
    /// <remarks>
    /// Child-only return groups are counted but cannot present an adult-led Household.
    /// </remarks>
    private static bool Matches(HinterlandPopulationTable groups, int slot, byte stage, int members) =>
        groups.Stage[slot] == stage
        && groups.Members(slot) == members
        && groups.AdultsTier1[slot] + groups.AdultsTier2[slot] + groups.AdultsTier3[slot] > 0;

    /// <summary>Whether any door on this edge can still admit somebody today.</summary>
    private bool HasRoom(int edge, Ticks now)
    {
        foreach (int gate in _world.Buildings.Gates(_world.Hinterlands).Walk(edge))
        {
            if (_world.GateHasRoom(gate, now))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Offers a family to each of an edge's doors in turn, starting after the last one used.</summary>
    /// <remarks>
    /// Resume after the last monotonic gate id, wrapping once. Recycled slots do not define priority.
    /// </remarks>
    private bool TryAdmit(int edge, in ArrivalProspect prospect, bool reserved, Ticks now)
    {
        IndexList gates = _world.Buildings.Gates(_world.Hinterlands);
        ulong last = _world.Hinterlands.LastGate[edge];

        for (int pass = 0; pass < 2; pass++)
        {
            foreach (int gate in gates.Walk(edge))
            {
                ulong id = _world.Buildings.Rows.IdAt(gate);

                if (pass == 0 ? id <= last : id > last)
                {
                    continue;
                }

                Admission outcome = _world.TryAdmitProspect(
                    prospect, _world.Buildings.Rows.At(gate), now, out _, reserved);

                if (outcome == Admission.Admitted)
                {
                    _world.Hinterlands.LastGate[edge] = id;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>What a waiting family makes of the city now, on its retained purse and tastes.</summary>
    private ProspectOutcome Decides(MapEdge which, int slot, Ticks now)
    {
        int edge = HinterlandTable.SlotOf(which);
        int door = _world.Buildings.Gates(_world.Hinterlands).PeekFront(edge);

        _world.HinterlandQueue.Compared[slot] = now;

        // Nothing to look through. Not a rejection, so the family keeps its place and its wait runs.
        if (door == Rows.NoSlot)
        {
            return ProspectOutcome.NoSample;
        }

        return _placement.Compare(
            ProspectOf(which, slot), _world.Buildings.Rows.At(door), now);
    }

    /// <summary>The family a queue row stands for, composition read off its group.</summary>
    private ArrivalProspect ProspectOf(MapEdge which, int slot)
    {
        HinterlandQueueTable queue = _world.HinterlandQueue;
        int group = _world.HinterlandPopulation.Rows.Resolve(queue.Group[slot]);

        return new ArrivalProspect(
            queue.Group[slot],
            which,
            _world.HinterlandPopulation.CompositionAt(group),
            queue.Purse[slot],
            queue.Identity[slot]);
    }

    /// <summary>Releases a waiting family's reservation and frees its row.</summary>
    private void Cancel(int edge, MapEdge which, int slot)
    {
        HinterlandQueueTable queue = _world.HinterlandQueue;
        int group = _world.HinterlandPopulation.Rows.Resolve(queue.Group[slot]);

        _world.HinterlandPopulation.Reserved[group]--;

        queue.Leave(_world.Hinterlands, which, slot);
    }

    /// <summary>Where this Tick's walk over the edge's compositions starts.</summary>
    private int StartOf(int edge)
    {
        HinterlandPopulationTable groups = _world.HinterlandPopulation;

        // A cursor naming a group that has been retired, or one that has been recycled onto another
        // edge, starts the walk at the head instead.
        if (groups.Rows.TryResolve(_world.Hinterlands.GroupCursor[edge], out int slot)
            && groups.Edge[slot] == (byte)HinterlandTable.EdgeAt(edge))
        {
            return slot;
        }

        return Head(edge);
    }

    private int Head(int edge)
    {
        int encoded = _world.Hinterlands.GroupHead[edge];
        return encoded == 0 ? Rows.NoSlot : encoded - 1;
    }

    private int Following(int slot)
    {
        int encoded = _world.HinterlandPopulation.GroupNext[slot];
        return encoded == 0 ? Rows.NoSlot : encoded - 1;
    }
}
