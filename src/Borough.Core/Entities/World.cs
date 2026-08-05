namespace Borough.Core.Entities;

using Borough.Core.Arithmetic;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// The tables, in declaration order, and the cross-table operations that keep them consistent.
/// </summary>
/// <remarks>
/// <para>
/// <b>Declaration order is a hash input.</b> The composition rule is <em>tables in declaration order,
/// arrays in index order</em>, and the order is the one below — Lots, Buildings, Households, Citizens
/// — which is also the dependency order, since every handle runs from a later table to an earlier
/// one. Reordering this list re-baselines the State Hash and is a deliberate act.
/// </para>
/// <para>
/// <b>Referential integrity lives here rather than on the tables.</b> No framework enforces that a
/// Household's dwelling lists it as an occupant (adr/0004), so the owning code does — and the owning
/// code for a relationship spanning two tables is neither of them. It also keeps
/// <c>BOR0901</c> honest: a table holding a reference to another table would be undeclared state
/// sitting beside the columns.
/// </para>
/// <para>
/// <b>There is no <c>step()</c> here, and no Tick.</b> Slice 5 builds those. This is the storage the
/// Tick will operate on, plus the hash it will be checked with — the hash is generated from the field
/// declaration, so it is a property of the table layer and building it after the Tick would mean
/// building it twice.
/// </para>
/// </remarks>
public sealed class World
{
    /// <summary>
    /// The State Hash's starting value: <c>"Borough"</c> in ASCII, with a version byte.
    /// </summary>
    /// <remarks>
    /// <b>The version byte is where a deliberate re-baseline is recorded.</b> <c>05 §4</c>'s test is
    /// that the hash never moves without somebody saying so; a change to the fold, to the composition
    /// order, or to <c>Randomness.Mix</c> moves every hash in the project at once, and the only way to
    /// tell that from a regression is for the change to be signed. Bump this when re-baselining, never
    /// otherwise.
    /// </remarks>
    private const ulong HashSeed = 0x426F_726F_7567_6801UL;

    private readonly Rows[] _tables;

    /// <param name="citizens">Initial Citizen capacity. Every other table is sized from it.</param>
    /// <remarks>
    /// <b>Sizing is a derivation, not a constant.</b> Every row count S4 task 2 derived is linear in
    /// population, so the ratios are stated per 1,000 Citizens and stay correct at 250k or at 2M —
    /// 360 Households, ~150 Buildings, ~225 Lots. 1M is a floor rather than a cap.
    /// </remarks>
    public World(int citizens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(citizens);

        Lots = new LotTable(PerThousand(citizens, 225));
        Buildings = new BuildingTable(PerThousand(citizens, 150), Lots);
        Households = new HouseholdTable(PerThousand(citizens, 360), Buildings);
        Citizens = new CitizenTable(citizens, Households, Buildings);

        _tables = [Lots.Rows, Buildings.Rows, Households.Rows, Citizens.Rows];

        Invariants = new InvariantRegistry();
        WorldInvariants.RegisterAll(Invariants);
    }

    /// <summary>Parcels of land.</summary>
    public LotTable Lots { get; }

    /// <summary>Structures.</summary>
    public BuildingTable Buildings { get; }

    /// <summary>People sharing a dwelling and finances.</summary>
    public HouseholdTable Households { get; }

    /// <summary>People.</summary>
    public CitizenTable Citizens { get; }

    /// <summary>Every table, in the declaration order the hash folds them in.</summary>
    public ReadOnlySpan<Rows> Tables => _tables;

    /// <summary>
    /// The three tiers of <c>02 §10</c>, and the channel the write sites below report through.
    /// </summary>
    /// <remarks>
    /// <b>Owned here rather than by the Simulation, because the claims are about this world.</b> The
    /// Simulation drives the tiers — when a slice runs is a Tick concern — but a world built by hand
    /// for a test has the same invariants as one a session played, and it would be able to violate
    /// them silently if the registry arrived with the Tick loop.
    /// </remarks>
    public InvariantRegistry Invariants { get; }

    /// <summary>
    /// The State Hash: every saved column of every table, folded through slice 2's <c>mix</c>.
    /// </summary>
    /// <remarks>
    /// <b>Values, never identity.</b> A handle's index is identity — a slot address assigned by a
    /// free list — so a handle column folds the target row's monotonic never-reused id instead. See
    /// <see cref="HandleColumn{TTarget}"/> for what that buys and what it costs.
    /// </remarks>
    public ulong HashState()
    {
        ulong hash = HashSeed;

        foreach (Rows table in _tables)
        {
            table.Fold(ref hash);
        }

        return hash;
    }

    /// <summary>Adds a Household to a Building, linking it into the Building's occupant list.</summary>
    /// <summary>Adds a Household to a Building, linking it into the Building's occupant list.</summary>
    public Handle<Household> CreateHousehold(Handle<Building> dwelling, byte lifeStage)
    {
        int buildingSlot = Buildings.Rows.Resolve(dwelling);

        Handle<Household> handle = Households.Rows.Allocate();
        int slot = Households.Rows.Resolve(handle);

        Households.Dwelling[slot] = dwelling;
        Households.LifeStage[slot] = lifeStage;

        Invariants.Require(
            !Lists(Occupants, buildingSlot, slot),
            Invariant.HouseholdIsNotAlreadyInThisBuilding,
            slot,
            buildingSlot);

        Occupants.InsertOrdered(buildingSlot, slot);

        return handle;
    }

