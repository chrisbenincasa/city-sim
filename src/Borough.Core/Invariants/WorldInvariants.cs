using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
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

        invariants.Register(InvariantTier.EndOfRun, EveryHandleResolves);
        invariants.Register(InvariantTier.EndOfRun, EveryoneIsInExactlyOnePlace);
        invariants.Register(InvariantTier.EndOfRun, MoneyIsRepresentable);
        invariants.Register(InvariantTier.EndOfRun, LayerMagnitudesAreBounded);
    }

    /// <summary>
    /// <c>adr/0003</c>'s <em>no quantity accumulates without bound</em>, applied to the Map Layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A diffusing Layer with a source and no decay is the exact shape the rule forbids</b>, and
    /// pollution is currently that shape: every emission adds to a Cell's source and nothing takes any
    /// away. Decay is a Ruleset number that does not exist yet, so what stands between the design and
    /// an overflow today is this check and the long-run test that runs it.
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
        int ceiling = MapLayers.PollutionKernel.SourceCeiling;

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
                report.Report(Invariant.HouseholdHomeExists, slot);
                continue;
            }

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

        Count(
            world.Occupants,
            world.Buildings.Rows,
            world.Households.Rows,
            Invariant.HouseholdIsInExactlyOneBuilding,
            report);
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
        HouseholdTable households = world.Households;
        long total = 0;

        for (int slot = 0; slot < households.Rows.SlotCount; slot++)
        {
            if (!households.Rows.IsLive(slot))
            {
                continue;
            }

            if (!TryAdd(ref total, households.Money[slot])
                || !TryAdd(ref total, households.Savings[slot]))
            {
                report.Report(Invariant.MoneyIsRepresentable, slot);
                return;
            }
        }
    }

    /// <summary>
    /// Walks every list once, counting how many times each element appears.
    /// </summary>
    private static void Count(
        IndexList list, Rows owners, Rows elements, Invariant invariant, InvariantRegistry report)
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
            if (elements.IsLive(element) && seen[element] != 1)
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
    /// Adds, or reports that it cannot.
    /// </summary>
    /// <remarks>
    /// Tested rather than caught. A <c>checked</c> block would express the same thing, but this
    /// check exists precisely because the sum is expected to be near its limit when it fires, and
    /// throwing to detect a condition you are deliberately looking for turns the diagnostic into the
    /// thing being diagnosed.
    /// </remarks>
    private static bool TryAdd(ref long total, Money amount)
    {
        long value = amount.Raw;

        if ((value > 0 && total > long.MaxValue - value)
            || (value < 0 && total < long.MinValue - value))
        {
            return false;
        }

        total += value;
        return true;
    }
}
