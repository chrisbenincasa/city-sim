using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Invariants;

/// <summary>
/// The checks the four tables of slice 4 can carry, registered into their tiers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Most of <c>02 §10</c>'s list is absent because most of what it names is.</b> Goods conserved
/// needs Bins, no Trip without a Fate needs Trips, and parking occupancy conserved needs parking —
/// all slice 7 or later. What exists today is the referential structure between Lots, Buildings,
/// Households and Citizens, and that is what is checked.
/// </para>
/// <para>
/// <b>Where a corpus invariant splits across two tiers, both halves are here and neither is
/// complete alone.</b> <em>No Citizen in two places</em> becomes an <c>O(changed)</c> check at the
/// write site, which is complete within one Household and blind across two, plus a whole-world walk
/// at the end of the run which is complete and unaffordable per Tick. That is the tiering doing its
/// job rather than a compromise, but a reader who saw only one half would over-trust it.
/// </para>
/// </remarks>
public static class WorldInvariants
{
    /// <summary>Registers every check this slice can make.</summary>
    public static void RegisterAll(InvariantRegistry invariants)
    {
        ArgumentNullException.ThrowIfNull(invariants);

        invariants.Register(InvariantTier.Staggered, HouseholdsLiveSomewhereReal);
        invariants.Register(InvariantTier.Staggered, CitizensBelongToARealHousehold);
        invariants.Register(InvariantTier.Staggered, SegmentsAreWellFormed);

        invariants.Register(InvariantTier.EndOfRun, EveryHandleResolves);
        invariants.Register(InvariantTier.EndOfRun, EveryoneIsInExactlyOnePlace);
        invariants.Register(InvariantTier.EndOfRun, MoneyIsRepresentable);

        // Registered after the overflow check and never before it. RunEndOfRun walks the list in order
        // and Require throws on the first violation, so a city whose money has run away has to report
        // as unrepresentable rather than as unconserved -- a sum that has overflowed is also unequal to
        // its anchor, and the second diagnosis is the one that names the bug.
        invariants.Register(InvariantTier.EndOfRun, MoneyIsConserved);
        invariants.Register(InvariantTier.EndOfRun, LayerMagnitudesAreBounded);
        invariants.Register(InvariantTier.EndOfRun, RuleInstancesAreQueuedExactlyOnce);
        invariants.Register(InvariantTier.EndOfRun, NoBuildingRunsRulesItsKindDoesNotDeclare);
        invariants.Register(InvariantTier.EndOfRun, LotsAndBuildingsAgreeWhoIsWhere);
        invariants.Register(InvariantTier.EndOfRun, ThePoolIsDenseAndAgreesWithTheHouseholds);
        invariants.Register(InvariantTier.EndOfRun, NoWaiterSleepsOnANonBlockingBin);
        invariants.Register(InvariantTier.EndOfRun, BinCapacitiesMatchTheirDeclarations);
        invariants.Register(InvariantTier.EndOfRun, TheAdjacencyDescribesTheSegments);
        invariants.Register(InvariantTier.EndOfRun, VacantLandHasAStreetToBuildOff);
        invariants.Register(InvariantTier.EndOfRun, TrafficIsConserved);
    }

