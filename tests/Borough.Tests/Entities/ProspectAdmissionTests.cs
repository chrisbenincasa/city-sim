using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0045</c> row 31 task 3: the prospect, and the door that admits exactly it.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is a seam that used to exist.</b> The old path drew a purse to ask whether
/// somebody could afford to come, then created a Household that drew its own — so the family that
/// could afford to come was not the family that came, and every preference an arrival had appeared
/// for the first time <em>after</em> it had already chosen. Here the family exists before the
/// comparison and <c>World.TryAdmitProspect</c> admits that one (<c>plans/0073</c> D4, D7).
/// </para>
/// <para>
/// ⚠ <b>Nothing here generates an occasion.</b> Who presents themselves, how often, and what happens
/// to somebody a full gate turns away are task 4's. These tests hand the door a prospect and assert
/// what it does with it — including, at length, what it does <em>not</em> do when it refuses.
/// </para>
/// </remarks>
public sealed class ProspectAdmissionTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>The only shipped world whose edges have anybody behind them, with a city on it.</summary>
    private static World Attracted()
    {
        World world = new(1_000, Shipped("attracted.toml"), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>The first live Outside Connection standing on <paramref name="edge"/>.</summary>
    private static int GateOn(World world, MapEdge edge)
    {
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot])
                && world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot)
                && world.EdgeOf(lot) == edge)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"this world has no gate on its {edge} edge.");
    }

    /// <summary>The group behind <paramref name="edge"/> holding Households in <paramref name="stage"/>.</summary>
    private static int GroupOf(World world, MapEdge edge, byte stage)
    {
        foreach (int slot in world.HinterlandPopulation.Groups(world.Hinterlands)
            .Walk(HinterlandTable.SlotOf(edge)))
        {
            if (world.HinterlandPopulation.Stage[slot] == stage)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"nobody in stage {stage} stands behind {edge}.");
    }

    private static ArrivalProspect Present(World world, MapEdge edge, byte stage, ulong identity)
    {
        int group = GroupOf(world, edge, stage);

        Assert.True(world.Rules.TryHinterland(edge, out HinterlandDefinition source));

        return ArrivalProspect.Of(
            Key,
            source,
            world.HinterlandPopulation.Rows.At(group),
            edge,
            world.HinterlandPopulation.CompositionAt(group),
            identity);
    }

    /// <summary>Everything a refusal must leave exactly where it found it.</summary>
    private readonly record struct Standing(
        int Stock, int Reserved, long Admitted, int Today, int Day, long People,
        long Households, int Pool, int Rows, int Citizens)
    {
        public static Standing Of(World world, int group, int gate) =>
            new(
                world.HinterlandPopulation.Stock[group],
                world.HinterlandPopulation.Reserved[group],
                world.HinterlandPopulation.Admitted[group],
                world.Buildings.ArrivalsToday[gate],
                world.Buildings.ArrivalDay[gate],
                world.PopulationLedger.Admissions[PopulationLedgerTable.Slot],
                world.PopulationLedger.HouseholdsAdmitted[PopulationLedgerTable.Slot],
                world.UnplacedPool.Count,
                world.Households.Rows.LiveCount,
                world.Citizens.Rows.LiveCount);
    }

    /// <summary>The purse that was compared is the purse that arrives.</summary>
    /// <remarks>
    /// 🔴 <b>The seam this row closed.</b> <c>PlacementEngine.ProspectCrosses</c>'s old overload drew
    /// on the gate's id and an ordinal; <c>World.TryArrive</c> then drew again on the new Household's.
    /// A family could pass the affordability filter and arrive unable to afford anything it was shown.
    /// </remarks>
    [Fact]
    public void The_family_that_compared_the_city_is_the_family_that_arrives()
    {
        World world = Attracted();
        ArrivalProspect prospect = Present(world, MapEdge.West, stage: 1, identity: 0x5EED);

        Assert.Equal(
            Admission.Admitted,
            world.TryAdmitProspect(
                prospect, world.Buildings.Rows.At(GateOn(world, MapEdge.West)), Ticks.Zero,
                out Handle<Household> household));

        int slot = world.Households.Rows.Resolve(household);

        Assert.Equal(prospect.Purse, world.BalanceOf(household));
        Assert.Equal(prospect.Identity, world.Households.ChoiceIdentity[slot]);
        Assert.Equal(prospect.Identity, world.Households.TasteIdentity(slot));
        Assert.Equal((byte)MapEdge.West, world.Households.ArrivalEdge[slot]);
        Assert.Equal(1, world.Households.Arrived[slot]);
        Assert.Equal(prospect.Stage, world.Households.LifeStage[slot]);
    }

    /// <summary>
    /// A purse is drawn inside the band its group is keyed by, whoever the family is.
    /// </summary>
    /// <remarks>
    /// <b>The band is the storage key, so a draw outside it would file a family in a row it does not
    /// belong to</b> — and a Departure computing the band back from the balance would then land
    /// somewhere else again.
    /// </remarks>
    [Fact]
    public void Every_prospect_carries_what_its_band_says_it_carries()
    {
        World world = Attracted();

        Assert.True(world.Rules.TryHinterland(MapEdge.West, out HinterlandDefinition west));

        for (byte stage = 1; stage <= 5; stage += 4)
        {
            for (ulong identity = 1; identity < 200; identity++)
            {
                ArrivalProspect prospect = Present(world, MapEdge.West, stage, identity);
                int band = prospect.Composition.MoneyBand;

                Assert.InRange(
                    prospect.Purse.Raw, west.BandFloor(band).Raw, west.BandCeiling(band).Raw);

                Assert.Equal(band, west.BandOf(prospect.Purse));
            }
        }
    }

    /// <summary>
    /// A family arrives with the children it has and the credentials its adults hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>World.AddMember</c> started everybody at the floor tier and stage alone created no
    /// children</b>, so the old door could only ever admit <em>n</em> identical adults. A
    /// <c>[[hinterland.population]]</c> states who a family is, and this is the door taking it
    /// literally (<c>plans/0073</c> D7).
    /// </para>
    /// <para>
    /// ⚠ <b>An imported child is not a birth.</b> Recording one would credit the city with a
    /// fertility it did not have, and <c>adr/0023</c>'s finite stock is the claim that would be
    /// measured against.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_family_arrives_with_real_children_and_real_credentials()
    {
        World world = Attracted();
        long births = world.PopulationLedger.Births[PopulationLedgerTable.Slot];

        ArrivalProspect prospect = Present(world, MapEdge.West, stage: 2, identity: 0xFA11);

        Assert.Equal(2, prospect.Composition.Children);
        Assert.Equal(2, prospect.Composition.Adults);

        Assert.Equal(
            Admission.Admitted,
            world.TryAdmitProspect(
                prospect, world.Buildings.Rows.At(GateOn(world, MapEdge.West)), Ticks.Zero,
                out Handle<Household> household));

        int slot = world.Households.Rows.Resolve(household);
        var tiers = new List<byte>();
        int children = 0;

        foreach (int member in world.Members.Walk(slot))
        {
            if (world.Citizens.Age[member] == 0)
            {
                children++;
                Assert.Equal(SchoolingRuleset.FloorTier, world.Citizens.SkillTier[member]);
            }
            else
            {
                tiers.Add(world.Citizens.SkillTier[member]);
            }
        }

        Assert.Equal(2, children);
        Assert.Equal([(byte)1, (byte)2], tiers.Order());

        Assert.Equal(births, world.PopulationLedger.Births[PopulationLedgerTable.Slot]);
        Assert.Equal(4, world.PopulationLedger.Admissions[PopulationLedgerTable.Slot]);
        Assert.Equal(1, world.PopulationLedger.HouseholdsAdmitted[PopulationLedgerTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Admitting one Household spends one, on its group and on its edge.</summary>
    [Fact]
    public void An_admission_leaves_the_edge_one_family_smaller()
    {
        World world = Attracted();
        int group = GroupOf(world, MapEdge.West, stage: 1);
        int edge = HinterlandTable.SlotOf(MapEdge.West);

        int stock = world.HinterlandPopulation.Stock[group];
        int members = world.HinterlandPopulation.Members(group);

        Assert.Equal(
            Admission.Admitted,
            world.TryAdmitProspect(
                Present(world, MapEdge.West, stage: 1, identity: 11),
                world.Buildings.Rows.At(GateOn(world, MapEdge.West)),
                Ticks.Zero,
                out _));

        Assert.Equal(stock - 1, world.HinterlandPopulation.Stock[group]);
        Assert.Equal(1, world.HinterlandPopulation.Admitted[group]);
        Assert.Equal(1, world.Hinterlands.AdmittedHouseholds[edge]);
        Assert.Equal(members, world.Hinterlands.AdmittedPeople[edge]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A gate on another edge opens onto another market, and admits nobody from this one.</summary>
    [Fact]
    public void A_gate_on_another_edge_admits_nobody_and_moves_nothing()
    {
        World world = Attracted();
        int group = GroupOf(world, MapEdge.West, stage: 1);
        int gate = GateOn(world, MapEdge.South);
        Standing before = Standing.Of(world, group, gate);

        Assert.Equal(
            Admission.GateIsOnAnotherEdge,
            world.TryAdmitProspect(
                Present(world, MapEdge.West, stage: 1, identity: 3),
                world.Buildings.Rows.At(gate), Ticks.Zero, out Handle<Household> household));

        Assert.Equal(default, household);
        Assert.Equal(before, Standing.Of(world, group, gate));
    }

    /// <summary>A Building that is not a gate is a caller's mistake and is refused as one.</summary>
    [Fact]
    public void A_building_that_is_not_a_gate_admits_nobody()
    {
        World world = Attracted();
        int group = GroupOf(world, MapEdge.West, stage: 1);
        int gate = GateOn(world, MapEdge.West);
        Standing before = Standing.Of(world, group, gate);

        int dwelling = -1;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount && dwelling < 0; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                dwelling = slot;
            }
        }

        Assert.Equal(
            Admission.GateAdmitsNobody,
            world.TryAdmitProspect(
                Present(world, MapEdge.West, stage: 1, identity: 4),
                world.Buildings.Rows.At(dwelling), Ticks.Zero, out _));

        Assert.Equal(before, Standing.Of(world, group, gate));
    }

    /// <summary>A full gate turns a family away and the Outside still has it.</summary>
    /// <remarks>
    /// <b>The ceiling is a rate and this is the shape that makes it one.</b> Admitting on the far
    /// side of it would make <c>arrivals_per_day</c> a suggestion; refunding the stock would make a
    /// full gate a reason for people to stop existing.
    /// </remarks>
    [Fact]
    public void A_full_gate_admits_nobody_and_spends_no_stock()
    {
        World world = Attracted();
        int gate = GateOn(world, MapEdge.West);
        int group = GroupOf(world, MapEdge.West, stage: 1);
        int ceiling = world.Rules.Kind(world.Buildings.Kind[gate]).ArrivalsPerDay;

        for (int admitted = 0; admitted < ceiling; admitted++)
        {
            Assert.Equal(
                Admission.Admitted,
                world.TryAdmitProspect(
                    Present(world, MapEdge.West, stage: 1, identity: (ulong)admitted + 1),
                    world.Buildings.Rows.At(gate), Ticks.Zero, out _));
        }

        Standing before = Standing.Of(world, group, gate);

        Assert.Equal(
            Admission.GateIsFullToday,
            world.TryAdmitProspect(
                Present(world, MapEdge.West, stage: 1, identity: 9999),
                world.Buildings.Rows.At(gate), Ticks.Zero, out _));

        Assert.Equal(before, Standing.Of(world, group, gate));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>An empty group presents nobody, however willing the city is to take them.</summary>
    [Fact]
    public void A_spent_stock_admits_nobody()
    {
        World world = Attracted();
        int group = GroupOf(world, MapEdge.West, stage: 1);
        int gate = GateOn(world, MapEdge.West);

        world.HinterlandPopulation.Stock[group] = 0;

        Standing before = Standing.Of(world, group, gate);

        Assert.Equal(
            Admission.StockIsSpent,
            world.TryAdmitProspect(
                Present(world, MapEdge.West, stage: 1, identity: 5),
                world.Buildings.Rows.At(gate), Ticks.Zero, out _));

        Assert.Equal(before, Standing.Of(world, group, gate));
    }

    /// <summary>
    /// A Household already promised to a gate cannot also be drawn by somebody else.
    /// </summary>
    /// <remarks>
    /// <b>Reserved is counted inside the stock and not beside it</b>, so the free count is what
    /// admission reads. A family queueing outside a full gate has not left the Outside — what it
    /// cannot be is spent twice.
    /// </remarks>
    [Fact]
    public void Reserved_stock_is_not_available_to_admit()
    {
        World world = Attracted();
        int group = GroupOf(world, MapEdge.West, stage: 1);
        int gate = GateOn(world, MapEdge.West);

        world.HinterlandPopulation.Stock[group] = 1;
        world.HinterlandPopulation.Reserved[group] = 1;

        Standing before = Standing.Of(world, group, gate);

        Assert.Equal(
            Admission.StockIsSpent,
            world.TryAdmitProspect(
                Present(world, MapEdge.West, stage: 1, identity: 6),
                world.Buildings.Rows.At(gate), Ticks.Zero, out _));

        Assert.Equal(before, Standing.Of(world, group, gate));
    }

    /// <summary>A prospect whose group has been retired since it was made admits nobody.</summary>
    [Fact]
    public void A_prospect_whose_group_is_gone_admits_nobody()
    {
        World world = Attracted();
        ArrivalProspect prospect = Present(world, MapEdge.West, stage: 5, identity: 8);

        int group = world.HinterlandPopulation.Rows.Resolve(prospect.Group);

        world.HinterlandPopulation.Stock[group] = 0;
        world.HinterlandPopulation.Retire(world.Hinterlands, group);
        world.HinterlandCompositions.Remove(world.HinterlandPopulation);

        Assert.Equal(
            Admission.GroupIsGone,
            world.TryAdmitProspect(
                prospect, world.Buildings.Rows.At(GateOn(world, MapEdge.West)), Ticks.Zero, out _));
    }

    /// <summary>
    /// A prospect that does not describe the row it names is refused rather than admitted from it.
    /// </summary>
    /// <remarks>
    /// <b>A handle outlives a retirement.</b> A recycled slot holding a different composition would
    /// otherwise admit a family the Outside never had and debit a group it never stood in.
    /// </remarks>
    [Fact]
    public void A_prospect_that_does_not_match_its_group_admits_nobody()
    {
        World world = Attracted();
        int group = GroupOf(world, MapEdge.West, stage: 1);
        int gate = GateOn(world, MapEdge.West);

        ArrivalProspect prospect = Present(world, MapEdge.West, stage: 1, identity: 12) with
        {
            Composition = new HinterlandComposition(1, 4, 0, 0, 9, 0)
        };

        Standing before = Standing.Of(world, group, gate);

        Assert.Equal(
            Admission.ProspectIsNotOfThatGroup,
            world.TryAdmitProspect(
                prospect, world.Buildings.Rows.At(gate), Ticks.Zero, out _));

        Assert.Equal(before, Standing.Of(world, group, gate));
    }

    /// <summary>
    /// A refusal does not roll a gate's meter over, so yesterday's full gate is still full to it.
    /// </summary>
    /// <remarks>
    /// <b>An old Day's quota reads as zero-used and is written only when an admission succeeds</b>
    /// (<c>plans/0073</c> D7). Resetting during validation would let a family the gate turned away
    /// clear the meter on its way out.
    /// </remarks>
    [Fact]
    public void A_refusal_does_not_roll_a_stale_meter_over()
    {
        World world = Attracted();
        int gate = GateOn(world, MapEdge.West);
        int group = GroupOf(world, MapEdge.West, stage: 1);

        world.Buildings.ArrivalDay[gate] = -1;
        world.Buildings.ArrivalsToday[gate] = 9_000;
        world.HinterlandPopulation.Stock[group] = 0;

        Standing before = Standing.Of(world, group, gate);

        Assert.Equal(
            Admission.StockIsSpent,
            world.TryAdmitProspect(
                Present(world, MapEdge.West, stage: 1, identity: 13),
                world.Buildings.Rows.At(gate), Ticks.Zero, out _));

        Assert.Equal(before, Standing.Of(world, group, gate));
        Assert.Equal(-1, world.Buildings.ArrivalDay[gate]);
        Assert.Equal(9_000, world.Buildings.ArrivalsToday[gate]);
    }

    /// <summary>The arrival joins the Pool at the gate it came through, like every other arrival.</summary>
    [Fact]
    public void An_admitted_family_waits_in_the_pool_at_its_gate()
    {
        World world = Attracted();
        int gate = GateOn(world, MapEdge.North);
        int before = world.UnplacedPool.Count;

        Assert.Equal(
            Admission.Admitted,
            world.TryAdmitProspect(
                Present(world, MapEdge.North, stage: 1, identity: 21),
                world.Buildings.Rows.At(gate), Ticks.Zero, out Handle<Household> household));

        Assert.Equal(before + 1, world.UnplacedPool.Count);

        int slot = world.Households.Rows.Resolve(household);

        Assert.True(world.Households.IsUnplaced(slot));
        Assert.Equal(
            world.Buildings.Rows.At(gate),
            world.UnplacedPool.GateAt(world.Households.PoolPosition(slot)));
    }
}