    /// <summary>Adds a Citizen to a Household, linking it into the Household's member list.</summary>
    /// <summary>Adds a Citizen to a Household, linking it into the Household's member list.</summary>
    public Handle<Citizen> CreateCitizen(Handle<Household> household, Ticks nextEventTick)
    {
        int householdSlot = Households.Rows.Resolve(household);

        Handle<Citizen> handle = Citizens.Rows.Allocate();
        int slot = Citizens.Rows.Resolve(handle);

        Citizens.HouseholdOf[slot] = household;
        Citizens.NextEventTick[slot] = nextEventTick;

        // 02 §10's per-Tick tier: O(changed), at the write site. A member list is small by
        // construction, so this is the cheap half of *no Citizen in two places* — complete within
        // one Household and blind across two, which is what the end-of-run walk is for.
        Invariants.Require(
            !Lists(Members, householdSlot, slot),
            Invariant.CitizenIsNotAlreadyInThisHousehold,
            slot,
            householdSlot);

        Members.InsertOrdered(householdSlot, slot);

        return handle;
    }

    /// <summary>Whether <paramref name="node"/> is already in <paramref name="owner"/>'s list.</summary>
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

    /// <summary>Retires a Citizen, unlinking it from its Household first.</summary>
    public void DestroyCitizen(Handle<Citizen> citizen)
    {
        int slot = Citizens.Rows.Resolve(citizen);

        if (Households.Rows.TryResolve(Citizens.HouseholdOf[slot], out int householdSlot))
        {
            Members.Remove(householdSlot, slot);
        }

        Citizens.Rows.Free(citizen);
    }

    /// <summary>
    /// Retires a Household and every Citizen in it, unlinking it from its dwelling.
    /// </summary>
    /// <remarks>
    /// The members go with it rather than being left behind, because a Citizen whose Household handle
    /// is stale is a row nothing can reach and nothing will free — which is adr/0006's rule about
    /// collections that only grow, arriving through the back door.
    /// </remarks>
    public void DestroyHousehold(Handle<Household> household)
    {
        int slot = Households.Rows.Resolve(household);

        int member = Members.PopFront(slot);
        while (member != Rows.NoSlot)
        {
            Citizens.Rows.Free(Citizens.Rows.At(member));
            member = Members.PopFront(slot);
        }

        if (Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot))
        {
            Occupants.Remove(buildingSlot, slot);
        }

        Households.Rows.Free(household);
    }

    /// <summary>
    /// Rebuilds every <see cref="Disposition.Derived"/> structure from saved state.
    /// </summary>
    /// <remarks>
    /// <b>This is what a load will call, and what proves the declaration was honest.</b> Every list
    /// rebuilt here is one the save does not write and the hash does not fold; the claim that made
    /// that legal is that each is a pure function of saved state. Calling this on a running world and
    /// finding the lists unchanged is the test of that claim — and it is a test the hash cannot
    /// perform for us, precisely because these fields are outside it.
    /// </remarks>
    public void RebuildDerived()
    {
        Buildings.OccupantHead.Span.Clear();
        Buildings.OccupantTail.Span.Clear();
        Households.DwellingNext.Span.Clear();
        Households.MemberHead.Span.Clear();
        Households.MemberTail.Span.Clear();
        Citizens.MemberNext.Span.Clear();

        IndexList occupants = Occupants;
        for (int slot = 0; slot < Households.Rows.SlotCount; slot++)
        {
            if (Households.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot))
            {
                occupants.InsertOrdered(buildingSlot, slot);
            }
        }

        IndexList members = Members;
        for (int slot = 0; slot < Citizens.Rows.SlotCount; slot++)
        {
            if (Citizens.Rows.IsLive(slot)
                && Households.Rows.TryResolve(Citizens.HouseholdOf[slot], out int householdSlot))
            {
                members.InsertOrdered(householdSlot, slot);
            }
        }
    }

    /// <summary>The Households living in each Building.</summary>
    /// <remarks>
    /// Bound freshly on each use rather than cached: the spans are over the live prefix of their
    /// columns, and that prefix moves when a row is allocated. A cached <see cref="IndexList"/> is a
    /// list that cannot see the row you just created.
    /// </remarks>
    public IndexList Occupants =>
        new(Buildings.OccupantHead, Buildings.OccupantTail, Households.DwellingNext);

    /// <summary>The Citizens in each Household.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList Members =>
        new(Households.MemberHead, Households.MemberTail, Citizens.MemberNext);

    private static int PerThousand(int citizens, int rows)
    {
        long scaled = IntegerMath.FloorDiv(((long)citizens * rows) + 999, 1000);
        return scaled > int.MaxValue ? int.MaxValue : (int)scaled;
    }
}