    /// <summary>
    /// Every Traveller on the road is on exactly one Segment's volume, and nobody else is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0041</c>'s invariant, in the tier a whole-graph sum belongs to. The argument for it is
    /// on <see cref="Invariant.SegmentVolumeIsConserved"/>; the argument for it being written while
    /// both sides are structurally zero is there too.
    /// </para>
    /// <para>
    /// <b>The right-hand side counts <em>vehicular</em> Travellers, and that qualifier is the whole
    /// reason the check has content.</b> A Traveller on foot contributes nothing —
    /// <c>CONTEXT.md</c> → Fidelity keeps pedestrians out of Stress entirely — so a version that
    /// counted every in-flight Traveller would fail on a walking city and would have to be weakened by
    /// whoever met it first.
    /// </para>
    /// </remarks>
    internal static void TrafficIsConserved(World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        RoadSegmentTable segments = world.Roads.Segments;

        long onTheRoad = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot))
            {
                continue;
            }

            onTheRoad += segments.VolumeForward[slot] + segments.VolumeBackward[slot];
        }

        long driving = 0;

        TravellerTable travellers = world.Travellers;
        LegTable legs = world.Legs;

        for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
        {
            if (!travellers.Rows.IsLive(slot))
            {
                continue;
            }

            if ((TravelMode)legs.Mode[travellers.CurrentLeg[slot]] != TravelMode.Foot)
            {
                driving++;
            }
        }

        report.Require(
            onTheRoad == driving, Invariant.SegmentVolumeIsConserved, (int)onTheRoad, (int)driving);
    }

    /// <summary>
    /// Every vacant Lot fronts a Street that exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The occupied case is deliberately excluded and is not an oversight</b> —
    /// <see cref="Invariant.VacantLotHasFrontage"/> carries the argument. A Building outlives its
    /// frontage (<c>adr/0079</c>), so requiring frontage of every Lot would make a legal road edit
    /// report a violation.
    /// </para>
    /// <para>
    /// <b>Both directions, because a stale index and an absent one fail differently.</b> The Lot must
    /// name a Segment, and that Segment must still be live: freeing a Segment without rebuilding
    /// frontage would leave a Lot pointing at a recycled slot, which reads as perfectly valid and is
    /// the exact defect the Epoch exists to prevent.
    /// </para>
    /// </remarks>
    internal static void VacantLandHasAStreetToBuildOff(World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        if (!world.HasStreets)
        {
            // No lattice, so no Lot in this world was ever meant to have frontage. Skipped rather
            // than passed vacuously per Lot, because the distinction is worth being able to read off
            // the code: this is a world without geography, not a world whose geography is broken.
            return;
        }

        LotTable lots = world.Lots;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || !lots.IsVacant(slot))
            {
                continue;
            }

            int segment = lots.FrontageSlot[slot] - 1;

            report.Require(
                segment >= 0 && world.Roads.Segments.Rows.IsLive(segment),
                Invariant.VacantLotHasFrontage,
                slot,
                segment);
        }
    }

    /// <summary>
    /// No waiter is asleep on a Bin that has stopped blocking it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0033</c>'s second required mitigation, built at last</b> — see
    /// <see cref="Invariant.WaiterIsBlockedByTheBinItNames"/> for why it is narrower than that ADR's
    /// wording and stronger than the reading it invites. It is the <em>only</em> thing in the design
    /// that can notice a missed wake, because a Rule asleep when it should be running leaves no trace
    /// in a State Hash: the rows are all consistent, and what is wrong is that one of them is not moving.
    /// </para>
    /// <para>
    /// <b>End of run, because that is what makes it affordable rather than because it is unimportant.</b>
    /// <c>02 §10</c>'s tiering is by frequency: this is a whole-world walk of both wait lists on every
    /// Bin, and there is one of them per run however long the run was. Per Tick it would be the
    /// <c>O(world)</c> guard <c>S0a</c> found costing 95% of a run.
    /// </para>
    /// <para>
    /// <b>Registered here rather than left to a caller, which is the mistake this file has already
    /// made once.</b> <c>HouseholdHomeExists</c> was reported by nothing — the only orphan among 26
    /// members — and was found by audit rather than by failure, so the registration line is the point
    /// of the exercise and not the paperwork around it.
    /// </para>
    /// </remarks>
    internal static void NoWaiterSleepsOnANonBlockingBin(World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        CheckQueueStillBlocks(world, world.SupplyWaiters, Blocking.Supply, report);
        CheckQueueStillBlocks(world, world.SpaceWaiters, Blocking.Space, report);
    }

    /// <summary>Walks one of the two wait lists on every Bin, re-deriving what stopped each waiter.</summary>
    private static void CheckQueueStillBlocks(
        World world, IndexList waiters, Blocking blocking, InvariantRegistry report)
    {
        BinTable bins = world.Bins;

        for (int bin = 0; bin < bins.Rows.SlotCount; bin++)
        {
            if (!bins.Rows.IsLive(bin))
            {
                continue;
            }

            foreach (int instance in waiters.Walk(bin))
            {
                // A row that is on the wrong list, or names a Bin other than this one, is
                // WaiterIsQueuedOnTheBinItNames' violation and not this one. Deriving against a Bin the
                // waiter never named would report a second violation for one defect, and the artifact
                // would carry two ids for one cause.
                if (!world.RuleInstances.Rows.IsLive(instance)
                    || world.RuleInstances.Blocked[instance] != blocking
                    || !bins.Rows.TryResolve(world.RuleInstances.WaitingOn[instance], out int named)
                    || named != bin)
                {
                    continue;
                }

                report.Require(
                    RuleEngine.BinStillBlocks(world, instance, bin, blocking),
                    Invariant.WaiterIsBlockedByTheBinItNames,
                    instance,
                    bin);
            }
        }
    }

    /// <summary>
    /// Every live Bin's capacity is what the Ruleset in force declares for its Building's kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The check <c>adr/0064</c> owes for making capacity derived.</b> See
    /// <see cref="Invariant.BinCapacityMatchesItsDeclaration"/> for why a stale derived column is silent:
    /// every row stays self-consistent, and the only wrong thing is a ceiling that agrees with a Ruleset
    /// nobody is running. Re-deriving from the same source the two write sites read is what makes it a
    /// check on the <em>rebuild's placement</em> rather than on its arithmetic.
    /// </para>
    /// <para>
    /// <b>A Bin whose owner has gone is checked at zero, like a dropped kind.</b> Both reduce to
    /// <em>declares no store of this Resource</em>, which is the derivation's answer and not an
    /// exemption — and the alternative, skipping such rows, would stop checking exactly the rows a
    /// reload has just disturbed.
    /// </para>
    /// </remarks>
    internal static void BinCapacitiesMatchTheirDeclarations(World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        BinTable bins = world.Bins;

        for (int bin = 0; bin < bins.Rows.SlotCount; bin++)
        {
            if (!bins.Rows.IsLive(bin))
            {
                continue;
            }

            // The treasury's ceiling comes from 04 §2 -- "Money is a Resource too, and its Bin is
            // unbounded" -- and not from a Building kind it does not have. Asserting it against
            // DeclaredCapacity would be asserting it against the zero returned for a kind nobody
            // declared, so the assertion here is the one the design actually makes.
            if (bins.OwnerKind[bin] != BinOwnerKind.Building)
            {
                report.Require(
                    bins.Capacity[bin] == long.MaxValue,
                    Invariant.BinCapacityMatchesItsDeclaration,
                    bin,
                    bins.Capacity[bin]);

                continue;
            }

            byte kind = world.Buildings.Rows.TryResolve(bins.Owner[bin], out int building)
                ? world.Buildings.Kind[building]
                : (byte)0;

            report.Require(
                bins.Capacity[bin] == world.DeclaredCapacity(kind, bins.Resource[bin]),
                Invariant.BinCapacityMatchesItsDeclaration,
                bin,
                bins.Capacity[bin]);
        }
    }

    /// <summary>
    /// The Unplaced Pool is dense, and every membership in it is a live unhoused Household.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Density is the half that would fail silently and expensively.</b> A dead slot inside
    /// <c>[0, Count)</c> makes a draw over the count name nothing, so a Lot that passed every term of
    /// the create predicate is not built on — at a rate set by how much the Pool has churned. The city
    /// then grows more slowly than its Ruleset says, with no error anywhere, which is precisely the
    /// shape <c>ZoneSampleTests</c>' coverage check was written against on the sampling side.
    /// </para>
    /// <para>
    /// <b>The agreement half is the bijection, both ways.</b> Pool to Household catches a membership
    /// left behind after somebody housed a Household without going through <see cref="World.Place"/>;
    /// Household to Pool catches a reverse index pointing at a position that has since been swapped
    /// or freed. Either one makes the draw favour somebody, and a favoured Household is indisting-
    /// uishable from luck.
    /// </para>
    /// </remarks>
    internal static void ThePoolIsDenseAndAgreesWithTheHouseholds(
        World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        UnplacedTable pool = world.UnplacedPool;
        HouseholdTable households = world.Households;

        for (int slot = 0; slot < pool.Rows.SlotCount; slot++)
        {
            // Live below the count, dead at or above it. Stated as one claim over every slot rather
            // than as a live-count comparison, because two compensating holes sum correctly.
            if (pool.Rows.IsLive(slot) != (slot < pool.Count))
            {
                report.Report(Invariant.ThePoolIsDense, slot);
                continue;
            }

            if (!pool.Rows.IsLive(slot))
            {
                continue;
            }

            if (!households.Rows.TryResolve(pool.Household[slot], out int householdSlot))
            {
                report.Report(Invariant.ThePoolNamesOnlyUnhousedHouseholds, slot);
                continue;
            }

            report.Require(
                households.PoolPosition(householdSlot) == slot,
                Invariant.ThePoolNamesOnlyUnhousedHouseholds,
                slot,
                householdSlot);

            report.Require(
                !world.Buildings.Rows.TryResolve(households.Dwelling[householdSlot], out _),
                Invariant.ThePoolNamesOnlyUnhousedHouseholds,
                slot,
                householdSlot);
        }
    }

    /// <summary>
    /// <c>02 §2.2</c>'s <em>a Lot is either vacant or holds exactly one Building</em>, whole-world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The claim is as old as the design and has never been checkable</b>, because until slice 10
    /// the relation was one-directional — a Building named its Lot and nothing named the Building. The
    /// reverse index is what makes the second direction expressible, and an index is exactly the kind
    /// of thing that is right when written and wrong three mechanisms later.
    /// </para>
    /// <para>
    /// <b>Both directions are walked, because each sees a failure the other cannot.</b> Walking
    /// Buildings catches an index left stale by a demolition that freed the row without vacating the
    /// Lot. Walking Lots catches an index still pointing at a slot that has since been freed — or
    /// worse, recycled into an unrelated Building, which reads as perfectly valid from the Building
    /// side and is the failure a generation counter exists to make loud.
    /// </para>
    /// <para>
    /// <b>End-of-run rather than staggered.</b> It is <c>O(Buildings + Lots)</c> with no per-row
    /// allocation, and the runs that surface an index bug are the long headless ones — <c>02 §10</c>'s
    /// shape, and the same argument the other four checks here make.
    /// </para>
    /// </remarks>
    internal static void LotsAndBuildingsAgreeWhoIsWhere(World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        BuildingTable buildings = world.Buildings;
        LotTable lots = world.Lots;

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (!buildings.Rows.IsLive(slot))
            {
                continue;
            }

            if (!lots.Rows.TryResolve(buildings.Lot[slot], out int lotSlot))
            {
                // A Building standing on a Lot that no longer exists. Reported here rather than left
                // to EveryHandleResolves because the consequence is specific: nothing can ever
                // demolish it, since a Zone Rule reaches a Building only through its Lot.
                report.Report(Invariant.LotHoldsExactlyOneBuilding, slot);
                continue;
            }

            report.Require(
                lots.BuildingOn(lotSlot) == slot,
                Invariant.LotHoldsExactlyOneBuilding,
                slot,
                lotSlot);
        }

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || lots.IsVacant(slot))
            {
                continue;
            }

            int building = lots.BuildingOn(slot);

            // IsLive bounds-checks, so a decode that has gone wrong entirely fails here rather than
            // indexing out of range.
            bool agrees = buildings.Rows.IsLive(building)
                && lots.Rows.TryResolve(buildings.Lot[building], out int back)
                && back == slot;

            report.Require(agrees, Invariant.LotHoldsExactlyOneBuilding, slot, building);
        }
    }

    /// <summary>
    /// <c>adr/0003</c>'s <em>no quantity accumulates without bound</em>, applied to the Map Layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is an overflow guard and no longer a design bound, which is what
    /// <c>adr/0051</c> restored it to.</b> Pollution was once a source with no removal — every emission
    /// added and nothing subtracted — and while that was true this check and the long-run test were the
    /// only things standing between the design and an overflow. <see cref="MapLayers.DecayPollution"/>
    /// is the sink now, and the bound on the level is emergent: a steady emitter settles where what it
    /// adds each cadence equals what the ground absorbs. So this fires only on arithmetic that could
    /// not be represented, which is the job it was written for.
    /// </para>
    /// <para>
    /// <b>The ceiling is the kernel's, not the integer's, and that is the point of checking it here
    /// rather than trusting the arithmetic to throw.</b> A two-pass tent multiplies a source by
    /// <c>Scale</c> — 6,561 at radius 8 — so a source Cell above roughly 327,000 cannot be represented
    /// diffused. Waiting for <c>LayerDiffusion</c> to overflow would report the failure at the Tick a
    /// plume happened to be recomputed, which is a diffusion cadence after the Tick that caused it.
    /// </para>
    /// <para>
    /// <b>End-of-run rather than staggered, because that is where the runs that surface it are.</b>
    /// <c>adr/0033</c>'s shape, quoted by <c>02 §10</c>: unaffordable per Tick and trivial once per
    /// run, however long the run was.
    /// </para>
    /// </remarks>
    internal static void LayerMagnitudesAreBounded(World world, InvariantRegistry report)
    {
        LayerCellTable cells = world.Layers.Cells;
        // This world's kernel rather than a static one: since slice 8 the radius is Ruleset data in
        // adr/0015's world-creation category, so the ceiling it implies is a property of the world
        // being checked and not of the build checking it.
        int ceiling = world.Layers.PollutionKernel.SourceCeiling;

        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot))
            {
                continue;
            }

            if (IntegerMath.Abs(cells.PollutionSource[slot]) > ceiling)
            {
                report.Report(Invariant.LayerMagnitudeIsBounded, slot);
                return;
            }

            int sealed_ = cells.Sealing[slot];

            if (sealed_ < 0 || sealed_ > CellGrid.TilesInCell)
            {
                report.Report(Invariant.SealingIsWithinTheCell, slot);
                return;
            }
        }
    }

    /// <summary>
    /// <c>02 §10</c>: every Household's home exists and lists them as an occupant.
    /// </summary>
    internal static void HouseholdsLiveSomewhereReal(
        World world, int slice, int slices, InvariantRegistry report)
    {
        HouseholdTable households = world.Households;
        (int from, int to) = InvariantRegistry.Range(slice, slices, households.Rows.SlotCount);
        IndexList occupants = world.Occupants;

        for (int slot = from; slot < to; slot++)
        {
            if (!households.Rows.IsLive(slot))
            {
                continue;
            }

            if (!world.Buildings.Rows.TryResolve(households.Dwelling[slot], out int building))
            {
                // adr/0054 qualified this rather than deleting it: no dwelling is legal precisely
                // when the Pool holds them. A Household that is neither housed nor looking is still
                // a row nothing will ever touch again, which is what the check is for.
                report.Require(
                    households.IsUnplaced(slot), Invariant.HouseholdIsHousedOrInThePool, slot);
                continue;
            }

            // And the other side of the exclusive-or, which is the half a one-directional check
            // would miss: a Household housed *and* in the Pool would be drawn for a second dwelling.
            report.Require(
                !households.IsUnplaced(slot), Invariant.HouseholdIsHousedOrInThePool, slot);

            report.Require(
                Lists(occupants, building, slot),
                Invariant.HouseholdIsAnOccupantOfItsHome,
                slot,
                building);
        }
    }

    /// <summary>The same claim for Citizens, whose Household is their place in the world.</summary>
    internal static void CitizensBelongToARealHousehold(
        World world, int slice, int slices, InvariantRegistry report)
    {
        CitizenTable citizens = world.Citizens;
        (int from, int to) = InvariantRegistry.Range(slice, slices, citizens.Rows.SlotCount);
        IndexList members = world.Members;

        for (int slot = from; slot < to; slot++)
        {
            if (!citizens.Rows.IsLive(slot))
            {
                continue;
            }

            if (!world.Households.Rows.TryResolve(citizens.HouseholdOf[slot], out int household))
            {
                report.Report(Invariant.CitizenHouseholdExists, slot);
                continue;
            }

            report.Require(
                Lists(members, household, slot),
                Invariant.CitizenIsAMemberOfItsHousehold,
                slot,
                household);
        }
    }

    /// <summary>
    /// <c>02 §10</c>: every cross-table handle valid.
    /// </summary>
    /// <remarks>
    /// <b>Driven by the columns rather than by a list of fields.</b> Every column is asked whether it
    /// dangles, so a handle column added in a later slice is covered the day it is declared. A walk
    /// naming the fields it knows about would share its blind spot with the bug it exists to find,
    /// which is the same argument the per-field declaration makes about the State Hash.
    /// </remarks>
    internal static void EveryHandleResolves(World world, InvariantRegistry report)
    {
        foreach (Rows table in world.Tables)
        {
            foreach (Column column in table.Columns)
            {
                for (int slot = 0; slot < table.SlotCount; slot++)
                {
                    // A freed row's columns are not cleared, so its stale handles are not a
                    // violation — nothing can reach them. NoFreedRowIsStillLinked is what says so.
                    if (table.IsLive(slot) && column.IsDangling(slot))
                    {
                        report.Report(Invariant.CrossTableHandleResolves, slot);
                    }
                }
            }
        }
    }

    /// <summary>
    /// <c>02 §10</c>: no Citizen in two places, completely, which needs the whole world at once.
    /// </summary>
    /// <remarks>
    /// <b>Counted rather than searched.</b> Asking of each Citizen <em>is this row in any other
    /// list</em> is <c>O(n²)</c>; walking every list once and counting appearances is <c>O(n)</c> and
    /// answers more — it catches the Citizen in no list at all, and the freed row still linked into
    /// one, which is <c>adr/0006</c>'s unreachable-and-unfreeable row arriving by the back door.
    /// </remarks>
    internal static void EveryoneIsInExactlyOnePlace(World world, InvariantRegistry report)
    {
        Count(
            world.Members,
            world.Households.Rows,
            world.Citizens.Rows,
            Invariant.CitizenIsInExactlyOneHousehold,
            report);

        // adr/0054's second consequence, and the suite found it rather than the plan: a Household in
        // the Unplaced Pool is in *no* Building's occupant list, so the unqualified count reported it
        // the instant the first eviction landed. The exemption is two-sided at no extra cost — an
        // unplaced Household that is still listed somewhere reads as one appearance against an
        // expected zero, which is the corruption that would let it be housed twice.
        Count(
            world.Occupants,
            world.Buildings.Rows,
            world.Households.Rows,
            Invariant.HouseholdIsInExactlyOneBuilding,
            report,
            absentIsLegal: world.Households.IsUnplaced);

        // Milestone 5b-bis task 2. Unemployment is the common case rather than the exception here --
        // no shipped Ruleset declares a job -- so the exemption carries almost every row, and what
        // it is actually checking is the other direction: that nobody is on a list they have no
        // handle for. The predicate is a handle resolve rather than a flag, because a Workplace is
        // Severable and a demolished employer leaves the Citizen holding a dangling one.
        Count(
            world.Workers,
            world.Buildings.Rows,
            world.Citizens.Rows,
            Invariant.CitizenIsInExactlyOneWorkplace,
            report,
            absentIsLegal: slot =>
                !world.Buildings.Rows.TryResolve(world.Citizens.Workplace[slot], out _));
    }

    /// <summary>
    /// <c>adr/0003</c>'s overflow detector, which is the half of <em>money conserved</em> that can be
    /// checked before there is a treasury to conserve it against.
    /// </summary>
    /// <remarks>
    /// <b>What this catches is an accumulator with no sink</b>, which is the failure
    /// <c>adr/0006</c> describes for collections and <c>adr/0003</c>'s extension describes for
    /// magnitudes. Conservation proper arrives with transactions; until then a sum that has run away
    /// is the visible end of the same bug.
    /// </remarks>
    internal static void MoneyIsRepresentable(World world, InvariantRegistry report)
    {
        MoneyLedger ledger = MoneyLedger.Of(world);

        if (!ledger.Representable)
        {
            report.Report(Invariant.MoneyIsRepresentable, ledger.Overflowed);
        }
    }

    /// <summary>
    /// <c>adr/0031</c>'s conservation invariant: the city holds exactly the money issued into it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two independent quantities, which is what <see cref="TrafficIsConserved"/> does and why it
    /// has content.</b> The left side walks the places money can sit; the right side is
    /// <see cref="MoneySupplyTable.Issued"/>, which only <c>World.Endow</c> moves. The argument for
    /// each half is on <see cref="Invariant.MoneyIsConserved"/>.
    /// </para>
    /// <para>
    /// <b>The places money can sit are one</b> since <c>adr/0114</c>: a Bin whose Resource is
    /// conserved. Its owner may be a Household, a Business or the treasury and this does not ask — see
    /// <see cref="MoneyLedger"/>, which is where that stopped being a list of actors to keep up to
    /// date, and which the Census now reads as well.
    /// </para>
    /// <para>
    /// <b>It returns silently on overflow rather than reporting.</b> A sum that cannot be represented
    /// is <see cref="Invariant.MoneyIsRepresentable"/>'s failure and that check is registered first, so
    /// it has already spoken; reporting here as well would give one bug two names, and the second name
    /// would be the misleading one.
    /// </para>
    /// </remarks>
    internal static void MoneyIsConserved(World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        MoneyLedger ledger = MoneyLedger.Of(world);

        if (!ledger.Representable)
        {
            return;
        }

        long issued = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

        report.Require(
            ledger.Total == issued, Invariant.MoneyIsConserved, other: ledger.Total - issued);
    }

    /// <summary>
    /// Every Rule Instance is in exactly one queue, and it is the queue it says it is in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The whole-world half of <see cref="Invariant.RuleInstanceIsArmedOrWaiting"/>.</b> Sharing
    /// one link column makes <em>on two lists at once</em> unrepresentable, and that is worth having,
    /// but it makes the opposite failure easy: a row taken off the Wheel and never subscribed, or
    /// subscribed and never appended, is on <em>no</em> list. Nothing will ever reach it again, which
    /// is <c>05 §9</c>'s asleep-for-ever with no write site to blame — so it can only be found by
    /// counting.
    /// </para>
    /// <para>
    /// <b>Counted the way <see cref="EveryoneIsInExactlyOnePlace"/> counts</b>, and for its reason:
    /// asking of each row <em>which queue am I on</em> is a search per row, and walking every queue
    /// once answers more. The <em>which</em> half is checked on the way past, while the walk already
    /// knows what queue it is in — a row queued on the wrong Bin wakes on writes to a Bin it does not
    /// care about and sleeps through the one it does.
    /// </para>
    /// </remarks>
    /// <summary>
    /// No Building runs a Rule Instance under a kind the Ruleset in force does not declare.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Whole-world rather than at a write site, because the write site cannot see the failure.</b>
    /// <see cref="World.CreateRuleInstance"/> is called with a kind's declared Rules and is therefore
    /// always locally correct; what this catches is a <em>reload</em> that derelicted a Building and
    /// left its Rules armed — one pass over the world, wrong for the rest of the run, and invisible
    /// from every row involved because each one is individually legal.
    /// </para>
    /// <para>
    /// <b>Both directions of one claim.</b> A derelict Building must run nothing, and a Building that
    /// is <em>not</em> derelict must run only Rules its current kind declares — the second half is
    /// what catches a migration that remapped a kind without refitting it, where the Rule Instances
    /// survive and quietly belong to the species the Building used to be.
    /// </para>
    /// </remarks>
    internal static void NoBuildingRunsRulesItsKindDoesNotDeclare(
        World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        BuildingTable buildings = world.Buildings;
        Ruleset rules = world.Rules;

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (!buildings.Rows.IsLive(slot))
            {
                continue;
            }

            byte kind = buildings.Kind[slot];

            foreach (int instance in world.BuildingRules.Walk(slot))
            {
                if (!rules.Declares(kind))
                {
                    report.Report(Invariant.DerelictBuildingRunsNoRules, slot, instance);
                    continue;
                }

                report.Require(
                    Declares(rules.RulesOf(kind), world.RuleInstances.Rule[instance]),
                    Invariant.DerelictBuildingRunsNoRules,
                    slot,
                    instance);
            }
        }
    }

    /// <summary>Whether a kind's declared chain heads include this Rule.</summary>
    private static bool Declares(ReadOnlySpan<RuleId> declared, RuleId rule)
    {
        foreach (RuleId candidate in declared)
        {
            if (candidate == rule)
            {
                return true;
            }
        }

        return false;
    }

    internal static void RuleInstancesAreQueuedExactlyOnce(World world, InvariantRegistry report)
    {
        RuleInstanceTable instances = world.RuleInstances;
        var seen = new int[instances.Rows.SlotCount];

        CountQueue(world, world.SupplyWaiters, Blocking.Supply, seen, report);
        CountQueue(world, world.SpaceWaiters, Blocking.Space, seen, report);

        IndexList armed = world.Wheel.Armed;

        for (int bucket = 0; bucket < EventWheel.Size; bucket++)
        {
            foreach (int instance in armed.Walk(bucket))
            {
                if (!Tally(seen, instance, instances.Rows, bucket, report))
                {
                    return;
                }

                bool armedHere = instances.Blocked[instance] == Blocking.Nothing
                    && EventWheel.BucketOf(instances.NextTick[instance]) == bucket;

                report.Require(
                    armedHere, Invariant.WaiterIsQueuedOnTheBinItNames, instance, bucket);

                // The bucket agreeing is not the same as the Tick being reachable. BucketOf is
                // NextTick % WHEEL_SIZE, so the test above is invariant under adding a whole period:
                // a row due 8,192 Ticks ago and a row due next period sit in the same bucket and both
                // pass it. That is the one error a modulus can make, and it cannot be caught by a
                // check written modulo the same number — so this one is written in absolute Ticks.
                // The window is half-open at the bottom, and the boundary is not a rounding choice:
                // Simulation advances _tick after running it, so the Tick handed to a tier is the NEXT
                // one to run, and a row armed for exactly it is due next rather than overdue. Mid-Tick
                // the distinction cannot arise — Phase 1 has already popped that bucket — so `>=`
                // costs nothing against the error this is here for, which is a stale NextTick a whole
                // period out.
                Ticks due = instances.NextTick[instance];

                report.Require(
                    due >= report.Tick && due < report.Tick + new Ticks(EventWheel.Size),
                    Invariant.AnArmedRowIsDueWithinOnePeriod,
                    instance,
                    bucket);
            }
        }

        for (int instance = 0; instance < seen.Length; instance++)
        {
            if (instances.Rows.IsLive(instance) && seen[instance] != 1)
            {
                report.Report(
                    Invariant.RuleInstanceIsArmedOrWaiting, instance, seen[instance]);
            }
        }
    }

    /// <summary>Walks one of the two wait lists on every Bin, counting and checking as it goes.</summary>
    private static void CountQueue(
        World world, IndexList waiters, Blocking blocking, int[] seen, InvariantRegistry report)
    {
        RuleInstanceTable instances = world.RuleInstances;
        BinTable bins = world.Bins;

        for (int bin = 0; bin < bins.Rows.SlotCount; bin++)
        {
            if (!bins.Rows.IsLive(bin))
            {
                continue;
            }

            foreach (int instance in waiters.Walk(bin))
            {
                if (!Tally(seen, instance, instances.Rows, bin, report))
                {
                    return;
                }

                bool queuedHere = instances.Blocked[instance] == blocking
                    && bins.Rows.TryResolve(instances.WaitingOn[instance], out int named)
                    && named == bin;

                report.Require(queuedHere, Invariant.WaiterIsQueuedOnTheBinItNames, instance, bin);
            }
        }
    }

    /// <summary>Records one appearance, refusing an index no live row could have.</summary>
    private static bool Tally(
        int[] seen, int instance, Rows instances, int owner, InvariantRegistry report)
    {
        if (instance < 0 || instance >= seen.Length)
        {
            report.Report(Invariant.RuleInstanceIsArmedOrWaiting, instance, owner);
            return false;
        }

        seen[instance]++;

        if (!instances.IsLive(instance))
        {
            report.Report(Invariant.NoFreedRowIsStillLinked, instance, owner);
        }

        return true;
    }

    /// <summary>
    /// Walks every list once, counting how many times each element appears.
    /// </summary>
    /// <param name="absentIsLegal">
    /// Elements that belong in no list at all. <b>An exemption rather than a skip</b>: an exempt
    /// element is expected to appear exactly zero times, so being listed anywhere still reports.
    /// </param>
    private static void Count(
        IndexList list,
        Rows owners,
        Rows elements,
        Invariant invariant,
        InvariantRegistry report,
        Func<int, bool>? absentIsLegal = null)
    {
        var seen = new int[elements.SlotCount];

        for (int owner = 0; owner < owners.SlotCount; owner++)
        {
            if (!owners.IsLive(owner))
            {
                continue;
            }

            foreach (int element in list.Walk(owner))
            {
                if (element < 0 || element >= seen.Length)
                {
                    report.Report(invariant, element, owner);
                    return;
                }

                seen[element]++;

                if (!elements.IsLive(element))
                {
                    report.Report(Invariant.NoFreedRowIsStillLinked, element, owner);
                }
            }
        }

        for (int element = 0; element < seen.Length; element++)
        {
            int expected = absentIsLegal is not null && absentIsLegal(element) ? 0 : 1;

            if (elements.IsLive(element) && seen[element] != expected)
            {
                report.Report(invariant, element, seen[element]);
            }
        }
    }

    /// <summary>Whether <paramref name="node"/> is in <paramref name="owner"/>'s list.</summary>
    /// <remarks>
    /// <c>O(list)</c>, which is <c>O(changed)</c> for a Household's members or a Building's
    /// occupants — both small by construction — and is what keeps the staggered tier's per-row cost
    /// bounded rather than quadratic in the population.
    /// </remarks>
    private static bool Lists(IndexList list, int owner, int node)
    {
        foreach (int element in list.Walk(owner))
        {
            if (element == node)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Every Segment names two live nodes, is traversable, and agrees with its own Arcs' masks.
    /// </summary>
    /// <remarks>
    /// <b>Staggered rather than end-of-run, because the failure it catches is silent.</b>
    /// <c>RoadGraph.RebuildDerived</c> skips a Segment whose endpoint is dangling — correctly, since a
    /// rebuild runs on the load path — so such a Segment folds into the State Hash while appearing in
    /// no adjacency. Nothing else in the project would ever report it, and a whole-world walk that ran
    /// once at the end of a 100,000-Tick run would name the Tick it finished on rather than the Tick
    /// it happened on.
    /// </remarks>
    internal static void SegmentsAreWellFormed(
        World world, int slice, int slices, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        RoadSegmentTable segments = world.Roads.Segments;
        RoadNodeTable nodes = world.Roads.Nodes;

        (int from, int to) = InvariantRegistry.Range(slice, slices, segments.Rows.SlotCount);

        for (int slot = from; slot < to; slot++)
        {
            if (!segments.Rows.IsLive(slot))
            {
                continue;
            }

            bool endpoints = nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                && nodes.Rows.TryResolve(segments.NodeB[slot], out int b)
                && a != b;

            report.Require(endpoints, Invariant.RoadSegmentEndpointsExist, slot);

            report.Require(
                segments.LengthTiles[slot].Raw > 0 && segments.FreeFlow[slot].Raw > 0,
                Invariant.RoadSegmentIsTraversable,
                slot);

            byte union = (byte)(segments.ModesForward[slot] | segments.ModesBackward[slot]);

            report.Require(
                segments.Modes[slot] == union,
                Invariant.SegmentModesAreTheUnionOfItsArcs,
                slot,
                segments.Modes[slot]);
        }
    }

    /// <summary>
    /// The CSR slices partition the Arc array, and every Arc is a direction of the Segment it names.
    /// </summary>
    /// <remarks>
    /// <b>The spike's <c>AssertWellFormed</c>, promoted from a sanity walk to the whole-world tier.</b>
    /// It is one walk over every Arc plus one over every node, which is <c>02 §10</c>'s definition of
    /// an end-of-run check; and it is the only place that can see the adjacency as a <em>structure</em>
    /// rather than an Arc at a time, because the property being checked is that the slices tile the
    /// array with no gap and no overlap.
    /// </remarks>
    internal static void TheAdjacencyDescribesTheSegments(World world, InvariantRegistry report)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(report);

        RoadNodeTable nodes = world.Roads.Nodes;
        RoadSegmentTable segments = world.Roads.Segments;
        RoadArcs arcs = world.Roads.Arcs;

        int expected = 0;

        // The slices tile the array: walked in slot order, each live node's run begins exactly where
        // the previous one ended. A gap would be an Arc nothing can reach and an overlap would be one
        // two nodes both claim, and neither is visible from a single Arc.
        for (int slot = 0; slot < nodes.Rows.SlotCount; slot++)
        {
            report.Require(
                nodes.ArcStart[slot] == expected, Invariant.ArcsAreDirectionsOfTheirSegments, slot);

            expected += nodes.ArcCount[slot];

            for (int arc = nodes.ArcStart[slot]; arc < expected; arc++)
            {
                int segment = arcs.Segment[arc];
                int target = arcs.Target[arc];

                bool describes =
                    segments.Rows.IsLive(segment)
                    && target != slot
                    && nodes.Rows.TryResolve(segments.NodeA[segment], out int a)
                    && nodes.Rows.TryResolve(segments.NodeB[segment], out int b)
                    && ((a == slot && b == target) || (b == slot && a == target));

                report.Require(
                    describes, Invariant.ArcsAreDirectionsOfTheirSegments, arc, segment);
            }
        }

        report.Require(
            expected == arcs.Count, Invariant.ArcsAreDirectionsOfTheirSegments, expected, arcs.Count);
    }
}
