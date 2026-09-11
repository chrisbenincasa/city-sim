using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0045</c> row 31 task 2: what stands behind an edge, and what happens to it when somebody
/// crosses back.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Hinterland was an economy with no inhabitants.</b> It stated a rent, a centrality and a purse
/// range, and every Household that ever crossed one of its gates was invented by the caller of an
/// <c>Arrive</c> command. These tests are about the stock: that the opening count is exactly what the
/// file declared, that a returning family joins the row it actually matches, and that neither number
/// can move without the account saying why.
/// </para>
/// <para>
/// ⚠ <b>Nothing here draws the stock down</b>, because nothing does yet — a prospect presenting
/// itself is task 3 and the engine that generates the occasion is task 4. What is under test is the
/// ownership: the rows, their index, and the two equations over them.
/// </para>
/// </remarks>
public sealed class HinterlandStockTests
{
    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>The only shipped world whose edges have anybody behind them.</summary>
    private static World Attracted() => new(1_000, Shipped("attracted.toml"));

    /// <summary>A composition no file authors: three Tier 1 adults and five children.</summary>
    private static HinterlandComposition Unauthored() => new(1, 3, 0, 0, 5, 0);

    private static Violation CaughtAtEnd(World world) =>
        Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world)).Violation;

    /// <summary>Every authored entry is a row, with the count and the composition the file stated.</summary>
    /// <remarks>
    /// <b>At construction and not on the first observation</b> (<c>plans/0073</c> D11). A stock filled
    /// in by whoever looked first would be a population that depended on being watched, and a reload
    /// would either double it or lose it.
    /// </remarks>
    [Fact]
    public void The_opening_stock_is_exactly_what_the_file_declared()
    {
        World world = Attracted();

        Assert.Equal(
            world.Rules.HinterlandPopulations.Length, world.HinterlandPopulation.Rows.LiveCount);

        long people = 0;
        long households = 0;

        foreach (HinterlandDefinition hinterland in world.Rules.Hinterlands)
        {
            for (int entry = 0; entry < hinterland.PopulationCount; entry++)
            {
                HinterlandPopulationDefinition declared =
                    world.Rules.HinterlandPopulations[hinterland.PopulationFirst + entry];

                Assert.True(
                    world.HinterlandCompositions.TryFind(
                        world.HinterlandPopulation,
                        hinterland.Edge,
                        HinterlandComposition.Of(declared),
                        out int slot));

                Assert.Equal(declared.Households, world.HinterlandPopulation.Stock[slot]);
                Assert.Equal(declared.Households, world.HinterlandPopulation.Target[slot]);
                Assert.Equal(declared.Households, world.HinterlandPopulation.Opening[slot]);
                Assert.Equal(1, world.HinterlandPopulation.Authored[slot]);
                Assert.Equal(0, world.HinterlandPopulation.Reserved[slot]);
                Assert.Equal(declared.People, world.HinterlandPopulation.People(slot));

                people += declared.People;
                households += declared.Households;
            }
        }

        Assert.Equal(
            people, world.PopulationLedger.OpeningOutsidePeople[PopulationLedgerTable.Slot]);
        Assert.Equal(
            households,
            world.PopulationLedger.OpeningOutsideHouseholds[PopulationLedgerTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A world whose file says nothing about population has four empty edges.</summary>
    /// <remarks>
    /// <b>Absence is the mode this build already had.</b> Every shipped file but one states no
    /// <c>[[hinterland.population]]</c>, so the rows exist, the account opens at zero, and nothing
    /// about the arrival path changes.
    /// </remarks>
    [Fact]
    public void A_file_declaring_no_population_leaves_the_edges_empty()
    {
        var world = new World(1_000, Shipped("bordered.toml"));

        Assert.Equal(HinterlandTable.Edges, world.Hinterlands.Rows.LiveCount);
        Assert.Equal(0, world.HinterlandPopulation.Rows.LiveCount);
        Assert.Equal(0, world.PopulationLedger.OpeningOutsidePeople[PopulationLedgerTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A returning family joins the row that matches it exactly, and opens no second one.</summary>
    [Fact]
    public void A_return_joins_the_group_it_matches()
    {
        World world = Attracted();
        HinterlandPopulationDefinition declared = world.Rules.HinterlandPopulations[1];
        HinterlandComposition composition = HinterlandComposition.Of(declared);
        MapEdge edge = world.Rules.Hinterlands[0].Edge;
        int groups = world.HinterlandPopulation.Rows.LiveCount;

        int slot = world.ReturnToHinterland(edge, composition);

        Assert.Equal(groups, world.HinterlandPopulation.Rows.LiveCount);
        Assert.Equal(declared.Households + 1, world.HinterlandPopulation.Stock[slot]);
        Assert.Equal(1, world.HinterlandPopulation.Returned[slot]);
        Assert.Equal(declared.Households, world.HinterlandPopulation.Target[slot]);

        int edgeSlot = HinterlandTable.SlotOf(edge);

        Assert.Equal(1, world.Hinterlands.ReturnedHouseholds[edgeSlot]);
        Assert.Equal(composition.Members, world.Hinterlands.ReturnedPeople[edgeSlot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A composition nobody authored opens its own row, resting at zero.
    /// </summary>
    /// <remarks>
    /// <b>Not rounded into the nearest authored template</b> (<c>plans/0073</c> D1). A city that loses
    /// a family of eight sends a family of eight back, and the alternative — filing them as the closest
    /// thing the file declared — would quietly edit who left. ⚠ <b>It rests at zero</b>, so the row
    /// drains away again rather than becoming a resting population the designer never authored.
    /// </remarks>
    [Fact]
    public void A_composition_nobody_authored_opens_a_group_that_rests_at_zero()
    {
        World world = Attracted();
        MapEdge edge = world.Rules.Hinterlands[0].Edge;
        int groups = world.HinterlandPopulation.Rows.LiveCount;

        int slot = world.ReturnToHinterland(edge, Unauthored());

        Assert.Equal(groups + 1, world.HinterlandPopulation.Rows.LiveCount);
        Assert.Equal(1, world.HinterlandPopulation.Stock[slot]);
        Assert.Equal(0, world.HinterlandPopulation.Target[slot]);
        Assert.Equal(0, world.HinterlandPopulation.Opening[slot]);
        Assert.Equal(0, world.HinterlandPopulation.Authored[slot]);
        Assert.Equal(8, world.HinterlandPopulation.People(slot));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A composition differing only by a Skill Tier is a different composition.
    /// </summary>
    /// <remarks>
    /// <b>The substitution D1 forbids by name.</b> A Tier 3 adult is not a Tier 1 adult, and the two
    /// groups are drawn on separately — so an index that matched on family shape alone would let the
    /// city drain credentials it never housed.
    /// </remarks>
    [Fact]
    public void A_tier_three_adult_does_not_join_a_tier_one_group()
    {
        World world = Attracted();
        MapEdge edge = world.Rules.Hinterlands[0].Edge;
        HinterlandPopulationDefinition young = world.Rules.HinterlandPopulations[0];
        HinterlandComposition authored = HinterlandComposition.Of(young);

        Assert.Equal(1, authored.AdultsTier1);

        var credentialled = authored with { AdultsTier1 = 0, AdultsTier3 = 1 };

        int opened = world.ReturnToHinterland(edge, credentialled);

        Assert.True(
            world.HinterlandCompositions.TryFind(
                world.HinterlandPopulation, edge, authored, out int untouched));

        Assert.NotEqual(untouched, opened);
        Assert.Equal(young.Households, world.HinterlandPopulation.Stock[untouched]);
        Assert.Equal(1, world.HinterlandPopulation.Stock[opened]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>The same composition behind a different edge is a different group.</summary>
    /// <remarks>
    /// <b>Four Hinterlands drift independently</b> (<c>CONTEXT.md</c> → Hinterland), so the edge is
    /// part of the key. Sharing a row would make a city draining one edge look like a city draining
    /// all four.
    /// </remarks>
    [Fact]
    public void The_same_composition_behind_two_edges_is_two_groups()
    {
        World world = Attracted();
        HinterlandComposition composition = Unauthored();

        int west = world.ReturnToHinterland(MapEdge.West, composition);
        int north = world.ReturnToHinterland(MapEdge.North, composition);

        Assert.NotEqual(west, north);
        Assert.Equal(1, world.HinterlandPopulation.Stock[west]);
        Assert.Equal(1, world.HinterlandPopulation.Stock[north]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Stock that moved for no recorded reason is caught, and the report says how far out.</summary>
    [Fact]
    public void Stock_that_moved_without_a_reason_is_caught()
    {
        World world = Attracted();

        world.HinterlandPopulation.Stock[0]++;

        Violation caught = CaughtAtEnd(world);

        Assert.Equal(Invariant.AHinterlandGroupIsAccounted, caught.Invariant);
        Assert.Equal(0, caught.Slot);
        Assert.Equal(1, caught.Other);
    }

    /// <summary>A queue cannot promise more Households than are standing there.</summary>
    [Fact]
    public void Reserving_more_than_stands_there_is_caught()
    {
        World world = Attracted();

        world.HinterlandPopulation.Reserved[0] = world.HinterlandPopulation.Stock[0] + 1;

        Assert.Equal(
            Invariant.AHinterlandGroupIsAccounted, CaughtAtEnd(world).Invariant);
    }

    /// <summary>
    /// Rebuilding the derived structures reproduces the index and the per-edge lists exactly.
    /// </summary>
    /// <remarks>
    /// <b>What makes the two structures derived rather than merely undeclared.</b> The returned group
    /// below takes a slot in the middle of the table, which is the case an appending rebuild gets
    /// wrong: the live list would be in arrival order and the rebuilt one in slot order, and nothing
    /// but this would notice.
    /// </remarks>
    [Fact]
    public void A_rebuild_reproduces_the_index_and_the_lists()
    {
        World world = Attracted();
        MapEdge edge = world.Rules.Hinterlands[0].Edge;

        int opened = world.ReturnToHinterland(edge, Unauthored());

        int[] listed = new int[HinterlandTable.Edges];

        for (int slot = 0; slot < HinterlandTable.Edges; slot++)
        {
            listed[slot] = world.HinterlandPopulation.Groups(world.Hinterlands).Length(slot);
        }

        world.RebuildDerived();

        for (int slot = 0; slot < HinterlandTable.Edges; slot++)
        {
            Assert.Equal(
                listed[slot], world.HinterlandPopulation.Groups(world.Hinterlands).Length(slot));
        }

        Assert.True(
            world.HinterlandCompositions.TryFind(
                world.HinterlandPopulation, edge, Unauthored(), out int found));

        Assert.Equal(opened, found);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A retired group leaves the index and the list with it.</summary>
    /// <remarks>
    /// <b>The <c>adr/0006</c> half of the index.</b> An entry that outlived its row would be a key the
    /// structure kept for ever, and the next lookup of that composition would resolve to a freed slot.
    /// </remarks>
    [Fact]
    public void A_retired_group_leaves_the_index_and_its_edge()
    {
        World world = Attracted();
        MapEdge edge = world.Rules.Hinterlands[0].Edge;
        int edgeSlot = HinterlandTable.SlotOf(edge);
        int listed = world.HinterlandPopulation.Groups(world.Hinterlands).Length(edgeSlot);

        int opened = world.HinterlandPopulation.Open(
            world.Hinterlands, edge, Unauthored(), stock: 0, target: 0, authored: false);

        world.HinterlandCompositions.Add(world.HinterlandPopulation, opened);

        Assert.Equal(
            listed + 1, world.HinterlandPopulation.Groups(world.Hinterlands).Length(edgeSlot));

        world.HinterlandPopulation.Retire(world.Hinterlands, opened);
        world.HinterlandCompositions.Remove(world.HinterlandPopulation);

        Assert.Equal(
            listed, world.HinterlandPopulation.Groups(world.Hinterlands).Length(edgeSlot));

        Assert.False(
            world.HinterlandCompositions.TryFind(
                world.HinterlandPopulation, edge, Unauthored(), out _));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A Household's composition is counted off its actual members, tier by tier.
    /// </summary>
    /// <remarks>
    /// <b>Not off the Life Stage's authored template.</b> A family that lost an adult to illness is a
    /// composition no file contains, and it still has to map to a row exactly — so what is counted is
    /// who is in the member list, with age zero being the only marker of childhood there is.
    /// </remarks>
    [Fact]
    public void A_households_composition_is_counted_off_its_members()
    {
        World world = Attracted();

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 2);

        Handle<Citizen> first = world.CreateCitizen(household);

        world.CreateCitizen(household);
        world.Bear(household);

        world.Citizens.SkillTier[world.Citizens.Rows.Resolve(first)] = SchoolingRuleset.TopTier;

        HinterlandDefinition destination = world.Rules.Hinterlands[0];
        HinterlandComposition composition = world.CompositionOf(household, destination);

        Assert.Equal(2, composition.Stage);
        Assert.Equal(1, composition.AdultsTier1);
        Assert.Equal(0, composition.AdultsTier2);
        Assert.Equal(1, composition.AdultsTier3);
        Assert.Equal(1, composition.Children);
        Assert.Equal(3, composition.Members);
    }

    /// <summary>
    /// The purse band belongs to the destination, and a balance outside its range maps to the nearest.
    /// </summary>
    /// <remarks>
    /// <b>A band is a third of one Hinterland's authored range</b>, so the same balance files
    /// differently behind different edges. ⚠ <b>Clamping changes where the family is filed and never
    /// what it carries out of the city</b>: the Money leaves through <c>World.Depart</c>'s supply
    /// decrement, which this does not touch.
    /// </remarks>
    [Fact]
    public void The_purse_band_is_the_destinations_and_clamps_to_it()
    {
        World world = Attracted();

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);

        world.CreateCitizen(household);

        HinterlandDefinition destination = world.Rules.Hinterlands[0];

        world.Endow(household, destination.EmigrantBalanceMax + new Money(1_000_000));

        Assert.Equal(
            HinterlandDefinition.MoneyBands - 1,
            world.CompositionOf(household, destination).MoneyBand);

        Assert.Equal(
            HinterlandDefinition.MoneyBands - 1,
            destination.BandOf(destination.EmigrantBalanceMax));

        Assert.Equal(0, destination.BandOf(destination.EmigrantBalanceMin));
    }

    /// <summary>
    /// A Skill Tier outside <c>adr/0104</c>'s three is reported, and the adult is still counted.
    /// </summary>
    /// <remarks>
    /// <b>Counted at the floor rather than dropped.</b> A composition whose size disagreed with the
    /// Household that produced it would be the worse failure, and a silent one: the account would
    /// balance and the Outside would hold a family missing a member.
    /// </remarks>
    [Fact]
    public void An_undeclared_skill_tier_is_reported_and_still_counted()
    {
        World world = Attracted();

        world.Invariants.Collect = true;

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);
        Handle<Citizen> only = world.CreateCitizen(household);

        world.Citizens.SkillTier[world.Citizens.Rows.Resolve(only)] = 9;

        HinterlandComposition composition =
            world.CompositionOf(household, world.Rules.Hinterlands[0]);

        Assert.Equal(1, composition.Members);
        Assert.Equal(1, composition.AdultsTier1);

        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.AnAdultHoldsADeclaredSkillTier);
    }
}
